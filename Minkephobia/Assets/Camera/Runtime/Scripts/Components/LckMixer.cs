using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Liv.Lck.Recorder;
using Liv.Lck.Settings;
using Liv.Lck.Telemetry;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Liv.Lck
{
    internal class LckMixer : ILckMixer
    {
        private const int _recordToggleCooldownMilliseconds = 250;
        private ILckCamera _activeCamera;
        private ILckRecorder _recorder;
        private ILckAudioMixer _audioMixer;
        private ILckStorageWatcher _lckStorageWatcher;


        private bool _isCapturing;

        private bool _isRecording;
        private bool _shouldStartRecording;
        private LckService.StopReason _stopReason;
        private bool _shouldStopRecording;

        private Coroutine _graphicsCoroutine;
        private RenderTexture _cameraTrackTexture;
        private CameraTrackDescriptor _cameraTrack;
        private LckRecorder.AudioTrack[] _audioTracks = new LckRecorder.AudioTrack[1];
        private bool _frameHasBeenRendered;

        private readonly Action<LckResult> _onRecordingStarted;
        private readonly Action<LckResult> _onRecordingStopped;
        private readonly Action<LckResult> _onLowStorageSpace;
        private readonly Action<LckResult, string, float> _onRecordingSavedCallback;

        private Stopwatch _recordingStopwatch;
        private uint _encodedFrames;

        public LckMixer(LckDescriptor qckDescriptor,
            Action<LckResult> onRecordingStarted,
            Action<LckResult> onRecordingStopped,
            Action<LckResult> onLowStorageSpace,
            Action<LckResult<RecordingData>> onRecordingSavedCallback)
        {
            _onRecordingStarted = onRecordingStarted;
            _onRecordingStopped = onRecordingStopped;
            _onLowStorageSpace = onLowStorageSpace;

            _recorder = new LckRecorder(onRecordingSavedCallback);
            _audioMixer = new LckAudioMixer(_recorder);

            _lckStorageWatcher = new LckStorageWatcher(OnLowStorageSpace);

            InitTrackTexture(qckDescriptor.cameraTrackDescriptor);

            LckMediator.CameraRegistered += OnCameraRegistered;
            LckMediator.CameraUnregistered += OnCameraUnregistered;
            LckMediator.MonitorRegistered += OnMonitorRegistered;
            LckMediator.MonitorUnregistered += OnMonitorUnregistered;

            LckMonoBehaviourMediator.OnApplicationLifecycleEvent += OnApplicationLifecycleEvent;
            LckMonoBehaviourMediator.StartCoroutine("LckMixer:Update", Update());
        }

        private void OnLowStorageSpace(LckResult result)
        {
            StopRecording(LckService.StopReason.LowStorageSpace);
            _onLowStorageSpace.Invoke(result);
        }

        private void InitTrackTexture(CameraTrackDescriptor cameraTrackDescriptor)
        {
            _cameraTrackTexture = InitTrack(cameraTrackDescriptor).Result;

            var cameras = LckMediator.GetCameras();
            if (!_cameraTrackTexture) return;

            if (_activeCamera == null)
            {
                foreach (var camera in cameras)
                {
                    ActivateCameraById(camera.CameraId);
                    break;
                }
            }
            else
            {
                ActivateCameraById(_activeCamera.CameraId);
            }

            SetMonitorTextureForAllMonitors();
        }

        public LckResult<RenderTexture> InitTrack(CameraTrackDescriptor cameraTrackDescriptor)
        {
            ReleaseCameraTrackTextures();

#if UNITY_2020
            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor((int)cameraTrackDescriptor.CameraResolutionDescriptor.Width, (int)cameraTrackDescriptor.CameraResolutionDescriptor.Height,
                RenderTextureFormat.ARGB32,  LckSettings.Instance.EnableStencilSupport ?  24 : 16)
#else
            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor((int)cameraTrackDescriptor.CameraResolutionDescriptor.Width, (int)cameraTrackDescriptor.CameraResolutionDescriptor.Height,
                GraphicsFormat.R8G8B8A8_UNorm, LckSettings.Instance.EnableStencilSupport ? GraphicsFormat.D24_UNorm_S8_UInt : GraphicsFormat.D16_UNorm)
#endif
            {
                memoryless = RenderTextureMemoryless.None,
                useMipMap = false,
                msaaSamples = 1,
                sRGB = true,
            };

            var renderTexture = new RenderTexture(renderTextureDescriptor);
            renderTexture.antiAliasing = 1;
            renderTexture.filterMode = FilterMode.Point;
            renderTexture.name = "LCK RenderTexture";
            renderTexture.Create();

            //NOTE: These need to be called twice to make sure the ptr is available
            renderTexture.GetNativeTexturePtr();
            renderTexture.GetNativeDepthBufferPtr();

            _cameraTrackTexture = renderTexture;

            _cameraTrack = cameraTrackDescriptor;
            return LckResult<RenderTexture>.NewSuccess(_cameraTrackTexture);
        }

        private void ReleaseCameraTrackTextures()
        {
            if (_isRecording)
            {
                LckLog.LogWarning("LCK Can't release render textures while recording.");
                return;
            }

            if (_cameraTrackTexture)
                _cameraTrackTexture.Release();
        }

        private IEnumerator Update()
        {
            var overflow = 0.0f;
            var renderStopwatch = new Stopwatch();
            renderStopwatch.Start();

            var recordToggleStopwatch = new Stopwatch();
            recordToggleStopwatch.Start();

            while (true)
            {
                if (_activeCamera != null)
                {
                    var frameTime = 1.0f / (double)_cameraTrack.Framerate;
                    if (renderStopwatch.Elapsed.TotalSeconds + overflow >= frameTime)
                    {
                        overflow = (float)(renderStopwatch.Elapsed.TotalSeconds + overflow - frameTime);
                        renderStopwatch.Restart();

                        _frameHasBeenRendered = true;
                        _activeCamera.ActivateCamera(_cameraTrackTexture);
                    }
                    else
                    {
                        _frameHasBeenRendered = false;
                        _activeCamera.DeactivateCamera();
                    }
                }

                switch (_isRecording)
                {
                    case false:
                        if (_shouldStartRecording && recordToggleStopwatch.ElapsedMilliseconds > _recordToggleCooldownMilliseconds)
                        {
                            _shouldStartRecording = false;
                            DoStartRecording();
                            recordToggleStopwatch.Restart();
                        }

                        if (_shouldStopRecording)
                        {
                            _shouldStopRecording = false;
                            _onRecordingStopped?.Invoke(LckResult.NewError(LckError.NotCurrentlyRecording, "There is no recording currently in progress to stop."));
                        }
                        break;
                    case true:
                        if (_shouldStopRecording && recordToggleStopwatch.ElapsedMilliseconds > _recordToggleCooldownMilliseconds)
                        {
                            _shouldStopRecording = false;
                            DoStopRecording();
                            recordToggleStopwatch.Restart();
                        }

                        if (_shouldStartRecording)
                        {
                            _shouldStartRecording = false;
                            _onRecordingStarted?.Invoke(LckResult.NewError(LckError.RecordingAlreadyStarted, "Recording already started."));
                        }

                        break;
                }

                yield return null;
            }
            // ReSharper disable once IteratorNeverReturns
        }

        public LckResult ActivateCameraById(string cameraId, string monitorId = null)
        {
            var cameraToActivate = LckMediator.GetCameraById(cameraId);
            if (cameraToActivate != null)
            {
                if (_activeCamera != null)
                {
                    _activeCamera.DeactivateCamera();
                }

                _activeCamera = cameraToActivate;
                _activeCamera.ActivateCamera(_cameraTrackTexture);

                if (!string.IsNullOrEmpty(monitorId))
                {
                    var monitor = LckMediator.GetMonitorById(monitorId);
                    if (monitor != null)
                    {
                        monitor.SetRenderTexture(_cameraTrackTexture);
                    }
                    else
                    {
                        return LckResult.NewError(LckError.MonitorIdNotFound, LckResultMessageBuilder.BuildMonitorIdNotFoundMessage(monitorId, LckMediator.GetMonitors().ToList()));
                    }
                }

                return LckResult.NewSuccess();
            }
            else
            {
                return LckResult.NewError(LckError.CameraIdNotFound, LckResultMessageBuilder.BuildCameraIdNotFoundMessage(cameraId, LckMediator.GetCameras().ToList()));
            }
        }

        public LckResult StopActiveCamera()
        {
            if (_activeCamera != null)
            {
                _activeCamera.DeactivateCamera();
                _activeCamera = null;
            }

            _isCapturing = false;
            return LckResult.NewSuccess();
        }

        public LckResult StartRecording()
        {
            if (_shouldStartRecording || _isRecording || _shouldStopRecording)
            {
                return LckResult.NewError(LckError.RecordingAlreadyStarted, "Recording already started.");
            }

            if (!_lckStorageWatcher.HasEnoughFreeStorage())
            {
                return LckResult.NewError(LckError.NotEnoughStorageSpace, "Not enough storage space.");
            }

            _shouldStartRecording = true;
            return LckResult.NewSuccess();
        }

        private void DoStartRecording()
        {
            _audioMixer.EnableCapture();
            List<LckRecorder.TrackInfo> tracks = new List<LckRecorder.TrackInfo>();
            if (_audioMixer != null)
            {
                // TODO: samplerate in FMOD
#if LCK_FMOD
                int sampleRate = AudioCaptureFMOD.samplerate == 0 ? 48000 : AudioCaptureFMOD.samplerate;
#else
                int sampleRate = AudioSettings.outputSampleRate == 0 ? 48000 : AudioSettings.outputSampleRate;
#endif
                _audioTracks = new[] { new LckRecorder.AudioTrack
                {
                    trackIndex = (uint)tracks.Count,
                    dataSize = 0,
                    timestampSamples = 0,
                    data = IntPtr.Zero
                }};
                tracks.Add(new LckRecorder.TrackInfo
                {
                    type = LckRecorder.TrackType.Audio,
                    bitrate = 1 << 20,
                    samplerate = (uint)sampleRate,
                    channels = 2
                });
            }

            int firstVideoTrackIndex = tracks.Count;

            tracks.Add(new LckRecorder.TrackInfo
            {
                type = LckRecorder.TrackType.Video,
                bitrate = _cameraTrack.Bitrate,
                width = _cameraTrack.CameraResolutionDescriptor.Width,
                height = _cameraTrack.CameraResolutionDescriptor.Height,
                framerate = _cameraTrack.Framerate,
            });


            _recorder.Start(tracks, _cameraTrackTexture, firstVideoTrackIndex, OnRecordingStartedCallback);
            _isRecording = true;
        }

        private void OnRecordingStartedCallback(LckResult result)
        {
            if (result.Success)
            {
                _graphicsCoroutine = LckMonoBehaviourMediator.StartCoroutine("EncodeFrame", EncodeFrame());
                LckTelemetry.SendTelemetry(new TelemetryEvent(TelemetryEventType.RecordingStarted));
            }

            _onRecordingStarted?.Invoke(result);
        }

        public IEnumerator EncodeFrame()
        {
            UInt64 audioTimestampSamples = 0;
            _recordingStopwatch = new Stopwatch();
            _recordingStopwatch.Start();
            _encodedFrames = 0;

            var readyTracks = new[] { false };

            while (_isRecording)
            {
                yield return null;

                try
                {
                    var nativeGameAudio = _audioMixer.GetMixedAudioHandle(_recorder.GetAudioFrameSize());

                    _audioTracks[0].data = nativeGameAudio.ptr();
                    _audioTracks[0].dataSize = (uint)nativeGameAudio.data().Length;
                    _audioTracks[0].timestampSamples = audioTimestampSamples;
                    _audioTracks[0].trackIndex = 0;

                    readyTracks[0] = _frameHasBeenRendered;

                    if (!_recorder.EncodeFrame(_recordingStopwatch, readyTracks, _audioTracks)) break;
                    _encodedFrames++;

                    audioTimestampSamples += (uint)nativeGameAudio.data().Length / 2;
                }
                catch (Exception e)
                {
                    LckLog.LogError("LCK EncodeFrame failed: " + e.Message);
                    LckTelemetry.SendTelemetry(new TelemetryEvent(TelemetryEventType.RecorderError, new Dictionary<string, object> { {"errorString", "EncodeFrameFailed"}, { "message", e.Message } }));
                    break;
                }
            }
        }

        public LckResult SetTrackResolution(CameraResolutionDescriptor cameraResolutionDescriptor)
        {
            if (_isRecording)
            {
                return LckResult.NewError(LckError.CantEditSettingsWhileRecording, "Can't change resolution while recording.");
            }

            _cameraTrack.CameraResolutionDescriptor = cameraResolutionDescriptor;
            try
            {
                InitTrackTexture(_cameraTrack);
            }
            catch (Exception e)
            {
                LckTelemetry.SendTelemetry(new TelemetryEvent(TelemetryEventType.RecorderError, new Dictionary<string, object> { { "errorString", "SetTrackResolutionFailed" }, { "message", e.Message } }));
                return LckResult.NewError(LckError.UnknownError, e.Message);
            }

            return LckResult.NewSuccess();
        }

        public LckResult SetTrackFramerate(uint framerate)
        {
            if (_isRecording)
            {
                return LckResult.NewError(LckError.CantEditSettingsWhileRecording, "Can't change framerate while recording.");
            }

            _cameraTrack.Framerate = framerate;
            return LckResult.NewSuccess();
        }

        public LckResult StopRecording(LckService.StopReason stopReason)
        {
            if (_shouldStopRecording || !_isRecording || _shouldStartRecording)
            {
                return LckResult.NewError(LckError.NotCurrentlyRecording, "No recording currently in progress to stop.");
            }

            _stopReason = stopReason;
            _shouldStopRecording = true;

            return LckResult.NewSuccess();
        }

        private void DoStopRecording()
        {
            LckLog.Log("LCK Stopping Recording");

            var recordingDuration = _recordingStopwatch.Elapsed.TotalSeconds;
            var context = new Dictionary<string, object> {
                { "recording.duration", recordingDuration },
                { "recording.encodedFrames", _encodedFrames },
                { "recording.stopReason", _stopReason.ToString() },
                { "recording.targetFramerate", _cameraTrack.Framerate },
                { "recording.targetBitrate", _cameraTrack.Bitrate },
                { "recording.targetResolution", _cameraTrack.CameraResolutionDescriptor.ToString() },
                { "recording.actualFramerate", (float)_encodedFrames / recordingDuration }
            };
            LckTelemetry.SendTelemetry(new TelemetryEvent(TelemetryEventType.RecordingStopped, context));

            _audioMixer.DisableCapture();
            _recordingStopwatch = new Stopwatch();
            _encodedFrames = 0;

            _recorder.Stop(OnRecordingStoppedCallback);

            _isRecording = false;
        }

        private void OnRecordingStoppedCallback(LckResult result)
        {
            _onRecordingStopped?.Invoke(result);
        }

        public bool IsRecording()
        {
            return _isRecording;
        }

        public bool IsCapturing()
        {
            return _isCapturing;
        }

        public LckResult SetMicrophoneCaptureActive(bool isActive)
        {
            return _audioMixer.SetMicrophoneOpen(isActive);
        }

        public LckResult SetGameAudioMute(bool isMute)
        {
            return _audioMixer.SetGameAudioMute(isMute);
        }

        public LckResult<bool> IsGameAudioMute()
        {
            return _audioMixer.IsGameAudioMute();
        }

        public LckResult<float> GetMicrophoneOutputLevel()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                return LckResult<float>.NewSuccess(_recorder.GetNativeMicrophoneVolume());
            }
            else
            {
                return LckResult<float>.NewSuccess(_audioMixer.GetMicrophoneOutputLevel());
            }
        }

        public LckResult<float> GetGameOutputLevel()
        {
            return LckResult<float>.NewSuccess(_audioMixer.GetGameOutputLevel());
        }

        private void SetMonitorTextureForAllMonitors()
        {
            foreach (var monitor in LckMediator.GetMonitors())
            {
                SetMonitorRenderTexture(monitor);
            }
        }

        private void SetMonitorRenderTexture(ILckMonitor monitor)
        {
            if (_cameraTrackTexture != null && monitor != null)
            {
                monitor.SetRenderTexture(_cameraTrackTexture);
                _isCapturing = true;
            }
            else
            {
                if (_cameraTrackTexture == null)
                {
                    LckLog.LogWarning($"LCK Camera track texture not found.");
                }
                if (monitor == null)
                {
                    LckLog.LogWarning($"LCK Monitor not found.");
                }
            }
        }

        private void OnCameraRegistered(ILckCamera camera)
        {

        }

        private void OnCameraUnregistered(ILckCamera camera)
        {
            if (_activeCamera == camera)
            {
                StopActiveCamera();
            }
        }

        private void OnMonitorRegistered(ILckMonitor monitor)
        {
            SetMonitorRenderTexture(monitor);
        }

        private static void OnMonitorUnregistered(ILckMonitor monitor)
        {
            monitor?.SetRenderTexture(null);
        }

        private void OnApplicationLifecycleEvent(LckMonoBehaviourMediator.ApplicationLifecycleEventType applicationLifecycleEventType)
        {
            if (_isRecording &&
                (applicationLifecycleEventType == LckMonoBehaviourMediator.ApplicationLifecycleEventType.Pause ||
                    applicationLifecycleEventType == LckMonoBehaviourMediator.ApplicationLifecycleEventType.Quit))
            {
                StopRecording(LckService.StopReason.ApplicationLifecycle);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_recorder != null)
                {
                    _recorder.Dispose();
                    _recorder = null;
                }

                if (_lckStorageWatcher != null)
                {
                    _lckStorageWatcher.Dispose();
                    _lckStorageWatcher = null;
                }

                LckMediator.CameraRegistered -= OnCameraRegistered;
                LckMediator.CameraUnregistered -= OnCameraUnregistered;
                LckMediator.MonitorRegistered -= OnMonitorRegistered;
                LckMediator.MonitorUnregistered -= OnMonitorUnregistered;

                LckMonoBehaviourMediator.OnApplicationLifecycleEvent -= OnApplicationLifecycleEvent;
            }
        }

        public LckResult<TimeSpan> GetRecordingDuration()
        {
            if (_recordingStopwatch == null || !_isRecording)
            {
                return LckResult<TimeSpan>.NewError(LckError.NotCurrentlyRecording, "Recording has not been started.");
            }

            return LckResult<TimeSpan>.NewSuccess(_recordingStopwatch.Elapsed);
        }

        ~LckMixer()
        {
            Dispose(false);
        }
    }
}
