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
 public static class DittoTransformation {
  static readonly Dictionary<string,Bitmap> Images=new Dictionary<string,Bitmap>();
  public static bool IsDitto(Data data){return Companions.Key(data).EndsWith("jihyun_ch",StringComparison.Ordinal);}
  public static bool Unlocked(Data data){return IsDitto(data)&&Companions.Level(data)>=5;}
  static Bitmap Load(string form){if(form!="dragon"&&form!="bird")return null;Bitmap image;if(Images.TryGetValue(form,out image))return image;try{using(var stream=typeof(DittoTransformation).Assembly.GetManifestResourceStream("DeskBuddy.Companion."+(form=="dragon"?"meta_after":"meta_after2"))){if(stream==null)return null;using(var original=new Bitmap(stream))image=new Bitmap(original);}Images[form]=image;return image;}catch(Exception ex){Startup.Log("Ditto transformation: "+ex.Message);return null;}}
  public static Bitmap CurrentImage(Data data){return Unlocked(data)?Load(data.DittoForm):null;}
  public static bool Change(Data data,Random random,out string error){error="";if(!Unlocked(data)){error="메타몽의 변신은 레벨 5부터 사용할 수 있어요.";return false;}string form=random.Next(2)==0?"dragon":"bird";if(Load(form)==null){error="변신 이미지를 찾을 수 없어요. 이미지가 포함된 버전으로 다시 빌드해주세요.";return false;}data.DittoForm=form;return true;}
  public static void Restore(Data data){data.DittoForm="";}
  public static void Tests(){var data=new Data{ImagePath="builtin:DeskBuddy.Character.jihyun_ch"};AdminAccess.SetLevel(data,4);if(Unlocked(data))throw new Exception("Ditto transformation level gate");AdminAccess.SetLevel(data,5);if(!Unlocked(data))throw new Exception("Ditto transformation level five unlock");string key=Companions.Key(data);data.Companions[key]=new LittlePet{Growth=10,Bond=7};var random=new Random(51);var forms=new HashSet<string>();string error;for(int i=0;i<32;i++){if(!Change(data,random,out error)||CurrentImage(data)==null)throw new Exception("Transformation resource loading");forms.Add(data.DittoForm);}if(forms.Count!=2||Companions.Key(data)!=key||Companions.Current(data).Bond!=7)throw new Exception("Random transformation keeps Ditto pet");AdminAccess.SetLevel(data,4);if(CurrentImage(data)!=null||Change(data,random,out error))throw new Exception("Transformation hides below level five");AdminAccess.SetLevel(data,5);data.DittoForm="dragon";var copy=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Data>(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(data));if(copy.DittoForm!="dragon"||Companions.Key(copy)!=key||Companions.Current(copy).Bond!=7)throw new Exception("Transformation preserves identity and pet persistence");Restore(copy);if(copy.DittoForm!=""||Companions.Key(copy)!=key)throw new Exception("Transformation restore");data.ImagePath="builtin:DeskBuddy.Character.jiyoung_ch";if(Unlocked(data)||CurrentImage(data)!=null)throw new Exception("Only Ditto can transform");}
 }

}
