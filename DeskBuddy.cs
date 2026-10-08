using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace DeskBuddy {

 public static class AppIdentity {
  public static readonly Icon Icon=LoadIcon();
  static Icon LoadIcon(){using(var stream=typeof(AppIdentity).Assembly.GetManifestResourceStream("DeskBuddy.AppIcon"))using(var icon=new Icon(stream))return (Icon)icon.Clone();}
 }

 public static class Startup {
  public static System.Threading.EventWaitHandle OpenRequest;
  public static string LogPath=Path.Combine(Store.Root,"startup.log");
  public static void Log(string message){try{Directory.CreateDirectory(Store.Root);File.AppendAllText(LogPath,AppClock.Now.ToString("yyyy-MM-dd HH:mm:ss")+" "+message+Environment.NewLine,Encoding.UTF8);}catch{}}
  public static void Error(Exception ex){Log(ex.ToString());GameAlert.Show("DeskBuddy를 실행하지 못했습니다.\n"+ex.Message+"\n\n오류 기록: "+LogPath,"DeskBuddy 시작 오류",MessageBoxButtons.OK,MessageBoxIcon.Error);}
 }

 public class TaskItem {
  public string Id=Guid.NewGuid().ToString(); public string Title=""; public DateTime Due;
  public bool Done; public bool Notified; public string Category="업무"; public string LocalDue="", Repeat="한 번", AnchorTime="", ScheduleImage="", EveTime="18:00", SnoozeLocal="", LastCompletedLocal=""; public bool AllDay, EveReminder, EveNotified; public int AppearanceMinutes=30; public int DurationMinutes;
 }
 public class Data {
  public int Fullness=70,Happiness=70,MealsGiven;public string CareLocal=AppClock.Encode(AppClock.Now),LastFood="",LastFedLocal="",RoomWallpaper="기본 벽지",RoomRug="기본 러그",RoomDecoration="기본 장식";
  public List<string> RoomOwned=new List<string>{"기본 벽지","기본 러그","기본 장식"};
  public int Coins=30, Completed, FocusCount, FocusMinutes,ExperienceAdjustment; public int InteractionExperience;public string LastInteractionLocal="";
  public string PetName="스누피", ImagePath="", Equipped="기본"; public int PixelSize=3; public int PetSize=140; public int BubbleScale=100; public int FocusTimerScale=100; public Dictionary<string,LittlePet> Companions=new Dictionary<string,LittlePet>();
  public List<MemoNote> Memos=new List<MemoNote>(); public List<string> Owned=new List<string>{"기본"}; public List<TaskItem> Tasks=new List<TaskItem>();
  public List<ProjectItem> Projects=new List<ProjectItem>(); public List<WorkDiaryItem> Diaries=new List<WorkDiaryItem>();
  public List<KpiItem> Kpis=new List<KpiItem>(); public List<MonthlyReport> MonthlyReports=new List<MonthlyReport>(); public string AiProvider="Claude"; public Dictionary<string,string> AiModels=new Dictionary<string,string>();
  public bool Wander=true, Quiet=false; public bool Cartwheels=true; public List<string> CharacterSamples=new List<string>(); public DateTime FocusEnd=DateTime.MinValue; public string FocusEndLocal=""; public int SessionMinutes; public string GamesDate=""; public int GamesPlayed;
 }
 public static class Store {
  public static readonly string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DeskBuddy");
  public static string FilePath=Path.Combine(Root,"data.json"); public static Data State=new Data(); public static string Warning="";
  public static void Load() {
   if(!File.Exists(FilePath))return;
   try { State=new JavaScriptSerializer().Deserialize<Data>(File.ReadAllText(FilePath,Encoding.UTF8));
    if(State==null || State.Tasks==null || State.Owned==null)throw new Exception("Invalid data");
    Scheduler.Normalize(State);Living.Normalize(State);StickyMemos.Normalize(State);DiaryCore.Normalize(State);State.FocusTimerScale=Math.Max(60,Math.Min(180,State.FocusTimerScale));State.BubbleScale=Math.Max(60,Math.Min(160,State.BubbleScale));if(State.Companions==null)State.Companions=new Dictionary<string,LittlePet>(); State.PixelSize=Math.Max(1,Math.Min(5,State.PixelSize));Store.State.PetSize=Math.Max(40,Math.Min(320,Store.State.PetSize));
   } catch { State=new Data(); Warning="저장 데이터를 읽지 못해 새 데이터를 사용합니다. 원본은 별도 파일로 보관합니다.";
    File.Copy(FilePath,FilePath+".damaged-"+AppClock.Now.ToString("yyyyMMddHHmmss"),true); }
  }
  public static bool Save() {
   try { Scheduler.PrepareSave(State); Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); string temp=FilePath+".tmp";
    File.WriteAllText(temp,new JavaScriptSerializer().Serialize(State),Encoding.UTF8);
    if(File.Exists(FilePath))File.Replace(temp,FilePath,FilePath+".bak");else File.Move(temp,FilePath);return true;
   } catch(Exception e){GameAlert.Show("저장하지 못했습니다.\n"+e.Message,"DeskBuddy",MessageBoxButtons.OK,MessageBoxIcon.Warning);return false;}
  }
 }
 public static class Rules {

  public static int GameReward(Data d,int score,DateTime now){return AwardGame(d,score,now,20);}
  public static int DodgeReward(Data d,bool won,DateTime now){return AwardGame(d,won?50:5,now,50);}
  public static int CastleReward(Data d,bool won,DateTime now,bool advanced=false){return AwardGame(d,won?(advanced?100:50):5,now,advanced?100:50);}
  static int AwardGame(Data d,int score,DateTime now,int maximum){
   string day=now.ToString("yyyy-MM-dd");if(d.GamesDate!=day){d.GamesDate=day;d.GamesPlayed=0;}
   if(d.GamesPlayed>=3)return -1;d.GamesPlayed++;int reward=Math.Max(0,Math.Min(maximum,score));d.Coins+=reward;return reward;
  }

  public static bool Complete(TaskItem t,Data d){return Scheduler.Complete(t,d,AppClock.Now);}
  public static bool Buy(string item,int price,Data d){if(d.Owned.Contains(item)){d.Equipped=item;return true;}if(d.Coins<price)return false;d.Coins-=price;d.Owned.Add(item);d.Equipped=item;return true;}
  public static bool FinishFocus(Data d,DateTime now){if(d.FocusEnd==DateTime.MinValue || AppClock.Wall(now)<AppClock.Wall(d.FocusEnd))return false;d.FocusCount++;d.FocusMinutes+=d.SessionMinutes;d.Coins+=d.SessionMinutes;d.FocusEnd=DateTime.MinValue;d.SessionMinutes=0;return true;}
 }
 public static class PetArt {
  static Rectangle customBounds;static Size customSize;
  static Rectangle Bounds(Bitmap image){int left=image.Width,top=image.Height,right=-1,bottom=-1;using(var copy=new Bitmap(image.Width,image.Height,PixelFormat.Format32bppArgb)){using(var g=Graphics.FromImage(copy))g.DrawImageUnscaled(image,0,0);var bits=copy.LockBits(new Rectangle(Point.Empty,copy.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{byte[] pixels=new byte[bits.Stride*bits.Height];System.Runtime.InteropServices.Marshal.Copy(bits.Scan0,pixels,0,pixels.Length);for(int y=0;y<copy.Height;y++)for(int x=0;x<copy.Width;x++)if(pixels[y*bits.Stride+x*4+3]>24){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}}finally{copy.UnlockBits(bits);}}return right<left?new Rectangle(Point.Empty,image.Size):Rectangle.FromLTRB(left,top,right+1,bottom+1);}
  public static Bitmap Custom,ScheduleCustom; static string schedulePath=""; public static bool HasCustom{get{return ScheduleCustom!=null||Custom!=null;}} public static bool SetScheduleImage(string path){path=path??"";if(path==schedulePath)return false;schedulePath=path;if(ScheduleCustom!=null){ScheduleCustom.Dispose();ScheduleCustom=null;}try{if(path.Length>0)using(var im=Image.FromFile(path))ScheduleCustom=new Bitmap(im);}catch(Exception ex){Startup.Log("Schedule image: "+ex.Message);}return true;}
  public static bool LoadCustom(string path){
   try { if(String.IsNullOrEmpty(path))path=CharacterLibrary.Default;if(String.IsNullOrEmpty(path)){if(Custom!=null)Custom.Dispose();Custom=null;return true;}
    var b=CharacterLibrary.Read(path);var bounds=Bounds(b);if(Custom!=null)Custom.Dispose();Custom=b;customBounds=bounds;customSize=b.Size;return true;
   }catch{return false;}
  }

  public static void DrawCustom(Graphics output,Rectangle box,bool facingLeft,string accessory){
   var picture=ScheduleCustom??Custom;if(picture==null)return;
   var state=output.Save();
   try{
    output.CompositingMode=CompositingMode.SourceOver;
    output.CompositingQuality=CompositingQuality.HighQuality;
    output.InterpolationMode=InterpolationMode.HighQualityBicubic;
    output.PixelOffsetMode=PixelOffsetMode.HighQuality;
    float scale=Math.Min((float)box.Width/picture.Width,(float)box.Height/picture.Height);
    int w=Math.Max(1,(int)Math.Round(picture.Width*scale)),h=Math.Max(1,(int)Math.Round(picture.Height*scale));
    var target=new Rectangle(box.X+(box.Width-w)/2,box.Y+(box.Height-h)/2,w,h);

    using(var attributes=new ImageAttributes()){
     attributes.SetWrapMode(WrapMode.TileFlipXY);
     output.DrawImage(picture,target,0,0,picture.Width,picture.Height,GraphicsUnit.Pixel,attributes);
    }
    if(accessory!="기본"){
     using(var overlay=new Bitmap(64,64,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(overlay)){
      DrawAccessory(g,accessory);
      output.InterpolationMode=InterpolationMode.NearestNeighbor;output.PixelOffsetMode=PixelOffsetMode.Half;
      var accessoryBox=box;if(picture==Custom&&picture.Size==customSize){accessoryBox=new Rectangle(target.X+(int)(customBounds.X*scale),target.Y+(int)(customBounds.Y*scale),Math.Max(1,(int)(customBounds.Width*scale)),Math.Max(1,(int)(customBounds.Height*scale)));}output.DrawImage(overlay,accessoryBox);
     }
    }
   }finally{output.Restore(state);}
  }

  static void DrawAccessory(Graphics g,string accessory){
    if(accessory=="축제 리본"){Rect(g,Theme.Line,20,4,27,10);Rect(g,Theme.Peach,22,6,9,6);Rect(g,Theme.Purple,36,6,9,6);Rect(g,Theme.Gold,31,7,5,6);Rect(g,Theme.Gold,27,14,4,5);Rect(g,Theme.Peach,36,14,4,5);return;}
    if(accessory=="전설 왕관"){Rect(g,Theme.Line,18,4,30,10);Rect(g,Theme.Gold,20,5,26,7);Rect(g,Theme.Gold,20,0,5,6);Rect(g,Theme.Gold,31,0,5,6);Rect(g,Theme.Gold,42,0,5,6);Rect(g,Color.FromArgb(147,116,205),22,9,22,4);Rect(g,Theme.Peach,31,6,5,5);Rect(g,Theme.Cream,32,6,2,2);return;}
    Color ink=Color.FromArgb(80,57,47),cream=Color.FromArgb(255,248,228),pink=Color.FromArgb(232,147,166),mint=Color.FromArgb(91,166,142);
    if(accessory=="별빛 모자"){using(var brush=new SolidBrush(Color.FromArgb(131,119,184)))g.FillPolygon(brush,new[]{new Point(22,11),new Point(33,0),new Point(43,11)});Rect(g,ink,18,11,29,3);Rect(g,Color.FromArgb(166,156,206),20,10,25,3);Rect(g,cream,32,5,3,3);Rect(g,cream,31,6,5,1);return;}
    if(accessory=="왕관"){Rect(g,ink,21,6,26,7);Rect(g,Color.FromArgb(237,190,91),23,7,22,4);Rect(g,Color.FromArgb(237,190,91),22,1,4,8);Rect(g,Color.FromArgb(237,190,91),31,0,6,8);Rect(g,Color.FromArgb(237,190,91),42,1,4,8);Rect(g,pink,32,8,3,2);Rect(g,cream,25,9,2,1);Rect(g,cream,40,9,2,1);return;}
    if(accessory=="민트 리본"||accessory=="딸기 리본"){Color c=accessory=="민트 리본"?mint:pink;Rect(g,ink,20,30,11,12);Rect(g,ink,35,30,11,12);Rect(g,c,22,32,7,8);Rect(g,c,37,32,7,8);Rect(g,cream,23,33,3,2);Rect(g,cream,38,33,3,2);Rect(g,ink,29,33,8,7);Rect(g,c,31,35,4,3);Rect(g,c,26,42,4,5);Rect(g,c,37,42,4,5);}
    if(accessory=="둥근 안경"){using(var pen=new Pen(ink,2)){g.DrawEllipse(pen,18,17,12,12);g.DrawEllipse(pen,34,17,12,12);g.DrawLine(pen,30,21,34,21);g.DrawLine(pen,14,20,18,21);g.DrawLine(pen,46,21,50,19);}Rect(g,cream,22,19,3,2);Rect(g,cream,38,19,3,2);}
    if(accessory=="꽃 화관"){Rect(g,mint,17,8,32,3);for(int x=18;x<48;x+=9){Rect(g,pink,x,4,7,8);Rect(g,pink,x-1,6,9,4);Rect(g,cream,x+2,7,3,2);}}
    if(accessory=="포근한 목도리"){Rect(g,ink,21,31,27,8);Rect(g,pink,23,32,23,5);Rect(g,cream,25,32,3,5);Rect(g,cream,35,32,3,5);Rect(g,pink,37,37,7,15);Rect(g,cream,37,41,7,3);Rect(g,cream,37,47,7,2);Rect(g,ink,37,52,2,3);Rect(g,ink,42,52,2,3);}
    if(accessory=="토끼 머리띠"){Rect(g,ink,23,0,8,17);Rect(g,ink,36,0,8,17);Rect(g,cream,25,1,4,14);Rect(g,cream,38,1,4,14);Rect(g,pink,26,3,2,9);Rect(g,pink,39,3,2,9);Rect(g,pink,22,15,24,3);}
    if(accessory=="별빛 날개"){Color lilac=Color.FromArgb(168,158,211);Rect(g,ink,2,26,13,23);Rect(g,ink,51,26,12,23);Rect(g,lilac,4,28,9,18);Rect(g,lilac,53,28,8,18);Rect(g,cream,5,31,4,2);Rect(g,cream,55,31,4,2);Rect(g,cream,8,38,4,2);Rect(g,cream,53,38,4,2);}
    if(accessory=="헤드폰"){Rect(g,ink,17,5,33,4);Rect(g,ink,14,9,5,18);Rect(g,ink,48,9,5,18);Rect(g,mint,13,18,8,12);Rect(g,mint,46,18,8,12);Rect(g,cream,15,20,2,7);Rect(g,cream,49,20,2,7);}
    if(accessory=="하트 친구"){Rect(g,Color.HotPink,49,5,5,5);Rect(g,Color.HotPink,57,5,5,5);Rect(g,Color.HotPink,50,10,11,4);Rect(g,Color.HotPink,53,14,5,4);}
  }
  static void Rect(Graphics g,Color c,int x,int y,int w,int h){using(var b=new SolidBrush(c))g.FillRectangle(b,x,y,w,h);}
  public static void Draw(Graphics output,Rectangle box,int frame,bool facingLeft,string accessory){
   if(HasCustom){DrawCustom(output,box,facingLeft,accessory);return;}
   using(var bmp=new Bitmap(64,64))using(var g=Graphics.FromImage(bmp)){
    {
     Color black=Color.FromArgb(36,36,43),white=Color.FromArgb(255,253,246);int bob=frame%2;
     Rect(g,black,25,29+bob,21,25);Rect(g,white,27,30+bob,17,22);
     Rect(g,black,16,11+bob,30,23);Rect(g,black,13,16+bob,38,14);
     Rect(g,white,18,13+bob,26,20);Rect(g,white,15,18+bob,35,10);
     Rect(g,black,47,18+bob,8,7);Rect(g,black,24,12+bob,9,24);Rect(g,black,25,32+bob,6,7);Rect(g,black,39,16+bob,3,4);
     Rect(g,Color.FromArgb(219,66,81),27,32+bob,17,3);
     Rect(g,black,21,42+bob,8,4);Rect(g,white,21,42+bob,6,2);Rect(g,black,42,45+bob,10,3);Rect(g,white,44,44+bob,8,2);
     Rect(g,black,25-(frame%2)*2,51,11,6);Rect(g,white,27-(frame%2)*2,52,7,3);
     Rect(g,black,38+(frame%2)*2,51,12,6);Rect(g,white,40+(frame%2)*2,52,8,3);
    }
    DrawAccessory(g,accessory);
    if(facingLeft)bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
    var mode=output.InterpolationMode;output.InterpolationMode=InterpolationMode.NearestNeighbor;output.PixelOffsetMode=PixelOffsetMode.Half;output.DrawImage(bmp,box);output.InterpolationMode=mode;
   }
  }
 }
 public class PetForm:Form {
  Timer animation=new Timer{Interval=50},clock=new Timer{Interval=1000};NotifyIcon tray;MainForm dashboard;Form game;Random random=new Random();int cartwheelFrame=-1;
  Queue<ScheduleNotice> pendingNotices=new Queue<ScheduleNotice>(); DateTime nextNotice=DateTime.MinValue,noticePoseUntil=DateTime.MinValue;string noticePose="",ignoredAppearanceId=""; int frame,direction=1,ticks;Point drag;bool dragging,shuttingDown;DateTime bubbleUntil;string bubble="오늘도 함께 한 걸음!";Rectangle area;
  public PetForm(){
   if(PetArt.Custom==null)PetArt.LoadCustom(Store.State.ImagePath);
   Text="DeskBuddy · 바탕화면 펫";Icon=AppIdentity.Icon;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
   BackColor=Color.Black;TransparencyKey=Color.Empty;Size=PetWindowSize();StartPosition=FormStartPosition.Manual;DoubleBuffered=true;AutoScaleMode=AutoScaleMode.None;
   area=Screen.PrimaryScreen.WorkingArea;Location=new Point(area.Right-Width-70,area.Bottom-Height-12);bubbleUntil=AppClock.Now.AddSeconds(12);
   var menu=new ContextMenuStrip();
   menu.Items.Add("업무 대시보드 열기",null,(s,e)=>OpenDashboard());
   menu.Items.Add("바탕화면 배구",null,(s,e)=>StartVolleyball());
   menu.Items.Add("마법의 성 모험",null,(s,e)=>StartCastle());menu.Items.Add("하늘에서 똥 피하기",null,(s,e)=>StartDodge());
   menu.Items.Add("옆돌기!",null,(s,e)=>Cartwheel());
   menu.Items.Add("밥 주기 · 12 G",null,(s,e)=>Feed("든든한 밥"));
   menu.Items.Add("간식 주기 · 7 G",null,(s,e)=>Feed("딸기 간식"));
   menu.Items.Add("산책 중 옆돌기 켜기 / 끄기",null,(s,e)=>{Store.State.Cartwheels=!Store.State.Cartwheels;Store.Save();});
   menu.Items.Add("응원 한마디",null,(s,e)=>Say("작은 완료 하나가 큰 성과가 돼!",false));
   menu.Items.Add("산책 켜기 / 끄기",null,(s,e)=>{Store.State.Wander=!Store.State.Wander;Store.Save();});
   menu.Items.Add("펫 숨기기 / 보이기",null,(s,e)=>Visible=!Visible);
   menu.Items.Add("종료",null,(s,e)=>Quit());ContextMenuStrip=menu;
   tray=new NotifyIcon{Icon=AppIdentity.Icon,Text="DeskBuddy · 업무 친구",Visible=true,ContextMenuStrip=menu};
   RefreshIdentity();
   tray.DoubleClick+=(s,e)=>{Show();OpenDashboard();};
   MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){drag=e.Location;dragging=true;}};
   MouseMove+=(s,e)=>{if(dragging)Location=new Point(Left+e.X-drag.X,Top+e.Y-drag.Y);};
   MouseUp+=(s,e)=>{dragging=false;KeepInside();};MouseDoubleClick+=(s,e)=>OpenDashboard();
   animation.Tick+=(s,e)=>Animate();
   clock.Tick+=(s,e)=>TickClock();animation.Start();clock.Start();
   Shown+=(s,e)=>{RenderLayered();OpenDashboard();if(Store.Warning.Length>0)GameAlert.Show(Store.Warning,"DeskBuddy");};
   FormClosing+=(s,e)=>{if(!shuttingDown){e.Cancel=true;Hide();}};
  }

  int actionFrames,observedLevel=Companions.Level(Store.State);string actionName="";DateTime celebrationUntil;
  public void SpecialAction(string name){int required=name=="함께 놀기"?5:3;if(Companions.Level(Store.State)<required){GameAlert.Show("레벨 "+required+"부터 사용할 수 있어요.");return;}if(game!=null||Store.State.FocusEnd!=DateTime.MinValue)return;if(required==5){var p=Companions.Current(Store.State);if(p==null){GameAlert.Show("먼저 알을 입양해주세요.");return;}if(AppClock.Now<AppClock.Decode(p.LastPlay,DateTime.MinValue).AddMinutes(1)){GameAlert.Show("함께 놀기는 1분마다 가능해요.");return;}p.Bond++;p.LastPlay=AppClock.Encode(AppClock.Now);Store.Save();}actionName=name;actionFrames=60;cartwheelFrame=-1;Say(name+"~ 같이 신나게 놀자!"+RewardInteraction(),false);}
  void CheckLevelUp(){int level=Companions.Level(Store.State);if(level==observedLevel)return;if(Application.OpenForms.Cast<Form>().Any(f=>f is AdminForm&&f.Visible))return;int previous=observedLevel;observedLevel=level;ApplyPetSize();if(level>previous){celebrationUntil=AppClock.Now.AddSeconds(12);string unlocked=Progression.Unlocks(previous,level);Say("LEVEL UP! 레벨 "+level+"!",false);var alert=new GameAlertForm("레벨 "+level+" 달성! 축하해요!"+(unlocked.Length==0?"":"\n"+unlocked),"LEVEL UP! 새 모험",MessageBoxButtons.OK,MessageBoxIcon.None);alert.Show(dashboard!=null&&!dashboard.IsDisposed? (IWin32Window)dashboard:this);if(dashboard!=null&&!dashboard.IsDisposed)dashboard.RefreshPage();}}
  void Animate(){
   if(actionFrames>0)actionFrames--;frame++;bool available=!dragging&&game==null&&Store.State.FocusEnd==DateTime.MinValue;
   if(!available){cartwheelFrame=-1;actionFrames=0;}if(actionFrames>0&&Companions.Level(Store.State)<(actionName=="함께 놀기"?5:3))actionFrames=0;
   if(available&&Store.State.Wander&&Store.State.Cartwheels&&actionFrames==0&&cartwheelFrame<0&&random.Next(450)==0)cartwheelFrame=0;
   if(available&&(cartwheelFrame>=0||Store.State.Wander&&++ticks%9==0)){
    Left+=direction*(cartwheelFrame>=0?5:4);area=Screen.FromControl(this).WorkingArea;
    if(Left<area.Left||Right>area.Right){direction=-direction;cartwheelFrame=-1;KeepInside();}
   }
   if(cartwheelFrame>=0&&++cartwheelFrame>=24)cartwheelFrame=-1;
   RenderLayered();
  }
  string RewardInteraction(){int before=Store.State.InteractionExperience;string previous=Store.State.LastInteractionLocal;if(!Companions.RewardInteraction(Store.State,AppClock.Now))return " (경험치는 30초마다!)";if(!Store.Save()){Store.State.InteractionExperience=before;Store.State.LastInteractionLocal=previous;return " (경험치를 저장하지 못했어)";}if(dashboard!=null&&!dashboard.IsDisposed)dashboard.RefreshPage();CheckLevelUp();return " +5 EXP";}
  public void Pat(){Say("쓰다듬어줘서 고마워!"+RewardInteraction(),false);}
  public void Cartwheel(){if(game!=null||Store.State.FocusEnd!=DateTime.MinValue)return;cartwheelFrame=0;Say("옆돌기~ 같이 한 바퀴!"+RewardInteraction(),false);}
  public void Feed(string name){string snapshot=new JavaScriptSerializer().Serialize(Store.State);string error;if(!Living.Feed(Store.State,name,AppClock.Now,out error)){GameAlert.Show(error,"밥 / 간식");return;}if(!Store.Save()){Store.State=new JavaScriptSerializer().Deserialize<Data>(snapshot);return;}Say("냠냠! "+name+" 고마워~",false);if(dashboard!=null&&!dashboard.IsDisposed)dashboard.RefreshPage();}
  public void CharacterChanged(){ApplyPetSize();noticePose="";noticePoseUntil=DateTime.MinValue;var active=Scheduler.Appearance(Store.State,AppClock.Now);ignoredAppearanceId=active==null?"":active.Id;PetArt.SetScheduleImage("");RefreshIdentity();}
  public void RefreshIdentity(){Text="DeskBuddy · "+Store.State.PetName;tray.Text=("DeskBuddy · "+Store.State.PetName);RenderLayered();}
  public void StartDodge(){if(Companions.Level(Store.State)<2){GameAlert.Show("레벨 2부터 똥 피하기를 할 수 있어요.");return;}StartGame(()=>new DodgeForm(this));}
  public void StartCastleAdvanced(){if(Companions.Level(Store.State)<3){GameAlert.Show("레벨 3부터 마법의 성 4·5탄을 할 수 있어요.");return;}StartGame(()=>new CastleForm(this,true));}
  public void StartCastle(){StartGame(()=>new CastleForm(this));}
  void StartGame(Func<Form> create){
   if(Store.State.FocusEnd!=DateTime.MinValue){if(Rules.FinishFocus(Store.State,AppClock.Now)){Store.Save();ApplyPetSize();}else{GameAlert.Show("집중 모험을 끝내고 한 판 해요!");return;}}
   if(game!=null&&!game.IsDisposed){game.WindowState=FormWindowState.Normal;game.Show();game.BringToFront();game.Activate();return;}
   try{
    game=create();game.FormClosed+=(s,e)=>{game=null;if(!shuttingDown){Show();RenderLayered();OpenDashboard();}};
    if(dashboard!=null&&!dashboard.IsDisposed)dashboard.Hide();Hide();cartwheelFrame=-1;
    game.WindowState=FormWindowState.Normal;game.TopMost=true;game.Show();game.BringToFront();game.Activate();game.TopMost=false;game.Focus();
    var castle=game as CastleForm;Startup.Log("Game opened: "+game.GetType().Name+(castle==null?"":" stage="+castle.Engine.Stage)+" visible="+game.Visible);
   }catch(Exception ex){if(game!=null){game.Dispose();game=null;}Show();RenderLayered();OpenDashboard();Startup.Log("Game launch failed: "+ex);GameAlert.Show("게임을 열지 못했어요.\n"+ex.Message,"게임 실행",MessageBoxButtons.OK,MessageBoxIcon.Error);}
  }
  int BubbleSpace{get{return 12+(int)(70*Store.State.BubbleScale/100.0);}}
  int FocusTimerHeight{get{return (int)Math.Round(40*Store.State.FocusTimerScale/100.0)+8;}}
  int FocusTimerSpace{get{return Store.State.FocusEnd==DateTime.MinValue?0:FocusTimerHeight;}}
  public Size PreviewSize{get{return PetWindowSize(true);}}
  public int MemoAvailable{get{return MemoBudget(false);}}
  int MemoBudget(bool preview){return Screen.FromControl(this).WorkingArea.Height-BubbleSpace-(preview?FocusTimerHeight:FocusTimerSpace)-Store.State.PetSize-8;}
  Size PetWindowSize(bool preview=false){int little=Companions.Current(Store.State)==null?0:Companions.SizeFor(Store.State.PetSize);return new Size(Math.Max(StickyMemos.Pinned(Store.State).Count>0?266:0,Math.Max(14+(int)(266*Store.State.BubbleScale/100.0),Math.Max(Store.State.PetSize+little+40,(int)Math.Round(140*Store.State.FocusTimerScale/100.0)+14))),BubbleSpace+StickyMemos.Space(Store.State,MemoBudget(preview))+(preview?FocusTimerHeight:FocusTimerSpace)+Store.State.PetSize+8);}
  public void ApplyPetSize(){
   int size=Math.Max(40,Math.Min(320,Store.State.PetSize));Store.State.PetSize=size;
   int bottom=Bottom,center=Left+Width/2;Size=PetWindowSize();
   Left=center-Width/2;Top=bottom-Height;KeepInside();RenderLayered();
  }
  public void StartVolleyball(){
   StartGame(()=>new VolleyballForm(this));
  }

  void KeepInside(){area=Screen.FromControl(this).WorkingArea;Left=Math.Max(area.Left,Math.Min(Left,area.Right-Width));Top=Math.Max(area.Top,Math.Min(Top,area.Bottom-Height));}
  public void OpenDashboard(){if(dashboard==null || dashboard.IsDisposed)dashboard=new MainForm(this);dashboard.Show();dashboard.WindowState=FormWindowState.Normal;dashboard.BringToFront();dashboard.Activate();Startup.Log("Dashboard visible="+dashboard.Visible+" handle="+dashboard.Handle);}
  public void Say(string message,bool notify){
   bubble=message;bubbleUntil=AppClock.Now.AddSeconds(18);RenderLayered();
   if(notify && !Store.State.Quiet){tray.BalloonTipTitle=Store.State.PetName+"의 업무 알림";tray.BalloonTipText=message;tray.ShowBalloonTip(5000);}
  }
  void TickClock(){ GoogleCalendar.Poll();
   if(Startup.OpenRequest!=null && Startup.OpenRequest.WaitOne(0)){Show();OpenDashboard();}
   DateTime now=AppClock.Now;bool changed=false;bool careChanged=Living.UpdateCare(Store.State,now);
   var notices=Scheduler.Poll(Store.State,now,out changed);foreach(var notice in notices)pendingNotices.Enqueue(notice);if(pendingNotices.Count>0&&now>=nextNotice){var notice=pendingNotices.Dequeue();if(Store.State.Tasks.Any(t=>t.Id==notice.Task.Id&&!t.Done)){Say(notice.Text,true);nextNotice=now.AddSeconds(19);noticePose=notice.Task.Id==ignoredAppearanceId?"":notice.Task.ScheduleImage;noticePoseUntil=now.AddSeconds(18);}}var appearance=Scheduler.Appearance(Store.State,now);if(appearance==null)ignoredAppearanceId="";else if(appearance.Id==ignoredAppearanceId)appearance=null;if(PetArt.SetScheduleImage(now<noticePoseUntil&&!String.IsNullOrEmpty(noticePose)?noticePose:appearance==null?"":appearance.ScheduleImage)){RenderLayered();if(dashboard!=null&&!dashboard.IsDisposed)dashboard.Invalidate(true);}
   if(Rules.FinishFocus(Store.State,now)){ApplyPetSize();Say("집중 완료! 코인을 받았어요.\n잠깐 스트레칭할까요?",true);changed=true;}
   CheckLevelUp();if(changed||careChanged){Store.Save();if(dashboard!=null && !dashboard.IsDisposed)dashboard.RefreshPage();}
   if(game==null && !Store.State.Quiet && now.Second==0 && now.Minute%30==0 && now.Hour>=9 && now.Hour<19 && Store.State.FocusEnd==DateTime.MinValue)
    Say(random.Next(2)==0?"물 한 잔 마시고 올까요?":"어깨를 펴고 잠깐 쉬어요!",false);
  }
  protected override CreateParams CreateParams{get{var cp=base.CreateParams;cp.ExStyle|=0x80000;return cp;}}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnPaint(PaintEventArgs e){DrawFrame(e.Graphics);}
  public Bitmap RenderSnapshot(bool preview=false){var dimensions=PetWindowSize(preview);var image=new Bitmap(dimensions.Width,dimensions.Height,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(image)){g.Clear(Color.Transparent);DrawFrame(g,preview);}return image;}
  void RenderLayered(){if(!IsHandleCreated||IsDisposed)return;using(var image=RenderSnapshot())AlphaWindow.Update(Handle,Location,image);}
  public void RenderLayeredForTest(){using(var image=RenderSnapshot())AlphaWindow.Update(Handle,Location,image);}
  void DrawFrame(Graphics graphics,bool preview=false){
   if(preview||AppClock.Now<bubbleUntil){
    int bw=(int)(266*Store.State.BubbleScale/100.0),bh=BubbleSpace-12;var r=new Rectangle((Width-bw)/2,5,bw,bh);using(var b=new SolidBrush(Color.FromArgb(255,253,246)))graphics.FillRectangle(b,r);
    Theme.Frame(graphics,r,Theme.Cream);
    float scale=Store.State.BubbleScale/100f;int padX=(int)Math.Round(10*scale),padY=(int)Math.Round(8*scale);var textBox=new Rectangle(r.X+padX,r.Y+padY,r.Width-2*padX,r.Height-2*padY);string caption=preview?"일정 알림! 잠깐 쉬고 함께 시작해요.":bubble;using(var f=Theme.BubbleFont(graphics,caption,textBox,Store.State.BubbleScale))Theme.Text(graphics,caption,f,Theme.Ink,textBox,ContentAlignment.MiddleCenter);
    using(var b=new SolidBrush(Color.FromArgb(255,253,246)))graphics.FillPolygon(b,new[]{new Point(Width/2-8,BubbleSpace-7),new Point(Width/2+8,BubbleSpace-7),new Point(Width/2,BubbleSpace+5)});
   }
   int memoSpace=StickyMemos.Space(Store.State,MemoBudget(preview));StickyMemos.Draw(graphics,Width,BubbleSpace,Store.State,MemoBudget(preview));
   int petTop=BubbleSpace+memoSpace+(preview?FocusTimerHeight:FocusTimerSpace);
   if(preview||Store.State.FocusEnd!=DateTime.MinValue)FocusTimerArt.Draw(graphics,new Rectangle((Width-(int)Math.Round(140*Store.State.FocusTimerScale/100.0))/2,BubbleSpace+memoSpace,(int)Math.Round(140*Store.State.FocusTimerScale/100.0),FocusTimerHeight-8),Store.State,AppClock.Now,preview);
   var transform=graphics.Save();int size=Store.State.PetSize;int little=Companions.Current(Store.State)==null?0:Companions.SizeFor(size);int mainX=(Width-size-little)/2;
   if(actionFrames>0){graphics.TranslateTransform(mainX+size/2f,petTop+size/2f);if(actionName=="공중제비"){double angle=(60-actionFrames)*Math.PI*2/60;graphics.RotateTransform((float)(angle*180/Math.PI));float fit=(float)(1/(Math.Abs(Math.Cos(angle))+Math.Abs(Math.Sin(angle))));graphics.ScaleTransform(fit,fit);}else{graphics.RotateTransform((float)Math.Sin(actionFrames*.35)*12);graphics.TranslateTransform(0,-Math.Abs((float)Math.Sin(actionFrames*.25))*8);}graphics.TranslateTransform(-mainX-size/2f,-petTop-size/2f);}
   if(cartwheelFrame>=0){graphics.TranslateTransform(mainX+size/2f,petTop+size/2f);graphics.RotateTransform(direction*cartwheelFrame*360f/24);double angle=cartwheelFrame*Math.PI*2/24;float fit=(float)(1/(Math.Abs(Math.Cos(angle))+Math.Abs(Math.Sin(angle))));graphics.ScaleTransform(fit,fit);graphics.TranslateTransform(-mainX-size/2f,-petTop-size/2f);}
   PetArt.Draw(graphics,new Rectangle(mainX,petTop+(frame/3%2),size,size),frame/3,direction<0,Progression.Accessory(Store.State));graphics.Restore(transform);Companions.Draw(graphics,new Rectangle(mainX+size+4,petTop+size-little-(actionFrames>0&&actionName=="함께 놀기"?(int)(Math.Abs(Math.Sin(actionFrames*.25))*8):0),little,little),Store.State,frame);Progression.Sparkles(graphics,new Rectangle(0,petTop,Width,size),frame,AppClock.Now<celebrationUntil);
   DateTime fed=AppClock.Decode(Store.State.LastFedLocal,DateTime.MinValue);if(fed!=DateTime.MinValue&&AppClock.Now>=fed&&AppClock.Now<fed.AddSeconds(5))LivingArt.Food(graphics,Store.State.LastFood,new Rectangle(Width/2+size/4,petTop+size/2,Math.Max(24,size/3),Math.Max(24,size/3)));
  }
  protected override void Dispose(bool disposing){if(disposing){animation.Dispose();clock.Dispose();if(tray!=null){tray.Visible=false;tray.Dispose();tray=null;}}base.Dispose(disposing);}
  public void Quit(){shuttingDown=true;if(game!=null&&!game.IsDisposed)game.Close();Store.Save();animation.Stop();clock.Stop();tray.Visible=false;tray.Dispose();Application.Exit();}
 }
 static class Program {

  [STAThread] static int Main(string[] args){
   try{
    LibraryLoader.Install();System.Threading.Thread.CurrentThread.CurrentCulture=new System.Globalization.CultureInfo("ko-KR");System.Threading.Thread.CurrentThread.CurrentUICulture=new System.Globalization.CultureInfo("ko-KR");Startup.Log("Starting "+Application.ExecutablePath+" args="+String.Join(" ",args));
    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
    Application.ThreadException+=(s,e)=>Startup.Error(e.Exception);
    AppDomain.CurrentDomain.UnhandledException+=(s,e)=>Startup.Log("Unhandled: "+e.ExceptionObject);
    return RunMain(args);
   }catch(Exception e){Startup.Error(e);return 1;}
  }
  static int RunMain(string[] args){

   if(args.Contains("--self-test"))return SelfTest();
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   if(args.Contains("--volley-test"))return VolleyballTests.Execute();
   if(args.Contains("--level-test"))return ProgressionUiTests.Execute();

   if(args.Contains("--memo-test"))return StickyMemoForm.TestUi();
   if(args.Contains("--schedule-range-test"))return ScheduleRangeTests.Execute();
   if(args.Contains("--focus-timer-test"))return FocusTimerArt.Preview();
   if(args.Contains("--month-calendar-test"))return ScheduleCalendarForm.TestUi();
   if(args.Contains("--calendar-test"))return GoogleCalendar.TestUi();
   if(args.Contains("--alert-test"))return GameAlert.Test();
   if(args.Contains("--dodge-test"))return DodgeForm.TestUi();
   if(args.Contains("--castle-test"))return CastleTests.Execute();
   if(args.Contains("--companion-test"))return CompanionUiTests.Execute();
   if(args.Contains("--living-test"))return LivingUiTests.Execute();
   if(args.Contains("--image-test")){int n=Array.IndexOf(args,"--image-test");if(n+1>=args.Length)return 2;return ImageTest(args[n+1]);}
   if(args.Contains("--ui-test"))return UiTest();
   if(args.Contains("--preview"))return PreviewShots();
   bool acquired;using(var mutex=new System.Threading.Mutex(true,"Local\\DeskBuddy.JY",out acquired)){

    if(!acquired){
     try{using(var signal=System.Threading.EventWaitHandle.OpenExisting("Local\\DeskBuddy.Open.JY"))signal.Set();Startup.Log("Requested existing window.");}
     catch(Exception e){Startup.Log("Activation failed: "+e);GameAlert.Show("DeskBuddy가 이미 실행 중입니다. 작업 표시줄의 숨겨진 아이콘에서 열어주세요.");}
     return 0;
    }
    Startup.OpenRequest=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"Local\\DeskBuddy.Open.JY");

    try{Store.Load();if(!PetArt.LoadCustom(Store.State.ImagePath)){Store.State.ImagePath="";GameAlert.Show("캐릭터 파일을 읽지 못해 기본 펫으로 시작합니다.");}

     Application.Run(new PetForm());
    }catch(Exception e){GameAlert.Show("실행 중 문제가 발생했습니다.\n"+e,"DeskBuddy");return 1;}finally{if(Startup.OpenRequest!=null){Startup.OpenRequest.Dispose();Startup.OpenRequest=null;}mutex.ReleaseMutex();}
   }return 0;
  }
  static int PreviewShots(){
   string target=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview");Directory.CreateDirectory(target);
   Store.State=new Data();
   Store.State.Tasks.Add(new TaskItem{Title="주간 회의 자료 정리",Due=AppClock.Now.AddMinutes(30),Category="회의"});
   Store.State.Tasks.Add(new TaskItem{Title="고객 요청 사항 검토",Due=AppClock.Now.AddHours(2)});
   using(var p=new PetForm())using(var f=new MainForm(p)){f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-2200,-1200);f.Show();Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(target,"dashboard.png"),ImageFormat.Png);}

    foreach(string name in new[]{"오늘의 업무","집중 스튜디오","미니게임","코인 상점","나의 캐릭터","설정 / 소개"}){
     f.Navigate(name);Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(target,name.Replace(" / ","-")+".png"),ImageFormat.Png);}
    }

    using(var b=new Bitmap(p.Width,p.Height)){p.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(target,"pet.png"),ImageFormat.Png);}
   }using(var editor=new ScheduleEditor(null,"휴가")){editor.StartPosition=FormStartPosition.Manual;editor.Location=new Point(-2200,-1200);editor.Show();Application.DoEvents();using(var b=new Bitmap(editor.Width,editor.Height)){editor.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(target,"schedule-editor.png"),ImageFormat.Png);}editor.Close();}return 0;  }

  static IEnumerable<Control> Descendants(Control root){foreach(Control c in root.Controls){yield return c;foreach(var child in Descendants(c))yield return child;}}
  static Button FindButton(Form form,string text){return Descendants(form).OfType<Button>().First(b=>b.Text==text);}
  static int UiTest(){
   string oldPath=Store.FilePath;Data oldState=Store.State;string temp=Path.Combine(Path.GetTempPath(),"DeskBuddy-test-"+Guid.NewGuid().ToString("N"));
   Directory.CreateDirectory(temp);Store.FilePath=Path.Combine(temp,"data.json");Store.State=new Data();
   try{
    using(var p=new PetForm())using(var f=new MainForm(p)){
     f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-2200,-1200);f.Show();Application.DoEvents();
     f.Navigate("오늘의 업무");Application.DoEvents();
     using(var editor=new ScheduleEditor(null,"")){editor.TitleInput.Text="검증용 업무";string error;if(!editor.TrySave(out error))throw new Exception(error);}f.RefreshPage();Application.DoEvents();
     using(var editor=new ScheduleEditor(null,"휴가")){if(!editor.AllDayToggle.Checked||!editor.EveToggle.Checked||editor.CategoryInput.Text!="휴가")throw new Exception("Vacation preset");editor.DateTimeInput.TimeInput.Text="25:00";DateTime invalid;if(editor.DateTimeInput.TryValue(out invalid))throw new Exception("Invalid editor time");}using(var editor=new ScheduleEditor(null,"출근")){if(editor.RepeatInput.Text!="평일"||editor.DateTimeInput.Value.Hour!=9||editor.DateTimeInput.DatePicker.ShowUpDown)throw new Exception("Commute calendar preset");}
     if(Store.State.Tasks.Count!=1)throw new Exception("UI add task");
     Descendants(f).OfType<ListBox>().First().SelectedIndex=0;
     FindButton(f,"완료 +20 코인").PerformClick();Application.DoEvents();
     if(Store.State.Coins!=50 || Store.State.Completed!=1)throw new Exception("UI completion");
     f.Navigate("코인 상점");FindButton(f,"구매 + 장착").PerformClick();Application.DoEvents();
     if(Store.State.Coins!=10 || Store.State.Equipped!="민트 리본")throw new Exception("UI purchase");
     f.Navigate("미니게임");Application.DoEvents();
     if(!FindButton(f,"바탕화면 배구 시작").Enabled)throw new Exception("Volleyball launch button");
     VolleyballTests.Run();ScheduleTests.Run();
     f.Navigate("나의 캐릭터");Application.DoEvents();
     Descendants(f).OfType<TextBox>().Single().Text="새 이름";FindButton(f,"이름 저장").PerformClick();Application.DoEvents();
     if(Store.State.PetName!="새 이름"||!Descendants(f).OfType<Label>().Any(l=>l.Text=="새 이름")||p.Text!="DeskBuddy · 새 이름")throw new Exception("Name refresh");
     string sample=Path.Combine(temp,"sample.png");using(var image=new Bitmap(24,48)){using(var g=Graphics.FromImage(image))g.Clear(Color.Aqua);image.Save(sample,ImageFormat.Png);}
     Store.State.CharacterSamples.Add(sample);f.RefreshPage();FindButton(f,"sample").PerformClick();Application.DoEvents();
     if(Store.State.ImagePath!=sample||PetArt.Custom==null||PetArt.Custom.Height!=48)throw new Exception("Sample selection");
     FindButton(f,"기본 캐릭터로 돌아가기").PerformClick();Application.DoEvents();
     if(Store.State.PetName!="스누피"||Store.State.ImagePath!="")throw new Exception("Default character name");
     var appearanceTask=new TaskItem{Id="selection-appearance",ScheduleImage=sample,LocalDue=AppClock.Encode(AppClock.Now),AppearanceMinutes=30};Store.State.Tasks.Add(appearanceTask);PetArt.SetScheduleImage(sample);
     foreach(string name in new[]{"가나디","메타몽","루피","햄뿡이","찌오"}){FindButton(f,name).PerformClick();Application.DoEvents();if(Store.State.PetName!=name||CharacterLibrary.Label(Store.State.ImagePath)!=name||p.Text!="DeskBuddy · "+name||!Descendants(f).OfType<Label>().Any(l=>l.Text==name))throw new Exception("Bundled character name: "+name);typeof(PetForm).GetMethod("TickClock",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p,null);if(PetArt.ScheduleCustom!=null)throw new Exception("Schedule image overrides selected character");using(var expected=CharacterLibrary.Read(Store.State.ImagePath)){if(PetArt.Custom.Size!=expected.Size)throw new Exception("Selected character image size");for(int y=0;y<expected.Height;y++)for(int x=0;x<expected.Width;x++)if(PetArt.Custom.GetPixel(x,y)!=expected.GetPixel(x,y))throw new Exception("Selected character pixels");}}
     Store.State.Tasks.Remove(appearanceTask);
     FindButton(f,"기본 캐릭터로 돌아가기").PerformClick();Application.DoEvents();
     p.Cartwheel();using(var start=p.RenderSnapshot()){
      var advance=typeof(PetForm).GetMethod("Animate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
      for(int i=0;i<6;i++)advance.Invoke(p,null);
      using(var rotated=p.RenderSnapshot()){bool changed=false;for(int y=82;y<start.Height;y++)for(int x=0;x<start.Width;x++)if(start.GetPixel(x,y)!=rotated.GetPixel(x,y))changed=true;if(!changed)throw new Exception("Cartwheel frame rendering");}
      for(int i=0;i<18;i++)advance.Invoke(p,null);
     }
     Descendants(f).OfType<PixelSizeSlider>().First().Value=84;
     if(Store.State.PetSize!=84||p.Height!=174)throw new Exception("Live pet sizing");
     f.Navigate("설정 / 소개");Application.DoEvents();
     var options=Descendants(f).OfType<PixelToggle>().ToArray();
     if(options.Length!=2)throw new Exception("Pixel options");
     options[0].Checked=false;options[1].Checked=true;
     if(Store.State.Wander||!Store.State.Quiet)throw new Exception("Pixel toggle state");
     f.Navigate("업무일지");Application.DoEvents();
     if(!Descendants(f).OfType<WorkDiaryPanel>().Any())throw new Exception("WorkDiaryPanel navigation");

     Store.State=new Data();Store.Load();
     if(Store.State.Coins!=10 || Store.State.Completed!=1 || Store.State.Tasks.Count!=1 || Store.State.Equipped!="민트 리본")throw new Exception("Disk persistence");
     if(Store.State.Wander||!Store.State.Quiet)throw new Exception("Pixel options persistence");if(Store.State.PetSize!=84)throw new Exception("Pet size persistence");
     if(Store.State.PetName!="스누피"||Store.State.CharacterSamples.Count!=1)throw new Exception("Name and library persistence");
    }
    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"validation.txt"),"PASS: name refresh and persistence, sample selection and persistence, all six bundled character names and selection refresh, task creation via UI, completion reward, accessory purchase, volleyball page and physics, live pet sizing, disk save/reload, pixel toggles and size persistence.\r\nWindows notification delivery and multi-monitor/high-DPI behavior require manual verification.");
    return 0;
   }catch(Exception e){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"validation.txt"),"FAIL: "+e);return 1;}
   finally{Store.FilePath=oldPath;Store.State=oldState;foreach(string file in Directory.GetFiles(temp))File.Delete(file);Directory.Delete(temp);}
  }


  static int ImageTest(string path){
   try{
    string target=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview");Directory.CreateDirectory(target);
    using(var synthetic=new Bitmap(80,20,PixelFormat.Format32bppArgb)){
     using(var g=Graphics.FromImage(synthetic)){g.Clear(Color.Transparent);using(var b=new SolidBrush(Color.FromArgb(128,0,200,0)))g.FillRectangle(b,8,0,64,20);using(var b=new SolidBrush(Color.Magenta))g.FillRectangle(b,26,0,28,20);}
     using(var g=Graphics.FromImage(synthetic))using(var marker=new SolidBrush(Color.Blue))g.FillRectangle(marker,10,2,6,4);
     PetArt.Custom=synthetic;
     using(var rendered=new Bitmap(160,160,PixelFormat.Format32bppPArgb))using(var g=Graphics.FromImage(rendered)){
      PetArt.Draw(g,new Rectangle(0,0,160,160),0,false,"기본");
      if(rendered.GetPixel(80,10).A!=0)throw new Exception("Aspect ratio or transparency");
      var center=rendered.GetPixel(80,80);if(center.A!=255||center.R<250||center.B<250)throw new Exception("Opaque magenta lost");
      var semi=rendered.GetPixel(32,80);if(semi.A<120||semi.A>136)throw new Exception("Semi-transparent edge lost");
     }
     using(var normal=new Bitmap(160,160))using(var turned=new Bitmap(160,160))using(var a=Graphics.FromImage(normal))using(var b=Graphics.FromImage(turned)){
      PetArt.Draw(a,new Rectangle(0,0,160,160),0,false,"기본");PetArt.Draw(b,new Rectangle(0,0,160,160),0,true,"기본");
      for(int y=0;y<160;y++)for(int x=0;x<160;x++)if(normal.GetPixel(x,y)!=turned.GetPixel(x,y))throw new Exception("Imported PNG was mirrored");
     }
     PetArt.Custom=null;
    }
    if(!PetArt.LoadCustom(path))throw new Exception("Source PNG load");
    using(var p=new PetForm())using(var snapshot=p.RenderSnapshot())snapshot.Save(Path.Combine(target,"zzio-fixed.png"),ImageFormat.Png);
    using(var p=new PetForm())using(var f=new MainForm(p)){
     f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-2200,-1200);f.Show();Application.DoEvents();
     using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(target,"zzio-room.png"),ImageFormat.Png);}
     p.StartPosition=FormStartPosition.Manual;p.Location=new Point(-2200,-1200);
     p.CreateControl();p.RenderLayeredForTest();
    }
    PetArt.LoadCustom("");
    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"image-validation.txt"),"PASS: original-resolution PNG rendering, aspect ratio, transparent margins, semi-transparent alpha, opaque magenta, sample PNG preview, UpdateLayeredWindow API.");
    return 0;
   }catch(Exception e){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"image-validation.txt"),"FAIL: "+e);return 1;}
   finally{PetArt.Custom=null;}
  }

  static int SelfTest(){
   // Tests call Store.Save() indirectly (e.g. diary rewards); keep them away from the user's real data.json.
   string realPath=Store.FilePath;string sandbox=Path.Combine(Path.GetTempPath(),"DeskBuddy-selftest-"+Guid.NewGuid().ToString("N"));
   Directory.CreateDirectory(sandbox);Store.FilePath=Path.Combine(sandbox,"data.json");
   try{
    StickyMemos.Tests();FocusTimerArt.Tests();CalendarDates.Tests();GoogleCalendar.Tests();CastleTests.Run();DodgeEngine.Tests();Progression.Tests();
    LivingTests.Run();Companions.Tests();AdminAccess.Tests();using(var image=new Bitmap(500,200))using(var g=Graphics.FromImage(image)){foreach(int scale in new[]{60,100,160}){var bounds=new Rectangle(0,0,(int)(246*scale/100.0),(int)(54*scale/100.0));using(var font=Theme.BubbleFont(g,"일정 알림",bounds,scale))if(Math.Abs(font.Size-16*scale/100f)>0.01f)throw new Exception("Bubble font proportional scale");using(var font=Theme.BubbleFont(g,"긴 일정 알림입니다. 회의 준비와 자료 확인을 마치고 참석해주세요.",bounds,scale))if(g.MeasureString("긴 일정 알림입니다. 회의 준비와 자료 확인을 마치고 참석해주세요.",font,bounds.Width).Height>bounds.Height)throw new Exception("Bubble text fit");}}
    VolleyballTests.Run();ScheduleTests.Run();DiaryTests.Run();
    var d=new Data();var t=new TaskItem();
    if(!Rules.Complete(t,d)||d.Coins!=50||d.Completed!=1)throw new Exception("Completion");
    if(Rules.Complete(t,d)||d.Coins!=50)throw new Exception("Duplicate");
    if(Rules.Buy("왕관",150,d)||d.Coins!=50)throw new Exception("Overspending");
    if(!Rules.Buy("민트 리본",40,d)||d.Coins!=10)throw new Exception("Purchase");
    if(!Rules.Buy("민트 리본",40,d)||d.Coins!=10)throw new Exception("Reequip");
    d.SessionMinutes=25;d.FocusEnd=AppClock.Now.AddSeconds(10);
    if(Rules.FinishFocus(d,AppClock.Now))throw new Exception("Early focus");
    if(!Rules.FinishFocus(d,AppClock.Now.AddMinutes(1))||d.Coins!=35||d.FocusMinutes!=25)throw new Exception("Focus");
    if(Rules.FinishFocus(d,AppClock.Now.AddMinutes(2)))throw new Exception("Duplicate focus");

    var gameData=new Data();
    if(Rules.GameReward(gameData,25,DateTime.Today)!=20||gameData.Coins!=50)throw new Exception("Game cap");
    Rules.GameReward(gameData,1,DateTime.Today);Rules.GameReward(gameData,1,DateTime.Today);
    if(Rules.GameReward(gameData,10,DateTime.Today)!=-1||gameData.Coins!=52)throw new Exception("Daily limit");
    if(Rules.GameReward(gameData,2,DateTime.Today.AddDays(1))!=2||gameData.GamesPlayed!=1)throw new Exception("Daily reset");

    d.Tasks.Add(t);var restored=new JavaScriptSerializer().Deserialize<Data>(new JavaScriptSerializer().Serialize(d));
    if(restored.Tasks.Count!=1||!restored.Tasks[0].Done||restored.Coins!=35)throw new Exception("JSON");
    using(var bmp=new Bitmap(180,180))using(var g=Graphics.FromImage(bmp))PetArt.Draw(g,new Rectangle(0,0,180,180),1,true,"왕관");return 0;
   }catch{return 1;}
   finally{Store.FilePath=realPath;try{Directory.Delete(sandbox,true);}catch{}}
  }
 }
}
