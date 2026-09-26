using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Slow sideways breathing of a fog bank. Slow enough never to draw the eye from the fight —
    /// its job is only to stop the room looking painted.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FogDrift : MonoBehaviour
    {
        [Tooltip("Relative speed; 1 is a slow walk across two units and back.")]
        [SerializeField, Min(0f)] private float _speed = 0.3f;
        [SerializeField, Min(0f)] private float _reach = 2f;

        private Vector3 _origin;
        private float _phase;

        private void Awake()
        {
            _origin = transform.position;
            _phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            var offset = Mathf.Sin(Time.time * _speed * 0.25f + _phase) * _reach;
            transform.position = _origin + new Vector3(offset, 0f, 0f);
        }
    }
}
