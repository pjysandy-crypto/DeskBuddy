using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace DeskBuddy {
 public static class GameAlert {
  public static DialogResult Show(string text,string title="친구의 메시지",MessageBoxButtons buttons=MessageBoxButtons.OK,MessageBoxIcon icon=MessageBoxIcon.None){
   try{var owner=Form.ActiveForm;if(owner==null||owner is PetForm)owner=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f.Visible&&!(f is PetForm));using(var dialog=new GameAlertForm(text,title,buttons,icon))return owner==null?dialog.ShowDialog():dialog.ShowDialog(owner);}
   catch{ return MessageBox.Show(text,title,buttons,icon); }
  }
  public static int Test(){try{PetArt.LoadCustom(Store.State.ImagePath);using(var dialog=new GameAlertForm("코인이 부족해요.\n퀘스트나 미니게임으로 코인을 모아보세요!","밥 / 간식",MessageBoxButtons.OK,MessageBoxIcon.None)){dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-2200,-1200);dialog.Show();string dir=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview");System.IO.Directory.CreateDirectory(dir);using(var image=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(System.IO.Path.Combine(dir,"game-alert.png"));}dialog.AcceptButton.PerformClick();if(dialog.DialogResult!=DialogResult.OK)throw new Exception("Alert OK");}using(var dialog=new GameAlertForm("일정을 삭제할까요?","일정 삭제",MessageBoxButtons.YesNo,MessageBoxIcon.None)){dialog.Show();dialog.CancelButton.PerformClick();if(dialog.DialogResult!=DialogResult.No)throw new Exception("Alert cancel must not confirm");}using(var dialog=new GameAlertForm("일정을 삭제할까요?","일정 삭제",MessageBoxButtons.YesNo,MessageBoxIcon.None)){dialog.Show();dialog.AcceptButton.PerformClick();if(dialog.DialogResult!=DialogResult.Yes)throw new Exception("Alert confirm");}System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"alert-validation.txt"),"PASS: themed alert rendering, OK, Yes/No confirmation, safe cancellation.");return 0;}catch(Exception ex){System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"alert-validation.txt"),"FAIL: "+ex);return 1;}}
 }
 public class GameAlertForm:Form {
  bool choice;
  public GameAlertForm(string message,string title,MessageBoxButtons buttons,MessageBoxIcon icon){choice=buttons==MessageBoxButtons.YesNo;Text=title;Icon=AppIdentity.Icon;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.CenterParent;ShowInTaskbar=false;TopMost=true;BackColor=Theme.Bg;AutoScaleMode=AutoScaleMode.None;DoubleBuffered=true;
   int bodyHeight;using(var g=CreateGraphics())using(var font=Theme.Font(10))bodyHeight=Math.Max(100,Math.Min(260,(int)Math.Ceiling(g.MeasureString(message,font,350).Height)+30));ClientSize=new Size(520,bodyHeight+148);
   var header=new PixelPanel{Location=new Point(8,8),Size=new Size(504,48),BackColor=icon==MessageBoxIcon.Error?Theme.Peach:Theme.Gold};Controls.Add(header);header.Controls.Add(Theme.Label(title,14,9,420,28,11));header.Controls.Add(Theme.Button("X",458,6,34,34,(s,e)=>Finish(choice?DialogResult.No:DialogResult.OK)));
   Controls.Add(new PetPortrait{Location=new Point(20,72),Size=new Size(108,100),BackColor=Theme.Cream});
   var messageBox=new TextBox{Text=message.Replace("\r\n","\n").Replace("\n","\r\n"),Location=new Point(148,80),Size=new Size(350,bodyHeight-12),Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None,BackColor=Theme.Cream,ForeColor=Theme.Ink,Font=Theme.Font(10),ScrollBars=bodyHeight>=260?ScrollBars.Vertical:ScrollBars.None,TabStop=false};Controls.Add(messageBox);
   int y=bodyHeight+82;var accept=Theme.Button(choice?"응, 진행할게":"확인",choice?270:162,y,choice?226:196,44,(s,e)=>Finish(choice?DialogResult.Yes:DialogResult.OK),true);Controls.Add(accept);AcceptButton=accept;
   if(choice){var cancel=Theme.Button("아니, 돌아갈래",24,y,226,44,(s,e)=>Finish(DialogResult.No));Controls.Add(cancel);CancelButton=cancel;}else CancelButton=accept;
  }
  void Finish(DialogResult result){DialogResult=result;Close();}
  protected override void OnFormClosing(FormClosingEventArgs e){if(DialogResult==DialogResult.None)DialogResult=choice?DialogResult.No:DialogResult.OK;base.OnFormClosing(e);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Theme.Frame(e.Graphics,ClientRectangle,Theme.Bg);Theme.Frame(e.Graphics,new Rectangle(12,64,496,Height-138),Theme.Cream);}
 }
}
