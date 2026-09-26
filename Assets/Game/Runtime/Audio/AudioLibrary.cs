using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Every sound in the game, in one asset. A missing entry is then a thing you can see rather
    /// than a silence you have to notice.
    ///
    /// In its own file because Unity resolves a saved ScriptableObject's script by filename;
    /// sharing one leaves the asset with no script and it loads as null without an error.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [SerializeField] private AudioEvent[] _events = Array.Empty<AudioEvent>();
        private Dictionary<AudioId, AudioEvent> _byId;

        public int Count => _events != null ? _events.Length : 0;

        public AudioEvent Event(AudioId id)
        {
            if (id == AudioId.None)
            {
                return null;
            }

            EnsureIndex();
            return _byId.TryGetValue(id, out var found) ? found : null;
        }

        public bool Has(AudioId id) => Event(id) != null;

        private void EnsureIndex()
        {
            if (_byId != null)
            {
                return;
            }

            _byId = new Dictionary<AudioId, AudioEvent>();
            foreach (var entry in _events)
            {
                if (entry == null || entry.Id == AudioId.None)
                {
                    continue;
                }

                if (!_byId.TryAdd(entry.Id, entry))
                {
                    Debug.LogError($"Audio library '{name}' maps {entry.Id} twice.", this);
                }
            }
        }

        /// <summary>
        /// Reports gaps at authoring time. A sound that is merely missing is reported as a
        /// warning; a sound with an entry but no clip is an error, because that one looks wired.
        /// </summary>
        public bool Validate()
        {
            EnsureIndex();
            var isValid = true;
            foreach (AudioId id in Enum.GetValues(typeof(AudioId)))
            {
                if (id == AudioId.None)
                {
                    continue;
                }

                var entry = Event(id);
                if (entry == null)
                {
                    Debug.LogWarning($"Audio library '{name}' has no event for {id}.", this);
                    continue;
                }

                if (!entry.HasClips)
                {
                    Debug.LogError($"Audio event for {id} has no clips.", entry);
                    isValid = false;
                }
            }

            return isValid;
        }

        private void OnValidate() => _byId = null;

        private void OnEnable() => _byId = null;
    }
}
