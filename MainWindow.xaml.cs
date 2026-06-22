using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Interop;


namespace maus
{
    public partial class MainWindow : Window
    {

        // =========================
        // Variables
        // =========================

        private System.Threading.Timer clickTimer;
        private bool isRunning = false;
        private Key selectedHotkey = Key.F2;
        private const int HOTKEY_ID = 1;
        private const int EMERGENCY_HOTKEY_ID = 2;
        private const int WM_HOTKEY = 0x0312;
        private const string APP_VERSION = "v1.1";


        // =========================
        // Konstruktor
        // =========================

        public MainWindow()
        {
            InitializeComponent();

            MouseButtonBox.Items.Add(
                new MouseClickType()
                {
                    Name = "Left Button",
                    Down = 0x0002,
                    Up = 0x0004
                }
            );

            MouseButtonBox.Items.Add(
                new MouseClickType()
                {
                    Name = "Middle Button",
                    Down = 0x0020,
                    Up = 0x0040
                }
            );

            MouseButtonBox.Items.Add(
                new MouseClickType()
                {
                    Name = "Right Button",
                    Down = 0x0008,
                    Up = 0x0010
                }
            );

            HotkeyBox.Text = selectedHotkey.ToString();
            MouseButtonBox.SelectedIndex = 0;

            VersionText.Text = APP_VERSION;
        }



        // =========================
        // Button Events
        // =========================

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Start_Clicker();
        }


        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop_Clicker();
        }


        // =========================
        // Eigene Funktionen
        // =========================

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr handle = new WindowInteropHelper(this).Handle;

            HwndSource source = HwndSource.FromHwnd(handle);

            source.AddHook(WndProc);

            RegisterMainHotkey();

            bool emergencySuccess = RegisterHotKey(
                handle,
                EMERGENCY_HOTKEY_ID,
                0,
                (uint)KeyInterop.VirtualKeyFromKey(Key.F6));

            if (!emergencySuccess)
            {
                MessageBox.Show(
                    "Emergency Hotkey konnte nicht registriert werden.");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;

            UnregisterMainHotkey();
            UnregisterHotKey(handle, EMERGENCY_HOTKEY_ID);

            base.OnClosed(e);
        }

        private IntPtr WndProc(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();

                if (hotkeyId == EMERGENCY_HOTKEY_ID)
                {
                    if (isRunning)
                    {
                        Stop_Clicker();
                    }
                }
                else if (hotkeyId == HOTKEY_ID)
                {
                    if (isRunning)
                    {
                        Stop_Clicker();
                    }
                    else
                    {
                        Start_Clicker();
                    }
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        private void SimulateMouse_Click()
        {
            int clickTypeIndex = 0;
            MouseClickType selected = null;

            Dispatcher.Invoke(() =>
            {
                clickTypeIndex = ClickTypeBox.SelectedIndex;
                selected = (MouseClickType)MouseButtonBox.SelectedItem;
            });

            if (selected == null) return;

            GetCursorPos(out POINT point);
            int clickCount = clickTypeIndex == 1 ? 2 : 1;

            for (int i = 0; i < clickCount; i++)
            {
                mouse_event(selected.Down, point.X, point.Y, 0, 0);
                mouse_event(selected.Up, point.X, point.Y, 0, 0);
            }
        }

        // unused keypboard simulation for future feature
        // private void SimulateKey_Press()
        // {
        //     keybd_event(0x41, 0, 0, 0);
        //     keybd_event(0x41, 0, 2, 0);
        // }

        private void IntervalBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void Start_Clicker()
        {
            StatusText.Text = "Status: On";

            int hours = 0;
            int minutes = 0;
            int seconds = 0;
            int milliseconds = 0;


            int.TryParse(HoursIntervalBox.Text, out hours);
            int.TryParse(MinutesIntervalBox.Text, out minutes);
            int.TryParse(SecondsIntervalBox.Text, out seconds);
            int.TryParse(MillisecondsIntervalBox.Text, out milliseconds);


            int interval =
                (hours * 60 * 60 * 1000)
                + (minutes * 60 * 1000)
                + (seconds * 1000)
                + milliseconds;

            if (interval < 1)
            {
                interval = 1;
            }

            clickTimer = new System.Threading.Timer(_ =>
            {
                SimulateMouse_Click();
            }, null, 0, interval);

            isRunning = true;
        }

        private void Stop_Clicker()
        {
            Dispatcher.Invoke(() => StatusText.Text = "Status: Off");
            clickTimer?.Dispose();
            clickTimer = null;
            isRunning = false;
        }

        private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F6)
            {
                return;
            }
            UnregisterMainHotkey();
            selectedHotkey = e.Key;
            RegisterMainHotkey();
            HotkeyBox.Text = selectedHotkey.ToString();
            e.Handled = true;
        }

        private void RegisterMainHotkey()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;

            RegisterHotKey(
                handle,
                HOTKEY_ID,
                0,
                (uint)KeyInterop.VirtualKeyFromKey(selectedHotkey));
        }

        private void UnregisterMainHotkey()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            UnregisterHotKey(handle, HOTKEY_ID);
        }

        // =========================
        // Windows API
        // =========================


        [DllImport("user32.dll")]
        public static extern void mouse_event(
            int dwFlags,
            int dx,
            int dy,
            int cButtons,
            int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern void keybd_event(
            byte bVk,
            byte bScan,
            uint dwFlags,
            int dwExtraInfo);


        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(
            IntPtr hWnd,
            int id,
            uint fsModifiers,
            uint vk);


        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(
            IntPtr hWnd,
            int id);

        // =========================
        // Hilfsdaten
        // =========================


        public struct POINT
        {
            public int X;
            public int Y;
        }

    }

    public class MouseClickType
    {
        public string Name { get; set; }
        public int Down { get; set; }
        public int Up { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}