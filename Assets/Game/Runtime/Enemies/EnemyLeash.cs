using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The village watchdog's chain (item 5b of the 15 zones, the user's choice of 2026-10-19): the
    /// dog is fast and leaps hard, but only as far as the chain lets it. The stake is where it was
    /// placed; past the chain's length the body stops, whatever the AI or a leap wanted, so the hero
    /// can walk round its reach or lure it to the end and strike from outside.
    ///
    /// Runs after the motor, so the clamp is the last word on the frame's velocity. The chain is
    /// drawn from the stake to the collar and pulls taut as the dog nears its end.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMotor2D))]
    public sealed class EnemyLeash : MonoBehaviour
    {
        [Tooltip("How far along the floor the dog may go from its stake.")]
        [SerializeField, Min(0.5f)] private float _length = 4.5f;
        [Tooltip("Where the chain meets the dog, relative to its feet; X is mirrored by facing.")]
        [SerializeField] private Vector2 _collar = new Vector2(0.45f, 0.55f);
        [SerializeField] private Color _chainColor = new Color(0.32f, 0.3f, 0.29f);

        private EnemyMotor2D _motor;
        private Rigidbody2D _body;
        private LineRenderer _chain;

        public Vector2 Stake { get; private set; }
        public float Length => _length;

        /// <summary>True while the chain holds the dog back.</summary>
        public bool IsTaut { get; private set; }

        private void Awake()
        {
            _motor = GetComponent<EnemyMotor2D>();
            _body = GetComponent<Rigidbody2D>();
            Stake = transform.position;
            _chain = BuildChain();
        }

        private void FixedUpdate()
        {
            var position = _body != null ? _body.position : (Vector2)transform.position;
            var velocity = _motor.Velocity;
            // Where this step would put the body: a leap covers more than the chain's slack in one.
            var next = position.x + velocity.x * Time.fixedDeltaTime - Stake.x;
            IsTaut = Mathf.Abs(next) >= _length;
            if (!IsTaut)
            {
                return;
            }

            var side = Mathf.Sign(next);
            var edge = Stake.x + side * _length;
            if (Mathf.Abs(position.x - Stake.x) > _length && _body != null)
            {
                _body.position = new Vector2(edge, position.y);
                position.x = edge;
            }

            _motor.SetVelocity(new Vector2((edge - position.x) / Time.fixedDeltaTime, velocity.y));
        }

        private void LateUpdate()
        {
            if (_chain == null)
            {
                return;
            }

            var collar = (Vector2)transform.position
                + new Vector2(_collar.x * _motor.FacingDirection, _collar.y);
            var stake = Stake + new Vector2(0f, 0.25f);
            // A slack chain sags to the floor; a taut one runs straight.
            var slack = 1f - Mathf.Clamp01(Mathf.Abs(collar.x - stake.x) / _length);
            var middle = (collar + stake) * 0.5f + Vector2.down * slack * 0.45f;
            _chain.SetPosition(0, stake);
            _chain.SetPosition(1, middle);
            _chain.SetPosition(2, collar);
        }

        private LineRenderer BuildChain()
        {
            var host = new GameObject("Chain");
            host.transform.SetParent(transform, false);
            var line = host.AddComponent<LineRenderer>();
            line.positionCount = 3;
            line.useWorldSpace = true;
            line.widthMultiplier = 0.07f;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null)
            {
                line.sharedMaterial = new Material(shader);
            }

            line.startColor = line.endColor = _chainColor;
            line.sortingLayerName = "Characters";
            line.sortingOrder = -1;
            return line;
        }
    }
}
