using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DeskBuddy {
 public static class CharacterLibrary {
  public static string[] Bundled {get{return typeof(CharacterLibrary).Assembly.GetManifestResourceNames().Where(n=>n.StartsWith("DeskBuddy.Character.",StringComparison.Ordinal)).OrderBy(n=>n.EndsWith(".jiyoung_ch",StringComparison.Ordinal)?0:1).ThenBy(n=>n,StringComparer.Ordinal).Select(n=>"builtin:"+n).ToArray();}}
  public static string Default {get{return Bundled.FirstOrDefault(n=>n.EndsWith(".jiyoung_ch",StringComparison.Ordinal))??Bundled.FirstOrDefault()??"";}}
  public static string Label(string path){string label=path.StartsWith("builtin:")?path.Substring("builtin:DeskBuddy.Character.".Length):Path.GetFileNameWithoutExtension(path);int suffix=label.LastIndexOf("__",StringComparison.Ordinal);if(suffix>=0)label=label.Substring(0,suffix);switch(label){case "jiyoung_ch":return "스누피";case "hyunji_ch":return "가나디";case "jihyun_ch":return "메타몽";case "jimin_ch":return "루피";case "rita_ch":return "햄뿡이";case "tae_ch":return "찌오";}return label;}
  public static Bitmap Read(string path){
   if(path.StartsWith("builtin:",StringComparison.Ordinal))using(var stream=typeof(CharacterLibrary).Assembly.GetManifestResourceStream(path.Substring(8)))using(var image=Image.FromStream(stream))return new Bitmap(image);
   using(var image=Image.FromFile(path))return new Bitmap(image);
  }
  public static IEnumerable<string> Samples(){if(Store.State.CharacterSamples==null)Store.State.CharacterSamples=new List<string>();return Bundled.Concat(Store.State.CharacterSamples.Where(File.Exists)).Distinct();}
  public static string Import(string source){
   if(new FileInfo(source).Length>10*1024*1024)throw new Exception("10MB 이하 이미지를 사용해주세요.");
   using(var image=Image.FromFile(source)){
    if(image.Width>4096||image.Height>4096)throw new Exception("4096px 이하 이미지를 사용해주세요.");
    Directory.CreateDirectory(Store.Root);string target=Path.Combine(Store.Root,Path.GetFileNameWithoutExtension(source)+"__"+Guid.NewGuid().ToString("N")+".png");
    using(var b=new Bitmap(image))b.Save(target,System.Drawing.Imaging.ImageFormat.Png);return target;
   }
  }
 }
 public class SamplePortrait:Control {
  Bitmap image;public SamplePortrait(string path){DoubleBuffered=true;try{image=CharacterLibrary.Read(path);}catch{} }
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(image==null)return;float scale=Math.Min((float)Width/image.Width,(float)Height/image.Height);int w=(int)(image.Width*scale),h=(int)(image.Height*scale);e.Graphics.DrawImage(image,new Rectangle((Width-w)/2,(Height-h)/2,w,h));}
  protected override void Dispose(bool disposing){if(disposing&&image!=null){image.Dispose();image=null;}base.Dispose(disposing);}
 }
}
