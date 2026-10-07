using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace DeskBuddy {
 public class ShopItem {
  public string Name,Kind,Slot,Description;public int Price,Fullness,Joy;
  public ShopItem(string name,string kind,string slot,int price,string description,int fullness=0,int joy=0){Name=name;Kind=kind;Slot=slot;Price=price;Description=description;Fullness=fullness;Joy=joy;}
 }
 public static class Living {
  public static readonly ShopItem[] Items={
   new ShopItem("축제 리본","캐릭터","옷",100,"Lv.4 · 세 가지 색의 축제 리본"),new ShopItem("전설 왕관","캐릭터","옷",250,"Lv.10 · 전설의 보석 왕관"),new ShopItem("축제 벽지","방","벽지",110,"Lv.4 · 풍선과 색종이의 방"),new ShopItem("왕실 벽지","방","벽지",220,"Lv.10 · 보랏빛 황금 왕실"),
   new ShopItem("기본","캐릭터","옷",0,"원래 모습 그대로"),
   new ShopItem("민트 리본","캐릭터","옷",40,"반짝이는 민트빛 새틴"),new ShopItem("별빛 모자","캐릭터","옷",70,"작은 별을 담은 마법 모자"),
   new ShopItem("하트 친구","캐릭터","옷",100,"둥실 떠다니는 사랑 한 조각"),new ShopItem("왕관","캐릭터","옷",150,"보석으로 장식한 작은 왕관"),
   new ShopItem("딸기 리본","캐릭터","옷",50,"딸기 우유빛 커다란 리본"),new ShopItem("둥근 안경","캐릭터","옷",65,"똑똑해 보이는 동그란 안경"),
   new ShopItem("꽃 화관","캐릭터","옷",85,"봄날의 꽃을 엮은 화관"),new ShopItem("포근한 목도리","캐릭터","옷",75,"따뜻한 체크 목도리"),
   new ShopItem("토끼 머리띠","캐릭터","옷",110,"쫑긋! 분홍 토끼 귀"),new ShopItem("별빛 날개","캐릭터","옷",140,"밤하늘을 닮은 작은 날개"),
   new ShopItem("헤드폰","캐릭터","옷",120,"음악과 함께하는 산책"),
   new ShopItem("기본 벽지","방","벽지",0,"햇살 가득한 크림색 방"),new ShopItem("민트 숲 벽지","방","벽지",60,"초록빛 숲속에서 쉬어요"),
   new ShopItem("복숭아 벽지","방","벽지",60,"포근한 복숭아 우유빛"),new ShopItem("보랏빛 밤 벽지","방","벽지",80,"작은 별이 빛나는 밤"),
   new ShopItem("기본 러그","방","러그",0,"따뜻한 나무색 러그"),new ShopItem("구름 러그","방","러그",55,"폭신한 구름 위에 앉아요"),
   new ShopItem("별무늬 러그","방","러그",65,"발밑에 펼쳐지는 별자리"),new ShopItem("딸기 러그","방","러그",65,"달콤한 딸기 한 알"),
   new ShopItem("기본 장식","방","장식",0,"소박한 책과 작은 화분"),new ShopItem("꽃 화분","방","장식",45,"매일 꽃 피는 작은 정원"),
   new ShopItem("별빛 조명","방","장식",80,"은은하게 빛나는 별 램프"),new ShopItem("작은 수족관","방","장식",120,"물고기 친구의 새 보금자리"),
   new ShopItem("든든한 밥","음식","음식",12,"포만감 +40 · 기분 +6",40,6),new ShopItem("딸기 간식","음식","음식",7,"포만감 +12 · 기분 +18",12,18),
   new ShopItem("과일 한 접시","음식","음식",14,"Lv.4 · 포만감 +25 · 기분 +25",25,25),new ShopItem("왕실 만찬","음식","음식",30,"Lv.10 · 포만감 +80 · 기분 +40",80,40),
   new ShopItem("수제 쿠키","음식","음식",10,"포만감 +15 · 기분 +25",15,25),new ShopItem("특별 도시락","음식","음식",20,"포만감 +60 · 기분 +15",60,15)
  };
  public static ShopItem Find(string name){return Items.FirstOrDefault(i=>i.Name==name);}
  public static void Normalize(Data d){if(d.RoomOwned==null)d.RoomOwned=new List<string>();foreach(string name in new[]{"기본 벽지","기본 러그","기본 장식"})if(!d.RoomOwned.Contains(name))d.RoomOwned.Add(name);d.Fullness=Math.Max(0,Math.Min(100,d.Fullness));d.Happiness=Math.Max(0,Math.Min(100,d.Happiness));if(String.IsNullOrEmpty(d.RoomWallpaper)||!d.RoomOwned.Contains(d.RoomWallpaper))d.RoomWallpaper="기본 벽지";if(String.IsNullOrEmpty(d.RoomRug)||!d.RoomOwned.Contains(d.RoomRug))d.RoomRug="기본 러그";if(String.IsNullOrEmpty(d.RoomDecoration)||!d.RoomOwned.Contains(d.RoomDecoration))d.RoomDecoration="기본 장식";}
  public static bool UpdateCare(Data d,DateTime now){DateTime last=AppClock.Decode(d.CareLocal,DateTime.MinValue);if(last==DateTime.MinValue||last>now){d.CareLocal=AppClock.Encode(now);return true;}int hours=(int)(now-last).TotalHours;if(hours<=0)return false;d.Fullness=Math.Max(0,d.Fullness-Math.Min(hours,100)*6);d.Happiness=Math.Max(0,d.Happiness-Math.Min(hours,100)*3);d.CareLocal=AppClock.Encode(last.AddHours(hours));return true;}
  public static string Equipped(Data d,ShopItem item){return item.Kind=="캐릭터"?d.Equipped:item.Slot=="벽지"?d.RoomWallpaper:item.Slot=="러그"?d.RoomRug:d.RoomDecoration;}
  public static bool Owned(Data d,ShopItem item){return item.Kind=="캐릭터"?d.Owned.Contains(item.Name):item.Kind=="방"&&d.RoomOwned.Contains(item.Name);}
  public static bool Buy(Data d,string name,out string error){error="";Normalize(d);var item=Find(name);if(item==null||item.Kind=="음식"){error="아이템을 찾을 수 없어요.";return false;}if(Companions.Level(d)<Progression.Required(name)){error="레벨 "+Progression.Required(name)+"부터 사용할 수 있어요.";return false;}if(!Owned(d,item)){if(d.Coins<item.Price){error="코인이 부족해요.";return false;}d.Coins-=item.Price;(item.Kind=="방"?d.RoomOwned:d.Owned).Add(name);}if(item.Kind=="캐릭터")d.Equipped=name;else if(item.Slot=="벽지")d.RoomWallpaper=name;else if(item.Slot=="러그")d.RoomRug=name;else d.RoomDecoration=name;return true;}
  public static bool Feed(Data d,string name,DateTime now,out string error){error="";var item=Find(name);if(item==null||item.Kind!="음식"){error="먹을 것을 선택해주세요.";return false;}if(Companions.Level(d)<Progression.Required(name)){error="레벨 "+Progression.Required(name)+"부터 먹일 수 있어요.";return false;}UpdateCare(d,now);if(d.Fullness>=100&&d.Happiness>=100){error="지금은 배도 부르고 기분도 좋아요! 조금 있다 먹어요.";return false;}if(d.Coins<item.Price){error="코인이 부족해요.";return false;}d.Coins-=item.Price;d.Fullness=Math.Min(100,d.Fullness+item.Fullness);d.Happiness=Math.Min(100,d.Happiness+item.Joy);d.MealsGiven++;d.LastFood=name;d.LastFedLocal=AppClock.Encode(now);return true;}
 }
 public static class LivingArt {
  static Color C(string hex){return ColorTranslator.FromHtml(hex);}static void R(Graphics g,string hex,int x,int y,int w,int h){Theme.Fill(g,C(hex),x,y,w,h);}
  public static void Food(Graphics g,string name,Rectangle box){var state=g.Save();g.TranslateTransform(box.X,box.Y);g.ScaleTransform(box.Width/64f,box.Height/64f);
   if(name=="든든한 밥"){R(g,"#50392F",10,32,44,5);R(g,"#EFAC8B",13,37,38,14);R(g,"#D18568",18,49,28,6);R(g,"#50392F",21,55,22,3);R(g,"#FFF8E4",14,23,36,9);R(g,"#FFF8E4",20,17,24,6);R(g,"#DBCAA6",21,23,3,5);R(g,"#DBCAA6",35,21,3,5);R(g,"#6E9E74",28,28,9,3);}
   else if(name=="딸기 간식"){R(g,"#9B4563",22,20,25,7);R(g,"#F5819B",17,27,35,13);R(g,"#F5819B",21,40,27,8);R(g,"#F5819B",27,48,15,6);R(g,"#71A67B",25,12,17,9);R(g,"#547C61",30,9,6,8);foreach(var p in new[]{new Point(25,29),new Point(38,31),new Point(30,40),new Point(42,40)})R(g,"#FFF0BD",p.X,p.Y,3,4);}
   else if(name=="수제 쿠키"){R(g,"#885F44",13,17,38,36);R(g,"#EBC486",17,13,30,44);R(g,"#EBC486",10,23,44,24);foreach(var p in new[]{new Point(22,22),new Point(39,26),new Point(28,37),new Point(42,42),new Point(17,42)})R(g,"#825B46",p.X,p.Y,5,5);}
   else if(name=="과일 한 접시"){R(g,"#91BABC",9,43,46,13);R(g,"#FFF8E4",12,41,40,11);R(g,"#ED869C",14,23,15,19);R(g,"#71A67B",17,19,10,5);R(g,"#EEC263",31,25,19,16);R(g,"#BD90C9",27,16,13,12);R(g,"#D0AADB",25,20,7,7);R(g,"#FFF0BD",19,27,3,3);R(g,"#FFF0BD",36,29,5,4);}
   else {R(g,"#50392F",8,22,48,33);R(g,"#D78778",11,25,42,27);R(g,"#FFF8E4",14,28,19,19);R(g,"#EFBD63",36,29,13,7);R(g,"#7AA485",36,40,13,7);R(g,"#A4C0A0",38,37,7,4);R(g,"#EBD9BA",28,15,30,3);}
   g.Restore(state);
  }
  public static void Room(Graphics g,Data d){if(Object.ReferenceEquals(d,Store.State)){int level=Companions.Level(d);d=new Data{RoomRug=level>=Progression.Required(d.RoomRug)?d.RoomRug:"기본 러그",RoomDecoration=level>=Progression.Required(d.RoomDecoration)?d.RoomDecoration:"기본 장식"};}
   if(d.RoomRug=="구름 러그"){R(g,"#91BABC",106,113,166,27);R(g,"#FFF9EB",116,109,145,27);R(g,"#FFF9EB",108,116,162,13);R(g,"#D9E6DA",121,130,134,3);}
   if(d.RoomRug=="별무늬 러그"){R(g,"#646386",106,111,166,28);R(g,"#8983AE",111,115,156,20);for(int x=122;x<264;x+=24){R(g,"#FFE1A0",x,120,6,6);R(g,"#FFE1A0",x+2,118,2,10);}}
   if(d.RoomRug=="딸기 러그"){R(g,"#B35F74",107,111,164,28);R(g,"#F2A3AA",112,115,154,20);for(int x=124;x<263;x+=24){R(g,"#D9778C",x,122,8,8);R(g,"#7E9C72",x+1,119,6,3);R(g,"#FFF0C8",x+3,124,2,2);}}
   if(d.RoomDecoration=="꽃 화분"){R(g,"#996653",310,98,38,5);R(g,"#DEA38B",314,103,30,21);R(g,"#70916B",320,74,3,24);R(g,"#70916B",336,79,3,19);R(g,"#F3B4BE",314,69,15,10);R(g,"#EDC780",330,73,15,10);R(g,"#FFF5CD",319,71,5,5);R(g,"#FFF5CD",335,76,5,4);}
   if(d.RoomDecoration=="별빛 조명"){R(g,"#FFF0CC",318,70,24,24);R(g,"#FFD989",324,65,12,34);R(g,"#FFD989",313,76,34,12);R(g,"#E8BD78",328,99,4,19);R(g,"#9D7552",319,118,22,5);}
   if(d.RoomDecoration=="작은 수족관"){R(g,"#5E7778",301,77,58,44);R(g,"#B7E0D8",304,81,52,35);R(g,"#EAF6DE",309,81,3,30);R(g,"#E8C996",304,111,52,5);R(g,"#7CA583",344,96,3,15);R(g,"#EDA781",321,91,11,6);R(g,"#EDA781",318,92,3,4);R(g,"#50392F",329,92,2,2);R(g,"#6D91AA",312,91,3,3);R(g,"#6D91AA",334,85,3,3);}
  }
 }
 public static class LivingTests {
  static void Check(bool value,string message){if(!value)throw new Exception("Living: "+message);}
  public static void Run(){string error;var d=new Data{Coins=200,Fullness=20,Happiness=30,ExperienceAdjustment=300};Check(Living.Feed(d,"든든한 밥",AppClock.Now,out error)&&d.Coins==188&&d.Fullness==60&&d.MealsGiven==1,"meal cost and care");Check(Living.Feed(d,"딸기 간식",AppClock.Now,out error)&&d.Coins==181&&d.Happiness==54,"snack");d.Coins=0;int fullness=d.Fullness;Check(!Living.Feed(d,"수제 쿠키",AppClock.Now,out error)&&d.Fullness==fullness,"food insufficient funds");d.Coins=200;Check(Living.Buy(d,"민트 숲 벽지",out error)&&d.Coins==140&&d.RoomWallpaper=="민트 숲 벽지","room buy");Check(Living.Buy(d,"민트 숲 벽지",out error)&&d.Coins==140,"owned room no charge");Check(Living.Buy(d,"구름 러그",out error)&&d.RoomWallpaper=="민트 숲 벽지"&&d.RoomRug=="구름 러그","room slots independent");d.Fullness=100;d.Happiness=100;int coins=d.Coins;Check(!Living.Feed(d,"딸기 간식",AppClock.Now,out error)&&d.Coins==coins,"full pet no charge");d.CareLocal=AppClock.Encode(AppClock.Now.AddHours(-2));Living.UpdateCare(d,AppClock.Now);Check(d.Fullness==88&&d.Happiness==94,"elapsed care");var restored=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Data>(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(d));Check(restored.RoomRug=="구름 러그"&&restored.MealsGiven==2,"persistence");var legacy=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Data>("{\"Coins\":123,\"Owned\":[\"기본\"],\"Tasks\":[]}");Living.Normalize(legacy);Check(legacy.Coins==123&&legacy.Fullness==70&&legacy.RoomOwned.Contains("기본 벽지"),"legacy migration");}
 }
 public static class LivingUiTests {
  static IEnumerable<Control> All(Control root){foreach(Control c in root.Controls){yield return c;foreach(var child in All(c))yield return child;}}
  static Button Button(Form form,string name){return All(form).OfType<Button>().First(b=>b.Text==name);}
  static void Item(Form form,string name){var card=All(form).OfType<Panel>().First(p=>p.Controls.OfType<Label>().Any(l=>l.Text==name));card.Controls.OfType<Button>().Single().PerformClick();Application.DoEvents();}
  static void Check(bool value,string message){if(!value)throw new Exception("Living UI: "+message);}
  static void Capture(Form form,string name){string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview");Directory.CreateDirectory(folder);using(var b=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(folder,name+".png"));}}
  public static int Execute(){string oldPath=Store.FilePath;Data oldState=Store.State;string temp=Path.Combine(Path.GetTempPath(),"DeskBuddy-living-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);Store.FilePath=Path.Combine(temp,"data.json");Store.State=new Data{Coins=500,ExperienceAdjustment=300,Fullness=20,Happiness=40,CareLocal=AppClock.Encode(AppClock.Now)};
   try{LivingTests.Run();using(var pet=new PetForm())using(var form=new MainForm(pet)){
    form.Location=new Point(-2200,-1200);form.Show();Application.DoEvents();Button(form,"밥 주기 12 G").PerformClick();Application.DoEvents();Check(Store.State.Coins==488&&Store.State.Fullness==60,"home meal");Button(form,"간식 주기 7 G").PerformClick();Check(Store.State.Coins==481&&Store.State.Happiness==64,"home snack");
    form.Navigate("코인 상점");Capture(form,"shop-accessories");Button(form,"방 꾸미기").PerformClick();Item(form,"민트 숲 벽지");Item(form,"구름 러그");Check(Store.State.Coins==366&&Store.State.RoomWallpaper=="민트 숲 벽지"&&Store.State.RoomRug=="구름 러그","room slots and cost");Item(form,"민트 숲 벽지");Check(Store.State.Coins==366,"re-equip free");Capture(form,"shop-room");
    Button(form,"캐릭터 꾸미기").PerformClick();Item(form,"꽃 화관");Check(Store.State.Coins==281&&Store.State.Equipped=="꽃 화관","new accessory");Button(form,"밥 / 간식").PerformClick();Capture(form,"shop-food");Item(form,"수제 쿠키");Check(Store.State.Coins==271&&Store.State.MealsGiven==3&&Store.State.Fullness==87,"food tab consumed");
    using(var editor=new ScheduleEditor(null,"야근")){Check(editor.CategoryInput.Text=="야근"&&editor.TitleInput.Text=="야근","overtime preset");string error;if(!editor.TrySave(out error))throw new Exception(error);}
    form.Navigate("오늘의 업무");All(form).OfType<ListBox>().Single().SelectedIndex=0;Button(form,"야근 완료 -20 코인").PerformClick();Application.DoEvents();Check(Store.State.Coins==251&&Store.State.Tasks.Single().Done,"overtime UI debit");Capture(form,"overtime-quests");
    form.Navigate("나의 방");Capture(form,"home-care");Store.State=new Data();Store.Load();Check(Store.State.Coins==251&&Store.State.Fullness==87&&Store.State.MealsGiven==3&&Store.State.RoomRug=="구름 러그"&&Store.State.Equipped=="꽃 화관"&&Store.State.Tasks[0].Done,"all new data persisted");
   }File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"living-validation.txt"),"PASS: legacy migration, care decay, food cost and consumption, home feeding, room purchase and independent slots, free re-equipping, accessory purchase, food tab, overtime preset and UI debit, duplicate deduction protection in self-test, disk save/reload and screenshots.");return 0;
   }catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"living-validation.txt"),"FAIL: "+ex);return 1;}finally{Store.FilePath=oldPath;Store.State=oldState;foreach(string file in Directory.GetFiles(temp))File.Delete(file);Directory.Delete(temp);}
  }
 }
}
