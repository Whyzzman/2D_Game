using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The far layer of one room's painted backdrop: it follows a share of the camera's offset from
    /// the room's middle, so it reads as further away than the room's own wall, and never slides
    /// more than the margin it was painted with — its edge is never seen. A sprite mask the size of
    /// the room keeps it from showing in the room next door.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomParallax : MonoBehaviour
    {
        [Tooltip("Share of the camera's offset from the room's middle the layer follows.")]
        [SerializeField, Range(0f, 1f)] private float _factor = 0.15f;
        [Tooltip("How far the layer may slide, in units: the margin painted around the room.")]
        [SerializeField, Min(0f)] private float _margin = 3f;
        [Tooltip("The room's middle in the world; the layer rests here when the camera is there.")]
        [SerializeField] private Vector2 _roomCentre;
        private Transform _camera;
        private Vector3 _rest;

        public float Factor => _factor;

        private void Awake()
        {
            _rest = transform.position;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                var main = Camera.main;
                if (main == null)
                {
                    return;
                }

                _camera = main.transform;
            }

            var offset = ((Vector2)_camera.position - _roomCentre) * _factor;
            offset.x = Mathf.Clamp(offset.x, -_margin, _margin);
            offset.y = Mathf.Clamp(offset.y, -_margin, _margin);
            transform.position = _rest + (Vector3)offset;
        }
    }
}
