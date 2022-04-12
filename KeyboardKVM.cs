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
		
		private NotifyIcon  trayIcon;
        private ContextMenu trayMenu;

		private string cKeyboardIdMatch;
		private int cInputConnected;
		private int cInputRemoved;

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
            Visible  = false;
            ShowInTaskbar = false;
			cKeyboardIdMatch = GetSettingDefault("KeyboardIdMatch", "");
			Int32.TryParse(GetSettingDefault("InputConnected", ""), out cInputConnected);
			Int32.TryParse(GetSettingDefault("InputRemoved", ""), out cInputRemoved);
			DeviceNotification.RegisterDeviceNotificationManaged(this.Handle, DeviceNotification.GUID_DEVINTERFACE_KEYBOARD);
            base.OnLoad(e);
        }

		private void OnExit(object sender, EventArgs e)
		{
            trayIcon.Visible = false;
			Application.Exit();
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
							DataDisplayChannel.SetVCPFeatureManaged(DataDisplayChannel.INPUT_SELECT, cInputConnected);
						}
						break;
                    case DeviceNotification.DBT_DEVICEREMOVECOMPLETE:
                        name = DeviceNotification.ExtractInfo(m.LParam);
						if (name.Contains(cKeyboardIdMatch))
						{
							DataDisplayChannel.SetVCPFeatureManaged(DataDisplayChannel.INPUT_SELECT, cInputRemoved);
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
			InitializeSettings();
			GetSettingDefault("KeyboardIdMatch", "{00001812-0000-1000-8000-00805f9b34fb}");
			GetSettingDefault("InputConnected", "");
			GetSettingDefault("InputRemoved", "");

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new KeyboardKVM(args));

			SaveSetting();
		}
	}
}