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
  public static void Log(string message){try{Directory.CreateDirectory(Store.Root);File.AppendAllText(LogPath,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+" "+message+Environment.NewLine,Encoding.UTF8);}catch{}}
  public static void Error(Exception ex){Log(ex.ToString());MessageBox.Show("DeskBuddy를 실행하지 못했습니다.\n"+ex.Message+"\n\n오류 기록: "+LogPath,"DeskBuddy 시작 오류",MessageBoxButtons.OK,MessageBoxIcon.Error);}
 }

 public class TaskItem {
  public string Id=Guid.NewGuid().ToString(); public string Title=""; public DateTime Due;
  public bool Done; public bool Notified; public string Category="업무";
 }
 public class Data {
  public int Coins=30, Completed, FocusCount, FocusMinutes;
  public string PetName="스누피", ImagePath="", Equipped="기본"; public int PixelSize=3;
  public List<string> Owned=new List<string>{"기본"}; public List<TaskItem> Tasks=new List<TaskItem>();
  public bool Wander=true, Quiet=false; public DateTime FocusEnd=DateTime.MinValue; public int SessionMinutes; public string GamesDate=""; public int GamesPlayed;
 }
 public static class Store {
  public static readonly string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DeskBuddy");
  public static string FilePath=Path.Combine(Root,"data.json"); public static Data State=new Data(); public static string Warning="";
  public static void Load() {
   if(!File.Exists(FilePath))return;
   try { State=new JavaScriptSerializer().Deserialize<Data>(File.ReadAllText(FilePath,Encoding.UTF8));
    if(State==null || State.Tasks==null || State.Owned==null)throw new Exception("Invalid data");
    State.PixelSize=Math.Max(1,Math.Min(5,State.PixelSize));
   } catch { State=new Data(); Warning="저장 데이터를 읽지 못해 새 데이터를 사용합니다. 원본은 별도 파일로 보관합니다.";
    File.Copy(FilePath,FilePath+".damaged-"+DateTime.Now.ToString("yyyyMMddHHmmss"),true); }
  }
  public static bool Save() {
   try { Directory.CreateDirectory(Root); string temp=FilePath+".tmp";
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

  public static bool Complete(TaskItem t,Data d){if(t.Done)return false;t.Done=true;d.Completed++;d.Coins+=20;return true;}
  public static bool Buy(string item,int price,Data d){if(d.Owned.Contains(item)){d.Equipped=item;return true;}if(d.Coins<price)return false;d.Coins-=price;d.Owned.Add(item);d.Equipped=item;return true;}
  public static bool FinishFocus(Data d,DateTime now){if(d.FocusEnd==DateTime.MinValue || now<d.FocusEnd)return false;d.FocusCount++;d.FocusMinutes+=d.SessionMinutes;d.Coins+=d.SessionMinutes;d.FocusEnd=DateTime.MinValue;d.SessionMinutes=0;return true;}
 }
 public static class Theme {
  public static Color Bg=Color.FromArgb(246,247,251), Ink=Color.FromArgb(34,42,61), Muted=Color.FromArgb(111,120,141), Purple=Color.FromArgb(111,83,224);
  public static Font Font(float size,FontStyle style=FontStyle.Regular){return new Font("맑은 고딕",size,style);}
  public static Label Label(string text,int x,int y,int w,int h,float size=10,Color? color=null){return new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=Font(size),ForeColor=color??Ink,BackColor=Color.Transparent};}
  public static Button Button(string text,int x,int y,int w,int h,EventHandler click,bool primary=false){
   var b=new Button{Text=text,Location=new Point(x,y),Size=new Size(w,h),FlatStyle=FlatStyle.Flat,Font=Font(10,FontStyle.Bold),BackColor=primary?Purple:Color.White,ForeColor=primary?Color.White:Ink,Cursor=Cursors.Hand};
   b.FlatAppearance.BorderColor=Color.FromArgb(224,227,238);b.Click+=click;return b;
  }
 }
 public static class PetArt {
  public static Bitmap Custom;
  public static bool LoadCustom(string path){
   try { if(String.IsNullOrEmpty(path)){if(Custom!=null)Custom.Dispose();Custom=null;return true;}
    using(var im=Image.FromFile(path)){var b=new Bitmap(im);if(Custom!=null)Custom.Dispose();Custom=b;}return true;
   }catch{return false;}
  }
  static void Rect(Graphics g,Color c,int x,int y,int w,int h){using(var b=new SolidBrush(c))g.FillRectangle(b,x,y,w,h);}
  public static void Draw(Graphics output,Rectangle box,int frame,bool facingLeft,string accessory){
   using(var bmp=new Bitmap(64,64))using(var g=Graphics.FromImage(bmp)){
    if(Custom!=null){
     g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
     int size=Math.Max(12,64/Store.State.PixelSize);
     using(var small=new Bitmap(size,size))using(var sg=Graphics.FromImage(small)){
      sg.InterpolationMode=InterpolationMode.HighQualityBicubic;
      float scale=Math.Min((float)size/Custom.Width,(float)size/Custom.Height);int w=Math.Max(1,(int)(Custom.Width*scale)),h=Math.Max(1,(int)(Custom.Height*scale));
      sg.DrawImage(Custom,(size-w)/2,(size-h)/2,w,h);g.DrawImage(small,new Rectangle(5,3,54,56),0,0,size,size,GraphicsUnit.Pixel);
     }
    }else{
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
    if(accessory=="별빛 모자"){Rect(g,Color.FromArgb(116,86,232),16,7,32,5);Rect(g,Color.FromArgb(116,86,232),23,0,18,8);Rect(g,Color.Gold,30,3,4,4);}
    if(accessory=="민트 리본"){Rect(g,Color.FromArgb(52,191,157),25,33,8,7);Rect(g,Color.FromArgb(52,191,157),36,33,8,7);Rect(g,Color.FromArgb(25,140,115),33,35,3,3);}
    if(accessory=="왕관"){Rect(g,Color.Goldenrod,22,2,24,9);Rect(g,Color.Gold,22,0,4,8);Rect(g,Color.Gold,32,0,4,8);Rect(g,Color.Gold,42,0,4,8);}
    if(accessory=="하트 친구"){Rect(g,Color.HotPink,49,5,5,5);Rect(g,Color.HotPink,57,5,5,5);Rect(g,Color.HotPink,50,10,11,4);Rect(g,Color.HotPink,53,14,5,4);}
    if(facingLeft)bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
    var mode=output.InterpolationMode;output.InterpolationMode=InterpolationMode.NearestNeighbor;output.PixelOffsetMode=PixelOffsetMode.Half;output.DrawImage(bmp,box);output.InterpolationMode=mode;
   }
  }
 }
 public class PetForm:Form {
  Timer animation=new Timer{Interval=150},clock=new Timer{Interval=1000};NotifyIcon tray;MainForm dashboard;Random random=new Random();
  int frame,direction=1,ticks;Point drag;bool dragging,shuttingDown;DateTime bubbleUntil;string bubble="오늘도 함께 한 걸음!";Rectangle area;
  public PetForm(){
   Text="DeskBuddy · 바탕화면 펫";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
   BackColor=Color.Magenta;TransparencyKey=Color.Magenta;Size=new Size(280,220);DoubleBuffered=true;AutoScaleMode=AutoScaleMode.None;
   area=Screen.PrimaryScreen.WorkingArea;Location=new Point(area.Right-Width-70,area.Bottom-Height-12);bubbleUntil=DateTime.Now.AddSeconds(12);
   var menu=new ContextMenuStrip();
   menu.Items.Add("업무 대시보드 열기",null,(s,e)=>OpenDashboard());
   menu.Items.Add("응원 한마디",null,(s,e)=>Say("작은 완료 하나가 큰 성과가 돼!",false));
   menu.Items.Add("산책 켜기 / 끄기",null,(s,e)=>{Store.State.Wander=!Store.State.Wander;Store.Save();});
   menu.Items.Add("펫 숨기기 / 보이기",null,(s,e)=>Visible=!Visible);
   menu.Items.Add("종료",null,(s,e)=>Quit());ContextMenuStrip=menu;
   tray=new NotifyIcon{Icon=SystemIcons.Information,Text="DeskBuddy · 업무 친구",Visible=true,ContextMenuStrip=menu};
   tray.DoubleClick+=(s,e)=>{Show();OpenDashboard();};
   MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){drag=e.Location;dragging=true;}};
   MouseMove+=(s,e)=>{if(dragging)Location=new Point(Left+e.X-drag.X,Top+e.Y-drag.Y);};
   MouseUp+=(s,e)=>{dragging=false;KeepInside();};MouseDoubleClick+=(s,e)=>OpenDashboard();
   animation.Tick+=(s,e)=>{frame++;if(!dragging && Store.State.Wander && Store.State.FocusEnd==DateTime.MinValue && ++ticks%3==0){Left+=direction*4;area=Screen.FromControl(this).WorkingArea;if(Left<area.Left || Right>area.Right){direction=-direction;KeepInside();}}Invalidate();};
   clock.Tick+=(s,e)=>TickClock();animation.Start();clock.Start();
   Shown+=(s,e)=>{OpenDashboard();if(Store.Warning.Length>0)MessageBox.Show(Store.Warning,"DeskBuddy");};
   FormClosing+=(s,e)=>{if(!shuttingDown){e.Cancel=true;Hide();}};
  }
  void KeepInside(){area=Screen.FromControl(this).WorkingArea;Left=Math.Max(area.Left,Math.Min(Left,area.Right-Width));Top=Math.Max(area.Top,Math.Min(Top,area.Bottom-Height));}
  public void OpenDashboard(){if(dashboard==null || dashboard.IsDisposed)dashboard=new MainForm(this);dashboard.Show();dashboard.WindowState=FormWindowState.Normal;dashboard.BringToFront();dashboard.Activate();Startup.Log("Dashboard visible="+dashboard.Visible+" handle="+dashboard.Handle);}
  public void Say(string message,bool notify){
   bubble=message;bubbleUntil=DateTime.Now.AddSeconds(18);Invalidate();
   if(notify && !Store.State.Quiet){tray.BalloonTipTitle=Store.State.PetName+"의 업무 알림";tray.BalloonTipText=message;tray.ShowBalloonTip(5000);}
  }
  void TickClock(){
   if(Startup.OpenRequest!=null && Startup.OpenRequest.WaitOne(0)){Show();OpenDashboard();}
   DateTime now=DateTime.Now;bool changed=false;
   var due=Store.State.Tasks.Where(t=>!t.Done && !t.Notified && t.Due<=now).ToList();
   if(due.Count>0){foreach(var t in due)t.Notified=true;Say(due.Count==1?"일정 시간이에요!\n"+due[0].Title:due.Count+"개 일정 시간이 지났어요!\n"+due[0].Title+" 외 "+(due.Count-1)+"개",true);changed=true;}
   if(Rules.FinishFocus(Store.State,now)){Say("집중 완료! 코인을 받았어요.\n잠깐 스트레칭할까요?",true);changed=true;}
   if(changed){Store.Save();if(dashboard!=null && !dashboard.IsDisposed)dashboard.RefreshPage();}
   if(!Store.State.Quiet && now.Second==0 && now.Minute%30==0 && now.Hour>=9 && now.Hour<19 && Store.State.FocusEnd==DateTime.MinValue)
    Say(random.Next(2)==0?"물 한 잔 마시고 올까요?":"어깨를 펴고 잠깐 쉬어요!",false);
  }
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e);
   if(DateTime.Now<bubbleUntil){
    var r=new Rectangle(7,5,266,70);using(var b=new SolidBrush(Color.FromArgb(255,253,246)))e.Graphics.FillRectangle(b,r);
    using(var p=new Pen(Color.FromArgb(218,211,241),2))e.Graphics.DrawRectangle(p,r);
    using(var f=Theme.Font(9))TextRenderer.DrawText(e.Graphics,bubble,f,new Rectangle(17,15,246,52),Theme.Ink,TextFormatFlags.WordBreak|TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
    using(var b=new SolidBrush(Color.FromArgb(255,253,246)))e.Graphics.FillPolygon(b,new[]{new Point(130,75),new Point(146,75),new Point(137,87)});
   }
   PetArt.Draw(e.Graphics,new Rectangle(70,82+(frame%2),140,140),frame,direction<0,Store.State.Equipped);
  }
  protected override void Dispose(bool disposing){if(disposing){animation.Dispose();clock.Dispose();if(tray!=null){tray.Visible=false;tray.Dispose();tray=null;}}base.Dispose(disposing);}
  public void Quit(){shuttingDown=true;Store.Save();animation.Stop();clock.Stop();tray.Visible=false;tray.Dispose();Application.Exit();}
 }
 public class Preview:Control {
  public Preview(){DoubleBuffered=true;}
  protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);PetArt.Draw(e.Graphics,new Rectangle((Width-160)/2,(Height-160)/2,160,160),0,false,Store.State.Equipped);}
 }
 public class MainForm:Form {
  Timer gameTimer;Random gameRandom=new Random();PetForm pet;Panel content;Label wallet,countdown;string page="오늘의 업무";Timer live=new Timer{Interval=1000};ListBox taskList;
  public MainForm(PetForm owner){
   pet=owner;Text="DeskBuddy — 일이 즐거워지는 작은 친구";AutoScaleMode=AutoScaleMode.None;
   ClientSize=new Size(980,690);StartPosition=FormStartPosition.CenterScreen;BackColor=Theme.Bg;Font=Theme.Font(10);
   FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;
   var sidebar=new Panel{Location=new Point(0,0),Size=new Size(220,690),BackColor=Color.FromArgb(30,35,50)};Controls.Add(sidebar);
   sidebar.Controls.Add(Theme.Label("D E S K B U D D Y",22,29,190,30,13,Color.White));
   sidebar.Controls.Add(Theme.Label("작은 친구, 더 나은 업무 하루",22,67,190,24,9,Color.FromArgb(173,178,204)));
   string[] pages={"오늘의 업무","집중 스튜디오","미니게임","코인 상점","나의 캐릭터","설정 / 소개"};
   for(int i=0;i<pages.Length;i++){string target=pages[i];var b=Theme.Button(target,16,132+i*58,188,44,(s,e)=>{page=target;RefreshPage();});b.BackColor=Color.FromArgb(45,49,70);b.ForeColor=Color.White;b.FlatAppearance.BorderSize=0;sidebar.Controls.Add(b);}
   sidebar.Controls.Add(Theme.Label("WORK · FOCUS · GROW",22,607,185,22,9,Color.FromArgb(173,178,204)));
   sidebar.Controls.Add(Theme.Label("일정 완료 +20 / 집중 1분 +1",22,636,190,22,9,Color.FromArgb(173,178,204)));
   wallet=Theme.Label("",250,26,690,32,13);Controls.Add(wallet);
   content=new Panel{Location=new Point(244,75),Size=new Size(710,600),AutoScroll=true};Controls.Add(content);RefreshPage();
   live.Tick+=(s,e)=>{if(countdown!=null && !countdown.IsDisposed){var remaining=Store.State.FocusEnd-DateTime.Now;countdown.Text=Store.State.FocusEnd==DateTime.MinValue?"준비되면 시작해요":String.Format("{0:00}:{1:00}",Math.Max(0,(int)remaining.TotalMinutes),Math.Max(0,remaining.Seconds));}};
   live.Start();FormClosed+=(s,e)=>{live.Dispose();if(gameTimer!=null){gameTimer.Dispose();gameTimer=null;}};
  }
  void Add(Control c){content.Controls.Add(c);}
  void Title(string title,string subtitle){Add(Theme.Label(title,8,2,680,40,22));Add(Theme.Label(subtitle,10,50,680,28,10,Theme.Muted));}
  public void RefreshPage(){if(gameTimer!=null){gameTimer.Stop();gameTimer.Dispose();gameTimer=null;}
   foreach(Control c in content.Controls.Cast<Control>().ToArray())c.Dispose();content.Controls.Clear();countdown=null;
   wallet.Text="나의 업무 친구  /  "+Store.State.PetName+"                       ● "+Store.State.Coins+" 코인";
   if(page=="오늘의 업무")Tasks();else if(page=="집중 스튜디오")FocusPage();else if(page=="미니게임")Game();else if(page=="코인 상점")Shop();else if(page=="나의 캐릭터")Character();else Settings();
  }
  void Tasks(){
   Title("오늘도, 하나씩 해봐요.","일정은 펫이 알려주고, 완료하면 20코인을 선물해요.");
   Add(Theme.Label("남은 일정  "+Store.State.Tasks.Count(t=>!t.Done)+"       누적 완료  "+Store.State.Completed+"       집중  "+Store.State.FocusMinutes+"분",12,93,680,38,12,Theme.Purple));
   Add(Theme.Label("업무 이름",12,122,180,22,9,Theme.Muted));
   var input=new TextBox{Location=new Point(12,145),Size=new Size(430,30),MaxLength=100,Font=Theme.Font(11)};Add(input);
   var category=new ComboBox{Location=new Point(456,145),Size=new Size(225,30),DropDownStyle=ComboBoxStyle.DropDownList};category.Items.AddRange(new object[]{"업무","회의","마감","개인","휴식"});category.SelectedIndex=0;Add(category);
   Add(Theme.Label("알림 날짜와 시간",12,179,280,22,9,Theme.Muted));
   var due=new DateTimePicker{Location=new Point(12,200),Size=new Size(430,30),Format=DateTimePickerFormat.Custom,CustomFormat="yyyy-MM-dd  HH:mm",ShowUpDown=true,Value=DateTime.Now.AddMinutes(30)};Add(due);
   Add(Theme.Button("+ 일정 추가",456,195,225,38,(s,e)=>{
    if(String.IsNullOrWhiteSpace(input.Text)){MessageBox.Show("업무 이름을 입력해주세요.");return;}
    if(due.Value<=DateTime.Now){MessageBox.Show("현재보다 나중 시간을 선택해주세요.");return;}
    Store.State.Tasks.Add(new TaskItem{Title=input.Text.Trim(),Due=due.Value,Category=category.Text});Store.Save();RefreshPage();
   },true));
   taskList=new ListBox{Location=new Point(12,255),Size=new Size(669,220),BorderStyle=BorderStyle.None,Font=Theme.Font(11),ItemHeight=35,DrawMode=DrawMode.OwnerDrawFixed,BackColor=Color.White};
   foreach(var t in Store.State.Tasks.OrderBy(t=>t.Done).ThenBy(t=>t.Due))taskList.Items.Add(t);
   taskList.DrawItem+=(s,e)=>{if(e.Index<0)return;e.DrawBackground();var t=(TaskItem)taskList.Items[e.Index];
    string label=(t.Done?"✓ ":t.Due<DateTime.Now?"! ":"○ ")+t.Due.ToString("MM/dd HH:mm")+"  ["+t.Category+"] "+t.Title;
    Color color=(e.State&DrawItemState.Selected)!=0?SystemColors.HighlightText:t.Done?Theme.Muted:Theme.Ink;
    TextRenderer.DrawText(e.Graphics,label,taskList.Font,e.Bounds,color,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);e.DrawFocusRectangle();
   };Add(taskList);
   Add(Theme.Button("완료 +20 코인",12,495,208,42,(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t==null){MessageBox.Show("완료할 일정을 선택해주세요.");return;}if(Rules.Complete(t,Store.State)){Store.Save();pet.Say("잘했어요! 업무 완료 +20 코인",false);RefreshPage();}},true));
   Add(Theme.Button("10분 뒤 다시 알림",232,495,220,42,(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t==null||t.Done)return;t.Due=DateTime.Now.AddMinutes(10);t.Notified=false;Store.Save();RefreshPage();}));
   Add(Theme.Button("일정 삭제",464,495,217,42,(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t==null)return;if(MessageBox.Show("선택한 일정을 삭제할까요?","일정 삭제",MessageBoxButtons.YesNo)==DialogResult.Yes){Store.State.Tasks.Remove(t);Store.Save();RefreshPage();}}));
   if(taskList.Items.Count==0)Add(Theme.Label("아직 일정이 없어요. 첫 업무를 등록해보세요.",25,275,610, 30,11,Theme.Muted));
   Add(Theme.Label("펫 더블클릭: 대시보드 열기 / 오른쪽 클릭: 빠른 메뉴",12,555,680,30,9,Theme.Muted));
  }

  void FocusPage(){
   Title("집중하는 시간도, 성장으로.","집중 중에는 펫이 제자리에 머물러요. 완료한 시간만큼 코인을 받아요.");
   Add(new Preview{Location=new Point(16,95),Size=new Size(660,180),BackColor=Color.White});
   countdown=Theme.Label(Store.State.FocusEnd==DateTime.MinValue?"준비되면 시작해요":"집중 중이에요",16,289,660,65,30,Theme.Purple);countdown.TextAlign=ContentAlignment.MiddleCenter;Add(countdown);
   Add(Theme.Label("집중 시간 (분)",20,370,180,28,11));var minutes=new NumericUpDown{Location=new Point(205,369),Size=new Size(135,30),Minimum=1,Maximum=120,Value=25};Add(minutes);
   Add(Theme.Button("집중 시작",360,360,310,45,(s,e)=>{
    if(Store.State.FocusEnd!=DateTime.MinValue){MessageBox.Show("진행 중인 집중을 먼저 마치거나 취소해주세요.");return;}
    Store.State.SessionMinutes=(int)minutes.Value;Store.State.FocusEnd=DateTime.Now.AddMinutes((int)minutes.Value);Store.Save();pet.Say("지금부터 "+minutes.Value+"분, 함께 집중해요!",false);RefreshPage();
   },true));
   Add(Theme.Button("집중 취소",20,425,650,40,(s,e)=>{if(Store.State.FocusEnd==DateTime.MinValue)return;if(MessageBox.Show("집중을 취소할까요? 완료 보상은 지급되지 않아요.","집중 취소",MessageBoxButtons.YesNo)==DialogResult.Yes){Store.State.FocusEnd=DateTime.MinValue;Store.State.SessionMinutes=0;Store.Save();RefreshPage();}}));
   Add(Theme.Label("누적 "+Store.State.FocusCount+"회 완료 · "+Store.State.FocusMinutes+"분 집중\n추천: 25분 집중 후 5분 휴식. 앱을 종료해도 종료 시각은 저장돼요.",20,490,650,70,11,Theme.Muted));
  }

  public void Navigate(string target){page=target;RefreshPage();}
  void Game(){
   Title("20초, 잠깐의 별빛 휴식.","움직이는 별을 클릭해요. 하루 3번, 한 판 최대 20코인을 받을 수 있어요.");
   int played=Store.State.GamesDate==DateTime.Now.ToString("yyyy-MM-dd")?Store.State.GamesPlayed:0;
   Add(Theme.Label("오늘 보상 횟수  "+played+" / 3       한 번 클릭 = 1코인",20,94,650, thirtyHeight(),12,Theme.Purple));
   var status=Theme.Label("준비되면 시작해요!",20,138,650,34,14);Add(status);
   var field=new Panel{Location=new Point(20,190),Size=new Size(650,290),BackColor=Color.White};Add(field);
   var star=Theme.Button("★",270,120,60, fiftyHeight(),null,true);star.Font=Theme.Font(22);star.Visible=false;field.Controls.Add(star);
   int score=0;DateTime end=DateTime.MinValue;
   var start=Theme.Button("별 잡기 시작",20,503,650,44,null,true);Add(start);
   Add(Theme.Label("진행 중 화면을 바꾸면 게임이 취소돼요. 업무와 집중으로도 코인을 모을 수 있어요.",20,558,650, thirtyHeight(),9,Theme.Muted));
   star.Click+=(s,e)=>{if(end==DateTime.MinValue || DateTime.Now>=end)return;score++;star.Location=new Point(gameRandom.Next(10,field.Width-star.Width-10),gameRandom.Next(10,field.Height-star.Height-10));status.Text="남은 시간 "+Math.Max(0,(int)Math.Ceiling((end-DateTime.Now).TotalSeconds))+"초  /  잡은 별 "+score+"개";};
   start.Click+=(s,e)=>{
    if(Store.State.FocusEnd!=DateTime.MinValue){MessageBox.Show("집중이 끝나면 잠깐 놀아요!");return;}
    played=Store.State.GamesDate==DateTime.Now.ToString("yyyy-MM-dd")?Store.State.GamesPlayed:0;
    if(played>=3){MessageBox.Show("오늘은 세 번 모두 보상을 받았어요. 내일 다시 만나요!");return;}
    score=0;end=DateTime.Now.AddSeconds(20);star.Visible=true;start.Enabled=false;status.Text="20초 동안 별을 잡아보세요!";
    gameTimer=new Timer{Interval=100};
    gameTimer.Tick+=(sender,eventArgs)=>{
     double seconds=(end-DateTime.Now).TotalSeconds;
     if(seconds>0){status.Text="남은 시간 "+(int)Math.Ceiling(seconds)+"초  /  잡은 별 "+score+"개";return;}
     gameTimer.Stop();gameTimer.Dispose();gameTimer=null;star.Visible=false;start.Enabled=true;
     int reward=Rules.GameReward(Store.State,score,DateTime.Now);
     Store.Save();pet.Say("별 "+score+"개를 잡았어요! +"+Math.Max(0,reward)+" 코인",false);RefreshPage();
    };gameTimer.Start();
   };
  }
  int thirtyHeight(){return 30;}int fiftyHeight(){return 50;}

  void Shop(){
   Title("열심히 일한 당신과 친구에게.","코인으로 액세서리를 구매하고 장착해요. 보유 아이템은 계속 사용할 수 있어요.");
   string[] names={"기본","민트 리본","별빛 모자","하트 친구","왕관"};int[] prices={0,40,70,100,150};
   for(int i=0;i<names.Length;i++){string name=names[i];int price=prices[i];int y=95+i*85;bool owned=Store.State.Owned.Contains(name);
    Add(Theme.Label(name+(Store.State.Equipped==name?"   · 장착 중":""),24,y+5,400,30,14));
    Add(Theme.Label(owned?"보유 아이템":price+" 코인",24,y+36,400,25,10,Theme.Muted));
    Add(Theme.Button(owned?"장착하기":"구매 + 장착",465,y+6,210,50,(s,e)=>{
     if(!Rules.Buy(name,price,Store.State)){MessageBox.Show("코인이 부족해요. 업무 완료와 집중으로 모아보세요.");return;}
     Store.Save();pet.Invalidate();pet.Say("새로운 스타일, 마음에 들어요!",false);RefreshPage();
    },!owned));
   }
  }
  void Character(){
   Title("내가 좋아하는 캐릭터와 함께.","기본 픽셀 스누피 또는 직접 고른 이미지로 나만의 업무 친구를 만들어보세요.");
   Add(new Preview{Location=new Point(20,95),Size=new Size(650,190),BackColor=Color.White});
   Add(Theme.Label("친구 이름",20,310,150,25,11));var name=new TextBox{Text=Store.State.PetName,Location=new Point(175,306),Size=new Size(330,30),MaxLength=24};Add(name);
   Add(Theme.Button("이름 저장",520,302,150,36,(s,e)=>{if(String.IsNullOrWhiteSpace(name.Text))return;Store.State.PetName=name.Text.Trim();Store.Save();RefreshPage();},true));
   Add(Theme.Button("캐릭터 이미지 불러오기",20,360,320,42,(s,e)=>Import()));
   Add(Theme.Button("기본 스누피로 돌아가기",350,360,320,42,(s,e)=>{Store.State.ImagePath="";Store.State.PetName="스누피";PetArt.LoadCustom("");Store.Save();pet.Invalidate();RefreshPage();}));
   Add(Theme.Label("픽셀 크기",20,428,150,28,11));var pixel=new NumericUpDown{Location=new Point(175,425),Size=new Size(120,30),Minimum=1,Maximum=5,Value=Store.State.PixelSize};
   pixel.ValueChanged+=(s,e)=>{Store.State.PixelSize=(int)pixel.Value;Store.Save();pet.Invalidate();foreach(Control c in content.Controls)if(c is Preview)c.Invalidate();};Add(pixel);
   Add(Theme.Label("투명 배경 PNG를 추천해요. JPG는 배경도 함께 표시돼요.\n이미지를 픽셀 느낌으로 표현하며 새 그림이나 걷기 프레임을 생성하지는 않아요.\n기본 펫은 걷는 동작, 가져온 캐릭터는 통통 튀는 동작으로 이동해요.",20,484,660,95,10,Theme.Muted));
  }
  void Import(){
   using(var dialog=new OpenFileDialog{Filter="캐릭터 이미지|*.png;*.jpg;*.jpeg;*.bmp",Title="좋아하는 캐릭터를 선택하세요"}){
    if(dialog.ShowDialog()!=DialogResult.OK)return;
    try{if(new FileInfo(dialog.FileName).Length>10*1024*1024)throw new Exception("10MB 이하 이미지를 사용해주세요.");
     using(var im=Image.FromFile(dialog.FileName)){
      if(im.Width>4096||im.Height>4096)throw new Exception("가로와 세로가 4096px 이하인 이미지를 사용해주세요.");
      Directory.CreateDirectory(Store.Root);string target=Path.Combine(Store.Root,"character-"+Guid.NewGuid().ToString("N")+".png");
      using(var b=new Bitmap(im))b.Save(target,ImageFormat.Png);if(!PetArt.LoadCustom(target))throw new Exception("이미지를 불러오지 못했습니다.");
      Store.State.ImagePath=target;Store.Save();pet.Invalidate();RefreshPage();
     }
    }catch(Exception e){MessageBox.Show(e.Message,"이미지 가져오기");}
   }
  }
  void Settings(){
   Title("업무를 방해하지 않는 동료.","개인 일정과 보상은 이 PC에 저장됩니다. 서버나 계정 없이 사용할 수 있어요.");
   var wander=new CheckBox{Text="바탕화면에서 자유롭게 산책하기",Checked=Store.State.Wander,Location=new Point(20,108),Size=new Size(650,34)};
   wander.CheckedChanged+=(s,e)=>{Store.State.Wander=wander.Checked;Store.Save();};Add(wander);
   var quiet=new CheckBox{Text="조용한 모드 (Windows 알림 및 정기 응원 끄기)",Checked=Store.State.Quiet,Location=new Point(20,158),Size=new Size(650,34)};
   quiet.CheckedChanged+=(s,e)=>{Store.State.Quiet=quiet.Checked;Store.Save();};Add(quiet);
   Add(Theme.Button("말풍선 알림 테스트",20,220,315,42,(s,e)=>pet.Say("5분 뒤 회의가 있어요!\n자료를 준비해볼까요?",true),true));
   Add(Theme.Button("내 데이터 백업",350,220,320,42,(s,e)=>{
    using(var dialog=new SaveFileDialog{Filter="JSON 백업|*.json",FileName="DeskBuddy-backup-"+DateTime.Now.ToString("yyyyMMdd")+".json"})
    if(dialog.ShowDialog()==DialogResult.OK){try{File.WriteAllText(dialog.FileName,new JavaScriptSerializer().Serialize(Store.State),Encoding.UTF8);MessageBox.Show("일정과 보상 데이터를 백업했습니다. 캐릭터 이미지는 별도로 보관해주세요.");}catch(Exception ex){MessageBox.Show("백업 실패: "+ex.Message);}}
   }));
   Add(Theme.Label("DESKBUDDY · 개인 업무 몰입 도우미",20,310,650,35,15,Theme.Purple));
   Add(Theme.Label("일정 알림 → 업무 완료 → 코인 보상 → 캐릭터 꾸미기\n\n업무 알림과 작은 보상을 연결해 업무 진행을 눈에 보이게 만듭니다.\n펫 드래그: 이동 / 더블클릭: 대시보드 / 오른쪽 클릭: 메뉴\n창을 닫아도 펫은 계속 실행됩니다. 완전 종료는 아래 버튼을 사용하세요.\n알림은 앱 실행 중 동작하며, 절전·종료 중 지난 일정은 복귀 후 알려줘요.",20,360,650,160,10,Theme.Muted));
   Add(Theme.Button("DeskBuddy 완전 종료",20,535,650,42,(s,e)=>pet.Quit()));
  }
 }
 static class Program {

  [STAThread] static int Main(string[] args){
   try{
    Startup.Log("Starting "+Application.ExecutablePath+" args="+String.Join(" ",args));
    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
    Application.ThreadException+=(s,e)=>Startup.Error(e.Exception);
    AppDomain.CurrentDomain.UnhandledException+=(s,e)=>Startup.Log("Unhandled: "+e.ExceptionObject);
    return RunMain(args);
   }catch(Exception e){Startup.Error(e);return 1;}
  }
  static int RunMain(string[] args){

   if(args.Contains("--self-test"))return SelfTest();
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   if(args.Contains("--ui-test"))return UiTest();
   bool acquired;using(var mutex=new System.Threading.Mutex(true,"Local\\DeskBuddy.JY",out acquired)){

    if(!acquired){
     try{using(var signal=System.Threading.EventWaitHandle.OpenExisting("Local\\DeskBuddy.Open.JY"))signal.Set();Startup.Log("Requested existing window.");}
     catch(Exception e){Startup.Log("Activation failed: "+e);MessageBox.Show("DeskBuddy가 이미 실행 중입니다. 작업 표시줄의 숨겨진 아이콘에서 열어주세요.");}
     return 0;
    }
    Startup.OpenRequest=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"Local\\DeskBuddy.Open.JY");

    try{Store.Load();if(!PetArt.LoadCustom(Store.State.ImagePath)){Store.State.ImagePath="";MessageBox.Show("캐릭터 파일을 읽지 못해 기본 펫으로 시작합니다.");}
     if(args.Contains("--preview"))return PreviewShots();
     Application.Run(new PetForm());
    }catch(Exception e){MessageBox.Show("실행 중 문제가 발생했습니다.\n"+e,"DeskBuddy");return 1;}finally{if(Startup.OpenRequest!=null){Startup.OpenRequest.Dispose();Startup.OpenRequest=null;}mutex.ReleaseMutex();}
   }return 0;
  }
  static int PreviewShots(){
   string target=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview");Directory.CreateDirectory(target);
   Store.State=new Data();
   Store.State.Tasks.Add(new TaskItem{Title="주간 회의 자료 정리",Due=DateTime.Now.AddMinutes(30),Category="회의"});
   Store.State.Tasks.Add(new TaskItem{Title="고객 요청 사항 검토",Due=DateTime.Now.AddHours(2)});
   using(var p=new PetForm())using(var f=new MainForm(p)){f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-2200,-1200);f.Show();Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(target,"dashboard.png"),ImageFormat.Png);}

    foreach(string name in new[]{"집중 스튜디오","미니게임","코인 상점","나의 캐릭터","설정 / 소개"}){
     f.Navigate(name);Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(target,name.Replace(" / ","-")+".png"),ImageFormat.Png);}
    }

    using(var b=new Bitmap(p.Width,p.Height)){p.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(target,"pet.png"),ImageFormat.Png);}
   }return 0;
  }

  static IEnumerable<Control> Descendants(Control root){foreach(Control c in root.Controls){yield return c;foreach(var child in Descendants(c))yield return child;}}
  static Button FindButton(Form form,string text){return Descendants(form).OfType<Button>().First(b=>b.Text==text);}
  static int UiTest(){
   string oldPath=Store.FilePath;Data oldState=Store.State;string temp=Path.Combine(Path.GetTempPath(),"DeskBuddy-test-"+Guid.NewGuid().ToString("N"));
   Directory.CreateDirectory(temp);Store.FilePath=Path.Combine(temp,"data.json");Store.State=new Data();
   try{
    using(var p=new PetForm())using(var f=new MainForm(p)){
     f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-2200,-1200);f.Show();Application.DoEvents();
     Descendants(f).OfType<TextBox>().First().Text="검증용 업무";
     FindButton(f,"+ 일정 추가").PerformClick();Application.DoEvents();
     if(Store.State.Tasks.Count!=1)throw new Exception("UI add task");
     Descendants(f).OfType<ListBox>().First().SelectedIndex=0;
     FindButton(f,"완료 +20 코인").PerformClick();Application.DoEvents();
     if(Store.State.Coins!=50 || Store.State.Completed!=1)throw new Exception("UI completion");
     f.Navigate("코인 상점");FindButton(f,"구매 + 장착").PerformClick();Application.DoEvents();
     if(Store.State.Coins!=10 || Store.State.Equipped!="민트 리본")throw new Exception("UI purchase");
     f.Navigate("미니게임");FindButton(f,"별 잡기 시작").PerformClick();Application.DoEvents();
     for(int i=0;i<5;i++)FindButton(f,"★").PerformClick();
     DateTime waitUntil=DateTime.Now.AddSeconds(21);
     while(DateTime.Now<waitUntil){Application.DoEvents();System.Threading.Thread.Sleep(15);}
     if(Store.State.Coins!=15 || Store.State.GamesPlayed!=1)throw new Exception("UI game completion");
     Store.State=new Data();Store.Load();
     if(Store.State.Coins!=15 || Store.State.Completed!=1 || Store.State.Tasks.Count!=1 || Store.State.Equipped!="민트 리본")throw new Exception("Disk persistence");
    }
    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"validation.txt"),"PASS: task creation via UI, completion reward, accessory purchase, 20-second game timer and reward, disk save/reload.\r\nWindows notification delivery and multi-monitor/high-DPI behavior require manual verification.");
    return 0;
   }catch(Exception e){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"validation.txt"),"FAIL: "+e);return 1;}
   finally{Store.FilePath=oldPath;Store.State=oldState;foreach(string file in Directory.GetFiles(temp))File.Delete(file);Directory.Delete(temp);}
  }

  static int SelfTest(){
   try{
    var d=new Data();var t=new TaskItem();
    if(!Rules.Complete(t,d)||d.Coins!=50||d.Completed!=1)throw new Exception("Completion");
    if(Rules.Complete(t,d)||d.Coins!=50)throw new Exception("Duplicate");
    if(Rules.Buy("왕관",150,d)||d.Coins!=50)throw new Exception("Overspending");
    if(!Rules.Buy("민트 리본",40,d)||d.Coins!=10)throw new Exception("Purchase");
    if(!Rules.Buy("민트 리본",40,d)||d.Coins!=10)throw new Exception("Reequip");
    d.SessionMinutes=25;d.FocusEnd=DateTime.Now.AddSeconds(10);
    if(Rules.FinishFocus(d,DateTime.Now))throw new Exception("Early focus");
    if(!Rules.FinishFocus(d,DateTime.Now.AddMinutes(1))||d.Coins!=35||d.FocusMinutes!=25)throw new Exception("Focus");
    if(Rules.FinishFocus(d,DateTime.Now.AddMinutes(2)))throw new Exception("Duplicate focus");

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
