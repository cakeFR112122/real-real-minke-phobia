using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Liv.Lck
{
    internal class LckAudioCapture : MonoBehaviour, ILckAudioSource
    {
        [HideInInspector] 
        public bool _captureAudio;
        
        private int dspBufferSize = 0;
        private float[] _levelMonitorBuffer;
        private float _levelRunningSum;
        private int _levelMonitorBufferIndex;
        private int _levelSampleCount;
        
        protected List<float[]> _audioFrames = new List<float[]>();
        protected bool _isOutputMute = false;

#if QCK_DUMP_AUDIO
        static int debugID = 0;
        BinaryWriter debugDSPFile;
        BinaryWriter debugBufferFile;
        string basePath;
#endif
        private void Awake()
        {
            int buffersCount = 0;
            int bufferSize = 0;
            AudioSettings.GetDSPBufferSize(out bufferSize, out buffersCount);
            dspBufferSize = bufferSize * 2;
            _levelMonitorBuffer = new float[dspBufferSize];
            VolumeUnitLevel = 1f;
#if QCK_DUMP_AUDIO
            basePath = Application.platform == RuntimePlatform.Android ? Application.persistentDataPath : ".";
#endif
        }

        public float[] GetAudioData(int frameSize, int numerOfFrames)
        {
            float[] rawData;

            // TODO: shrink the locking scope
            lock (this)
            {
                int dspChunksPerFrame = frameSize / dspBufferSize;
                int wantedChunks = numerOfFrames * dspChunksPerFrame;
                int requiredChunks = Mathf.Min(wantedChunks, dspChunksPerFrame * (_audioFrames.Count / dspChunksPerFrame));
                rawData = new float[requiredChunks * dspBufferSize];
                for (var i = 0; i < requiredChunks; i++)
                {
                    _audioFrames[i].CopyTo(rawData, i * dspBufferSize);
                }
#if QCK_DUMP_AUDIO
                foreach (float f in rawData)
                    debugBufferFile.Write(f);
#endif
                // TODO: only this might need to be on lock
                _audioFrames.RemoveRange(0, requiredChunks);
            }
            return rawData;
        }
        
        public float GetCurrentAudioLevel()
        {
            return _levelSampleCount > 0 ? _levelRunningSum / _levelSampleCount : 0;
        }

        public virtual void EnableCapture()
        {
#if QCK_DUMP_AUDIO
            debugDSPFile = new(File.OpenWrite($"{basePath}/audio{debugID}_dsp.raw"));
            debugBufferFile = new(File.OpenWrite($"{basePath}/audio{debugID}_buf.raw"));
            debugID++;
#endif
            _captureAudio = true;
        }

        public virtual void DisableCapture()
        {
#if QCK_DUMP_AUDIO
            debugDSPFile.Close();
            debugBufferFile.Close();
#endif
            _captureAudio = false;
        }

        public float VolumeUnitLevel { get; set; }

        void OnAudioFilterRead(float[] data, int channels)
        {
            CaptureAudioLevel(data, channels);
            if (_captureAudio)
            {
#if QCK_DUMP_AUDIO
                foreach (float f in data)
                    debugDSPFile.Write(f);
#endif
                CaptureAudio(data);
            }

            if (_isOutputMute)
            {
                ArrayFill(data, 0.0f);
            }
        }

        private static void ArrayFill<T>(T[] array, T value)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }
        
        private void CaptureAudio(float[] data)
        {
            var newData = new float[data.Length];
            for (var index = 0; index < data.Length; index++)
            {
                var floatData = data[index] * VolumeUnitLevel;
                newData[index] = floatData;
            }

            lock (this)
            {
                _audioFrames.Add(newData);
            }
        }

        void CaptureAudioLevel(float[] data, int channels)
        {
            for (var i = 0; i < data.Length; i += channels)
            {
                _levelRunningSum -= Mathf.Abs(_levelMonitorBuffer[_levelMonitorBufferIndex]);
                _levelRunningSum = Mathf.Max(0.01f, _levelRunningSum);
                
                var newValue = 0f;
                for (int channel = 0; channel < channels; channel++)
                {
                    newValue += Mathf.Abs(data[i + channel]);
                }

                newValue /= channels;

                _levelRunningSum += newValue * VolumeUnitLevel;
                _levelMonitorBuffer[_levelMonitorBufferIndex] = newValue;
                _levelMonitorBufferIndex = (_levelMonitorBufferIndex + 1) % dspBufferSize;
            }

            _levelSampleCount = Mathf.Min(_levelSampleCount + (data.Length / channels), dspBufferSize);
        }

        public int GetAvailableAudioFrames(int frameSize)
        {
            int dspChunksPerFrame = frameSize / dspBufferSize;
            return _audioFrames.Count / dspChunksPerFrame;
        }
    }
}
