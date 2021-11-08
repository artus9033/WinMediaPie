using NAudio.CoreAudioApi;
using System;

namespace WinMediaPie
{
    public class VolumeChangeEventArgs : EventArgs
    {
        public float volumePercent;
        public bool isMuted;
    }

    class NotificationClientImplementation : NAudio.CoreAudioApi.Interfaces.IMMNotificationClient
    {
        public event EventHandler<VolumeChangeEventArgs> VolumeChange;
        private MMDevice currentDefaultDevice = null;
        private MMDeviceEnumerator mMDeviceEnumerator;

        /// <summary>
        /// Class that listens for audio device change and for audio volume changes, and audio playback mute/unmute
        /// </summary>
        /// <remarks>
        /// Calling Initialize is needed after instantiating this class AND assigning a callback to <see cref="VolumeChange"/>
        /// </remarks>
        /// <param name="mMDeviceEnumerator">An instance of MMDeviceEnumerator that identifies the controlled sound device</param>
        public NotificationClientImplementation(MMDeviceEnumerator mMDeviceEnumerator)
        {
            this.mMDeviceEnumerator = mMDeviceEnumerator;

            currentDefaultDevice = mMDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            MaybeSetupVolumeNotificationListener();
        }

        /// <summary>
        /// Must be called after instantiating this class AND assigning a callback to <see cref="VolumeChange"/>
        /// </summary>
        internal void Initialize()
        {
            VolumeChange(this, new VolumeChangeEventArgs
            {
                volumePercent = currentDefaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100,
                isMuted = currentDefaultDevice.AudioEndpointVolume.Mute
            });
        }

        /// <summary>
        /// Sets the volume to specified percentage level
        /// </summary>
        /// <param name="newValue">New volume level (percent, 0 - 100)</param>
        internal void SetVolume(float newValue)
        {
            currentDefaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = newValue / 100;
        }

        /// <summary>
        /// Unmutes the current audio device
        /// </summary>
        internal void Unmute()
        {
            currentDefaultDevice.AudioEndpointVolume.Mute = false;
        }

        /// <summary>
        /// Mutes the current audio device
        /// </summary>
        internal void Mute()
        {
            currentDefaultDevice.AudioEndpointVolume.Mute = true;
        }

        /// <summary>
        /// Sets up audio device's event listeners if <see cref="currentDefaultDevice"/> is not null
        /// </summary>
        private void MaybeSetupVolumeNotificationListener()
        {
            if (currentDefaultDevice != null)
            {
                currentDefaultDevice.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
            }
        }

        /// <summary>
        /// Clears audio device's event listeners if <see cref="currentDefaultDevice"/> is not null\
        /// Probably needed when the default audio devices is changed
        /// </summary>
        private void MaybeClearVolumeNotificationListener()
        {
            if (currentDefaultDevice != null)
            {
                currentDefaultDevice.AudioEndpointVolume.OnVolumeNotification -= AudioEndpointVolume_OnVolumeNotification;
            }
        }

        public void OnDefaultDeviceChanged(DataFlow dataFlow, Role role, string defaultDeviceId)
        {
            Console.WriteLine($"Default playback device changed to {dataFlow.ToString()}");
            MaybeClearVolumeNotificationListener();
            currentDefaultDevice = mMDeviceEnumerator.GetDevice(defaultDeviceId);
            MaybeSetupVolumeNotificationListener();
        }

        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            Console.WriteLine($"Volume changed to {Math.Round(data.MasterVolume * 100)}%, {(data.Muted ? "muted" : "unmuted")}");

            VolumeChange(this, new VolumeChangeEventArgs
            {
                volumePercent = data.MasterVolume * 100,
                isMuted = data.Muted
            });
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
            // Do nothing
        }

        public void OnDeviceRemoved(string deviceId)
        {
            // Do nothing
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            Console.WriteLine($"Device {deviceId} state changed to {newState}");
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            // Do nothing
        }
    }
}
