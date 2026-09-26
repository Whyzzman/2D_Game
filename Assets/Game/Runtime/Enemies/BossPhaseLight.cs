using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom
{
    /// <summary>
    /// The light a boss carries: a cold gleam in the eye slits while it stands disciplined, and
    /// the violet of its broken core once phase two begins — so the change reads in the room's
    /// light as well as in the pose. It only reads the boss; it decides nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossPhaseLight : MonoBehaviour
    {
        [SerializeField] private BossController _boss;
        [SerializeField] private Light2D _light;
        [SerializeField] private Color _phaseOneColor = new Color(0.6f, 0.75f, 1f);
        [SerializeField, Min(0f)] private float _phaseOneIntensity = 0.45f;
        [SerializeField] private Color _phaseTwoColor = new Color(0.62f, 0.3f, 1f);
        [SerializeField, Min(0f)] private float _phaseTwoIntensity = 1.5f;
        [SerializeField, Min(0f)] private float _pulseSpeed = 2.2f;

        private void Awake()
        {
            if (_boss == null) { _boss = GetComponentInParent<BossController>(); }
            if (_light == null) { _light = GetComponent<Light2D>(); }
        }

        private void LateUpdate()
        {
            if (_boss == null || _light == null)
            {
                return;
            }

            var alive = _boss.Actor != null && _boss.Actor.IsAlive;
            var dormant = _boss.State == BossState.Dormant;
            if (!alive || dormant)
            {
                _light.intensity = dormant && alive ? _phaseOneIntensity * 0.35f : 0f;
                _light.color = _phaseOneColor;
                return;
            }

            if (_boss.Phase >= 2)
            {
                // The corrupted core breathes; phase one is steady.
                var pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * _pulseSpeed * Mathf.PI);
                _light.color = _phaseTwoColor;
                _light.intensity = _phaseTwoIntensity * pulse;
            }
            else
            {
                _light.color = _phaseOneColor;
                _light.intensity = _phaseOneIntensity;
            }
        }
    }
}
