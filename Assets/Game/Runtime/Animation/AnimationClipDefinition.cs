using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// One authored animation: its frames and how fast they play. Frame-based on purpose — the
    /// Visual Bible fixed 12 FPS for stances and 24 FPS for combat, and frames map directly onto
    /// the attack windows the game already has.
    ///
    /// Shared by the player, the ordinary enemies and the boss. They differ in which states exist,
    /// never in what a clip is, so there is one definition of a clip in the project.
    /// </summary>
    [Serializable]
    public sealed class AnimationClipDefinition
    {
        [SerializeField] private string _clipName = string.Empty;
        [SerializeField] private Sprite[] _frames = Array.Empty<Sprite>();
        [SerializeField, Min(1f)] private float _framesPerSecond = 12f;
        [SerializeField] private bool _isLooping = true;
        [Tooltip("Optional, one per frame: where loose cloth hangs from (x, y in units from the "
            + "pivot, drawn facing right) and the angle it hangs at (z, degrees from straight down, "
            + "positive towards the back).")]
        [SerializeField] private Vector3[] _anchors = Array.Empty<Vector3>();

        public AnimationClipDefinition() { }

        public AnimationClipDefinition(string clipName, Sprite[] frames, float fps, bool isLooping,
            Vector3[] anchors = null)
        {
            _clipName = clipName;
            _frames = frames;
            _framesPerSecond = fps;
            _isLooping = isLooping;
            _anchors = anchors ?? Array.Empty<Vector3>();
        }

        public string ClipName => _clipName;
        public IReadOnlyList<Sprite> Frames => _frames;
        public float FramesPerSecond => _framesPerSecond;
        public bool IsLooping => _isLooping;
        public int FrameCount => _frames != null ? _frames.Length : 0;
        public bool HasFrames => FrameCount > 0;

        /// <summary>How long one pass takes. A clip with no frames takes no time.</summary>
        public float Duration => HasFrames ? FrameCount / Mathf.Max(1f, _framesPerSecond) : 0f;

        /// <summary>
        /// The frame to show at a moment in time. A non-looping clip holds its last frame rather
        /// than vanishing, so an animation that outlives its state never leaves a blank sprite.
        /// </summary>
        public Sprite FrameAt(float elapsed)
        {
            if (!HasFrames)
            {
                return null;
            }

            var index = Mathf.FloorToInt(Mathf.Max(0f, elapsed) * _framesPerSecond);
            index = _isLooping ? index % FrameCount : Mathf.Min(index, FrameCount - 1);
            return _frames[index];
        }

        /// <summary>
        /// The frame's hanging point for secondary motion, when the clip has one for that frame. A
        /// clip without anchors — an enemy's, or one an artist has not annotated — says so rather
        /// than inventing a point.
        /// </summary>
        public bool TryGetAnchor(int frame, out Vector3 anchor)
        {
            if (_anchors != null && frame >= 0 && frame < _anchors.Length
                && _anchors.Length == FrameCount)
            {
                anchor = _anchors[frame];
                return true;
            }

            anchor = default;
            return false;
        }

        /// <summary>The frame index shown at that moment, for events that fire per frame.</summary>
        public int FrameIndexAt(float elapsed)
        {
            if (!HasFrames)
            {
                return -1;
            }

            var index = Mathf.FloorToInt(Mathf.Max(0f, elapsed) * _framesPerSecond);
            return _isLooping ? index % FrameCount : Mathf.Min(index, FrameCount - 1);
        }
    }
}
