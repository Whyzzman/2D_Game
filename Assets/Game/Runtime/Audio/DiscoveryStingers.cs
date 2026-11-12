using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A short phrase over the zone's music when the hero finds something: a secret room, an altar lit
    /// for the first time, the first look out from a height over far country (GAME_FEEL_IDEAS item 16). The music steps back for it, so the find is an
    /// event and not one more sound in the room. It makes itself and listens to whatever world is
    /// loaded, so no scene has to carry it.
    /// </summary>
    public sealed class DiscoveryStingers : MonoBehaviour
    {
        /// <summary>A load marks rooms and altars all at once; none of that is a find.</summary>
        private const float QuietAfterLoad = 2f;
        private const float Gap = 3f;

        /// <summary>
        /// Rooms with a view: high and open, another zone seen from them (decided by Claude under the standing
        /// consent, 2026-11-12). Not a secret room (that has its own phrase) and not an arena (the boss has music).
        /// </summary>
        public static readonly System.Collections.Generic.HashSet<string> Vistas =
            new System.Collections.Generic.HashSet<string>
            {
                "room_forest_watchtower",   // the ruined tower: the forest's crowns to the village
                "room_forest_far_gate",     // the forest's edge: the hill and the village above
                "room_village_feather",     // the cliff under the feather: the capital burning below
                "room_capital_bell_tower",  // the capital's belfry: the city to the palace
                "room_marsh_north_bridge"   // the north walkway: the marsh to its chapel
            };

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
                    if (area != null && area.IsSecret)
                    {
                        return AudioId.DiscoverySecret;
                    }

                    return Vistas.Contains(id) ? AudioId.DiscoveryVista : (AudioId?)null;
                default:
                    return null;
            }
        }
    }
}
