namespace Liv.Lck
{
    internal interface ILckAudioSource
    {
        float[] GetAudioData(int frameSize, int numerOfFrames);
        int GetAvailableAudioFrames(int frameSize);
        float GetCurrentAudioLevel();
        void EnableCapture();
        void DisableCapture();
        float VolumeUnitLevel{ get; set; }
    }
}
