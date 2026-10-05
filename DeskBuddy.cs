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

 public static class Startup {
  public static System.Threading.EventWaitHandle OpenRequest;
  public static string LogPath=Path.Combine(Store.Root,"startup.log");
  public static void Log(string message){try{Directory.CreateDirectory(Store.Root);File.AppendAllText(LogPath,AppClock.Now.ToString("yyyy-MM-dd HH:mm:ss")+" "+message+Environment.NewLine,Encoding.UTF8);}catch{}}
  public static void Error(Exception ex){Log(ex.ToString());MessageBox.Show("DeskBuddy를 실행하지 못했습니다.\n"+ex.Message+"\n\n오류 기록: "+LogPath,"DeskBuddy 시작 오류",MessageBoxButtons.OK,MessageBoxIcon.Error);}
 }

 public class TaskItem {
  public string Id=Guid.NewGuid().ToString(); public string Title=""; public DateTime Due;
  public bool Done; public bool Notified; public string Category="업무"; public string LocalDue="", Repeat="한 번", AnchorTime="", ScheduleImage="", EveTime="18:00", SnoozeLocal="", LastCompletedLocal=""; public bool AllDay, EveReminder, EveNotified; public int AppearanceMinutes=30;
 }
 public class Data {
  public int Coins=30, Completed, FocusCount, FocusMinutes;
  public string PetName="스누피", ImagePath="", Equipped="기본"; public int PixelSize=3; public int PetSize=140;
  public List<string> Owned=new List<string>{"기본"}; public List<TaskItem> Tasks=new List<TaskItem>();
  public bool Wander=true, Quiet=false; public DateTime FocusEnd=DateTime.MinValue; public string FocusEndLocal=""; public int SessionMinutes; public string GamesDate=""; public int GamesPlayed;
 }
 public static class Store {
  public static readonly string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DeskBuddy");
  public static string FilePath=Path.Combine(Root,"data.json"); public static Data State=new Data(); public static string Warning="";
  public static void Load() {
   if(!File.Exists(FilePath))return;
   try { State=new JavaScriptSerializer().Deserialize<Data>(File.ReadAllText(FilePath,Encoding.UTF8));
    if(State==null || State.Tasks==null || State.Owned==null)throw new Exception("Invalid data");
    Scheduler.Normalize(State); State.PixelSize=Math.Max(1,Math.Min(5,State.PixelSize));Store.State.PetSize=Math.Max(40,Math.Min(320,Store.State.PetSize));
   } catch { State=new Data(); Warning="저장 데이터를 읽지 못해 새 데이터를 사용합니다. 원본은 별도 파일로 보관합니다.";
    File.Copy(FilePath,FilePath+".damaged-"+AppClock.Now.ToString("yyyyMMddHHmmss"),true); }
  }
  public static bool Save() {
   try { Scheduler.PrepareSave(State); Directory.CreateDirectory(Root); string temp=FilePath+".tmp";
    File.WriteAllText(temp,new JavaScriptSerializer().Serialize(State),Encoding.UTF8);
    if(File.Exists(FilePath))File.Replace(temp,FilePath,FilePath+".bak");else File.Move(temp,FilePath);return true;
   } catch(Exception e){MessageBox.Show("저장하지 못했습니다.\n"+e.Message,"DeskBuddy",MessageBoxButtons.OK,MessageBoxIcon.Warning);return false;}
  }
 }
 public static class Rules {

  public static int GameReward(Data d,int score,DateTime now){
   string day=now.ToString("yyyy-MM-dd");if(d.GamesDate!=day){d.GamesDate=day;d.GamesPlayed=0;}
   if(d.GamesPlayed>=3)return -1;d.GamesPlayed++;int reward=Math.Max(0,Math.Min(20,score));d.Coins+=reward;return reward;
  }

  public static bool Complete(TaskItem t,Data d){return Scheduler.Complete(t,d,AppClock.Now);}
  public static bool Buy(string item,int price,Data d){if(d.Owned.Contains(item)){d.Equipped=item;return true;}if(d.Coins<price)return false;d.Coins-=price;d.Owned.Add(item);d.Equipped=item;return true;}
  public static bool FinishFocus(Data d,DateTime now){if(d.FocusEnd==DateTime.MinValue || AppClock.Wall(now)<AppClock.Wall(d.FocusEnd))return false;d.FocusCount++;d.FocusMinutes+=d.SessionMinutes;d.Coins+=d.SessionMinutes;d.FocusEnd=DateTime.MinValue;d.SessionMinutes=0;return true;}
 }
 public static class PetArt {
  public static Bitmap Custom,ScheduleCustom; static string schedulePath=""; public static bool HasCustom{get{return ScheduleCustom!=null||Custom!=null;}} public static bool SetScheduleImage(string path){path=path??"";if(path==schedulePath)return false;schedulePath=path;if(ScheduleCustom!=null){ScheduleCustom.Dispose();ScheduleCustom=null;}try{if(path.Length>0)using(var im=Image.FromFile(path))ScheduleCustom=new Bitmap(im);}catch(Exception ex){Startup.Log("Schedule image: "+ex.Message);}return true;}
  public static bool LoadCustom(string path){
   try { if(String.IsNullOrEmpty(path)){if(Custom!=null)Custom.Dispose();Custom=null;return true;}
    using(var im=Image.FromFile(path)){var b=new Bitmap(im);if(Custom!=null)Custom.Dispose();Custom=b;}return true;
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
      output.DrawImage(overlay,box);
     }
    }
   }finally{output.Restore(state);}
  }

  static void DrawAccessory(Graphics g,string accessory){
    if(accessory=="별빛 모자"){Rect(g,Color.FromArgb(116,86,232),16,7,32,5);Rect(g,Color.FromArgb(116,86,232),23,0,18,8);Rect(g,Color.Gold,30,3,4,4);}
    if(accessory=="민트 리본"){Rect(g,Color.FromArgb(52,191,157),25,33,8,7);Rect(g,Color.FromArgb(52,191,157),36,33,8,7);Rect(g,Color.FromArgb(25,140,115),33,35,3,3);}
    if(accessory=="왕관"){Rect(g,Color.Goldenrod,22,2,24,9);Rect(g,Color.Gold,22,0,4,8);Rect(g,Color.Gold,32,0,4,8);Rect(g,Color.Gold,42,0,4,8);}
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
  Timer animation=new Timer{Interval=150},clock=new Timer{Interval=1000};NotifyIcon tray;MainForm dashboard;VolleyballForm game;Random random=new Random();
  Queue<ScheduleNotice> pendingNotices=new Queue<ScheduleNotice>(); DateTime nextNotice=DateTime.MinValue,noticePoseUntil=DateTime.MinValue;string noticePose=""; int frame,direction=1,ticks;Point drag;bool dragging,shuttingDown;DateTime bubbleUntil;string bubble="오늘도 함께 한 걸음!";Rectangle area;
  public PetForm(){
   Text="DeskBuddy · 바탕화면 펫";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
   BackColor=Color.Black;TransparencyKey=Color.Empty;Size=new Size(Math.Max(280,Store.State.PetSize+28),82+Store.State.PetSize+8);StartPosition=FormStartPosition.Manual;DoubleBuffered=true;AutoScaleMode=AutoScaleMode.None;
   area=Screen.PrimaryScreen.WorkingArea;Location=new Point(area.Right-Width-70,area.Bottom-Height-12);bubbleUntil=AppClock.Now.AddSeconds(12);
   var menu=new ContextMenuStrip();
   menu.Items.Add("업무 대시보드 열기",null,(s,e)=>OpenDashboard());
   menu.Items.Add("바탕화면 배구",null,(s,e)=>StartVolleyball());
   menu.Items.Add("응원 한마디",null,(s,e)=>Say("작은 완료 하나가 큰 성과가 돼!",false));
   menu.Items.Add("산책 켜기 / 끄기",null,(s,e)=>{Store.State.Wander=!Store.State.Wander;Store.Save();});
   menu.Items.Add("펫 숨기기 / 보이기",null,(s,e)=>Visible=!Visible);
   menu.Items.Add("종료",null,(s,e)=>Quit());ContextMenuStrip=menu;
   tray=new NotifyIcon{Icon=SystemIcons.Information,Text="DeskBuddy · 업무 친구",Visible=true,ContextMenuStrip=menu};
   tray.DoubleClick+=(s,e)=>{Show();OpenDashboard();};
   MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){drag=e.Location;dragging=true;}};
   MouseMove+=(s,e)=>{if(dragging)Location=new Point(Left+e.X-drag.X,Top+e.Y-drag.Y);};
   MouseUp+=(s,e)=>{dragging=false;KeepInside();};MouseDoubleClick+=(s,e)=>OpenDashboard();
   animation.Tick+=(s,e)=>{frame++;if(!dragging && game==null && Store.State.Wander && Store.State.FocusEnd==DateTime.MinValue && ++ticks%3==0){Left+=direction*4;area=Screen.FromControl(this).WorkingArea;if(Left<area.Left || Right>area.Right){direction=-direction;KeepInside();}}RenderLayered();};
   clock.Tick+=(s,e)=>TickClock();animation.Start();clock.Start();
   Shown+=(s,e)=>{RenderLayered();OpenDashboard();if(Store.Warning.Length>0)MessageBox.Show(Store.Warning,"DeskBuddy");};
   FormClosing+=(s,e)=>{if(!shuttingDown){e.Cancel=true;Hide();}};
  }

  public void ApplyPetSize(){
   int size=Math.Max(40,Math.Min(320,Store.State.PetSize));Store.State.PetSize=size;
   int bottom=Bottom,center=Left+Width/2;Size=new Size(Math.Max(280,size+28),82+size+8);
   Left=center-Width/2;Top=bottom-Height;KeepInside();RenderLayered();
  }
  public void StartVolleyball(){
   if(Store.State.FocusEnd!=DateTime.MinValue){MessageBox.Show("집중 모험을 끝내고 한 판 해요!");return;}
   if(game!=null&&!game.IsDisposed){game.Activate();return;}
   if(dashboard!=null&&!dashboard.IsDisposed)dashboard.Hide();Hide();
   game=new VolleyballForm(this);
   game.FormClosed+=(s,e)=>{game=null;if(!shuttingDown){Show();RenderLayered();OpenDashboard();}};
   game.Show();game.Activate();
  }

  void KeepInside(){area=Screen.FromControl(this).WorkingArea;Left=Math.Max(area.Left,Math.Min(Left,area.Right-Width));Top=Math.Max(area.Top,Math.Min(Top,area.Bottom-Height));}
  public void OpenDashboard(){if(dashboard==null || dashboard.IsDisposed)dashboard=new MainForm(this);dashboard.Show();dashboard.WindowState=FormWindowState.Normal;dashboard.BringToFront();dashboard.Activate();Startup.Log("Dashboard visible="+dashboard.Visible+" handle="+dashboard.Handle);}
  public void Say(string message,bool notify){
   bubble=message;bubbleUntil=AppClock.Now.AddSeconds(18);RenderLayered();
   if(notify && !Store.State.Quiet){tray.BalloonTipTitle=Store.State.PetName+"의 업무 알림";tray.BalloonTipText=message;tray.ShowBalloonTip(5000);}
  }
  void TickClock(){
   if(Startup.OpenRequest!=null && Startup.OpenRequest.WaitOne(0)){Show();OpenDashboard();}
   DateTime now=AppClock.Now;bool changed=false;
   var notices=Scheduler.Poll(Store.State,now,out changed);foreach(var notice in notices)pendingNotices.Enqueue(notice);if(pendingNotices.Count>0&&now>=nextNotice){var notice=pendingNotices.Dequeue();if(Store.State.Tasks.Any(t=>t.Id==notice.Task.Id&&!t.Done)){Say(notice.Text,true);nextNotice=now.AddSeconds(19);noticePose=notice.Task.ScheduleImage;noticePoseUntil=now.AddSeconds(18);}}var appearance=Scheduler.Appearance(Store.State,now);if(PetArt.SetScheduleImage(now<noticePoseUntil&&!String.IsNullOrEmpty(noticePose)?noticePose:appearance==null?"":appearance.ScheduleImage)){RenderLayered();if(dashboard!=null&&!dashboard.IsDisposed)dashboard.Invalidate(true);}
   if(Rules.FinishFocus(Store.State,now)){Say("집중 완료! 코인을 받았어요.\n잠깐 스트레칭할까요?",true);changed=true;}
   if(changed){Store.Save();if(dashboard!=null && !dashboard.IsDisposed)dashboard.RefreshPage();}
   if(game==null && !Store.State.Quiet && now.Second==0 && now.Minute%30==0 && now.Hour>=9 && now.Hour<19 && Store.State.FocusEnd==DateTime.MinValue)
    Say(random.Next(2)==0?"물 한 잔 마시고 올까요?":"어깨를 펴고 잠깐 쉬어요!",false);
  }
  protected override CreateParams CreateParams{get{var cp=base.CreateParams;cp.ExStyle|=0x80000;return cp;}}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnPaint(PaintEventArgs e){DrawFrame(e.Graphics);}
  public Bitmap RenderSnapshot(){var image=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(image)){g.Clear(Color.Transparent);DrawFrame(g);}return image;}
  void RenderLayered(){if(!IsHandleCreated||IsDisposed)return;using(var image=RenderSnapshot())AlphaWindow.Update(Handle,Location,image);}
  public void RenderLayeredForTest(){using(var image=RenderSnapshot())AlphaWindow.Update(Handle,Location,image);}
  void DrawFrame(Graphics graphics){
   if(AppClock.Now<bubbleUntil){
    var r=new Rectangle(7,5,266,70);using(var b=new SolidBrush(Color.FromArgb(255,253,246)))graphics.FillRectangle(b,r);
    Theme.Frame(graphics,r,Theme.Cream);
    using(var f=Theme.Font(9))Theme.Text(graphics,bubble,f,Theme.Ink,new Rectangle(17,15,246,52),ContentAlignment.MiddleCenter);
    using(var b=new SolidBrush(Color.FromArgb(255,253,246)))graphics.FillPolygon(b,new[]{new Point(130,75),new Point(146,75),new Point(137,87)});
   }
   PetArt.Draw(graphics,new Rectangle((Width-Store.State.PetSize)/2,82+(frame%2),Store.State.PetSize,Store.State.PetSize),frame,direction<0,Store.State.Equipped);
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
   if(args.Contains("--image-test")){int n=Array.IndexOf(args,"--image-test");if(n+1>=args.Length)return 2;return ImageTest(args[n+1]);}
   if(args.Contains("--ui-test"))return UiTest();
   if(args.Contains("--preview"))return PreviewShots();
   bool acquired;using(var mutex=new System.Threading.Mutex(true,"Local\\DeskBuddy.JY",out acquired)){

    if(!acquired){
     try{using(var signal=System.Threading.EventWaitHandle.OpenExisting("Local\\DeskBuddy.Open.JY"))signal.Set();Startup.Log("Requested existing window.");}
     catch(Exception e){Startup.Log("Activation failed: "+e);MessageBox.Show("DeskBuddy가 이미 실행 중입니다. 작업 표시줄의 숨겨진 아이콘에서 열어주세요.");}
     return 0;
    }
    Startup.OpenRequest=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"Local\\DeskBuddy.Open.JY");

    try{Store.Load();if(!PetArt.LoadCustom(Store.State.ImagePath)){Store.State.ImagePath="";MessageBox.Show("캐릭터 파일을 읽지 못해 기본 펫으로 시작합니다.");}

     Application.Run(new PetForm());
    }catch(Exception e){MessageBox.Show("실행 중 문제가 발생했습니다.\n"+e,"DeskBuddy");return 1;}finally{if(Startup.OpenRequest!=null){Startup.OpenRequest.Dispose();Startup.OpenRequest=null;}mutex.ReleaseMutex();}
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
     Descendants(f).OfType<PixelSizeSlider>().Single().Value=84;
     if(Store.State.PetSize!=84||p.Height!=174)throw new Exception("Live pet sizing");
     f.Navigate("설정 / 소개");Application.DoEvents();
     var options=Descendants(f).OfType<PixelToggle>().ToArray();
     if(options.Length!=2)throw new Exception("Pixel options");
     options[0].Checked=false;options[1].Checked=true;
     if(Store.State.Wander||!Store.State.Quiet)throw new Exception("Pixel toggle state");

     Store.State=new Data();Store.Load();
     if(Store.State.Coins!=10 || Store.State.Completed!=1 || Store.State.Tasks.Count!=1 || Store.State.Equipped!="민트 리본")throw new Exception("Disk persistence");
     if(Store.State.Wander||!Store.State.Quiet)throw new Exception("Pixel options persistence");if(Store.State.PetSize!=84)throw new Exception("Pet size persistence");
    }
    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"validation.txt"),"PASS: task creation via UI, completion reward, accessory purchase, volleyball page and physics, live pet sizing, disk save/reload, pixel toggles and size persistence.\r\nWindows notification delivery and multi-monitor/high-DPI behavior require manual verification.");
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
   try{
    VolleyballTests.Run();ScheduleTests.Run();
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
  }
 }
}
