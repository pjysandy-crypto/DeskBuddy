$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$sizes=@(16,24,32,48,64,128,256)
$images=@()
foreach($size in $sizes){
 $bitmap=New-Object Drawing.Bitmap $size,$size
 $graphics=[Drawing.Graphics]::FromImage($bitmap)
 $graphics.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
 $graphics.Clear([Drawing.Color]::Transparent)
 $graphics.ScaleTransform($size/64.0,$size/64.0)
 $background=New-Object Drawing.Drawing2D.GraphicsPath
 $background.AddArc(2,2,16,16,180,90)
 $background.AddArc(46,2,16,16,270,90)
 $background.AddArc(46,46,16,16,0,90)
 $background.AddArc(2,46,16,16,90,90)
 $background.CloseFigure()
 $mint=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(84,139,112))
 $cream=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255,248,228))
 $peach=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(239,172,139))
 $outline=New-Object Drawing.Pen ([Drawing.Color]::FromArgb(80,57,47)),2
 $graphics.FillPath($mint,$background)
 $graphics.DrawPath($outline,$background)
 $graphics.FillEllipse($cream,12,20,10,13)
 $graphics.FillEllipse($cream,23,11,10,15)
 $graphics.FillEllipse($cream,35,12,10,15)
 $graphics.FillEllipse($cream,45,23,9,12)
 $pad=New-Object Drawing.Drawing2D.GraphicsPath
 $pad.AddBezier(17,44,18,37,25,29,32,29)
 $pad.AddBezier(32,29,39,29,47,38,47,45)
 $pad.AddBezier(47,45,47,55,38,51,32,49)
 $pad.AddBezier(32,49,25,52,16,54,17,44)
 $pad.CloseFigure()
 $graphics.FillPath($cream,$pad)
 $heart=New-Object Drawing.Drawing2D.GraphicsPath
 $heart.AddBezier(32,45,16,36,27,32,32,37)
 $heart.AddBezier(32,37,37,32,48,36,32,45)
 $heart.CloseFigure()
 $graphics.FillPath($peach,$heart)
 $stream=New-Object IO.MemoryStream
 $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
 $images+=,@($size,$stream.ToArray())
 if($size -eq 256){$bitmap.Save((Join-Path $PSScriptRoot 'deskbuddy-icon.png'),[Drawing.Imaging.ImageFormat]::Png)}
 $stream.Dispose();$heart.Dispose();$pad.Dispose();$outline.Dispose();$mint.Dispose();$cream.Dispose();$peach.Dispose();$background.Dispose();$graphics.Dispose();$bitmap.Dispose()
}
$iconStream=[IO.File]::Create((Join-Path $PSScriptRoot 'deskbuddy.ico'))
$writer=New-Object IO.BinaryWriter $iconStream
try{
 $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$sizes.Count)
 $offset=6+16*$sizes.Count
 foreach($entry in $images){$dimension=if($entry[0] -eq 256){0}else{$entry[0]};$writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$entry[1].Length);$writer.Write([uint32]$offset);$offset+=$entry[1].Length}
 foreach($entry in $images){$writer.Write([byte[]]$entry[1])}
}finally{$writer.Dispose();$iconStream.Dispose()}
