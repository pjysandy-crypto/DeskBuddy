using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeskBuddy {
 public static class GoogleCalendar {
  static string SecretPath {get{return Path.Combine(Store.Root,"calendar.secret");}}
  static readonly byte[] Entropy=Encoding.UTF8.GetBytes("DeskBuddy.GoogleCalendar.v1");
  static DateTime next=DateTime.MinValue;
  static bool busy;
  public static string Status="아직 연결하지 않았어요.";
  public static string Address(){try{return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(SecretPath),Entropy,DataProtectionScope.CurrentUser));}catch{return "";}}
  public static Uri Validate(string address){Uri uri;if(!Uri.TryCreate(address.Trim(),UriKind.Absolute,out uri)||uri.Scheme!="https"||uri.Host!="calendar.google.com"||!uri.AbsolutePath.StartsWith("/calendar/ical/",StringComparison.Ordinal)||!uri.AbsolutePath.EndsWith("/basic.ics",StringComparison.Ordinal)||uri.UserInfo!=""||!uri.IsDefaultPort||uri.Query!=""||uri.Fragment!="")throw new Exception("구글 캘린더의 iCal 비공개 주소를 입력해주세요.");return uri;}
  public static void SaveAddress(string address){Validate(address);Directory.CreateDirectory(Store.Root);File.WriteAllBytes(SecretPath,ProtectedData.Protect(Encoding.UTF8.GetBytes(address.Trim()),Entropy,DataProtectionScope.CurrentUser));}
  public static void Disconnect(){if(busy)throw new Exception("일정을 가져오는 중이에요. 잠시 후 다시 시도해주세요.");if(File.Exists(SecretPath))File.Delete(SecretPath);Store.State.Tasks.RemoveAll(t=>Imported(t));Store.Save();Status="연결을 해제했어요. 구글 원본 일정은 유지돼요.";}
  public static bool Imported(TaskItem t){return t!=null&&t.Id.StartsWith("gcal:",StringComparison.Ordinal);}
  public static int TestUi(){try{Tests();using(var form=new GoogleCalendarForm()){form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-2200,-1200);form.Show();Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"calendar-preview.png"));}if(form.Controls.OfType<TextBox>().Single().UseSystemPasswordChar!=true)throw new Exception("Secret not masked");form.Close();}File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"calendar-validation.txt"),"PASS: timezone, recurring events, overrides, cancellations, idempotent merge, monthly/all-day, address validation, secret masking, settings rendering.");return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"calendar-validation.txt"),"FAIL: "+ex.GetType().Name);return 1;}}
  public static async Task<string> Sync(string address,bool connect){
   if(busy)throw new Exception("이미 일정을 가져오고 있어요.");Validate(address);busy=true;next=AppClock.Now.AddMinutes(15);
   try{
    DateTime start=AppClock.Now.Date.AddDays(-1),end=start.AddDays(92);
    var parsed=await Task.Run(()=>Download(address,start,end));
    if(connect)SaveAddress(address);
    var serializer=new System.Web.Script.Serialization.JavaScriptSerializer();var previous=serializer.Deserialize<List<TaskItem>>(serializer.Serialize(Store.State.Tasks));
    Merge(Store.State,parsed,start,end);if(!Store.Save()){Store.State.Tasks=previous;throw new Exception("일정을 저장하지 못했어요. 다시 가져와주세요.");}
    foreach(var form in Application.OpenForms.OfType<MainForm>().ToArray())form.RefreshPage();
    foreach(var form in Application.OpenForms.OfType<ScheduleCalendarForm>().ToArray())form.Reload();
    Status=parsed.Count+"개 일정 · "+AppClock.Now.ToString("MM/dd HH:mm")+" 갱신";return Status;
   }catch{Status="연결 또는 일정 해석에 실패했어요. 기존 일정은 유지돼요.";throw new Exception(Status+"\n주소와 인터넷 연결을 확인해주세요. 지원하지 않는 반복 형식이나 시간대일 수도 있어요.");}
   finally{busy=false;}
  }
  public static async void Poll(){if(Environment.GetCommandLineArgs().Skip(1).Any(a=>a.StartsWith("--"))||busy||AppClock.Now<next)return;next=AppClock.Now.AddMinutes(15);string address=Address();if(address.Length==0)return;try{await Sync(address,false);}catch{}}
  static List<TaskItem> Download(string address,DateTime start,DateTime end){
   ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
   var request=(HttpWebRequest)WebRequest.Create(Validate(address));request.AllowAutoRedirect=false;request.Timeout=20000;request.ReadWriteTimeout=20000;
   using(var response=request.GetResponse())using(var stream=response.GetResponseStream())using(var memory=new MemoryStream()){
    byte[] buffer=new byte[8192];int count;while((count=stream.Read(buffer,0,buffer.Length))>0){memory.Write(buffer,0,count);if(memory.Length>8*1024*1024)throw new Exception("Calendar too large");}
    return ICalendar.Parse(Encoding.UTF8.GetString(memory.ToArray()),start,end);
   }
  }
  public static void Merge(Data data,List<TaskItem> incoming,DateTime start,DateTime end){
   var existing=data.Tasks.Where(Imported).ToDictionary(t=>t.Id);
   var keys=new HashSet<string>(incoming.Select(t=>t.Id));
   data.Tasks.RemoveAll(t=>Imported(t)&&Scheduler.Due(t)>=start&&Scheduler.Due(t)<end&&!keys.Contains(t.Id));
   foreach(var item in incoming){TaskItem old;if(existing.TryGetValue(item.Id,out old)){
     bool moved=Scheduler.Due(old)!=Scheduler.Due(item);old.Title=item.Title;old.AllDay=item.AllDay;old.DurationMinutes=item.DurationMinutes;Scheduler.SetDue(old,Scheduler.Due(item));if(moved){old.Notified=Scheduler.Due(item)<AppClock.Now;old.EveNotified=false;old.SnoozeLocal="";}
    }else {item.Notified=Scheduler.Due(item)<AppClock.Now;data.Tasks.Add(item);}
   }
  }
  public static void Tests(){
   var start=new DateTime(2026,10,1);string text="BEGIN:VCALENDAR\r\nBEGIN:VEVENT\r\nUID:one\r\nDTSTART:20261007T010000Z\r\nSUMMARY:회의\\, 준비\r\nEND:VEVENT\r\nBEGIN:VEVENT\r\nUID:two\r\nDTSTART;TZID=Asia/Seoul:20261005T090000\r\nRRULE:FREQ=WEEKLY;COUNT=4;BYDAY=MO,WE\r\nEXDATE;TZID=Asia/Seoul:20261007T090000\r\nSUMMARY:반복\r\nEND:VEVENT\r\nEND:VCALENDAR";
   var rows=ICalendar.Parse(text,start,start.AddMonths(1));if(rows.Count!=4||Scheduler.Due(rows[0]).Hour!=10||rows[0].Title!="회의, 준비")throw new Exception("Calendar timezone/recurrence");
   var d=new Data();Merge(d,rows,start,start.AddMonths(1));d.Tasks[0].Done=true;Merge(d,rows.Select(t=>new TaskItem{Id=t.Id,Title=t.Title,Due=t.Due,LocalDue=t.LocalDue}).ToList(),start,start.AddMonths(1));if(d.Tasks.Count!=4||!d.Tasks[0].Done)throw new Exception("Calendar idempotence");
   var saved=d.Tasks[0];saved.Title="local";Merge(d,new List<TaskItem>(),start,start.AddMonths(1));if(d.Tasks.Count!=0)throw new Exception("Calendar deletion");
   bool rejected=false;try{Validate("https://example.com/basic.ics");}catch{rejected=true;}if(!rejected)throw new Exception("Calendar address validation");
   string modified=text.Replace("EXDATE;TZID=Asia/Seoul:20261007T090000","").Replace("END:VCALENDAR","BEGIN:VEVENT\r\nUID:two\r\nRECURRENCE-ID;TZID=Asia/Seoul:20261007T090000\r\nDTSTART;TZID=Asia/Seoul:20261008T120000\r\nSUMMARY:이동\r\nEND:VEVENT\r\nEND:VCALENDAR");
   var moved=ICalendar.Parse(modified,start,start.AddMonths(1));if(!moved.Any(t=>t.Title=="이동"&&Scheduler.Due(t)==new DateTime(2026,10,8,12,0,0)))throw new Exception("Calendar override");
   var cancelled=ICalendar.Parse(modified.Replace("SUMMARY:이동","STATUS:CANCELLED\r\nSUMMARY:이동"),start,start.AddMonths(1));if(cancelled.Count!=4||cancelled.Any(t=>t.Title=="이동"))throw new Exception("Calendar cancelled occurrence");
   string monthly="BEGIN:VCALENDAR\nBEGIN:VEVENT\nUID:monthly\nDTSTART;VALUE=DATE:20260131\nRRULE:FREQ=MONTHLY;COUNT=3\nSUMMARY:월간\nEND:VEVENT\nEND:VCALENDAR";
   var monthRows=ICalendar.Parse(monthly,new DateTime(2026,1,1),new DateTime(2026,6,1));if(monthRows.Count!=3||Scheduler.Due(monthRows[1]).Month!=3||!monthRows.All(t=>t.AllDay))throw new Exception("Calendar monthly/all-day");
   rejected=false;try{ICalendar.Parse(text.Replace("FREQ=WEEKLY","FREQ=HOURLY"),start,start.AddMonths(1));}catch{rejected=true;}if(!rejected)throw new Exception("Unsupported recurrence must fail");
  }
 }

 // Expand bounded read-only occurrences; reject unsupported rules instead of silently dropping events.
 public static class ICalendar {
  class Property {public string Name,Value;public Dictionary<string,string> Params=new Dictionary<string,string>();}
  class Event {public List<Property> Fields=new List<Property>();public Property Get(string name){return Fields.FirstOrDefault(p=>p.Name==name);}public string Text(string name){var p=Get(name);return p==null?"":p.Value;}}
  static Property Read(string line){int colon=line.IndexOf(':');if(colon<0)throw new Exception("Invalid property");var chunks=line.Substring(0,colon).Split(';');var p=new Property{Name=chunks[0].ToUpperInvariant(),Value=line.Substring(colon+1)};foreach(string chunk in chunks.Skip(1)){int eq=chunk.IndexOf('=');if(eq>0)p.Params[chunk.Substring(0,eq).ToUpperInvariant()]=chunk.Substring(eq+1).Trim('"');}return p;}
  static TimeZoneInfo Zone(Property p){string id;if(!p.Params.TryGetValue("TZID",out id))id="Asia/Seoul";switch(id){case "Asia/Seoul":id="Korea Standard Time";break;case "Asia/Tokyo":id="Tokyo Standard Time";break;case "America/New_York":id="Eastern Standard Time";break;case "America/Los_Angeles":id="Pacific Standard Time";break;case "America/Chicago":id="Central Standard Time";break;case "Europe/London":id="GMT Standard Time";break;case "Europe/Paris":id="Romance Standard Time";break;case "Etc/UTC":case "UTC":id="UTC";break;}return TimeZoneInfo.FindSystemTimeZoneById(id);}
  static DateTime Local(Property p,string value){return DateTime.ParseExact(value.TrimEnd('Z'),value.Length==8?"yyyyMMdd":"yyyyMMdd'T'HHmmss",CultureInfo.InvariantCulture,DateTimeStyles.None);}
  static DateTime Korea(Property p,string value){DateTime local=Local(p,value);if(value.Length==8)return local;DateTime utc=value.EndsWith("Z")?DateTime.SpecifyKind(local,DateTimeKind.Utc):TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local,DateTimeKind.Unspecified),Zone(p));return AppClock.Wall(utc);}
  static DateTime Korea(Property p,DateTime value){return Korea(p,value.ToString(p.Value.Length==8?"yyyyMMdd":"yyyyMMdd'T'HHmmss",CultureInfo.InvariantCulture)+(p.Value.EndsWith("Z")?"Z":""));}
  static string Unescape(string s){var b=new StringBuilder();for(int i=0;i<s.Length;i++){if(s[i]=='\\'&&i+1<s.Length){char c=s[++i];b.Append(c=='n'||c=='N'?'\n':c);}else b.Append(s[i]);}return b.ToString();}
  static int Weekday(string s){switch(s){case "SU":return 0;case "MO":return 1;case "TU":return 2;case "WE":return 3;case "TH":return 4;case "FR":return 5;case "SA":return 6;default:throw new Exception("Invalid weekday");}}
  static bool ByDay(DateTime day,string list){return list.Split(',').Any(v=>{
   string name=v.Substring(v.Length-2);if((int)day.DayOfWeek!=Weekday(name))return false;if(v.Length==2)return true;
   int nth=int.Parse(v.Substring(0,v.Length-2),CultureInfo.InvariantCulture);return nth>0?(day.Day-1)/7+1==nth:-(DateTime.DaysInMonth(day.Year,day.Month)-day.Day)/7-1==nth;
  });}
  static IEnumerable<DateTime> Occurrences(Event ev,Property p,DateTime start,DateTime end){
   DateTime first=Local(p,p.Value);string rule=ev.Text("RRULE");if(rule==""){yield return first;yield break;}
   var r=rule.Split(';').Select(x=>x.Split('=')).ToDictionary(x=>x[0],x=>x[1]);
   foreach(string k in r.Keys)if(!new[]{"FREQ","INTERVAL","COUNT","UNTIL","BYDAY","BYMONTHDAY","BYMONTH","WKST"}.Contains(k))throw new Exception("Unsupported recurrence");
   string freq=r["FREQ"];if(!new[]{"DAILY","WEEKLY","MONTHLY","YEARLY"}.Contains(freq))throw new Exception("Unsupported frequency");
   int interval=r.ContainsKey("INTERVAL")?int.Parse(r["INTERVAL"]):1,limit=r.ContainsKey("COUNT")?int.Parse(r["COUNT"]):int.MaxValue;if(interval<1||limit<1)throw new Exception("Invalid recurrence");
   DateTime until=r.ContainsKey("UNTIL")?Korea(new Property{Value=r["UNTIL"],Params=p.Params},r["UNTIL"]):DateTime.MaxValue;
   int wkst=r.ContainsKey("WKST")?Weekday(r["WKST"]):1,total=0;
   DateTime firstWeek=first.Date.AddDays(-((int)first.DayOfWeek-wkst+7)%7);
   for(int i=0;i<200000;i++){
    DateTime day=first.Date.AddDays(i),candidate=day+first.TimeOfDay,korea=Korea(p,candidate);if(korea>=end||korea>until)yield break;
    int months=(day.Year-first.Year)*12+day.Month-first.Month;
    bool allowed=freq=="DAILY"?i%interval==0:freq=="WEEKLY"?((int)(day-firstWeek).TotalDays/7)%interval==0:freq=="MONTHLY"?months%interval==0:(day.Year-first.Year)%interval==0;
    if(r.ContainsKey("BYMONTH"))allowed&=r["BYMONTH"].Split(',').Any(v=>int.Parse(v)==day.Month);else if(freq=="YEARLY")allowed&=day.Month==first.Month;
    if(r.ContainsKey("BYMONTHDAY"))allowed&=r["BYMONTHDAY"].Split(',').Any(v=>{int n=int.Parse(v);return day.Day==(n>0?n:DateTime.DaysInMonth(day.Year,day.Month)+n+1);});
    else if((freq=="MONTHLY"||freq=="YEARLY")&&!r.ContainsKey("BYDAY"))allowed&=day.Day==first.Day;
    if(r.ContainsKey("BYDAY"))allowed&=ByDay(day,r["BYDAY"]);else if(freq=="WEEKLY")allowed&=day.DayOfWeek==first.DayOfWeek;
    if(allowed){if(++total>limit)yield break;if(korea>=start)yield return candidate;}
   }
   throw new Exception("Recurrence exceeds limit");
  }
  public static List<TaskItem> Parse(string text,DateTime start,DateTime end){
   if(!text.TrimStart('\uFEFF',' ','\r','\n').StartsWith("BEGIN:VCALENDAR")||!text.Contains("END:VCALENDAR"))throw new Exception("Invalid calendar");
   var lines=new List<string>();foreach(string line in text.Replace("\r\n","\n").Split('\n')){if((line.StartsWith(" ")||line.StartsWith("\t"))&&lines.Count>0)lines[lines.Count-1]+=line.Substring(1);else lines.Add(line.TrimEnd('\r'));}
   var events=new List<Event>();Event current=null;int nested=0;foreach(string line in lines){if(line=="BEGIN:VEVENT"){current=new Event();nested=0;}else if(line=="END:VEVENT"){if(current==null)throw new Exception("Invalid event");events.Add(current);current=null;}else if(current!=null){if(line.StartsWith("BEGIN:"))nested++;else if(line.StartsWith("END:"))nested--;else if(nested==0&&line.Length>0)current.Fields.Add(Read(line));}}
   if(current!=null)throw new Exception("Incomplete calendar");var result=new List<TaskItem>();
   foreach(var group in events.GroupBy(e=>e.Text("UID"))){if(group.Key=="")throw new Exception("Missing UID");var master=group.FirstOrDefault(e=>e.Get("RECURRENCE-ID")==null);if(master==null)throw new Exception("Missing recurrence master");if(master.Text("STATUS")=="CANCELLED")continue;
    var date=master.Get("DTSTART");if(date==null)throw new Exception("Missing start");if(master.Get("RDATE")!=null||group.Any(e=>e.Get("RECURRENCE-ID")!=null&&e.Get("RECURRENCE-ID").Params.ContainsKey("RANGE")))throw new Exception("Unsupported recurrence extension");var exceptions=new HashSet<DateTime>(master.Fields.Where(p=>p.Name=="EXDATE").SelectMany(p=>p.Value.Split(',').Select(v=>Korea(p,v))));
    var overrides=group.Where(e=>e.Get("RECURRENCE-ID")!=null).ToDictionary(e=>Korea(e.Get("RECURRENCE-ID"),e.Text("RECURRENCE-ID")));
    var occurrences=new Dictionary<DateTime,Event>();foreach(var local in Occurrences(master,date,start.AddDays(-2),end.AddDays(2))){DateTime original=Korea(date,local);if(!exceptions.Contains(original))occurrences[original]=master;}
    foreach(var entry in overrides)occurrences[entry.Key]=entry.Value;
    foreach(var occurrence in occurrences){Event ev=occurrence.Value;if(ev.Text("STATUS")=="CANCELLED")continue;Property actual=ev.Get("DTSTART");DateTime due=ev==master?occurrence.Key:Korea(actual,actual.Value);if(due<start||due>=end)continue;
     string identity=group.Key+"|"+AppClock.Encode(occurrence.Key);string id;using(var hash=SHA256.Create())id=Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(identity)));
     var task=new TaskItem{Id="gcal:"+id,Title=Unescape(ev.Text("SUMMARY")),Category="업무",AllDay=actual.Value.Length==8,Repeat="한 번"};if(task.Title=="")task.Title="구글 일정";var endProperty=ev.Get("DTEND");if(endProperty!=null){DateTime eventStart=Korea(actual,actual.Value),eventEnd=Korea(endProperty,endProperty.Value);task.DurationMinutes=(int)Math.Max(0,(eventEnd-eventStart).TotalMinutes);}Scheduler.SetDue(task,due);result.Add(task);
    }
   }
   return result;
  }
 }
 public class GoogleCalendarForm:Form {
  TextBox address;Label status;Button sync,disconnect;
  public GoogleCalendarForm(){Text="구글 캘린더 연결";Icon=AppIdentity.Icon;StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;AutoScaleMode=AutoScaleMode.None;ClientSize=new Size(650,360);BackColor=Theme.Bg;
   Controls.Add(Theme.Label("GOOGLE CALENDAR",20,16,600,40,22));Controls.Add(Theme.Label("iCal 형식의 비공개 주소를 입력해주세요.",20,70,610,30));address=Theme.Input(GoogleCalendar.Address(),20,112,610);address.UseSystemPasswordChar=true;Controls.Add(address);
   Controls.Add(Theme.Label("약 15분마다 갱신 · 오늘부터 90일 일정 가져오기\n일정 수정은 구글에서 해주세요. 완료 상태는 이 PC에 저장돼요.",20,155,610,52,10,Theme.Muted));
   status=Theme.Label(GoogleCalendar.Status,20,215,610,45,10);Controls.Add(status);
   sync=Theme.Button("연결 / 지금 갱신",20,284,296,46,async(s,e)=>{sync.Enabled=disconnect.Enabled=false;address.Enabled=false;status.Text="일정을 가져오고 있어요...";try{status.Text=await GoogleCalendar.Sync(address.Text,true);}catch(Exception ex){GameAlert.Show(ex.Message,"캘린더 연결");status.Text=GoogleCalendar.Status;}finally{if(!IsDisposed){sync.Enabled=disconnect.Enabled=address.Enabled=true;}}},true);Controls.Add(sync);
   disconnect=Theme.Button("연결 해제",334,284,296,46,(s,e)=>{if(GameAlert.Show("연결을 해제하고 가져온 구글 일정을 제거할까요?\n구글 원본 일정은 유지돼요.","캘린더 연결",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;try{GoogleCalendar.Disconnect();address.Text="";status.Text=GoogleCalendar.Status;}catch(Exception ex){GameAlert.Show(ex.Message);}});Controls.Add(disconnect);
   FormClosing+=(s,e)=>{if(!sync.Enabled)e.Cancel=true;};
  }
 }
}
