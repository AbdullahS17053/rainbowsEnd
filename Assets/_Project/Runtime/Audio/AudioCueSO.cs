using System;
using UnityEngine;

namespace RainbowsEnd.Audio
{
    public enum AudioLayer
    {
        Ambient = 1,
        PlayerAction = 2,
        EarshotGated = 3,
        GlobalPriority = 4
    }

    [CreateAssetMenu(fileName = "SO_AudioCue", menuName = "Rainbows End/Audio/Audio Cue")]
    public sealed class AudioCueSO : ScriptableObject
    {
        [SerializeField] private AudioLayer layer = AudioLayer.PlayerAction;
        [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
        [SerializeField] private bool bypassEarshot;

        public AudioLayer Layer => layer;
        public AudioClip[] Clips => clips;
        public float Volume => volume;
        public Vector2 PitchRange => pitchRange;
        public bool BypassEarshot => bypassEarshot;

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        public float GetRandomPitch()
        {
            return UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        }
    }
}
