using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// A boss fight's presentation: the arena seals with a flare and carries glowing runes, its
    /// lights burn brighter for the fight, every boss swing glints, a charge or leap glows the
    /// body red first, and a leap's slam plays where it lands.
    /// </summary>
    public sealed class BossArenaTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private BossEncounter _encounter;
        private BossController _boss;
        private CombatFeedbackService _feedback;
        private ArenaSealGlow _glow;
        private readonly List<FeedbackKind> _played = new List<FeedbackKind>();
        private BossSwingPhase? _slamPhase;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            FeedbackSettings.ResetToDefaults();
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _encounter = TombScene.GuardianEncounter();
            _boss = TombScene.Guardian();
            _feedback = Object.FindAnyObjectByType<CombatFeedbackService>();
            _feedback.Played += OnPlayed;
            yield return null;
            _glow = _encounter.GetComponentInChildren<ArenaSealGlow>();
            _played.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (_feedback != null) { _feedback.Played -= OnPlayed; }
            FeedbackSettings.ResetToDefaults();
            GamePause.Clear();
            SaveSystem.Delete();
            GameTimeScale.Reset();
        }

        private void OnPlayed(FeedbackKind kind, Vector2 position)
        {
            _played.Add(kind);
            if (kind == FeedbackKind.BossSlam) { _slamPhase = _boss.SwingPhase; }
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float seconds)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        /// <summary>Into the Guardian's hall, <paramref name="dx"/> past the trigger by his door.</summary>
        private IEnumerator EnterArena(float dx = 0f)
        {
            _player.GetComponent<CombatActor>().SetInvulnerable(ImmunitySource.Script, 999f);
            _motor.ResetTo(TombScene.ArenaEntry() + new Vector2(dx, 0f));
            yield return WaitUntil(() => _encounter.IsArenaSealed, 2f);
        }

        [UnityTest]
        public IEnumerator TheArenaSealsWithRunesBurnsBrighterAndBreaksTheSealOnVictory()
        {
            Assert.That(_glow, Is.Not.Null, "The arena has no seal glow.");
            Assert.That(_glow.IsGlowing, Is.False, "The runes glow before the fight.");

            yield return EnterArena();
            yield return null;
            Assert.That(_played, Does.Contain(FeedbackKind.ArenaSeal), "Sealing the arena made no flare.");
            Assert.That(_glow.IsGlowing, Is.True, "The sealed barrier carries no runes.");

            yield return WaitUntil(() => _glow.FightBlend >= 1f, 3f);
            Assert.That(_glow.FightBlend, Is.EqualTo(1f), "The arena lights did not rise for the fight.");

            _boss.Actor.ClearInvulnerability();
            _boss.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.left, isUnblockable: true,
                attacker: _player.gameObject));
            yield return WaitUntil(() => !_encounter.IsArenaSealed, 6f);
            yield return null;

            Assert.That(_played, Does.Contain(FeedbackKind.ArenaOpen), "The beaten boss's seal broke silently.");
            Assert.That(_glow.IsGlowing, Is.False);
            yield return WaitUntil(() => _glow.FightBlend <= 0f, 3f);
            Assert.That(_glow.FightBlend, Is.Zero, "The arena lights stayed at fight brightness.");
        }

        [UnityTest]
        public IEnumerator TheSealNamesTheBossOnATitleCardThatFadesAway()
        {
            var hud = Object.FindAnyObjectByType<HudController>();
            Assert.That(hud.BossTitle, Is.Not.Null);
            Assert.That(hud.BossTitle.IsShowing, Is.False);

            yield return EnterArena();
            yield return null;
            yield return null;
            Assert.That(hud.BossTitle.IsShowing, Is.True, "The fight began without the boss's name.");
            Assert.That(hud.BossTitle.Name, Is.EqualTo("Страж Гробниці"));
            Assert.That(hud.BossTitle.Epithet, Is.EqualTo("ТОЙ, ХТО НЕ СПИТЬ"));

            yield return new WaitForSecondsRealtime(BossTitleCard.Total + 0.3f);
            Assert.That(hud.BossTitle.IsShowing, Is.False, "The title card never left.");

            foreach (var boss in Object.FindObjectsByType<BossController>(FindObjectsInactive.Include))
            {
                Assert.That(boss.Epithet, Is.Not.Empty, $"{boss.name} has no epithet.");
            }
        }

        [UnityTest]
        public IEnumerator DyingLiftsTheSealWithoutTheVictoryFlare()
        {
            yield return EnterArena();
            _player.GetComponent<CombatActor>().ClearInvulnerability();
            Object.FindAnyObjectByType<PlayerRespawner>().Kill();
            yield return WaitUntil(() => !_encounter.IsArenaSealed, 6f);
            yield return null;

            Assert.That(_encounter.IsArenaSealed, Is.False);
            Assert.That(_played.Contains(FeedbackKind.ArenaOpen), Is.False, "Dying broke the seal with the victory flare.");
        }

        [UnityTest]
        public IEnumerator AChargeGlintsThenGlowsRedBeforeItRuns()
        {
            var flash = _boss.GetComponent<HitFlash>();
            var warned = false;
            // Six units off: in phase one only the charge reaches that far.
            yield return EnterArena(2f);
            GameTimeScale.SetBaseScale(2f);
            yield return WaitUntil(() =>
            {
                warned |= flash != null && flash.IsWarning;
                return _played.Contains(FeedbackKind.BossCharge);
            }, 25f);

            Assert.That(_played, Does.Contain(FeedbackKind.EnemyTelegraph), "The boss's windup did not glint.");
            Assert.That(_played, Does.Contain(FeedbackKind.BossDanger), "The charge came without a warning.");
            Assert.That(warned, Is.True, "The boss's body did not glow red before the charge.");
            Assert.That(_played.IndexOf(FeedbackKind.BossDanger),
                Is.LessThan(_played.IndexOf(FeedbackKind.BossCharge)), "The warning came after the charge.");
        }

        [UnityTest]
        public IEnumerator ALeapsSlamPlaysWhereItLandsNotWhereItJumps()
        {
            yield return EnterArena(-1f);
            // Into phase two, where the leap lives.
            // In small blows, since armour takes its share of each one.
            for (var i = 0; i < 200 && _boss.Actor.Health.Normalized > 0.45f; i++)
            {
                _boss.Actor.ClearInvulnerability();
                _boss.Actor.ApplyDamage(new DamageInfo(_boss.Actor.Health.Max * 0.04f, Vector2.left,
                    isUnblockable: true, attacker: _player.gameObject));
            }

            yield return WaitUntil(() => _boss.Phase == 2, 5f);
            Assert.That(_boss.Phase, Is.EqualTo(2));
            GameTimeScale.SetBaseScale(2f);
            yield return WaitUntil(() => _played.Contains(FeedbackKind.BossSlam), 40f);

            Assert.That(_played, Does.Contain(FeedbackKind.BossSlam), "No leap in forty seconds.");
            Assert.That(_slamPhase, Is.EqualTo(BossSwingPhase.Travel),
                "The slam played away from the landing.");
        }
    }
}
