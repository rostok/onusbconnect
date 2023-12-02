using System;
using System.Drawing;
using System.Windows.Forms;
using System.Management;
using System.Reflection;
using System.Diagnostics;

[assembly : AssemblyProduct("onUSBconnect")]
[assembly : AssemblyTitle("onUSBconnect")]
[assembly : AssemblyConfiguration("")]
[assembly : AssemblyCompany("rostok - https://github.com/rostok/")]
[assembly : AssemblyTrademark("rostok")]
[assembly : AssemblyCulture("")]
[assembly : AssemblyVersion("1.0.0.0")]
[assembly : AssemblyFileVersion("1.0.0.0")]

public class OnUSBConnect : Form
{
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;
    private string vendorID;
    private string productID;
    private string commandToRun;

    public OnUSBConnect(string[] args)
    {
        if (args.Length < 2)
        {
            MessageBox.Show("Usage: onusbconnect.exe VENDOR_ID:PRODUCT_ID command");
            Application.Exit();
            return;
        }

        var ids = args[0].ToUpper().Split(':');
        if (ids.Length != 2)
        {
            MessageBox.Show("Invalid VENDOR_ID:PRODUCT_ID format");
            Application.Exit();
            return;
        }

        vendorID = "VID_" + ids[0];
        productID = "PID_" + ids[1];
        commandToRun = String.Join(" ", args, 1, args.Length - 1);

        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add(vendorID+":"+productID);
        trayMenu.Items.Add(commandToRun, null, (sender, e) => Trigger());
        trayMenu.Items.Add("");
        trayMenu.Items.Add("Exit", null, (sender, e) => Application.Exit());

        trayIcon = new NotifyIcon();
        trayIcon.Text = "on-usb-connect\n\n"+vendorID+":"+productID;
        Icon appIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        trayIcon.Icon = appIcon;

        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;

        StartUSBWatcher();
    }

    private void StartUSBWatcher()
    {
        var watcher = new ManagementEventWatcher();
        //var query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
        //var query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBControllerDevice'");
		//var query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
		//var query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBControllerDevice'");
		//var query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
		
		//var query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 0.01 WHERE TargetInstance ISA 'Win32_USBHub'");
		//var query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 0.01 WHERE TargetInstance ISA 'Win32_USBControllerDevice'");
  	    //var query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_PnPEntity' AND TargetInstance.DeviceID LIKE '%USB%'");

  	    var query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE (TargetInstance ISA 'Win32_USBHub') OR (TargetInstance ISA 'Win32_PnPEntity') OR (TargetInstance ISA 'Win32_USBControllerDevice') ");
		
        watcher.EventArrived += (sender, e) => OnUSBEvent(e);
        watcher.Query = query;
        watcher.Start();
    }

    private void OnUSBEvent(EventArrivedEventArgs e)
    {
        ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
        string deviceID = instance.Properties["DeviceID"].Value.ToString();
		Console.WriteLine("inserted: "+deviceID);
        if (deviceID.Contains(vendorID) && deviceID.Contains(productID)) Trigger();
    }

    private void Trigger()
    {
        Console.WriteLine("USB detected trigger");
        // Run the command silently
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {commandToRun}",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        Process.Start(startInfo);
    }

    protected override void OnLoad(EventArgs e)
    {
        Visible = false; // Hide form window.
        ShowInTaskbar = false; // Remove from taskbar.
        base.OnLoad(e);
    }

    static void Main(string[] args)
    {
        Application.Run(new OnUSBConnect(args));
    }
}
