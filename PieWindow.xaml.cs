﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WinMediaPie
{

    /// <summary>
    /// PieWindow logic
    /// </summary>
    public partial class PieWindow : Window
    {

        // Aero Glass externs

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

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
        public const int KEYEVENTF_EXTENDEDKEY = 0x0001; // Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002; // Key up flag

        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xf020;
        private const int SC_MAXIMIZE = 0xf030;
        private const int SC_MOVE = 0xF010;
        private const int SC_SIZE = 0xF000;

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_APPWINDOW = 0x00040000;

        private const int HIDE_WINDOW_DELAY = 600;

        private Action displayParent;
        private Task closeSelfTask = null;
        private int lastCloseSelfTaskId = -1;
        int suppressVolSliderValueChanges = 0;

        private NAudio.CoreAudioApi.MMDeviceEnumerator deviceEnum = new NAudio.CoreAudioApi.MMDeviceEnumerator();
        private NotificationClientImplementation notificationClient;
        private NAudio.CoreAudioApi.Interfaces.IMMNotificationClient notifyClient;

        private bool isMuted;
        private float volumePercent;

        private UIElement lastFocusedElement;

        public bool ShouldBeDisplayedInitially()
        {
            return (bool) Properties.Settings.Default.RememberKeepPieWindowOpen;
        }

        /// <summary>
        /// Creates a PieWindow
        /// </summary>
        /// <param name="displayParent">Callback to a function which hides this window and shows the parent window</param>
        public PieWindow(Action displayParent)
        {
            this.displayParent = displayParent;

            InitializeComponent();

            //lastFocusedElement = playPauseButton;

            playPauseButton.GotKeyboardFocus += HandleNewElementFocused;
            previousMediaButton.GotKeyboardFocus += HandleNewElementFocused;
            nextMediaButton.GotKeyboardFocus += HandleNewElementFocused;
            muteButton.GotKeyboardFocus += HandleNewElementFocused;

            keepMeOpenToggleButton.IsChecked = Properties.Settings.Default.RememberKeepPieWindowOpen;

            System.Drawing.Rectangle workArea = Common.Helpers.WindowHelpers.CurrentScreen(this).Bounds;
            this.Top = workArea.Height * 0.5 - this.Height / 2;
            this.Left = workArea.Width - this.Width;

            this.ShowInTaskbar = false;

            volumeSlider.ValueChanged += VolumeSlider_ValueChanged;

            notificationClient = new NotificationClientImplementation(deviceEnum);
            notificationClient.VolumeChange += NotificationClient_VolumeChange;
            notifyClient = notificationClient;
            deviceEnum.RegisterEndpointNotificationCallback(notifyClient);
            notificationClient.Initialize();
        }

        internal void HandleNewElementFocused(object sender, RoutedEventArgs e)
        {
            lastFocusedElement = (UIElement) sender;
        }

        internal void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);

            var accent = new AccentPolicy();
            var accentStructSize = Marshal.SizeOf(accent);
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
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
                    var visualBrushResource = new MahApps.Metro.IconPacks.PackIconMaterial();

                    if (muted)
                    {
                        visualBrushResource.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.VolumeOff;
                        volumeSlider.IsEnabled = false;
                        volumeText.Opacity = 0.7;
                        volumeText.TextDecorations = new TextDecorationCollection(TextDecorations.Strikethrough);
                    }
                    else
                    {
                        visualBrushResource.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.VolumeHigh;
                        volumeSlider.IsEnabled = true;
                        volumeText.Opacity = 1;
                        volumeText.TextDecorations = new TextDecorationCollection();
                    }

                    muteButtonIcon.Visual = visualBrushResource;

                    suppressVolSliderValueChanges++;
                    volumeSlider.Value = percent;
                    volumeText.Text = $"{(int)Math.Round(percent)}%";
                }
            );
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            Dispatcher d = Dispatcher.CurrentDispatcher;
            var self = this;
            Action action = () =>
            {
                if (!self.IsMouseOver && (bool)!self.keepMeOpenToggleButton.IsChecked)
                {
                    this.Back();
                }
            };

            this.closeSelfTask = new Task((thisTaskId) =>
            {
                System.Threading.Thread.Sleep(HIDE_WINDOW_DELAY);
                if (self.lastCloseSelfTaskId == (int)thisTaskId)
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

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (suppressVolSliderValueChanges > 0)
            {
                suppressVolSliderValueChanges--;
            }
            else
            {
                notificationClient.SetVolume((float)e.NewValue);
            }
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

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (this.IsVisible)
            {
                this.EnableBlur();

                var targetFocusElement = lastFocusedElement ?? playPauseButton;

                if (Keyboard.FocusedElement != targetFocusElement)
                {
                    this.BringIntoView();
                    this.Focus();
                    this.Activate();
                    targetFocusElement.Focus();
                    Keyboard.Focus(targetFocusElement);
                }
            }
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

        private void Back()
        {
            this.displayParent();
        }

        private void SendKeyboardEvent(byte eventCode)
        {
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

        private void KeepMeOpenToggleButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RememberKeepPieWindowOpen = (bool) keepMeOpenToggleButton.IsChecked;
            Properties.Settings.Default.Save();
        }
    }
}
