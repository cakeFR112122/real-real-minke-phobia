using Liv.NGFX;

namespace Liv.Lck
{
    internal interface ILckAudioMixer
    {
        Handle<float[]> GetMixedAudioHandle(int frameSize);
        void EnableCapture();
        void DisableCapture();
        LckResult SetMicrophoneOpen(bool isOpen);
        LckResult SetGameAudioMute(bool isMute);
        LckResult<bool> IsMicrophoneMute();
        LckResult<bool> IsGameAudioMute();
        float GetMicrophoneOutputLevel();
        float GetGameOutputLevel();
    }
}
