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
  public static Font PixelFont(float pixels){return new Font(fonts.Families.Length>0?fonts.Families[0]:new FontFamily("굴림체"),pixels,FontStyle.Regular,GraphicsUnit.Pixel);}
  public static Font BubbleFont(Graphics g,string text,Rectangle bounds,int scale){float pixels=16*scale/100f;while(true){var font=PixelFont(pixels);if(g.MeasureString(text,font,bounds.Width).Height<=bounds.Height||pixels<=6)return font;font.Dispose();pixels=Math.Max(6,pixels-0.5f);}}
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
  public Data Decor;public bool ShowPet=true;public bool Animated{get{return clock.Enabled;}set{clock.Enabled=value;}}
  Timer clock=new Timer{Interval=180};int frame;public bool Night;
  public RoomScene(){DoubleBuffered=true;clock.Tick+=(s,e)=>{frame++;if(Visible)Invalidate();};clock.Start();Cursor=Cursors.Hand;}
  protected override void Dispose(bool disposing){if(disposing)clock.Dispose();base.Dispose(disposing);}
  static void R(Graphics g,string hex,int x,int y,int w,int h){Theme.Fill(g,ColorTranslator.FromHtml(hex),x,y,w,h);}
  protected override void OnPaint(PaintEventArgs e){
   using(var b=new Bitmap(384,150))using(var g=Graphics.FromImage(b)){
    var decor=Decor??Store.State;string wallpaper=Decor==null&&Companions.Level(Store.State)<Progression.Required(decor.RoomWallpaper)?"기본 벽지":decor.RoomWallpaper;string wall=wallpaper=="축제 벽지"?"#D9EAE0":wallpaper=="왕실 벽지"?"#A99AC5":decor.RoomWallpaper=="민트 숲 벽지"?"#D0DFCA":wallpaper=="복숭아 벽지"?"#F3D3D1":wallpaper=="보랏빛 밤 벽지"?"#9E9ABA":"#F4D9AE";
    R(g,wall,0,0,384,102);R(g,wallpaper=="보랏빛 밤 벽지"?"#9185A2":"#C69770",0,102,384,48);R(g,"#775240",0,100,384,3);
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
    LivingArt.Room(g,decor);if(wallpaper=="축제 벽지"){for(int i=0;i<7;i++){R(g,i%2==0?"#E89BB0":"#EAC96B",124+i*21,12+i%2*8,9,12);R(g,"#7A937F",128+i*21,24+i%2*8,1,12);}}if(wallpaper=="왕실 벽지"){R(g,"#D9BA75",130,3,5,85);R(g,"#D9BA75",257,3,5,85);R(g,"#6D5287",137,4,118,15);R(g,"#F0D29B",184,5,24,10);}if(ShowPet)Progression.Sparkles(g,new Rectangle(125,60,140,80),frame,false);
    var fed=AppClock.Decode(decor.LastFedLocal,DateTime.MinValue);if(ShowPet&&fed!=DateTime.MinValue&&AppClock.Now>=fed&&AppClock.Now<fed.AddSeconds(5))LivingArt.Food(g,decor.LastFood,new Rectangle(208,99,34,34));
    if(ShowPet)Companions.Draw(g,new Rectangle(221,132-Companions.SizeFor(74),Companions.SizeFor(74),Companions.SizeFor(74)),Store.State,frame);
    if(ShowPet&&!PetArt.HasCustom)PetArt.Draw(g,new Rectangle(139,58+(frame%2),74,74),frame,false,Progression.Accessory(Store.State));
    if(frame%12<6){R(g,"#D98980",217,80,2,2);R(g,"#D98980",215,82,6,2);R(g,"#D98980",217,84,2,2);}
    e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(b,ClientRectangle);
    if(ShowPet&&PetArt.HasCustom)PetArt.Draw(e.Graphics,new Rectangle((int)(139*Width/384.0),(int)((58+frame%2)*Height/150.0),(int)(74*Width/384.0),(int)(74*Height/150.0)),frame,false,Progression.Accessory(Store.State));
   }
  }
  
 }
 public class PetPortrait:Control {
  public string Item;public PetPortrait(){DoubleBuffered=true;BackColor=Theme.Cream;}
  protected override void OnPaint(PaintEventArgs e){Theme.Frame(e.Graphics,ClientRectangle,BackColor);PetArt.Draw(e.Graphics,new Rectangle((Width-100)/2,(Height-100)/2,100,100),0,false,Item??Progression.Accessory(Store.State));}
 }
 public class ItemPortrait:Control {
  public string Item;
  protected override void OnPaint(PaintEventArgs e){PetArt.Draw(e.Graphics,new Rectangle((Width-90)/2,0,90,90),0,false,Item);}
 }
 public class FoodPortrait:Control {public string Food;public FoodPortrait(){DoubleBuffered=true;}protected override void OnPaint(PaintEventArgs e){LivingArt.Food(e.Graphics,Food,new Rectangle((Width-84)/2,0,84,84));}}

 public class PixelSizeSlider:Control {
  public int Minimum=40,Maximum=320;int value=140;public event EventHandler ValueChanged;
  public int Value{get{return value;}set{int next=Math.Max(Minimum,Math.Min(Maximum,value));if(next==this.value)return;this.value=next;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
  public PixelSizeSlider(){DoubleBuffered=true;TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.Slider;Size=new Size(300,36);}
  void Pick(int x){Value=Minimum+(int)Math.Round(Math.Max(0,Math.Min(1,(x-12)/(double)Math.Max(1,Width-24)))*(Maximum-Minimum));}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();Capture=true;Pick(e.X);}}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture&&e.Button==MouseButtons.Left)Pick(e.X);}
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);Capture=false;}
  protected override bool IsInputKey(Keys k){return k==Keys.Left||k==Keys.Right||base.IsInputKey(k);}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Left)Value-=5;if(e.KeyCode==Keys.Right)Value+=5;if(e.KeyCode==Keys.Home)Value=Minimum;if(e.KeyCode==Keys.End)Value=Maximum;}
  protected override void OnPaint(PaintEventArgs e){
   Theme.Frame(e.Graphics,new Rectangle(4,12,Width-8,12),Theme.Light);
   int x=12+(int)((Width-24)*(value-Minimum)/(double)(Maximum-Minimum));Theme.Fill(e.Graphics,Theme.Purple,8,16,Math.Max(1,x-8),4);
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
  PetForm pet;Panel content,sidebar;Label wallet,countdown,friend;Timer live=new Timer{Interval=1000};
  DateTime questDate=DateTime.MinValue;string page="나의 방",shopTab="캐릭터";ListBox taskList;Dictionary<string,Button> nav=new Dictionary<string,Button>();Point drag;
  public MainForm(PetForm owner){
   pet=owner;Icon=AppIdentity.Icon;Text="DeskBuddy · 작은 업무 모험";AutoScaleMode=AutoScaleMode.None;ClientSize=new Size(1024,744);
   StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.None;BackColor=Theme.Bg;Font=Theme.Font(10);DoubleBuffered=true;
   var header=new PixelPanel{Location=new Point(0,0),Size=new Size(1024,56),BackColor=Theme.Peach};Controls.Add(header);
   var brand=Theme.Label("DESKBUDDY  /  작은 업무 모험",20,9,690, 30,13);header.Controls.Add(brand);
   MouseEventHandler down=(s,e)=>{if(e.Button==MouseButtons.Left)drag=PointToClient(Cursor.Position);};MouseEventHandler move=(s,e)=>{if(e.Button==MouseButtons.Left){Point now=PointToClient(Cursor.Position);Location=new Point(Left+now.X-drag.X,Top+now.Y-drag.Y);}};
   header.MouseDown+=down;header.MouseMove+=move;brand.MouseDown+=down;brand.MouseMove+=move;
   header.Controls.Add(Theme.Button("_",923,9,38,38,(s,e)=>WindowState=FormWindowState.Minimized));
   header.Controls.Add(Theme.Button("X",969,9,38,38,(s,e)=>Close()));
   sidebar=new PixelPanel{Location=new Point(12, 70),Size=new Size(212,636),BackColor=Color.FromArgb(237,222,190)};Controls.Add(sidebar);
   sidebar.Controls.Add(new PetPortrait{Location=new Point(16,14),Size=new Size(180,104),BackColor=Theme.Cream});
   friend=Theme.Label(Store.State.PetName,16,124,180,26,11);friend.TextAlign=ContentAlignment.MiddleCenter;sidebar.Controls.Add(friend);
   string[] pages={"나의 방","오늘의 업무","메모관리","집중 스튜디오","미니게임","코인 상점","나의 캐릭터","펫 키우기","설정 / 소개"};
   string[] names={"나의 방","퀘스트 보드","메모관리","집중 모험","미니게임","아이템 상점","캐릭터","펫 키우기","옵션"};
   string[] icons={"home","quest","quest","focus","star","shop","pet","pet","options"};
   for(int i=0;i<pages.Length;i++){string target=pages[i];var b=(PixelButton)Theme.Button(names[i],16,171+i*42,180,39,(s,e)=>{if(target=="메모관리"){using(var memos=new StickyMemoForm(pet))memos.ShowDialog(this);RefreshPage();}else Navigate(target);});b.IconId=icons[i];sidebar.Controls.Add(b);nav[target]=b;}
   sidebar.Controls.Add(Theme.Label("SAVE FILE  01",20,558,176,24,10,Theme.Muted));sidebar.Controls.Add(Theme.Label("일정 완료  +20 G\n집중 1분    +1 G",20,588,178, 40,10,Theme.Muted));
   wallet=Theme.Label("",244, 70,752,36,11);Controls.Add(wallet);
   content=new Panel{Location=new Point(244,119),Size=new Size(752,588),BackColor=Theme.Bg,AutoScroll=true};Controls.Add(content);
   Controls.Add(Theme.Label("이동: 펫 드래그  /  메뉴: 더블클릭  /  창을 닫아도 펫은 남아있어요.",22,714,974,22,10,Theme.Muted));
   RefreshPage();live.Tick+=(s,e)=>{if(page=="오늘의 업무"&&questDate!=AppClock.Now.Date)RefreshPage();if(countdown!=null && !countdown.IsDisposed){var remaining=Store.State.FocusEnd-AppClock.Now;countdown.Text=FocusTimerArt.Remaining(Store.State,AppClock.Now);}};live.Start();
   FormClosed+=(s,e)=>{live.Dispose();};
  }
  
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(Theme.Line,4))e.Graphics.DrawRectangle(p,2,2,Width-4,Height-4);}
  void Add(Control c){content.Controls.Add(c);}
  Label L(string text,int x,int y,int w,int h,float size=10,Color? color=null){var c=Theme.Label(text,x,y,w,h,size,color);Add(c);return c;}
  Button B(string text,int x,int y,int w,int h,EventHandler action,bool primary=false){var c=Theme.Button(text,x,y,w,h,action,primary);Add(c);return c;}
  PixelPanel Card(int x,int y,int w,int h,Color? color=null){var p=new PixelPanel{Location=new Point(x,y),Size=new Size(w,h),BackColor=color??Theme.Cream};Add(p);return p;}
  void Title(string english,string title,string hint){L(english,0,0,720,22,10,Theme.Muted);L(title,0,27,720,40,22);L(hint,0,74,720,26,10,Theme.Muted);}
  public void Navigate(string target){page=target;RefreshPage();}
  public void RefreshPage(){
   content.AutoScrollPosition=Point.Empty;
   friend.Text=Store.State.PetName;
   
   foreach(Control c in content.Controls.Cast<Control>().ToArray())c.Dispose();content.Controls.Clear();countdown=null;
   int xp=Companions.Experience(Store.State);wallet.Text="Lv."+(1+xp/100)+"  "+Store.State.PetName+(Companions.Level(Store.State)>=10?" · 전설의 친구":"")+"      GOLD "+Store.State.Coins.ToString("0000")+" G";
   foreach(var pair in nav){pair.Value.BackColor=pair.Key==page?Theme.Gold:Theme.Cream;pair.Value.Invalidate();}
   foreach(Control c in sidebar.Controls)if(c is PetPortrait)c.Invalidate();
   if(page=="나의 방")Home();else if(page=="오늘의 업무")Tasks();else if(page=="집중 스튜디오")FocusPage();else if(page=="미니게임")Game();else if(page=="코인 상점")Shop();else if(page=="나의 캐릭터")Character();else if(page=="펫 키우기"){Title("LITTLE PET / FROM AN EGG","우리 친구의 작은 펫","레벨 2부터 알을 입양해 함께 돌봐주세요.");CompanionPanel();}else Settings();
  }
  void Home(){
   Title("HOME / SAVE FILE 01",Store.State.PetName+"의 작은 방","쓰다듬기·놀기 +5 EXP · 교감 경험치는 30초에 한 번!");
   var scene=new RoomScene{Location=new Point(0,112),Size=new Size(720,223)};scene.Click+=(s,e)=>pet.Pat();Add(scene);
   var care=Card(0,351,720,79);care.Controls.Add(Theme.Label("포만감 "+Store.State.Fullness+" / 100   기분 "+Store.State.Happiness+" / 100",16,12,350,26,10));
   care.Controls.Add(Theme.Label(Store.State.Fullness<30?"꼬르륵~ 밥 먹고 싶어!":"맛있는 것 먹고 같이 쉬자~",16,44,350,24,10,Theme.Muted));
   care.Controls.Add(Theme.Button("밥 주기 12 G",371,20,158,40,(s,e)=>{pet.Feed("든든한 밥");RefreshPage();},true));
   care.Controls.Add(Theme.Button("간식 주기 7 G",541,20,164,40,(s,e)=>{pet.Feed("딸기 간식");RefreshPage();}));
   int xp=Companions.Experience(Store.State);int next=100-xp%100;
   var hud=Card(0,442,720,64);hud.Controls.Add(Theme.Label("NEXT LEVEL",18,12,135, 20,10,Theme.Muted));
   hud.Controls.Add(Theme.Label("EXP  "+(xp%100)+" / 100",18, 33,160,20));
   var bar=new Panel{Location=new Point(179, 20),Size=new Size(363,22),BackColor=Theme.Light};hud.Controls.Add(bar);
   bar.Controls.Add(new Panel{Location=new Point(3,3),Size=new Size(Math.Max(1,(int)(357*(xp%100)/100.0)),16),BackColor=Theme.Purple});
   hud.Controls.Add(Theme.Label("남은 퀘스트  "+Store.State.Tasks.Count(t=>!t.Done),552,20,160,26));
   B("퀘스트 보드",0,522,174,42,(s,e)=>Navigate("오늘의 업무"),true);
   B("쓰다듬기",186,522,104,42,(s,e)=>pet.Pat());
   B("옆돌기",302,522,104,42,(s,e)=>pet.Cartwheel());
   B("방 꾸미기",418,522,146,42,(s,e)=>{shopTab="방";Navigate("코인 상점");});
   B("상점 구경",576,522,144,42,(s,e)=>Navigate("코인 상점"));
   B("작은 펫 키우기",0,580,720,42,(s,e)=>Navigate("펫 키우기"));
   var dance=B("댄스 · Lv.3",0,640,232,42,(s,e)=>pet.SpecialAction("댄스"));dance.Enabled=Companions.Level(Store.State)>=3;var flip=B("공중제비 · Lv.3",244,640,232,42,(s,e)=>pet.SpecialAction("공중제비"));flip.Enabled=dance.Enabled;var together=B("함께 놀기 · Lv.5",488,640,232,42,(s,e)=>pet.SpecialAction("함께 놀기"));together.Enabled=Companions.Level(Store.State)>=5&&Companions.Current(Store.State)!=null;
   L("레벨 2 펫 · 3 액션 · 4 특별 상점 · 5 함께 놀기 · 7 별빛 · 10 왕실",0,701,720,28,9,Theme.Muted);
  }
  
  void CompanionPanel(){var little=Companions.Current(Store.State);var card=Card(0,112,720,151);card.Controls.Add(new CompanionPortrait{Location=new Point(16,18),Size=new Size(100,100)});card.Controls.Add(Theme.Label("작은 펫 · "+(Companions.Level(Store.State)<2?"레벨 2부터 이용 가능":Companions.Stage(little)),130,16,560,28,12));card.Controls.Add(Theme.Label(little==null?"레벨 2부터 이용 가능해요. 기존 성장 기록은 보관돼요.":little.Growth>=10?"성장 완료! 친밀도 "+little.Bond+" · 계속 함께 놀아요.":"돌봄 "+little.Growth+" / 10 · 3회 부화, 10회 성장 완료",130,50,560,25,10,Theme.Muted));
   if(little==null){var adopt=Theme.Button("알 입양하기",130,90,250,40,(s,e)=>CompanionAction(0),true);adopt.Enabled=Companions.Level(Store.State)>=2;card.Controls.Add(adopt);}else{card.Controls.Add(Theme.Button(little.Growth<3?"알 보살피기 5 G":"펫 놀아주기 5 G",130,90,250,40,(s,e)=>CompanionAction(1),true));card.Controls.Add(Theme.Button(little.Growth<3?"영양 주기 8 G":"펫 밥 주기 8 G",396,90,250,40,(s,e)=>CompanionAction(2)));}
   L("각 캐릭터의 펫은 따로 자라요. 돌봄은 1분마다 가능해요.",0,280,720,28,10,Theme.Muted);var guide=Card(0,329,720,147);guide.Controls.Add(Theme.Label("알 → 아기 펫 → 다 자란 펫",20,16,680,30,13));guide.Controls.Add(Theme.Label("돌봄 / 놀아주기 5 G · 영양 / 밥 주기 8 G",20,55,680,26,10));guide.Controls.Add(Theme.Label("찌오의 펫은 반짝이는 둥근 아기 새로 자라요.",20,91,680,26,10,Theme.Muted)); }
  void CompanionAction(int action){string snapshot=new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(Store.State),error;bool ok=action==0?Companions.Adopt(Store.State,out error):Companions.Care(Store.State,action==2,AppClock.Now,out error);if(!ok){GameAlert.Show(error,"작은 펫 키우기");return;}if(!Store.Save()){Store.State=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Data>(snapshot);return;}pet.ApplyPetSize();pet.Say(action==0?"새 알이 생겼어! 함께 키워보자~":"작은 펫이 무럭무럭 자라고 있어!",false);RefreshPage();}
  void EditSchedule(TaskItem item,string preset){if(GoogleCalendar.Imported(item)){GameAlert.Show("구글에서 가져온 일정은 구글 캘린더에서 수정해주세요.");return;}using(var editor=new ScheduleEditor(item,preset)){if(editor.ShowDialog(this)==DialogResult.OK)RefreshPage();}}
  void Tasks(){ questDate=AppClock.Now.Date;
   Title("QUEST BOARD","오늘의 일정과 퀘스트","한국시간 · "+AppClock.Now.ToString("MM/dd (ddd) HH:mm")+" · 일정 +20 G / 야근 -20 G");
   B("+ 일정 추가",0,111,190,44,(s,e)=>EditSchedule(null,""),true);
   B("출근",202,111,110,44,(s,e)=>EditSchedule(null,"출근"));
   B("퇴근",324,111,110,44,(s,e)=>EditSchedule(null,"퇴근"));
   B("휴가",446,111,110,44,(s,e)=>EditSchedule(null,"휴가"));
   B("야근",568,111,152,44,(s,e)=>EditSchedule(null,"야근"));
   B("달력 보기",0,165,170,34,(s,e)=>{using(var calendar=new ScheduleCalendarForm(pet))calendar.ShowDialog(this);RefreshPage();},true);
   L("날짜별 일정 · 반복 예정 · Google 일정도 함께",184,167,550,30,10,Theme.Muted);
   var board=Card(0,207,744,280);
   taskList=new ListBox{Location=new Point(12,12),Size=new Size(720,256),BorderStyle=BorderStyle.None,Font=Theme.Font(10),DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=64,BackColor=Theme.Cream};
   foreach(var t in Store.State.Tasks.Where(t=>Scheduler.Due(t)<questDate.AddDays(1)&&(Scheduler.Due(t).Date==questDate||Scheduler.End(t)>questDate)).OrderBy(t=>t.Done).ThenBy(t=>Scheduler.Due(t)))taskList.Items.Add(t);
   taskList.DrawItem+=(s,e)=>{if(e.Index<0)return;var t=(TaskItem)taskList.Items[e.Index];Theme.Fill(e.Graphics,(e.State&DrawItemState.Selected)!=0?Color.FromArgb(243,218,164):Theme.Cream,e.Bounds.X,e.Bounds.Y,e.Bounds.Width,e.Bounds.Height);
    PixelIcons.Draw(e.Graphics,t.Done?"star":"quest",new Point(10,e.Bounds.Y+16),2);
    Theme.Text(e.Graphics,t.Title,taskList.Font,t.Done?Theme.Muted:Theme.Ink,new Rectangle(42,e.Bounds.Y+3,560,24));
    Theme.Text(e.Graphics,Scheduler.Due(t).ToString("MM/dd (ddd) ")+Scheduler.TimeRange(t,Scheduler.Due(t))+" · "+t.Category+" · "+(GoogleCalendar.Imported(t)?"Google":Scheduler.RepeatLabel(t)),taskList.Font,Theme.Muted,new Rectangle(42,e.Bounds.Y+26,650,20));
    Theme.Text(e.Graphics,(t.AllDay?"종일  ":"")+(t.EveReminder?"전날 "+t.EveTime+" 알림  ":"")+(!String.IsNullOrEmpty(t.ScheduleImage)?"모습 변경":""),Theme.Font(8),Theme.Purple,new Rectangle(42,e.Bounds.Y+45,620,18));
    Theme.Text(e.Graphics,t.Done?"CLEAR":Scheduler.Overtime(t)?"-20 G":"+20 G",taskList.Font,t.Done?Theme.Purple:Scheduler.Overtime(t)?Color.FromArgb(177,90,105):Theme.Ink,new Rectangle(590,e.Bounds.Y+3,110,24),ContentAlignment.MiddleRight);
    Theme.Fill(e.Graphics,Theme.Light,4,e.Bounds.Bottom-2,e.Bounds.Width-8,1);
   };board.Controls.Add(taskList);
   taskList.DoubleClick+=(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t!=null)EditSchedule(t,"");};
   if(taskList.Items.Count==0)board.Controls.Add(Theme.Label("오늘은 일정이 없어요.\n다른 날짜는 달력에서 확인하세요!",28,80,680,50,14,Theme.Muted));
   var completeButton=B("완료 +20 코인",0,503,234,42,(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t==null)return;int before=Store.State.Coins;if(Rules.Complete(t,Store.State)){Store.Save();int delta=Store.State.Coins-before;pet.Say(Scheduler.Overtime(t)?"야근 끝! 이제 푹 쉬자. "+delta+" G":"QUEST CLEAR! +20 G",false);RefreshPage();}else GameAlert.Show("이미 완료했거나 다음 날짜의 반복 일정이에요.");},true);
   taskList.SelectedIndexChanged+=(s,e)=>{if(!completeButton.IsDisposed){var t=taskList.SelectedItem as TaskItem;completeButton.Text=t!=null&&Scheduler.Overtime(t)?"야근 완료 -20 코인":"완료 +20 코인";}};
   B("일정 수정",250,503,234,42,(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t!=null)EditSchedule(t,"");});
   B("10분 뒤 알림",510,503,234,42,(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t==null||t.Done)return;Scheduler.Snooze(t,AppClock.Now);Store.Save();RefreshPage();});
   B("이번 반복 건너뛰기",0,551,360,32,(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(t!=null&&Scheduler.Skip(t,AppClock.Now)){Store.Save();RefreshPage();}});
   B("일정 삭제",384,551,360,32,(s,e)=>{var t=taskList.SelectedItem as TaskItem;if(GoogleCalendar.Imported(t)){GameAlert.Show("구글 캘린더에서 삭제하면 다음 갱신에 반영돼요.");return;}if(t!=null&&GameAlert.Show(t.Repeat=="한 번"?"일정을 삭제할까요?":"반복 일정 전체를 삭제할까요?","일정 삭제",MessageBoxButtons.YesNo)==DialogResult.Yes){Store.State.Tasks.Remove(t);Store.Save();RefreshPage();}});
  }

  void FocusPage(){
   Title("FOCUS ADVENTURE","집중 모험","집중한 시간이 경험치와 코인이 돼요. 1분 = 1 G");
   Add(new RoomScene{Location=new Point(0,111),Size=new Size(744,235),Night=true});
   var timer=Card(0,361,744,80);countdown=Theme.Label("READY",16,14,320,52,22,Theme.Purple);timer.Controls.Add(countdown);
   timer.Controls.Add(Theme.Label("집중 시간",360,24,140, 28));
   var minutes=new NumericUpDown{Location=new Point(520,24),Size=new Size(193, 30),Minimum=1,Maximum=120,Value=25,BackColor=Theme.Cream,ForeColor=Theme.Ink,Font=Theme.Font(10)};timer.Controls.Add(minutes);
   B("집중 시작",0,459,360, 50,(s,e)=>{if(Store.State.FocusEnd!=DateTime.MinValue){GameAlert.Show("이미 집중 중이에요.");return;}Store.State.SessionMinutes=(int)minutes.Value;Store.State.FocusEnd=AppClock.Now.AddMinutes((int)minutes.Value);Store.Save();pet.ApplyPetSize();pet.Say("집중 모험 시작! 함께 힘내보자.",false);RefreshPage();},true);
   B("집중 취소",376,459,368, 50,(s,e)=>{if(Store.State.FocusEnd==DateTime.MinValue)return;if(GameAlert.Show("모험을 취소할까요? 완료 보상은 지급되지 않아요.","집중 취소",MessageBoxButtons.YesNo)==DialogResult.Yes){Store.State.FocusEnd=DateTime.MinValue;Store.State.SessionMinutes=0;Store.Save();pet.ApplyPetSize();RefreshPage();}});
   L("완료한 모험 "+Store.State.FocusCount+"회  /  누적 집중 "+Store.State.FocusMinutes+"분",0,535,744, 30,10,Theme.Muted);
  }

  void Game(){
   Title("MINIGAMES / PLAY WITH YOUR BUDDY","친구와 미니게임","배구 · 마법의 성 · 하늘에서 똥 피하기, 함께 도전해요!");
   Add(new VolleyballPreview(pet){Location=new Point(0,111),Size=new Size(744,310)});
   L("← → 또는 A/D 이동   SPACE 점프   ↓ 또는 X 스매시",0,437,744,28);
   L("ESC 일시정지 / 다른 창 전환 시 자동 정지 / R 다시 한 판",0,468,744,28,10,Theme.Muted);
   B("바탕화면 배구 시작",0,507,232,48,(s,e)=>pet.StartVolleyball(),true);
   B("마법의 성 모험 시작",244,507,232,48,(s,e)=>pet.StartCastle(),true);
   var dodge=B("똥 피하기 시작",488,507,232,48,(s,e)=>pet.StartDodge(),true);dodge.Enabled=Companions.Level(Store.State)>=2;if(!dodge.Enabled)dodge.Text="똥 피하기 · Lv.2";
   L("화살표로 피하기 · 20 / 25 / 30초 생존 · 3탄 모두 통과하면 +50 G",0,567,744,28,10,Theme.Muted);
   var advanced=B("마법의 성 4·5탄 · Lv.3 · 클리어 +100 G",0,648,720,46,(s,e)=>pet.StartCastleAdvanced(),true);advanced.Enabled=Companions.Level(Store.State)>=3;
   int played=Store.State.GamesDate==AppClock.Now.ToString("yyyy-MM-dd")?Store.State.GamesPlayed:0;
   L("성 / 똥 피하기 3탄 +50 G · 배구 승리 +20 G · 오늘 "+played+" / 3",0,607,744,20,10,Theme.Muted);
  }

  void Shop(){
   Living.Normalize(Store.State);Title("BUDDY BOUTIQUE / LITTLE JOYS","친구의 작은 백화점","옷을 고르고, 방을 꾸미고, 맛있는 한 끼를 선물해요.");
   string[] tabs={"캐릭터","방","음식"};string[] titles={"캐릭터 꾸미기","방 꾸미기","밥 / 간식"};
   for(int t=0;t<3;t++){string target=tabs[t];var tab=B(titles[t],t*244,111,232,42,(s,e)=>{shopTab=target;RefreshPage();},shopTab==target);tab.BackColor=shopTab==target?Theme.Purple:Theme.Cream;}
   var items=Living.Items.Where(i=>i.Kind==shopTab).OrderBy(i=>Progression.Required(i.Name)).ThenBy(i=>i.Price).ToArray();
   for(int i=0;i<items.Length;i++){
    var item=items[i];bool owned=Living.Owned(Store.State,item),equipped=item.Kind!="음식"&&Living.Equipped(Store.State,item)==item.Name;
    int x=i%3*244,y=176+i/3*254;var card=Card(x,y,232,244,equipped?Color.FromArgb(235,230,197):Theme.Cream);
    var stage=new Panel{Location=new Point(10,10),Size=new Size(212,92),BackColor=item.Kind=="음식"?Color.FromArgb(246,222,211):item.Kind=="방"?Color.FromArgb(220,230,213):Color.FromArgb(230,223,236)};card.Controls.Add(stage);
    if(item.Kind=="캐릭터")stage.Controls.Add(new ItemPortrait{Item=item.Name,Location=new Point(0,0),Size=new Size(212,90)});
    else if(item.Kind=="음식")stage.Controls.Add(new FoodPortrait{Food=item.Name,Location=new Point(0,0),Size=new Size(212,90)});
    else{var demo=new Data{RoomWallpaper=Store.State.RoomWallpaper,RoomRug=Store.State.RoomRug,RoomDecoration=Store.State.RoomDecoration};if(item.Slot=="벽지")demo.RoomWallpaper=item.Name;else if(item.Slot=="러그")demo.RoomRug=item.Name;else demo.RoomDecoration=item.Name;stage.Controls.Add(new RoomScene{Decor=demo,ShowPet=false,Animated=false,Location=new Point(0,0),Size=new Size(212,92)});}
    var label=Theme.Label(item.Name,10,107,212,27,12);label.TextAlign=ContentAlignment.MiddleCenter;card.Controls.Add(label);
    var hint=Theme.Label(item.Description.StartsWith("Lv.")?item.Description:"Lv."+Progression.Required(item.Name)+" · "+item.Description,10,136,212,32,8,Theme.Muted);hint.TextAlign=ContentAlignment.MiddleCenter;card.Controls.Add(hint);
    bool unlocked=Companions.Level(Store.State)>=Progression.Required(item.Name);var price=Theme.Label(!unlocked?"Lv."+Progression.Required(item.Name)+"에서 열려요":equipped?"사용 중":owned?"보유 중 · 다시 꾸미기":item.Price+" G"+(item.Kind=="음식"?" · 한 번 먹기":""),10,173,212,22,9,Theme.Purple);price.TextAlign=ContentAlignment.MiddleCenter;card.Controls.Add(price);
    var button=Theme.Button(item.Kind=="음식"?"바로 먹이기":owned?"장착하기":"구매 + 장착",10,201,212,35,(s,e)=>{
     if(item.Kind=="음식"){pet.Feed(item.Name);RefreshPage();return;}string error;if(!Living.Buy(Store.State,item.Name,out error)){GameAlert.Show(error,"작은 백화점");return;}Store.Save();pet.Say(item.Kind=="방"?"우리 방이 더 예뻐졌어!":"새 스타일 어때?",false);RefreshPage();
    },!owned);button.Enabled=unlocked;if(!unlocked)button.Text="잠김 · Lv."+Progression.Required(item.Name);card.Controls.Add(button);
   }
  }
  void Character(){
   Title("CHARACTER / CUSTOMIZE","나만의 친구","좋아하는 캐릭터 이미지로 교체하고 이름을 지어줘요.");
   var form=Card(0,289,720,70);
   form.Controls.Add(Theme.Label("이름",16, 20,80, 28));
   var name=Theme.Input(Store.State.PetName,98,20,406);name.MaxLength=24;form.Controls.Add(name);
   EventHandler saveName=(s,e)=>{if(String.IsNullOrWhiteSpace(name.Text)){GameAlert.Show("이름을 입력해주세요.");return;}string previous=Store.State.PetName;Store.State.PetName=name.Text.Trim();if(!Store.Save()){Store.State.PetName=previous;return;}pet.RefreshIdentity();RefreshPage();};
   form.Controls.Add(Theme.Button("이름 저장",520,14,188,42,saveName,true));
   name.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;saveName(s,e);}};
   B("캐릭터 이미지 불러오기",0,375,350,46,(s,e)=>Import(),true);
   B("기본 캐릭터로 돌아가기",370,375,350,46,(s,e)=>SelectCharacter(""));

   var sizing=Card(0,437,720,70);sizing.Controls.Add(Theme.Label("바탕화면 크기",16,20,175,30));
   var slider=new PixelSizeSlider{Location=new Point(197,17),Size=new Size(315,36),Value=Store.State.PetSize};
   var sizeLabel=Theme.Label(Store.State.PetSize+" px",525,20,90,30);sizing.Controls.Add(sizeLabel);sizing.Controls.Add(slider);
   slider.ValueChanged+=(s,e)=>{Store.State.PetSize=slider.Value;pet.ApplyPetSize();sizeLabel.Text=slider.Value+" px";Store.Save();};
   sizing.Controls.Add(Theme.Button("기본",607,16,98,38,(s,e)=>slider.Value=140));
   var bubbleSizing=Card(0,519,720,70);bubbleSizing.Controls.Add(Theme.Label("말풍선 크기",16,20,108,30));bubbleSizing.Controls.Add(Theme.Button("미리보기",120,16,74,38,(s,e)=>{using(var preview=new SizePreviewForm(pet))preview.ShowDialog(this);RefreshPage();}));
   var bubbleSlider=new PixelSizeSlider{Minimum=60,Maximum=160,Location=new Point(197,17),Size=new Size(315,36),Value=Store.State.BubbleScale};var bubbleLabel=Theme.Label(Store.State.BubbleScale+" %",525,20,80,30);bubbleSizing.Controls.Add(bubbleSlider);bubbleSizing.Controls.Add(bubbleLabel);bubbleSlider.ValueChanged+=(s,e)=>{Store.State.BubbleScale=bubbleSlider.Value;pet.ApplyPetSize();bubbleLabel.Text=bubbleSlider.Value+" %";Store.Save();};bubbleSizing.Controls.Add(Theme.Button("기본",607,16,98,38,(s,e)=>bubbleSlider.Value=100));
   var timerSizing=Card(0,601,720,70);timerSizing.Controls.Add(Theme.Label("집중 타이머",16,20,108,30));timerSizing.Controls.Add(Theme.Button("미리보기",120,16,74,38,(s,e)=>{using(var preview=new SizePreviewForm(pet))preview.ShowDialog(this);RefreshPage();}));
   var timerSlider=new PixelSizeSlider{Minimum=60,Maximum=180,Location=new Point(197,17),Size=new Size(315,36),Value=Store.State.FocusTimerScale};var timerLabel=Theme.Label(timerSlider.Value+" %",525,20,80,30);timerSizing.Controls.Add(timerSlider);timerSizing.Controls.Add(timerLabel);timerSlider.ValueChanged+=(s,e)=>{Store.State.FocusTimerScale=timerSlider.Value;pet.ApplyPetSize();timerLabel.Text=timerSlider.Value+" %";Store.Save();};timerSizing.Controls.Add(Theme.Button("기본",607,16,98,38,(s,e)=>timerSlider.Value=100));
   L("기본 친구를 고르면 이름도 함께 바뀌어요.",0,689,495,28,10,Theme.Muted);
   B("샘플 여러 개 추가",510,685,210,38,(s,e)=>AddSamples());
   int index=0;foreach(string sample in CharacterLibrary.Samples()){
    string path=sample;int x=index%6*120,y=index<6?111:749+(index-6)/6*166;bool selected=Store.State.ImagePath==path||(Store.State.ImagePath==""&&path==CharacterLibrary.Default);
    var card=Card(x,y,112,160,selected?Theme.Gold:Theme.Cream);card.Controls.Add(new SamplePortrait(path){Location=new Point(6,6),Size=new Size(100,112)});
    card.Controls.Add(Theme.Button(selected?"선택됨":CharacterLibrary.Label(path),6,121,100,33,(s,e)=>SelectCharacter(path)));index++;
   }
   if(index==0)L("샘플 여러 개 추가로 이미지들을 등록해 주세요.",0,650,744,30,10,Theme.Muted);

  }
  void SelectCharacter(string path){if(!PetArt.LoadCustom(path)){GameAlert.Show("캐릭터 이미지를 읽지 못했습니다.");return;}Store.State.ImagePath=path;if(string.IsNullOrEmpty(path)||path.StartsWith("builtin:",StringComparison.Ordinal))Store.State.PetName=CharacterLibrary.Label(string.IsNullOrEmpty(path)?CharacterLibrary.Default:path);Store.Save();pet.CharacterChanged();RefreshPage();}
  void AddSamples(){
   using(var dialog=new OpenFileDialog{Filter="캐릭터 이미지|*.png;*.jpg;*.jpeg;*.bmp",Multiselect=true,Title="샘플 캐릭터들을 선택하세요"}){
    if(dialog.ShowDialog()!=DialogResult.OK)return;
    foreach(string source in dialog.FileNames)try{string target=CharacterLibrary.Import(source);if(Store.State.CharacterSamples==null)Store.State.CharacterSamples=new List<string>();Store.State.CharacterSamples.Add(target);}catch(Exception ex){GameAlert.Show(Path.GetFileName(source)+": "+ex.Message);}
    Store.Save();RefreshPage();
   }
  }
  void Import(){
   using(var dialog=new OpenFileDialog{Filter="캐릭터 이미지|*.png;*.jpg;*.jpeg;*.bmp",Title="캐릭터 선택"}){
    if(dialog.ShowDialog()!=DialogResult.OK)return;
    try{if(new FileInfo(dialog.FileName).Length>10*1024*1024)throw new Exception("10MB 이하 이미지를 사용해주세요.");
     using(var im=Image.FromFile(dialog.FileName)){if(im.Width>4096||im.Height>4096)throw new Exception("4096px 이하 이미지를 사용해주세요.");
      Directory.CreateDirectory(Store.Root);string target=Path.Combine(Store.Root,"character-"+Guid.NewGuid().ToString("N")+".png");
      using(var b=new Bitmap(im))b.Save(target,System.Drawing.Imaging.ImageFormat.Png);if(!PetArt.LoadCustom(target))throw new Exception("이미지를 불러오지 못했습니다.");
      Store.State.ImagePath=target;Store.Save();pet.CharacterChanged();RefreshPage();
     }
    }catch(Exception e){GameAlert.Show(e.Message,"캐릭터 선택");}
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
    if(dialog.ShowDialog()==DialogResult.OK){try{File.WriteAllText(dialog.FileName,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(Store.State),System.Text.Encoding.UTF8);GameAlert.Show("저장 파일을 백업했어요. 캐릭터 이미지는 별도로 보관해주세요.");}catch(Exception ex){GameAlert.Show("백업 실패: "+ex.Message);}}
   }));
   B("말풍선 알림 테스트",0,424,360, 46,(s,e)=>pet.Say("회의 준비할 시간! 자료 챙겼어?",true),true);
   B("DeskBuddy 완전 종료",376,424,368, 46,(s,e)=>pet.Quit());
   B("구글 캘린더 연결",0,648,720,46,(s,e)=>{using(var calendar=new GoogleCalendarForm())calendar.ShowDialog(this);RefreshPage();});
   B("관리자 설정",0,590,720,46,(s,e)=>{using(var admin=new AdminForm())admin.ShowDialog(this);pet.ApplyPetSize();RefreshPage();});
   var note=Card(0,488,744,88,Color.FromArgb(233,222,195));note.Controls.Add(Theme.Label("TIP / 창을 닫아도 바탕화면 펫은 남아요.\n완전 종료는 위 버튼으로! 알림은 앱 실행 중에 동작해요.",18, 16,704, 60,10,Theme.Muted));
  }
  
 }
}
