using Android.App;
using Android.Content;
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
using Microsoft.Azure.CognitiveServices.Vision;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Xamarin.Cognitive.Face.Droid;

namespace FinalApp
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener
    {

        public FaceServiceRestClient faceServiceRestClient = new FaceServiceRestClient("https://westcentralus.api.cognitive.microsoft.com/face/v1.0", "725a30b5298c45fbb006b66933c98614");
        private string groupPersonId = "cvai";
        public ImageView imageView;
        public Bitmap imageBitMap;
        Button btnTake, btnGallery, btnResult, buttonDetect;
        public List<FaceModel> detectedFaces = new List<FaceModel>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            BottomNavigationView navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation); //gallery
            navigation.SetOnNavigationItemSelectedListener(this);

            imageBitMap = BitmapFactory.DecodeResource(Resources, Resource.Drawable.passportpic);

            imageView = FindViewById<ImageView>(Resource.Id.image);
            imageView.SetImageBitmap(imageBitMap);

            buttonDetect = FindViewById<Button>(Resource.Id.btnDetect);
       
            btnTake = FindViewById<Button>(2131361950); //takePic button 
            btnGallery = FindViewById<Button>(2131361951); //fromgallery  button 
            btnResult = FindViewById<Button>(2131361952); //result

            buttonDetect.Click += delegate
            {
                byte[] bitmapData;
                using (var stream = new MemoryStream())
                {
                    imageBitMap.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
                    bitmapData = stream.ToArray();
                }

                var inputStream = new MemoryStream(bitmapData);
                new DetectTask(this).Execute(inputStream);
            };


          btnTake.Click += delegate
            {
                Intent intent = new Intent(MediaStore.ActionImageCapture);
                StartActivityForResult(intent, 0);
                
            };

          
            btnResult.Click += delegate
            {
                string[] facesID = new string[detectedFaces.Count];
                for (int i = 0; i < detectedFaces.Count; i++) facesID[i] = detectedFaces[i].faceId;

                new IdentificationTask(this, groupPersonId).Execute(facesID);
            };

        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.navigation_home:
                    return true;
                case Resource.Id.navigation_dashboard:
                    return true;
                case Resource.Id.navigation_notifications:
                    return true;
            }
            return false;
        }
    }

    class IdentificationTask : AsyncTask<string, string, string>
    {
        private MainActivity mainActivity;
        private string personGroupId;
        //private ProgressDialog mDialog = new ProgressDialog(Application.Context);
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
                    , 1); // max number of candidates returned 

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
          //  mDialog.Window.SetType(WindowManagerTypes.SystemAlert);
           // mDialog.Show();
        }
        protected override void OnProgressUpdate(params string[] values)
        {
           // mDialog.SetMessage(values[0]);
        }
        protected override void OnPostExecute(string result)
        {
           // mDialog.Dismiss();


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
                    var candidate = identify.candidates[0];
                    var personId = candidate.personId;
                    new PersonDetectionTask(mainActivity, personGroupId).Execute(personId);
                }

            }
        }

    }

    class DetectTask : AsyncTask<Stream, string, string>
    {
        private MainActivity mainActivity;
        private string personGroupId;
        private ProgressBar mDialog = new ProgressBar(Application.Context);

        public DetectTask(MainActivity mainActivity, string personGroupId)
        {
            this.mainActivity = mainActivity;
            this.personGroupId = personGroupId;
        }

        public DetectTask(MainActivity mainActivity)
        {
            this.mainActivity = mainActivity;
        }

        protected override string RunInBackground(params Stream[] @params)
        {
  

            PublishProgress("Detecting...");
            var result = mainActivity.faceServiceRestClient.Detect(@params[0],true,false,null);

            if (result == null )
            {
                PublishProgress("Nothing detected");
                return null;
            }

            PublishProgress($"Detection successfull.{result.Length} face detected.");
            Gson gson = new Gson();
            var stringResult = gson.ToJson(result);
            return stringResult;
        }
        protected override void OnPreExecute()
        {
  
        }
        protected override void OnProgressUpdate(params string[] values)
        {
           
        }

        protected override void OnPostExecute(string result)
        {


            var faces = JsonConvert.DeserializeObject<List<FaceModel>>(result);
            mainActivity.detectedFaces = faces;
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

        public PersonDetectionTask(MainActivity mainActivity)
        {
            this.mainActivity = mainActivity;
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
                DrawHelper.DrawRectangleOnBitmap(mainActivity.imageBitMap,
                 mainActivity.detectedFaces,
                 person.name));
        }

    
    }

}

