using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The camera's reactions to the fight, kept apart from the follow itself: it widens for a
    /// boss so the whole arena and every telegraph are on screen, and gives a parry a short push
    /// in — the beat that makes the timing feel earned.
    ///
    /// Restrained on purpose: a slow widen over a second, a punch of a few percent for a tenth of
    /// a second, and the reduced-motion setting turns the punch off.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CameraFollow2D))]
    public sealed class CameraDirector : MonoBehaviour
    {
        [SerializeField, Range(1f, 1.4f)] private float _bossZoom = 1.18f;
        [SerializeField, Min(0.05f)] private float _bossZoomTime = 1.1f;
        [SerializeField, Range(0f, 0.2f)] private float _parryPunch = 0.06f;
        [SerializeField, Min(0.02f)] private float _parryPunchTime = 0.12f;

        private CameraFollow2D _follow;
        private BossEncounter[] _encounters = System.Array.Empty<BossEncounter>();
        private PlayerCharacter _player;

        private void Awake()
        {
            _follow = GetComponent<CameraFollow2D>();
            _encounters = FindObjectsByType<BossEncounter>(FindObjectsInactive.Include);
            _player = FindAnyObjectByType<PlayerCharacter>();
        }

        private void OnEnable()
        {
            foreach (var encounter in _encounters)
            {
                encounter.FightStarted += OnFightStarted;
                encounter.FightEnded += OnFightEnded;
            }

            if (_player != null)
            {
                _player.Parried += OnParried;
            }
        }

        private void OnDisable()
        {
            foreach (var encounter in _encounters)
            {
                if (encounter != null)
                {
                    encounter.FightStarted -= OnFightStarted;
                    encounter.FightEnded -= OnFightEnded;
                }
            }

            if (_player != null)
            {
                _player.Parried -= OnParried;
            }
        }

        private void OnFightStarted(BossController boss) => _follow.SetZoom(_bossZoom, _bossZoomTime);

        private void OnFightEnded(BossController boss) => _follow.SetZoom(1f, _bossZoomTime * 1.5f);

        private void OnParried(DamageInfo info)
        {
            if (!FeedbackSettings.ReducedMotion)
            {
                _follow.Punch(_parryPunch, _parryPunchTime);
            }
        }
    }
}
