using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices; // Needed for P/Invoke and StructLayout

[assembly: AssemblyProduct("onUSBconnect")]
[assembly: AssemblyTitle("onUSBconnect")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("rostok - https://github.com/rostok/")]
[assembly: AssemblyTrademark("rostok")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("1.3.0.0")] // Incremented version
[assembly: AssemblyFileVersion("1.3.0.0")]

public class OnUSBConnect : Form
{
    // --- Win32 API Constants and Structures ---

    // For RegisterDeviceNotification
    private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
    private const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x00000004; // Important: Listen broadly first
    private static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED"); // GUID for USB devices

    // Window message
    private const int WM_DEVICECHANGE = 0x0219;

    // WM_DEVICECHANGE event types (wParam)
    private const int DBT_DEVICEARRIVAL = 0x8000;
    private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
    private const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;

    // Structure for DEV_BROADCAST_DEVICEINTERFACE
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] // Use Unicode (Auto might work too)
    private struct DEV_BROADCAST_DEVICEINTERFACE
    {
        public int dbcc_size;
        public int dbcc_devicetype;
        public int dbcc_reserved;
        public Guid dbcc_classguid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)] // Adjust size if needed, but 255 is common
        public string dbcc_name;
    }

    // Structure for DEV_BROADCAST_HDR (nested structure, optional but good practice)
    [StructLayout(LayoutKind.Sequential)]
    public struct DEV_BROADCAST_HDR
    {
       public int dbch_size;
       public int dbch_devicetype;
       public int dbch_reserved;
    }

    // --- Win32 API Functions (P/Invoke) ---
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] // Use Unicode
    private static extern IntPtr RegisterDeviceNotification(
        IntPtr hRecipient,
        IntPtr NotificationFilter, // Need to marshal the structure
        int Flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterDeviceNotification(IntPtr Handle);

    // --- Class Members ---
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;
    private string vendorID;    // e.g., VID_1A2B
    private string productID;   // e.g., PID_3C4D
    private string deviceIdCheckString; // String to search for in the device path, e.g., "VID_1A2B&PID_3C4D"
    private string commandToRun;
    private IntPtr notificationHandle = IntPtr.Zero; // Handle for device notification registration


    public OnUSBConnect(string[] args)
    {
        if (args.Length < 2)
        {
            MessageBox.Show("Usage: onusbconnect.exe VENDOR_ID:PRODUCT_ID command");
            Application.Exit();
        }
        var ids = args[0].ToUpper().Split(':');
        if (ids.Length != 2)
        {
            MessageBox.Show("Invalid VENDOR_ID:PRODUCT_ID format (e.g., 1A2B:3C4D)");
            Application.Exit();
        }
        try
        {
            int currentProcessId = Process.GetCurrentProcess().Id;
            foreach (Process proc in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName))
            {
                if (proc.Id != currentProcessId)
                { try { proc.Kill(); } catch { /* Ignore */ } }
            }
        } catch (Exception ex) { Console.WriteLine($"Process check error: {ex.Message}"); }

        vendorID = "VID_" + ids[0];
        productID = "PID_" + ids[1];
        // Device paths often look like \\?\USB#VID_xxxx&PID_yyyy#[instance_id]#{guid}
        // So we search for the combined VID & PID part. Case-insensitive check later.
        deviceIdCheckString = $"{vendorID}&{productID}";
        commandToRun = String.Join(" ", args, 1, args.Length - 1);

        // --- Tray icon and menu setup remains the same ---
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

        // Registration will happen in OnLoad after the Handle is created
    }

    // Override OnLoad to register for device notifications *after* the window handle is created
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Visible = false; // Hide form window.
        ShowInTaskbar = false; // Remove from taskbar.
        RegisterForDeviceNotifications();
    }

    private void RegisterForDeviceNotifications()
    {
        // Prepare the structure for registration
        DEV_BROADCAST_DEVICEINTERFACE notificationFilter = new DEV_BROADCAST_DEVICEINTERFACE();
        notificationFilter.dbcc_size = Marshal.SizeOf(notificationFilter);
        notificationFilter.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE;
        notificationFilter.dbcc_reserved = 0;
        notificationFilter.dbcc_classguid = GUID_DEVINTERFACE_USB_DEVICE; // Filter for USB devices

        // Allocate memory and marshal the structure
        IntPtr filterPtr = IntPtr.Zero;
        try
        {
            filterPtr = Marshal.AllocHGlobal(notificationFilter.dbcc_size);
            Marshal.StructureToPtr(notificationFilter, filterPtr, true);

            // Register for notifications
            notificationHandle = RegisterDeviceNotification(this.Handle, filterPtr, DEVICE_NOTIFY_WINDOW_HANDLE);

            if (notificationHandle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"Failed to register device notification. Error code: {error}");
                MessageBox.Show($"Failed to register for USB device notifications. Error: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Consider exiting if registration fails critically
            }
            else
            {
                Console.WriteLine("Successfully registered for USB device notifications.");
            }
        }
        catch (Exception ex)
        {
             Console.WriteLine($"Exception during device notification registration: {ex.Message}");
             MessageBox.Show($"Exception registering for USB device notifications: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Override the form's window procedure to intercept messages
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m); // Call base class processing

        if (m.Msg == WM_DEVICECHANGE && m.LParam != IntPtr.Zero) // Check if it's our message and lParam is valid
        {
            int eventType = m.WParam.ToInt32();

            // Only process arrival events for now
            if (eventType == DBT_DEVICEARRIVAL)
            {
                // Marshal lParam to DEV_BROADCAST_HDR to check device type
                DEV_BROADCAST_HDR hdr = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_HDR));

                if (hdr.dbch_devicetype == DBT_DEVTYP_DEVICEINTERFACE)
                {
                     Console.WriteLine("WM_DEVICECHANGE: DBT_DEVICEARRIVAL for DBT_DEVTYP_DEVICEINTERFACE received.");
                    // Marshal lParam to the full DEV_BROADCAST_DEVICEINTERFACE structure
                    DEV_BROADCAST_DEVICEINTERFACE devInterface =
                        (DEV_BROADCAST_DEVICEINTERFACE)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_DEVICEINTERFACE));

                    // The dbcc_name field contains the device path
                    string devicePath = devInterface.dbcc_name;
                    Console.WriteLine($"  Device Path: {devicePath}");


                    // Check if the device path contains our specific VID & PID combination
                    // Use case-insensitive comparison
                    if (!string.IsNullOrEmpty(devicePath) &&
                        devicePath.IndexOf(deviceIdCheckString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"*** MATCH FOUND! Target device ({deviceIdCheckString}) arrived. Triggering command. ***");
                        Trigger();
                    }
                    else
                    {
                         Console.WriteLine($"  ... does not match target '{deviceIdCheckString}'.");
                    }
                }
            }
        }
    }


    private void Trigger()
    {
        Console.WriteLine($"Executing command: {commandToRun}");
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{commandToRun}\"", // Encapsulate command in quotes
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command: {ex.Message}");
            trayIcon?.ShowBalloonTip(2000, "Execution Error", $"Failed to run command: {ex.Message}", ToolTipIcon.Error);
        }
    }


    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.Run(new OnUSBConnect(args));
    }
}
