using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A short phrase over the zone's music when the hero finds something: a secret room, an altar lit
    /// for the first time (GAME_FEEL_IDEAS item 16). The music steps back for it, so the find is an
    /// event and not one more sound in the room. It makes itself and listens to whatever world is
    /// loaded, so no scene has to carry it.
    /// </summary>
    public sealed class DiscoveryStingers : MonoBehaviour
    {
        /// <summary>A load marks rooms and altars all at once; none of that is a find.</summary>
        private const float QuietAfterLoad = 2f;
        private const float Gap = 3f;

        private WorldState _world;
        private float _quietUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var stingers = new GameObject("DiscoveryStingers").AddComponent<DiscoveryStingers>();
            DontDestroyOnLoad(stingers.gameObject);
        }

        private void Update()
        {
            var current = WorldState.Current;
            if (current == _world)
            {
                return;
            }

            Unsubscribe();
            _world = current;
            if (_world != null)
            {
                _world.Changed += OnChanged;
                _world.Reloaded += OnReloaded;
                _quietUntil = Time.unscaledTime + QuietAfterLoad;
            }
        }

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (_world != null)
            {
                _world.Changed -= OnChanged;
                _world.Reloaded -= OnReloaded;
            }
        }

        private void OnReloaded() => _quietUntil = Time.unscaledTime + QuietAfterLoad;

        private void OnChanged(WorldFlag flag, string id)
        {
            var sound = SoundFor(flag, id, _world != null ? _world.Graph : null);
            if (sound == null || Time.unscaledTime < _quietUntil)
            {
                return;
            }

            _quietUntil = Time.unscaledTime + Gap;
            AudioManager.Play(sound.Value, transform.position);
            AudioManager.Instance?.Duck(AudioBus.Music, 0.55f, 1.2f);
        }

        /// <summary>Which phrase a change in the world earns, if any.</summary>
        public static AudioId? SoundFor(WorldFlag flag, string id, WorldGraph graph)
        {
            switch (flag)
            {
                case WorldFlag.CheckpointActivated:
                    return AudioId.DiscoveryAltar;
                case WorldFlag.AreaDiscovered:
                    var area = graph != null ? graph.Area(id) : null;
                    return area != null && area.IsSecret ? AudioId.DiscoverySecret : (AudioId?)null;
                default:
                    return null;
            }
        }
    }
}
