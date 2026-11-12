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
        [Tooltip("Phase three (the King's eclipse): the crown is the light the arena still has.")]
        [SerializeField] private Color _phaseThreeColor = new Color(1f, 0.64f, 0.3f);
        [SerializeField, Min(0f)] private float _phaseThreeIntensity = 2.8f;
        [SerializeField, Min(0f)] private float _phaseThreeRadius = 8f;

        private float _baseRadius;

        private void Awake()
        {
            if (_boss == null) { _boss = GetComponentInParent<BossController>(); }
            if (_light == null) { _light = GetComponent<Light2D>(); }
            _baseRadius = _light != null ? _light.pointLightOuterRadius : 0f;
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
                _light.pointLightOuterRadius = _baseRadius;
                _light.color = _phaseOneColor;
                return;
            }

            _light.pointLightOuterRadius = _boss.Phase >= 3 ? _phaseThreeRadius : _baseRadius;
            if (_boss.Phase >= 3)
            {
                // a slow flicker, a fire held in a crown rather than a core that breathes
                var flicker = 0.9f + 0.1f * Mathf.Sin(Time.time * 7.3f) * Mathf.Sin(Time.time * 2.9f);
                _light.color = _phaseThreeColor;
                _light.intensity = _phaseThreeIntensity * flicker;
            }
            else if (_boss.Phase >= 2)
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
