using System;
using UnityEngine;

namespace Liv.NativeAudioBridge
{
    public interface INativeAudioPlayer : IDisposable
    {
        void PreloadAudioClip(AudioClip audioClip);
        void PlayAudioClip(AudioClip audioClip);
        void StopAllAudio();
    }
}
