using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The torn back edge of the half-cloak, hanging off the drawn cloak and lagging behind the
    /// body (the user's choice, 2026-09-27: a live edge on the frames rather than a separate cloak
    /// or a scarf). The frames stay as they are; this is a short chain on springs that trails behind
    /// a run, flaps at a stop or a turn and settles while the hero stands.
    ///
    /// It hangs from the point each frame names (<see cref="ITrailAnchorSource.TryGetTrailAnchor"/>),
    /// so it stays on the cloak's hem through every pose. Enemies use the same chain for what
    /// hangs off them (visual item 4, 2026-09-28): a hound's tail, rags, a hood, a cape, roots. The final artist paints only this edge,
    /// as one strip texture; the chain bends it.
    ///
    /// Presentation only: it reads the body and never touches gameplay. Time stops it with the
    /// game — a pause or a hit-stop freezes the cloth like everything else.
    /// </summary>
    [DefaultExecutionOrder(100)] // after the frame is chosen and the squash has scaled the body
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class CloakTail : MonoBehaviour
    {
        /// <summary>Links in the chain; enough to curve, few enough to stay a cut edge.</summary>
        public const int Links = 4;

        /// <summary>How long the edge hangs below the cloak; the art pipeline keeps it off the floor.</summary>
        public const float DefaultLength = 0.26f;

        private const float Step = 1f / 120f;
        private const int MaxSteps = 8;
        /// <summary>A jump further than this in one frame is a respawn, not motion.</summary>
        private const float SnapDistance = 2f;
        /// <summary>
        /// How fast the hanging point catches up with the frame's (per second). Frames are discrete,
        /// so the drawn hem jumps a few pixels from one to the next and the whole way across on a
        /// turn; followed exactly, every jump kicks the cloth. The point slides instead, hidden
        /// behind the body, and the body's own travel through the world is never smoothed.
        /// </summary>
        private const float AnchorFollow = 30f;
        /// <summary>How quickly the cloth feels a change in the body's speed (per second).</summary>
        private const float VelocityFollow = 10f;
        /// <summary>The furthest the wind of motion bends the hang, relative to its own weight.</summary>
        private const float MaxTrail = 1.5f;
        /// <summary>Each link aims partly along the one above it, so the edge curves rather than kinks.</summary>
        private const float ParentPull = 0.4f;
        private const float MaxSwing = 30f;

        [Tooltip("The animation that names the hanging point per frame (an ITrailAnchorSource).")]
        [SerializeField] private MonoBehaviour _animation;
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("The strip, root on the left and torn tip on the right.")]
        [SerializeField] private Texture2D _texture;
        [SerializeField, Min(0.05f)] private float _length = DefaultLength;
        [Tooltip("Hanging point used when the frame names none: x, y from the body pivot, z the angle.")]
        [SerializeField] private Vector3 _restAnchor = new Vector3(-0.2f, 0.38f, 14f);
        [Tooltip("How briskly a link swings back to where it should hang (radians per second).")]
        [SerializeField, Min(1f)] private float _stiffness = 13f;
        [Tooltip("Below 1 the edge overshoots and flaps once or twice before it settles.")]
        [SerializeField, Range(0.1f, 1f)] private float _damping = 0.38f;
        [Tooltip("How far back running bends it, per unit of speed (sideways).")]
        [SerializeField, Min(0f)] private float _trail = 0.16f;
        [Tooltip("The same for rising and falling: less, so a fall streams it rather than standing it up.")]
        [SerializeField, Min(0f)] private float _verticalTrail = 0.06f;
        [Tooltip("How hard a sudden start, stop or turn throws it.")]
        [SerializeField, Min(0f)] private float _inertia = 1.5f;

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        private readonly Vector3[] _nodes = new Vector3[Links + 1];
        // Each link's angle from straight down (radians, positive towards +x) and its swing speed:
        // links of fixed length can never stretch or explode, whatever the body does.
        private readonly float[] _angles = new float[Links + 1];
        private readonly float[] _swings = new float[Links + 1];
        private LineRenderer _line;
        private MaterialPropertyBlock _block;
        private ITrailAnchorSource _source;
        private Vector3 _root;
        private Vector3 _offset;
        private Vector2 _rootVelocity;
        private Vector2 _hang = Vector2.down;
        private Vector2 _targetHang = Vector2.down;
        private float _accumulator;
        private bool _placed;

        /// <summary>The hanging point on the cloak, in world space.</summary>
        public Vector3 Root => _nodes[0];

        /// <summary>The torn end, in world space.</summary>
        public Vector3 Tip => _nodes[Links];

        /// <summary>The direction the cloth hangs in when nothing moves, in world space.</summary>
        public Vector2 RestDirection => _hang;

        private float LinkLength => _length / Links;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = Links + 1;
            _line.useWorldSpace = true;
            _source = _animation as ITrailAnchorSource ?? GetComponentInParent<ITrailAnchorSource>();
            if (_body == null && _source != null) { _body = _source.Body; }
            if (_body != null)
            {
                _line.sortingLayerID = _body.sortingLayerID;
                // Behind the body: it comes out from under the drawn cloak's hem.
                _line.sortingOrder = _body.sortingOrder - 1;
            }

            if (_texture != null)
            {
                _block = new MaterialPropertyBlock();
                _line.GetPropertyBlock(_block);
                _block.SetTexture(MainTex, _texture);
                _line.SetPropertyBlock(_block);
            }
        }

        private void OnEnable() => _placed = false;

        private void LateUpdate() => Tick(Time.deltaTime);

        /// <summary>Advances the cloth; separate from LateUpdate so tests can step it exactly.</summary>
        public void Tick(float deltaTime)
        {
            if (_body == null)
            {
                return;
            }

            deltaTime = Mathf.Max(0f, deltaTime);
            var previousRoot = _root;
            var target = ReadAnchor();
            var origin = _body.transform.position;
            if (!_placed || (origin + _offset - previousRoot).sqrMagnitude > SnapDistance * SnapDistance)
            {
                _offset = target - origin;
                _hang = _targetHang;
                _root = target;
                Hang();
                previousRoot = _root;
            }
            else
            {
                var follow = 1f - Mathf.Exp(-AnchorFollow * deltaTime);
                _offset = Vector3.Lerp(_offset, target - origin, follow);
                _hang = Vector2.Lerp(_hang, _targetHang, follow).normalized;
                _root = origin + _offset;
            }

            _accumulator = Mathf.Min(_accumulator + deltaTime, Step * MaxSteps);
            var steps = Mathf.FloorToInt(_accumulator / Step);
            for (var i = 0; i < steps; i++)
            {
                // The root moves smoothly across the frame rather than in one jump per render.
                Simulate((Vector2)(_root - previousRoot) / (steps * Step));
            }

            _accumulator -= steps * Step;
            Lay();
            Present();
        }

        /// <summary>The frame's hanging point in the world; sets the hang direction to aim for.</summary>
        private Vector3 ReadAnchor()
        {
            var anchor = _restAnchor;
            if (_source != null && _source.TryGetTrailAnchor(out var framed))
            {
                anchor = framed;
            }

            var body = _body.transform;
            // Frames are drawn facing right; the motor mirrors the visual, and the hang with it.
            var facing = body.lossyScale.x < 0f ? -1f : 1f;
            var angle = anchor.z * Mathf.Deg2Rad;
            _targetHang = new Vector2(-Mathf.Sin(angle) * facing, -Mathf.Cos(angle));
            return body.TransformPoint(new Vector3(anchor.x, anchor.y, 0f));
        }

        /// <summary>Lays the chain straight along its hanging direction, at rest.</summary>
        private void Hang()
        {
            var angle = AngleOf(_hang);
            for (var i = 0; i <= Links; i++)
            {
                _angles[i] = angle;
                _swings[i] = 0f;
            }

            _rootVelocity = Vector2.zero;
            _accumulator = 0f;
            _placed = true;
        }

        private static float AngleOf(Vector2 direction) => Mathf.Atan2(direction.x, -direction.y);

        private void Simulate(Vector2 rootVelocity)
        {
            var follow = 1f - Mathf.Exp(-VelocityFollow * Step);
            var smoothed = Vector2.Lerp(_rootVelocity, rootVelocity, follow);
            var acceleration = (smoothed - _rootVelocity) / Step;
            _rootVelocity = smoothed;

            // Moving through the air bends the hang back against the motion — what makes a run
            // stream the edge behind and a stop swing it forward.
            var wind = new Vector2(-smoothed.x * _trail, -smoothed.y * _verticalTrail);
            wind = Vector2.ClampMagnitude(wind, MaxTrail);
            var aim = AngleOf(_hang + wind);

            for (var i = 1; i <= Links; i++)
            {
                // Looser towards the tip: the torn end moves most.
                var stiffness = _stiffness * (1f - 0.12f * (i - 1));
                var goal = i == 1 ? aim : aim + Mathf.DeltaAngle(aim * Mathf.Rad2Deg,
                    _angles[i - 1] * Mathf.Rad2Deg) * Mathf.Deg2Rad * ParentPull;
                var error = Mathf.DeltaAngle(_angles[i] * Mathf.Rad2Deg, goal * Mathf.Rad2Deg)
                    * Mathf.Deg2Rad;
                // A sudden change of speed throws the link the other way, across its own line.
                var across = acceleration.x * Mathf.Cos(_angles[i]) + acceleration.y * Mathf.Sin(_angles[i]);
                var swing = stiffness * stiffness * error - 2f * _damping * stiffness * _swings[i]
                    - _inertia * across;
                _swings[i] = Mathf.Clamp(_swings[i] + swing * Step, -MaxSwing, MaxSwing);
                _angles[i] += _swings[i] * Step;
            }
        }

        /// <summary>Places the links end to end from the hanging point.</summary>
        private void Lay()
        {
            var link = LinkLength;
            _nodes[0] = _root;
            for (var i = 1; i <= Links; i++)
            {
                _nodes[i] = _nodes[i - 1]
                    + new Vector3(Mathf.Sin(_angles[i]), -Mathf.Cos(_angles[i]), 0f) * link;
            }
        }

        private void Present()
        {
            var visible = _body.enabled && _body.gameObject.activeInHierarchy;
            _line.enabled = visible;
            if (!visible)
            {
                return;
            }

            _line.SetPositions(_nodes);
            var color = new Color(1f, 1f, 1f, _body.color.a);
            _line.startColor = color;
            _line.endColor = color;
        }
    }
}
