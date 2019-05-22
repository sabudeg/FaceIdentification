using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using FinalApp.Model;

namespace FaceIdentificationApp.Helper
{
    public class DrawHelper
    {
        public static Bitmap DrawRectangleOnBitmap(Bitmap mBitmap, List<FaceModel> facesDetected, string Name)
        {
            Bitmap bitmap = mBitmap.Copy(Bitmap.Config.Argb8888, true);
            Canvas canvas = new Canvas(bitmap);

            //Rectangle 
            Paint paint = new Paint();
            paint.AntiAlias = true;
            paint.SetStyle(Paint.Style.Stroke);
            paint.Color = Color.White;
            paint.StrokeWidth = 12;

            if (facesDetected != null)
            {
                foreach (var face in facesDetected)
                {
                    var faceRectangle = face.faceRectangle;
                    canvas.DrawRect(faceRectangle.left,
                        faceRectangle.top,
                        faceRectangle.left + faceRectangle.width,
                        faceRectangle.top + faceRectangle.height,
                        paint);

                    int cX = (faceRectangle.left + faceRectangle.width);
                    int cY = (faceRectangle.top + faceRectangle.height);

                    DrawTextOnCanvas(canvas, 100, cX / 2 + cX / 5, cY + 100, Color.White, Name);

                }
            }
            return bitmap;

        }

        private static void DrawTextOnCanvas(Canvas canvas, int textSize, int cX, int cY, Color color, string name)
        {
            Paint paint = new Paint();
            paint.AntiAlias = true;
            paint.SetStyle(Paint.Style.Fill);
            paint.Color = color;
            paint.TextSize = textSize;

            canvas.DrawText(name, cX, cY, paint);
        }
    }
}