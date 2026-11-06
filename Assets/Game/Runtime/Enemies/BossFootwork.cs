using System;
using System.Collections;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// How a boss moves when it is not swinging (2026-11-06, user: «рухливість босів»; decided by Claude under
    /// standing approval). Between moves it no longer stands still for the whole pause: it walks in when the hero is
    /// far, gives ground when he crowds it, and shifts its weight in and out while it chooses — always facing him,
    /// never off a ledge or into a wall. The pause lasts as long as before, so the openings the fight gives are the
    /// same. Stepping back and closing in came here from <see cref="BossController"/> with it.
    /// </summary>
    public sealed class BossFootwork
    {
        /// <summary>Of the boss's walking speed: a stalk, not a march.</summary>
        public const float StalkSpeed = 0.5f;
        /// <summary>One beat of the weave: hold, step in, hold, step out.</summary>
        public const float WeaveBeat = 0.5f;

        private readonly EnemyMotor2D _motor;
        private readonly Transform _self;
        private readonly Action<BossState> _setState;

        public BossFootwork(EnemyMotor2D motor, Transform self, Action<BossState> setState)
        {
            _motor = motor;
            _self = self;
            _setState = setState;
        }

        /// <summary>
        /// The pause between moves, spent on its feet: closes in past <paramref name="far"/>, backs off inside
        /// <paramref name="near"/>, weaves between them.
        /// </summary>
        public IEnumerator Stalk(Transform player, float duration, float near, float far, float speed)
        {
            var elapsed = 0f;
            while (elapsed < duration && player != null)
            {
                _motor.FaceTowards(player.position.x);
                var distance = Mathf.Abs(player.position.x - _self.position.x);
                var beat = Mathf.FloorToInt(elapsed / WeaveBeat) % 4;
                var step = distance > far ? 1 : distance < near ? -1 : beat == 1 ? 1 : beat == 3 ? -1 : 0;
                // a weave step never takes it inside the near line
                if (step > 0 && distance <= far && distance - speed * WeaveBeat < near)
                {
                    step = 0;
                }

                if (step != 0 && CanStep(step * _motor.FacingDirection))
                {
                    _setState(BossState.Reposition);
                    _motor.Drive(speed * StalkSpeed * step * (step < 0 ? 0.8f : 1f));
                }
                else
                {
                    _setState(BossState.Idle);
                    _motor.Stop();
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            _motor.Stop();
            _setState(BossState.Idle);
        }

        /// <summary>Standing inside the boss is not a safe spot: it steps back, facing the hero, and swings.</summary>
        public IEnumerator BackOff(Transform player, float speed)
        {
            _setState(BossState.Reposition);
            var elapsed = 0f;
            var away = player != null && player.position.x > _self.position.x ? -1 : 1;
            while (elapsed < 0.4f)
            {
                _motor.Face(-away);
                if (!_motor.HasGroundToward(away) || _motor.HasWallToward(away))
                {
                    break;
                }

                _motor.Drive(-speed * 0.6f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _motor.Stop();
        }

        /// <summary>Nothing in reach: walks up to <paramref name="range"/> of the hero, for at most <paramref name="timeout"/>.</summary>
        public IEnumerator Close(Transform player, float range, float timeout, float speed)
        {
            _setState(BossState.Reposition);
            var elapsed = 0f;
            while (elapsed < timeout && player != null)
            {
                if (Mathf.Abs(player.position.x - _self.position.x) <= range)
                {
                    break;
                }

                _motor.FaceTowards(player.position.x);
                if (!_motor.HasGroundAhead() || _motor.HasWallAhead())
                {
                    break;
                }

                _motor.Drive(speed);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _motor.Stop();
        }

        private bool CanStep(int direction) => _motor.HasGroundToward(direction) && !_motor.HasWallToward(direction);
    }
}
