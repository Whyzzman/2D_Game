using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Enemy attacks read like Hollow Knight's (GAME_FEEL_IDEAS item 12): through the windup the body
    /// draws a breath — a little taller and narrower, as a chest fills — and 0.2 s before the blow a
    /// sharp white flash sits on the weapon's point. The first glint at the windup's start stays; this
    /// adds the "now" to its "soon". Only while the swing is really winding up: a hurt or dead enemy
    /// lets the breath go and gets no flash.
    ///
    /// Owned and ticked by <see cref="CombatFeedbackService"/>. Animation still reads gameplay: this
    /// times itself from the attack's own windup and touches only the body sprite's scale, which
    /// nothing else writes while the enemy lives.
    /// </summary>
    public sealed class TelegraphCues
    {
        /// <summary>How long before the blow the point flashes.</summary>
        public const float FlashLead = 0.2f;
        /// <summary>The breath at its fullest: taller by this, narrower by 60 % of it.</summary>
        public const float Breath = 0.05f;

        private sealed class Cue
        {
            public EnemyController Enemy;
            public AttackDefinition Attack;
            public VisualSockets Sockets;
            public Transform Body;
            public float Started;
            public float Windup;
            public bool Flashed;
        }

        private readonly List<Cue> _cues = new List<Cue>();
        private readonly Action<Vector2> _flash;

        /// <param name="flash">Plays the point's flash at a position.</param>
        public TelegraphCues(Action<Vector2> flash)
        {
            _flash = flash;
        }

        public int Count => _cues.Count;

        public void Begin(EnemyController enemy, AttackDefinition attack, VisualSockets sockets)
        {
            if (enemy == null || attack == null || attack.Windup < FlashLead + 0.05f)
            {
                return;
            }

            var animation = enemy.GetComponentInChildren<EnemyAnimationController>(true);
            Drop(enemy);
            _cues.Add(new Cue
            {
                Enemy = enemy, Attack = attack, Sockets = sockets,
                Body = animation != null && animation.Body != null ? animation.Body.transform : null,
                Started = Time.time, Windup = attack.Windup
            });
        }

        /// <summary>Runs after the animation has drawn the frame.</summary>
        public void Tick()
        {
            for (var i = _cues.Count - 1; i >= 0; i--)
            {
                var cue = _cues[i];
                var winding = cue.Enemy != null && cue.Enemy.IsAlive && cue.Enemy.IsAttacking
                    && cue.Enemy.AttackPhase == AttackPhase.Windup;
                var age = Time.time - cue.Started;
                if (!winding || age > cue.Windup)
                {
                    Release(cue);
                    _cues.RemoveAt(i);
                    continue;
                }

                // In over the first 70 % of the windup, held, then let go into the blow.
                var t = Mathf.Clamp01(age / (cue.Windup * 0.7f));
                var breath = Breath * Mathf.Sin(t * Mathf.PI * 0.5f);
                if (cue.Body != null) { cue.Body.localScale = new Vector3(1f - breath * 0.6f, 1f + breath, 1f); }

                if (!cue.Flashed && age >= cue.Windup - FlashLead)
                {
                    cue.Flashed = true;
                    var point = cue.Sockets != null && cue.Sockets.Has(VisualSocket.Weapon)
                        ? (Vector2)cue.Sockets.Position(VisualSocket.Weapon)
                        : (Vector2)cue.Enemy.transform.position + Vector2.up;
                    _flash?.Invoke(point);
                }
            }
        }

        public void Clear()
        {
            foreach (var cue in _cues) { Release(cue); }
            _cues.Clear();
        }

        private void Drop(EnemyController enemy)
        {
            for (var i = _cues.Count - 1; i >= 0; i--)
            {
                if (_cues[i].Enemy == enemy)
                {
                    Release(_cues[i]);
                    _cues.RemoveAt(i);
                }
            }
        }

        private static void Release(Cue cue)
        {
            if (cue.Body != null) { cue.Body.localScale = Vector3.one; }
        }
    }
}
