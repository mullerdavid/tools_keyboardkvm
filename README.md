Compile:
SET DOTNET_PATH=c:\Windows\Microsoft.NET\Framework\v4.0.30319\
%DOTNET_PATH%csc.exe /target:winexe /optimize /out:keyboardkvm.exe /win32icon:icon.ico *.cs

Edit config.xml and place it next to the executable.

Right click on tray icon to exit.

KeyboardIdMatch can be used to match a keyboard InstanceId. The program is checking that the event contains this string.

To get keyboard GUID you can use the Get-PnpDevice powershell cmdlet.
For example the following for BT keyboards.
Get-PnpDevice -Class 'Keyboard' | Where-Object InstanceId -like '*{00001812-0000-1000-8000-00805f9b34fb}*'

InputConnected and InputRemoved can be found with ControlMyMonitor for example. Check the VCP Code 0x60 Input Select.
https://www.nirsoft.net/utils/control_my_monitor.html