using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>Where the zones of a hazard attack go, and in what order.</summary>
    public enum HazardPattern
    {
        /// <summary>One zone, as a shaman's pool or the King's ember scar.</summary>
        Single,
        /// <summary>One zone after another, each where the target stands at that moment.</summary>
        Rain,
        /// <summary>A row of zones running out from the attacker, one step at a time.</summary>
        March,
        /// <summary>
        /// The same row, dropped the other way round: from where it ends (a wall, the edge of the floor)
        /// in toward the attacker, on both sides at once — the King's «Моє слово — стіна».
        /// </summary>
        Converge
    }

    /// <summary>The geometry of a hazard pattern, kept apart from the scene so it can be checked alone.</summary>
    public static class HazardPatterns
    {
        /// <summary>One zone of a march: how far along the attacker's facing, and on which step it drops.</summary>
        public readonly struct Step
        {
            public readonly float Offset;
            public readonly int Index;

            public Step(float offset, int index)
            {
                Offset = offset;
                Index = index;
            }
        }

        /// <summary>
        /// A row of zones <paramref name="spacing"/> apart from <paramref name="start"/> outward, mirrored
        /// behind the attacker when <paramref name="bothSides"/>. A <paramref name="shifted"/> row sits half
        /// a step nearer, so the gaps of the row before it are exactly where this one lands.
        /// </summary>
        public static List<Step> March(int count, float start, float spacing, bool bothSides, bool shifted)
        {
            var steps = new List<Step>();
            var first = Mathf.Max(0f, shifted ? start - spacing * 0.5f : start);
            for (var i = 0; i < Mathf.Max(1, count); i++)
            {
                var offset = first + i * spacing;
                steps.Add(new Step(offset, i));
                // a zone under the attacker itself is one zone, not one for each side
                if (bothSides && offset > 0.01f)
                {
                    steps.Add(new Step(-offset, i));
                }
            }

            return steps;
        }

        /// <summary>
        /// When each zone of a converging row drops: a side's farthest zone first, then one step nearer
        /// every <paramref name="interval"/>, so both fronts start at once wherever their sides end.
        /// </summary>
        public static float[] ConvergeTimes(IReadOnlyList<Step> kept, float interval)
        {
            var farAhead = -1;
            var farBehind = -1;
            foreach (var step in kept)
            {
                if (step.Offset >= 0f) { farAhead = Mathf.Max(farAhead, step.Index); }
                else { farBehind = Mathf.Max(farBehind, step.Index); }
            }

            var times = new float[kept.Count];
            for (var i = 0; i < kept.Count; i++)
            {
                var far = kept[i].Offset >= 0f ? farAhead : farBehind;
                times[i] = (far - kept[i].Index) * interval;
            }

            return times;
        }

        /// <summary>The floor under a point: the centre a zone of that height takes when it rests on it.</summary>
        public static bool TryGround(Vector2 from, float zoneHeight, int groundMask, out Vector2 centre)
        {
            var hit = Physics2D.Raycast(from + Vector2.up * 1.5f, Vector2.down, 8f, groundMask);
            if (hit.collider == null)
            {
                centre = from;
                return false;
            }

            centre = new Vector2(from.x, hit.point.y + zoneHeight * 0.5f - 0.1f);
            return true;
        }
    }

    /// <summary>
    /// A hazard attack spread over time: the skulls that fall one after another on the Plague Stag's
    /// bells, the stone that runs out from the Ash Commander's halberd, the Bellringer's toll. Each zone
    /// it drops is an ordinary <see cref="HazardZone"/>, so each still shows itself before it bites.
    /// </summary>
    public sealed class HazardVolley : MonoBehaviour
    {
        private struct Drop
        {
            public float At;
            public Vector2 Point;
            public bool AtTarget;
        }

        private static readonly List<HazardVolley> Running = new List<HazardVolley>();

        private readonly List<Drop> _drops = new List<Drop>();
        private GameObject _prefab;
        private Faction _faction;
        private GameObject _owner;
        private Transform _target;
        private float _zoneHeight;
        private int _groundMask;
        private float _elapsed;
        private int _next;

        /// <summary>How many volleys are still dropping; the leak check reads it.</summary>
        public static int RunningCount => Running.Count;

        /// <summary>Zones dropped where the target stands, <paramref name="interval"/> apart.</summary>
        public static HazardVolley Rain(GameObject prefab, Faction faction, GameObject owner, Transform target,
            int count, float interval)
        {
            if (prefab == null || target == null)
            {
                return null;
            }

            var volley = Create(prefab, faction, owner, target);
            for (var i = 0; i < Mathf.Max(1, count); i++)
            {
                volley._drops.Add(new Drop { At = i * interval, AtTarget = true });
            }

            return volley;
        }

        /// <summary>
        /// A row of zones along the floor from <paramref name="origin"/>. A side of the row ends where the
        /// floor does or a wall stands, so nothing erupts outside the arena.
        /// </summary>
        public static HazardVolley March(GameObject prefab, Faction faction, GameObject owner, Vector2 origin,
            int facing, List<HazardPatterns.Step> steps, float interval, bool converge = false)
        {
            if (prefab == null || steps == null)
            {
                return null;
            }

            var volley = Create(prefab, faction, owner, null);
            var direction = facing >= 0 ? 1f : -1f;
            var blockedAhead = float.MaxValue;
            var blockedBehind = float.MaxValue;
            steps.Sort((a, b) => Mathf.Abs(a.Offset).CompareTo(Mathf.Abs(b.Offset)));
            var kept = new List<HazardPatterns.Step>();
            foreach (var step in steps)
            {
                var ahead = step.Offset >= 0f;
                var reach = Mathf.Abs(step.Offset);
                if (reach >= (ahead ? blockedAhead : blockedBehind))
                {
                    continue;
                }

                var from = origin + new Vector2(step.Offset * direction, 0f);
                var walled = Physics2D.Linecast(origin + Vector2.up * 0.4f, from + Vector2.up * 0.4f,
                    volley._groundMask).collider != null;
                if (walled || !HazardPatterns.TryGround(from, volley._zoneHeight, volley._groundMask, out var point))
                {
                    if (ahead) { blockedAhead = reach; } else { blockedBehind = reach; }
                    continue;
                }

                volley._drops.Add(new Drop { At = step.Index * interval, Point = point });
                kept.Add(step);
            }

            if (converge)
            {
                // timed from where each side really ends, so a wall near the attacker starts its front at once
                var times = HazardPatterns.ConvergeTimes(kept, interval);
                for (var i = 0; i < times.Length; i++)
                {
                    var drop = volley._drops[i];
                    drop.At = times[i];
                    volley._drops[i] = drop;
                }
            }

            volley._drops.Sort((a, b) => a.At.CompareTo(b.At));
            return volley;
        }

        private static HazardVolley Create(GameObject prefab, Faction faction, GameObject owner, Transform target)
        {
            var volley = new GameObject("HazardVolley").AddComponent<HazardVolley>();
            var zone = prefab.GetComponent<HazardZone>();
            volley._prefab = prefab;
            volley._faction = faction;
            volley._owner = owner;
            volley._target = target;
            volley._zoneHeight = zone != null ? zone.Size.y : 1f;
            volley._groundMask = 1 << 9; // Ground (layers have no names)
            return volley;
        }

        private void OnEnable() => Running.Add(this);

        private void OnDisable() => Running.Remove(this);

        private void Update()
        {
            _elapsed += Time.deltaTime;
            while (_next < _drops.Count && _drops[_next].At <= _elapsed)
            {
                Place(_drops[_next]);
                _next++;
            }

            if (_next >= _drops.Count)
            {
                Destroy(gameObject);
            }
        }

        private void Place(Drop drop)
        {
            var point = drop.Point;
            if (drop.AtTarget)
            {
                if (_target == null)
                {
                    return;
                }

                // In the air the zone still lands on the floor below: a jump is not a way out of it.
                HazardPatterns.TryGround(_target.position, _zoneHeight, _groundMask, out point);
            }

            var zone = Instantiate(_prefab, point, Quaternion.identity).GetComponent<HazardZone>();
            if (zone != null)
            {
                zone.Arm(_faction, _owner);
            }
        }

        /// <summary>Stops every volley of a side mid-way; what it already dropped is swept by <see cref="HazardZone.ClearSpawned"/>.</summary>
        public static void Cancel(Faction faction)
        {
            for (var i = Running.Count - 1; i >= 0; i--)
            {
                var volley = Running[i];
                if (volley != null && volley._faction == faction)
                {
                    Destroy(volley.gameObject);
                }
            }
        }
    }
}
