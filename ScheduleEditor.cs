using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using ComponentFactory.Krypton.Toolkit;
namespace DeskBuddy {
 public class ScheduleDateTimePicker:Panel {
  public KryptonDateTimePicker DatePicker;public ComboBox TimeInput;
  public ScheduleDateTimePicker(){
   Size=new Size(468,34);BackColor=Theme.Cream;
   DatePicker=new KryptonDateTimePicker{Location=new Point(0,0),Size=new Size(300,34),Format=DateTimePickerFormat.Custom,CustomFormat="yyyy-MM-dd (ddd)",ShowUpDown=false,CalendarTodayDate=AppClock.Now.Date,CalendarFirstDayOfWeek=Day.Monday,CalendarTodayText="오늘"};
   DatePicker.StateCommon.Content.Font=Theme.Font(10);DatePicker.StateCommon.Back.Color1=Theme.Cream;DatePicker.StateCommon.Border.Color1=Theme.Line;
   Controls.Add(DatePicker);TimeInput=Times(314,0,154,"09:00");Controls.Add(TimeInput);Value=AppClock.Now.AddMinutes(30);
  }
  public static ComboBox Times(int x,int y,int w,string text){
   var c=new ComboBox{Location=new Point(x,y),Size=new Size(w,30),Font=Theme.Font(10),BackColor=Theme.Cream,ForeColor=Theme.Ink,DropDownStyle=ComboBoxStyle.DropDown,AutoCompleteMode=AutoCompleteMode.SuggestAppend,AutoCompleteSource=AutoCompleteSource.ListItems,MaxLength=5};
   for(int h=0;h<24;h++)for(int m=0;m<60;m+=15)c.Items.Add(h.ToString("00")+":"+m.ToString("00"));c.Text=text;return c;
  }
  public DateTime Value{get{DateTime t;if(!TryValue(out t))throw new FormatException("시간은 09:00처럼 입력해주세요.");return t;}set{DatePicker.Value=value.Date;TimeInput.Text=value.ToString("HH:mm");}}
  public bool TryValue(out DateTime value){TimeSpan time;value=DatePicker.Value.Date;if(!AppClock.Time(TimeInput.Text,out time))return false;value=DateTime.SpecifyKind(value+time,DateTimeKind.Unspecified);return true;}
  public void SetDate(DateTime day){DatePicker.Value=day.Date;}
 }
 public class SchedulePicture:Control {
  Bitmap image;
  public SchedulePicture(){DoubleBuffered=true;BackColor=Theme.Cream;}
  public void SetSource(string path){
   Bitmap next=null;if(!String.IsNullOrEmpty(path))using(var im=Image.FromFile(path))next=new Bitmap(im);
   if(image!=null)image.Dispose();image=next;Invalidate();
  }
  protected override void OnPaint(PaintEventArgs e){
   Theme.Frame(e.Graphics,ClientRectangle,BackColor);
   if(image==null){int side=Math.Max(1,Math.Min(Width,Height)-12);PetArt.Draw(e.Graphics,new Rectangle((Width-side)/2,(Height-side)/2,side,side),0,false,Store.State.Equipped);return;}
   float scale=Math.Min((Width-16)/(float)image.Width,(Height-16)/(float)image.Height);int w=(int)(image.Width*scale),h=(int)(image.Height*scale);
   var mode=e.Graphics.InterpolationMode;e.Graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
   e.Graphics.DrawImage(image,new Rectangle((Width-w)/2,(Height-h)/2,w,h));e.Graphics.InterpolationMode=mode;
  }
  protected override void Dispose(bool disposing){if(disposing&&image!=null)image.Dispose();base.Dispose(disposing);}
 }
 public class ScheduleEditor:Form {
  public TextBox TitleInput;public ScheduleDateTimePicker DateTimeInput;public ComboBox RepeatInput,CategoryInput,EveTimeInput,DurationInput;
  public ComboBox EndTimeInput; public PixelToggle EndNextDay; public PixelToggle EveToggle,AllDayToggle;TaskItem existing;string picturePath="";SchedulePicture preview;Label clockLabel;
  int selectedDuration=60;Label rangeSummary;Button[] durationButtons; Timer clock=new Timer{Interval=1000};
  public ScheduleEditor(TaskItem item,string preset=""){
   existing=item;Icon=AppIdentity.Icon;Text=item==null?"새 일정":"일정 수정";AutoScaleMode=AutoScaleMode.None;ClientSize=new Size(760,710);
   StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.None;BackColor=Theme.Bg;Font=Theme.Font(10);DoubleBuffered=true;
   Controls.Add(Theme.Label("SCHEDULE / "+(item==null?"새 일정":"일정 수정"),22,18,620,30,14));
   Controls.Add(Theme.Button("X",704,12,40,40,(s,e)=>{DialogResult=DialogResult.Cancel;Close();}));
   clockLabel=Theme.Label("",22,53,706,28,10,Theme.Muted);Controls.Add(clockLabel);UpdateClock();
   var form=new PixelPanel{Location=new Point(16,92),Size=new Size(728,340)};Controls.Add(form);
   form.Controls.Add(Theme.Label("일정 이름",16,10,480,24));
   TitleInput=Theme.Input(item==null?"":item.Title,16,38,471);TitleInput.MaxLength=100;form.Controls.Add(TitleInput);
   CategoryInput=Choice(new[]{"업무","회의","마감","개인","휴식","출근","퇴근","휴가","야근"},503,38,207);form.Controls.Add(CategoryInput);
   form.Controls.Add(Theme.Label("날짜",16,78,400,24,10,Theme.Muted));
   DateTimeInput=new ScheduleDateTimePicker{Location=new Point(16,104)};form.Controls.Add(DateTimeInput);
   DateTimeInput.Size=new Size(471,34);DateTimeInput.DatePicker.Size=new Size(471,34);
   form.Controls.Add(Theme.Button("오늘",503,102,98,38,(s,e)=>DateTimeInput.SetDate(AppClock.Now.Date)));
   form.Controls.Add(Theme.Button("내일",613,102,98,38,(s,e)=>DateTimeInput.SetDate(AppClock.Now.Date.AddDays(1))));
   var startCard=new PixelPanel{Location=new Point(16,151),Size=new Size(304,80),BackColor=Color.FromArgb(228,239,214)};form.Controls.Add(startCard);
   startCard.Controls.Add(Theme.Label("시작 시간",14,8,268,22,10,Theme.Muted));
   StyleTime(DateTimeInput.TimeInput,startCard);
   form.Controls.Add(Theme.Label("~",334,179,40,30,14,Theme.Muted));
   var endCard=new PixelPanel{Location=new Point(392,151),Size=new Size(320,80),BackColor=Color.FromArgb(255,231,196)};form.Controls.Add(endCard);
   endCard.Controls.Add(Theme.Label("종료 시간",14,8,280,22,10,Theme.Muted));
   EndTimeInput=ScheduleDateTimePicker.Times(0,0,100,"10:00");StyleTime(EndTimeInput,endCard);
   durationButtons=new Button[4];int[] durations={30,60,90,120};string[] labels={"30분","1시간","90분","2시간"};
   for(int i=0;i<4;i++){int minutes=durations[i];durationButtons[i]=Theme.Button(labels[i],16+i*80,242,72,32,(s,e)=>SetDuration(minutes));form.Controls.Add(durationButtons[i]);}
   form.Controls.Add(Theme.Label("다음날 종료",408,244,168,28,10,Theme.Muted));EndNextDay=new PixelToggle{Location=new Point(604,240),Text="다음날 종료"};form.Controls.Add(EndNextDay);
   rangeSummary=Theme.Label("",16,277,696,20,10,Theme.Purple);form.Controls.Add(rangeSummary);
   form.Controls.Add(Theme.Label("반복",16,305,60,28));RepeatInput=Choice(new[]{"한 번","매일","평일","매주"},82,305,238);form.Controls.Add(RepeatInput);
   form.Controls.Add(Theme.Label("종일 일정",408,305,168,28,10,Theme.Muted));AllDayToggle=new PixelToggle{Location=new Point(604,301),Text="종일 일정"};form.Controls.Add(AllDayToggle);
   var eve=new PixelPanel{Location=new Point(16,448),Size=new Size(728,68)};Controls.Add(eve);
   eve.Controls.Add(Theme.Label("전날 미리 알림",16,12,260,24));eve.Controls.Add(Theme.Label("내일 일정과 휴가를 먼저 알려줘요.",16, 40,450,24,10,Theme.Muted));
   EveToggle=new PixelToggle{Location=new Point(468, 20),Text="전날 미리 알림"};eve.Controls.Add(EveToggle);
   EveTimeInput=ScheduleDateTimePicker.Times(590,24,121,"18:00");eve.Controls.Add(EveTimeInput);
   var appearance=new PixelPanel{Location=new Point(16,532),Size=new Size(728,82)};Controls.Add(appearance);
   preview=new SchedulePicture{Location=new Point(16,8),Size=new Size(64,64)};appearance.Controls.Add(preview);
   appearance.Controls.Add(Theme.Label("일정 중 캐릭터 모습",96,8,300,24));
   appearance.Controls.Add(Theme.Button("PNG 선택",420,14,138,36,(s,e)=>ChoosePicture()));
   appearance.Controls.Add(Theme.Button("기본 모습",570,14,138,36,(s,e)=>{picturePath="";preview.SetSource("");}));
   appearance.Controls.Add(Theme.Label("시작~종료 시간 동안 선택한 모습을 유지해요.",96,44,608,24,10,Theme.Muted));
   DurationInput=Choice(new[]{"30분","1시간","2시간"},322,100,200);DurationInput.Visible=false;appearance.Controls.Add(DurationInput);

   Controls.Add(Theme.Label("일정 완료 +20 G / 야근 분류 또는 제목에 '야근' 포함: 완료 -20 G (최저 0 G)",22,625,714,30,10,Theme.Muted));
   Controls.Add(Theme.Button(item==null?"+ 일정 추가":"변경 저장", 16,658,470, 40,(s,e)=>{string error;if(!TrySave(out error))GameAlert.Show(error,"일정 설정");},true));
   Controls.Add(Theme.Button("취소",503,658,241, 40,(s,e)=>{DialogResult=DialogResult.Cancel;Close();}));
   if(item!=null){
    DateTimeInput.Value=Scheduler.Due(item);TitleInput.Text=item.Title;CategoryInput.SelectedItem=item.Category;RepeatInput.SelectedItem=item.Repeat;AllDayToggle.Checked=item.AllDay;EveToggle.Checked=item.EveReminder;EveTimeInput.Text=item.EveTime;
    DurationInput.SelectedIndex=item.AppearanceMinutes>=120?2:item.AppearanceMinutes>=60?1:0;picturePath=item.ScheduleImage;
    if(!String.IsNullOrEmpty(picturePath)&&File.Exists(picturePath))preview.SetSource(picturePath);
   }else if(preset=="출근"||preset=="퇴근"){
    TitleInput.Text=preset;CategoryInput.SelectedItem=preset;RepeatInput.SelectedItem="평일";
    var value=AppClock.Now.Date.AddHours(preset=="출근"?9:18);if(value<=AppClock.Now)value=value.AddDays(1);
    DateTimeInput.Value=Scheduler.FirstAllowed(value,"평일");
   }else if(preset=="야근"){
    TitleInput.Text="야근";CategoryInput.SelectedItem="야근";var end=AppClock.Now.Date.AddHours(21);if(end<=AppClock.Now)end=end.AddDays(1);DateTimeInput.Value=end;
   }else if(preset=="휴가"){
    TitleInput.Text="휴가";CategoryInput.SelectedItem="휴가";DateTimeInput.Value=AppClock.Now.Date.AddDays(1).AddHours(9);AllDayToggle.Checked=true;EveToggle.Checked=true;
   }
   DateTime finish=item!=null&&item.DurationMinutes>0?Scheduler.End(item):DateTimeInput.Value.AddHours(1);EndTimeInput.Text=finish.ToString("HH:mm");EndNextDay.Checked=finish.Date>DateTimeInput.Value.Date;DateTimeInput.TimeInput.TextChanged+=(s,e)=>{DateTime start;if(DateTimeInput.TryValue(out start)){var end=start.AddMinutes(selectedDuration);EndTimeInput.Text=end.ToString("HH:mm");EndNextDay.Checked=end.Date>start.Date;}};
   EndTimeInput.Enabled=EndNextDay.Enabled=!AllDayToggle.Checked;AllDayToggle.CheckedChanged+=(s,e)=>EndTimeInput.Enabled=EndNextDay.Enabled=!AllDayToggle.Checked;
   EveTimeInput.Enabled=EveToggle.Checked;EveToggle.CheckedChanged+=(s,e)=>EveTimeInput.Enabled=EveToggle.Checked;
   DurationInput.Enabled=!AllDayToggle.Checked;AllDayToggle.CheckedChanged+=(s,e)=>DurationInput.Enabled=!AllDayToggle.Checked;
   EndTimeInput.TextChanged+=(s,e)=>UpdateRange();EndNextDay.CheckedChanged+=(s,e)=>UpdateRange();AllDayToggle.CheckedChanged+=(s,e)=>UpdateRange();DateTimeInput.TimeInput.TextChanged+=(s,e)=>UpdateRange();UpdateRange();
   clock.Tick+=(s,e)=>UpdateClock();clock.Start();FormClosed+=(s,e)=>clock.Dispose();
  }
  
  static void StyleTime(ComboBox input,Panel card){card.Controls.Add(input);input.Location=new Point(14,34);input.Size=new Size(card.Width-28,36);input.Font=Theme.PixelFont(26);input.FlatStyle=FlatStyle.Flat;input.BackColor=card.BackColor;input.ForeColor=Theme.Ink;input.AccessibleName=card.Controls[0].Text;input.IntegralHeight=false;input.DropDownHeight=200;}
  void SetDuration(int minutes){DateTime start;if(!DateTimeInput.TryValue(out start))return;DateTime end=start.AddMinutes(minutes);EndTimeInput.Text=end.ToString("HH:mm");EndNextDay.Checked=end.Date>start.Date;UpdateRange();}
  void UpdateRange(){bool enabled=!AllDayToggle.Checked;DateTimeInput.TimeInput.Enabled=EndTimeInput.Enabled=EndNextDay.Enabled=enabled;foreach(var button in durationButtons){button.Enabled=enabled;button.BackColor=Theme.Cream;}if(!enabled){rangeSummary.ForeColor=Theme.Purple;rangeSummary.Text="하루 전체 일정";return;}DateTime start;TimeSpan time;if(!DateTimeInput.TryValue(out start)||!AppClock.Time(EndTimeInput.Text,out time)){rangeSummary.ForeColor=Color.FromArgb(177,90,105);rangeSummary.Text="시간을 14:00처럼 입력해주세요.";return;}DateTime end=start.Date+time;if(EndNextDay.Checked)end=end.AddDays(1);int minutes=(int)(end-start).TotalMinutes;if(minutes>0)selectedDuration=minutes;rangeSummary.ForeColor=minutes>0?Theme.Purple:Color.FromArgb(177,90,105);rangeSummary.Text=minutes>0?"총 "+(minutes/60>0?minutes/60+"시간 ":"")+(minutes%60>0?minutes%60+"분":"")+"  ·  "+start.ToString("HH:mm")+" ~ "+end.ToString("HH:mm")+(EndNextDay.Checked?" (다음날)":""):"종료 시간을 시작 시간보다 늦게 설정해주세요.";int[] values={30,60,90,120};for(int i=0;i<values.Length;i++)if(minutes==values[i])durationButtons[i].BackColor=Theme.Gold;}
  static ComboBox Choice(string[] options,int x,int y,int w){
   var c=new ComboBox{Location=new Point(x,y),Size=new Size(w,30),DropDownStyle=ComboBoxStyle.DropDownList,Font=Theme.Font(10),BackColor=Theme.Cream,ForeColor=Theme.Ink};c.Items.AddRange(options);c.SelectedIndex=0;return c;
  }
  void UpdateClock(){clockLabel.Text="현재 "+AppClock.Now.ToString("yyyy-MM-dd HH:mm:ss")+"  /  한국시간 · 24시간 표기";}
  void ChoosePicture(){
   using(var dialog=new OpenFileDialog{Filter="캐릭터 이미지|*.png;*.jpg;*.jpeg;*.bmp",Title="이 일정의 캐릭터 모습"}){
    if(dialog.ShowDialog()!=DialogResult.OK)return;
    try{using(var im=Image.FromFile(dialog.FileName))if(im.Width>4096||im.Height>4096||new FileInfo(dialog.FileName).Length>10*1024*1024)throw new Exception("10MB / 4096px 이하 이미지를 선택해주세요.");
     preview.SetSource(dialog.FileName);picturePath=dialog.FileName;
    }catch(Exception ex){GameAlert.Show(ex.Message,"이미지 선택");}
   }
  }
  public bool TrySave(out string error){
   error="";DateTime due;TimeSpan eve;
   if(String.IsNullOrWhiteSpace(TitleInput.Text)){error="일정 이름을 입력해주세요.";return false;}
   if(!DateTimeInput.TryValue(out due)){error="시간은 09:00 또는 18:30처럼 입력해주세요. (00:00~23:59)";return false;}
   if(EveToggle.Checked&&!AppClock.Time(EveTimeInput.Text,out eve)){error="전날 알림 시각도 18:00처럼 입력해주세요.";return false;}
   TimeSpan endTime;int duration=1440;if(!AllDayToggle.Checked){if(!AppClock.Time(EndTimeInput.Text,out endTime)){error="종료 시간은 15:30처럼 입력해주세요.";return false;}DateTime end=due.Date+endTime;if(EndNextDay.Checked)end=end.AddDays(1);if(end<=due){error="종료 시간은 시작 시간보다 늦어야 해요. 자정을 넘기면 다음날 종료를 켜주세요.";return false;}duration=(int)(end-due).TotalMinutes;}else due=due.Date;
   due=Scheduler.FirstAllowed(due,RepeatInput.Text);
   if(due.AddMinutes(duration)<=AppClock.Now&&(existing==null||due!=Scheduler.Due(existing))){error="현재보다 나중 날짜와 시각을 선택해주세요.";return false;}
   try{
    string copied=picturePath;
    if(!String.IsNullOrEmpty(picturePath)&&(existing==null||picturePath!=existing.ScheduleImage)){
     Directory.CreateDirectory(Store.Root);copied=Path.Combine(Store.Root,"schedule-"+Guid.NewGuid().ToString("N")+".png");
     using(var im=Image.FromFile(picturePath))using(var b=new Bitmap(im))b.Save(copied,ImageFormat.Png);
    }
    var t=new TaskItem{DurationMinutes=duration,Title=TitleInput.Text.Trim(),Category=CategoryInput.Text,Repeat=RepeatInput.Text,AllDay=AllDayToggle.Checked,EveReminder=EveToggle.Checked,EveTime=EveTimeInput.Text,AnchorTime=due.ToString("HH:mm"),ScheduleImage=copied,AppearanceMinutes=DurationInput.SelectedIndex==2?120:DurationInput.SelectedIndex==1?60:30};
    Scheduler.SetDue(t,due);
    int replacedIndex=-1;if(existing==null)Store.State.Tasks.Add(t);else{
     t.Id=existing.Id;t.LastCompletedLocal=existing.LastCompletedLocal;t.Done=existing.Done;
     if(due==Scheduler.Due(existing)){t.Notified=existing.Notified;t.EveNotified=existing.EveNotified;}
     int index=Store.State.Tasks.FindIndex(x=>x.Id==existing.Id);if(index<0){error="수정할 일정을 찾을 수 없어요.";return false;}replacedIndex=index;Store.State.Tasks[index]=t;
    }
    if(!Store.Save()){if(existing==null)Store.State.Tasks.Remove(t);else if(replacedIndex>=0)Store.State.Tasks[replacedIndex]=existing;error="저장하지 못했습니다.";return false;}
    DialogResult=DialogResult.OK;Close();return true;
   }catch(Exception ex){error="일정을 저장하지 못했습니다. "+ex.Message;return false;}
  }
  protected override void Dispose(bool disposing){if(disposing)clock.Dispose();base.Dispose(disposing);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(Theme.Line,4))e.Graphics.DrawRectangle(p,2,2,Width-4,Height-4);}
 }
}
