using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
namespace DeskBuddy {
 public class FallingPoop {public float X,Y,Speed,Drift,Warning;public int Size;public bool Aimed;}
 public class DodgeEngine {
  public int Stage=1,Lives=3;public float X=390,Y=480,Elapsed,Invincible;public bool StageClear,Over,Won;
  public List<FallingPoop> Drops=new List<FallingPoop>();Random random;float spawn;int waves;bool awarded;
  public DodgeEngine():this(new Random()){} public DodgeEngine(int seed):this(new Random(seed)){} DodgeEngine(Random source){random=source;}
  public float Duration{get{return 15+Stage*5;}}
  public float Interval{get{return Math.Max(.12f,(Stage==1?.36f:Stage==2?.24f:.17f)-Math.Min(1,Elapsed/Duration)*.06f);}}
  public bool Next(){if(!StageClear||Over||Stage>=3)return false;Stage++;StageClear=false;Elapsed=0;spawn=.6f;waves=0;Drops.Clear();X=390;Y=480;Invincible=.7f;return true;}
  void Spawn(){
   waves++;int count=Stage==1?1:Stage==2?2:3;
   float pressure=Math.Min(1,Elapsed/Duration);
   for(int i=0;i<count;i++){
    bool aimed=i==0&&waves%(Stage==1?4:3)==0;
    int size=random.Next(22,Stage==3?43:37);
    float px=aimed?X:24+(i+(float)random.NextDouble())*(732f/count);
    Drops.Add(new FallingPoop{X=Math.Max(24,Math.Min(756,px)),Y=65-size,Size=size,Aimed=aimed,
     Warning=aimed?.55f:0,Speed=210+Stage*70+pressure*90+random.Next(80),
     Drift=Stage>=2&&!aimed?(float)(random.NextDouble()*2-1)*(Stage==3?85:55):0});
   }
  }
  public void Step(float dt,int horizontal,int vertical){
   if(Over||StageClear)return;dt=Math.Max(0,Math.Min(.03f,dt));Elapsed+=dt;Invincible=Math.Max(0,Invincible-dt);
   float diagonal=horizontal!=0&&vertical!=0?.7071f:1;
   X=Math.Max(26,Math.Min(754,X+horizontal*320*diagonal*dt));Y=Math.Max(355,Math.Min(524,Y+vertical*260*diagonal*dt));spawn-=dt;
   if(spawn<=0){spawn+=Interval;Spawn();}
   for(int i=Drops.Count-1;i>=0;i--){
    var p=Drops[i];if(p.Warning>0){p.Warning=Math.Max(0,p.Warning-dt);continue;}
    p.Y+=p.Speed*dt;p.X+=p.Drift*dt;
    if(p.X<p.Size/2){p.X=p.Size/2;p.Drift=Math.Abs(p.Drift);}else if(p.X>780-p.Size/2){p.X=780-p.Size/2;p.Drift=-Math.Abs(p.Drift);}
    if(Invincible<=0&&new RectangleF(X-17,Y-35,34,35).IntersectsWith(new RectangleF(p.X-p.Size/2,p.Y,p.Size,p.Size))){
     Lives--;Invincible=.85f;Drops.RemoveAt(i);if(Lives<=0){Over=true;return;}
    }else if(p.Y>565)Drops.RemoveAt(i);
   }
   if(Elapsed>=Duration){if(Stage==3){Won=true;Over=true;}else StageClear=true;}
  }
  public int Award(Data d,DateTime now){if(!Over||awarded)return -2;awarded=true;return Rules.DodgeReward(d,Won,now);}
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  public static void Tests(){
   var e=new DodgeEngine(37);var d=new Data();float x=e.X;e.Step(.02f,1,0);Check(e.X>x&&!e.Next()&&e.Award(d,AppClock.Now)==-2,"Dodge move and stage gate");
   e.Drops.Clear();e.Drops.Add(new FallingPoop{X=e.X,Y=e.Y-35,Size=30});e.Step(.001f,0,0);Check(e.Lives==2,"Dodge collision");
   e.Drops.Add(new FallingPoop{X=e.X,Y=e.Y-35,Size=30});e.Step(.001f,0,0);Check(e.Lives==2,"Dodge invincibility");
   for(int stage=1;stage<=3;stage++){e.Drops.Clear();e.Elapsed=e.Duration-.001f;e.Invincible=99;e.Step(.01f,0,0);if(stage<3)Check(e.StageClear&&!e.Over&&e.Next()&&e.Stage==stage+1&&e.Lives==2,"Dodge progression preserves health");}
   Check(e.Won&&e.Award(d,AppClock.Now)==50&&e.Award(d,AppClock.Now)==-2&&d.Coins==80,"Dodge reward once");
   e=new DodgeEngine(37){Lives=1};e.Drops.Add(new FallingPoop{X=e.X,Y=e.Y-35,Size=30});e.Step(.001f,0,0);Check(e.Over&&!e.Won,"Dodge loss");
   d=new Data{GamesDate=AppClock.Now.ToString("yyyy-MM-dd"),GamesPlayed=3};Check(e.Award(d,AppClock.Now)==-1&&d.Coins==30,"Dodge daily cap");
   var counts=new int[3];
   for(int stage=1;stage<=3;stage++){e=new DodgeEngine(37){Stage=stage,Invincible=99};bool aimed=false,drift=false;float minSpeed=float.MaxValue;
    for(int tick=0;tick<100;tick++){e.Step(.02f,0,0);foreach(var drop in e.Drops){aimed|=drop.Aimed;drift|=drop.Drift!=0;minSpeed=Math.Min(minSpeed,drop.Speed);}}
    counts[stage-1]=e.Drops.Count;Check(aimed&&minSpeed>=210+stage*70,"Aimed attack and increased speed");if(stage>=2)Check(drift,"Drifting attacks");
    float initial=e.Interval;e.Elapsed=e.Duration*.9f;Check(e.Interval<initial,"Pressure rises within stage");
   }
   Check(counts[0]>=3&&counts[1]>counts[0]&&counts[2]>counts[1],"Increasing rain density");
   e=new DodgeEngine(37);var warning=new FallingPoop{X=e.X,Y=e.Y-35,Size=30,Speed=300,Warning=.5f};e.Drops.Add(warning);float y=warning.Y;
   e.Step(.02f,0,0);Check(e.Lives==3&&warning.Y==y&&warning.Warning<.5f,"Warning does not move or hit");
   e=new DodgeEngine(37);for(int tick=0;tick<1000&&!e.Over;tick++)e.Step(.02f,0,0);Check(e.Over&&!e.Won,"Standing still is unsafe");
  }
 }

 public class DodgeForm:Form {
  DodgeEngine engine=new DodgeEngine();PetForm owner;Timer timer=new Timer{Interval=16};Stopwatch watch=new Stopwatch();double previous;bool started,paused,left,right,up,down;int reward=-2;
  public DodgeForm(PetForm pet){owner=pet;Icon=AppIdentity.Icon;Text="DeskBuddy · 하늘에서 똥 피하기";ClientSize=new Size(780,600);BackColor=Theme.Bg;DoubleBuffered=true;KeyPreview=true;StartPosition=FormStartPosition.CenterScreen;timer.Tick+=(s,e)=>Tick();Shown+=(s,e)=>{watch.Start();timer.Start();};Deactivate+=(s,e)=>{ClearInput();if(started&&!engine.Over)paused=true;Invalidate();};MouseDown+=(s,e)=>{if(new Rectangle(235,280,310,48).Contains(e.Location)&&(!started||paused||engine.Over||engine.StageClear))Start();};}
  public static int TestUi(){try{DodgeEngine.Tests();using(var pet=new PetForm())using(var form=new DodgeForm(pet)){var key=typeof(DodgeForm).GetMethod("OnKeyDown",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);key.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});key.Invoke(form,new object[]{new KeyEventArgs(Keys.Right)});if(!form.started||!form.right)throw new Exception("Dodge keyboard input");form.engine.Step(.02f,1,0);if(form.engine.X<=390)throw new Exception("Dodge movement");form.engine.Drops.Add(new FallingPoop{X=350,Y=180,Size=32,Speed=240});string dir=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview");System.IO.Directory.CreateDirectory(dir);using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(System.IO.Path.Combine(dir,"dodge-game.png"));}key.Invoke(form,new object[]{new KeyEventArgs(Keys.Escape)});if(!form.paused||form.right)throw new Exception("Dodge pause clears input");for(int i=1;i<=2;i++){form.engine.Elapsed=form.engine.Duration-.001f;form.engine.Invincible=99;form.engine.Step(.01f,0,0);key.Invoke(form,new object[]{new KeyEventArgs(Keys.Enter)});if(form.engine.Stage!=i+1)throw new Exception("Dodge Enter next stage");}key.Invoke(form,new object[]{new KeyEventArgs(Keys.R)});if(form.engine.Stage!=1||form.started)throw new Exception("Dodge restart");}System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"dodge-validation.txt"),"PASS: arrow input, collision and invincibility, 3-stage progression, Enter next stage, pause, restart, one-time 50-coin reward, shared daily cap, game rendering.");return 0;}catch(Exception ex){System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"dodge-validation.txt"),"FAIL: "+ex);return 1;}}
  void ClearInput(){left=right=up=down=false;}
  void Start(){if(engine.Over){engine=new DodgeEngine();reward=-2;}else if(engine.StageClear)engine.Next();started=true;paused=false;ClearInput();previous=watch.Elapsed.TotalSeconds;Invalidate();}
  void Tick(){double now=watch.Elapsed.TotalSeconds;float dt=(float)Math.Min(.08,now-previous);previous=now;if(started&&!paused&&!engine.Over&&!engine.StageClear){while(dt>0){float step=Math.Min(dt,.02f);engine.Step(step,(right?1:0)-(left?1:0),(down?1:0)-(up?1:0));dt-=step;}if(engine.Over){reward=engine.Award(Store.State,AppClock.Now);if(reward>=0)Store.Save();owner.Say(engine.Won?(reward==50?"똥 피하기 3탄 클리어! +50 G":"똥 피하기 3탄 클리어!"):"앗! 다음에는 더 잘 피할 수 있어!",false);}}Invalidate();}
  protected override bool IsInputKey(Keys k){k&=Keys.KeyCode;return k==Keys.Left||k==Keys.Right||k==Keys.Up||k==Keys.Down||base.IsInputKey(k);}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Enter)Start();else if(e.KeyCode==Keys.Escape||e.KeyCode==Keys.P){paused=!paused;ClearInput();}else if(e.KeyCode==Keys.R){engine=new DodgeEngine();started=paused=false;reward=-2;ClearInput();}else{if(e.KeyCode==Keys.Left)left=true;if(e.KeyCode==Keys.Right)right=true;if(e.KeyCode==Keys.Up)up=true;if(e.KeyCode==Keys.Down)down=true;}e.SuppressKeyPress=true;}
  protected override void OnKeyUp(KeyEventArgs e){base.OnKeyUp(e);if(e.KeyCode==Keys.Left)left=false;if(e.KeyCode==Keys.Right)right=false;if(e.KeyCode==Keys.Up)up=false;if(e.KeyCode==Keys.Down)down=false;}
  void TextAt(Graphics g,string text,Rectangle r,float size=11){using(var f=Theme.Font(size))Theme.Text(g,text,f,Theme.Ink,r,ContentAlignment.MiddleCenter);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;Theme.Fill(g,Color.FromArgb(190,221,221),0,62,780,490);for(int i=0;i<5;i++){Theme.Fill(g,Theme.Cream,40+i*160,90+i%2*40,92,18);Theme.Fill(g,Theme.Cream,62+i*160,80+i%2*40,45,12);}Theme.Fill(g,Theme.Purple,0,543,780,18);
   foreach(var p in engine.Drops){if(p.Warning>0){using(var pen=new Pen(Color.FromArgb(180,215,63,46),2)){pen.DashStyle=System.Drawing.Drawing2D.DashStyle.Dash;g.DrawLine(pen,p.X,65,p.X,541);}TextAt(g,"!",new Rectangle((int)p.X-15,69,30,28),18);continue;}float unit=p.Size/16f;var saved=g.Save();g.TranslateTransform(p.X-p.Size/2,p.Y);g.ScaleTransform(unit,unit);Theme.Fill(g,Theme.Line,6,0,4,4);Theme.Fill(g,Theme.Line,3,4,10,5);Theme.Fill(g,Theme.Line,0,9,16,7);Theme.Fill(g,Color.FromArgb(162,110,73),4,5,8,4);Theme.Fill(g,Color.FromArgb(162,110,73),1,10,14,5);Theme.Fill(g,Theme.Cream,4,10,2,2);Theme.Fill(g,Theme.Cream,10,10,2,2);g.Restore(saved);}
   if(engine.Invincible<=0||(int)(engine.Invincible*10)%2==0)PetArt.Draw(g,new Rectangle((int)engine.X-25,(int)engine.Y-50,50,50),0,left,Progression.Accessory(Store.State));
   Theme.Frame(g,new Rectangle(0,0,780,62),Theme.Cream);TextAt(g,engine.Stage+"탄 / 3    ♥ "+engine.Lives+"    남은 시간 "+Math.Max(0,(int)Math.Ceiling(engine.Duration-engine.Elapsed))+"초",new Rectangle(10,10,760,38),14);TextAt(g,"← ↑ ↓ → 이동   ENTER 시작 / 다음 탄   ESC 정지   R 재도전",new Rectangle(0,563,780,28),10);
   if(!started||paused||engine.StageClear||engine.Over){using(var b=new SolidBrush(Color.FromArgb(210,248,238,216)))g.FillRectangle(b,0,62,780,490);string title=engine.Over?(engine.Won?"3탄 모두 피했어요!":"앗! 똥을 맞았어요!"):engine.StageClear?engine.Stage+"탄 클리어!":paused?"잠깐 쉬는 중":"하늘에서 똥 피하기";TextAt(g,title,new Rectangle(30,165,720,50),22);TextAt(g,engine.Over?(reward==-1?"오늘 게임 보상을 모두 받았어요.":"+"+Math.Max(0,reward)+" G"):engine.StageClear?"체력 회복 없음! 다음 탄은 더 빠르고 많이 떨어져요!":"체력 3개로 20 / 25 / 30초! 빨간 예고선을 피하세요 · +50 G",new Rectangle(30,225,720,30));Theme.Frame(g,new Rectangle(235,280,310,48),Theme.Gold);TextAt(g,"ENTER / 클릭으로 "+(engine.Over?"재도전":engine.StageClear?"다음 탄":"시작 / 계속"),new Rectangle(235,280,310,48));}
  }
  protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
 }
}
