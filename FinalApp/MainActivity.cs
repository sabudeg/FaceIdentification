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
using Java.Util;
using Newtonsoft.Json;
using Plugin.Permissions;
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
        Button btnDetect, btnIdentify, btnTake, btnGallery;
        List<FaceModel> facesDetected = new List<FaceModel>();
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        => PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            mBitmap = BitmapFactory.DecodeResource(Resources, Resource.Drawable.krakow);
            imageView = FindViewById<ImageView>(Resource.Id.imageView);
            imageView.SetImageBitmap(mBitmap);


            //     btnIdentify = FindViewById<Button>(Resource.Id.btnIdentify);
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

                mBitmap = BitmapFactory.DecodeStream(stream);
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
                    PublishProgress("Identifying...");

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
                PublishProgress("Getting person...");
                UUID uuid = UUID.FromString(@params[0]);

                var person = mainActivity.faceServiceRestClient.GetPerson(personGroupId, uuid);
                Gson gson = new Gson();
                var result = gson.ToJson(person);
                return result;
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
                PublishProgress("Detecting...");
                var result = mainActivity.faceServiceRestClient.Detect(@params[0], true, false, null);
                if (result == null)
                {
                    PublishProgress("Detection Finished. Nothing detected");
                    return null;
                }
                PublishProgress($"Detection Finished. {result.Length} face(s) detected");

                Gson gson = new Gson();
                var stringResult = gson.ToJson(result);
                return stringResult;
            }
            protected override void OnPreExecute()
            {
            }
            protected override void OnPostExecute(string result)
            {
                var faces = JsonConvert.DeserializeObject<List<FaceModel>>(result);
                mainActivity.facesDetected = faces;

                string[] facesID = new string[mainActivity.facesDetected.Count];
                for (int i = 0; i < mainActivity.facesDetected.Count; i++)
                    facesID[i] = mainActivity.facesDetected[i].faceId;

                new IdentificationTask(mainActivity, mainActivity.personGroupId).Execute(facesID);

            }
            protected override void OnProgressUpdate(params string[] values)
            {
            }

        }
    }


}

