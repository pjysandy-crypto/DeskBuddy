using System;
using System.Drawing;
using System.Windows.Forms;

namespace DeskBuddy {
 public class SizePreviewCanvas:Control {
  PetForm pet;
  public SizePreviewCanvas(PetForm owner){pet=owner;DoubleBuffered=true;BackColor=Theme.Light;}
  public void UpdateSize(){Size=new Size(Math.Max(660,pet.PreviewSize.Width+20),Math.Max(580,pet.PreviewSize.Height+20));Invalidate();}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Theme.Frame(e.Graphics,ClientRectangle,BackColor);using(var image=pet.RenderSnapshot(true)){e.Graphics.DrawImageUnscaled(image,(Width-image.Width)/2,(Height-image.Height)/2);}}
 }
 public class SizePreviewForm:Form {
  public SizePreviewForm(PetForm pet){Text="캐릭터·말풍선·집중 타이머 미리보기";Icon=AppIdentity.Icon;ClientSize=new Size(720,716);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;AutoScaleMode=AutoScaleMode.None;BackColor=Theme.Bg;
   Controls.Add(Theme.Label("실제 크기 미리보기 · 슬라이더를 움직여 맞춰보세요.",20,10,680,28,11));
   var viewport=new Panel{Location=new Point(20,48),Size=new Size(680,464),AutoScroll=true};Controls.Add(viewport);var canvas=new SizePreviewCanvas(pet){Location=Point.Empty,Size=new Size(660,580)};viewport.Controls.Add(canvas);canvas.UpdateSize();
   AddSlider(pet,canvas,"캐릭터 크기",524,40,320,Store.State.PetSize,140,false);
   AddSlider(pet,canvas,"말풍선 크기",576,60,160,Store.State.BubbleScale,100,true);
   Controls.Add(Theme.Label("집중 타이머",20,635,160,28));var timer=new PixelSizeSlider{Minimum=60,Maximum=180,Value=Store.State.FocusTimerScale,Location=new Point(180,628),Size=new Size(320,36)};var timerLabel=Theme.Label(timer.Value+" %",516,635,85,28);Controls.Add(timer);Controls.Add(timerLabel);timer.ValueChanged+=(s,e)=>{Store.State.FocusTimerScale=timer.Value;pet.ApplyPetSize();timerLabel.Text=timer.Value+" %";((SizePreviewCanvas)canvas).UpdateSize();Store.Save();};Controls.Add(Theme.Button("기본",608,628,92,36,(s,e)=>timer.Value=100));
   Controls.Add(Theme.Label("바탕화면에도 바로 적용되고 자동으로 저장돼요.",20,680,680,25,10,Theme.Muted));
  }
  void AddSlider(PetForm pet,Control canvas,string text,int y,int min,int max,int value,int reset,bool bubble){Controls.Add(Theme.Label(text,20,y+7,160,28));var slider=new PixelSizeSlider{Minimum=min,Maximum=max,Value=value,Location=new Point(180,y),Size=new Size(320,36)};var label=Theme.Label(value+(bubble?" %":" px"),516,y+7,85,28);Controls.Add(slider);Controls.Add(label);slider.ValueChanged+=(s,e)=>{if(bubble)Store.State.BubbleScale=slider.Value;else Store.State.PetSize=slider.Value;pet.ApplyPetSize();label.Text=slider.Value+(bubble?" %":" px");((SizePreviewCanvas)canvas).UpdateSize();Store.Save();};Controls.Add(Theme.Button("기본",608,y,92,36,(s,e)=>slider.Value=reset));}
 }
}
