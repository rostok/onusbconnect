# OnUSBConnect
Executes action when USB device of specified Vendor and Product ID is connected to the computer.
The rationale for this was easy swithing of input and monitors between two computers.
A common device for both machines was a USB hub switch providing mouse and keyboard. Unfortunately
the hub has only a single button switching connection but provides no extra feedback or message.
However switching monitors can be triggered on event of keyboard being plugged in.

# Usage
OnUSBConnect runs as windowless tray app. It will wait until a specified USB device is connected
to the computer. Once it is it will run the command passed to it. 

Syntax & options: 

    onusbconnect vendorid:productid command

# Installation
Place `onusbconnect.exe` somewhere safe and run it, for example like this:

    onusbconnect.exe 413c:2107 controlmymonitor.exe /SetValue Primary 60h 17 /SetValue Secondary 60h 17
    
To find the monitor options use https://www.nirsoft.net/utils/control_my_monitor.html
To get vendorid:productid use https://www.nirsoft.net/utils/usb_devices_view.html

# Building
Clone the repo and build running:

    compile.bat 

csc.exe should be in path. 
Image Magick's magick.exe is optional, as ico file is already in repo.

# License
MIT

USB_icon.svg is taken from Wikipedia