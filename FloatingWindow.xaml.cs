using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace WinMediaPie
{
    /// <summary>
    /// Logika interakcji dla klasy PieWindow.xaml
    /// </summary>
    public partial class FloatingWindow : Window
    {

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr window, int index, int value);
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr window, int index);

        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xf020;
        private const int SC_MAXIMIZE = 0xf030;
        private const int SC_MOVE = 0xF010;
        private const int SC_SIZE = 0xF000;

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_APPWINDOW = 0x00040000;

        private PieWindow pieWindow;
        private bool IsBeingDisplayed = false;

        private bool IsFirstExposure = true;

        /// <summary>
        /// Floating window that is an activator and wrapper for PieWindow
        /// </summary>
        public FloatingWindow()
        {
            InitializeComponent();

            this.ShowInTaskbar = false;
            this.pieWindow = new PieWindow(this.ShowFloatingWindow);

            // First display the floating window anyway, to position it properly
            this.ShowFloatingWindow();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (IsFirstExposure)
            {
                if (this.pieWindow.ShouldBeDisplayedInitially())
                {
                    this.ShowPie();
                }
                IsFirstExposure = false;
            }
        }

        /// <summary>
        /// Hides all descendant windows
        /// </summary>
        public void HideAllWindows()
        {
            this.ShowFloatingWindow();
        }

        /// <summary>
        /// Hides the PieWindow and shows this floating window
        /// </summary>
        private void ShowFloatingWindow()
        {
            this.pieWindow.Hide();
            this.Show();
            System.Drawing.Rectangle workArea = Common.Helpers.WindowHelpers.CurrentScreen(this).Bounds;
            this.Top = workArea.Height * 0.5 - this.Height / 2;
            this.Left = workArea.Width - this.Width;
            this.IsBeingDisplayed = true;
        }

        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            this.ShowPie();
        }

        /// <summary>
        /// Hides this floating window and shows the PieWindow
        /// </summary>
        void ShowPie()
        {
            this.pieWindow.WindowState = WindowState.Minimized;
            this.pieWindow.Show();
            this.pieWindow.WindowState = WindowState.Normal;
            this.pieWindow.BringIntoView();
            this.pieWindow.Focus();
            HideSelf();
        }

        void HideSelf()
        {
            this.Hide();
            this.IsBeingDisplayed = false;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            base.OnClosing(e);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);

            // Make it gone frmo ALT+TAB
            int windowStyle = GetWindowLong(source.Handle, GWL_EXSTYLE);
            SetWindowLong(source.Handle, GWL_EXSTYLE, windowStyle | WS_EX_TOOLWINDOW);
        }

        // Makes the window unmovable, neither maxibizable nor minimalizable & unresizable
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SYSCOMMAND)
            {
                int wParamInt = wParam.ToInt32();
                if (wParamInt == SC_MINIMIZE || wParamInt == SC_MAXIMIZE || wParamInt == SC_MOVE || wParamInt == SC_SIZE)
                {
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        private void TogglePie(object sender, TouchEventArgs e)
        {
            this.TogglePie();
        }

        private void TogglePie(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.TogglePie();
        }

        /// <summary>
        /// Toggles the visibility of PieWindow
        /// </summary>
        private void TogglePie()
        {
            if (this.IsBeingDisplayed) this.ShowPie(); else this.ShowFloatingWindow();
            this.IsBeingDisplayed = !this.IsBeingDisplayed;
        }
    }
}
