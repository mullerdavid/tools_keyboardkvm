using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Configuration;

[assembly: AssemblyTitle("KeyboardKVM")]
[assembly: AssemblyDefaultAlias("KeyboardKVM")]
[assembly: AssemblyProduct("KeyboardKVM")]
[assembly: AssemblyDescription("KeyboardKVM")]
[assembly: AssemblyCompany("Deathbaron")]
[assembly: AssemblyCopyright("Deathbaron - 2022")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyCulture("")]

namespace KeyboardKVM
{
	public class KeyboardKVM : Form
	{

		private NotifyIcon trayIcon;
		private ContextMenu trayMenu;

		private string cKeyboardIdMatch;
		private string cMonitorMatch;
		private int cInputConnected;
		private int cInputRemoved;

		private int sTargetInput;
		private DateTime sShiftReleasedTime;
		private System.Threading.Timer timer;

		public KeyboardKVM(String[] args)
		{
			trayMenu = new ContextMenu();
			trayMenu.MenuItems.Add("Exit", OnExit);

			trayIcon = new NotifyIcon();
			trayIcon.Text = "KeyboardKVM";
			trayIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

			trayIcon.ContextMenu = trayMenu;
			trayIcon.Visible = true;
		}

		protected override void OnLoad(EventArgs e)
		{
			Visible = false;
			ShowInTaskbar = false;
			cKeyboardIdMatch = GetSettingDefault("KeyboardIdMatch", "");
			cMonitorMatch = GetSettingDefault("MonitorMatch", "");
			Int32.TryParse(GetSettingDefault("InputConnected", ""), out cInputConnected);
			Int32.TryParse(GetSettingDefault("InputRemoved", ""), out cInputRemoved);
			DeviceNotification.RegisterDeviceNotificationManaged(this.Handle, DeviceNotification.GUID_DEVINTERFACE_KEYBOARD);
			timer = new System.Threading.Timer(_ => SwitchTarget());
			sShiftReleasedTime = new DateTime(0);
			InterceptKeys.KeyboardProc shiftcheck = new InterceptKeys.KeyboardProc((int vkCode, bool down) => {
                    if ((vkCode == InterceptKeys.VK_LSHIFT || vkCode == InterceptKeys.VK_RSHIFT) && !down)
					{
						sShiftReleasedTime = DateTime.Now;
					}
                });
			InterceptKeys.SetHook(shiftcheck);
			base.OnLoad(e);
		}

		private void OnExit(object sender, EventArgs e)
		{
			trayIcon.Visible = false;
			Application.Exit();
		}

		private void SwitchTarget()
		{
			IntPtr hMonitor = DataDisplayChannel.GetMonitorHandle(cMonitorMatch);
			DataDisplayChannel.SetVCPFeatureManaged(hMonitor, DataDisplayChannel.INPUT_SELECT, sTargetInput);
		}

		private void SwitchTargetDelayed(int input)
		{
			sTargetInput = input;
			timer.Change(TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (m.Msg == DeviceNotification.WM_DEVICECHANGE)
			{
                string name = "";
				switch ((int)m.WParam)
				{
                    case DeviceNotification.DBT_DEVICEARRIVAL:
                        name = DeviceNotification.ExtractInfo(m.LParam);
						if (name.Contains(cKeyboardIdMatch))
						{
							SwitchTargetDelayed(cInputConnected);
						}
						break;
                    case DeviceNotification.DBT_DEVICEREMOVECOMPLETE:
                        name = DeviceNotification.ExtractInfo(m.LParam);
						double interval = (DateTime.Now - sShiftReleasedTime).TotalMilliseconds;
						if (name.Contains(cKeyboardIdMatch) && 2000<interval)
						{
							SwitchTargetDelayed(cInputRemoved);
						}
						break;
				}
			}
		}

		private static Configuration config = null;

		private static void InitializeSettings()
		{
			string configfile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "config.xml");
			config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap { ExeConfigFilename = configfile }, ConfigurationUserLevel.None);
		}

		protected static String GetSettingDefault(String key, String def)
		{
			KeyValueConfigurationCollection settings = config.AppSettings.Settings;
			if (settings[key] == null)
			{
				settings.Add(key, def);
			}
			return settings[key].Value;
		}

		private static void SaveSetting()
		{
			if (config != null)
				config.Save(ConfigurationSaveMode.Modified);
		}

		[STAThread]
		public static void Main(String[] args)
		{
			Mutex mutex = new System.Threading.Mutex(false, "KeyboardKVM-79a8f802-a938-4de7-afa9-ade612518743");
			try
			{
				if (mutex.WaitOne(0, false))
				{
					InitializeSettings();
					GetSettingDefault("KeyboardIdMatch", "{00001812-0000-1000-8000-00805f9b34fb}");
					GetSettingDefault("MonitorMatch", "DISPLAY1");
					GetSettingDefault("InputConnected", "");
					GetSettingDefault("InputRemoved", "");

					Application.EnableVisualStyles();
					Application.SetCompatibleTextRenderingDefault(false);
					Application.Run(new KeyboardKVM(args));

					SaveSetting();
				}
			}
			finally
			{
				if (mutex != null)
				{
					mutex.Close();
					mutex = null;
				}
			}
		}
	}
}
