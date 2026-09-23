using System.Collections.Generic;
using UnityEngine;

namespace RainbowsEnd.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        [Header("Layer 1: Ambient")]
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioClip ambientLoopClip;
        [SerializeField] [Range(0f, 1f)] private float ambientVolume = 0.4f;

        [Header("Layer 2: Player Actions")]
        [SerializeField] private int playerActionPoolSize = 8;
        [SerializeField] [Range(0f, 1f)] private float playerActionVolume = 1.0f;

        [Header("Layer 3: Earshot Gated Sounds")]
        [SerializeField] private int spatialPoolSize = 12;
        [SerializeField] [Range(0f, 1f)] private float spatialVolume = 0.85f;

        [Header("Layer 4: Global Priority")]
        [SerializeField] private int globalPriorityPoolSize = 4;
        [SerializeField] [Range(0f, 1f)] private float globalPriorityVolume = 1.0f;

        private readonly List<AudioSource> _playerActionSources = new List<AudioSource>();
        private readonly List<AudioSource> _spatialSources = new List<AudioSource>();
        private readonly List<AudioSource> _globalSources = new List<AudioSource>();
        private AudioHearingRadiusService _hearingService;

        private int _playerSourceIndex;
        private int _spatialSourceIndex;
        private int _globalSourceIndex;

        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        private AudioListener _audioListener;
        private RainbowsEnd.Gameplay.Player.PlayerRoot _playerRoot;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            EnsureAudioListener();
            InitializeSources();
            StartAmbientLoop();
        }

        private void LateUpdate()
        {
            UpdateListenerPosition();
        }

        public void EnsureAudioListener()
        {
            if (_audioListener == null)
            {
                _audioListener = FindFirstObjectByType<AudioListener>();
                if (_audioListener == null)
                {
                    GameObject listenerObj = new GameObject("GroundAudioListener");
                    listenerObj.transform.SetParent(transform, false);
                    _audioListener = listenerObj.AddComponent<AudioListener>();
                }
            }
        }

        private void UpdateListenerPosition()
        {
            EnsureAudioListener();
            if (_audioListener == null) return;

            if (_playerRoot == null)
            {
                _playerRoot = FindFirstObjectByType<RainbowsEnd.Gameplay.Player.PlayerRoot>();
            }

            Vector3 groundPos;
            if (_playerRoot != null)
            {
                groundPos = _playerRoot.transform.position;
            }
            else if (Camera.main != null)
            {
                groundPos = Camera.main.transform.position;
                groundPos.z = 0f;
            }
            else
            {
                groundPos = Vector3.zero;
            }

            // Only reposition our dedicated GroundAudioListener
            if (_audioListener.transform.parent == transform || _audioListener.gameObject.name == "GroundAudioListener")
            {
                _audioListener.transform.position = groundPos;
            }
        }

        private void Start()
        {
            _hearingService = AudioHearingRadiusService.Instance;
            if (_hearingService == null)
            {
                _hearingService = gameObject.AddComponent<AudioHearingRadiusService>();
            }
        }

        private void InitializeSources()
        {
            if (_playerActionSources.Count > 0)
            {
                return;
            }

            // Ambient source
            if (ambientSource == null)
            {
                GameObject ambObj = new GameObject("AmbientSource");
                ambObj.transform.SetParent(transform, false);
                ambientSource = ambObj.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
                ambientSource.volume = ambientVolume;
            }

            // Player action 2D pool
            for (int i = 0; i < playerActionPoolSize; i++)
            {
                GameObject obj = new GameObject($"PlayerActionSource_{i}");
                obj.transform.SetParent(transform, false);
                AudioSource src = obj.AddComponent<AudioSource>();
                src.spatialBlend = 0f; // Pure 2D
                src.playOnAwake = false;
                _playerActionSources.Add(src);
            }

            // Spatial pool (tuned for top-down perspective)
            for (int i = 0; i < spatialPoolSize; i++)
            {
                GameObject obj = new GameObject($"SpatialSource_{i}");
                obj.transform.SetParent(transform, false);
                AudioSource src = obj.AddComponent<AudioSource>();
                src.spatialBlend = 0.35f; // Hybrid 2D/3D: gives stereo panning without getting muted by top-down camera height
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 8f; // Full volume across immediate combat zone
                src.maxDistance = 50f; // Audible across entire top-down arena
                src.dopplerLevel = 0f;
                src.playOnAwake = false;
                _spatialSources.Add(src);
            }

            // Global priority pool
            for (int i = 0; i < globalPriorityPoolSize; i++)
            {
                GameObject obj = new GameObject($"GlobalPrioritySource_{i}");
                obj.transform.SetParent(transform, false);
                AudioSource src = obj.AddComponent<AudioSource>();
                src.spatialBlend = 0f; // Pure 2D
                src.playOnAwake = false;
                _globalSources.Add(src);
            }
        }

        public void StartAmbientLoop(AudioClip customClip = null)
        {
            if (ambientSource == null) return;
            AudioClip clip = customClip != null ? customClip : ambientLoopClip;
            if (clip != null)
            {
                ambientSource.clip = clip;
                ambientSource.volume = ambientVolume;
                ambientSource.Play();
            }
        }

        public void PlayPlayerAction(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            EnsureAudioListener();
            if (_playerActionSources.Count == 0) return;

            AudioSource src = _playerActionSources[_playerSourceIndex % _playerActionSources.Count];
            _playerSourceIndex++;

            src.pitch = pitch;
            src.volume = volume * playerActionVolume;
            src.PlayOneShot(clip);
        }

        public void PlaySpatialGated(AudioClip clip, Vector2 worldPosition, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            EnsureAudioListener();
            if (_spatialSources.Count == 0) return;

            _hearingService ??= AudioHearingRadiusService.Instance;
            if (_hearingService != null)
            {
                if (!_hearingService.IsWithinEarshot(worldPosition, out float earshotScale))
                {
                    return; // Gated out of listening radius!
                }
                volume *= earshotScale;
            }

            AudioSource src = _spatialSources[_spatialSourceIndex % _spatialSources.Count];
            _spatialSourceIndex++;

            src.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
            src.pitch = pitch;
            src.volume = volume * spatialVolume;
            src.PlayOneShot(clip);
        }

        public void PlayGlobalPriority(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            EnsureAudioListener();
            if (_globalSources.Count == 0) return;

            AudioSource src = _globalSources[_globalSourceIndex % _globalSources.Count];
            _globalSourceIndex++;

            src.pitch = pitch;
            src.volume = volume * globalPriorityVolume;
            src.PlayOneShot(clip);
        }

        public void PlayCue(AudioCueSO cue, Vector2 worldPosition)
        {
            if (cue == null) return;

            AudioClip clip = cue.GetRandomClip();
            if (clip == null) return;

            float pitch = cue.GetRandomPitch();
            float vol = cue.Volume;

            if (cue.BypassEarshot || cue.Layer == AudioLayer.GlobalPriority)
            {
                PlayGlobalPriority(clip, vol, pitch);
            }
            else if (cue.Layer == AudioLayer.PlayerAction)
            {
                PlayPlayerAction(clip, vol, pitch);
            }
            else if (cue.Layer == AudioLayer.EarshotGated)
            {
                PlaySpatialGated(clip, worldPosition, vol, pitch);
            }
        }
    }
}
