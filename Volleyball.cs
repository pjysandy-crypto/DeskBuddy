using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace DeskBuddy {
 public class VolleyInput {public bool Left,Right,Jump,Smash;}
 public class VolleyPlayer {public double X,Y,Vx,Vy,ContactCooldown,SmashTime,SmashCooldown;}
 public class VolleyballEngine {
  public const double Width=900,Ground=450,NetX=450,NetTop=285,BallRadius=12,PlayerRadius=30;
  public VolleyPlayer Player=new VolleyPlayer(),Computer=new VolleyPlayer();
  public double BallX,BallY,BallVx,BallVy,RallyDelay=1,HitFlash;public PointF HitSpot;public bool LastSmash;
  public int PlayerScore,ComputerScore;public bool Over,ComputerEnabled=true;bool serveLeft=true,rewarded;
  public VolleyballEngine(){Player.X=210;Computer.X=690;Player.Y=Computer.Y=Ground-PlayerRadius;Serve();}
  static double Clamp(double v,double lo,double hi){return Math.Max(lo,Math.Min(hi,v));}
  void Serve(){BallX=serveLeft?Player.X:Computer.X;BallY=210;BallVx=serveLeft?285:-285;BallVy=-310;}
  public void Step(double dt,VolleyInput input){
   if(Over)return;dt=Clamp(dt,0,.025);HitFlash=Math.Max(0,HitFlash-dt);
   UpdatePlayer(Player,dt,(input.Right?1:0)-(input.Left?1:0),input.Jump,input.Smash,true);
   double move=0;bool jump=false;
   if(ComputerEnabled){

    double flight=(-BallVy+Math.Sqrt(BallVy*BallVy+2*920*Math.Max(0,Ground-70-BallY)))/920;
    double target=BallX>NetX||BallVx>0?BallX+BallVx*Clamp(flight,0,1.4):680;
    if(target>Width-20)target=2*(Width-20)-target;

    target=Clamp(target,NetX+44,Width-44);move=Math.Abs(target-Computer.X)<8?0:Math.Sign(target-Computer.X);
    jump=BallX>NetX && Math.Abs(BallX-Computer.X)<95 && BallY<370 && BallVy>20;
   }
   UpdatePlayer(Computer,dt,move,jump,false,false);
   if(RallyDelay>0){RallyDelay=Math.Max(0,RallyDelay-dt);if(RallyDelay==0)Serve();return;}
   BallVy+=920*dt;BallX+=BallVx*dt;BallY+=BallVy*dt;
   if(BallX<BallRadius+8){BallX=BallRadius+8;BallVx=Math.Abs(BallVx)*.92;}
   if(BallX>Width-BallRadius-8){BallX=Width-BallRadius-8;BallVx=-Math.Abs(BallVx)*.92;}
   if(BallY<74+BallRadius){BallY=74+BallRadius;BallVy=Math.Abs(BallVy)*.9;}
   HitPlayer(Player,true);HitPlayer(Computer,false);HitNet();
   if(BallY+BallRadius>=Ground){
    if(BallX<NetX){ComputerScore++;serveLeft=true;}else{PlayerScore++;serveLeft=false;}
    BallY=Ground-BallRadius;BallVx=BallVy=0;Over=PlayerScore>=5||ComputerScore>=5;RallyDelay=1.2;
   }
  }
  void UpdatePlayer(VolleyPlayer p,double dt,double movement,bool jump,bool smash,bool left){
   p.ContactCooldown=Math.Max(0,p.ContactCooldown-dt);p.SmashCooldown=Math.Max(0,p.SmashCooldown-dt);p.SmashTime=Math.Max(0,p.SmashTime-dt);
   p.Vx=movement*(left?320:280);p.X=Clamp(p.X+p.Vx*dt,left?44:NetX+42,left?NetX-42:Width-44);
   if(jump&&p.Y>=Ground-PlayerRadius-.1)p.Vy=-620;
   if(smash&&p.SmashCooldown<=0){p.SmashTime=.32;p.SmashCooldown=.65;}
   p.Vy+=1450*dt;p.Y+=p.Vy*dt;
   if(p.Y>=Ground-PlayerRadius){p.Y=Ground-PlayerRadius;p.Vy=0;}
  }
  void HitPlayer(VolleyPlayer p,bool left){
   double dx=BallX-p.X,dy=BallY-p.Y,d=Math.Sqrt(dx*dx+dy*dy),r=PlayerRadius+BallRadius;
   if(d>=r||p.ContactCooldown>0)return;
   double nx=d<.001?0:dx/d,ny=d<.001?-1:dy/d;
   BallX=p.X+nx*(r+1);BallY=p.Y+ny*(r+1);
   bool smash=p.SmashTime>0;
   BallVx=(left?1:-1)*(smash?720:275)+p.Vx*.55+nx*180;
   BallVy=smash?(BallY<NetTop-30?100:-410):Math.Min(-330,-460+p.Vy*.18);
   BallVx=Clamp(BallVx,-850,850);p.ContactCooldown=.14;HitFlash=.28;HitSpot=new PointF((float)BallX,(float)BallY);LastSmash=smash;
  }
  void HitNet(){
   double nearX=Clamp(BallX,NetX-5,NetX+5),nearY=Clamp(BallY,NetTop,Ground);
   double dx=BallX-nearX,dy=BallY-nearY,d=Math.Sqrt(dx*dx+dy*dy);
   if(d>=BallRadius)return;
   if(d<.001){dx=BallVx>=0?-1:1;dy=0;d=1;}
   double nx=dx/d,ny=dy/d;BallX=nearX+nx*(BallRadius+.5);BallY=nearY+ny*(BallRadius+.5);
   double dot=BallVx*nx+BallVy*ny;if(dot<0){BallVx-=1.8*dot*nx;BallVy-=1.8*dot*ny;}
  }
  public int Award(Data data,DateTime now){
   if(!Over||rewarded)return -2;rewarded=true;return Rules.GameReward(data,PlayerScore>=5?20:5,now);
  }
 }
 public class VolleyballForm:Form {
  public VolleyballEngine Engine=new VolleyballEngine();VolleyInput input=new VolleyInput();Timer timer=new Timer{Interval=16};
  Stopwatch watch=new Stopwatch();double previous,accumulator;bool started,paused;int reward=-2;PetForm owner;
  Queue<PointF> trail=new Queue<PointF>();Point drag;bool movingWindow;
  public VolleyballForm(PetForm pet){
   owner=pet;Icon=AppIdentity.Icon;Text="DeskBuddy · 바탕화면 배구";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=true;TopMost=true;AutoScaleMode=AutoScaleMode.None;
   ClientSize=new Size(900,540);StartPosition=FormStartPosition.Manual;
   var screen=Screen.FromControl(pet).WorkingArea;Location=new Point(screen.Left+Math.Max(0,(screen.Width-900)/2),screen.Bottom-Height-12);
   KeyPreview=true;BackColor=Color.Black;TransparencyKey=Color.Empty;
   timer.Tick+=(s,e)=>Tick();Shown+=(s,e)=>{watch.Start();Activate();RenderLayered();timer.Start();};
   Deactivate+=(s,e)=>{input=new VolleyInput();if(started&&!Engine.Over)paused=true;};
   FormClosed+=(s,e)=>timer.Stop();MouseDown+=(s,e)=>ClickGame(e);MouseUp+=(s,e)=>movingWindow=false;
   MouseMove+=(s,e)=>{if(movingWindow){Location=new Point(Left+e.X-drag.X,Top+e.Y-drag.Y);RenderLayered();}};
  }
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x80000;return p;}}
  protected override bool IsInputKey(Keys keyData){var k=keyData&Keys.KeyCode;return k==Keys.Left||k==Keys.Right||k==Keys.Up||k==Keys.Down||base.IsInputKey(keyData);}
  protected override void OnKeyDown(KeyEventArgs e){
   base.OnKeyDown(e);Keys k=e.KeyCode;
   if(k==Keys.Escape||k==Keys.P){paused=!paused;input=new VolleyInput();e.SuppressKeyPress=true;RenderLayered();return;}
   if(k==Keys.R){Restart();e.SuppressKeyPress=true;return;}
   if(k==Keys.Enter||k==Keys.Space){if(!started||paused){started=true;paused=false;input=new VolleyInput();e.SuppressKeyPress=true;RenderLayered();return;}}
   if(k==Keys.Left||k==Keys.A)input.Left=true;if(k==Keys.Right||k==Keys.D)input.Right=true;
   if(k==Keys.Up||k==Keys.W||k==Keys.Space)input.Jump=true;
   if(k==Keys.Down||k==Keys.X)input.Smash=true;e.SuppressKeyPress=true;
  }
  protected override void OnKeyUp(KeyEventArgs e){
   base.OnKeyUp(e);Keys k=e.KeyCode;
   if(k==Keys.Left||k==Keys.A)input.Left=false;if(k==Keys.Right||k==Keys.D)input.Right=false;
   if(k==Keys.Up||k==Keys.W||k==Keys.Space)input.Jump=false;if(k==Keys.Down||k==Keys.X)input.Smash=false;
  }
  void Restart(){Engine=new VolleyballEngine();input=new VolleyInput();trail.Clear();started=false;paused=false;reward=-2;accumulator=0;RenderLayered();}
  void Tick(){
   double now=watch.Elapsed.TotalSeconds,elapsed=Math.Min(.08,now-previous);previous=now;
   if(started&&!paused&&!Engine.Over){
    accumulator+=elapsed;while(accumulator>=1.0/120){Engine.Step(1.0/120,input);accumulator-=1.0/120;}
    if(Engine.RallyDelay<=0){trail.Enqueue(new PointF((float)Engine.BallX,(float)Engine.BallY));while(trail.Count>9)trail.Dequeue();}
    if(Engine.Over){reward=Engine.Award(Store.State,DateTime.Now);if(reward>=0)Store.Save();owner.Say(Engine.PlayerScore>=5?"배구 승리! +"+Math.Max(0,reward)+" G":"좋은 경기였어! 다음엔 이겨보자.",false);}
   }else accumulator=0;
   RenderLayered();
  }
  Rectangle ActionButton=new Rectangle(290,290,320,48),ExitButton=new Rectangle(852,12,36,36);
  void ClickGame(MouseEventArgs e){
   Activate();if(ExitButton.Contains(e.Location)){Close();return;}
   if((!started||paused||Engine.Over)&&ActionButton.Contains(e.Location)){
    if(Engine.Over)Restart();started=true;paused=false;input=new VolleyInput();RenderLayered();return;
   }
   if(e.Y<60){movingWindow=true;drag=e.Location;}
  }
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnPaint(PaintEventArgs e){Draw(e.Graphics);}
  public Bitmap Snapshot(){var b=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);Draw(g);}return b;}
  void RenderLayered(){if(IsHandleCreated&&!IsDisposed)using(var b=Snapshot())AlphaWindow.Update(Handle,Location,b);}
  public void RenderForTest(){using(var b=Snapshot())AlphaWindow.Update(Handle,Location,b);}
  void DrawText(Graphics g,string text,int x,int y,int w,int h,float size=10,Color? color=null,ContentAlignment align=ContentAlignment.MiddleCenter){
   using(var f=Theme.Font(size))Theme.Text(g,text,f,color??Theme.Ink,new Rectangle(x,y,w,h),align);
  }
  public Bitmap SnapshotCourt(){var b=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);Draw(g,false);}return b;}
  void Draw(Graphics g,bool showMenu=true){
   Theme.Frame(g,new Rectangle(8,8,884,54),Theme.Cream);
   DrawText(g,"YOU  "+Engine.PlayerScore,20,14,160,36,14,Theme.Purple);DrawText(g,"PET VOLLEY / 5점 먼저!",200,16,440,32);
   DrawText(g,"CPU  "+Engine.ComputerScore,664,14,164,36,14,Theme.Muted);
   Theme.Frame(g,ExitButton,Theme.Peach);DrawText(g,"X",852,12,36,36);
   Theme.Fill(g,Color.FromArgb(185,222,194,141),12,450,876, 30);Theme.Fill(g,Theme.Line,12,450,876,4);
   Theme.Fill(g,Color.FromArgb(235,94,132,104),12,480,876,10);
   for(int x=24;x<884;x+=32)Theme.Fill(g,Color.FromArgb(210,255,240,209),x,464,16,3);
   Theme.Fill(g,Theme.Line,445,285,10,165);Theme.Fill(g,Theme.Cream,449,289,3,161);
   Theme.Fill(g,Theme.Gold,442,280,16,8);
   for(int y=306;y<450;y+=16)Theme.Fill(g,Color.FromArgb(220,195,175,136),436,y,28,2);
   DrawPlayer(g,Engine.Player,true);DrawPlayer(g,Engine.Computer,false);
   int n=0;foreach(var t in trail){int a=30+n++*14;Theme.Fill(g,Color.FromArgb(a,239,188,82),(int)t.X-3,(int)t.Y-3,6,6);}
   DrawBall(g,(float)Engine.BallX,(float)Engine.BallY);
   if(Engine.HitFlash>0){
    var p=Engine.HitSpot;int d=(int)((.28-Engine.HitFlash)*90+12);
    for(int i=0;i<4;i++)Theme.Fill(g,Engine.LastSmash?Theme.Peach:Theme.Gold,(int)p.X+(i%2==0?-d:d),(int)p.Y+(i<2?-d:d),5,5);
    if(Engine.LastSmash)DrawText(g,"SMASH!",(int)p.X- 80,(int)p.Y-65,160, 30,14,Theme.Peach);
   }
   Theme.Frame(g,new Rectangle(8,496,884,40),Theme.Cream);
   DrawText(g,"← → 이동 / SPACE 점프 / ↓ 또는 X 스매시 / ESC 일시정지 / R 다시",12,497,876,34,10,Theme.Ink);
   if(showMenu&&(!started||paused||Engine.Over)){
    Theme.Frame(g,new Rectangle(234,150,432,208),Theme.Cream);
    string title=Engine.Over?(Engine.PlayerScore>=5?"YOU WIN!":"NEXT TIME!"):paused?"PAUSED":"PET VOLLEY";
    DrawText(g,title,246,168,408, 50,22,Engine.Over?Theme.Purple:Theme.Ink);
    string hint=Engine.Over?(reward>=0?"보상 +"+reward+" G":"오늘의 코인 보상은 모두 받았어요."):paused?"다른 창을 열면 자동으로 잠시 멈춰요.":"내 캐릭터로 컴퓨터 친구와 배구 한 판!";
    DrawText(g,hint,246,224,408, 50);
    Theme.Frame(g,ActionButton,Theme.Purple);DrawText(g,Engine.Over?"다시 한 판 / R":paused?"계속하기 / ENTER":"경기 시작 / ENTER",290,290,320,48,10,Theme.Cream);
   }else if(Engine.RallyDelay>0){DrawText(g,"READY!",300,156,300, 50,22,Theme.Purple);}
  }
  
  void DrawPlayer(Graphics g,VolleyPlayer p,bool mine){
   Theme.Fill(g,Color.FromArgb(70,58,41,32),(int)p.X-36,443,72,5);
   if(mine)PetArt.Draw(g,new Rectangle((int)p.X- 50,(int)p.Y- 60,100,100),(int)(watch.Elapsed.TotalSeconds*6),false,Progression.Accessory(Store.State));
   else{
    using(var b=new Bitmap(32,32))using(var a=Graphics.FromImage(b)){
     Theme.Fill(a,Theme.Line,5,6,22,22);Theme.Fill(a,Theme.Line,3,12,26,13);
     Theme.Fill(a,Theme.Gold,7,7,18,19);Theme.Fill(a,Theme.Gold,5,13,22,10);
     Theme.Fill(a,Theme.Line,10,13,3,4);Theme.Fill(a,Theme.Line,20,13,3,4);
     Theme.Fill(a,Theme.Cream,10,13,1,1);Theme.Fill(a,Theme.Cream,20,13,1,1);
     Theme.Fill(a,Theme.Peach,7,18,4,2);Theme.Fill(a,Theme.Peach,23,18,3,2);
     Theme.Fill(a,Theme.Line,15,20,4,2);Theme.Fill(a,Theme.Line,5,26,8,4);Theme.Fill(a,Theme.Line, 20,26,8,4);
     var state=g.Save();g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
     g.DrawImage(b,new Rectangle((int)p.X-48,(int)p.Y- 50,96,96));g.Restore(state);
    }
   }
  }
  
  static void DrawBall(Graphics g,float x,float y){
   Theme.Fill(g,Theme.Line,(int)x-9,(int)y-12,18,24);Theme.Fill(g,Theme.Line,(int)x-12,(int)y-8,24,16);
   Theme.Fill(g,Theme.Cream,(int)x-8,(int)y-10,16,20);Theme.Fill(g,Theme.Cream,(int)x-10,(int)y-7,20,14);
   Theme.Fill(g,Theme.Peach,(int)x-8,(int)y-8,7,5);Theme.Fill(g,Theme.Purple,(int)x+2,(int)y+1,7,7);
   Theme.Fill(g,Theme.Line,(int)x-1,(int)y-8,2,17);
  }
  protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
 }

 public class VolleyProbe:VolleyballForm {
  public VolleyProbe(PetForm pet):base(pet){}
  public void KeyForTest(Keys key,bool down){if(down)base.OnKeyDown(new KeyEventArgs(key));else base.OnKeyUp(new KeyEventArgs(key));}
  public void PumpForTest(int ms){var until=DateTime.UtcNow.AddMilliseconds(ms);while(DateTime.UtcNow<until){Application.DoEvents();System.Threading.Thread.Sleep(4);}}
 }

 public static class VolleyballTests {
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  public static void Run(){
   var input=new VolleyInput();var e=new VolleyballEngine{ComputerEnabled=false,RallyDelay=0};
   double x=e.Player.X;input.Right=true;e.Step(.02,input);Check(e.Player.X>x,"Move right");
   input.Right=false;input.Jump=true;e.Step(.02,input);Check(e.Player.Vy<0&&e.Player.Y<420,"Jump");
   input.Jump=false;for(int i=0;i<150;i++)e.Step(1.0/120,input);Check(e.Player.Y<=420&&e.Player.X<450,"Player bounds");
   e=new VolleyballEngine{ComputerEnabled=false,RallyDelay=0};e.BallX=430;e.BallY=320;e.BallVx=650;e.BallVy=0;e.Step(.02,new VolleyInput());Check(e.BallVx<0&&e.BallX<445,"Net rebound");
   e=new VolleyballEngine{ComputerEnabled=false,RallyDelay=0};e.BallX=e.Player.X;e.BallY=e.Player.Y-41;e.BallVy=300;e.Step(.01,new VolleyInput());Check(e.BallVy<0,"Player bounce");
   e=new VolleyballEngine{ComputerEnabled=false,RallyDelay=0};e.BallX=e.Player.X;e.BallY=e.Player.Y-41;e.BallVy=300;e.Step(.01,new VolleyInput{Smash=true});Check(e.LastSmash&&e.BallVx>600,"Smash");
   e=new VolleyballEngine{ComputerEnabled=false,RallyDelay=0};e.BallX=100;e.BallY=440;e.BallVy=0;e.Step(.01,new VolleyInput());Check(e.ComputerScore==1,"Floor scoring");e.Step(.01,new VolleyInput());Check(e.ComputerScore==1,"No duplicate score");
   e=new VolleyballEngine{ComputerEnabled=false};
   for(int n=0;n<5;n++){e.RallyDelay=0;e.BallX=740;e.BallY=441;e.BallVy=0;e.Step(.01,new VolleyInput());}
   Check(e.Over&&e.PlayerScore==5,"First to five");var d=new Data();Check(e.Award(d,DateTime.Today)==20&&d.Coins==50,"Match reward");Check(e.Award(d,DateTime.Today)==-2&&d.Coins==50,"No duplicate match reward");
   e=new VolleyballEngine();for(int i=0;i<20000&&!e.Over;i++)e.Step(1.0/120,new VolleyInput{Right=i%500<250,Left=i%500>=250,Jump=i%200<25,Smash=i%300<30});
   Check(!Double.IsNaN(e.BallX)&&!Double.IsInfinity(e.BallVy)&&e.PlayerScore<=5&&e.ComputerScore<=5,"Physics stability");
  }
  public static int Execute(){
   try{Run();using(var p=new PetForm())using(var f=new VolleyballForm(p)){
     f.Engine.RallyDelay=0;f.Engine.BallX=350;f.Engine.BallY=220;
     string target=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview");Directory.CreateDirectory(target);
     using(var b=f.Snapshot())b.Save(Path.Combine(target,"volleyball.png"),ImageFormat.Png);
     f.Location=new Point(-2200,-1200);f.RenderForTest();
    }

    using(var p=new PetForm())using(var game=new VolleyProbe(p)){
     game.Location=new Point(-2200,-1200);game.Show();game.Activate();Application.DoEvents();
     game.KeyForTest(Keys.Enter,true);game.KeyForTest(Keys.Right,true);double startX=game.Engine.Player.X;game.PumpForTest(160);
     if(game.Engine.Player.X<=startX)throw new Exception("Window right-key input");
     game.KeyForTest(Keys.Right,false);game.KeyForTest(Keys.Space,true);game.PumpForTest(120);
     if(game.Engine.Player.Y>=420)throw new Exception("Window jump-key input");
     game.KeyForTest(Keys.Space,false);game.KeyForTest(Keys.X,true);game.PumpForTest(48);
     if(game.Engine.Player.SmashTime<=0)throw new Exception("Window smash-key input");
     game.KeyForTest(Keys.X,false);game.KeyForTest(Keys.Escape,true);startX=game.Engine.Player.X;
     game.KeyForTest(Keys.Right,true);game.PumpForTest(120);
     if(game.Engine.Player.X!=startX)throw new Exception("Window pause");
     game.KeyForTest(Keys.Escape,true);game.KeyForTest(Keys.Right,true);game.PumpForTest(100);
     if(game.Engine.Player.X<=startX)throw new Exception("Window resume");
     game.Close();
    }

    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"volleyball-validation.txt"),"PASS: move, jump, court bounds, net collision, character bounce, smash, floor scoring, duplicate score protection, first-to-five, reward once, long physics simulation, desktop alpha rendering, real game-window key handlers, pause and resume.");return 0;
   }catch(Exception e){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"volleyball-validation.txt"),"FAIL: "+e);return 1;}
  }
 }
}
