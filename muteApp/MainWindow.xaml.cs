using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Windows.Interop;

namespace muteApp
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool? muted;
        bool isBinding = false;
        IntPtr handle;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int nVirtKey);

        private const int HOTKEY_ID = 9005;
                
        const int VK_CONTROL = 0x11;
        const int VK_MENU = 0x12; //ALT
        const int VK_RMENU = 0xA5; //right ALT

        //Modifiers:
        private const uint MOD_NONE = 0x0000; //[NONE]
        private const uint MOD_ALT = 0x0001; //ALT
        private const uint MOD_CONTROL = 0x0002; //CTRL
        private const uint MOD_SHIFT = 0x0004; //SHIFT
        private const uint MOD_WIN = 0x0008; //WINDOWS
                                             //CAPS LOCK:
        private int VK_BINDING = 0x76;

        private HwndSource source;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            handle = new WindowInteropHelper(this).Handle;
            source = HwndSource.FromHwnd(handle);
            source.AddHook(HwndHook);

            BindHotkey(KeyInterop.KeyFromVirtualKey((int)VK_BINDING), ModifierKeys.Control);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:


                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == VK_BINDING)
                            {
                                string pname;
                                int pid = GetActiveWindowPID(out pname);
                                //Get current audio state to toggle
                                muted = VolumeMixer.GetApplicationMute(pid);

                                if (muted != null)
                                {
                                    VolumeMixer.SetApplicationMute(pid, (bool)(muted = !muted));
                                    stateBox.Text += "Setting app \'" + pname + "\' to " + muted.ToString() + "\n";
                                }
                                else
                                    stateBox.Text += "App \'" + pname + "\' has no volume settings\n";
                            }
                            stateBox.Focus();
                            stateBox.CaretIndex = stateBox.Text.Length;
                            stateBox.ScrollToEnd();

                            handled = true;
                            break;
                    }
                    break;                    
                case 0x100: //keypress
                    if (isBinding)
                    {
                        if ((GetAsyncKeyState(VK_RMENU) & 0x8000) != 0)
                        {
                            bindButton.Content = "LALT + ...";
                            if ((wParam.ToInt32() != VK_CONTROL) &&
                               (wParam.ToInt32() != VK_MENU))
                            {
                                isBinding = false;

                                BindHotkey(KeyInterop.KeyFromVirtualKey(wParam.ToInt32()), ModifierKeys.Control | ModifierKeys.Alt);
                            }
                        }
                        else if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
                        {
                            bindButton.Content = "CTRL + ...";
                            if((wParam.ToInt32() != VK_CONTROL) && //means we also hit our key, which is not a modifier key anymore
                               (wParam.ToInt32() != VK_MENU)) //we should also check if the MENU key (ALT) was pressed, because right ALT is CTRL + ALT
                            {
                                //we want to register a new binding
                                isBinding = false;
                                
                                BindHotkey(KeyInterop.KeyFromVirtualKey(wParam.ToInt32()), ModifierKeys.Control);
                            }
                        }
                    }
                    break;
                case 0x0104: //WM_SYSKEYDOWN, needed for ALT aka MENU key (only left one)
                    if(isBinding)
                    {
                        if ((GetAsyncKeyState(VK_MENU) & 0x8000) != 0)
                        {
                            bindButton.Content = "ALT + ...";
                            if (wParam.ToInt32() != VK_MENU)
                            {
                                isBinding = false;

                                BindHotkey(KeyInterop.KeyFromVirtualKey(wParam.ToInt32()), ModifierKeys.Alt);
                            }
                        }
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private int GetActiveWindowPID(out string pname)
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            pname = "";

            if (GetWindowText(handle, Buff, nChars) <= 0)
                return -1;

            foreach (Process p in Process.GetProcesses("."))
            {
                try
                {
                    if (p.MainWindowTitle.Length > 0)
                        if (p.MainWindowTitle == (Buff.ToString()))
                        {
                            pname = p.ProcessName;
                            return p.Id;
                        }
                }
                catch (Exception e)
                { }
            }

            return -1;
        }

        public MainWindow()
        {
            InitializeComponent();

            stateBox.Text = string.Empty;
        }
        //test
        private void bindButton_Click(object sender, RoutedEventArgs e)
        {
            isBinding = true;
            bindButton.Content = "binding...";
            UnregisterHotKey(handle, HOTKEY_ID);
        }

        private void BindHotkey(Key k, ModifierKeys m)
        {
            bindButton.Content = "bind key";
            RegisterHotKey(handle, HOTKEY_ID, (uint)m, Convert.ToUInt32(VK_BINDING = KeyInterop.VirtualKeyFromKey(k)));
            stateBox.Text += "Bound hotkey: " + m.ToString() + " + " + k + "\n";
        }
    }

}