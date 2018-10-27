using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NAudio.CoreAudioApi;

namespace WinMediaPie
{
    public class VolumeChangeEventArgs : EventArgs {
        public float volumePercent;
        public bool isMuted;
    }

    class NotificationClientImplementation : NAudio.CoreAudioApi.Interfaces.IMMNotificationClient
    {
        public event EventHandler<VolumeChangeEventArgs> VolumeChange;
        private MMDevice currentDefaultDevice = null;
        private MMDeviceEnumerator mMDeviceEnumerator;

        public NotificationClientImplementation(MMDeviceEnumerator mMDeviceEnumerator)
        {
            this.mMDeviceEnumerator = mMDeviceEnumerator;

            currentDefaultDevice = mMDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            MaybeSetupVolumeNotificationListener();
        }

        public void Initialize()
        {
            VolumeChange(this, new VolumeChangeEventArgs
            {
                volumePercent = currentDefaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100,
                isMuted = currentDefaultDevice.AudioEndpointVolume.Mute
            });
        }

        public void OnDefaultDeviceChanged(DataFlow dataFlow, Role role, string defaultDeviceId)
        {
            Console.WriteLine("OnDefaultDeviceChanged --> {0}", dataFlow.ToString());
            MaybeClearVolumeNotificationListener();
            currentDefaultDevice = mMDeviceEnumerator.GetDevice(defaultDeviceId);
            MaybeSetupVolumeNotificationListener();
        }

        private void MaybeSetupVolumeNotificationListener()
        {
            if (currentDefaultDevice != null)
            {
                currentDefaultDevice.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
            }
        }

        private void MaybeClearVolumeNotificationListener()
        {
            if (currentDefaultDevice != null)
            {
                currentDefaultDevice.AudioEndpointVolume.OnVolumeNotification -= AudioEndpointVolume_OnVolumeNotification;
            }
        }

        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            Console.WriteLine("New Volume {0}", data.MasterVolume);
            Console.WriteLine("Muted      {0}", data.Muted);

            VolumeChange(this, new VolumeChangeEventArgs {
                volumePercent = data.MasterVolume * 100,
                isMuted = data.Muted
            });
        }

        internal void SetVolume(float newValue)
        {
            currentDefaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = newValue / 100;
        }

        internal void Unmute()
        {
            currentDefaultDevice.AudioEndpointVolume.Mute = false;
        }

        internal void Mute()
        {
            currentDefaultDevice.AudioEndpointVolume.Mute = true;
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
            Console.WriteLine("OnDeviceStateChanged\n Device Id -->{0} : Device State {1}", deviceId, newState);
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            // Do nothing
        }
    }
}
