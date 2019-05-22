using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FinalApp.Model
{
    public class IdentifyResultModel
    {
        public List<Candidates> candidates { get; set; }
        public string faceId { get; set; }

    }

    public class Candidates
    {
        public double confidence { get; set; }
        public string personId { get; set; }
    }
}