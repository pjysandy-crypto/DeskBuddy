using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DeskBuddy {
 public class BasketballEngine {
  public const float Radius=11,LaunchX=104,LaunchY=478,Gravity=620;
  public int Stage{get;private set;}public int Goals,ShotsUsed;public bool StageClear,Over,Won,ShotActive;
  public float Time,BallX=LaunchX,BallY=LaunchY,Vx,Vy,Cooldown;float flight;bool awarded;
  public string Message="조준하고 슛!";
  public int Target{get{return Stage+1;}}public int ShotLimit{get{return 7+Stage;}}
  public float HoopWidth{get{return Stage==1?92:Stage==2?78:66;}}
  public float HoopSpeed{get{return Stage==1?.85f:Stage==2?1.25f:1.65f;}}
  public float HoopX{get{return 570+130*(float)Math.Sin(Time*HoopSpeed);}}
  public float HoopY{get{return Stage==1?270:Stage==2?250:235+22*(float)Math.Sin(Time*.9f);}}
  public float Wind{get{return Stage==1?0:(Stage==2?38:78)*(float)Math.Sin(Time*.8f);}}
  public RectangleF Defender{get{return Stage<2?RectangleF.Empty:new RectangleF(358,215+95*(float)Math.Sin(Time*1.15f),24,88);}}
  public RectangleF Drone{get{return Stage<3?RectangleF.Empty:new RectangleF(470+65*(float)Math.Sin(Time*1.4f),140+50*(float)Math.Cos(Time*1.1f),64,18);}}
  public BasketballEngine(){Stage=1;}
  void ResetStage(){Goals=ShotsUsed=0;StageClear=Over=Won=ShotActive=false;Time=flight=Cooldown=0;BallX=LaunchX;BallY=LaunchY;Vx=Vy=0;Message="조준하고 슛!";}
  public bool Next(){if(!StageClear||Over||Stage>=3)return false;Stage++;ResetStage();return true;}
  public bool Shoot(float angle,float power){
   if(Over||StageClear||ShotActive||Cooldown>0||ShotsUsed>=ShotLimit)return false;
   angle=Math.Max(40,Math.Min(78,angle));power=Math.Max(600,Math.Min(840,power));double radians=angle*Math.PI/180;
   BallX=LaunchX;BallY=LaunchY;Vx=power*(float)Math.Cos(radians);Vy=-power*(float)Math.Sin(radians);flight=0;ShotActive=true;ShotsUsed++;Message="날아가는 중!";return true;
  }
  static bool Clip(float delta,float distance,ref float low,ref float high){if(Math.Abs(delta)<.00001f)return distance>=0;float t=distance/delta;if(delta<0){if(t>high)return false;low=Math.Max(low,t);}else{if(t<low)return false;high=Math.Min(high,t);}return true;}
  public static bool SegmentHits(float x0,float y0,float x1,float y1,RectangleF r){if(r.IsEmpty)return false;r.Inflate(Radius,Radius);float low=0,high=1,dx=x1-x0,dy=y1-y0;return Clip(-dx,x0-r.Left,ref low,ref high)&&Clip(dx,r.Right-x0,ref low,ref high)&&Clip(-dy,y0-r.Top,ref low,ref high)&&Clip(dy,r.Bottom-y0,ref low,ref high);}
  public void Step(float dt){
   if(Over||StageClear)return;dt=Math.Max(0,Math.Min(.02f,dt));float oldHoopX=HoopX,oldHoopY=HoopY;Time+=dt;Cooldown=Math.Max(0,Cooldown-dt);if(!ShotActive)return;
   float ox=BallX,oy=BallY;Vx+=Wind*dt;Vy+=Gravity*dt;BallX+=Vx*dt;BallY+=Vy*dt;flight+=dt;
   if(SegmentHits(ox,oy,BallX,BallY,Defender)||SegmentHits(ox,oy,BallX,BallY,Drone)){Finish(false,"수비에 막혔어요!");return;}
   float before=oy-oldHoopY,after=BallY-HoopY;
   if(Vy>0&&before<0&&after>=0){float portion=-before/(after-before),crossX=ox+(BallX-ox)*portion,hx=oldHoopX+(HoopX-oldHoopX)*portion;
    if(Math.Abs(crossX-hx)<HoopWidth/2-Radius){Finish(true,"골인! NICE SHOT!");return;}
    if(Math.Abs(Math.Abs(crossX-hx)-HoopWidth/2)<Radius+4){Vy=-Math.Abs(Vy)*.5f;BallY=HoopY-Radius-4;Message="림을 맞았어요!";}
   }
   var board=new RectangleF(HoopX+HoopWidth/2+12,HoopY-64,8,78);
   if(Vx>0&&SegmentHits(ox,oy,BallX,BallY,board)){BallX=board.Left-Radius-1;Vx=-Math.Abs(Vx)*.65f;Message="백보드에 맞았어요!";}
   if(BallY>548||BallX<-40||BallX>830||BallY<-180||flight>4.5f)Finish(false,"아깝다! 다시 조준해요.");
  }
  void Finish(bool goal,string message){ShotActive=false;Cooldown=.65f;BallX=LaunchX;BallY=LaunchY;Message=message;if(goal)Goals++;if(Goals>=Target){if(Stage==3){Won=Over=true;}else StageClear=true;}else if(ShotsUsed>=ShotLimit)Over=true;}
  public int Award(Data data,DateTime now){if(!Over||awarded)return -2;awarded=true;return Rules.BasketballReward(data,Won,now);}
  static void Check(bool ok,string message){if(!ok)throw new Exception("Basketball: "+message);}
  static void PutThroughHoop(BasketballEngine e){e.ShotActive=true;e.BallX=e.HoopX;e.BallY=e.HoopY-1;e.Vx=0;e.Vy=200;e.Step(.01f);}
  public static void Tests(){
   var e=new BasketballEngine();var data=new Data();DateTime now=AppClock.Now;Check(!e.Next()&&e.Award(data,now)==-2,"no skip or early reward");float hx=e.HoopX;e.Step(.02f);Check(e.HoopX!=hx,"moving hoop");Check(e.Shoot(62,720)&&!e.Shoot(62,720)&&e.ShotsUsed==1,"single shot input");float x=e.BallX;e.Step(.02f);Check(e.BallX>x&&e.BallY<LaunchY,"projectile launch");
   e=new BasketballEngine();e.ShotActive=true;e.BallX=e.HoopX;e.BallY=e.HoopY+1;e.Vy=-200;e.Step(.01f);Check(e.Goals==0,"upward pass is not a basket");
   e=new BasketballEngine();for(int stage=1;stage<=3;stage++){Check(e.Stage==stage,"stage progression");for(int goal=0;goal<e.Target;goal++)PutThroughHoop(e);if(stage<3){Check(e.StageClear&&!e.Over&&e.Award(data,now)==-2&&data.GamesPlayed==0,"no intermediate reward");float width=e.HoopWidth,speed=e.HoopSpeed;Check(e.Next()&&e.HoopWidth<width&&e.HoopSpeed>speed&&e.ShotsUsed==0,"difficulty and stage reset");}}
   Check(e.Won&&e.Over&&!e.Next()&&e.Award(data,now)==50&&e.Award(data,now)==-2&&data.Coins==80,"final reward once");
   e=new BasketballEngine();e.ShotsUsed=e.ShotLimit;e.ShotActive=true;e.BallX=100;e.BallY=549;e.Step(.01f);data=new Data();Check(e.Over&&!e.Won&&e.Award(data,now)==5,"shot exhaustion");data=new Data{GamesDate=now.ToString("yyyy-MM-dd"),GamesPlayed=3};e.awarded=false;Check(e.Award(data,now)==-1&&data.Coins==30,"shared daily cap");e.awarded=false;Check(e.Award(data,now.AddDays(1))==5&&data.GamesPlayed==1,"daily reset");
   Check(SegmentHits(100,250,600,250,new RectangleF(350,220,24,88)),"swept blocker collision");
   for(int stage=1;stage<=3;stage++){bool possible=false;for(int phase=0;phase<12&&!possible;phase++)for(int angle=44;angle<=76&&!possible;angle+=4)for(int power=620;power<=840&&!possible;power+=20){var shot=new BasketballEngine();shot.Stage=stage;shot.Time=phase*.5f;shot.Shoot(angle,power);for(int frame=0;frame<600&&shot.ShotActive;frame++)shot.Step(1f/120);possible=shot.Goals>0;}Check(possible,"real scoring path stage "+stage);}
  }
 }
 public class BasketballForm:Form {
  public BasketballEngine Engine=new BasketballEngine();readonly PetForm owner;readonly Timer timer=new Timer{Interval=16};readonly Stopwatch watch=new Stopwatch();readonly Queue<PointF> trail=new Queue<PointF>();
  double previous;bool started,paused,left,right,charging;float angle=62,power=720,chargeTime;int reward=-2;
  readonly Rectangle actionButton=new Rectangle(235,284,310,48);
  public BasketballForm(PetForm pet){owner=pet;Text="DeskBuddy · 움직이는 골대 농구";Icon=AppIdentity.Icon;ClientSize=new Size(780,620);BackColor=Theme.Bg;DoubleBuffered=true;KeyPreview=true;StartPosition=FormStartPosition.CenterScreen;
   timer.Tick+=(s,e)=>Tick();Shown+=(s,e)=>{watch.Start();previous=watch.Elapsed.TotalSeconds;timer.Start();};Deactivate+=(s,e)=>{ClearInput();if(started&&!Engine.Over)paused=true;Invalidate();};MouseMove+=(s,e)=>{if(Playing&&!charging&&!Engine.ShotActive){float dx=e.X-BasketballEngine.LaunchX,dy=BasketballEngine.LaunchY-e.Y;if(dx>0&&dy>0)angle=Math.Max(40,Math.Min(78,(float)(Math.Atan2(dy,dx)*180/Math.PI)));}};
   MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;if(actionButton.Contains(e.Location)&&(!started||paused||Engine.Over||Engine.StageClear))Start();else if(Playing&&!Engine.ShotActive)Charge();};MouseUp+=(s,e)=>{if(e.Button==MouseButtons.Left)Release();};
  }
  bool Playing{get{return started&&!paused&&!Engine.Over&&!Engine.StageClear;}}
  void ClearInput(){left=right=charging=false;chargeTime=0;}
  void Reset(){Engine=new BasketballEngine();started=paused=false;reward=-2;angle=62;power=720;ClearInput();trail.Clear();previous=watch.Elapsed.TotalSeconds;Invalidate();}
  void Start(){if(Engine.Over){Engine=new BasketballEngine();reward=-2;trail.Clear();}else if(Engine.StageClear){Engine.Next();trail.Clear();}started=true;paused=false;ClearInput();power=720;previous=watch.Elapsed.TotalSeconds;Invalidate();}
  void Charge(){if(!Playing||Engine.ShotActive||Engine.Cooldown>0||charging)return;charging=true;chargeTime=0;power=600;}
  void Release(){if(!charging)return;charging=false;if(Playing&&Engine.Shoot(angle,power))trail.Clear();Invalidate();}
  void Tick(){double now=watch.Elapsed.TotalSeconds;float dt=(float)Math.Max(0,Math.Min(.08,now-previous));previous=now;if(Playing){angle=Math.Max(40,Math.Min(78,angle+((left?1:0)-(right?1:0))*40*dt));if(charging){chargeTime+=dt;power=600+240*(1-(float)Math.Cos(chargeTime*Math.PI*1.25))/2;}while(dt>0){float step=Math.Min(dt,1f/120);Engine.Step(step);dt-=step;}if(Engine.ShotActive){trail.Enqueue(new PointF(Engine.BallX,Engine.BallY));while(trail.Count>12)trail.Dequeue();}else trail.Clear();if(Engine.StageClear||Engine.Over)ClearInput();if(Engine.Over){reward=Engine.Award(Store.State,AppClock.Now);if(reward>=0)Store.Save();owner.Say(Engine.Won?"농구 3탄 클리어!"+(reward>=0?" +"+reward+" G":""):"농구 아깝다! 다음에는 골인!",false);}}Invalidate();}
  protected override bool IsInputKey(Keys k){k&=Keys.KeyCode;return k==Keys.Left||k==Keys.Right||base.IsInputKey(k);}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Enter){if(!Playing)Start();}else if(e.KeyCode==Keys.R)Reset();else if(e.KeyCode==Keys.Escape||e.KeyCode==Keys.P){if(started&&!Engine.Over&&!Engine.StageClear){paused=!paused;ClearInput();previous=watch.Elapsed.TotalSeconds;}}else if(e.KeyCode==Keys.Left||e.KeyCode==Keys.A)left=true;else if(e.KeyCode==Keys.Right||e.KeyCode==Keys.D)right=true;else if(e.KeyCode==Keys.Space)Charge();e.SuppressKeyPress=true;Invalidate();}
  protected override void OnKeyUp(KeyEventArgs e){base.OnKeyUp(e);if(e.KeyCode==Keys.Left||e.KeyCode==Keys.A)left=false;if(e.KeyCode==Keys.Right||e.KeyCode==Keys.D)right=false;if(e.KeyCode==Keys.Space)Release();e.SuppressKeyPress=true;}
  void TextAt(Graphics g,string text,Rectangle box,float size=11,Color? color=null){using(var f=Theme.Font(size))Theme.Text(g,text,f,color??Theme.Ink,box,ContentAlignment.MiddleCenter);}
  static void Ball(Graphics g,float x,float y,int radius){using(var b=new SolidBrush(Color.FromArgb(243,150,63)))g.FillEllipse(b,x-radius,y-radius,radius*2,radius*2);using(var p=new Pen(Theme.Line,2)){g.DrawEllipse(p,x-radius,y-radius,radius*2,radius*2);g.DrawLine(p,x-radius,y,x+radius,y);g.DrawArc(p,x-radius/2,y-radius,radius,radius*2,90,180);}}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;Theme.Fill(g,Color.FromArgb(209,229,223),0,62,780,490);for(int y=90;y<520;y+=55)using(var p=new Pen(Color.FromArgb(188,211,204)))g.DrawLine(p,0,y,780,y);Theme.Fill(g,Color.FromArgb(211,164,116),0,530,780,22);using(var p=new Pen(Theme.Cream,3)){g.DrawLine(p,0,532,780,532);g.DrawArc(p,14,435,180,150,180,180);}
   float hx=Engine.HoopX,hy=Engine.HoopY,hw=Engine.HoopWidth;Theme.Fill(g,Theme.Line,(int)(hx+hw/2+15),(int)hy,8,(int)(530-hy));Theme.Frame(g,new Rectangle((int)(hx+hw/2+12),(int)hy-64,10,78),Theme.Cream);using(var p=new Pen(Theme.Cream,2)){for(int i=0;i<6;i++)g.DrawLine(p,hx-hw/2+i*hw/5,hy+6,hx-hw/3+i*hw/7.5f,hy+42);g.DrawLine(p,hx-hw/3,hy+42,hx+hw/3,hy+42);}using(var p=new Pen(Color.FromArgb(210,94,66),5))g.DrawLine(p,hx-hw/2,hy,hx+hw/2,hy);
   if(Engine.Stage>=2){Theme.Frame(g,Rectangle.Round(Engine.Defender),Theme.Purple);TextAt(g,"X",Rectangle.Round(Engine.Defender),12,Theme.Cream);}if(Engine.Stage>=3){Theme.Frame(g,Rectangle.Round(Engine.Drone),Theme.Peach);using(var p=new Pen(Theme.Line,2)){g.DrawLine(p,Engine.Drone.Left-12,Engine.Drone.Top-4,Engine.Drone.Left+16,Engine.Drone.Top-4);g.DrawLine(p,Engine.Drone.Right-16,Engine.Drone.Top-4,Engine.Drone.Right+12,Engine.Drone.Top-4);}}
   PetArt.Draw(g,new Rectangle(42,458,68,68),0,false,Progression.Accessory(Store.State));if(!Engine.ShotActive){double a=angle*Math.PI/180;using(var p=new Pen(Theme.Purple,3)){p.DashStyle=System.Drawing.Drawing2D.DashStyle.Dash;g.DrawLine(p,BasketballEngine.LaunchX,BasketballEngine.LaunchY,BasketballEngine.LaunchX+70*(float)Math.Cos(a),BasketballEngine.LaunchY-70*(float)Math.Sin(a));}Ball(g,BasketballEngine.LaunchX,BasketballEngine.LaunchY,11);}else{int n=0;foreach(var t in trail){using(var b=new SolidBrush(Color.FromArgb(30+n++*12,243,150,63)))g.FillEllipse(b,t.X-4,t.Y-4,8,8);}Ball(g,Engine.BallX,Engine.BallY,11);}
   Theme.Frame(g,new Rectangle(0,0,780,62),Theme.Cream);TextAt(g,Engine.Stage+"탄 / 3   골인 "+Engine.Goals+" / "+Engine.Target+"   남은 공 "+(Engine.ShotLimit-Engine.ShotsUsed),new Rectangle(10,10,760,38),15);TextAt(g,"조준 "+(int)angle+"°    슛 세기 "+(int)((power-600)/240*100)+"%    "+(Engine.Stage==1?"바람 없음":"바람 "+(Engine.Wind<0?"←":"→")+" "+Math.Abs((int)Engine.Wind)),new Rectangle(12,70,756,30),11);TextAt(g,Engine.Message,new Rectangle(150,490,560,32),12);
   Theme.Frame(g,new Rectangle(20,566,210,18),Theme.Cream);Theme.Fill(g,Theme.Gold,24,570,(int)(202*(power-600)/240),10);TextAt(g,"← → 조준 · SPACE 누르고 떼면 슛 · 마우스 조준/길게 클릭",new Rectangle(242,559,525,30),9);TextAt(g,"ENTER 시작/다음 탄 · ESC/P 정지 · R 처음부터",new Rectangle(20,588,740,26),10);
   if(!Playing){using(var b=new SolidBrush(Color.FromArgb(230,248,238,216)))g.FillRectangle(b,0,62,780,490);string title=Engine.Over?(Engine.Won?"농구 3탄 모두 골인!":"공을 모두 사용했어요"):Engine.StageClear?Engine.Stage+"탄 클리어!":paused?"잠깐 쉬는 중":"움직이는 골대 농구";TextAt(g,title,new Rectangle(30,168,720,52),22);string detail=Engine.Over?(reward==-1?"오늘 게임 보상을 모두 받았어요.":"+"+Math.Max(0,reward)+" G"):Engine.StageClear?"다음 탄은 골대가 더 빠르고 좁아요!":"1탄 2골 → 2탄 3골 → 3탄 4골 · 모두 성공하면 +50 G";TextAt(g,detail,new Rectangle(30,229,720,35),11);Theme.Frame(g,actionButton,Theme.Gold);TextAt(g,"ENTER / 클릭으로 "+(Engine.Over?"다시 도전":Engine.StageClear?"다음 탄":"시작 / 계속"),actionButton,12);TextAt(g,"SPACE를 눌러 세기를 고르고 떼면 슛!",new Rectangle(30,350,720,30),12);TextAt(g,"2탄: 움직이는 수비 + 바람 / 3탄: 추가 드론 + 강한 바람",new Rectangle(30,390,720,30),10);}
  }
  protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
  public static int TestUi(){string oldPath=Store.FilePath;Data old=Store.State;string folder=Path.Combine(Path.GetTempPath(),"DeskBuddy-basketball-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);Store.FilePath=Path.Combine(folder,"data.json");Store.State=new Data();try{BasketballEngine.Tests();using(var pet=new PetForm())using(var form=new BasketballForm(pet)){form.OnKeyDown(new KeyEventArgs(Keys.Enter));form.OnKeyDown(new KeyEventArgs(Keys.Left));if(!form.Playing||!form.left)throw new Exception("Basketball aim keys");form.OnKeyDown(new KeyEventArgs(Keys.Space));form.OnKeyDown(new KeyEventArgs(Keys.Space));form.power=720;form.OnKeyUp(new KeyEventArgs(Keys.Space));if(!form.Engine.ShotActive||form.Engine.ShotsUsed!=1)throw new Exception("Basketball hold/release shot");form.OnKeyDown(new KeyEventArgs(Keys.Escape));if(!form.paused||form.charging||form.left)throw new Exception("Basketball pause input");form.OnKeyDown(new KeyEventArgs(Keys.Enter));form.Engine.StageClear=true;form.OnKeyDown(new KeyEventArgs(Keys.Enter));if(form.Engine.Stage!=2)throw new Exception("Basketball next stage");form.OnKeyDown(new KeyEventArgs(Keys.R));if(form.Engine.Stage!=1||form.started)throw new Exception("Basketball restart");string previews=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview");Directory.CreateDirectory(previews);for(int stage=1;stage<=3;stage++){form.started=true;form.Engine.Time=1;using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(previews,"basketball-stage-"+stage+".png"));}if(stage<3){form.Engine.StageClear=true;form.Start();}}}File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"basketball-validation.txt"),"PASS: projectile paths, moving hoops, blockers, three stages, rewards and shared daily cap, aim/charge/release, pause, restart and rendering.");return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"basketball-validation.txt"),"FAIL: "+ex);return 1;}finally{Store.FilePath=oldPath;Store.State=old;Directory.Delete(folder,true);}}
 }
}
