using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace DeskBuddy {
 public static class ScheduleRangeTests {
  public static int Execute(){Data previous=Store.State;string oldPath=Store.FilePath;string temp=Path.Combine(Path.GetTempPath(),"DeskBuddy-range-"+Guid.NewGuid());Directory.CreateDirectory(temp);Store.FilePath=Path.Combine(temp,"data.json");Store.State=new Data();
   try{DateTime day=AppClock.Now.Date.AddDays(2);string error;
    using(var editor=new ScheduleEditor(null)){editor.TitleInput.Text="회의 준비";editor.DateTimeInput.Value=day.AddHours(14);editor.EndTimeInput.Text="15:30";editor.RepeatInput.Text="매일";editor.StartPosition=FormStartPosition.Manual;editor.Location=new Point(-2200,-1200);editor.Show();Application.DoEvents();using(var image=new Bitmap(editor.Width,editor.Height)){editor.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"schedule-range-preview.png"));}if(!editor.TrySave(out error))throw new Exception(error);}
    var task=Store.State.Tasks.Single();if(task.DurationMinutes!=90||Scheduler.End(task)!=day.AddHours(15.5)||Scheduler.TimeRange(task,Scheduler.Due(task))!="14:00~15:30")throw new Exception("Range saved");Scheduler.Skip(task,day);if(task.DurationMinutes!=90||Scheduler.End(task)-Scheduler.Due(task)!=TimeSpan.FromMinutes(90))throw new Exception("Recurring duration");
    using(var editor=new ScheduleEditor(null)){editor.TitleInput.Text="밤 일정";editor.DateTimeInput.Value=day.AddHours(23);editor.EndTimeInput.Text="01:00";editor.EndNextDay.Checked=false;if(editor.TrySave(out error))throw new Exception("Invalid end accepted");editor.EndNextDay.Checked=true;if(!editor.TrySave(out error))throw new Exception(error);}
    task=Store.State.Tasks.Last();if(task.DurationMinutes!=120||CalendarDates.OnDay(Store.State.Tasks,day.AddDays(1)).All(e=>e.Task.Id!=task.Id))throw new Exception("Overnight visibility");
    task.Repeat="매일";Scheduler.Skip(task,day.AddDays(1).AddMinutes(30));if(Scheduler.Due(task)!=day.AddDays(1).AddHours(23))throw new Exception("Overnight repeat must not skip tonight");
    var copy=new JavaScriptSerializer().Deserialize<Data>(new JavaScriptSerializer().Serialize(Store.State));if(copy.Tasks.Last().DurationMinutes!=120)throw new Exception("Range persistence");
    string ics="BEGIN:VCALENDAR\nBEGIN:VEVENT\nUID:range\nDTSTART;TZID=Asia/Seoul:20261007T140000\nDTEND;TZID=Asia/Seoul:20261007T153000\nRRULE:FREQ=DAILY;COUNT=2\nSUMMARY:회의\nEND:VEVENT\nEND:VCALENDAR";
    var rows=ICalendar.Parse(ics,new DateTime(2026,10,1),new DateTime(2026,11,1));if(rows.Count!=2||rows.Any(t=>t.DurationMinutes!=90))throw new Exception("Google end-time import");
    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"schedule-range-validation.txt"),"PASS: start/end editor, invalid ranges, overnight visibility, recurring duration, persistence, Google DTEND, editor rendering.");return 0;
   }catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"schedule-range-validation.txt"),"FAIL: "+ex);return 1;}
   finally{Store.State=previous;Store.FilePath=oldPath;Directory.Delete(temp,true);}
  }
 }
}
