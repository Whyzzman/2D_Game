using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenKingdom
{
    /// <summary>
    /// Development-only readout and state forcing for enemy and boss animation. It answers the two
    /// questions that cannot be answered at full speed — does the visible contact frame fall inside
    /// the real active window, and does every state actually have art — and it does so by reading
    /// the running fight rather than by simulating one.
    ///
    /// Off by default, reads state only, and forces nothing unless a key is pressed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAnimationDebugTools : MonoBehaviour
    {
        [Tooltip("F6 toggles the readout, F7 cycles the inspected enemy, [ ] step its state.")]
        [SerializeField] private bool _isVisible;
        [SerializeField] private bool _drawHitboxes = true;
        private EnemyAnimationController[] _enemies = Array.Empty<EnemyAnimationController>();
        private BossAnimationController _boss;
        private int _index;
        private readonly StringBuilder _text = new StringBuilder();
        private GUIStyle _style;

        private void Start() => Rescan();

        /// <summary>Enemies are spawned and reset by the world, so the list is refreshed on demand.</summary>
        public void Rescan()
        {
            _enemies = FindObjectsByType<EnemyAnimationController>();
            _boss = FindAnyObjectByType<BossAnimationController>();
            if (_enemies.Length > 0)
            {
                _index = Mathf.Clamp(_index, 0, _enemies.Length - 1);
            }
        }

        private EnemyAnimationController Selected =>
            _enemies.Length > 0 && _index < _enemies.Length ? _enemies[_index] : null;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f6Key.wasPressedThisFrame && DebugPanels.MayToggle(this))
            {
                _isVisible = !_isVisible;
                DebugPanels.Report(this, _isVisible);
                if (_isVisible)
                {
                    Rescan();
                }
            }

            if (!_isVisible)
            {
                return;
            }

            if (keyboard.f7Key.wasPressedThisFrame) { CycleTarget(); }
            if (keyboard.f8Key.wasPressedThisFrame) { Release(); }
            if (keyboard.leftBracketKey.wasPressedThisFrame) { StepEnemyState(-1); }
            if (keyboard.rightBracketKey.wasPressedThisFrame) { StepEnemyState(1); }
            if (keyboard.semicolonKey.wasPressedThisFrame) { StepBossState(-1); }
            if (keyboard.quoteKey.wasPressedThisFrame) { StepBossState(1); }
        }

        private void CycleTarget()
        {
            Rescan();
            if (_enemies.Length == 0)
            {
                return;
            }

            Release();
            _index = (_index + 1) % _enemies.Length;
        }

        /// <summary>Hands every forced state back, so nothing is left pinned after inspecting.</summary>
        public void Release()
        {
            foreach (var enemy in _enemies)
            {
                if (enemy != null)
                {
                    enemy.ForcedState = null;
                }
            }

            if (_boss != null)
            {
                _boss.ForcedState = null;
            }
        }

        public void StepEnemyState(int direction)
        {
            var enemy = Selected;
            if (enemy == null)
            {
                return;
            }

            var states = (EnemyAnimationState[])Enum.GetValues(typeof(EnemyAnimationState));
            var current = Array.IndexOf(states, enemy.ForcedState ?? enemy.State);
            enemy.ForcedState = states[(current + direction + states.Length) % states.Length];
        }

        public void StepBossState(int direction)
        {
            if (_boss == null)
            {
                return;
            }

            var states = (BossAnimationState[])Enum.GetValues(typeof(BossAnimationState));
            var current = Array.IndexOf(states, _boss.ForcedState ?? _boss.State);
            _boss.ForcedState = states[(current + direction + states.Length) % states.Length];
        }

        private void OnDisable()
        {
            Release();
            DebugPanels.Report(this, false);
        }

        private void OnGUI()
        {
            if (!_isVisible)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 13, richText = false };
            Build();
            GUI.Label(new Rect(12f, 340f, 620f, 380f), _text.ToString(), _style);
        }

        private void Build()
        {
            _text.Clear();
            _text.AppendLine("ENEMY ANIMATION  F6 hide · F7 next · F8 release · [ ] enemy · ; ' boss");
            AppendEnemy();
            _text.AppendLine();
            AppendBoss();
        }

        private void AppendEnemy()
        {
            var animation = Selected;
            if (animation == null)
            {
                _text.AppendLine("no enemies in the scene");
                return;
            }

            var enemy = animation.GetComponent<EnemyController>();
            var combat = animation.GetComponent<EnemyCombat>();
            var guard = animation.GetComponent<EnemyGuard>();
            var stagger = animation.GetComponent<StaggerMeter>();
            _text.Append($"[{_index + 1}/{_enemies.Length}] ").AppendLine(animation.name);
            _text.Append("  ai: ").Append(enemy != null ? enemy.State.ToString() : "n/a")
                .Append("   facing: ").Append(enemy != null ? enemy.FacingDirection : 0)
                .Append("   move: ")
                .AppendLine(enemy != null ? enemy.CurrentAttackKind.ToString() : "n/a");
            _text.Append("  animation: ").Append(animation.State);
            if (animation.ForcedState.HasValue)
            {
                _text.Append("  (FORCED)");
            }

            _text.AppendLine();
            AppendClip(animation.Clip, animation.NormalizedTime, animation.PlaybackSpeed);
            AppendAttackWindow(combat != null ? combat.CurrentAttack : null,
                combat != null && combat.Phase == AttackPhase.Active, animation.Clip);
            if (guard != null)
            {
                _text.Append("  guard: ").Append(guard.CurrentGuard.ToString("0"))
                    .Append("/").Append(guard.MaxGuard.ToString("0"))
                    .Append(guard.IsBroken ? "  BROKEN" : guard.IsGuarding ? "  guarding" : "")
                    .AppendLine();
            }

            if (stagger != null)
            {
                _text.Append("  poise: ").Append(stagger.Current.ToString("0"))
                    .Append("/").Append(stagger.Threshold.ToString("0")).AppendLine();
            }
        }

        private void AppendBoss()
        {
            if (_boss == null)
            {
                _text.AppendLine("no boss in the scene");
                return;
            }

            var boss = _boss.GetComponent<BossController>();
            _text.AppendLine("GUARDIAN");
            if (boss != null)
            {
                _text.Append("  phase: ").Append(boss.Phase)
                    .Append("   state: ").Append(boss.State)
                    .Append("   attack: ")
                    .Append(boss.CurrentAttackKind.HasValue
                        ? boss.CurrentAttackKind.Value.ToString()
                        : "-")
                    .Append(" #").Append(boss.CurrentSwingIndex)
                    .Append("   swing: ").AppendLine(boss.SwingPhase.ToString());
                _text.Append("  invulnerable: ")
                    .Append(boss.Actor != null && boss.Actor.IsInvulnerable ? "yes" : "no")
                    .Append("   transitions played: ").AppendLine(_boss.TransitionsPlayed.ToString());
            }

            _text.Append("  animation: ").Append(_boss.State);
            if (_boss.ForcedState.HasValue)
            {
                _text.Append("  (FORCED)");
            }

            _text.AppendLine();
            AppendClip(_boss.Clip, _boss.NormalizedTime, _boss.PlaybackSpeed);
            AppendAttackWindow(boss != null ? boss.CurrentSwing : null,
                boss != null && boss.SwingPhase == BossSwingPhase.Active, _boss.Clip);
        }

        private void AppendClip(AnimationClipDefinition clip, float normalized, float speed)
        {
            if (clip == null || !clip.HasFrames)
            {
                _text.AppendLine("  clip: MISSING — showing the fallback");
                return;
            }

            _text.Append("  clip: ")
                .Append(string.IsNullOrEmpty(clip.ClipName) ? "(unnamed)" : clip.ClipName)
                .Append("  ").Append(clip.FrameCount).Append(" frames @ ")
                .Append(clip.FramesPerSecond.ToString("0")).Append(" fps  t=")
                .Append(normalized.ToString("0.00")).Append("  playback ")
                .Append(speed.ToString("0.00")).Append("x");
            if (Mathf.Abs(speed - 1f) > 0.25f)
            {
                _text.Append("  <- clip length disagrees with the action");
            }

            _text.AppendLine();
        }

        /// <summary>
        /// The part that matters: where the real active window sits, and which frames the drawn
        /// point of contact should fall between. The numbers come from the attack data, never from
        /// the art.
        /// </summary>
        private void AppendAttackWindow(AttackDefinition attack, bool isActive,
            AnimationClipDefinition clip)
        {
            if (attack == null)
            {
                return;
            }

            var total = Mathf.Max(0.0001f, attack.Duration);
            var from = attack.Windup / total;
            var to = (attack.Windup + attack.ActiveTime) / total;
            var frames = clip != null ? Mathf.Max(1, clip.FrameCount) : 1;
            _text.Append("  attack: ").Append(attack.DisplayName)
                .Append("  windup ").Append(attack.Windup.ToString("0.00"))
                .Append(" | active ").Append(attack.ActiveTime.ToString("0.00"))
                .Append(" | recovery ").Append(attack.Recovery.ToString("0.00"))
                .Append(isActive ? "   HITBOX LIVE" : "").AppendLine();
            _text.Append("  contact should land in frames ")
                .Append(Mathf.RoundToInt(from * frames)).Append("..")
                .Append(Mathf.RoundToInt(to * frames)).AppendLine();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draws the real hitbox of whatever is swinging, so the drawn blade can be checked against
        /// the box the game actually uses. Editor only, and derived from the attack data.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_drawHitboxes || !Application.isPlaying)
            {
                return;
            }

            foreach (var animation in _enemies)
            {
                if (animation == null)
                {
                    continue;
                }

                var enemy = animation.GetComponent<EnemyController>();
                var combat = animation.GetComponent<EnemyCombat>();
                if (enemy == null || combat == null || combat.CurrentAttack == null)
                {
                    continue;
                }

                DrawBox(animation.transform.position, combat.CurrentAttack,
                    enemy.FacingDirection, combat.Phase == AttackPhase.Active);
            }

            var boss = _boss != null ? _boss.GetComponent<BossController>() : null;
            if (boss != null && boss.CurrentSwing != null)
            {
                var motor = boss.GetComponent<EnemyMotor2D>();
                DrawBox(boss.transform.position, boss.CurrentSwing,
                    motor != null ? motor.FacingDirection : 1,
                    boss.SwingPhase == BossSwingPhase.Active);
            }
        }

        private static void DrawBox(Vector3 origin, AttackDefinition attack, int facing,
            bool isActive)
        {
            var centre = (Vector2)origin
                + new Vector2(attack.HitboxOffset.x * facing, attack.HitboxOffset.y);
            Gizmos.color = isActive
                ? new Color(1f, 0.25f, 0.15f, 0.95f)
                : new Color(1f, 0.8f, 0.3f, 0.45f);
            Gizmos.DrawWireCube(centre, attack.HitboxSize);
        }
#endif
    }
}
