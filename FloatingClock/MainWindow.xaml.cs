using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace FloatingClock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Mouse position relative to window, set on LeftMouseDown
        /// </summary>
        private Point relativeMousePosition;

        /// <summary>
        /// For setting the time/date text
        /// </summary>
        private DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();

            // set up basic event handlers
            FloatingClockWindow.MouseMove += MainWindow_MouseMove;
            FloatingClockWindow.KeyDown += FloatingClockWindow_KeyDown;
            FloatingClockWindow.MouseLeftButtonDown += FloatingClockWindow_MouseLeftButtonDown;
            FloatingClockWindow.Loaded += FloatingClockWindow_Loaded;

            // instantiate and initialize the clock timer
            timer = new DispatcherTimer
            {
                Interval = System.TimeSpan.FromMilliseconds(1000)
            };
            timer.Tick += new System.EventHandler(Clock_Tick);
            timer.Start();
        }

        /// <summary>
        /// Use some Windows API to make the window disappear from Alt-Tab
        /// </summary>
        /// Courtesy of https://stackoverflow.com/a/551847/10148350
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FloatingClockWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }

        /// <summary>
        /// Called every interval, updates the clock and date displays
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clock_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            if (now.Month < 10)
                DateBlock.Text = now.ToString(" M/dd/yyyy");
            else
                DateBlock.Text = now.ToString("MM/dd/yyyy");

            if (now.Hour % 12 < 10)
                ClockBlock.Text = now.ToString(" h:mm");
            else
                ClockBlock.Text = now.ToString("hh:mm");
        }

        /// <summary>
        /// Capture the mouse cursor position relative to window when the user clicks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FloatingClockWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            relativeMousePosition = e.GetPosition(this);
        }

        /// <summary>
        /// Exit the program when the user hits Escape
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FloatingClockWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        /// <summary>
        /// Allows the user to drag the window around the screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point absoluteMousePosition = GetCursorPosition();
                double left = absoluteMousePosition.X - relativeMousePosition.X;
                double top = absoluteMousePosition.Y - relativeMousePosition.Y;
                Left = left; Top = top;
            }
        }

        #region Windows API

        // for simplicity and extensibility put these into enums
        [Flags]
        public enum ExtendedWindowStyles
        {
            WS_EX_TOOLWINDOW = 0x00000080
        }

        public enum GetWindowLongFields
        {
            GWL_EXSTYLE = (-20)
        }

        /// <summary>
        /// for grabbing the absolute cursor position from Windows
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        /// <summary>
        /// Grabs the absolute cursor position from Windows and converts it to a Point object
        /// </summary>
        /// <returns></returns>
        public static Point GetCursorPosition()
        {
            GetCursorPos(out POINT lpPoint);

            return lpPoint;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        public static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        public static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);

        /// <summary>
        /// Simple conversion from IntPtr to a int
        /// </summary>
        /// <param name="intPtr"></param>
        /// <returns></returns>
        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        /// <summary>
        /// Wrapper for Windows API interface
        /// </summary>
        /// Courtesy of https://stackoverflow.com/a/551847/10148350
        /// <param name="hWnd"></param>
        /// <param name="nIndex"></param>
        /// <param name="dwNewLong"></param>
        /// <returns></returns>
        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;

            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                int tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            } else
            {
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }
    }
    #endregion
}
