using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Resources;

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

        FloatingWindow floatingWindow;

        System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();

        /// <summary>
        /// Creates a MainWindow displaying app settings and bootstrapping the whole application
        /// </summary>
        public MainWindow()
        {
            this.PrepareFS();

            InitializeComponent();

            this.StateChanged += this.WindowStateChanged;
            this.Loaded += MainWindow_Loaded;

            if (AutorunManager.IsAlreadyAddedToCurrentUserStartup() || AutorunManager.IsAlreadyAddedToAllUsersStartup())
            {
                autostartlocal.IsChecked = true;
            }
            
            this.InitializeContextMenu();

            this.floatingWindow = new FloatingWindow();
        }

        private void InitializeContextMenu()
        {
            var components = new System.ComponentModel.Container();
            var contextMenu = new System.Windows.Forms.ContextMenu();
            var menuItemSettings = new System.Windows.Forms.MenuItem();
            var menuItemAbout = new System.Windows.Forms.MenuItem();
            var menuItemExit = new System.Windows.Forms.MenuItem();

            // Initialize contextMenu
            contextMenu.MenuItems.AddRange(
                        new System.Windows.Forms.MenuItem[] { menuItemSettings, menuItemAbout, menuItemExit });

            int menuItemIndex = -1;

            // Initialize menuItemSettings
            menuItemSettings.Index = ++menuItemIndex;
            menuItemSettings.Text = "Settings";
            menuItemSettings.Click += new System.EventHandler(this.SettingsClick);

            // Initialize menuItemAbout
            menuItemAbout.Index = ++menuItemIndex;
            menuItemAbout.Text = "About";
            menuItemAbout.Click += new System.EventHandler(this.AboutClick);

            // Initialize menuItemExit
            menuItemExit.Index = ++menuItemIndex;
            menuItemExit.Text = "Exit";
            menuItemExit.Click += new System.EventHandler(this.ExitClick);

            this.notifyIcon.Icon = this.GetIconFromFile(Constants.AppPaths.iconIcoPath);
            this.notifyIcon.Visible = false;
            this.notifyIcon.Text = Constants.APP_ID;
            this.notifyIcon.DoubleClick += this.NotifyIconDoubleClicked;
            this.notifyIcon.ContextMenu = contextMenu;
        }

        /// <summary>
        /// Unpacks app assets to a temporary directory
        /// </summary>
        private void PrepareFS()
        {
            if (!Directory.Exists(Constants.AppPaths.appTmpPath))
            {
                Directory.CreateDirectory(Constants.AppPaths.appTmpPath);
            }

            Uri iconPngPackUri = new Uri("pack://application:,,,/Assets/icon.png");
            this.EnsureUnpacked(iconPngPackUri, Constants.AppPaths.iconPngPath);

            Uri iconIcoPackUri = new Uri("pack://application:,,,/Assets/icon.png");
            this.EnsureUnpacked(iconIcoPackUri, Constants.AppPaths.iconIcoPath);
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
            Console.WriteLine("Bringing the window back to foreground...");

            this.floatingWindow.HideAllWindows();
            this.floatingWindow.Hide();

            this.notifyIcon.Visible = false;
            this.Show();
            this.BringIntoView();
            this.Activate();
            this.Focus();
        }

        /// <summary>
        /// Hides this window and shows the floating ones instead
        /// </summary>
        /// <param name="hide">Whether to hide this window and force the FloatingWindow to be shown</param>
        private void PutToBackground(bool hide = true)
        {
            this.notifyIcon.Visible = true;
            if (hide)
            {
                this.Hide();
                this.floatingWindow.ShowSelfOrPie();

                Toaster.Toast($"{Constants.APP_ID} is running in background");
                Console.WriteLine("Hiding the window to background...");
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.BeginInvoke((Action)delegate
            {
                this.PutToBackground(true);
            });
        }

        private void DockToTrayClick(object sender, EventArgs e)
        {
            this.PutToBackground();
        }

        private void SettingsClick(object sender, EventArgs e)
        {
            Console.WriteLine("Showing the settings window!");
            this.PutToForeground();
        }

        private void AboutClick(object sender, EventArgs e)
        {
            this.PutToForeground();
            Console.WriteLine("Showing the 'about' dialog!");
            this.ShowMessageAsync("About the app", "WinMediaPie - lightweight floating volume & media playback controls sidebar for Windows 10 & 11 developed by artus9033, with KamykO's contribution", MessageDialogStyle.Affirmative);
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

        private void Autostartlocal_IsCheckedChanged(object sender, EventArgs e)
        {
            ToggleSwitch self = (ToggleSwitch)sender;

            try
            {
                if ((bool)self.IsChecked)
                {
                    AutorunManager.AddToCurrentUserAutorun();
                }
                else
                {
                    AutorunManager.RemoveFromCurrentUserAutorun();
                }
            }
            catch (Exception ex)
            {
                // Revert the toggle, as the operation did not succeed
                self.IsChecked = !(bool)self.IsChecked;
                Console.WriteLine($"Error adding the app to the current user's autorun: {ex.ToString()}");
                this.ShowMessageAsync("Oops, something went wrong", $"An unexpected error occured: {ex.ToString()}");
            }
        }
    }
}
