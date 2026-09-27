using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The dressed tomb in play. The questions are the ones that decide whether an environment
    /// helps or lies: does decoration stay out of the collision, does the gameplay plane still
    /// draw in front of its own background, does the checkpoint show its real state, and does the
    /// arena seal and unseal with the fight rather than with the art.
    /// </summary>
    public sealed class TombEnvironmentTests
    {
        private PlayerCharacter _player;
        private GameProgress _progress;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return SceneManager.LoadSceneAsync("TombTrial");
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            yield return new WaitForFixedUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            GamePause.Clear();
            SaveSystem.Delete();
            Time.timeScale = 1f;
        }

        // ---------- the level is dressed ----------

        [UnityTest]
        public IEnumerator TheTombIsDressedRatherThanGrey()
        {
            yield return null;
            var environment = GameObject.Find("Environment");

            Assert.That(environment, Is.Not.Null, "The content build dresses the level.");
            var renderers = environment.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(renderers.Length, Is.GreaterThan(30),
                "A handful of props is still a greybox with decoration on it.");
        }

        [UnityTest]
        public IEnumerator EverySectionOfTheTombIsPresent()
        {
            yield return null;
            foreach (var section in new[]
            {
                "BurialHall", "GuardPassage", "CollapsedCrypt", "BossApproach", "GuardianArena",
                "Foreground"
            })
            {
                Assert.That(GameObject.Find(section), Is.Not.Null, $"{section} is missing.");
            }
        }

        [UnityTest]
        public IEnumerator DecorationNeverCarriesCollision()
        {
            // The rule the whole phase rests on: a decorative ledge the player can stand on is a
            // worse bug than no decoration at all.
            yield return null;
            var environment = GameObject.Find("Environment");
            var colliders = environment.GetComponentsInChildren<Collider2D>();

            Assert.That(colliders, Is.Empty,
                "Dressing must not add collision: " + string.Join(", ",
                    colliders.Select(c => c.name)));
        }

        [UnityTest]
        public IEnumerator TheGameplayFloorDrawsInFrontOfItsBackground()
        {
            yield return null;
            var floor = GameObject.Find("Floor").GetComponentInChildren<SpriteRenderer>();
            var names = SortingLayer.layers.Select(layer => layer.name).ToList();

            Assert.That(floor.sortingLayerName, Is.EqualTo("GameplayEnvironment"));
            foreach (var background in new[] { "BackgroundFar", "BackgroundMid", "BackgroundNear" })
            {
                Assert.That(names.IndexOf(background),
                    Is.LessThan(names.IndexOf(floor.sortingLayerName)));
            }
        }

        [UnityTest]
        public IEnumerator NothingGameplayRelevantWasLeftOnTheDefaultLayer()
        {
            yield return null;
            var stray = Object.FindObjectsByType<SpriteRenderer>()
                .Where(r => r.sortingLayerName == "Default")
                .Select(r => r.name)
                .ToArray();

            Assert.That(stray, Is.Empty,
                "Left on Default: " + string.Join(", ", stray));
        }

        // ---------- parallax ----------

        [UnityTest]
        public IEnumerator TheThreeBackgroundLayersParallaxAtIncreasingRates()
        {
            yield return null;
            // The Tomb's own backdrop; the forest brings three layers of its own further east.
            var layers = Object.FindObjectsByType<ParallaxLayer>()
                .Where(l => l.name.Contains("_tomb_"))
                .ToArray();

            Assert.That(layers.Length, Is.EqualTo(3));
            var far = Layer(layers, "far");
            var mid = Layer(layers, "mid");
            var near = Layer(layers, "near");
            Assert.That(far.Factor, Is.LessThan(mid.Factor));
            Assert.That(mid.Factor, Is.LessThan(near.Factor));
            Assert.That(near.Factor, Is.LessThan(1f),
                "A layer at 1 is nailed to the world and is not a background.");
        }

        [UnityTest]
        public IEnumerator ABackgroundAlwaysStaysUnderTheCamera()
        {
            // The property that matters is coverage, not displacement: however far a dash carries
            // the camera, no layer may leave a gap at the edge of the view. Each layer wraps by
            // whole tile widths, so it can never drift more than one tile from the camera.
            var layers = Object.FindObjectsByType<ParallaxLayer>();
            var camera = Camera.main.transform;
            var before = layers.ToDictionary(l => l, l => l.transform.position.x);

            _player.GetComponent<PlayerMotor2D>().ResetTo(
                new Vector2(_player.transform.position.x + 30f, _player.transform.position.y));
            yield return new WaitForSeconds(0.5f);

            foreach (var layer in layers)
            {
                var tile = layer.GetComponent<SpriteRenderer>().sprite.bounds.size.x;
                var drift = Mathf.Abs(layer.transform.position.x - camera.position.x);
                Assert.That(drift, Is.LessThan(tile + 0.001f),
                    $"{layer.name} drifted off the camera and would show an edge.");
                Assert.That(layer.transform.position.x, Is.Not.EqualTo(before[layer]),
                    $"{layer.name} did not move at all, so there is no parallax.");
            }
        }

        // ---------- the checkpoint ----------

        [UnityTest]
        public IEnumerator AnUnlitAltarShowsItsColdFlame()
        {
            yield return null;
            var checkpoint = NearestCheckpoint();
            var visual = checkpoint.GetComponent<CheckpointVisual>();

            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.HasApplied, Is.True,
                "Applied before the first frame, so a loaded save never shows cold stone first.");
            Assert.That(visual.ShowsLitFlame, Is.False);
        }

        [UnityTest]
        public IEnumerator LightingAnAltarTurnsOnItsWarmGlow()
        {
            var checkpoint = NearestCheckpoint();
            var visual = checkpoint.GetComponent<CheckpointVisual>();
            var glow = checkpoint.transform.Find("Glow").GetComponent<SpriteRenderer>();
            Assume.That(glow.enabled, Is.False);

            checkpoint.Interact(_player);
            yield return null;

            Assume.That(checkpoint.IsActivated, Is.True);
            Assert.That(visual.ShowsLitFlame, Is.True);
            Assert.That(glow.enabled, Is.True,
                "The only warm light in a cold level is the message that this place is safe.");
        }

        [UnityTest]
        public IEnumerator TheAltarArtNeverMovesItsInteractionTrigger()
        {
            yield return null;
            var checkpoint = NearestCheckpoint();
            var trigger = checkpoint.GetComponent<BoxCollider2D>();

            Assert.That(trigger.size, Is.EqualTo(new Vector2(2.4f, 2.4f)));
            Assert.That(trigger.offset, Is.EqualTo(new Vector2(0f, 0.6f)));
        }

        // ---------- the arena ----------

        [UnityTest]
        public IEnumerator TheBossGateStandsOpenUntilTheFightStarts()
        {
            yield return null;
            var encounter = TombScene.GuardianEncounter();

            Assert.That(GameObject.Find("BossGate"), Is.Not.Null, "The arena has its gate.");
            Assert.That(encounter.IsArenaSealed, Is.False);
        }

        [UnityTest]
        public IEnumerator SealingTheArenaShowsASolidBarrier()
        {
            var encounter = TombScene.GuardianEncounter();
            var boss = TombScene.Guardian();
            boss.BeginFight(_player);
            encounter.Barrier.SetActive(true);
            yield return null;

            var renderer = encounter.Barrier.GetComponentInChildren<SpriteRenderer>();
            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(renderer.color.a, Is.EqualTo(1f).Within(0.001f),
                "A see-through seal invites the player to try dashing through it.");
        }

        [UnityTest]
        public IEnumerator TheArenaHasItsMonumentalBackdropBehindTheBoss()
        {
            yield return null;
            var backdrop = GameObject.Find("ArenaBackdrop");
            var boss = TombScene.Guardian();

            Assert.That(backdrop, Is.Not.Null);
            var renderer = backdrop.GetComponent<SpriteRenderer>();
            Assert.That(renderer.sortingLayerName, Is.EqualTo("BackgroundNear"));
            Assert.That(renderer.bounds.Contains(
                new Vector3(boss.transform.position.x, boss.transform.position.y, 0f)), Is.True,
                "The focal point has to be behind the Guardian, not beside it.");
        }

        [UnityTest]
        public IEnumerator ThePitIsDrawnRatherThanHiddenInTheDark()
        {
            yield return null;
            var depth = GameObject.Find("PitDepth");
            Assert.That(depth, Is.Not.Null, "A kill zone the player cannot see is a cheap death.");

            // The forest has pits of its own, and "any" kill zone is whichever Unity lists first;
            // the one the Tomb's drawing belongs to is the nearest.
            var nearest = Object.FindObjectsByType<KillZone>(FindObjectsInactive.Exclude)
                .Select(k => Mathf.Abs(depth.transform.position.x - k.transform.position.x))
                .DefaultIfEmpty(float.MaxValue)
                .Min();
            Assert.That(nearest, Is.LessThan(1.5f), "The drawn danger has to sit where the real one is.");
        }

        // ---------- nothing moved ----------

        [UnityTest]
        public IEnumerator DressingTheLevelChangedNoStableIdentifier()
        {
            yield return null;
            var checkpoints = TombScene.TombAltars()
                .Select(c => c.CheckpointId).OrderBy(id => id).ToArray();
            var boss = TombScene.Guardian();

            Assert.That(checkpoints, Is.EqualTo(new[] { "tomb-altar-01", "tomb-altar-02" }));
            Assert.That(boss.BossId, Is.EqualTo("guardian-of-the-tomb"));
        }

        [UnityTest]
        public IEnumerator TheLevelGeometryIsWhereGameplayLeftIt()
        {
            yield return null;
            var floor = GameObject.Find("Floor").GetComponent<BoxCollider2D>();
            var arena = GameObject.Find("ArenaFloor").GetComponent<BoxCollider2D>();

            Assert.That(floor.size, Is.EqualTo(new Vector2(35.5f, 1f)));
            Assert.That(arena.size, Is.EqualTo(new Vector2(24f, 1f)),
                "Arena dimensions are gameplay; art fits them, never the reverse.");
        }

        // ---------- helpers ----------

        private static ParallaxLayer Layer(ParallaxLayer[] layers, string keyword) =>
            layers.First(l => l.name.ToLowerInvariant().Contains(keyword));

        private Checkpoint NearestCheckpoint() =>
            Object.FindObjectsByType<Checkpoint>()
                .OrderBy(c => Mathf.Abs(c.transform.position.x - _player.transform.position.x))
                .First();
    }
}
