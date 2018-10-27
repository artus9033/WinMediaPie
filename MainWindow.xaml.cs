using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Resources;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace WinMediaPie
{
    public partial class MainWindow : MetroWindow
    {

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowLong(IntPtr hWnd, int index, IntPtr dwNewLong);
        [DllImport("user32.dll")]
        static extern bool MoveWindow(IntPtr hWndChild, int x, int y, int nWidth, int nHeight, bool bRepaint);

        const string APP_ID = "WinMediaPie";

        FloatingWindow floatingWindow;

        System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();

        /// <summary>
        /// Stores paths to icon assets
        /// </summary>
        class AppPaths
        {
            public static string appTmpPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), APP_ID);
            public static string iconPngPath = System.IO.Path.Combine(AppPaths.appTmpPath, "icon.png");
            public static string iconIcoPath = System.IO.Path.Combine(AppPaths.appTmpPath, "icon.ico");
        }

        /// <summary>
        /// Creates a MainWindow displaying app settings and bootstrapping the whole application
        /// </summary>
        public MainWindow()
        {
            this.PrepareFS();

            InitializeComponent();

            this.StateChanged += this.WindowStateChanged;
            this.Loaded += MainWindow_Loaded;

            var components = new System.ComponentModel.Container();
            var contextMenu = new System.Windows.Forms.ContextMenu();
            var menuItemExit = new System.Windows.Forms.MenuItem();

            // Initialize contextMenu1
            contextMenu.MenuItems.AddRange(
                        new System.Windows.Forms.MenuItem[] { menuItemExit });

            // Initialize menuItem1
            menuItemExit.Index = 0;
            menuItemExit.Text = "Exit";
            menuItemExit.Click += new System.EventHandler(this.ExitClick);

            this.notifyIcon.Icon = this.GetIconFromFile(AppPaths.iconIcoPath);
            this.notifyIcon.Visible = false;
            this.notifyIcon.Text = APP_ID;
            this.notifyIcon.DoubleClick += this.NotifyIconDoubleClicked;
            this.notifyIcon.ContextMenu = contextMenu;

            this.floatingWindow = new FloatingWindow();
        }

        /// <summary>
        /// Shows a Windows 10 ModernUI toast notification
        /// </summary>
        /// <param name="text">The content of the toast</param>
        public void Toast(string text = "")
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

        /// <summary>
        /// Unpacks app assets to a temporary directory
        /// </summary>
        private void PrepareFS()
        {
            if (!Directory.Exists(AppPaths.appTmpPath))
            {
                Directory.CreateDirectory(AppPaths.appTmpPath);
            }

            Uri iconPngPackUri = new Uri("pack://application:,,,/Assets/icon.png");
            this.EnsureUnpacked(iconPngPackUri, AppPaths.iconPngPath);

            Uri iconIcoPackUri = new Uri("pack://application:,,,/Assets/icon.png");
            this.EnsureUnpacked(iconIcoPackUri, AppPaths.iconIcoPath);
        }

        /// <summary>
        /// Makes sure an icon is unpacked to disk
        /// </summary>
        /// <param name="packUri">Pack URI under which the icon is accessible</param>
        /// <param name="diskPath">Destination path on disk to which the icon is unpacked</param>
        private void EnsureUnpacked(Uri packUri, string diskPath)
        {
            StreamResourceInfo sourceInfo = System.Windows.Application.GetResourceStream(packUri);
            FileStream ofstream = File.Create(diskPath);
            sourceInfo.Stream.Seek(0, SeekOrigin.Begin);
            sourceInfo.Stream.CopyTo(ofstream);
            ofstream.Close();
        }

        /// <summary>
        /// Loads a <see cref="System.Drawing.Icon"/> from disk
        /// </summary>
        /// <param name="path">Path to the icon on disk</param>
        /// <returns></returns>
        private System.Drawing.Icon GetIconFromFile(string path)
        {
            return System.Drawing.Icon.FromHandle(new Bitmap(path).GetHicon());
        }

        /// <summary>
        /// Closes descendant floating windows, shows, maximizes and focuses this window
        /// </summary>
        private void PutToForeground()
        {
            this.notifyIcon.Visible = false;
            this.WindowState = WindowState.Minimized;
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Focus();
            this.BringIntoView();
            this.floatingWindow.HideAllWindows();
            this.floatingWindow.Hide();
            Console.WriteLine("Bringing the window back to foreground...");
        }


        private void PutToBackground(bool hide = true)
        {
            this.notifyIcon.Visible = true;
            if (hide)
            {
                this.Hide();
                this.floatingWindow.Show();
            }
            this.Toast(" is running in background");
            Console.WriteLine("Hiding the window to background...");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.BeginInvoke((Action)delegate
            {
                this.PutToBackground(true);
            });
        }

        private void AboutClick(object sender, EventArgs e)
        {
            Console.WriteLine("Showing the 'about' dialog!");
            this.ShowMessageAsync("About the app", "WinMediaPie developed by Morys, Janus & Pawlica", MessageDialogStyle.Affirmative);
        }

        private void ExitClick(object sender, EventArgs e)
        {
            Console.WriteLine("Exiting the app!");
            Environment.Exit(0);
        }

        private void NotifyIconDoubleClicked(object sender, EventArgs e)
        {
            this.PutToForeground();
        }

        private void WindowStateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                PutToBackground(false);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            PutToBackground(true);
        }
    }
}
