using UnityEngine;

namespace Liv.Lck.Settings
{
    public class LckSettings : ScriptableObject
    {
        public const string SettingsPath = "Assets/Resources/LckSettings.asset";

        [SerializeField]
        public string TrackingId = "";

        [Space(10)]
        [SerializeField]
        public string RecordingFilenamePrefix = "MyGame";

        [SerializeField]
        public string RecordingAlbumName = "MyGameAlbum";

        [SerializeField]
        public string RecordingDateSuffixFormat = "yyyy-MM-dd_HH-mm-ss";

        [Space(10)]
        [Header("Advanced")]
        [SerializeField]
        public LogLevel BaseLogLevel = LogLevel.Error;

        [SerializeField]
        public Liv.NGFX.LogLevel NativeLogLevel = Liv.NGFX.LogLevel.Error;


        [SerializeField]
        [Tooltip(
            "Enabling stencil buffer support allows for advanced rendering effects, such as masking and outlining, to be recorded in the recording. "
                + "UI elements may often utilise the stencil buffer and may otherwise appear incorrect in the recordings. "
                + "Disable to optimise performance if stencil effects are not needed."
        )]
        public bool EnableStencilSupport = true;

        [Header("Telemetry")]
        [SerializeField]
        public bool AllowLocationTelemetry = true;
        [SerializeField]
        public bool AllowDeviceTelemetry = true;

        [Space(10)]
        [Header("Tablet Using Collider Settings")]
        [Tooltip(
            "When using the 'LCK Tablet Using Collider' prefab. Trigger events will check this tag. "
                + "Make sure to add this tag on your XR Rig Direct Interactors for both controllers"
        )]
        [SerializeField]
        public string TriggerEnterTag = "Hand";

        [HideInInspector]
    public const string Version = "1.1.4";

        public static LckSettings _instance;


        public static LckSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<LckSettings>("LckSettings");
                    if (_instance != null)
                    {
#if !UNITY_EDITOR
                        LckLog.Log($"LCK Settings loaded from Resources");
#endif
                        var idIsValid = System.Guid.TryParse(
                            _instance.TrackingId,
                            out System.Guid id
                        );
                        if (!idIsValid)
                        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                            LckLog.LogWarning(
                                "LCK TrackingId has not been set. This is only valid in development builds. Please set it in the LCK settings"
                            );
#else
                            LckLog.LogError(
                                "LCK TrackingId has not been set. This is only valid in development builds. Please set it in the LCK settings"
                            );
#endif
                        }
                    }
                }

#if UNITY_EDITOR
                if (_instance == null)
                {
                    LckSettings scriptableObject = ScriptableObject.CreateInstance<LckSettings>();

                    var parentFolder = System.IO.Path.GetDirectoryName(SettingsPath);
                    if (!System.IO.Directory.Exists(parentFolder))
                    {
                        System.IO.Directory.CreateDirectory(parentFolder);
                    }

                    UnityEditor.AssetDatabase.CreateAsset(scriptableObject, SettingsPath);
                    UnityEditor.AssetDatabase.SaveAssets();
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<LckSettings>(
                        SettingsPath
                    );
                    if (_instance != null)
                    {
                        LckLog.LogWarning("LCK settings asset created at " + SettingsPath);
                    }
                }
#endif

                if (_instance == null)
                {
                    _instance = CreateInstance<LckSettings>();

                    if (_instance == null)
                    {
                        LckLog.LogWarning("LCK No settings found, using default settings");
                    }
                }

                return _instance;
            }
        }
    }
}
