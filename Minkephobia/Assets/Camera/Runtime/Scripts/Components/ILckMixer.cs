using System;
using Liv.Lck.Recorder;

namespace Liv.Lck
{
    internal interface ILckMixer : IDisposable
    {
        LckResult ActivateCameraById(string cameraId, string monitorId = null);
        public LckResult StopActiveCamera();
        LckResult StartRecording();
        LckResult StopRecording(LckService.StopReason stopReason);
        bool IsRecording();
        bool IsCapturing();
        LckResult SetMicrophoneCaptureActive(bool isActive);
        LckResult SetGameAudioMute(bool isMute);
        LckResult<bool> IsGameAudioMute();
        LckResult<float> GetMicrophoneOutputLevel();
        LckResult<float> GetGameOutputLevel();
        LckResult SetTrackResolution(CameraResolutionDescriptor cameraResolutionDescriptor);
        LckResult SetTrackFramerate(uint framerate);
        LckResult<TimeSpan> GetRecordingDuration();
    }
}
