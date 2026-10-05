using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DeskBuddy {
 public static class Theme {
  public static Color Bg=Color.FromArgb(248,238,216),Ink=Color.FromArgb( 70,51,46),Muted=Color.FromArgb(135,110,87),Purple=Color.FromArgb(84,139,112);
  public static Color Cream=Color.FromArgb(255,248,228),Peach=Color.FromArgb(239,172,139),Gold=Color.FromArgb(237,190,91),Line=Color.FromArgb( 80,57,47),Light=Color.FromArgb(224,207,173);
  
  static PrivateFontCollection fonts=new PrivateFontCollection();static IntPtr memory;
  [DllImport("gdi32.dll")] static extern IntPtr AddFontMemResourceEx(IntPtr data,uint size,IntPtr reserved,ref uint count);
  static Theme(){
   using(var stream=typeof(Theme).Assembly.GetManifestResourceStream("DeskBuddy.PixelFont")){
    if(stream==null)return;byte[] bytes=new byte[stream.Length];stream.Read(bytes,0,bytes.Length);
    memory=Marshal.AllocCoTaskMem(bytes.Length);Marshal.Copy(bytes,0,memory,bytes.Length);
    fonts.AddMemoryFont(memory,bytes.Length);uint count=0;AddFontMemResourceEx(memory,(uint)bytes.Length,IntPtr.Zero,ref count);
   }
  }
  public static Font Font(float size,FontStyle style=FontStyle.Regular){
   float px=size>=22?32:size>=14?24:16;
   return fonts.Families.Length>0?new Font(fonts.Families[0],px,FontStyle.Regular,GraphicsUnit.Pixel):new Font("굴림체",px,FontStyle.Regular,GraphicsUnit.Pixel);
  }
  public static Label Label(string text,int x,int y,int w,int h,float size=10,Color? color=null){return new PixelLabel{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=Font(size),ForeColor=color??Ink,BackColor=Color.Transparent};}
  public static Button Button(string text,int x,int y,int w,int h,EventHandler click,bool primary=false){
   var b=new PixelButton{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=Font(10),BackColor=primary?Purple:Cream,ForeColor=primary?Cream:Ink};if(click!=null)b.Click+=click;return b;
  }
  public static void Fill(Graphics g,Color c,int x,int y,int w,int h){using(var b=new SolidBrush(c))g.FillRectangle(b,x,y,w,h);}
  public static void Frame(Graphics g,Rectangle r,Color fill){
   Fill(g,Line,r.X+4,r.Y,r.Width-8,r.Height);Fill(g,Line,r.X,r.Y+4,r.Width,r.Height-8);
   Fill(g,fill,r.X+4,r.Y+4,r.Width-8,r.Height-8);
   Fill(g,Color.FromArgb(255,252,237),r.X+5,r.Y+5,r.Width-10,2);
  }
  public static void Text(Graphics g,string text,Font font,Color color,Rectangle bounds,ContentAlignment align=ContentAlignment.MiddleLeft){
   g.TextRenderingHint=TextRenderingHint.SingleBitPerPixelGridFit;using(var b=new SolidBrush(color))using(var sf=new StringFormat()){
    sf.Alignment=(align==ContentAlignment.MiddleCenter || align==ContentAlignment.TopCenter)?StringAlignment.Center:align==ContentAlignment.MiddleRight?StringAlignment.Far:StringAlignment.Near;
    sf.LineAlignment=align==ContentAlignment.TopLeft || align==ContentAlignment.TopCenter?StringAlignment.Near:StringAlignment.Center;sf.Trimming=StringTrimming.EllipsisCharacter;
    g.DrawString(text,font,b,bounds,sf);
   }
  }
  public static TextBox Input(string text,int x,int y,int w){return new TextBox{Text=text,Location=new Point(x,y),Size=new Size(w,28),Font=Font(10),BackColor=Cream,ForeColor=Ink,BorderStyle=BorderStyle.FixedSingle};}
 }
 public class PixelLabel:Label {
  public PixelLabel(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
  protected override void OnPaint(PaintEventArgs e){Theme.Text(e.Graphics,Text,Font,ForeColor,ClientRectangle,TextAlign);}
 }
 public class PixelButton:Button {
  bool hover,pressed;public string IconId="";
  public PixelButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}
  protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
  protected override void OnPaint(PaintEventArgs e){
   Color fill=!Enabled?Theme.Light:hover?ControlPaint.Light(BackColor,.12f):BackColor;
   var r=new Rectangle(0,pressed?3:0,Width,Height-(pressed?3:4));Theme.Frame(e.Graphics,r,fill);
   if(!pressed)Theme.Fill(e.Graphics,Theme.Line,4,Height-4,Width-8,4);
   Theme.Fill(e.Graphics,ControlPaint.Dark(fill,.08f),4,Height-10,Width-8,3);
   int offset=String.IsNullOrEmpty(IconId)?0:32;
   if(offset>0)PixelIcons.Draw(e.Graphics,IconId,new Point(14,Height/2-10),2);
   Theme.Text(e.Graphics,Text,Font,Enabled?ForeColor:Theme.Muted,new Rectangle(offset+5,pressed?3:0,Width-offset-10,Height-5),ContentAlignment.MiddleCenter);
   if(Focused){using(var p=new Pen(ForeColor)){p.DashStyle=DashStyle.Dot;e.Graphics.DrawRectangle(p,8,7,Width-17,Height-17);}}
  }
 }
 public class PixelPanel:Panel {
  public PixelPanel(){DoubleBuffered=true;BackColor=Theme.Cream;Padding=new Padding(10);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Theme.Frame(e.Graphics,ClientRectangle,BackColor);}
 }
 public class PixelToggle:CheckBox {
  public PixelToggle(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Size=new Size(104,36);Cursor=Cursors.Hand;Font=Theme.Font(10);AccessibleRole=AccessibleRole.CheckButton;}
  protected override void OnPaint(PaintEventArgs e){
   Theme.Frame(e.Graphics,new Rectangle(0,0,Width,Height),Checked?Theme.Purple:Theme.Light);
   Theme.Fill(e.Graphics,Theme.Cream,Checked?Width-30:6,7,22,22);
   Theme.Text(e.Graphics,Checked?"ON":"OFF",Font,Checked?Theme.Cream:Theme.Ink,new Rectangle(Checked?2:30,0,Width-32,Height),ContentAlignment.MiddleCenter);
  }
 }
 public static class PixelIcons {
  static Dictionary<string,string[]> art=new Dictionary<string,string[]>{
   {"home",new[]{"0000110000","0001111000","0011111100","0111111110","1111111111","0011111100","0011001100","0011001100","0011001100","0011111100"}},
   {"quest",new[]{"0111111110","0110000110","0111111110","0100000010","0110111010","0100000010","0110111010","0100000010","0111111110","0000000000"}},
   {"focus",new[]{"0001111000","0011001100","0110000110","0100100010","1100100011","1100111011","0100000010","0110000110","0011001100","0001111000"}},
   {"star",new[]{"0000110000","0000110000","0001111000","1111111111","0111111110","0011111100","0011111100","0111001110","0110000110","0100000010"}},
   {"shop",new[]{"0111111110","1111111111","1101101101","1101101101","0111111110","0100000010","0101110010","0101010010","0101010010","0111111110"}},
   {"pet",new[]{"0011001100","0111111110","0111111110","0101001010","0111111110","0010110100","0001111000","0011111100","0011111100","0001111000"}},
   {"options",new[]{"0001100000","0111111100","0110011100","1100000110","1110011110","0111111100","0011110000","0001100000","0000000000","0000000000"}},
   {"coin",new[]{"0001111000","0011111100","0110001110","1101100111","1101100111","1101100111","1101100111","0110001110","0011111100","0001111000"}}
  };
  public static void Draw(Graphics g,string id,Point p,int scale){
   if(!art.ContainsKey(id))id="star";string[] rows=art[id];
   Color c=id=="coin"?Theme.Gold:id=="star"?Theme.Gold:Theme.Ink;
   for(int y=0;y<rows.Length;y++)for(int x=0;x<rows[y].Length;x++)if(rows[y][x]=='1')Theme.Fill(g,c,p.X+x*scale,p.Y+y*scale,scale,scale);
  }
 }
 public class RoomScene:Control {
  Timer clock=new Timer{Interval=180};int frame;public bool Night;
  public RoomScene(){DoubleBuffered=true;clock.Tick+=(s,e)=>{frame++;if(Visible)Invalidate();};clock.Start();Cursor=Cursors.Hand;}
  protected override void Dispose(bool disposing){if(disposing)clock.Dispose();base.Dispose(disposing);}
  static void R(Graphics g,string hex,int x,int y,int w,int h){Theme.Fill(g,ColorTranslator.FromHtml(hex),x,y,w,h);}
  protected override void OnPaint(PaintEventArgs e){
   using(var b=new Bitmap(384,150))using(var g=Graphics.FromImage(b)){
    R(g,"#F4D9AE",0,0,384,102);R(g,"#C69770",0,102,384,48);R(g,"#775240",0,100,384,3);
    for(int y=0;y<95;y+=16)for(int x=8;x<384;x+=16){R(g,"#E7C599",x,y,2,2);R(g,"#FFF0CC",x+1,y+1,1,1);}
    for(int y=112;y<150;y+=12){R(g,"#A67756",0,y,384,1);for(int x=(y%24==4?24:0);x<384;x+=48)R(g,"#A67756",x,y-11,1,11);}
    R(g,"#674B3D",20,15,91,65);R(g,"#FFF3D5",23,18,85,59);R(g,Night?"#3C4766":"#ACDAD9",27,22,77,51);
    R(g,Night?"#FFE1A0":"#FFECA6",82,26,12,12);R(g,Night?"#65797D":"#8BAA78",27,53,77,20);
    R(g,"#678466",31,45,20,20);R(g,"#678466",78,49,26,24);R(g,"#88AB7C",35,41,12,18);
    R(g,"#FFF3D5",63,19,3,57);R(g,"#FFF3D5",25,44,80,3);

    R(g,"#B97370",16,15,10,70);R(g,"#E2AAA0",16,20,4,61);R(g,"#B97370",105,15,10,70);R(g,"#E2AAA0",109,20,4,61);
    R(g,"#76553F",281,20,78,75);R(g,"#B58159",285,24,70,66);R(g,"#76553F",284,48,72,4);R(g,"#76553F",284,72,72,4);
    string[] book={"#7FAD97","#CB7872","#EACA83","#88A9B3","#E2AB82"};
    for(int i=0;i<8;i++){R(g,book[i%5],290+i*7,30+(i%2)*3,5,18-(i%2)*3);R(g,"#FFF0D0",291+i*7,33+(i%2)*3,3,1);}
    R(g,"#F2DDA9",292,57,15,13);R(g,"#678A67",296,52,7,6);R(g,"#F2DDA9",331,56,14,14);R(g,"#A2754F",293,82,48,5);
    R(g,"#76553F",132,21,23,24);R(g,"#FFF4D7",135,24,17,18);R(g,"#76553F",142,27,2,8);R(g,"#76553F",143,34,5,2);
    R(g,"#E7B87E",108,111,161,28);R(g,"#FFE3AA",112,115,153,20);R(g,"#C58D68",116,117,145,2);R(g,"#C58D68",116,131,145,2);
    R(g,"#865348",235,67,34, 40);R(g,"#CA6862",237,66,30,42);
    for(int i=0;i<6;i++)R(g,"#884F45",230+i*4,64-i*4,44-i*8,4);
    R(g,"#DF8173",235, 60,34,7);R(g,"#5B443C",245, 80,14,28);R(g,"#FFF0D0",244, 73,16,3);
    R(g,"#745344",26,102,25,23);R(g,"#BF7E60",28,105,21,17);
    R(g,"#668958",35,81,8,23);R(g,"#7DA56B",22,86,16,7);R(g,"#7DA56B",41,88,16,7);R(g,"#93B781",28,77,11,8);R(g,"#93B781",40,75,10,11);
    R(g,"#76553F",314,102,44,4);R(g,"#5A7D84",318,107,36,6);R(g,"#C5DFD0",322,109,28,3);
    if(!PetArt.HasCustom)PetArt.Draw(g,new Rectangle(139,58+(frame%2),74,74),frame,false,Store.State.Equipped);
    if(frame%12<6){R(g,"#D98980",217,80,2,2);R(g,"#D98980",215,82,6,2);R(g,"#D98980",217,84,2,2);}
    e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(b,ClientRectangle);
    if(PetArt.HasCustom)PetArt.Draw(e.Graphics,new Rectangle((int)(139*Width/384.0),(int)((58+frame%2)*Height/150.0),(int)(74*Width/384.0),(int)(74*Height/150.0)),frame,false,Store.State.Equipped);
   }
  }
  
 }
 public class PetPortrait:Control {
  public string Item;public PetPortrait(){DoubleBuffered=true;BackColor=Theme.Cream;}
  protected override void OnPaint(PaintEventArgs e){Theme.Frame(e.Graphics,ClientRectangle,BackColor);PetArt.Draw(e.Graphics,new Rectangle((Width-100)/2,(Height-100)/2,100,100),0,false,Item??Store.State.Equipped);}
 }
 public class ItemPortrait:Control {
  public string Item;
  protected override void OnPaint(PaintEventArgs e){PetArt.Draw(e.Graphics,new Rectangle((Width-90)/2,0,90,90),0,false,Item);}
 }

 public class PixelSizeSlider:Control {
  int value=140;public event EventHandler ValueChanged;
  public int Value{get{return value;}set{int next=Math.Max(40,Math.Min(320,value));if(next==this.value)return;this.value=next;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
  public PixelSizeSlider(){DoubleBuffered=true;TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.Slider;Size=new Size(300,36);}
  void Pick(int x){Value=40+(int)Math.Round(Math.Max(0,Math.Min(1,(x-12)/(double)Math.Max(1,Width-24)))*280);}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();Capture=true;Pick(e.X);}}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture&&e.Button==MouseButtons.Left)Pick(e.X);}
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);Capture=false;}
  protected override bool IsInputKey(Keys k){return k==Keys.Left||k==Keys.Right||base.IsInputKey(k);}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Left)Value-=5;if(e.KeyCode==Keys.Right)Value+=5;if(e.KeyCode==Keys.Home)Value=40;if(e.KeyCode==Keys.End)Value=320;}
  protected override void OnPaint(PaintEventArgs e){
   Theme.Frame(e.Graphics,new Rectangle(4,12,Width-8,12),Theme.Light);
   int x=12+(int)((Width-24)*(value-40)/280.0);Theme.Fill(e.Graphics,Theme.Purple,8,16,Math.Max(1,x-8),4);
   Theme.Frame(e.Graphics,new Rectangle(x-9,3,18,30),Focused?Theme.Gold:Theme.Cream);
  }
 }
 public class VolleyballPreview:Control {
  Bitmap preview;
  public VolleyballPreview(PetForm owner){DoubleBuffered=true;using(var game=new VolleyballForm(owner))preview=game.SnapshotCourt();}
  protected override void OnPaint(PaintEventArgs e){Theme.Frame(e.Graphics,ClientRectangle,Color.FromArgb(221,230,210));e.Graphics.DrawImage(preview,new Rectangle(8,8,Width-16,Height-16));}
  protected override void Dispose(bool disposing){if(disposing&&preview!=null)preview.Dispose();base.Dispose(disposing);}
 }

 public class MainForm:Form {
  PetForm pet;Panel content,sidebar;Label wallet,countdown;Timer live=new Timer{Interval=1000};
  string page="나의 방";ListBox taskList;Dictionary<string,Button> nav=new Dictionary<string,Button>();Point drag;
  public MainForm(PetForm owner){
   pet=owner;Text="DeskBuddy · 작은 업무 모험";AutoScaleMode=AutoScaleMode.None;ClientSize=new Size(1024,744);
   StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.None;BackColor=Theme.Bg;Font=Theme.Font(10);DoubleBuffered=true;
   var header=new PixelPanel{Location=new Point(0,0),Size=new Size(1024,56),BackColor=Theme.Peach};Controls.Add(header);
   var brand=Theme.Label("DESKBUDDY  /  작은 업무 모험",20,9,690, 30,13);header.Controls.Add(brand);
   MouseEventHandler down=(s,e)=>{if(e.Button==MouseButtons.Left)drag=PointToClient(Cursor.Position);};MouseEventHandler move=(s,e)=>{if(e.Button==MouseButtons.Left){Point now=PointToClient(Cursor.Position);Location=new Point(Left+now.X-drag.X,Top+now.Y-drag.Y);}};
   header.MouseDown+=down;header.MouseMove+=move;brand.MouseDown+=down;brand.MouseMove+=move;
   header.Controls.Add(Theme.Button("_",923,9,38,38,(s,e)=>WindowState=FormWindowState.Minimized));
   header.Controls.Add(Theme.Button("X",969,9,38,38,(s,e)=>Close()));
   sidebar=new PixelPanel{Location=new Point(12, 70),Size=new Size(212,636),BackColor=Color.FromArgb(237,222,190)};Controls.Add(sidebar);
   sidebar.Controls.Add(new PetPortrait{Location=new Point(16,14),Size=new Size(180,104),BackColor=Theme.Cream});
   var friend=Theme.Label(Store.State.PetName,16,124,180,26,11);friend.TextAlign=ContentAlignment.MiddleCenter;sidebar.Controls.Add(friend);
   string[] pages={"나의 방","오늘의 업무","집중 스튜디오","미니게임","코인 상점","나의 캐릭터","설정 / 소개"};
   string[] names={"나의 방","퀘스트 보드","집중 모험","펫 배구","아이템 상점","캐릭터","옵션"};
   string[] icons={"home","quest","focus","star","shop","pet","options"};
   for(int i=0;i<pages.Length;i++){string target=pages[i];var b=(PixelButton)Theme.Button(names[i],16,171+i* 50,180,42,(s,e)=>Navigate(target));b.IconId=icons[i];sidebar.Controls.Add(b);nav[target]=b;}
   sidebar.Controls.Add(Theme.Label("SAVE FILE  01",20,546,176,24,10,Theme.Muted));sidebar.Controls.Add(Theme.Label("일정 완료  +20 G\n집중 1분    +1 G",20,576,178, 40,10,Theme.Muted));
   wallet=Theme.Label("",244, 70,752,36,11);Controls.Add(wallet);
   content=new Panel{Location=new Point(244,119),Size=new Size(752,588),BackColor=Theme.Bg,AutoScroll=true};Controls.Add(content);
   Controls.Add(Theme.Label("이동: 펫 드래그  /  메뉴: 더블클릭  /  창을 닫아도 펫은 남아있어요.",22,714,974,22,10,Theme.Muted));
   RefreshPage();live.Tick+=(s,e)=>{if(countdown!=null && !countdown.IsDisposed){var remaining=Store.State.FocusEnd-AppClock.Now;countdown.Text=Store.State.FocusEnd==DateTime.MinValue?"READY":String.Format("{0:00}:{1:00}",Math.Max(0,(int)remaining.TotalMinutes),Math.Max(0,remaining.Seconds));}};live.Start();
   FormClosed+=(s,e)=>{live.Dispose();};
  }
  
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(Theme.Line,4))e.Graphics.DrawRectangle(p,2,2,Width-4,Height-4);}
  void Add(Control c){content.Controls.Add(c);}
  Label L(string text,int x,int y,int w,int h,float size=10,Color? color=null){var c=Theme.Label(text,x,y,w,h,size,color);Add(c);return c;}
  Button B(string text,int x,int y,int w,int h,EventHandler action,bool primary=false){var c=Theme.Button(text,x,y,w,h,action,primary);Add(c);return c;}
  PixelPanel Card(int x,int y,int w,int h,Color? color=null){var p=new PixelPanel{Location=new Point(x,y),Size=new Size(w,h),BackColor=color??Theme.Cream};Add(p);return p;}
  void Title(string english,string title,string hint){L(english,0,0,744,22,10,Theme.Muted);L(title,0,27,744, 40,22);L(hint,0,74,744,26,10,Theme.Muted);}
  public void Navigate(string target){page=target;RefreshPage();}
  public void RefreshPage(){
   
   foreach(Control c in content.Controls.Cast<Control>().ToArray())c.Dispose();content.Controls.Clear();countdown=null;
   int xp=Store.State.Completed*20+Store.State.FocusMinutes;wallet.Text="Lv."+(1+xp/100)+"  "+Store.State.PetName+"                   GOLD  "+Store.State.Coins.ToString("0000")+" G";
   foreach(var pair in nav){pair.Value.BackColor=pair.Key==page?Theme.Gold:Theme.Cream;pair.Value.Invalidate();}
   foreach(Control c in sidebar.Controls)if(c is PetPortrait)c.Invalidate();
   if(page=="나의 방")Home();else if(page=="오늘의 업무")Tasks();else if(page=="집중 스튜디오")FocusPage();else if(page=="미니게임")Game();else if(page=="코인 상점")Shop();else if(page=="나의 캐릭터")Character();else Settings();
  }
  void Home(){
   Title("HOME / SAVE FILE 01",Store.State.PetName+"의 작은 방","오늘의 일도, 잠깐의 휴식도. 여기서 함께 시작해요.");
   var scene=new RoomScene{Location=new Point(0,112),Size=new Size(744,291)};scene.Click+=(s,e)=>pet.Say("쓰다듬어줘서 고마워! 오늘도 함께하자.",false);Add(scene);
   int xp=Store.State.Completed*20+Store.State.FocusMinutes;int next=100-xp%100;
   var hud=Card(0,419,744,64);hud.Controls.Add(Theme.Label("NEXT LEVEL",18,12,135, 20,10,Theme.Muted));
   hud.Controls.Add(Theme.Label("EXP  "+(xp%100)+" / 100",18, 33,160,20));
   var bar=new Panel{Location=new Point(179, 20),Size=new Size(363,22),BackColor=Theme.Light};hud.Controls.Add(bar);
   bar.Controls.Add(new Panel{Location=new Point(3,3),Size=new Size(Math.Max(1,(int)(357*(xp%100)/100.0)),16),BackColor=Theme.Purple});
   hud.Controls.Add(Theme.Label("남은 퀘스트  "+Store.State.Tasks.Count(t=>!t.Done),558, 20,170,26));
   B("퀘스트 받으러 가기",0,503,260,48,(s,e)=>Navigate("오늘의 업무"),true);
   B("쓰다듬기",276,503,218,48,(s,e)=>pet.Say("헤헤. 네가 있어서 좋아!",false));
   B("상점 구경하기",510,503,234,48,(s,e)=>Navigate("코인 상점"));
   L("방 안의 친구를 클릭해서 인사해보세요.",0,563,744,22,10,Theme.Muted);
  }
  
  void EditSchedule(TaskItem item,string preset){using(var editor=new ScheduleEditor(item,preset)){if(editor.ShowDialog(this)==DialogResult.OK)RefreshPage();}}
  void Tasks(){
   Title("QUEST BOARD","일정과 퀘스트","한국시간 · "+AppClock.Now.ToString("MM/dd (ddd) HH:mm")+" · 완료하면 20 G");
   B("+ 일정 추가",0,111,234,44,(s,e)=>EditSchedule(null,""),true);
   B("출근",250,111,150,44,(s,e)=>EditSchedule(null,"출근"));
   B("퇴근",416,111,150,44,(s,e)=>EditSchedule(null,"퇴근"));
   B("휴가",582,111,162,44,(s,e)=>EditSchedule(null,"휴가"));
   L("달력으로 날짜 선택 · 시간 직접 입력 · 반복 · 전날 알림 · 일정별 모습",0,167,744,30,10,Theme.Muted);
   var board=Card(0,207,744,280);
   taskList=new ListBox{Location=new Point(12,12),Size=new Size(720,256),BorderStyle=BorderStyle.None,Font=Theme.Font(10),DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=64,BackColor=Theme.Cream};
   foreach(var t in Store.State.Tasks.OrderBy(t=>t.Done).ThenBy(t=>Scheduler.Due(t)))taskList.Items.Add(t);
   taskList.DrawItem+=(s,e)=>{if(e.Index<0)return;var t=(TaskItem)taskList.Items[e.Index];Theme.Fill(e.Graphics,(e.State&DrawItemState.Selected)!=0?Color.FromArgb(243,218,164):Theme.Cream,e.Bounds.X,e.Bounds.Y,e.Bounds.Width,e.Bounds.Height);
    PixelIcons.Draw(e.Graphics,t.Done?"star":"quest",new Point(10,e.Bounds.Y+16),2);
    Theme.Text(e.Graphics,t.Title,taskList.Font,t.Done?Theme.Muted:Theme.Ink,new Rectangle(42,e.Bounds.Y+3,560,24));
    Theme.Text(e.Graphics,Scheduler.Due(t).ToString("MM/dd (ddd) HH:mm")+" · "+t.Category+" · "+Scheduler.RepeatLabel(t),taskList.Font,Theme.Muted,new Rectangle(42,e.Bounds.Y+26,650,20));
    Theme.Text(e.Graphics,(t.AllDay?"종일  ":"")+(t.EveReminder?"전날 "+t.EveTime+" 알림  ":"")+(!String.IsNullOrEmpty(t.ScheduleImage)?"모습 변경":""),Theme.Font(8),Theme.Purple,new Rectangle(42,e.Bounds.Y+45,620,18));
    Theme.Text(e.Graphics,t.Done?"CLEAR":"+20 G",taskList.Font,t.Done?Theme.Purple:Theme.Ink,new Rectangle(590,e.Bounds.Y+3,110,24),ContentAlignment.MiddleRight);
    Theme.Fill(e.Graphics,Theme.Light,4,e.Bounds.Bottom-2,e.Bounds.Width-8,1);
   };board.Controls.Add(taskList);
   taskList.DoubleClick+=(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t!=null)EditSchedule(t,"");};
   if(taskList.Items.Count==0)board.Controls.Add(Theme.Label("첫 일정을 등록해보세요!",28,80,680,50,14,Theme.Muted));
   B("완료 +20 코인",0,503,234,42,(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t==null)return;if(Rules.Complete(t,Store.State)){Store.Save();pet.Say("QUEST CLEAR! +20 G",false);RefreshPage();}else MessageBox.Show("이미 완료했거나 다음 날짜의 반복 일정이에요.");},true);
   B("일정 수정",250,503,234,42,(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t!=null)EditSchedule(t,"");});
   B("10분 뒤 알림",510,503,234,42,(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t==null||t.Done)return;Scheduler.Snooze(t,AppClock.Now);Store.Save();RefreshPage();});
   B("이번 반복 건너뛰기",0,551,360,32,(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t!=null&&Scheduler.Skip(t,AppClock.Now)){Store.Save();RefreshPage();}});
   B("일정 삭제",384,551,360,32,(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t!=null&&MessageBox.Show(t.Repeat=="한 번"?"일정을 삭제할까요?":"반복 일정 전체를 삭제할까요?","일정 삭제",MessageBoxButtons.YesNo)==DialogResult.Yes){Store.State.Tasks.Remove(t);Store.Save();RefreshPage();}});
  }

  void FocusPage(){
   Title("FOCUS ADVENTURE","집중 모험","집중한 시간이 경험치와 코인이 돼요. 1분 = 1 G");
   Add(new RoomScene{Location=new Point(0,111),Size=new Size(744,235),Night=true});
   var timer=Card(0,361,744,80);countdown=Theme.Label("READY",16,14,320,52,22,Theme.Purple);timer.Controls.Add(countdown);
   timer.Controls.Add(Theme.Label("집중 시간",360,24,140, 28));
   var minutes=new NumericUpDown{Location=new Point(520,24),Size=new Size(193, 30),Minimum=1,Maximum=120,Value=25,BackColor=Theme.Cream,ForeColor=Theme.Ink,Font=Theme.Font(10)};timer.Controls.Add(minutes);
   B("집중 시작",0,459,360, 50,(s,e)=>{if(Store.State.FocusEnd!=DateTime.MinValue){MessageBox.Show("이미 집중 중이에요.");return;}Store.State.SessionMinutes=(int)minutes.Value;Store.State.FocusEnd=AppClock.Now.AddMinutes((int)minutes.Value);Store.Save();pet.Say("집중 모험 시작! 함께 힘내보자.",false);RefreshPage();},true);
   B("집중 취소",376,459,368, 50,(s,e)=>{if(Store.State.FocusEnd==DateTime.MinValue)return;if(MessageBox.Show("모험을 취소할까요? 완료 보상은 지급되지 않아요.","집중 취소",MessageBoxButtons.YesNo)==DialogResult.Yes){Store.State.FocusEnd=DateTime.MinValue;Store.State.SessionMinutes=0;Store.Save();RefreshPage();}});
   L("완료한 모험 "+Store.State.FocusCount+"회  /  누적 집중 "+Store.State.FocusMinutes+"분",0,535,744, 30,10,Theme.Muted);
  }

  void Game(){
   Title("PET VOLLEY / DESKTOP MATCH","바탕화면 배구","내 캐릭터로 컴퓨터 친구와 대결해요. 먼저 5점을 따면 승리!");
   Add(new VolleyballPreview(pet){Location=new Point(0,111),Size=new Size(744,310)});
   L("← → 또는 A/D 이동   SPACE 점프   ↓ 또는 X 스매시",0,437,744,28);
   L("ESC 일시정지 / 다른 창 전환 시 자동 정지 / R 다시 한 판",0,468,744,28,10,Theme.Muted);
   B("바탕화면 배구 시작",0,507,744,48,(s,e)=>pet.StartVolleyball(),true);
   int played=Store.State.GamesDate==AppClock.Now.ToString("yyyy-MM-dd")?Store.State.GamesPlayed:0;
   L("승리 +20 G / 패배 +5 G / 오늘 보상 "+played+" / 3 · 게임은 계속 가능해요.",0,567,744,20,10,Theme.Muted);
  }

  void Shop(){
   Title("ITEM SHOP","친구를 위한 작은 선물","미리보기에서 스타일을 고르고 코인으로 구매해요.");
   string[] names={"기본","민트 리본","별빛 모자","하트 친구","왕관"};int[] prices={0,40,70,100,150};
   for(int i=0;i<names.Length;i++){string name=names[i];int price=prices[i];bool owned=Store.State.Owned.Contains(name);bool equipped=Store.State.Equipped==name;int x=(i%3)*254,y=112+(i/3)*224;
    var card=Card(x,y,236,207,equipped?Color.FromArgb(237,226,187):Theme.Cream);
    card.Controls.Add(new ItemPortrait{Item=name,Location=new Point(12,12),Size=new Size(212,90)});
    var label=Theme.Label(name,8,108,220, 28,12);label.TextAlign=ContentAlignment.MiddleCenter;card.Controls.Add(label);
    var priceLabel=Theme.Label(equipped?"EQUIPPED":owned?"보유 중":price+" G",8,135,220,22,10,Theme.Muted);priceLabel.TextAlign=ContentAlignment.MiddleCenter;card.Controls.Add(priceLabel);
    card.Controls.Add(Theme.Button(owned?"장착하기":"구매 + 장착",12,164,212, 33,(s,e)=>{
     if(!Rules.Buy(name,price,Store.State)){MessageBox.Show("코인이 부족해요. 퀘스트나 집중 모험을 완료해보세요.");return;}
     Store.Save();pet.Invalidate();pet.Say("새 옷! 마음에 들어.",false);RefreshPage();
    },!owned));
   }
   var note=Card(508,336,236,207,Color.FromArgb(233,218,185));note.Controls.Add(Theme.Label("HOW TO EARN\n\n퀘스트   +20 G\n집중 1분  +1 G\n배구 승리 +20 G",18,18,204,174,10,Theme.Muted));
  }
  void Character(){
   Title("CHARACTER / CUSTOMIZE","나만의 친구","좋아하는 캐릭터 이미지로 교체하고 이름을 지어줘요.");
   Add(new RoomScene{Location=new Point(0,111),Size=new Size(744,235)});
   var form=Card(0,365,744, 70);
   form.Controls.Add(Theme.Label("이름",16, 20,80, 28));
   var name=Theme.Input(Store.State.PetName,98, 20,426);name.MaxLength=24;form.Controls.Add(name);
   form.Controls.Add(Theme.Button("이름 저장",540,14,188,42,(s,e)=>{if(String.IsNullOrWhiteSpace(name.Text))return;Store.State.PetName=name.Text.Trim();Store.Save();RefreshPage();},true));
   B("캐릭터 이미지 불러오기",0,451,364, 46,(s,e)=>Import(),true);
   B("기본 스누피로 돌아가기",380,451,364, 46,(s,e)=>{Store.State.ImagePath="";Store.State.PetName="스누피";PetArt.LoadCustom("");Store.Save();pet.Invalidate();RefreshPage();});

   var sizing=Card(0,510,744,70);sizing.Controls.Add(Theme.Label("바탕화면 크기",16,20,175,30));
   var slider=new PixelSizeSlider{Location=new Point(197,17),Size=new Size(315,36),Value=Store.State.PetSize};
   var sizeLabel=Theme.Label(Store.State.PetSize+" px",525,20,90,30);sizing.Controls.Add(sizeLabel);sizing.Controls.Add(slider);
   slider.ValueChanged+=(s,e)=>{Store.State.PetSize=slider.Value;pet.ApplyPetSize();sizeLabel.Text=slider.Value+" px";Store.Save();};
   sizing.Controls.Add(Theme.Button("기본",629,16,98,38,(s,e)=>slider.Value=140));

  }
  void Import(){
   using(var dialog=new OpenFileDialog{Filter="캐릭터 이미지|*.png;*.jpg;*.jpeg;*.bmp",Title="캐릭터 선택"}){
    if(dialog.ShowDialog()!=DialogResult.OK)return;
    try{if(new FileInfo(dialog.FileName).Length>10*1024*1024)throw new Exception("10MB 이하 이미지를 사용해주세요.");
     using(var im=Image.FromFile(dialog.FileName)){if(im.Width>4096||im.Height>4096)throw new Exception("4096px 이하 이미지를 사용해주세요.");
      Directory.CreateDirectory(Store.Root);string target=Path.Combine(Store.Root,"character-"+Guid.NewGuid().ToString("N")+".png");
      using(var b=new Bitmap(im))b.Save(target,System.Drawing.Imaging.ImageFormat.Png);if(!PetArt.LoadCustom(target))throw new Exception("이미지를 불러오지 못했습니다.");
      Store.State.ImagePath=target;Store.Save();pet.Invalidate();RefreshPage();
     }
    }catch(Exception e){MessageBox.Show(e.Message,"캐릭터 선택");}
   }
  }
  void Option(int y,string title,string hint,bool value,EventHandler changed){

   var row=Card(0,y,744,88);row.Controls.Add(Theme.Label(title, 20,14,540, 30,13));
   row.Controls.Add(Theme.Label(hint, 20, 47,540,24,10,Theme.Muted));
   var toggle=new PixelToggle{Location=new Point(614,26),Checked=value,Text=title,AccessibleName=title};toggle.CheckedChanged+=changed;row.Controls.Add(toggle);
  }
  
  void Settings(){
   Title("OPTIONS","모험 설정","친구가 함께하는 방식을 바꿔요.");
   Option(112,"자유 산책","친구가 바탕화면을 천천히 돌아다녀요.",Store.State.Wander,(s,e)=>{Store.State.Wander=((CheckBox)s).Checked;Store.Save();});
   Option(216,"조용한 모드","Windows 알림과 정기 응원을 쉬어요.",Store.State.Quiet,(s,e)=>{Store.State.Quiet=((CheckBox)s).Checked;Store.Save();});
   var save=Card(0,320,744,86);save.Controls.Add(Theme.Label("SAVE FILE 01", 20,14,430,26,13));save.Controls.Add(Theme.Label("일정과 아이템은 이 PC에 자동 저장돼요.", 20, 46,480,24,10,Theme.Muted));
   save.Controls.Add(Theme.Button("내 데이터 백업",530, 20,196, 46,(s,e)=>{
    using(var dialog=new SaveFileDialog{Filter="JSON 백업|*.json",FileName="DeskBuddy-backup-"+AppClock.Now.ToString("yyyyMMdd")+".json"})
    if(dialog.ShowDialog()==DialogResult.OK){try{File.WriteAllText(dialog.FileName,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(Store.State),System.Text.Encoding.UTF8);MessageBox.Show("저장 파일을 백업했어요. 캐릭터 이미지는 별도로 보관해주세요.");}catch(Exception ex){MessageBox.Show("백업 실패: "+ex.Message);}}
   }));
   B("말풍선 알림 테스트",0,424,360, 46,(s,e)=>pet.Say("회의 준비할 시간! 자료 챙겼어?",true),true);
   B("DeskBuddy 완전 종료",376,424,368, 46,(s,e)=>pet.Quit());
   var note=Card(0,488,744,88,Color.FromArgb(233,222,195));note.Controls.Add(Theme.Label("TIP / 창을 닫아도 바탕화면 펫은 남아요.\n완전 종료는 위 버튼으로! 알림은 앱 실행 중에 동작해요.",18, 16,704, 60,10,Theme.Muted));
  }
  
 }
}
