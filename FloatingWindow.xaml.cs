﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

        public FloatingWindow()
        {
            InitializeComponent();

            this.ShowInTaskbar = false;
            this.pieWindow = new PieWindow(this.Display);

            this.Display();
        }

        public void HideAllWindows()
        {
            this.Display();
        }

        private void Display() {
            this.pieWindow.Hide();
            this.Show();
            System.Drawing.Rectangle workArea = Common.Helpers.WindowHelpers.CurrentScreen(this).Bounds;
            this.Top = workArea.Height * 0.5 - this.Height / 2;
            this.Left = workArea.Width - this.Width;
            this.IsBeingDisplayed = true;
        }

        void LetGo()
        {
            this.Hide();
            this.pieWindow.Show();
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

            //Make it gone frmo the ALT+TAB
            int windowStyle = GetWindowLong(source.Handle, GWL_EXSTYLE);
            SetWindowLong(source.Handle, GWL_EXSTYLE, windowStyle | WS_EX_TOOLWINDOW);
        }

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

        private void Click(object sender, TouchEventArgs e)
        {
            this.Click();
        }

        private void Click(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.Click();
        }

        private void Click()
        {
            if (this.IsBeingDisplayed) this.LetGo(); else this.Display();
            this.IsBeingDisplayed = !this.IsBeingDisplayed;
        }
    }
}