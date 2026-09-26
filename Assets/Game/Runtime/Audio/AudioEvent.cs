using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// One sound the game can ask for, as data: which clips, how loud, how varied, how many at
    /// once, and how important it is when the mix runs out of room.
    ///
    /// Authored rather than coded because mixing is tuning, and tuning belongs somewhere it can be
    /// compared. Two sounds that should not resemble each other can be read side by side here
    /// instead of being hunted through call sites.
    ///
    /// It holds presentation only. Nothing here affects damage, timing or AI.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Audio Event", fileName = "audio_event")]
    public sealed class AudioEvent : ScriptableObject
    {
        [SerializeField] private AudioId _id = AudioId.None;
        [Tooltip("Variants. More than one stops a repeated action wearing a groove in the ear.")]
        [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();
        [SerializeField] private AudioBus _bus = AudioBus.Sfx;

        [Header("Level")]
        [SerializeField, Range(0f, 1f)] private float _volume = 0.8f;
        [Tooltip("Random level wobble; small, or it reads as a fault rather than as life.")]
        [SerializeField, Range(0f, 0.5f)] private float _volumeJitter = 0.06f;
        [Tooltip("Random pitch wobble. Past a few percent it stops being a game and becomes a toy.")]
        [SerializeField, Range(0f, 0.3f)] private float _pitchJitter = 0.04f;

        [Header("Space")]
        [Tooltip("0 is in your head, 1 is in the world. A side-view game wants a hint, not realism.")]
        [SerializeField, Range(0f, 1f)] private float _spatialBlend = 0.55f;
        [SerializeField, Min(1f)] private float _maxDistance = 26f;

        [Header("Discipline")]
        [Tooltip("How many of this sound may play at once. The main defence against a wall of noise.")]
        [SerializeField, Min(1)] private int _maxVoices = 4;
        [Tooltip("Shortest gap between two of these. Stops a held key becoming a machine gun.")]
        [SerializeField, Min(0f)] private float _minimumInterval;
        [Tooltip("Higher wins when voices run out. Telegraphs and parries sit at the top.")]
        [SerializeField, Min(0)] private int _priority = 50;
        [SerializeField] private bool _loop;

        public AudioId Id => _id;
        public AudioBus Bus => _bus;
        public float Volume => _volume;
        public float SpatialBlend => _spatialBlend;
        public float MaxDistance => _maxDistance;
        public int MaxVoices => _maxVoices;
        public float MinimumInterval => _minimumInterval;
        public int Priority => _priority;
        public bool Loop => _loop;
        public int ClipCount => _clips != null ? _clips.Length : 0;
        public bool HasClips => ClipCount > 0;

        /// <summary>
        /// Picks a variant, avoiding the one just played when there is a choice. Hearing the same
        /// file twice in a row is what makes a sound library sound like a sound library.
        /// </summary>
        public AudioClip PickClip(ref int lastIndex)
        {
            if (!HasClips)
            {
                return null;
            }

            if (ClipCount == 1)
            {
                lastIndex = 0;
                return _clips[0];
            }

            var index = UnityEngine.Random.Range(0, ClipCount);
            if (index == lastIndex)
            {
                index = (index + 1) % ClipCount;
            }

            lastIndex = index;
            return _clips[index];
        }

        public float PickVolume() =>
            Mathf.Clamp01(_volume * (1f + UnityEngine.Random.Range(-_volumeJitter, _volumeJitter)));

        public float PickPitch() =>
            1f + UnityEngine.Random.Range(-_pitchJitter, _pitchJitter);
    }
}
