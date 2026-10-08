using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace DeskBuddy {
 public static class CompanionSprites {
  static readonly Dictionary<string,Bitmap> Images=new Dictionary<string,Bitmap>();
  static readonly Dictionary<string,Rectangle> SourceBoxes=new Dictionary<string,Rectangle>();
  static Rectangle ContentBounds(Bitmap image){int left=image.Width,top=image.Height,right=-1,bottom=-1;for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).A>0){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}return right<left?new Rectangle(0,0,image.Width,image.Height):Rectangle.FromLTRB(left,top,right+1,bottom+1);}

  public static bool Draw(Graphics g,string key,bool baby){
   string name=key.EndsWith("hyunji_ch",StringComparison.Ordinal)?"beagle":key.EndsWith("jihyun_ch",StringComparison.Ordinal)?"purple":key.EndsWith("jimin_ch",StringComparison.Ordinal)?"beaver":key.EndsWith("rita_ch",StringComparison.Ordinal)?"hamster":!baby&&key.EndsWith("tae_ch",StringComparison.Ordinal)?"zzio":baby&&key.EndsWith("jiyoung_ch",StringComparison.Ordinal)?"woodstock-baby":null;
   if(name==null)return false;
   if(!baby)name+="-adult";
   Bitmap image;if(!Images.TryGetValue(name,out image)){using(var stream=typeof(CompanionSprites).Assembly.GetManifestResourceStream("DeskBuddy.Companion."+name))using(var original=new Bitmap(stream))image=new Bitmap(original);Images[name]=image;SourceBoxes[name]=(name.StartsWith("purple",StringComparison.Ordinal)||name.StartsWith("beaver",StringComparison.Ordinal))?ContentBounds(image):new Rectangle(0,0,image.Width,image.Height);}
   var saved=g.Save();if(baby){g.TranslateTransform(4,7);g.ScaleTransform(.75f,.75f);}g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
   Rectangle source=SourceBoxes[name];float scale=Math.Min(30f/source.Width,30f/source.Height),width=source.Width*scale,height=source.Height*scale;g.DrawImage(image,new RectangleF((32-width)/2,32-height,width,height),source,GraphicsUnit.Pixel);g.Restore(saved);return true;
  }
 }
}