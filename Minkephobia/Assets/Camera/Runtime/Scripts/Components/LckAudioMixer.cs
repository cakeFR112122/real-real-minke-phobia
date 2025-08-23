using System.Collections.Generic;
using System.Linq;
using Liv.NGFX;
using Liv.Lck.Recorder;
using UnityEngine;
#if PLATFORM_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Liv.Lck
{
    internal class LckAudioMixer : ILckAudioMixer
    {
        private bool _isMicrophoneOpen = true;
        private bool _isGameAudioMute = false;

        private List<ILckAudioSource> _audioSources = new List<ILckAudioSource>();
        private int _microphoneAudioSourceIndex = -1;
        private ILckAudioSource _microphone;
        private ILckRecorder _recorder;
#if QCK_DUMP_AUDIO
        BinaryWriter debugGameFile;
        BinaryWriter debugMixFile;
        string basePath;
#endif

        public LckAudioMixer(ILckRecorder recorder)
        {
            _recorder = recorder;
            FindAudioSources();
        }

        public Handle<float[]> GetMixedAudioHandle(int frameSize)
        {
            return MixAudioArrays(frameSize);
        }
        
        public void EnableCapture()
        {
#if QCK_DUMP_AUDIO
            basePath = Application.platform == RuntimePlatform.Android ? Application.persistentDataPath : ".";
            debugGameFile = new(File.OpenWrite($"{basePath}/audio_game.raw"));
            debugMixFile = new(File.OpenWrite($"{basePath}/audio_mix.raw"));
#endif
            FindAudioSources();
            
            foreach (var audioCapture in _audioSources)
            {
                audioCapture.EnableCapture();
            }
        }

        public void DisableCapture()
        {
#if QCK_DUMP_AUDIO
            debugGameFile.Close();
            debugMixFile.Close();
#endif
            foreach (var audioCapture in _audioSources)
            {
                audioCapture.DisableCapture();
            }
        }

        private void FindAudioSources()
        {
            _audioSources.Clear();
            _microphoneAudioSourceIndex = -1;
            _microphone = null;
            
            var audioCaptures = LckMonoBehaviourMediator.FindObjectsOfComponentType<LckAudioCapture>();
            if (audioCaptures.Length > 0)
            {
                for (var index = 0; index < audioCaptures.Length; index++)
                {
                    var audioCapture = audioCaptures[index];
                    _audioSources.Add(audioCapture);
                    if (audioCapture is LckMicrophone)
                    {
                        _microphoneAudioSourceIndex = index;
                        if (CheckMicAudioPermissions())
                        {
                            _microphone = audioCapture;
                        }
                    }
                }
            }
            else
            {
                var listeners = LckMonoBehaviourMediator.FindObjectsOfComponentType<AudioListener>().Where(l => l.isActiveAndEnabled).ToArray();
                if (listeners.Length > 0)
                {
#if QCK_FMOD
                    _audioSources.Add(listeners[0].gameObject.AddComponent<AudioCaptureFMOD>());
#else
                    _audioSources.Add(listeners[0].gameObject.AddComponent<LckAudioCapture>());
#endif
                }
                else
                {
                    LckLog.LogError("LCK No AudioListeners found in the scene");
                    LckMonoBehaviourMediator.AddComponentToMediator<AudioListener>();
#if QCK_FMOD
                    _audioSources.Add(LckMonobehaviourMediator.AddComponentToMediator<AudioCaptureFMOD>());
#else
                    _audioSources.Add(LckMonoBehaviourMediator.AddComponentToMediator<LckAudioCapture>());
#endif
                }
            }

            if (_microphone == null)
            {
                if( CheckMicAudioPermissions() && Application.platform != RuntimePlatform.Android)
                {
                    _microphone = LckMonoBehaviourMediator.AddComponentToMediator<LckMicrophone>();
                    _audioSources.Add(_microphone);
                    _microphoneAudioSourceIndex = _audioSources.Count - 1;
                }
            }
            
            SetMicrophoneVolume();
        }

        private bool CheckMicAudioPermissions()
        {
#if PLATFORM_ANDROID && !UNITY_EDITOR
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            return true;
#endif
        }

        public LckResult SetMicrophoneOpen(bool isOpen)
        {
#if PLATFORM_ANDROID && !UNITY_EDITOR
            if (Application.platform == RuntimePlatform.Android)
            {
                if (!CheckMicAudioPermissions())
                {
                    return LckResult.NewError(LckError.MicrophonePermissionDenied,
                        "The app has not been granted microphone permissions.");
                }

                var setMicrophoneOpenResult = _recorder.SetMicrophoneOpen(isOpen);
                _isMicrophoneOpen = setMicrophoneOpenResult.Result;

                if (setMicrophoneOpenResult.Error != null)
                    return setMicrophoneOpenResult.Success
                        ? LckResult.NewSuccess()
                        : LckResult.NewError((LckError)setMicrophoneOpenResult.Error,
                            setMicrophoneOpenResult.Message ?? string.Empty);
            }
#endif
            _isMicrophoneOpen = isOpen;
            SetMicrophoneVolume();

            return LckResult.NewSuccess();
        }

        private void SetMicrophoneVolume()
        {
            if(_microphone != null)
                _microphone.VolumeUnitLevel = _isMicrophoneOpen ? 1 : 0;
        }

        public LckResult<bool> IsMicrophoneMute()
        {
            return LckResult<bool>.NewSuccess(!_isMicrophoneOpen);
        }

        public LckResult SetGameAudioMute(bool isMute)
        {
            _isGameAudioMute = isMute;
            SetGameAudioVolume();
            return LckResult.NewSuccess();
        }

        private void SetGameAudioVolume()
        {
            for (int i = 0; i < _audioSources.Count; i++)
            {
                if (i != _microphoneAudioSourceIndex)
                {
                    _audioSources[i].VolumeUnitLevel = _isGameAudioMute ? 0 : 1;
                }
            }
        }
        
        public LckResult<bool> IsGameAudioMute()
        {
            return LckResult<bool>.NewSuccess(_isGameAudioMute);
        }

        public float GetMicrophoneOutputLevel()
        {
            if (_microphone == null)
            {
                LckLog.LogError("LCK Microphone not found");
                return 0;
            }
            
            var audioLevel = _isMicrophoneOpen ? _microphone.GetCurrentAudioLevel() : 0;
            return audioLevel;
        }
        
        public float GetGameOutputLevel()
        {
            var gameAudioSourceIndex = -1;
            for (var i = 0; i < _audioSources.Count; i++)
            {
                if (i == _microphoneAudioSourceIndex) continue;
                
                gameAudioSourceIndex = i;
                break;
            }
            
            var audioLevel = _isGameAudioMute ? 0 : _audioSources[gameAudioSourceIndex].GetCurrentAudioLevel();
            return audioLevel;
        }

        private Handle<float[]> MixAudioArrays(int frameSize)
        {
            if (_audioSources == null || !_audioSources.Any() || _audioSources.Count == 0)
            {
                LckLog.LogError("LCK No audio arrays provided");
                return null;
            }
            
            int availableFrames = int.MaxValue;
            foreach (var source in _audioSources)
                availableFrames = Mathf.Min(availableFrames, source.GetAvailableAudioFrames(frameSize));
            
            // This is not the game audio, source at index 0 is not deterministic
            var nativeGameAudio = new Handle<float[]>(_audioSources[0].GetAudioData(frameSize, availableFrames));
            var game = nativeGameAudio.data();
#if QCK_DUMP_AUDIO
            foreach (float f in game)
                debugGameFile.Write(f);
#endif

            var audioArraysList = new List<float[]>();
            for (var index = 1; index < _audioSources.Count; index++)
            {
                var nativeTrackAudio = _audioSources[index].GetAudioData(frameSize, availableFrames);
                if (nativeTrackAudio.Length != game.Length)
                    LckLog.LogError($"LCK Mixer: game {game.Length} - track{index} {nativeTrackAudio.Length}");
                audioArraysList.Add(nativeTrackAudio);
            }
            
            // This is now useless since availableFrames already makes sure the audio have the same length
            // TODO: cleanup
            var smallestAudioDataLength = game.Length;
            foreach (var floats in audioArraysList) smallestAudioDataLength = Mathf.Min(floats.Length, smallestAudioDataLength);

            for (var i = 0; i < smallestAudioDataLength; i++)
            {
                foreach (var audioArray in audioArraysList)
                {
                    game[i] += audioArray[i];
                }
            }
            
#if QCK_DUMP_AUDIO
            foreach (float f in game)
                debugMixFile.Write(f);
#endif

            return nativeGameAudio;
        }
    }
}
