using System;
using System.Runtime.InteropServices;


namespace KeyboardKVM
{
    internal static class DataDisplayChannel
    {
        public const byte INPUT_SELECT = 0x60;
        private const int PHYSICAL_MONITOR_DESCRIPTION_SIZE = 128;

        [StructLayout(LayoutKind.Sequential)]
        private struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PHYSICAL_MONITOR_DESCRIPTION_SIZE)]
            public string szPhysicalMonitorDescription;
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

        internal enum MONITOR_DEFAULTTO
        {
            NULL = 0x00000000,
            PRIMARY = 0x00000001,
            NEAREST = 0x00000002,
        }
        public static void SetVCPFeatureManaged(byte bVCPCode, int dwNewValue)
        {
            IntPtr hMonitor = MonitorFromWindow(IntPtr.Zero, MONITOR_DEFAULTTO.PRIMARY);
            
            uint physicalMonitorCount = 0;
            GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out physicalMonitorCount);

            PHYSICAL_MONITOR pmon = new PHYSICAL_MONITOR();
            bool success = GetPhysicalMonitorsFromHMONITOR(hMonitor, physicalMonitorCount, ref pmon);

            success = SetVCPFeature(pmon.hPhysicalMonitor, bVCPCode, dwNewValue);
            success = DestroyPhysicalMonitors(physicalMonitorCount, ref pmon);
        }
    }
}
