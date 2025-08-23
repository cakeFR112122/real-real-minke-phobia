using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Liv.Lck.Recorder
{
    internal interface ILckRecorder : IDisposable
    {
        void Start(List<LckRecorder.TrackInfo> tracks, RenderTexture renderTexture, int firstVideoTrackIndex, Action<LckResult> onRecordingStartedCallback);
        void Stop(Action<LckResult> onRecordingStoppedCallback);
        void ReleaseNativeRenderBuffers();
        bool EncodeFrame(Stopwatch sw, bool[] readyTracks, LckRecorder.AudioTrack[] audioTracks);
        int GetAudioFrameSize();
        float GetNativeMicrophoneVolume();
        
#if PLATFORM_ANDROID
        LckResult<bool> SetMicrophoneOpen(bool open);
#endif
    }
}
