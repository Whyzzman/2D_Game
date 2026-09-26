using UnityEngine;

namespace ForgottenKingdom
{
    public sealed class ReturnToStart : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D _player;
        [SerializeField] private Transform _spawn;
        [SerializeField] private float _minimumY = -8f;

        private void Awake()
        {
            if (_player == null || _spawn == null)
            {
                Debug.LogError("ReturnToStart requires a Player and a spawn point.", this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (_player.transform.position.y < _minimumY)
            {
                _player.ResetTo(_spawn.position);
            }
        }
    }
}
