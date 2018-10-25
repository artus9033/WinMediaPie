using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Resources;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace WinMediaPie
{
    public partial class MainWindow : MetroWindow
    {
        public const int GWL_STYLE = (-16);
        public IntPtr WS_VISIBLE = (IntPtr)0x10000000L;

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowLong(IntPtr hWnd, int index, IntPtr dwNewLong);
        [DllImport("user32.dll")]
        static extern bool MoveWindow(IntPtr hWndChild, int x, int y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", EntryPoint = "GetDC")]
        public static extern IntPtr GetDC(IntPtr ptr);
        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

        public void windowedSndvol()
        {/*
            ProcessStartInfo psi = new ProcessStartInfo("sndvol.exe");
            psi.WindowStyle = ProcessWindowStyle.Maximized;
            psi.Arguments = "";
            Process p = Process.Start(psi);
            Thread.Sleep(500);
            SetWindowLong(p.MainWindowHandle, GWL_STYLE, WS_VISIBLE);
            SetParent(p.MainWindowHandle, sndvolPanel.Handle);
            MoveWindow(p.MainWindowHandle, 0, 0, sndvolPanel.Width, sndvolPanel.Height, true);*/
        }

        const string APP_ID = "WinMediaPie";

        System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();

        class AppPaths
        {
            public static string appTmpPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), APP_ID);
            public static string iconPngPath = System.IO.Path.Combine(AppPaths.appTmpPath, "icon.png");
            public static string iconIcoPath = System.IO.Path.Combine(AppPaths.appTmpPath, "icon.ico");
        }

        public MainWindow()
        {
            this.prepareFS();

            InitializeComponent();

            System.Windows.Forms.Timer timer;
            timer = new System.Windows.Forms.Timer();
            timer.Tick += new EventHandler(this.T_Tick);
            timer.Interval = 400;
            timer.Enabled = true;

            this.StateChanged += this.windowStateChanged;

            var components = new System.ComponentModel.Container();
            var contextMenu = new System.Windows.Forms.ContextMenu();
            var menuItemExit = new System.Windows.Forms.MenuItem();

            // Initialize contextMenu1
            contextMenu.MenuItems.AddRange(
                        new System.Windows.Forms.MenuItem[] { menuItemExit });

            // Initialize menuItem1
            menuItemExit.Index = 0;
            menuItemExit.Text = "Exit";
            menuItemExit.Click += new System.EventHandler(this.exitClick);

            this.notifyIcon.Icon = this.getIconFromFile(AppPaths.iconIcoPath);
            this.notifyIcon.Visible = false;
            this.notifyIcon.Text = APP_ID;
            this.notifyIcon.DoubleClick += this.notifyIconDoubleClicked;
            this.notifyIcon.ContextMenu = contextMenu;
        }

        private void bringBack()
        {
            this.notifyIcon.Visible = false;
            this.Show();
            this.Focus();
            this.BringIntoView();
            Debug.WriteLine("Bringing the window back...");
        }

        private void letGo(bool hide = true)
        {
            this.notifyIcon.Visible = true;
            if (hide)
            {
                this.Hide();
            }
            this.toast("Running in background");
            Debug.WriteLine("Hiding the window...");
        }

        public void toast(string text = "")
        {
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText03);

            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");

            stringElements[0].AppendChild(toastXml.CreateTextNode(APP_ID));
            stringElements[0].AppendChild(toastXml.CreateTextNode(text));

            XmlNodeList imageElements = toastXml.GetElementsByTagName("image");
            imageElements[0].Attributes.GetNamedItem("src").NodeValue = AppPaths.iconPngPath;

            ToastNotification toast = new ToastNotification(toastXml);

            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
        }

        private void prepareFS()
        {
            if (!Directory.Exists(AppPaths.appTmpPath))
            {
                Directory.CreateDirectory(AppPaths.appTmpPath);
            }

            Uri iconPngPackUri = new Uri("pack://application:,,,/Assets/icon.png");
            this.ensureUnpacked(iconPngPackUri, AppPaths.iconPngPath);

            Uri iconIcoPackUri = new Uri("pack://application:,,,/Assets/icon.png");
            this.ensureUnpacked(iconIcoPackUri, AppPaths.iconIcoPath);
        }

        private void ensureUnpacked(Uri packUri, string diskPath)
        {
            StreamResourceInfo sourceInfo = System.Windows.Application.GetResourceStream(packUri);
            FileStream ofstream = File.Create(diskPath);
            sourceInfo.Stream.Seek(0, SeekOrigin.Begin);
            sourceInfo.Stream.CopyTo(ofstream);
            ofstream.Close();
        }

        private System.Drawing.Icon getIconFromFile(string path)
        {
            return System.Drawing.Icon.FromHandle(new Bitmap(path).GetHicon());
        }

        private void aboutClick(object sender, EventArgs e)
        {
            Debug.WriteLine("Showing the 'about' dialog!");
            this.ShowMessageAsync("About the app", "WinMediaPie developed by Morys, Janus & Pawlica", MessageDialogStyle.Affirmative);
        }

        private void exitClick(object sender, EventArgs e)
        {
            Debug.WriteLine("Exiting the app!");
            Environment.Exit(0);
        }

        private void notifyIconDoubleClicked(object sender, EventArgs e)
        {
            this.bringBack();
        }

        private void windowStateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                letGo(false);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            letGo(true);
        }

        private void T_Tick(object sender, EventArgs e)
        {
            System.Drawing.Rectangle screen = Screen.PrimaryScreen.Bounds;

            IntPtr desktop = GetDC(IntPtr.Zero);
            Graphics g = Graphics.FromHdc(desktop);

            System.Drawing.Brush brush = new SolidBrush(System.Drawing.Color.FromArgb(158, 255, 0, 0));

            g.FillRectangle(brush, new System.Drawing.Rectangle(0, 0, screen.Right, screen.Bottom));

            g.Dispose();
            ReleaseDC(IntPtr.Zero, desktop);
        }
    }
}
