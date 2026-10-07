using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace DeskBuddy {
 public static class CompanionSprites {
  static readonly Dictionary<string,Bitmap> Images=new Dictionary<string,Bitmap>();
  public static bool Draw(Graphics g,string key,bool baby){
   string name=key.EndsWith("hyunji_ch",StringComparison.Ordinal)?"beagle":key.EndsWith("jihyun_ch",StringComparison.Ordinal)?"purple":key.EndsWith("jimin_ch",StringComparison.Ordinal)?"beaver":key.EndsWith("rita_ch",StringComparison.Ordinal)?"hamster":null;
   if(name==null)return false;
   Bitmap image;if(!Images.TryGetValue(name,out image)){using(var stream=typeof(CompanionSprites).Assembly.GetManifestResourceStream("DeskBuddy.Companion."+name))using(var original=new Bitmap(stream))image=new Bitmap(original);Images[name]=image;}
   var saved=g.Save();if(baby){g.TranslateTransform(4,7);g.ScaleTransform(.75f,.75f);}g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
   float scale=Math.Min(30f/image.Width,30f/image.Height),width=image.Width*scale,height=image.Height*scale;g.DrawImage(image,new RectangleF((32-width)/2,32-height,width,height),new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel);g.Restore(saved);return true;
  }
 }
}