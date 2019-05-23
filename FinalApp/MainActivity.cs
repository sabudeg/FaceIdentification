using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using FaceIdentificationApp.Helper;
using FinalApp.Model;
using GoogleGson;
using Java.IO;
using Java.Util;
using Newtonsoft.Json;
using Plugin.Permissions;
using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Cognitive.Face.Droid;

namespace FinalApp
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private FaceServiceRestClient faceServiceRestClient = new FaceServiceRestClient("https://westcentralus.api.cognitive.microsoft.com/face/v1.0", "725a30b5298c45fbb006b66933c98614");
        private string personGroupId = "cvai";
        public ImageView imageView;
        public Bitmap mBitmap;
        string galleryPath;
        Button btnDetect, btnIdentify, btnTake, btnGallery;
        List<FaceModel> facesDetected = new List<FaceModel>();
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        => PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            mBitmap = BitmapFactory.DecodeResource(Resources, Resource.Drawable.psfix);
            imageView = FindViewById<ImageView>(Resource.Id.imageView);
            imageView.SetImageBitmap(mBitmap);


            btnIdentify = FindViewById<Button>(Resource.Id.btnIdentify);
            btnTake = FindViewById<Button>(Resource.Id.btnTake);
            btnGallery = FindViewById<Button>(Resource.Id.fromGallery);
            btnDetect = FindViewById<Button>(Resource.Id.btnDetect);


            btnTake.Click += delegate
            {
                Intent intent = new Intent(MediaStore.ActionImageCapture);
                StartActivityForResult(intent, 0);
            };

            btnDetect.Click += delegate
            {
                byte[] bitmapData;
                using (var stream = new MemoryStream())
                {
                    //    mBitmap = BitmapFactory.DecodeFile(galleryPath);

                    mBitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
                    bitmapData = stream.ToArray();
                }
                var inputStream = new MemoryStream(bitmapData);
                new DetectTask(this).Execute(inputStream);
            };

            btnGallery.Click += delegate
            {
                Intent intent = new Intent(Intent.ActionPick, Android.Provider.MediaStore.Images.Media.ExternalContentUri);
                StartActivityForResult(intent, 2);
            };

            btnIdentify.Click += delegate
            {
                string[] facesID = new string[facesDetected.Count];
                for (int i = 0; i < facesDetected.Count; i++)
                    facesID[i] = facesDetected[i].faceId;

                new IdentificationTask(this, personGroupId).Execute(facesID);
            };


        }
        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if ((requestCode == 0))
            {
                mBitmap = (Bitmap)data.Extras.Get("data");
                imageView.SetImageBitmap(mBitmap);
            }

            if (requestCode == 2)
            {
                Stream stream = ContentResolver.OpenInputStream(data.Data);
                imageView.SetImageBitmap(BitmapFactory.DecodeStream(stream));

                // mBitmap = BitmapFactory.DecodeFile(data.Data.Path);
                // mBitmap = (Bitmap)BitmapFactory.DecodeStream(stream);

                // galleryPath = data.Data.EncodedPath;
                mBitmap = MediaStore.Images.Media.GetBitmap(ContentResolver, data.Data);
            }
        }

        class DetectTask : AsyncTask<Stream, string, string>
        {
            private MainActivity mainActivity;
            public DetectTask(MainActivity mainActivity)
            {
                this.mainActivity = mainActivity;
            }
            protected override string RunInBackground(params Stream[] @params)
            {
                var result = mainActivity.faceServiceRestClient.Detect(@params[0], true, false, null);
                if (result == null)
                {
                    return null;
                }

                Gson gson = new Gson();
                var stringResult = gson.ToJson(result);
                return stringResult;
            }
            protected override void OnPreExecute()
            {
            }
            protected override void OnPostExecute(string result)
            {
                try
                {
                    var faces = JsonConvert.DeserializeObject<List<FaceModel>>(result);
                    var bitmap = DrawRectanglesOnBitmap(mainActivity.mBitmap, faces);
                    mainActivity.imageView.SetImageBitmap(bitmap);
                    mainActivity.facesDetected = faces;

                }
                catch (Exception e)
                {
                  //  Toast.MakeText(mainActivity.ApplicationContext, "No one detected", ToastLength.Short).Show();
                }

            }
            protected override void OnProgressUpdate(params string[] values)
            {
            }

            private Bitmap DrawRectanglesOnBitmap(Bitmap mBitmap, List<FaceModel> faces)
            {
                Bitmap bitmap = mBitmap.Copy(Bitmap.Config.Argb8888, true);
                Canvas canvas = new Canvas(bitmap);
                Paint paint = new Paint
                {
                    AntiAlias = true
                };
                paint.SetStyle(Paint.Style.Stroke);
                paint.Color = Color.White;
                paint.StrokeWidth = 12;

                foreach (var face in faces)
                {
                    var faceRectangle = face.faceRectangle;
                    canvas.DrawRect(faceRectangle.left,
                        faceRectangle.top,
                        faceRectangle.left + faceRectangle.width,
                        faceRectangle.top + faceRectangle.height,
                        paint);
                }
                return bitmap;

            }

        }


        class IdentificationTask : AsyncTask<string, string, string>
        {
            private MainActivity mainActivity;
            private string personGroupId;
            public IdentificationTask(MainActivity mainActivity, string personGroupId)
            {
                this.mainActivity = mainActivity;
                this.personGroupId = personGroupId;
            }

            protected override string RunInBackground(params string[] @params)
            {
                try
                {
                    UUID[] uuidList = new UUID[@params.Length];
                    for (int i = 0; i < @params.Length; i++)
                        uuidList[i] = UUID.FromString(@params[i]);

                    var result = mainActivity.faceServiceRestClient.Identity(personGroupId
                        , uuidList
                        , 1);

                    Gson gson = new Gson();
                    var resultString = gson.ToJson(result);
                    return resultString;

                }
                catch (System.Exception)
                {
                    return null;
                }
            }
            protected override void OnPreExecute()
            {
            }
            protected override void OnProgressUpdate(params string[] values)
            {
            }
            protected override void OnPostExecute(string result)
            {
                try
                {
                    var identifyList = JsonConvert.DeserializeObject<List<IdentifyResultModel>>(result);
                    foreach (var identify in identifyList)
                    {
                        if (identify.candidates.Count == 0)
                        {
                            Toast.MakeText(mainActivity.ApplicationContext, "No one detected", ToastLength.Long).Show();
                            continue;
                        }
                        else
                        {
                            Toast.MakeText(mainActivity.ApplicationContext, identifyList.Count + " detected.", ToastLength.Long).Show();
                            var candidate = identify.candidates[0];
                            var personId = candidate.personId;
                            new PersonDetectionTask(mainActivity, personGroupId).Execute(personId);
                        }
                    }
                }
                catch (Exception e)
                {
                    Toast.MakeText(mainActivity.ApplicationContext, "No one detected", ToastLength.Long).Show();
                }

            }


        }
        class PersonDetectionTask : AsyncTask<string, string, string>
        {
            private MainActivity mainActivity;
            private string personGroupId;
            public PersonDetectionTask(MainActivity mainActivity, string personGroupId)
            {
                this.mainActivity = mainActivity;
                this.personGroupId = personGroupId;
            }

            protected override string RunInBackground(params string[] @params)
            {
                try
                {
                    UUID uuid = UUID.FromString(@params[0]);

                var person = mainActivity.faceServiceRestClient.GetPerson(personGroupId, uuid);
                Gson gson = new Gson();
                var result = gson.ToJson(person);
                return result;
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            protected override void OnPreExecute()
            {
            }
            protected override void OnProgressUpdate(params string[] values)
            {
            }

            protected override void OnPostExecute(string result)
            {

                var person = JsonConvert.DeserializeObject<PersonModel>(result);
                mainActivity.imageView.SetImageBitmap(
                    DrawHelper.DrawRectangleOnBitmap(mainActivity.mBitmap,
                     mainActivity.facesDetected,
                     person.name));

               // if (mainActivity.faceServiceRestClient.GetPersons(personGroupId)) // EĞER PERSONGRUBUNDA KISI KAYITLIYSA ADINI YAZDIR
            }
        }

    }

}

