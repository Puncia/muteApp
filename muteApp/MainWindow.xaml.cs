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

        private const int HOTKEY_ID = 9005;

        //Modifiers:
        private const uint MOD_NONE = 0x0000; //[NONE]
        private const uint MOD_ALT = 0x0001; //ALT
        private const uint MOD_CONTROL = 0x0002; //CTRL
        private const uint MOD_SHIFT = 0x0004; //SHIFT
        private const uint MOD_WIN = 0x0008; //WINDOWS
                                             //CAPS LOCK:
        private uint VK_BINDING = 0x76;

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

        private void bindButton_Click(object sender, RoutedEventArgs e)
        {
            isBinding = true;
            bindButton.Content = "binding...";
            UnregisterHotKey(handle, HOTKEY_ID);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!isBinding)
                return;

            //CTRL
            if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl))
            {
                BindHotkey(e.Key, ModifierKeys.Control);
                isBinding = false;
            }

            //ALT
            //if (((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) && (e.Key != Key.LeftAlt && e.Key != Key.RightAlt))
            //{
            //    bindButton.Content = "bind key";
            //    RegisterHotKey(handle, HOTKEY_ID, MOD_ALT, VK_BINDING = (uint)KeyInterop.VirtualKeyFromKey(e.Key));
            //    stateBox.Text += "Bound hotkey: ALT + " + e.Key + "\n";
            //    isBinding = false;
            //}
        }

        private void BindHotkey(Key k, ModifierKeys m)
        {
            bindButton.Content = "bind key";
            RegisterHotKey(handle, HOTKEY_ID, MOD_CONTROL, VK_BINDING = (uint)KeyInterop.VirtualKeyFromKey(k));
            stateBox.Text += "Bound hotkey: CTRL + " + k + "\n";
        }
    }

}