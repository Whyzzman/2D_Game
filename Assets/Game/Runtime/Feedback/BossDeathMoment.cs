using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A boss's death as the end of something (GAME_FEEL_IDEAS item 11): the world slows to a quarter
    /// for 0.6 s, light breaks out of the body at one point after another, as if from cracks, and a
    /// last flare comes as time returns — with the cry held back until then
    /// (<see cref="CombatAudioBinder"/> waits <see cref="CryDelay"/>). Before, the death weighed as
    /// much as a heavy hit.
    ///
    /// Owned and ticked by <see cref="CombatFeedbackService"/>; draws through its light pool and
    /// borrows the base time scale, handing it back only if nobody changed it meanwhile. The slow is
    /// scaled by the hit-stop accessibility setting, and gone at zero.
    /// </summary>
    public sealed class BossDeathMoment
    {
        public const float SlowScale = 0.25f;
        public const float SlowTime = 0.6f;
        public const int Cracks = 6;

        /// <summary>When the cry comes: as time returns, the hit-stop setting applied.</summary>
        public static float CryDelay => SlowTime * FeedbackSettings.HitStopMultiplier;

        public static readonly Color CrackLight = new Color(1f, 0.92f, 0.78f, 1f);

        private readonly Action<Vector2, Color, float, float> _light;
        private readonly float[] _crackAt = new float[Cracks];
        private readonly Vector2[] _crackPoint = new Vector2[Cracks];
        private readonly bool[] _crackDone = new bool[Cracks];
        private Vector2 _centre;
        private float _age = -1f;
        private float _length;
        private bool _slowing;
        private bool _flared;

        /// <param name="light">Plays one light: position, colour, radius, seconds.</param>
        public BossDeathMoment(Action<Vector2, Color, float, float> light)
        {
            _light = light;
        }

        public bool IsRunning => _age >= 0f;

        public void Begin(Bounds body)
        {
            _centre = body.center;
            _age = 0f;
            _flared = false;
            _length = SlowTime * FeedbackSettings.HitStopMultiplier;
            // One crack after another, each at its own place on the body, the last just before the end.
            for (var i = 0; i < Cracks; i++)
            {
                _crackAt[i] = Mathf.Max(_length, 0.3f) * (0.08f + 0.8f * i / Cracks);
                _crackPoint[i] = new Vector2(
                    UnityEngine.Random.Range(body.min.x + body.extents.x * 0.3f, body.max.x - body.extents.x * 0.3f),
                    UnityEngine.Random.Range(body.min.y + body.extents.y * 0.3f, body.max.y - body.extents.y * 0.2f));
                _crackDone[i] = false;
            }

            if (_length > 0.01f && GameTimeScale.BaseScale >= 0.999f)
            {
                GameTimeScale.SetBaseScale(SlowScale);
                _slowing = true;
            }
        }

        /// <param name="dt">Real seconds: the moment runs on the clock it is slowing.</param>
        public void Tick(float dt)
        {
            if (_age < 0f) { return; }

            _age += dt;
            var span = Mathf.Max(_length, 0.3f);
            for (var i = 0; i < Cracks; i++)
            {
                if (_crackDone[i] || _age < _crackAt[i]) { continue; }

                _crackDone[i] = true;
                _light?.Invoke(_crackPoint[i], CrackLight, 1.1f + i * 0.12f, 0.35f + (span - _crackAt[i]));
            }

            if (_age >= _length) { EndSlow(); }

            if (!_flared && _age >= span)
            {
                _flared = true;
                _light?.Invoke(_centre, CrackLight, 4.5f, 0.6f);
                _age = -1f;
            }
        }

        public void Stop()
        {
            EndSlow();
            _age = -1f;
        }

        private void EndSlow()
        {
            if (!_slowing) { return; }

            _slowing = false;
            if (Mathf.Approximately(GameTimeScale.BaseScale, SlowScale))
            {
                GameTimeScale.SetBaseScale(1f);
            }
        }
    }
}
