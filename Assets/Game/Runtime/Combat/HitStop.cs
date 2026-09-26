using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Very short global freeze after a solid hit. Time scale is global by nature, so the runner is
    /// created on demand instead of being wired into every attacker.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class HitStop : MonoBehaviour
    {
        private static HitStop _runner;
        private float _remaining;
        private float _restoreTimeScale = 1f;

        public static bool IsActive => _runner != null && _runner._remaining > 0f;

        public static void Request(float duration)
        {
            if (duration <= 0f || !Application.isPlaying)
            {
                return;
            }

            if (_runner == null)
            {
                var host = new GameObject(nameof(HitStop)) { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(host);
                _runner = host.AddComponent<HitStop>();
            }

            _runner.Begin(duration);
        }

        private void Begin(float duration)
        {
            // A longer freeze wins; overlapping hits must not stack into a noticeable pause.
            if (_remaining <= 0f)
            {
                _restoreTimeScale = Time.timeScale;
            }

            _remaining = Mathf.Max(_remaining, duration);
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (_remaining <= 0f)
            {
                return;
            }

            _remaining -= Time.unscaledDeltaTime;
            if (_remaining <= 0f)
            {
                Time.timeScale = _restoreTimeScale;
            }
        }

        private void OnDestroy()
        {
            if (_remaining > 0f)
            {
                Time.timeScale = _restoreTimeScale;
            }

            if (_runner == this)
            {
                _runner = null;
            }
        }
    }
}
