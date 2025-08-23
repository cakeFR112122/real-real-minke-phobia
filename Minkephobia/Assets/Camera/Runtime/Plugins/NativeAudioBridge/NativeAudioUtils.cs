using UnityEngine;

namespace Liv.NativeAudioBridge
{
    public static class NativeAudioUtils
    {
        public static sbyte[] ConvertAudioClipToByteArray(AudioClip audioClip)
        {
            var samples = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(samples, 0);

            var bytes = new sbyte[samples.Length * sizeof(short)];
            var rescaleFactor = 32767;

            for (var i = 0; i < samples.Length; i++)
            {
                var value = (short)(samples[i] * rescaleFactor);
                bytes[i * 2] = (sbyte)(value & 0x00ff);
                bytes[i * 2 + 1] = (sbyte)((value & 0xff00) >> 8);
            }

            return bytes;
        }
    }
}
