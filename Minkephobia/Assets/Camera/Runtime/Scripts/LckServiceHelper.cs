using Liv.Lck.Recorder;
using UnityEngine;

namespace Liv.Lck
{
    [DefaultExecutionOrder(-1000)]
    public class LckServiceHelper : MonoBehaviour
    {
        [SerializeField]
        private uint _framerate = 30;
        [SerializeField]
        private uint _bitrate = 5 << 20;
        [SerializeField]
        private uint _height = 1280;
        [SerializeField]
        private uint _width = 720;

        private void Awake()
        {
            var track = new CameraTrackDescriptor {
                CameraResolutionDescriptor = new CameraResolutionDescriptor(_width, _height),
                      Bitrate = _bitrate,
                      Framerate = _framerate,
            };

            var result = LckService.CreateService(new LckDescriptor {cameraTrackDescriptor = track});

            if (!result.Success)
            {
                Debug.LogError("LCK Could not create Service:" + result.Error + " " + result.Message);
                return;
            }
        }

        private void OnDestroy()
        {
            LckService.DestroyService();
        }
    }
}
