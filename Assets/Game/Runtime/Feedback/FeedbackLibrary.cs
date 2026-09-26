using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Maps each combat moment to its profile. One asset, so the whole feel of the game can be
    /// read in one place and a missing entry is obvious rather than silent.
    ///
    /// In its own file because Unity resolves a saved ScriptableObject's script by filename:
    /// sharing a file leaves the asset with no script at all and it then loads as null without a
    /// single error anywhere.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Feedback Library", fileName = "FeedbackLibrary")]
    public sealed class FeedbackLibrary : ScriptableObject
    {
        [Serializable]
        private sealed class Entry
        {
            public FeedbackKind kind;
            public FeedbackProfile profile;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
        [Tooltip("Shown when a moment has no profile of its own; keeps a gap visible, not silent.")]
        [SerializeField] private FeedbackProfile _fallback;
        private Dictionary<FeedbackKind, FeedbackProfile> _byKind;

        public FeedbackProfile Fallback => _fallback;

        public FeedbackProfile Profile(FeedbackKind kind)
        {
            EnsureIndex();
            return _byKind.TryGetValue(kind, out var profile) && profile != null
                ? profile
                : _fallback;
        }

        public bool Has(FeedbackKind kind)
        {
            EnsureIndex();
            return _byKind.TryGetValue(kind, out var profile) && profile != null;
        }

        private void EnsureIndex()
        {
            if (_byKind != null)
            {
                return;
            }

            _byKind = new Dictionary<FeedbackKind, FeedbackProfile>();
            foreach (var entry in _entries)
            {
                if (entry?.profile == null)
                {
                    continue;
                }

                if (!_byKind.TryAdd(entry.kind, entry.profile))
                {
                    Debug.LogError($"Feedback library '{name}' maps {entry.kind} twice.", this);
                }
            }
        }

        /// <summary>Reports gaps at authoring time rather than as silence during a fight.</summary>
        public bool Validate()
        {
            EnsureIndex();
            var isValid = _fallback != null;
            if (!isValid)
            {
                Debug.LogError($"Feedback library '{name}' has no fallback profile.", this);
            }

            foreach (FeedbackKind kind in Enum.GetValues(typeof(FeedbackKind)))
            {
                if (!Has(kind))
                {
                    Debug.LogError($"Feedback library '{name}' is missing {kind}.", this);
                    isValid = false;
                }
            }

            return isValid;
        }

        private void OnValidate() => _byKind = null;

        private void OnEnable() => _byKind = null;
    }
}
