using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace DeskBuddy {
 public class CastleInput { public bool Left,Right,Jump,Magic; }
 public class CastleEnemy {public float X,Y,Home,Speed=65,Range=85;public int Direction=1,Health=1;public bool Alive=true;}
 public class CastleShot {public float X,Y;public int Direction;}
 public class CastleEngine {
  public const int WorldHeight=1660;
  public readonly bool Advanced;public int FirstStage{get{return Advanced?4:1;}}public int LastStage{get{return Advanced?5:3;}}public int WinCoins{get{return Advanced?100:50;}}public int Stage{get;private set;}public bool StageClear{get;private set;}
  public float DoorX{get{return Platforms[14].X+Platforms[14].Width/2;}}
  public string StageName{get{return Stage==1?"작은 성":Stage==2?"바람의 탑":Stage==3?"어둠의 성":Stage==4?"폭풍의 성":"황금 마법탑";}}
  public List<RectangleF> Platforms=new List<RectangleF>();
  public List<PointF> Stars=new List<PointF>();public List<bool> Collected=new List<bool>();
  public List<CastleEnemy> Enemies=new List<CastleEnemy>();public List<CastleShot> Shots=new List<CastleShot>();
  public float X=170,Y=WorldHeight-80,Vy;public int Facing=1,Lives=3,Count;public bool Won,Over,Grounded;
  public int MagicLeft=3;public float Invincible;float cooldown;bool jumpHeld,magicHeld,awarded;
  public CastleEngine(bool advanced=false){Advanced=advanced;Stage=FirstStage;BuildStage();}
  void BuildStage(){
   Platforms.Clear();Stars.Clear();Collected.Clear();Enemies.Clear();Shots.Clear();
   X=170;Y=WorldHeight-80;Vy=0;Facing=1;Lives=3;Count=0;Grounded=false;Invincible=0;cooldown=0;jumpHeld=false;magicHeld=false;StageClear=false;
   int gap=Stage==1?104:Stage==2?110:112;float width=Stage==1?240:Stage==2?190:Stage==3?145:Stage==4?135:115;
   int[] second={430,260,460,290,470,280,440,270,460,300,470,290,440,270};
   int[] third={420,220,420,570,380,220,420,570,390,220,420,570,380,260};
   int[] fourth={320,510,340,170,350,540,350,170,360,550,360,180,370,540};int[] fifth={300,500,320,140,330,520,340,160,350,540,360,180,370,550};
   Platforms.Add(new RectangleF(0,WorldHeight-40,780,40));
   for(int i=1;i<=14;i++){
    float center=Stage==1?(i%2==0?270:430):Stage==2?second[i-1]:Stage==3?third[i-1]:Stage==4?fourth[i-1]:fifth[i-1];float x=center-width/2,y=WorldHeight-40-i*gap;
    Platforms.Add(new RectangleF(x,y,width,16));
    if(i%4==0){Stars.Add(new PointF(center,y-28));Collected.Add(false);}
    if(Stage==1?i%3==0:i%2==0||Stage>=3&&i%3==0)Enemies.Add(new CastleEnemy{X=center+(Stage==1?30:0),Home=center,Y=y-18,Speed=Stage==1?65:Stage==2?95:Stage==3?130:Stage==4?160:185,Range=Stage==1?85:width/2-24,Health=Stage>=3?(Stage==5?3:2):1});
   }
  }
  public bool NextStage(){if(!StageClear||Over||Stage>=LastStage)return false;Stage++;BuildStage();return true;}
  public void Step(float dt,CastleInput input){
   if(Over||StageClear)return;dt=Math.Min(.03f,Math.Max(0,dt));
   Invincible=Math.Max(0,Invincible-dt);cooldown=Math.Max(0,cooldown-dt);
   int move=(input.Right?1:0)-(input.Left?1:0);if(move!=0)Facing=move;
   X=Math.Max(22,Math.Min(758,X+move*225*dt));
   if(input.Jump&&!jumpHeld&&Grounded){Vy=-555;Grounded=false;}jumpHeld=input.Jump;
   if(input.Magic&&!magicHeld&&cooldown<=0&&MagicLeft>0){MagicLeft--;Shots.Add(new CastleShot{X=X+Facing*25,Y=Y-23,Direction=Facing});cooldown=.28f;}magicHeld=input.Magic;
   float oldY=Y;Vy+=1000*dt;Y+=Vy*dt;Grounded=false;
   foreach(var p in Platforms)if(Vy>=0&&oldY<=p.Y+.1f&&Y>=p.Y&&X+17>p.Left&&X-17<p.Right){Y=p.Y;Vy=0;Grounded=true;break;}
   foreach(var enemy in Enemies)if(enemy.Alive){enemy.X+=enemy.Direction*enemy.Speed*dt;if(Math.Abs(enemy.X-enemy.Home)>enemy.Range){enemy.X=enemy.Home+enemy.Direction*enemy.Range;enemy.Direction=-enemy.Direction;}
    if(Invincible<=0&&Math.Abs(X-enemy.X)<32&&Math.Abs(Y-22-enemy.Y)<34)Damage();}
   for(int i=Shots.Count-1;i>=0;i--){var shot=Shots[i];shot.X+=shot.Direction*480*dt;bool remove=shot.X<0||shot.X>780;
    foreach(var enemy in Enemies)if(enemy.Alive&&Math.Abs(shot.X-enemy.X)<25&&Math.Abs(shot.Y-enemy.Y)<25){if(--enemy.Health<=0)enemy.Alive=false;remove=true;break;}
    if(remove)Shots.RemoveAt(i);
   }
   for(int i=0;i<Stars.Count;i++)if(!Collected[i]&&Math.Abs(X-Stars[i].X)<34&&Math.Abs(Y-22-Stars[i].Y)<40){Collected[i]=true;Count++;}
   if(Y>WorldHeight+60){Damage();X=170;Y=WorldHeight-80;Vy=0;}
   if(Count==Stars.Count&&Grounded&&Y==Platforms[14].Y&&Math.Abs(X-DoorX)<55){if(Stage==LastStage){Won=true;Over=true;}else StageClear=true;}
  }
  void Damage(){if(Invincible>0||Over)return;Lives--;Invincible=1.8f;if(Lives<=0)Over=true;}
  public int Award(Data data,DateTime now){if(!Over||awarded)return -2;awarded=true;return Rules.CastleReward(data,Won,now,Advanced);}
 }
 public class CastleForm:Form {
  public CastleEngine Engine=new CastleEngine();CastleInput input=new CastleInput();Timer timer=new Timer{Interval=16};Stopwatch watch=new Stopwatch();double previous,accumulator;
  bool started,paused;int reward=-2;PetForm owner;bool advanced;
  public CastleForm(PetForm pet,bool advanced=false){
   this.advanced=advanced;Engine=new CastleEngine(advanced);
   owner=pet;Icon=AppIdentity.Icon;Text="DeskBuddy · 마법의 성 모험";ClientSize=new Size(780,600);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(28,28,57);DoubleBuffered=true;KeyPreview=true;
   timer.Tick+=(s,e)=>Tick();Shown+=(s,e)=>{watch.Start();timer.Start();};Deactivate+=(s,e)=>{input=new CastleInput();if(started&&!Engine.Over)paused=true;Invalidate();};
   MouseDown+=(s,e)=>{if(new Rectangle(235,280,310,48).Contains(e.Location)&&(!started||paused||Engine.Over||Engine.StageClear))Start();};
  }
  void Start(){if(Engine.Over){Engine=new CastleEngine(advanced);reward=-2;}else if(Engine.StageClear)Engine.NextStage();started=true;paused=false;input=new CastleInput();accumulator=0;Invalidate();}
  void Tick(){double now=watch.Elapsed.TotalSeconds,elapsed=Math.Min(.08,now-previous);previous=now;
   if(started&&!paused&&!Engine.Over&&!Engine.StageClear){accumulator+=elapsed;while(accumulator>=1.0/120){Engine.Step(1f/120,input);accumulator-=1.0/120;}
    if(Engine.Over){reward=Engine.Award(Store.State,AppClock.Now);if(reward>=0)Store.Save();owner.Say(Engine.Won?(reward>=0?Engine.LastStage+"탄까지 클리어! +"+reward+" G":Engine.LastStage+"탄까지 클리어! 멋져!"):"다음에는 꼭 성 꼭대기까지!",false);}}
   else accumulator=0;Invalidate();
  }
  protected override bool IsInputKey(Keys keyData){Keys k=keyData&Keys.KeyCode;return k==Keys.Left||k==Keys.Right||k==Keys.Up||base.IsInputKey(keyData);}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);Keys k=e.KeyCode;
   if(k==Keys.Enter){Start();}else if(k==Keys.Escape||k==Keys.P){paused=!paused;input=new CastleInput();}
   else if(k==Keys.R){Engine=new CastleEngine(advanced);reward=-2;started=false;paused=false;input=new CastleInput();}
   else{if(k==Keys.Left||k==Keys.A)input.Left=true;if(k==Keys.Right||k==Keys.D)input.Right=true;if(k==Keys.Space||k==Keys.Up||k==Keys.W)input.Jump=true;if(k==Keys.Z||k==Keys.X)input.Magic=true;}
   e.SuppressKeyPress=true;Invalidate();
  }
  protected override void OnKeyUp(KeyEventArgs e){base.OnKeyUp(e);Keys k=e.KeyCode;if(k==Keys.Left||k==Keys.A)input.Left=false;if(k==Keys.Right||k==Keys.D)input.Right=false;if(k==Keys.Space||k==Keys.Up||k==Keys.W)input.Jump=false;if(k==Keys.Z||k==Keys.X)input.Magic=false;}
  void TextAt(Graphics g,string text,Rectangle r,float size=11){using(var font=Theme.Font(size))Theme.Text(g,text,font,Theme.Cream,r,ContentAlignment.MiddleCenter);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Graphics g=e.Graphics;
   float camera=Math.Max(0,Math.Min(CastleEngine.WorldHeight-520,Engine.Y-330));
   using(var pen=new Pen(Color.FromArgb(47,46,77),2)){for(int y=70-(int)camera%48;y<560;y+=48){g.DrawLine(pen,0,y,780,y);for(int x=(y/48%2)*48;x<780;x+=96)g.DrawLine(pen,x,y,x,y+48);}}
   var state=g.Save();g.SetClip(new Rectangle(0,62,780,498));g.TranslateTransform(0,64-camera);
   foreach(var p in Engine.Platforms){Theme.Frame(g,Rectangle.Round(p),Theme.Light);Theme.Fill(g,Theme.Purple,(int)p.X,(int)p.Y,(int)p.Width,4);}
   Theme.Frame(g,new Rectangle((int)Engine.DoorX-34,(int)Engine.Platforms[14].Y-72,68,72),Engine.Count==3?Theme.Gold:Theme.Muted);
   for(int i=0;i<Engine.Stars.Count;i++)if(!Engine.Collected[i]){PointF s=Engine.Stars[i];using(var brush=new SolidBrush(Theme.Gold))g.FillPolygon(brush,new[]{new PointF(s.X,s.Y-15),new PointF(s.X+5,s.Y-5),new PointF(s.X+15,s.Y),new PointF(s.X+5,s.Y+5),new PointF(s.X,s.Y+15),new PointF(s.X-5,s.Y+5),new PointF(s.X-15,s.Y),new PointF(s.X-5,s.Y-5)});}
   foreach(var monster in Engine.Enemies)if(monster.Alive){Theme.Fill(g,monster.Health>1?Color.FromArgb(184,119,215):Theme.Peach,(int)monster.X-18,(int)monster.Y-15,36,30);Theme.Fill(g,Theme.Line,(int)monster.X-10,(int)monster.Y-5,5,6);Theme.Fill(g,Theme.Line,(int)monster.X+5,(int)monster.Y-5,5,6);if(monster.Health>1)Theme.Fill(g,Theme.Gold,(int)monster.X-9,(int)monster.Y-20,18,3);}
   foreach(var shot in Engine.Shots){using(var brush=new SolidBrush(Color.FromArgb(151,224,249)))g.FillEllipse(brush,shot.X-7,shot.Y-7,14,14);}
   if(Engine.Invincible<=0||(int)(Engine.Invincible*10)%2==0)PetArt.Draw(g,new Rectangle((int)Engine.X-25,(int)Engine.Y-50,50,50),0,Engine.Facing<0,Progression.Accessory(Store.State));
   g.Restore(state);Theme.Fill(g,Theme.Line,0,0,780,62);TextAt(g,Engine.Stage+"탄 / "+Engine.LastStage+" · "+Engine.StageName+"   ♥ "+Engine.Lives+"   별 "+Engine.Count+" / 3   마법 "+Engine.MagicLeft+" / 3",new Rectangle(10,10,760,38),14);
   TextAt(g,"← → 이동   SPACE 점프   Z 마법 (전체 3회)   ESC 정지   R 다시 시작",new Rectangle(0,561,780,30),10);
   if(!started||paused||Engine.Over||Engine.StageClear){using(var brush=new SolidBrush(Color.FromArgb(205,28,28,57)))g.FillRectangle(brush,0,62,780,498);
    string title=Engine.Over?(Engine.Won?Engine.LastStage+"탄까지 클리어!":"다시 도전해요!"):Engine.StageClear?Engine.Stage+"탄 클리어!":paused?"잠깐 쉬는 중":"마법의 성 · "+Engine.FirstStage+"~"+Engine.LastStage+"탄 모험";
    TextAt(g,title,new Rectangle(30,165,720,50),22);
    TextAt(g,Engine.Over?(reward==-1?"오늘 보상은 모두 받았어요.":"+"+Math.Max(0,reward)+" G"):Engine.StageClear?(Engine.Stage+1)+"탄에서는 더 좁은 발판과 빠른 몬스터가 기다려요.":"각 탄 별 3개를 모아요. "+Engine.LastStage+"탄까지 클리어하면 +"+Engine.WinCoins+" G!",new Rectangle(30,225,720,30));
    Theme.Frame(g,new Rectangle(235,280,310,48),Theme.Purple);TextAt(g,"ENTER / 클릭으로 "+(Engine.Over?Engine.FirstStage+"탄부터 재도전":Engine.StageClear?(Engine.Stage+1)+"탄 시작":"시작 / 계속"),new Rectangle(235,280,310,48));
   }
  }
  protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
 }
 public static class CastleTests {
  static void Check(bool ok,string text){if(!ok)throw new Exception("Castle: "+text);}
  public static void Run(){
   var e=new CastleEngine();for(int i=0;i<60;i++)e.Step(1f/120,new CastleInput());Check(e.Grounded,"floor landing");
   float y=e.Y,x=e.X;e.Step(1f/120,new CastleInput{Jump=true,Right=true});Check(e.Y<y&&e.X>x,"jump and move");
   e=new CastleEngine();e.X=430;e.Y=e.Platforms[1].Y-1;e.Vy=100;e.Step(.02f,new CastleInput());Check(e.Grounded&&e.Y==e.Platforms[1].Y,"platform landing");
   e=new CastleEngine();e.X=e.Enemies[0].X-35;e.Y=e.Enemies[0].Y+23;e.Step(.03f,new CastleInput{Magic=true});Check(!e.Enemies[0].Alive,"magic hit");
   e=new CastleEngine();e.X=e.Stars[0].X;e.Y=e.Stars[0].Y+22;e.Step(.001f,new CastleInput());e.Step(.001f,new CastleInput());Check(e.Count==1,"collect once");
   var d=new Data();Check(!e.NextStage(),"no stage skip");
   e.Count=3;e.X=e.DoorX;e.Y=e.Platforms[14].Y;e.Vy=0;e.Step(.001f,new CastleInput());Check(e.StageClear&&!e.Won&&!e.Over,"stage one clear");
   Check(e.Award(d,AppClock.Now)==-2&&d.Coins==30&&d.GamesPlayed==0,"no intermediate reward");
   float stopped=e.X;e.Step(.03f,new CastleInput{Right=true});Check(e.X==stopped,"clear screen freezes play");
   Check(e.NextStage()&&e.Stage==2&&e.Count==0&&e.Lives==3&&!e.StageClear&&e.Shots.Count==0,"stage two reset");
   Check(e.Platforms[1].Width==190&&e.Enemies.Count==7&&e.Enemies[0].Speed==95,"stage two harder");
   e.Count=3;e.X=e.DoorX;e.Y=e.Platforms[14].Y;e.Step(.001f,new CastleInput());Check(e.StageClear&&e.Award(d,AppClock.Now)==-2&&d.GamesPlayed==0,"stage two clear without reward");
   Check(e.NextStage()&&e.Stage==3&&e.Platforms[1].Width==145&&e.Enemies.Count>7&&e.Enemies[0].Health==2,"stage three harder");
   e.Count=3;e.X=e.DoorX;e.Y=e.Platforms[14].Y;e.Step(.001f,new CastleInput());Check(e.Won&&e.Over&&!e.StageClear&&!e.NextStage(),"final win");
   Check(e.Award(d,AppClock.Now)==50&&e.Award(d,AppClock.Now)==-2&&d.Coins==80&&d.GamesPlayed==1,"fifty coins once");
   for(int i=1;i<e.Platforms.Count;i++)Check(e.Platforms[i-1].Y-e.Platforms[i].Y<555f*555/2000,"reachable heights");
   e=new CastleEngine();e.X=e.Enemies[0].X;e.Y=e.Enemies[0].Y+22;e.Step(.001f,new CastleInput());int lives=e.Lives;e.Step(.001f,new CastleInput());Check(lives==2&&e.Lives==2,"damage invincibility");
   e.Lives=1;e.Invincible=0;e.X=e.Enemies[0].X;e.Y=e.Enemies[0].Y+22;e.Step(.001f,new CastleInput());Check(e.Over&&!e.Won,"loss");
   d=new Data();Check(e.Award(d,AppClock.Now)==5&&d.Coins==35,"failure reward unchanged");
   d=new Data{GamesDate=AppClock.Now.ToString("yyyy-MM-dd"),GamesPlayed=3};Check(Rules.CastleReward(d,true,AppClock.Now)==-1&&d.Coins==30,"shared daily limit");
   Check(Rules.CastleReward(d,true,AppClock.Now.AddDays(1))==50&&d.GamesPlayed==1,"castle daily reset");
   var limited=new CastleEngine();limited.Enemies.Clear();for(int n=0;n<4;n++){limited.Step(.01f,new CastleInput{Magic=true});for(int t=0;t<12;t++)limited.Step(.03f,new CastleInput());}Check(limited.MagicLeft==0,"magic limited to three casts");limited.Count=3;limited.X=limited.DoorX;limited.Y=limited.Platforms[14].Y;limited.Vy=0;limited.Step(.001f,new CastleInput());Check(limited.NextStage()&&limited.MagicLeft==0,"magic budget persists between stages");Check(new CastleEngine().MagicLeft==3,"restart restores magic");
   var extra=new CastleEngine(true);d=new Data();Check(extra.Stage==4&&extra.Platforms[1].Width==135,"advanced stage four");extra.Count=3;extra.X=extra.DoorX;extra.Y=extra.Platforms[14].Y;extra.Step(.001f,new CastleInput());Check(extra.StageClear&&extra.Award(d,AppClock.Now)==-2&&extra.NextStage(),"four then five");Check(extra.Stage==5&&extra.Enemies[0].Health==3&&extra.MagicLeft==3,"harder stage five");extra.Count=3;extra.X=extra.DoorX;extra.Y=extra.Platforms[14].Y;extra.Step(.001f,new CastleInput());Check(extra.Won&&extra.Award(d,AppClock.Now)==100&&extra.Award(d,AppClock.Now)==-2&&d.Coins==130,"advanced hundred coins once");
   PlayableCourse();PlayableCourse(true);
  }
  static void PlayableCourse(bool advanced=false){
   var e=new CastleEngine(advanced);
   for(int stage=e.FirstStage;stage<=e.LastStage;stage++){
    foreach(var enemy in e.Enemies)enemy.Alive=false;
    for(int t=0;t<90;t++)e.Step(1f/120,new CastleInput());
    e.X=e.Platforms[1].X+e.Platforms[1].Width/2;
    for(int p=1;p<=14;p++){
     float target=e.Platforms[p].X+e.Platforms[p].Width/2;
     for(int t=0;t<180;t++){
      e.Step(1f/120,new CastleInput{Jump=t==0,Right=e.X<target-1,Left=e.X>target+1});
      if(e.Grounded&&e.Y==e.Platforms[p].Y)break;
     }
     Check(e.Grounded&&e.Y==e.Platforms[p].Y,"actual jump path stage "+stage+" platform "+p);
    }
    Check(e.Count==3,"stars reachable stage "+stage);
    if(stage<e.LastStage)Check(e.StageClear&&e.NextStage(),"playthrough transition");else Check(e.Won,"playthrough final victory");
   }
   var armored=new CastleEngine();
   for(int stage=1;stage<3;stage++){armored.Count=3;armored.X=armored.DoorX;armored.Y=armored.Platforms[14].Y;armored.Step(.001f,new CastleInput());armored.NextStage();}
   var monster=armored.Enemies[0];armored.Shots.Add(new CastleShot{X=monster.X,Y=monster.Y,Direction=1});armored.Step(.001f,new CastleInput());Check(monster.Alive&&monster.Health==1,"armored enemy first hit");
   armored.Shots.Add(new CastleShot{X=monster.X,Y=monster.Y,Direction=1});armored.Step(.001f,new CastleInput());Check(!monster.Alive,"armored enemy second hit");
  }
  public static int Execute(){
   string oldPath=Store.FilePath;Data oldState=Store.State;string folderTest=Path.Combine(Path.GetTempPath(),"DeskBuddy-castle-"+Guid.NewGuid().ToString("N"));
   Directory.CreateDirectory(folderTest);Store.FilePath=Path.Combine(folderTest,"data.json");Store.State=new Data();
   try{Run();using(var pet=new PetForm())using(var game=new CastleProbe(pet)){
    game.Location=new Point(-2200,-1200);game.Show();Application.DoEvents();game.Key(Keys.Enter,true);game.Pump(250);float x=game.Engine.X;
    game.Key(Keys.Right,true);game.Pump(120);Check(game.Engine.X>x,"window movement");game.Key(Keys.Right,false);
    game.Key(Keys.Space,true);game.Pump(100);Check(game.Engine.Vy<0,"window jump");game.Key(Keys.Space,false);
    game.Key(Keys.Z,true);game.Pump(160);Check(game.Engine.MagicLeft==2,"window magic uses one charge");game.Key(Keys.Z,false);
    game.Key(Keys.Escape,true);x=game.Engine.X;game.Key(Keys.Right,true);game.Pump(90);Check(game.Engine.X==x,"window pause");
    game.Key(Keys.Escape,true);game.Key(Keys.Right,true);game.Pump(90);Check(game.Engine.X>x,"window resume");game.Key(Keys.Right,false);
    game.Key(Keys.R,true);Check(game.Engine.Lives==3&&game.Engine.Count==0,"window restart");game.Key(Keys.Enter,true);game.Pump(50);
    for(int stage=1;stage<=3;stage++){
     game.Engine.Count=3;game.Engine.X=game.Engine.DoorX;game.Engine.Y=game.Engine.Platforms[14].Y;game.Engine.Vy=0;
     if(stage<3){game.Engine.Step(.001f,new CastleInput());game.Key(Keys.Enter,true);Check(game.Engine.Stage==stage+1&&!game.Engine.StageClear&&game.Engine.Lives==3,"window next stage");}
     else game.Pump(90);
    }
    Check(game.Engine.Won&&Store.State.Coins==80&&Store.State.GamesPlayed==1,"window final fifty coins");game.Pump(40);Store.State=new Data();Store.Load();Check(Store.State.Coins==80&&Store.State.GamesPlayed==1,"reward persisted once");
    game.Key(Keys.R,true);Check(game.Engine.Stage==1&&!game.Engine.Over,"restart returns to stage one");game.Key(Keys.Enter,true);game.Pump(50);
    string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview");Directory.CreateDirectory(folder);using(var bitmap=new Bitmap(game.Width,game.Height)){game.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine(folder,"castle.png"));}game.Close();
   }File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"castle-validation.txt"),"PASS: three sequential stages, increasing difficulty, actual jump path through all maps, armored enemies, intermediate clear without reward, final 50 coins once, shared daily limit/reset, landing, movement, magic, stars, damage invincibility, win/loss, window keys, pause/resume, next stage, restart from stage one.");return 0;
   }catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"castle-validation.txt"),"FAIL: "+ex);return 1;}
   finally{Store.FilePath=oldPath;Store.State=oldState;foreach(string file in Directory.GetFiles(folderTest))File.Delete(file);Directory.Delete(folderTest);}
  }
 }
 public class CastleProbe:CastleForm {
  public CastleProbe(PetForm pet):base(pet){}
  public void Key(Keys key,bool down){if(down)base.OnKeyDown(new KeyEventArgs(key));else base.OnKeyUp(new KeyEventArgs(key));}
  public void Pump(int ms){var until=DateTime.UtcNow.AddMilliseconds(ms);while(DateTime.UtcNow<until){Application.DoEvents();System.Threading.Thread.Sleep(4);}}
 }
}
