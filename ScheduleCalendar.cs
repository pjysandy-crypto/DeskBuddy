using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DeskBuddy {
 public class CalendarEntry {
  public TaskItem Task;public DateTime Due;
  public bool Projected {get{return Due!=Scheduler.Due(Task);}}
 }
 public static class CalendarDates {
  public static DateTime FirstCell(DateTime month){DateTime first=new DateTime(month.Year,month.Month,1);return first.AddDays(-(int)first.DayOfWeek);}
  public static List<CalendarEntry> OnDay(IEnumerable<TaskItem> tasks,DateTime day){
   var result=new List<CalendarEntry>();foreach(var task in tasks){DateTime due=Scheduler.Due(task);bool match=due.Date==day.Date||(due<day.Date.AddDays(1)&&Scheduler.End(task)>day.Date);
    if(!match&&!task.Done&&day.Date>due.Date){switch(task.Repeat){case "매일":match=true;break;case "평일":match=day.DayOfWeek!=DayOfWeek.Saturday&&day.DayOfWeek!=DayOfWeek.Sunday;break;case "매주":match=day.DayOfWeek==due.DayOfWeek;break;}}
    if(match)result.Add(new CalendarEntry{Task=task,Due=due.Date<day.Date&&Scheduler.End(task)>day.Date?due:day.Date+due.TimeOfDay});
   }return result.OrderBy(t=>!t.Task.AllDay).ThenBy(t=>t.Due).ThenBy(t=>t.Task.Title).ToList();
  }
  public static void Tests(){
   if(FirstCell(new DateTime(2026,10,1))!=new DateTime(2026,9,27)||FirstCell(new DateTime(2028,2,1)).DayOfWeek!=DayOfWeek.Sunday)throw new Exception("Calendar month alignment");
   var d=new Data();var t=new TaskItem{Title="주간",Repeat="매주"};Scheduler.SetDue(t,new DateTime(2026,10,5,9,0,0));d.Tasks.Add(t);
   if(OnDay(d.Tasks,new DateTime(2026,10,12)).Count!=1||!OnDay(d.Tasks,new DateTime(2026,10,12))[0].Projected||OnDay(d.Tasks,new DateTime(2026,10,13)).Count!=0)throw new Exception("Calendar weekly projection");
   t.Repeat="평일";if(OnDay(d.Tasks,new DateTime(2026,10,10)).Count!=0||OnDay(d.Tasks,new DateTime(2026,10,9)).Count!=1)throw new Exception("Calendar weekdays");
   t.Done=true;if(OnDay(d.Tasks,new DateTime(2026,10,9)).Count!=0)throw new Exception("Calendar completed projection");
  }
 }
 public class CalendarDay:Button {
  public DateTime Day;public bool InMonth,Selected;public List<CalendarEntry> Entries;
  public CalendarDay(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;Theme.Frame(g,ClientRectangle,Selected?Theme.Gold:InMonth?Theme.Cream:Theme.Bg);
   Color ink=!InMonth?Theme.Muted:Day.DayOfWeek==DayOfWeek.Sunday?Color.FromArgb(173,80,85):Day.DayOfWeek==DayOfWeek.Saturday?Color.FromArgb(68,117,166):Theme.Ink;
   using(var font=Theme.PixelFont(16))Theme.Text(g,Day.Day.ToString(),font,ink,new Rectangle(9,5,30,20));
   if(Day==AppClock.Now.Date){using(var pen=new Pen(Theme.Purple,2))g.DrawRectangle(pen,7,4,27,20);}
   for(int i=0;i<Math.Min(2,Entries.Count);i++){var entry=Entries[i];using(var font=Theme.PixelFont(12))using(var brush=new SolidBrush(entry.Task.Done?Theme.Muted:GoogleCalendar.Imported(entry.Task)?Theme.Purple:Theme.Ink))using(var format=new StringFormat(StringFormatFlags.NoWrap)){format.Trimming=StringTrimming.EllipsisCharacter;g.DrawString(entry.Task.Title,font,brush,new Rectangle(8,28+i*17,Width-16,17),format);}}
   if(Entries.Count>2)using(var font=Theme.PixelFont(12))Theme.Text(g,"+"+(Entries.Count-2)+"개",font,Theme.Muted,new Rectangle(8,62,Width-16,14));
   if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(5,5,Width-10,Height-10));
  }
 }
 public class ScheduleCalendarForm:Form {
  DateTime month,selected;Label monthLabel,dateLabel,detail;Panel grid;ListBox agenda;Button complete,edit,remove;PetForm pet;
  public ScheduleCalendarForm(PetForm owner){pet=owner;selected=AppClock.Now.Date;month=new DateTime(selected.Year,selected.Month,1);Text="DeskBuddy · 일정 달력";Icon=AppIdentity.Icon;AutoScaleMode=AutoScaleMode.None;ClientSize=new Size(984,682);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;BackColor=Theme.Bg;Font=Theme.Font(10);
   Controls.Add(Theme.Label("MY CALENDAR / 일정 달력",20,12,740,40,22));
   Controls.Add(Theme.Button("<",20,67,48,40,(s,e)=>ChangeMonth(-1)));monthLabel=Theme.Label("",82,72,350,28,13);Controls.Add(monthLabel);
   Controls.Add(Theme.Button(">",440,67,48,40,(s,e)=>ChangeMonth(1)));Controls.Add(Theme.Button("오늘",500,67,84,40,(s,e)=>SelectDate(AppClock.Now.Date)));
   grid=new Panel{Location=new Point(16,118),Size=new Size(572,532),BackColor=Theme.Bg};Controls.Add(grid);
   var right=new PixelPanel{Location=new Point(604,67),Size=new Size(364,583)};Controls.Add(right);
   dateLabel=Theme.Label("",14,14,336,32,13);right.Controls.Add(dateLabel);
   agenda=new ListBox{Location=new Point(14,56),Size=new Size(336,312),DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=62,BorderStyle=BorderStyle.None,BackColor=Theme.Cream,Font=Theme.Font(10)};right.Controls.Add(agenda);
   agenda.DrawItem+=(s,e)=>{if(e.Index<0)return;var row=(CalendarEntry)agenda.Items[e.Index];Theme.Fill(e.Graphics,(e.State&DrawItemState.Selected)!=0?Theme.Light:Theme.Cream,e.Bounds.X,e.Bounds.Y,e.Bounds.Width,e.Bounds.Height);Theme.Text(e.Graphics,row.Task.Title,agenda.Font,row.Task.Done?Theme.Muted:Theme.Ink,new Rectangle(8,e.Bounds.Y+5,e.Bounds.Width-16,25));Theme.Text(e.Graphics,(Scheduler.TimeRange(row.Task,row.Due))+" · "+(GoogleCalendar.Imported(row.Task)?"Google":row.Task.Category)+(row.Projected?" · 반복 예정":row.Task.Done?" · 완료":""),agenda.Font,Theme.Muted,new Rectangle(8,e.Bounds.Y+33,e.Bounds.Width-16,22));};
   detail=Theme.Label("",14,373,336,54,10,Theme.Muted);right.Controls.Add(detail);agenda.SelectedIndexChanged+=(s,e)=>UpdateActions();agenda.DoubleClick+=(s,e)=>EditSelected();
   right.Controls.Add(Theme.Button("+ 선택한 날짜에 일정 추가",14,435,336,40,(s,e)=>{using(var editor=new ScheduleEditor(null)){editor.DateTimeInput.Value=selected.AddHours(9);if(editor.ShowDialog(this)==DialogResult.OK)Reload();}},true));
   complete=Theme.Button("완료",14,486,160,38,(s,e)=>CompleteSelected());right.Controls.Add(complete);
   edit=Theme.Button("수정",186,486,164,38,(s,e)=>EditSelected());right.Controls.Add(edit);
   remove=Theme.Button("삭제",14,533,336,34,(s,e)=>DeleteSelected());right.Controls.Add(remove);Reload();
  }
  public void SelectDate(DateTime day){selected=day.Date;month=new DateTime(day.Year,day.Month,1);Reload();}
  public void ChangeMonth(int delta){month=month.AddMonths(delta);selected=new DateTime(month.Year,month.Month,Math.Min(selected.Day,DateTime.DaysInMonth(month.Year,month.Month)));Reload();}
  public void Reload(){
   monthLabel.Text=month.ToString("yyyy년 M월");foreach(Control c in grid.Controls.Cast<Control>().ToArray())c.Dispose();grid.Controls.Clear();
   string[] names={"일","월","화","수","목","금","토"};for(int i=0;i<7;i++){var label=Theme.Label(names[i],i*82,0,78,26,10,i==0?Color.FromArgb(173,80,85):i==6?Color.FromArgb(68,117,166):Theme.Muted);label.TextAlign=ContentAlignment.MiddleCenter;grid.Controls.Add(label);}
   DateTime first=CalendarDates.FirstCell(month);for(int i=0;i<42;i++){DateTime day=first.AddDays(i);var button=new CalendarDay{Day=day,InMonth=day.Month==month.Month,Selected=day==selected,Entries=CalendarDates.OnDay(Store.State.Tasks,day),Location=new Point(i%7*82,30+i/7*82),Size=new Size(78,78),AccessibleName=day.ToString("yyyy-MM-dd")+" 일정"};button.Click+=(s,e)=>SelectDate(day);grid.Controls.Add(button);}
   dateLabel.Text=selected.ToString("M월 d일 (ddd)");agenda.Items.Clear();foreach(var entry in CalendarDates.OnDay(Store.State.Tasks,selected))agenda.Items.Add(entry);if(agenda.Items.Count>0)agenda.SelectedIndex=0;UpdateActions();
  }
  CalendarEntry Selection {get{return agenda.SelectedItem as CalendarEntry;}}
  void UpdateActions(){var row=Selection;bool actual=row!=null&&!row.Projected;complete.Enabled=actual&&!row.Task.Done&&row.Due<=AppClock.Now;edit.Enabled=row!=null&&!GoogleCalendar.Imported(row.Task);remove.Enabled=actual&&!GoogleCalendar.Imported(row.Task);complete.Text=row!=null&&Scheduler.Overtime(row.Task)?"완료 -20 G":"완료 +20 G";detail.Text=row==null?"이날은 일정이 없어요.\n새 일정을 추가해보세요!":row.Projected?"반복 예정 일정이에요.\n수정하면 반복 일정 전체에 적용돼요.":GoogleCalendar.Imported(row.Task)?"구글에서 가져온 일정이에요.\n수정·삭제는 구글에서 해주세요.":row.Task.Done?"완료한 일정이에요. 잘했어요!":"일정 시간이 지나면 완료할 수 있어요.";}
  void EditSelected(){var row=Selection;if(row==null)return;if(GoogleCalendar.Imported(row.Task)){GameAlert.Show("구글 캘린더에서 수정하면 다음 갱신에 반영돼요.");return;}using(var editor=new ScheduleEditor(row.Task))if(editor.ShowDialog(this)==DialogResult.OK)Reload();}
  void CompleteSelected(){var row=Selection;if(row==null||row.Projected||row.Due>AppClock.Now)return;if(Rules.Complete(row.Task,Store.State)){Store.Save();pet.Say(Scheduler.Overtime(row.Task)?"야근 끝! 이제 푹 쉬자. -20 G":"QUEST CLEAR! +20 G",false);Reload();}}
  void DeleteSelected(){var row=Selection;if(row==null||row.Projected||GoogleCalendar.Imported(row.Task))return;if(GameAlert.Show("선택한 일정을 삭제할까요?\n반복 일정은 전체가 삭제돼요.","일정 삭제",MessageBoxButtons.YesNo)==DialogResult.Yes){Store.State.Tasks.Remove(row.Task);Store.Save();Reload();}}
  public static int TestUi(){var previous=Store.State;try{CalendarDates.Tests();Store.State=new Data();foreach(int day in new[]{1,7,7,7,16,31}){var task=new TaskItem{Title=day==7?"구글 회의와 준비 자료 확인":"일정 샘플",Id=day==7?"gcal:"+Guid.NewGuid():Guid.NewGuid().ToString()};Scheduler.SetDue(task,new DateTime(2026,10,day,9,0,0));Store.State.Tasks.Add(task);}using(var pet=new PetForm())using(var form=new ScheduleCalendarForm(pet)){form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-2200,-1200);form.Show();form.SelectDate(new DateTime(2026,10,7));Application.DoEvents();if(form.agenda.Items.Count!=3||form.grid.Controls.OfType<CalendarDay>().Count()!=42)throw new Exception("Calendar date selection");using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"month-calendar-preview.png"));}form.SelectDate(new DateTime(2028,2,29));form.ChangeMonth(12);if(form.selected!=new DateTime(2029,2,28))throw new Exception("Calendar leap-year navigation");}File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"month-calendar-validation.txt"),"PASS: month alignment, 42-cell grid, date filtering, leap-year navigation, weekly/weekday projections, completed-state visibility, calendar rendering.");return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"month-calendar-validation.txt"),"FAIL: "+ex);return 1;}finally{Store.State=previous;}}
 }
}
