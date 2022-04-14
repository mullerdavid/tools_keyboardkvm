using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace KeyboardKVM
{
	internal static class InterceptKeys
	{
		private const int WH_KEYBOARD_LL = 13;
		public const int WM_KEYDOWN = 0x0100;
		public const int WM_KEYUP = 0x0101;
		
		public const int VK_LSHIFT = 0xA0;
		public const int VK_RSHIFT = 0xA1;
		

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook,
			LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
			IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
		
		private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
		
		public delegate void KeyboardProc(int vkCode, bool down);
		
		private static IntPtr _hhook = IntPtr.Zero;
		private static HashSet<KeyboardProc> _hooks = new HashSet<KeyboardProc>();

		public static void SetHook(KeyboardProc proc)
		{
			if (_hhook == IntPtr.Zero )
			{
				using (Process curProcess = Process.GetCurrentProcess())
				using (ProcessModule curModule = curProcess.MainModule)
				{
					LowLevelKeyboardProc _proc = HookCallback;
					_hhook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
				}
			}
			
			_hooks.Add(proc);
		}
		
		public static void RemoveHook(KeyboardProc proc)
		{
			_hooks.Remove(proc);
		}

		private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			int msg = (int)wParam;
			bool down = (msg == WM_KEYDOWN);
			bool up = (msg == WM_KEYUP);
			if (nCode >= 0 && (down || up))
			{
				int vkCode = Marshal.ReadInt32(lParam);
				foreach (KeyboardProc proc in _hooks) 
				{
					proc(vkCode, down);
				}
			}
			return CallNextHookEx(_hhook, nCode, wParam, lParam);
		}
	}
}