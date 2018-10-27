﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WinMediaPie
{
    /// <summary>
    /// Logika interakcji dla klasy PieWindow.xaml
    /// </summary>
    public partial class PieWindow : Window
    {
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr window, int index, int value);
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll", SetLastError = true)]
#pragma warning disable IDE1006 // Style nazewnictwa
        public static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, IntPtr extraInfo);
#pragma warning restore IDE1006 // Style nazewnictwa

        public const int VK_MEDIA_NEXT_TRACK = 0xB0;
        public const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        public const int VK_MEDIA_PREV_TRACK = 0xB1;
        public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag

        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xf020;
        private const int SC_MAXIMIZE = 0xf030;
        private const int SC_MOVE = 0xF010;
        private const int SC_SIZE = 0xF000;

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_APPWINDOW = 0x00040000;

        private Action displayParent;
        private Task closeSelfTask = null;
        private int lastCloseSelfTaskId = -1;

        private NAudio.CoreAudioApi.MMDeviceEnumerator deviceEnum = new NAudio.CoreAudioApi.MMDeviceEnumerator();
        private NotificationClientImplementation notificationClient;
        private NAudio.CoreAudioApi.Interfaces.IMMNotificationClient notifyClient;

        private bool isMuted;
        private float volumePercent;

        /// <summary>
        /// Creates a PieWindow
        /// </summary>
        /// <param name="displayParent">Callback to a function which hides this window and shows the parent window</param>
        public PieWindow(Action displayParent)
        {
            this.displayParent = displayParent;

            InitializeComponent();

            System.Drawing.Rectangle workArea = Common.Helpers.WindowHelpers.CurrentScreen(this).Bounds;
            this.Top = workArea.Height * 0.5 - this.Height / 2;
            this.Left = workArea.Width - this.Width;

            this.ShowInTaskbar = false;

            glosnoscSlider.ValueChanged += GlosnoscSlider_ValueChanged;

            notificationClient = new NotificationClientImplementation(deviceEnum);
            notificationClient.VolumeChange += NotificationClient_VolumeChange;
            notifyClient = notificationClient;
            deviceEnum.RegisterEndpointNotificationCallback(notifyClient);
            notificationClient.Initialize();
        }

        /// <summary>
        /// Mutes or unmutes the current audio playback device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MuteOrUnmute(object sender, RoutedEventArgs e)
        {
            if (isMuted)
            {
                notificationClient.Unmute();
            }
            else
            {
                notificationClient.Mute();
            }
        }

        /// <summary>
        /// Updates the UI buttons and slider values
        /// </summary>
        private void UpdateUI()
        {
            var muted = isMuted;
            var percent = volumePercent;
            System.Windows.Application.Current.Dispatcher.Invoke(
                () =>
                {
                    if (muted)
                    {
                        muteButtonIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/volume-high.png"));
                        glosnoscSlider.IsEnabled = false;
                    }
                    else
                    {
                        muteButtonIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/volume-off.png"));
                        glosnoscSlider.IsEnabled = true;
                    }
                    glosnoscSlider.Value = percent;
                }
            );
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            Dispatcher d = Dispatcher.CurrentDispatcher;
            var self = this;
            Action action = ()=>
            {
                if (!self.IsMouseOver)
                {
                    this.Back();
                }
            };

            this.closeSelfTask = new Task((thisTaskId) =>
            {
                System.Threading.Thread.Sleep(1500);
                if (self.lastCloseSelfTaskId == (int) thisTaskId)
                {
                    d.BeginInvoke(action);
                }
            }, ++this.lastCloseSelfTaskId);
            this.closeSelfTask.Start();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key.Equals(Key.Escape))
            {
                this.Back();
            }
        }

        private void GlosnoscSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            notificationClient.SetVolume((float)e.NewValue);
        }

        private void NotificationClient_VolumeChange(object sender, VolumeChangeEventArgs e)
        {
            isMuted = e.isMuted;
            volumePercent = e.volumePercent;

            UpdateUI();
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

        private void Back(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.Back();
        }

        private void Back(object sender, System.Windows.Input.TouchEventArgs e)
        {
            this.Back();
        }

        private void Back()
        {
            this.displayParent();
        }

        private void SendKeyboardEvent(byte eventCode){
            keybd_event(eventCode, 0, KEYEVENTF_EXTENDEDKEY, IntPtr.Zero);
            keybd_event(eventCode, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
        }

        private void PreviousMediaClick(object sender, RoutedEventArgs e)
        {
            SendKeyboardEvent(VK_MEDIA_PREV_TRACK);
        }

        private void NextMediaClick(object sender, RoutedEventArgs e)
        {
            SendKeyboardEvent(VK_MEDIA_NEXT_TRACK);
        }

        private void PlayPauseMediaClick(object sender, RoutedEventArgs e)
        {
            SendKeyboardEvent(VK_MEDIA_PLAY_PAUSE);
        }
    }
}
