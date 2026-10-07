using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
namespace DeskBuddy {
 public static class LibraryLoader {
  static bool installed;
  public static void Install(){
   if(installed)return;installed=true;
   AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{
    if(new AssemblyName(e.Name).Name!="ComponentFactory.Krypton.Toolkit")return null;
    using(var resource=typeof(LibraryLoader).Assembly.GetManifestResourceStream("DeskBuddy.Krypton")){
     if(resource==null)return null;using(var buffer=new MemoryStream()){resource.CopyTo(buffer);return Assembly.Load(buffer.ToArray());}
    }
   };
  }
 }
 public static class AppClock {
  static TimeZoneInfo korea=TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
  public static DateTime Now{get{return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,korea);}}
  public static DateTime Wall(DateTime t){
   if(t==DateTime.MinValue)return t;
   if(t.Kind==DateTimeKind.Utc)return TimeZoneInfo.ConvertTimeFromUtc(t,korea);
   if(t.Kind==DateTimeKind.Local)return TimeZoneInfo.ConvertTime(t,korea);
   return t;
  }
  public static string Encode(DateTime t){return t==DateTime.MinValue?"":Wall(t).ToString("yyyy-MM-dd'T'HH:mm:ss",CultureInfo.InvariantCulture);}
  public static DateTime Decode(string text,DateTime fallback){
   DateTime t;if(DateTime.TryParseExact(text,"yyyy-MM-dd'T'HH:mm:ss",CultureInfo.InvariantCulture,DateTimeStyles.None,out t))return DateTime.SpecifyKind(t,DateTimeKind.Unspecified);
   return Wall(fallback);
  }
  public static bool Time(string text,out TimeSpan time){
   DateTime t;bool ok=DateTime.TryParseExact((text??"").Trim(),new[]{"H:mm","HH:mm","HHmm"},CultureInfo.InvariantCulture,DateTimeStyles.None,out t);
   time=ok?new TimeSpan(t.Hour,t.Minute,0):TimeSpan.Zero;return ok;
  }
 }
 public class ScheduleNotice {public TaskItem Task;public bool Eve;public string Text;}
 public static class Scheduler {
  public static DateTime Due(TaskItem t){return AppClock.Decode(t.LocalDue,t.Due);}
  public static DateTime End(TaskItem t){return Due(t).AddMinutes(t.DurationMinutes>0?t.DurationMinutes:t.AllDay?1440:0);}
  public static string TimeRange(TaskItem t,DateTime start){if(t.AllDay)return "종일";if(t.DurationMinutes<=0)return start.ToString("HH:mm");DateTime end=start.AddMinutes(t.DurationMinutes);return start.ToString("HH:mm")+"~"+end.ToString("HH:mm")+(end.Date>start.Date?" (+"+(end.Date-start.Date).Days+"일)":"");}
  public static void SetDue(TaskItem t,DateTime value){t.Due=DateTime.SpecifyKind(value,DateTimeKind.Unspecified);t.LocalDue=AppClock.Encode(t.Due);}
  public static string RepeatLabel(TaskItem t){return t.Repeat=="평일"?"월~금":t.Repeat=="매주"?"매주 "+Due(t).ToString("ddd"):t.Repeat;}
  public static void Normalize(Data d){
   foreach(var t in d.Tasks){SetDue(t,Due(t));if(String.IsNullOrEmpty(t.Repeat))t.Repeat="한 번";TimeSpan time;if(!AppClock.Time(t.AnchorTime,out time))t.AnchorTime=Due(t).ToString("HH:mm");if(!AppClock.Time(t.EveTime,out time))t.EveTime="18:00";}
   d.FocusEnd=AppClock.Decode(d.FocusEndLocal,d.FocusEnd);
  }
  public static void PrepareSave(Data d){
   foreach(var t in d.Tasks)SetDue(t,Due(t));
   d.FocusEnd=AppClock.Wall(d.FocusEnd);d.FocusEndLocal=AppClock.Encode(d.FocusEnd);
  }
  static bool Weekday(DateTime day){return day.DayOfWeek!=DayOfWeek.Saturday&&day.DayOfWeek!=DayOfWeek.Sunday;}
  public static DateTime FirstAllowed(DateTime value,string repeat){while(repeat=="평일"&&!Weekday(value))value=value.AddDays(1);return value;}
  static DateTime At(DateTime day,TaskItem t){TimeSpan time;if(!AppClock.Time(t.AnchorTime,out time))time=Due(t).TimeOfDay;return day.Date+time;}
  static void Rearm(TaskItem t,DateTime value){SetDue(t,value);t.Notified=false;t.EveNotified=false;t.SnoozeLocal="";t.Done=false;}
  public static bool Roll(TaskItem t,DateTime now){
   if(t.Repeat=="한 번"||t.Done)return false;
   bool adjusted=false;DateTime due=Due(t);
   if(t.Repeat=="평일"&&!Weekday(due)){Rearm(t,FirstAllowed(due,t.Repeat));due=Due(t);adjusted=true;}
   if(due.Date>=now.Date||(t.DurationMinutes>0&&End(t)>now))return adjusted;
   DateTime day=now.Date;
   if(t.Repeat=="매주"){int days=(now.Date-due.Date).Days;day=due.Date.AddDays(((days+6)/7)*7);}
   Rearm(t,FirstAllowed(At(day,t),t.Repeat));return true;
  }
  static DateTime Next(TaskItem t,DateTime now){
   DateTime due=Due(t),day=due.Date.AddDays(t.Repeat=="매주"?7:1);
   if(At(day,t)<=now)day=now.Date.AddDays(At(now.Date,t)<=now?1:0);
   if(t.Repeat=="매주")while(day.DayOfWeek!=due.DayOfWeek)day=day.AddDays(1);
   return FirstAllowed(At(day,t),t.Repeat);
  }
  public static bool Overtime(TaskItem t){return t.Category=="야근"||(t.Title??"").Contains("야근");}
  public static int CompletionCoins(TaskItem t){return Overtime(t)?-20:20;}
  public static bool Complete(TaskItem t,Data d,DateTime now){
   if(t.Done)return false;DateTime due=Due(t);string key=AppClock.Encode(due);
   if(t.Repeat!="한 번"&&(due.Date>now.Date||t.LastCompletedLocal==key))return false;
   d.Completed++;d.Coins=Math.Max(0,d.Coins+CompletionCoins(t));t.LastCompletedLocal=key;
   if(t.Repeat=="한 번")t.Done=true;else Rearm(t,Next(t,now));return true;
  }
  public static bool Skip(TaskItem t,DateTime now){if(t.Repeat=="한 번"||t.Done)return false;Rearm(t,Next(t,now));return true;}
  public static void Snooze(TaskItem t,DateTime now){t.SnoozeLocal=AppClock.Encode(now.AddMinutes(10));t.Notified=false;}
  public static List<ScheduleNotice> Poll(Data d,DateTime now,out bool changed){
   var result=new List<ScheduleNotice>();changed=false;
   foreach(var t in d.Tasks){
    if(Roll(t,now))changed=true;if(t.Done)continue;DateTime due=Due(t);
    TimeSpan eve;if(t.EveReminder&&!t.EveNotified&&AppClock.Time(t.EveTime,out eve)&&now.Date==due.Date.AddDays(-1)&&now>=due.Date.AddDays(-1)+eve){
     t.EveNotified=true;changed=true;result.Add(new ScheduleNotice{Task=t,Eve=true,Text=t.Category=="휴가"?"내일은 휴가입니다~\n"+t.Title:"내일 "+due.ToString("HH:mm")+" "+t.Title+" 일정이 있어요."});
    }
    DateTime trigger=String.IsNullOrEmpty(t.SnoozeLocal)?due:AppClock.Decode(t.SnoozeLocal,due);
    if(!t.Notified&&now>=trigger){
     t.Notified=true;changed=true;result.Add(new ScheduleNotice{Task=t,Text=t.AllDay?"오늘은 "+t.Title+"입니다~":"일정 시간이 됐어요!\n"+t.Title});
    }
   }return result;
  }
  public static TaskItem Appearance(Data d,DateTime now){
   return d.Tasks.Where(t=>!t.Done&&!String.IsNullOrEmpty(t.ScheduleImage)).Where(t=>{
    DateTime due=Due(t),start=t.AllDay?due.Date:due,end=t.DurationMinutes>0?End(t):t.AllDay?due.Date.AddDays(1):due.AddMinutes(Math.Max(1,t.AppearanceMinutes));
    return now>=start&&now<end;
   }).OrderByDescending(t=>t.AllDay).ThenByDescending(t=>Due(t)).FirstOrDefault();
  }
 }
 public static class ScheduleTests {
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  public static void Run(){
   var utc=new DateTime(2026,10,5,0,0,0,DateTimeKind.Utc);Check(AppClock.Wall(utc).Hour==9,"UTC to Seoul");
   var d=new Data();var t=new TaskItem{Title="출근",Repeat="평일",AnchorTime="09:00"};Scheduler.SetDue(t,new DateTime(2026,10,9,9,0,0));d.Tasks.Add(t);
   bool changed;var notices=Scheduler.Poll(d,new DateTime(2026,10,9,8,59,59),out changed);Check(notices.Count==0,"No early reminder");
   notices=Scheduler.Poll(d,new DateTime(2026,10,9,9,0,0),out changed);Check(notices.Count==1,"Due reminder");
   Check(Scheduler.Poll(d,new DateTime(2026,10,9,9,1,0),out changed).Count==0,"No duplicate reminder");
   Check(Scheduler.Complete(t,d,new DateTime(2026,10,9,10,0,0))&&Scheduler.Due(t)==new DateTime(2026,10,12,9,0,0),"Friday to Monday");
   Check(!Scheduler.Complete(t,d,new DateTime(2026,10,9,10,0,1))&&d.Coins==50,"No future repeat reward");
   var weekly=new TaskItem{Repeat="매주",AnchorTime="14:00"};Scheduler.SetDue(weekly,new DateTime(2026,10,5,14,0,0));
   Scheduler.Roll(weekly,new DateTime(2026,10,8,10,0,0));Check(Scheduler.Due(weekly)==new DateTime(2026,10,12,14,0,0),"Weekly recurrence");
   var daily=new TaskItem{Repeat="매일",AnchorTime="08:00"};Scheduler.SetDue(daily,new DateTime(2026,10,1,8,0,0));Scheduler.Roll(daily,new DateTime(2026,10,5,17,0,0));
   Check(Scheduler.Due(daily)==new DateTime(2026,10,5,8,0,0),"Missed recurring catchup");
   d=new Data();var holiday=new TaskItem{Title="휴가",Category="휴가",AllDay=true,EveReminder=true,EveTime="18:00",ScheduleImage="holiday.png"};
   Scheduler.SetDue(holiday,new DateTime(2026,10,6,9,0,0));d.Tasks.Add(holiday);
   Check(Scheduler.Poll(d,new DateTime(2026,10,5,17,59,0),out changed).Count==0,"No early eve reminder");
   notices=Scheduler.Poll(d,new DateTime(2026,10,5,18,0,0),out changed);Check(notices.Count==1&&notices[0].Text.Contains("내일은 휴가"),"Vacation eve message");
   Check(Scheduler.Poll(d,new DateTime(2026,10,5,18,1,0),out changed).Count==0,"No duplicate eve reminder");
   Check(Scheduler.Appearance(d,new DateTime(2026,10,6,0,0,0))==holiday&&Scheduler.Appearance(d,new DateTime(2026,10,7,0,0,0))==null,"All-day pose window");
   holiday.AllDay=false;holiday.AppearanceMinutes=30;Check(Scheduler.Appearance(d,new DateTime(2026,10,6,9,29,0))==holiday&&Scheduler.Appearance(d,new DateTime(2026,10,6,9,30,0))==null,"Timed pose window");
   holiday.Notified=true;Scheduler.Snooze(holiday,new DateTime(2026,10,6,9,1,0));Check(Scheduler.Poll(d,new DateTime(2026,10,6,9,10,0),out changed).Count==0,"Snooze not early");
   Check(Scheduler.Poll(d,new DateTime(2026,10,6,9,11,0),out changed).Count==1,"Snooze delivery");
   var serializer=new System.Web.Script.Serialization.JavaScriptSerializer();Scheduler.PrepareSave(d);var restored=serializer.Deserialize<Data>(serializer.Serialize(d));Scheduler.Normalize(restored);
   Check(Scheduler.Due(restored.Tasks[0])==new DateTime(2026,10,6,9,0,0),"Local schedule roundtrip");
   d.FocusEnd=new DateTime(2026,10,6,10,0,0);Scheduler.PrepareSave(d);restored=serializer.Deserialize<Data>(serializer.Serialize(d));Scheduler.Normalize(restored);Check(restored.FocusEnd.Hour==10,"Focus timezone roundtrip");
   TimeSpan time;Check(AppClock.Time("9:00",out time)&&time.Hours==9&&!AppClock.Time("25:00",out time),"24-hour input validation");
   var legacy=new Data();legacy.Tasks.Add(new TaskItem{Due=utc});Scheduler.Normalize(legacy);Check(Scheduler.Due(legacy.Tasks[0]).Hour==9,"Legacy UTC migration");
   d=new Data{Coins=75};var overtime=new TaskItem{Title="프로젝트 마무리",Category="야근"};Check(Scheduler.Complete(overtime,d,AppClock.Now)&&d.Coins==55,"Overtime deduct twenty");Check(!Scheduler.Complete(overtime,d,AppClock.Now)&&d.Coins==55,"No duplicate overtime deduction");
   overtime=new TaskItem{Title="개발 야근",Category="업무"};Check(Scheduler.Complete(overtime,d,AppClock.Now)&&d.Coins==35,"Overtime title detection");
   overtime=new TaskItem{Category="야근"};d.Coins=7;Check(Scheduler.Complete(overtime,d,AppClock.Now)&&d.Coins==0,"Overtime zero floor");
   overtime=new TaskItem{Category="야근",Repeat="매일",AnchorTime="21:00"};Scheduler.SetDue(overtime,AppClock.Now.Date.AddHours(21));d.Coins=100;Check(Scheduler.Complete(overtime,d,AppClock.Now)&&d.Coins==80&&!Scheduler.Complete(overtime,d,AppClock.Now),"Recurring overtime charged once per occurrence");Check(Scheduler.Complete(overtime,d,AppClock.Now.AddDays(1))&&d.Coins==60,"Next overtime occurrence");
  }
 }
}
