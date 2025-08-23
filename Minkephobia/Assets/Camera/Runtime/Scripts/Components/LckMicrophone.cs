using UnityEngine;

namespace Liv.Lck
{
    [RequireComponent(typeof(AudioSource))]
    internal class LckMicrophone : LckAudioCapture
    {
        private AudioSource micOut;

        private void Start()
        {
            SetupMicrophone();
        }

        private void SetupMicrophone()
        {
            _isOutputMute = true;
            micOut = GetComponent<AudioSource>();
            if(Microphone.devices.Length == 0)
            {
                LckLog.LogError("LCK - no microphone found");
                return;
            }

            Microphone.GetDeviceCaps(null, out int minFreq, out int maxFreq);

            if( maxFreq < AudioSettings.outputSampleRate)
            {
                LckLog.LogError("LCK - microphone does not support output sample rate currently set to " + AudioSettings.outputSampleRate);
                return;
            }

            micOut.clip = Microphone.Start(null, true, 10, AudioSettings.outputSampleRate);
            micOut.loop = true;
            while (!(Microphone.GetPosition(null) > 0)) { }
            micOut.Play();
        }

        public override void EnableCapture()
        {
            _isOutputMute = true;
            base.EnableCapture();
        }
    }
}
