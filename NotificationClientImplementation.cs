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
        /// <example>
        /// Example usage:
        /// <code>
        /// notificationClient = new NotificationClientImplementation(deviceEnum);
        /// notificationClient.VolumeChange += NotificationClient_VolumeChange;
        /// notifyClient = notificationClient;
        /// deviceEnum.RegisterEndpointNotificationCallback(notifyClient);
        /// notificationClient.Initialize();
        /// </code>
        /// </example>
        /// <param name="mMDeviceEnumerator">An instance of MMDeviceEnumerator to be utilized by this instance</param>
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

        /// <summary>
        /// This handles default audio playback device change
        /// </summary>
        /// <param name="dataFlow"></param>
        /// <param name="role"></param>
        /// <param name="defaultDeviceId"></param>
        public void OnDefaultDeviceChanged(DataFlow dataFlow, Role role, string defaultDeviceId)
        {
            Console.WriteLine("OnDefaultDeviceChanged --> {0}", dataFlow.ToString());
            MaybeClearVolumeNotificationListener();
            currentDefaultDevice = mMDeviceEnumerator.GetDevice(defaultDeviceId);
            MaybeSetupVolumeNotificationListener();
        }

        /// <summary>
        /// This handles volume level and audio mute changes
        /// </summary>
        /// <param name="data"></param>
        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            Console.WriteLine("New Volume {0}", data.MasterVolume);
            Console.WriteLine("Muted      {0}", data.Muted);

            VolumeChange(this, new VolumeChangeEventArgs
            {
                volumePercent = data.MasterVolume * 100,
                isMuted = data.Muted
            });
        }

        /// <summary>
        /// Handler executed when a new audio playback device is added
        /// </summary>
        /// <param name="pwstrDeviceId"></param>
        public void OnDeviceAdded(string pwstrDeviceId)
        {
            // Do nothing
        }

        /// <summary>
        /// Handler executed when an audio playback device is removed
        /// </summary>
        /// <param name="deviceId"></param>
        public void OnDeviceRemoved(string deviceId)
        {
            // Do nothing
        }

        /// <summary>
        /// Handles audio playback device state changes
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="newState"></param>
        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            Console.WriteLine("OnDeviceStateChanged\n Device Id -->{0} : Device State {1}", deviceId, newState);
        }

        /// <summary>
        /// Handles audio playback device property value changes
        /// </summary>
        /// <param name="pwstrDeviceId"></param>
        /// <param name="key"></param>
        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            // Do nothing
        }
    }
}
