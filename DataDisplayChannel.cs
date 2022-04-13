using System;
using System.Runtime.InteropServices;


namespace KeyboardKVM
{
    internal static class DataDisplayChannel
    {
        public const byte INPUT_SELECT = 0x60;
        private const int PHYSICAL_MONITOR_DESCRIPTION_SIZE = 128;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PHYSICAL_MONITOR_DESCRIPTION_SIZE)]
            public string szPhysicalMonitorDescription;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MONITORINFOEX
        {
            public int Size;
            public RECT Monitor;
            public RECT WorkArea;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;

            public void Initialize()
            {
                cb = 0;
                DeviceName = new string((char)32, 32);
                DeviceString = new string((char)32, 128);
                DeviceID = new string((char)32, 128);
                DeviceKey = new string((char)32, 128);
                cb = Marshal.SizeOf(this);
            }
        }

        [Flags()]
        public enum DisplayDeviceStateFlags : int
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            /// <summary>The device is part of the desktop.</summary>
            PrimaryDevice = 0x4,
            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,
            /// <summary>The device is VGA compatible.</summary>
            VGACompatible = 0x10,
            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,
            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000,
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, MONITOR_DEFAULTTO dwFlags);

        [DllImport("Dxva2.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

        [DllImport("Dxva2.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, ref PHYSICAL_MONITOR physicalMonitorArray);

        [DllImport("Dxva2.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, ref PHYSICAL_MONITOR physicalMonitorArray);

        [DllImport("Dxva2.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, int dwNewValue);

        [DllImport("Dxva2.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr hMonitor, byte bVCPCode, ref IntPtr makeNull, out int currentValue, out int maxValue);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("User32.dll")]
        private static extern bool EnumDisplayDevices(byte[] lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, int dwFlags);

        internal enum MONITOR_DEFAULTTO
        {
            NULL = 0x00000000,
            PRIMARY = 0x00000001,
            NEAREST = 0x00000002,
        }

        public static byte[] ToLPTStr(this string str)
        {
            var lptArray = new byte[str.Length + 1];

            var index = 0;
            foreach (char c in str.ToCharArray())
                lptArray[index++] = Convert.ToByte(c);

            lptArray[index] = Convert.ToByte('\0');

            return lptArray;
        }

        private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

        public static IntPtr GetMonitorHandle(string filter = null)
        {
            IntPtr hMonitor = IntPtr.Zero;

            if (filter != null)
            {
                MonitorEnumDelegate MonitorEnumProc = new MonitorEnumDelegate((IntPtr hMonitorEnum, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData) => {
                    MONITORINFOEX mi = new MONITORINFOEX() { Size = Marshal.SizeOf(typeof(MONITORINFOEX)) };

                    if (GetMonitorInfo(hMonitorEnum, ref mi))
                    {
                        DISPLAY_DEVICE device = new DISPLAY_DEVICE();
                        device.Initialize();

                        if (EnumDisplayDevices(mi.DeviceName.ToLPTStr(), 0, ref device, 0))
                        {
                            if (mi.DeviceName.Contains(filter) || device.DeviceString.Contains(filter))
                            {
                                hMonitor = hMonitorEnum;
                            }
                        }
                    }

                    return true;
                });
                EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);
            }
            else
            {
                hMonitor = MonitorFromWindow(IntPtr.Zero, MONITOR_DEFAULTTO.PRIMARY);
            }

            return hMonitor;
        }

        public static void SetVCPFeatureManaged(IntPtr hMonitor, byte bVCPCode, int dwNewValue)
        {
            if (hMonitor == IntPtr.Zero) return;

            uint physicalMonitorCount = 0;
            GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out physicalMonitorCount);

            PHYSICAL_MONITOR pmon = new PHYSICAL_MONITOR();
            bool success = GetPhysicalMonitorsFromHMONITOR(hMonitor, physicalMonitorCount, ref pmon);

            success = SetVCPFeature(pmon.hPhysicalMonitor, bVCPCode, dwNewValue);
            success = DestroyPhysicalMonitors(physicalMonitorCount, ref pmon);
        }
    }
}
