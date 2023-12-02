taskkill /f /im onusbconnect.exe 
del onusbconnect.exe  
magick -background white -density 320 USB_icon.svg -rotate -45 -shave 200 -resize 64x64 +dither -colors 2  icon.ico 
csc /win32icon:icon.ico  /target:winexe /reference:System.Windows.Forms.dll,System.Drawing.dll,System.Management.dll onusbconnect.cs 

