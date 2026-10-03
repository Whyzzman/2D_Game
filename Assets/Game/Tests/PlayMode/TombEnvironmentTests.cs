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
            yield return TestWorld.Load();
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
            var renderers = Dressings().SelectMany(d => d.GetComponentsInChildren<SpriteRenderer>()).ToArray();
            Assert.That(renderers.Length, Is.GreaterThan(100),
                "A handful of props is still a greybox with decoration on it.");
        }

        [UnityTest]
        public IEnumerator EveryRoomOfTheTombsTextIsBuiltAndDressed()
        {
            yield return null;
            foreach (var room in TombScene.Layout.Rooms)
            {
                var built = GameObject.Find(room.Id);
                Assert.That(built, Is.Not.Null, $"{room.Id} is missing.");
                Assert.That(built.transform.Find("Dressing"), Is.Not.Null, $"{room.Id} has no dressing.");
            }
        }

        [UnityTest]
        public IEnumerator EveryRoomHasItsOwnBackdropInItsOwnTone()
        {
            // The user's rule (2026-10-03): no picture repeats from room to room so the eye catches it.
            yield return null;
            var tones = new System.Collections.Generic.Dictionary<Color, string>();
            foreach (var dressing in Dressings())
            {
                var backdrop = dressing.Find("Backdrop")?.GetComponent<SpriteRenderer>();
                Assert.That(backdrop, Is.Not.Null, $"{dressing.parent.name} has no backdrop.");
                Assert.That(backdrop.sortingLayerName, Is.EqualTo("BackgroundMid"));
                Assert.That(tones.ContainsKey(backdrop.color), Is.False,
                    $"{dressing.parent.name} wears the same tone as {(tones.TryGetValue(backdrop.color, out var other) ? other : "")}.");
                tones[backdrop.color] = dressing.parent.name;
            }
        }

        [UnityTest]
        public IEnumerator DecorationNeverCarriesCollision()
        {
            // The rule the whole phase rests on: a decorative ledge the player can stand on is a
            // worse bug than no decoration at all.
            yield return null;
            var colliders = Dressings().SelectMany(d => d.GetComponentsInChildren<Collider2D>()).ToArray();

            Assert.That(colliders, Is.Empty,
                "Dressing must not add collision: " + string.Join(", ",
                    colliders.Select(c => c.name)));
        }

        [UnityTest]
        public IEnumerator TheGameplayFloorDrawsInFrontOfItsBackground()
        {
            yield return null;
            var floor = GameObject.Find("room_tomb_entrance").GetComponentsInChildren<BoxCollider2D>()
                .First(b => b.name.StartsWith("Solid_")).GetComponentInChildren<SpriteRenderer>();
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

        // ---------- parallax (the Forest's and the Capital's; the Tomb has a backdrop per room) ----------

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

        // ---------- nothing moved ----------

        [UnityTest]
        public IEnumerator DressingTheLevelChangedNoStableIdentifier()
        {
            yield return null;
            var checkpoints = TombScene.TombAltars()
                .Select(c => c.CheckpointId).OrderBy(id => id).ToArray();
            var boss = TombScene.Guardian();

            Assert.That(checkpoints, Is.EqualTo(new[] { "tomb-altar-01", "tomb-altar-02", "tomb-altar-03", "tomb-altar-04" }),
                "The two old altars keep their ids; the map's two more are new.");
            Assert.That(boss.BossId, Is.EqualTo("guardian-of-the-tomb"));
        }

        [UnityTest]
        public IEnumerator TheGuardiansFloorIsAsTheTextHasIt()
        {
            yield return null;
            var arena = TombScene.Layout.WorldBounds(TombScene.Layout.Room("room_tomb_guardian_arena"));
            var floor = Object.FindObjectsByType<BoxCollider2D>()
                .Where(b => !b.isTrigger && b.gameObject.layer == 9 && arena.Contains(b.bounds.center)
                    && b.bounds.max.y < arena.center.y)
                .OrderByDescending(b => b.bounds.size.x).First();
            Assert.That(floor.bounds.size.x, Is.GreaterThanOrEqualTo(24f),
                "Arena dimensions are gameplay; art fits them, never the reverse.");
            Assert.That(floor.bounds.max.y, Is.EqualTo(TombScene.Spot("guardian").y).Within(0.01f));
        }

        // ---------- helpers ----------

        private static Transform[] Dressings() =>
            TombScene.Layout.Rooms.Select(r => GameObject.Find(r.Id)?.transform.Find("Dressing"))
                .Where(d => d != null).ToArray();

        private static ParallaxLayer Layer(ParallaxLayer[] layers, string keyword) =>
            layers.First(l => l.name.ToLowerInvariant().Contains(keyword));

        private Checkpoint NearestCheckpoint() =>
            Object.FindObjectsByType<Checkpoint>()
                .OrderBy(c => Mathf.Abs(c.transform.position.x - _player.transform.position.x))
                .First();
    }
}
