using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The world in the real scene: the graph agrees with the level, discovery and the map follow
    /// the player, fast travel keeps every rule, shortcuts and doors stay how they were left, and
    /// the obvious ways of skipping a gate do not work.
    /// </summary>
    public sealed class WorldTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private PlayerRespawner _respawner;
        private GameProgress _progress;
        private WorldState _world;
        private FastTravelService _travel;
        private ShrineMenu _shrine;
        private WorldMapScreen _map;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return LoadLevel();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
            GamePause.Clear();
            GameTimeScale.Reset();
        }

        // ---------- the graph and the level agree ----------

        [UnityTest]
        public IEnumerator TheWorldHasOneOfEachServiceAndAGraph()
        {
            yield return null;
            Assert.That(Object.FindObjectsByType<WorldState>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<FastTravelService>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<ShrineMenu>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<WorldMapScreen>().Length, Is.EqualTo(1));
            Assert.That(_progress.World, Is.Not.Null);
            Assert.That(WorldGraphValidator.Validate(_progress.World), Is.Empty);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EveryAltarInTheLevelIsInTheDatabaseAndInsideItsArea()
        {
            yield return TestWorld.LoadEveryZone();
            var graph = _progress.World;
            var altars = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include);
            Assert.That(altars.Select(a => a.CheckpointId),
                Is.EquivalentTo(graph.Checkpoints.Select(c => c.CheckpointId)),
                "The travel list is generated from the database; the level must match it.");

            foreach (var altar in altars)
            {
                var definition = graph.Checkpoint(altar.CheckpointId);
                var area = graph.Area(definition.AreaId);
                Assert.That(area.WorldBounds.Contains(altar.RespawnPosition), Is.True,
                    $"{altar.CheckpointId} stands outside {area.AreaId}.");
                Assert.That(altar.DisplayName, Is.EqualTo(definition.DisplayName));
            }
        }

        [UnityTest]
        public IEnumerator EveryTriggerShortcutAndMarkerNamesSomethingInTheGraph()
        {
            // Every zone's pieces, the ones of zones not loaded around the Tomb included.
            yield return TestWorld.LoadEveryZone();
            var graph = _progress.World;
            foreach (var trigger in Object.FindObjectsByType<AreaDiscoveryTrigger>())
            {
                var area = graph.Area(trigger.AreaId);
                Assert.That(area, Is.Not.Null, trigger.name);
                Assert.That(area.DiscoveryRule, Is.EqualTo(AreaDiscoveryRule.OnTrigger));
            }

            var shortcutIds = graph.Connections.Where(c => c.IsShortcut)
                .Select(c => c.PersistentUnlockId).ToArray();
            var shortcuts = Object.FindObjectsByType<Shortcut>();
            Assert.That(shortcuts.Select(s => s.ShortcutId), Is.EquivalentTo(shortcutIds));

            foreach (var gate in Object.FindObjectsByType<AbilityGate>())
            {
                var connection = graph.Connection(gate.ConnectionId);
                Assert.That(connection, Is.Not.Null, gate.name);
                Assert.That(connection.RequiredAbilityId, Is.EqualTo(gate.RequiredAbilityId));
            }
        }

        // ---------- discovery and the map ----------

        [UnityTest]
        public IEnumerator ANewGameKnowsOnlyItsFirstRoom()
        {
            yield return new WaitForSeconds(0.3f);
            Assert.That(_world.CurrentAreaId, Is.EqualTo("room_tomb_entrance"));
            Assert.That(_world.CurrentZoneId, Is.EqualTo("zone_forgotten_tomb"));
            Assert.That(_progress.DiscoveredAreas, Is.EquivalentTo(new[] { "room_tomb_entrance" }));

            _map.Open();
            Assert.That(_map.IsOpen, Is.True);
            Assert.That(GamePause.IsPaused, Is.True, "The map pauses, as the inventory does.");
            Assert.That(_map.View.Nodes.Count(n => n.State == MapNodeState.Discovered), Is.EqualTo(1));
            Assert.That(_map.View.Node("room_tomb_entrance").IsCurrent, Is.True);
            Assert.That(_map.SelectedAreaId, Is.EqualTo("room_tomb_entrance"));
            _map.Close();
            Assert.That(GamePause.IsPaused, Is.False);
        }

        [UnityTest]
        public IEnumerator EnteringARoomDiscoversItOnceHoweverOftenItIsEntered()
        {
            var discoveries = 0;
            _world.Changed += (flag, id) =>
            {
                if (flag == WorldFlag.AreaDiscovered && id == "room_tomb_burial_hall") { discoveries++; }
            };

            for (var i = 0; i < 4; i++)
            {
                yield return Place(BurialHall());
                Assert.That(_world.CurrentAreaId, Is.EqualTo("room_tomb_burial_hall"));
                yield return Place(TombScene.Ground(0f));
            }

            Assert.That(discoveries, Is.EqualTo(1));
            Assert.That(_progress.IsAreaDiscovered("room_tomb_burial_hall"), Is.True);
        }

        [UnityTest]
        public IEnumerator ASecretIsFoundOnlyByGoingInside()
        {
            // Standing on the Guardian's floor by the wall the ossuary hides behind is not being in it.
            yield return Place(TombScene.ArenaFloor(6.5f));
            Assert.That(_progress.IsAreaDiscovered("room_tomb_royal_ossuary"), Is.False);

            var ossuary = TombScene.Layout.WorldBounds(TombScene.Layout.Room("room_tomb_royal_ossuary"));
            yield return Place(new Vector2(ossuary.center.x, ossuary.yMin + 1.85f));
            Assert.That(_progress.IsAreaDiscovered("room_tomb_royal_ossuary"), Is.True);
            Assert.That(_world.CurrentAreaId, Is.EqualTo("room_tomb_royal_ossuary"));
        }

        [UnityTest]
        public IEnumerator TheMapRedrawsOnlyWhenTheWorldChanges()
        {
            yield return null;
            _progress.MarkAreaDiscovered("room_tomb_burial_hall");
            _map.Open();
            var builds = _map.BuildCount;
            yield return null;
            yield return null;
            Assert.That(_map.BuildCount, Is.EqualTo(builds), "Nothing changed, nothing rebuilt.");

            _progress.MarkCheckpointActivated("tomb-altar-01");
            yield return null;
            Assert.That(_map.BuildCount, Is.EqualTo(builds + 1));
            Assert.That(_map.View.Node("room_tomb_burial_hall").Icons
                .Single(i => i.Id == "tomb-altar-01").IsActive, Is.True);
            _map.Close();
            Assert.That(_map.View, Is.Null, "A closed map keeps nothing built.");
        }

        [UnityTest]
        public IEnumerator OpeningTheMapFiftyTimesLeavesNothingBehind()
        {
            yield return null;
            for (var i = 0; i < 50; i++)
            {
                _map.Toggle();
                _map.Toggle();
            }

            yield return null;
            Assert.That(_map.IsOpen, Is.False);
            Assert.That(GamePause.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheMapCannotOpenOverATransitionOrADeath()
        {
            _respawner.Kill();
            yield return null;
            _map.Open();
            Assert.That(_map.IsOpen, Is.False, "A dead player has no map.");
            yield return new WaitForSeconds(3f);
            Assert.That(_player.IsAlive, Is.True);
        }

        [UnityTest]
        public IEnumerator DiscoveredRoomsSurviveAQuit()
        {
            yield return Place(BurialHall());
            yield return Place(TombScene.Spot("stone-wardens-key") + new Vector2(2f, 0.85f));
            _progress.SaveNow();

            yield return LoadLevel();

            Assert.That(_progress.IsAreaDiscovered("room_tomb_burial_hall"), Is.True);
            Assert.That(_progress.IsAreaDiscovered("room_tomb_guard_passage"), Is.True);
            Assert.That(_progress.IsAreaDiscovered("room_forest_entrance"), Is.False);
        }

        // ---------- crossing into the forest ----------

        [UnityTest]
        public IEnumerator TheTombDoorOpensWithTheGuardianAndTheForestTakesOverTheSound()
        {
            var door = Object.FindObjectsByType<PersistentDoor>()
                .Single(d => d.name == "TombExitGate");
            Assert.That(door.IsOpen, Is.False);

            var guardian = TombScene.GuardianEncounter();
            yield return Place(TombScene.ArenaEntry());
            guardian.Boss.Actor.ClearInvulnerability();
            guardian.Boss.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.left,
                isUnblockable: true, attacker: _player.gameObject));
            yield return null;
            Assert.That(door.IsOpen, Is.True, "The door opens in the same event the boss dies.");

            var titles = 0;
            _world.ZoneTitleRequested += _ => titles++;
            yield return new WaitForSeconds(1.5f);
            yield return Place(new Vector2(70f, 0.85f));
            Assert.That(_world.CurrentZoneId, Is.EqualTo("zone_withered_forest"));
            Assert.That(MusicDirector.Instance.ZoneId, Is.EqualTo("zone_withered_forest"));
            Assert.That(Object.FindAnyObjectByType<AmbienceController>().ZoneId,
                Is.EqualTo("zone_withered_forest"));
            Assert.That(titles, Is.EqualTo(1));

            yield return Place(new Vector2(55f, 0.85f));
            Assert.That(_world.CurrentZoneId, Is.EqualTo("zone_forgotten_tomb"));
            Assert.That(MusicDirector.Instance.ZoneId, Is.EqualTo("zone_forgotten_tomb"));
            yield return Place(new Vector2(70f, 0.85f));
            Assert.That(titles, Is.EqualTo(1), "The title card plays on the first crossing only.");
        }

        [UnityTest]
        public IEnumerator CrossingIntoTheForestSweepsFogInTheForestsColour()
        {
            var hud = Object.FindAnyObjectByType<HudController>();
            var graph = _progress.World;
            Assert.That(graph.Zone("zone_withered_forest").FogTint,
                Is.Not.EqualTo(graph.Zone("zone_forgotten_tomb").FogTint), "Each region has its own fog.");
            yield return Place(new Vector2(55f, 0.85f));
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(_world.CurrentZoneId, Is.EqualTo("zone_forgotten_tomb"));
            Assert.That(hud.ZoneCurtain.IsPlaying, Is.False, "Standing in the first zone plays nothing.");

            yield return Place(new Vector2(70f, 0.85f));
            Assert.That(_world.CurrentZoneId, Is.EqualTo("zone_withered_forest"));
            Assert.That(hud.ZoneCurtain.IsPlaying, Is.True, "The border is marked by the fog.");

            var deadline = Time.realtimeSinceStartup + 3f;
            while (hud.ZoneCurtain.IsPlaying && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(hud.ZoneCurtain.IsPlaying, Is.False, "It passes; nothing waits on it.");
        }

        [UnityTest]
        public IEnumerator WithReducedMotionTheCurtainIsAPlainFade()
        {
            var hud = Object.FindAnyObjectByType<HudController>();
            FeedbackSettings.ReducedMotion = true;
            try
            {
                yield return Place(new Vector2(55f, 0.85f));
                yield return Place(new Vector2(70f, 0.85f));
                Assert.That(hud.ZoneCurtain.IsPlaying, Is.True);
                var front = hud.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement
                    .Q("zone-curtain-front");
                Assert.That(front.style.translate.value.x.value, Is.EqualTo(0f).Within(0.01f), "Nothing slides.");
            }
            finally
            {
                FeedbackSettings.ResetToDefaults();
            }
        }

        [UnityTest]
        public IEnumerator TheForestsCardRisesWithItsThemeNotBefore()
        {
            var hud = Object.FindAnyObjectByType<HudController>();
            yield return Place(new Vector2(55f, 0.85f));
            yield return Place(new Vector2(70f, 0.85f));
            var entered = Time.realtimeSinceStartup;
            var music = MusicDirector.Instance;
            while (!hud.ZoneTitleText.Contains("ЛІС") && Time.realtimeSinceStartup - entered < 4f)
            {
                yield return null;
            }

            StringAssert.Contains("ЛІС", hud.ZoneTitleText, "The card comes, music or not.");
            Assert.That(hud.ZoneTitleIsShowing, Is.True);
            Assert.That(hud.ZoneTitleIsShort, Is.False, "The first visit is the full card.");
            var waited = Time.realtimeSinceStartup - entered;
            Assert.That(music == null || music.ThemeArrival("zone_withered_forest") >= 0.3f || waited >= 2.4f,
                Is.True, "It rose with the forest's theme, or waited no longer than its limit.");
        }

        [UnityTest]
        public IEnumerator ASavedBeatenGuardianOpensTheDoorBeforeTheFirstFrame()
        {
            _progress.MarkBossDefeated(TombScene.GuardianId);
            _progress.SaveNow();
            yield return LoadLevel();

            var door = Object.FindObjectsByType<PersistentDoor>()
                .Single(d => d.name == "TombExitGate");
            Assert.That(door.IsOpen, Is.True);
            Assert.That(GameObject.Find("TombExitWall"), Is.Null, "The wall is gone, not just open.");
        }

        // ---------- fast travel ----------

        [UnityTest]
        public IEnumerator TravelIsLockedUntilTheGuardianFalls()
        {
            yield return LightAll("tomb-altar-01", "tomb-altar-02");
            yield return StandAt("tomb-altar-01");

            Assert.That(_travel.IsUnlocked, Is.False);
            Assert.That(_travel.Check("tomb-altar-01", "tomb-altar-02"), Is.EqualTo(TravelResult.Locked));

            _progress.MarkBossDefeated(TombScene.GuardianId);
            Assert.That(_travel.Check("tomb-altar-01", "tomb-altar-02"), Is.EqualTo(TravelResult.Started));
        }

        [UnityTest]
        public IEnumerator OnlyLitAltarsAreDestinations()
        {
            yield return LightAll("tomb-altar-01", "tomb-altar-02");
            _progress.MarkBossDefeated(TombScene.GuardianId);
            yield return StandAt("tomb-altar-01");

            var list = _travel.Destinations("tomb-altar-01");
            Assert.That(list.Select(d => d.CheckpointId),
                Is.EquivalentTo(new[] { "tomb-altar-01", "tomb-altar-02" }));
            Assert.That(list.Single(d => d.CheckpointId == "tomb-altar-01").IsCurrent, Is.True);
            Assert.That(list.Single(d => d.CheckpointId == "tomb-altar-01").IsSelectable, Is.False);

            Assert.That(_travel.TryTravel("tomb-altar-01", "warden-shrine"),
                Is.EqualTo(TravelResult.NotActivated), "Never an altar the player did not light.");
            Assert.That(_travel.TryTravel("tomb-altar-01", "castle-gate"),
                Is.EqualTo(TravelResult.UnknownDestination));
            Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo("tomb-altar-01"));
        }

        [UnityTest]
        public IEnumerator TravelStartsOnlyFromAnAltarAndNeverMidFight()
        {
            yield return LightAll("tomb-altar-01", "tomb-altar-02");
            _progress.MarkBossDefeated(TombScene.GuardianId);

            yield return Place(new Vector2(0f, 0.85f));
            Assert.That(_travel.Check("tomb-altar-01", "tomb-altar-02"),
                Is.EqualTo(TravelResult.NotAtShrine));

            yield return StandAt("tomb-altar-01");
            _player.GetComponent<PlayerCombatStatus>().Report();
            Assert.That(_travel.Check("tomb-altar-01", "tomb-altar-02"),
                Is.EqualTo(TravelResult.InCombat));
            _player.GetComponent<PlayerCombatStatus>().Clear();

            _respawner.Kill();
            yield return null;
            Assert.That(_travel.Check("tomb-altar-01", "tomb-altar-02"),
                Is.EqualTo(TravelResult.PlayerDead));
        }

        [UnityTest]
        public IEnumerator NoTravelOutOfABossFight()
        {
            yield return LightAll("tomb-altar-01", "tomb-altar-02", "forest-entrance",
                "warden-shrine");
            _progress.MarkBossDefeated(TombScene.GuardianId);
            yield return StandAt("warden-shrine");

            var warden = TombScene.WardenEncounter();
            // Where the Warden's fight begins, wherever the Forest's text puts his arena.
            yield return Place((Vector2)warden.transform.position + new Vector2(0f, -1f));
            var until = Time.realtimeSinceStartup + 4f;
            while (!warden.IsFightActive && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            Assert.That(warden.IsFightActive, Is.True, "In his arena, the fight begins.");
            yield return StandAt("warden-shrine");

            Assert.That(_travel.Check("warden-shrine", "tomb-altar-01"),
                Is.EqualTo(TravelResult.BossFight));
        }

        [UnityTest]
        public IEnumerator TravellingToTheForestMovesEverythingThatShouldMoveAndNothingElse()
        {
            yield return LightAll("tomb-altar-01", "tomb-altar-02", "forest-entrance",
                "royal-road-shrine");
            _progress.MarkBossDefeated(TombScene.GuardianId);
            yield return StandAt("tomb-altar-01");
            _player.Health.Remove(40f);

            Assert.That(_travel.TryTravel("tomb-altar-01", "royal-road-shrine"),
                Is.EqualTo(TravelResult.Started));
            yield return WaitForTravel();

            var shrine = _respawner.FindCheckpoint("royal-road-shrine");
            Assert.That(_player.transform.position.x, Is.EqualTo(shrine.RespawnPosition.x).Within(0.6f));
            Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo("royal-road-shrine"));
            Assert.That(SaveSystem.Read().checkpointId, Is.EqualTo("royal-road-shrine"),
                "The destination is the new last checkpoint, and it is on disk.");
            Assert.That(_player.Health.Current, Is.EqualTo(_player.Health.Max), "Travel rests.");
            Assert.That(_world.CurrentZoneId, Is.EqualTo("zone_withered_forest"));
            Assert.That(MusicDirector.Instance.ZoneId, Is.EqualTo("zone_withered_forest"));
            // The view may stop at the room's edge short of the player; it must not trail behind.
            var follow = Camera.main.GetComponent<CameraFollow2D>();
            Assert.That(Camera.main.transform.position.x,
                Is.EqualTo(follow.TargetPosition.x).Within(0.2f), "No camera lag behind a jump.");
            Assert.That(follow.ActiveBounds.HasValue && follow.ActiveBounds.Value.Contains(
                (Vector2)_player.transform.position), Is.True, "The view is on the destination's room.");
            Assert.That(Object.FindObjectsByType<PlayerCharacter>().Length, Is.EqualTo(1));
            Assert.That(_player.GetComponent<PlayerInputReader>(), Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TravelWorksBackToTheTombAndWithinAZone()
        {
            yield return LightAll("tomb-altar-01", "tomb-altar-02", "forest-entrance",
                "deep-forest-shrine");
            _progress.MarkBossDefeated(TombScene.GuardianId);
            yield return StandAt("deep-forest-shrine");

            Assert.That(_travel.TryTravel("deep-forest-shrine", "forest-entrance"),
                Is.EqualTo(TravelResult.Started));
            yield return WaitForTravel();
            Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo("forest-entrance"));

            yield return StandAt("forest-entrance");
            Assert.That(_travel.TryTravel("forest-entrance", "tomb-altar-02"),
                Is.EqualTo(TravelResult.Started));
            yield return WaitForTravel();
            Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo("tomb-altar-02"));
            Assert.That(_world.CurrentZoneId, Is.EqualTo("zone_forgotten_tomb"));
            Assert.That(Object.FindAnyObjectByType<AmbienceController>().ZoneId,
                Is.EqualTo("zone_forgotten_tomb"), "No forest ambience left in the crypt.");
        }

        [UnityTest]
        public IEnumerator TwentyJourneysLeaveOneOfEverything()
        {
            yield return LightAll("tomb-altar-01", "forest-entrance");
            _progress.MarkBossDefeated(TombScene.GuardianId);
            var from = "tomb-altar-01";
            var to = "forest-entrance";
            GameTimeScale.SetBaseScale(4f);
            for (var i = 0; i < 20; i++)
            {
                yield return StandAt(from);
                Assert.That(_travel.TryTravel(from, to), Is.EqualTo(TravelResult.Started), $"trip {i}");
                yield return WaitForTravel();
                (from, to) = (to, from);
            }

            GameTimeScale.SetBaseScale(1f);
            Assert.That(Object.FindObjectsByType<PlayerCharacter>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioManager>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<MusicDirector>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<GameProgress>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<WorldState>().Length, Is.EqualTo(1));
            Assert.That(AudioManager.Instance.LoopingVoices, Is.LessThanOrEqualTo(3),
                "Ambience beds do not pile up with every crossing.");
            Assert.That(_respawner.IsSequenceRunning, Is.False);
            Assert.That(GamePause.IsPaused, Is.False);
        }

        [UnityTest]
        public IEnumerator ADestinationThatVanishesMidFadeChangesNothing()
        {
            yield return LightAll("tomb-altar-01", "tomb-altar-02");
            _progress.MarkBossDefeated(TombScene.GuardianId);
            yield return StandAt("tomb-altar-01");
            _progress.SaveNow();
            var before = _player.transform.position;

            Assert.That(_travel.TryTravel("tomb-altar-01", "tomb-altar-02"),
                Is.EqualTo(TravelResult.Started));
            _respawner.FindCheckpoint("tomb-altar-02").gameObject.SetActive(false);
            yield return WaitForTravel();

            Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo("tomb-altar-01"),
                "A failed journey never overwrites the last checkpoint.");
            Assert.That(SaveSystem.Read().checkpointId, Is.EqualTo("tomb-altar-01"));
            Assert.That(_player.transform.position.x, Is.EqualTo(before.x).Within(0.3f));
            Assert.That(_player.IsAlive, Is.True);
            Assert.That(_travel.LastMessage, Is.Not.Empty, "The player is told it did not work.");
        }

        [UnityTest]
        public IEnumerator NothingCanKillThePlayerBetweenTwoPlaces()
        {
            yield return LightAll("tomb-altar-01", "tomb-altar-02");
            _progress.MarkBossDefeated(TombScene.GuardianId);
            yield return StandAt("tomb-altar-01");
            _travel.TryTravel("tomb-altar-01", "tomb-altar-02");
            yield return null;

            _player.Actor.ApplyDamage(new DamageInfo(9999f, Vector2.right));
            Assert.That(_player.IsAlive, Is.True);
            yield return WaitForTravel();
            Assert.That(_player.IsAlive, Is.True);
            Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo("tomb-altar-02"));
        }

        [UnityTest]
        public IEnumerator TheAltarMenuRestsOrTravelsWhenSomethingPresentsIt()
        {
            yield return LightAll("tomb-altar-01", "tomb-altar-02");
            _progress.MarkBossDefeated(TombScene.GuardianId);
            yield return StandAt("tomb-altar-01");
            var altar = _respawner.FindCheckpoint("tomb-altar-01");

            // Nothing presents it: interacting rests at once, as it always did.
            var hud = Object.FindAnyObjectByType<HudController>();
            hud.enabled = false;
            Assert.That(_shrine.HasPresenter, Is.False);
            altar.Interact(_player);
            Assert.That(_shrine.IsOpen, Is.False);
            yield return new WaitForSeconds(2f);

            // The HUD draws it: the altar opens its menu.
            hud.enabled = true;
            altar.Interact(_player);
            Assert.That(_shrine.IsOpen, Is.True);
            Assert.That(GamePause.IsPaused, Is.True);
            Assert.That(_shrine.CanOfferTravel, Is.True);

            Assert.That(_shrine.TravelTo("tomb-altar-02"), Is.EqualTo(TravelResult.Started));
            Assert.That(_shrine.IsOpen, Is.False);
            Assert.That(GamePause.IsPaused, Is.False);
            yield return WaitForTravel();
            Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo("tomb-altar-02"));
        }

        [UnityTest]
        public IEnumerator TheAltarMenuDrawnByTheHudLeadsToTravelAndBack()
        {
            var view = Object.FindAnyObjectByType<HudController>().Shrine;
            yield return LightAll("tomb-altar-01", "tomb-altar-02");
            yield return StandAt("tomb-altar-01");
            var altar = _respawner.FindCheckpoint("tomb-altar-01");

            // Before the Guardian: no travel entry at all.
            altar.Interact(_player);
            Assert.That(view.IsOpen, Is.True);
            Assert.That(view.VisibleItems, Has.None.EqualTo("shrine-travel"));

            // The character's screens from the altar come back to it.
            view.OpenWindow(InventoryView.Page.Character);
            Assert.That(_shrine.IsOpen, Is.False);
            var inventory = Object.FindAnyObjectByType<InventoryScreen>();
            Assert.That(inventory.IsOpen, Is.True);
            inventory.Close();
            Assert.That(_shrine.IsOpen, Is.True, "Closing the window returns to the altar.");
            view.Back();
            Assert.That(_shrine.IsOpen, Is.False);
            Assert.That(GamePause.IsPaused, Is.False);

            _progress.MarkBossDefeated(TombScene.GuardianId);
            altar.Interact(_player);
            Assert.That(view.VisibleItems, Does.Contain("shrine-travel"));
            view.ShowTravel();
            Assert.That(view.IsTravelShown, Is.True);
            var index = -1;
            for (var i = 0; i < view.Destinations.Count; i++)
            {
                if (view.Destinations[i].CheckpointId == "tomb-altar-02") { index = i; }
            }

            Assert.That(index, Is.GreaterThanOrEqualTo(0), "Every lit altar is listed.");
            view.Back();
            Assert.That(view.IsTravelShown, Is.False, "Back from the list is the menu, not away.");
            view.ShowTravel();
            Assert.That(view.TravelTo(index), Is.EqualTo(TravelResult.Started));
            Assert.That(view.IsOpen, Is.False);
            yield return WaitForTravel();
            Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo("tomb-altar-02"));
        }

        [UnityTest]
        public IEnumerator ThePauseOpensTheMapAndTheMapShowsWhereThePlayerIs()
        {
            var hud = Object.FindAnyObjectByType<HudController>();
            yield return null;
            hud.Pause.Open();
            hud.Pause.OpenMap();
            Assert.That(hud.Map.IsOpen, Is.True);
            Assert.That(_map.IsOpen, Is.True);
            yield return null;

            var root = hud.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
            Assert.That(_map.Level, Is.EqualTo(MapLevel.World), "The map opens on the world (2026-10-03).");
            Assert.That(_map.FocusZoneId, Is.EqualTo(_world.CurrentZoneId), "With the hero's region chosen.");
            Assert.That(_map.EnterZone(), Is.True);
            yield return null;
            var current = _map.View.Node(_map.SelectedAreaId);
            Assert.That(current, Is.Not.Null);
            Assert.That(current.IsCurrent, Is.True, "The cursor starts on the player.");
            Assert.That(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(root, "map-info-name").text, Is.EqualTo(current.Label));

            hud.Pause.Toggle(); // Escape: out of the region, to the world.
            Assert.That(_map.IsOpen, Is.True);
            Assert.That(_map.Level, Is.EqualTo(MapLevel.World));
            hud.Pause.Toggle(); // Escape again: the map closes.
            Assert.That(_map.IsOpen, Is.False);
            Assert.That(hud.Pause.IsOpen, Is.True, "Opened from the pause, the map returns there.");
            hud.Pause.Resume();
        }

        // ---------- shortcuts ----------

        [UnityTest]
        public IEnumerator TheChainGateStaysOpenThroughDeathAndReload()
        {
            // The Tomb's shortcut: bars across the door from the chain shaft into the entrance,
            // wound up from the shaft's side.
            var winch = Object.FindObjectsByType<Shortcut>()
                .Single(s => s.ShortcutId == "shortcut_tomb_chain_gate");
            var bars = GameObject.Find("ChainGate");
            Assert.That(bars, Is.Not.Null, "The bars stand in the door before the winch.");

            yield return Place(TombScene.Spot("chain-winch") + new Vector2(0f, 0.85f));
            winch.Interact(_player);
            Assert.That(_progress.IsShortcutUnlocked("shortcut_tomb_chain_gate"), Is.True);
            yield return new WaitForSeconds(1.5f);
            Assert.That(bars.activeSelf, Is.False, "The way through is open.");

            var again = 0;
            _world.Changed += (flag, _) => again += flag == WorldFlag.ShortcutUnlocked ? 1 : 0;
            winch.Interact(_player);
            Assert.That(again, Is.Zero, "A second pull changes nothing.");

            _respawner.Kill();
            yield return new WaitForSeconds(3f);
            Assert.That(bars.activeSelf, Is.False, "Death does not drop the bars.");

            yield return new WaitForSeconds(0.5f);
            yield return LoadLevel();
            var reloaded = Resources.FindObjectsOfTypeAll<GameObject>()
                .First(g => g.name == "ChainGate" && g.scene.IsValid());
            Assert.That(reloaded.activeSelf, Is.False, "Nor does quitting.");
        }

        [UnityTest]
        public IEnumerator TheRoadGateOpensFromTheEastAndTheMapFollows()
        {
            _progress.MarkAreaDiscovered("room_forest_gate_arch");
            _progress.MarkAreaDiscovered("room_forest_broken_arch");
            var bars = GameObject.Find("RoadGate");
            Assert.That(bars, Is.Not.Null);

            _map.Open();
            var before = _map.View.Links.Single(l => l.Connection.ConnectionId == "conn_forest_gate_arch_broken_arch");
            Assert.That(before.State, Is.EqualTo(MapLinkState.ShortcutLocked));
            _map.Close();

            var winch = Object.FindObjectsByType<Shortcut>()
                .Single(s => s.ShortcutId == "shortcut_forest_road_gate");
            winch.Interact(_player);
            Assert.That(bars.activeSelf, Is.False, "The bars leave the road at once.");

            _map.Open();
            var after = _map.View.Links.Single(l => l.Connection.ConnectionId == "conn_forest_gate_arch_broken_arch");
            Assert.That(after.State, Is.EqualTo(MapLinkState.Open));
            _map.Close();
            yield return null;
        }

        // ---------- old saves ----------

        [UnityTest]
        public IEnumerator APhaseTwentyFourForestSaveLoadsIntoTheForestWithAMap()
        {
            File.WriteAllText(SaveSystem.FilePath, "{\"version\":9,\"checkpointId\":\"royal-road-shrine\","
                + "\"zoneId\":\"zone_withered_forest\",\"respawnPosition\":{\"x\":115,\"y\":0.8},"
                + "\"activatedCheckpoints\":[\"tomb-altar-01\",\"tomb-altar-02\",\"forest-entrance\","
                + "\"royal-road-shrine\"],\"defeatedBosses\":[\"guardian-of-the-tomb\"]}");
            yield return LoadLevel();
            yield return new WaitForSeconds(0.3f);

            Assert.That(_respawner.ActiveCheckpointId, Is.EqualTo("royal-road-shrine"));
            Assert.That(_world.CurrentZoneId, Is.EqualTo("zone_withered_forest"));
            Assert.That(_progress.IsAreaDiscovered("room_forest_entrance"), Is.True);
            Assert.That(_progress.IsAreaDiscovered("room_tomb_royal_ossuary"), Is.False);
            Assert.That(_travel.IsUnlocked, Is.True, "Every lit altar is a destination at once.");
            Assert.That(Object.FindObjectsByType<PersistentDoor>()
                .Single(d => d.name == "TombExitGate").IsOpen, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        // ---------- ways around a gate that must not work ----------

        [UnityTest]
        public IEnumerator NoDashCarriesThePlayerThroughTheSealedTombDoor()
        {
            _progress.UnlockAbility(AbilityIds.AirDash);
            yield return Place(new Vector2(61f, 0.85f));
            _motor.TryBeginDash(1);
            yield return new WaitForSeconds(0.5f);
            Assert.That(_player.transform.position.x, Is.LessThan(62f));

            var shadow = _player.GetComponent<ShadowAbilities>();
            shadow.Begin(ShadowAction.Dash);
            yield return new WaitForSeconds(0.4f);
            shadow.Cancel();
            Assert.That(_player.transform.position.x, Is.LessThan(62f));
        }

        [UnityTest]
        public IEnumerator AShadowStrikeCannotReachAnEnemyBehindTheSealedTombDoor()
        {
            var zombie = TombScene.TombEnemies().First(e => e.name.StartsWith("ZombieSoldier"));
            zombie.GetComponent<EnemyMotor2D>().enabled = false;
            zombie.transform.position = new Vector2(64f, 0.9f);
            yield return Place(new Vector2(61.2f, 0.85f));
            _motor.ResetTo(new Vector2(61.2f, 0.85f));
            yield return new WaitForFixedUpdate();

            var shadow = _player.GetComponent<ShadowAbilities>();
            shadow.Begin(ShadowAction.Strike);
            shadow.StrikeDisplacement();
            shadow.Cancel();
            yield return new WaitForFixedUpdate();

            Assert.That(_player.transform.position.x, Is.LessThan(62f),
                "Sight and the path both stop at the door.");
        }

        // ---------- helpers ----------

        /// <summary>The burial hall's floor, east of its altar.</summary>
        private static Vector2 BurialHall() => TombScene.Spot("altar") + new Vector2(4f, 0.85f);

        private IEnumerator LoadLevel()
        {
            yield return TestWorld.Load();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _respawner = Object.FindAnyObjectByType<PlayerRespawner>();
            _progress = Object.FindAnyObjectByType<GameProgress>();
            _world = Object.FindAnyObjectByType<WorldState>();
            _travel = Object.FindAnyObjectByType<FastTravelService>();
            _shrine = Object.FindAnyObjectByType<ShrineMenu>();
            _map = Object.FindAnyObjectByType<WorldMapScreen>();
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator Place(Vector2 position)
        {
            _player.GetComponent<PlayerCombatStatus>().Clear();
            _motor.ResetTo(position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator StandAt(string checkpointId)
        {
            var altar = _respawner.FindCheckpoint(checkpointId);
            yield return Place(new Vector2(altar.transform.position.x, altar.transform.position.y + 0.1f));
        }

        private IEnumerator LightAll(params string[] ids)
        {
            foreach (var id in ids)
            {
                yield return StandAt(id);
                _respawner.FindCheckpoint(id).Interact(_player);
                yield return null;
            }

            yield return StandAt(ids[0]);
            _respawner.FindCheckpoint(ids[0]).Interact(_player);
            if (_shrine.IsOpen) { _shrine.Rest(); }
            yield return new WaitForSeconds(2f);
        }

        private IEnumerator WaitForTravel()
        {
            var deadline = Time.realtimeSinceStartup + 8f;
            yield return null;
            while ((_travel.IsTraveling || _respawner.IsSequenceRunning)
                   && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(_travel.IsTraveling, Is.False, "The journey has to finish.");
            yield return new WaitForFixedUpdate();
        }
    }
}
