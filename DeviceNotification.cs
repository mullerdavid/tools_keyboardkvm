using System;
using System.Runtime.InteropServices;

namespace KeyboardKVM
{
    internal static class DeviceNotification
    {
        public const int DBT_DEVNODES_CHANGED = 0x7;
        public const int DBT_DEVICEARRIVAL = 0x8000;
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        public const int DBT_DEVTYP_DEVICEINTERFACE = 0x5;
        public const int WM_DEVICECHANGE = 0x0219;
        public static readonly Guid GUID_BTHPORT_DEVICE_INTERFACE = new Guid("0850302A-B344-4fda-9BE9-90576B8D46F0");
        public static readonly Guid GUID_BTH_DEVICE_INTERFACE = new Guid("00F40965-E89D-4487-9890-87C3ABB211F4");
        public static readonly Guid GUID_BLUETOOTHLE_DEVICE_INTERFACE = new Guid("781AEE18-7733-4CE4-ADD0-91F41C67B592");
        public static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");
        public static readonly Guid GUID_DEVINTERFACE_KEYBOARD = new Guid("884B96C3-56EF-11D1-BC8C-00A0C91405DD");

        [StructLayout(LayoutKind.Sequential)]
        struct DEV_BROADCAST_HDR
        {
            public uint dbch_size;
            public uint dbch_devicetype;
            public uint dbcc_reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] dbcc_name;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, int Flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnregisterDeviceNotification(IntPtr Handle);

        public static IntPtr RegisterDeviceNotificationManaged(IntPtr hRecipient, Guid filter)
        {
            DEV_BROADCAST_DEVICEINTERFACE dbi = new DEV_BROADCAST_DEVICEINTERFACE();
            dbi.dbcc_size = Marshal.SizeOf(dbi);
            dbi.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE;
            dbi.dbcc_classguid = filter;

            IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf(dbi));
            Marshal.StructureToPtr(dbi, buffer, false);

            return RegisterDeviceNotification(hRecipient, buffer, 0);
        }

        public static string ExtractInfo(IntPtr lParam)
        {
            DEV_BROADCAST_HDR? hdr = (DEV_BROADCAST_HDR?)Marshal.PtrToStructure(lParam, typeof(DEV_BROADCAST_HDR));
            if (hdr.HasValue && hdr.Value.dbch_devicetype == DBT_DEVTYP_DEVICEINTERFACE)
            {
                DEV_BROADCAST_DEVICEINTERFACE? dbi = (DEV_BROADCAST_DEVICEINTERFACE?)Marshal.PtrToStructure(lParam, typeof(DEV_BROADCAST_DEVICEINTERFACE));
                if (dbi.HasValue)
                {
                    int size = dbi.Value.dbcc_size - (int)Marshal.OffsetOf(typeof(DEV_BROADCAST_DEVICEINTERFACE), "dbcc_name");
                    byte[] buffer = new byte[size];
                    Array.Copy(dbi.Value.dbcc_name, buffer, size);
                    string name = System.Text.Encoding.Unicode.GetString(buffer);
                    return name;
                }
            }
            return "";
        }

        public static void UnregisterDeviceNotificationManaged(IntPtr Handle)
        {
            UnregisterDeviceNotification(Handle);
        }
    }
}