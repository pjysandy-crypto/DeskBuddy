using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace DeskBuddy {
 public static class AlphaWindow {
  [StructLayout(LayoutKind.Sequential)] struct PointNative {public int X,Y;public PointNative(int x,int y){X=x;Y=y;}}
  [StructLayout(LayoutKind.Sequential)] struct SizeNative {public int Width,Height;public SizeNative(int w,int h){Width=w;Height=h;}}
  [StructLayout(LayoutKind.Sequential,Pack=1)] struct Blend {public byte Operation,Flags,Alpha,Format;}
  [DllImport("user32.dll",SetLastError=true)] static extern bool UpdateLayeredWindow(IntPtr window,IntPtr dc,ref PointNative pos,ref SizeNative size,IntPtr source,ref PointNative origin,uint key,ref Blend blend,uint flags);
  [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr window);
  [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr window,IntPtr dc);
  [DllImport("gdi32.dll",SetLastError=true)] static extern IntPtr CreateCompatibleDC(IntPtr dc);
  [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
  [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
  [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
  public static void Update(IntPtr window,Point location,Bitmap bitmap){
   IntPtr screen=GetDC(IntPtr.Zero),memory=IntPtr.Zero,handle=IntPtr.Zero,previous=IntPtr.Zero;
   try{
    memory=CreateCompatibleDC(screen);if(memory==IntPtr.Zero)throw new Win32Exception();
    handle=bitmap.GetHbitmap(Color.FromArgb(0));previous=SelectObject(memory,handle);
    var pos=new PointNative(location.X,location.Y);var size=new SizeNative(bitmap.Width,bitmap.Height);var origin=new PointNative(0,0);
    var blend=new Blend{Operation=0,Flags=0,Alpha=255,Format=1};
    if(!UpdateLayeredWindow(window,screen,ref pos,ref size,memory,ref origin,0,ref blend,2))throw new Win32Exception(Marshal.GetLastWin32Error());
   }finally{if(previous!=IntPtr.Zero)SelectObject(memory,previous);if(handle!=IntPtr.Zero)DeleteObject(handle);if(memory!=IntPtr.Zero)DeleteDC(memory);if(screen!=IntPtr.Zero)ReleaseDC(IntPtr.Zero,screen);}
  }
 }
}