using System;
using System.Drawing;
using System.IO;
namespace DeskBuddy {
 public static class FocusTimerArt {
  public static string Remaining(Data data,DateTime now){if(data.FocusEnd==DateTime.MinValue)return "READY";int seconds=(int)Math.Max(0,Math.Ceiling((data.FocusEnd-now).TotalSeconds));return String.Format("{0:00}:{1:00}",seconds/60,seconds%60);}
  public static void Draw(Graphics g,Rectangle bounds,Data data,DateTime now,bool preview){Theme.Frame(g,bounds,Theme.Cream);string text=preview&&data.FocusEnd==DateTime.MinValue?"24:11":Remaining(data,now);using(var font=Theme.PixelFont(24*data.FocusTimerScale/100f))Theme.Text(g,text,font,Theme.Purple,new Rectangle(bounds.X+6,bounds.Y+3,bounds.Width-12,bounds.Height-6),ContentAlignment.MiddleCenter);}
  public static void Tests(){var previous=Store.State;try{var now=new DateTime(2026,10,7,15,0,0);Store.State=new Data{FocusEnd=now.AddSeconds(1451)};if(Remaining(Store.State,now)!="24:11"||Remaining(Store.State,now.AddSeconds(1452))!="00:00")throw new Exception("Focus countdown");using(var pet=new PetForm()){foreach(int scale in new[]{60,100,180}){Store.State.FocusTimerScale=scale;pet.ApplyPetSize();using(var active=pet.RenderSnapshot()){int height=active.Height;Store.State.FocusEnd=DateTime.MinValue;pet.ApplyPetSize();using(var inactive=pet.RenderSnapshot())if(height<=inactive.Height)throw new Exception("Focus timer hiding");Store.State.FocusEnd=now.AddMinutes(25);pet.ApplyPetSize();}}} }finally{Store.State=previous;}}
  public static int Preview(){try{var previous=Store.State;try{Store.State=new Data{FocusEnd=AppClock.Now.AddSeconds(1451),Wander=false};using(var pet=new PetForm())using(var image=pet.RenderSnapshot(true)){image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"focus-timer-preview.png"));}}finally{Store.State=previous;}Tests();File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"focus-timer-validation.txt"),"PASS: countdown formatting, expiry, scale 60/100/180, hiding after cancellation, timer rendering.");return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"focus-timer-validation.txt"),"FAIL: "+ex);return 1;}}
 }
}
