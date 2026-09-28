using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The world as data: the shipped graph, the rules read from it, the map built from it and how
    /// an old save gains a map. No scene is loaded; everything here is decided by the graph and a
    /// save's contents, which is what makes it testable without playing.
    /// </summary>
    public sealed class WorldGraphDataTests
    {
        private const string GraphPath = "Assets/Game/Data/WorldGraph.asset";
        private const string Guardian = "guardian-of-the-tomb";
        private const string Warden = "hollow-warden";
        private const string King = "ashen-king";
        private WorldGraph _graph;

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Delete();
            _graph = UnityEditor.AssetDatabase.LoadAssetAtPath<WorldGraph>(GraphPath);
            Assert.That(_graph, Is.Not.Null, "The content builder writes the world graph.");
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
        }

        // ---------- the shipped graph ----------

        [Test]
        public void TheShippedGraphHasNoProblems()
        {
            var problems = WorldGraphValidator.Validate(_graph);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void IdsAreUniqueAndEveryReferenceResolves()
        {
            Assert.That(_graph.Areas.Select(a => a.AreaId), Is.Unique);
            Assert.That(_graph.Connections.Select(c => c.ConnectionId), Is.Unique);
            Assert.That(_graph.Checkpoints.Select(c => c.CheckpointId), Is.Unique);
            foreach (var connection in _graph.Connections)
            {
                Assert.That(_graph.Area(connection.FromAreaId), Is.Not.Null, connection.ConnectionId);
                Assert.That(_graph.Area(connection.ToAreaId), Is.Not.Null, connection.ConnectionId);
                if (!string.IsNullOrEmpty(connection.RequiredAbilityId))
                {
                    Assert.That(_graph.Ability(connection.RequiredAbilityId), Is.Not.Null,
                        $"{connection.ConnectionId} asks for an ability the world does not know.");
                }
            }

            foreach (var checkpoint in _graph.Checkpoints)
            {
                var area = _graph.Area(checkpoint.AreaId);
                Assert.That(area, Is.Not.Null, checkpoint.CheckpointId);
                Assert.That(area.ZoneId, Is.EqualTo(checkpoint.ZoneId), checkpoint.CheckpointId);
            }
        }

        [Test]
        public void TheIdsThatSavesAlreadyHoldAreUnchanged()
        {
            // Saves from PHASE 24 recorded these; renaming any of them would orphan a save.
            Assert.That(_graph.Zone("zone_forgotten_tomb"), Is.Not.Null);
            Assert.That(_graph.Zone("zone_withered_forest"), Is.Not.Null);
            foreach (var id in new[] { "tomb-altar-01", "tomb-altar-02", "forest-entrance",
                "royal-road-shrine", "deep-forest-shrine", "warden-shrine" })
            {
                Assert.That(_graph.Checkpoint(id), Is.Not.Null, id);
            }
        }

        [Test]
        public void EveryTwoWayConnectionResolvesFromBothEnds()
        {
            foreach (var connection in _graph.Connections)
            {
                Assert.That(_graph.ConnectionsOf(connection.FromAreaId), Does.Contain(connection));
                Assert.That(_graph.ConnectionsOf(connection.ToAreaId), Does.Contain(connection));
                Assert.That(connection.LeadsFrom(connection.FromAreaId), Is.True);
                if (connection.IsBidirectional)
                {
                    Assert.That(connection.LeadsFrom(connection.ToAreaId), Is.True,
                        $"{connection.ConnectionId} is two-way but only resolves one way.");
                }
            }
        }

        [Test]
        public void TheOnlyOneWayConnectionsAreTheDocumentedOnes()
        {
            var oneWay = _graph.Connections.Where(c => !c.IsBidirectional)
                .Select(c => c.ConnectionId).OrderBy(id => id).ToArray();
            Assert.That(oneWay, Is.EqualTo(new[]
            {
                "conn_forest_road_ledge",
                "conn_forest_watchtower_drop"
            }), "A new one-way connection must be added to WORLD_GRAPH.md and here.");
        }

        [Test]
        public void TheMainPathNeedsNoOptionalAbilityAndNoBuildChoice()
        {
            var reached = WorldReachability.Reachable(_graph, _graph.StartAreaId,
                WorldReachability.AbilityAssumption.GuaranteedOnly);
            foreach (var area in _graph.Areas.Where(a => !a.IsOptional))
            {
                var zone = _graph.Zone(area.ZoneId);
                if (zone.IsImplemented)
                {
                    Assert.That(reached, Does.Contain(area.AreaId),
                        $"{area.AreaId} is on the main path but needs more than guaranteed movement.");
                }
            }

            Assert.That(reached, Does.Contain("room_forest_warden_arena"));
            Assert.That(reached, Does.Contain("room_forest_far_gate"),
                "Beyond the Warden, with only what the Warden gives.");
        }

        [Test]
        public void ShadowDashIsACombatSkillAndNoGateAsksForIt()
        {
            Assert.That(_graph.KindOf(AbilityIds.ShadowDash), Is.EqualTo(AbilityKind.CombatSkill));
            Assert.That(_graph.KindOf(AbilityIds.DoubleJump),
                Is.EqualTo(AbilityKind.TraversalProgression));
            Assert.That(_graph.Ability(AbilityIds.DoubleJump).GuaranteedByBossId, Is.EqualTo(Warden));
            Assert.That(_graph.KindOf(AbilityIds.AirDash), Is.EqualTo(AbilityKind.OptionalTraversal));
            Assert.That(_graph.KindOf(AbilityIds.WallRun), Is.EqualTo(AbilityKind.OptionalTraversal));
            Assert.That(_graph.Connections.Any(c => c.RequiredAbilityId == AbilityIds.ShadowDash),
                Is.False);
        }

        [Test]
        public void EveryAbilityGateLeadsToAnOptionalPlace()
        {
            foreach (var connection in _graph.Connections.Where(c =>
                         !string.IsNullOrEmpty(c.RequiredAbilityId)))
            {
                Assert.That(_graph.Area(connection.ToAreaId).IsOptional, Is.True,
                    $"{connection.ConnectionId} gates something on the main path by ability.");
            }

            var gatesByAbility = _graph.Connections
                .Where(c => !string.IsNullOrEmpty(c.RequiredAbilityId))
                .GroupBy(c => c.RequiredAbilityId)
                .ToDictionary(g => g.Key, g => g.Count());
            Assert.That(gatesByAbility.Values.Sum(), Is.InRange(4, 12));
            Assert.That(gatesByAbility.ContainsKey(AbilityIds.DoubleJump), Is.True);
            Assert.That(gatesByAbility.ContainsKey(AbilityIds.AirDash), Is.True);
            Assert.That(gatesByAbility.ContainsKey(AbilityIds.WallRun), Is.True);
            Assert.That(_graph.Connections.Any(c => c.RequiredAbilityId == AbilityIds.DoubleJump
                && _graph.Area(c.ToAreaId).ZoneId == "zone_forgotten_tomb"), Is.True,
                "The second jump has something to find back in the Tomb.");
        }

        [Test]
        public void EachZoneHasAPersistentShortcutOpenedFromItsFarSide()
        {
            var shortcuts = _graph.Connections.Where(c => c.IsShortcut).ToArray();
            foreach (var zone in new[] { "zone_forgotten_tomb", "zone_withered_forest" })
            {
                Assert.That(shortcuts.Any(c => _graph.Area(c.FromAreaId).ZoneId == zone), Is.True,
                    zone);
            }

            foreach (var shortcut in shortcuts)
            {
                Assert.That(shortcut.PersistentUnlockId, Is.Not.Empty);
                Assert.That(shortcut.Touches(shortcut.OpenedFromAreaId), Is.True);
            }
        }

        [Test]
        public void TheFutureRoadIsSealedNamelessAndCannotBeTravelledTo()
        {
            var future = _graph.Connections.Single(c => c.IsFuture);
            var beyond = _graph.Area(future.ToAreaId);
            Assert.That(_graph.Zone(beyond.ZoneId).IsImplemented, Is.False);
            Assert.That(beyond.DisplayName, Is.Empty, "No spoiler for a region that is not built.");
            Assert.That(future.RequiredBossId, Is.EqualTo(King));
            Assert.That(_graph.Checkpoints.Any(c => c.ZoneId == beyond.ZoneId), Is.False);
            Assert.That(WorldRules.IsOpen(future, new ProgressDataWorldQuery(Beaten(Guardian, Warden, King))),
                Is.False, "Beating the King opens his hall, not a zone that does not exist.");
        }

        [Test]
        public void EveryAltarIsATravelDestinationInABuiltZone()
        {
            Assert.That(_graph.Checkpoints.Count, Is.EqualTo(9));
            foreach (var checkpoint in _graph.Checkpoints)
            {
                Assert.That(checkpoint.CanFastTravel, Is.True, checkpoint.CheckpointId);
                Assert.That(_graph.Zone(checkpoint.ZoneId).IsImplemented, Is.True);
                Assert.That(checkpoint.DisplayName, Is.Not.Empty);
            }
        }

        [Test]
        public void AreasAreFoundByPositionAndPocketsOnlyByTheirTriggers()
        {
            Assert.That(_graph.AreaAt("TombTrial", new Vector2(-12f, 0.85f)).AreaId,
                Is.EqualTo("room_tomb_entrance"));
            Assert.That(_graph.AreaAt("TombTrial", new Vector2(50f, 1f)).AreaId,
                Is.EqualTo("room_tomb_guardian_arena"));
            Assert.That(_graph.AreaAt("TombTrial", new Vector2(70f, 1f)).AreaId,
                Is.EqualTo("room_forest_root_tunnel"));
            Assert.That(_graph.AreaAt("TombTrial", new Vector2(197f, 12.9f)).AreaId,
                Is.EqualTo("room_forest_watchtower_top"), "The smaller area wins where they overlap.");
            // Standing on the ossuary's shelf is still the passage by bounds; only its trigger
            // reveals the secret.
            Assert.That(_graph.AreaAt("TombTrial", new Vector2(13.5f, 8f)).AreaId,
                Is.EqualTo("room_tomb_guard_passage"));
            Assert.That(_graph.AreaAt("SomeOtherScene", new Vector2(-12f, 0.85f)), Is.Null);
        }

        // ---------- the validator catches what it claims to ----------

        [Test]
        public void TheValidatorCatchesBrokenGraphs()
        {
            var broken = Graph(
                areas: Area("a", "z") + "," + Area("a", "z") + "," + Area("b", "z"),
                connections: Link("c1", "a", "missing") + "," + Link("c2", "a", "b", ability: "flight"));
            var problems = string.Join("\n", WorldGraphValidator.Validate(broken));

            Assert.That(problems, Does.Contain("Duplicate area id 'a'"));
            Assert.That(problems, Does.Contain("missing area 'missing'"));
            Assert.That(problems, Does.Contain("unknown ability 'flight'"));
            Object.DestroyImmediate(broken);
        }

        [Test]
        public void TheValidatorNoticesAnAreaWithNoPlaceInTheWorld()
        {
            var graph = Graph(
                areas: "{\"_areaId\":\"a\",\"_zoneId\":\"z\"}",
                connections: string.Empty);
            var problems = string.Join("\n", WorldGraphValidator.Validate(graph));

            Assert.That(problems, Does.Contain("has no world bounds"),
                "A Rect that failed to load reads as zero and must not pass silently.");
            Object.DestroyImmediate(graph);
        }

        [Test]
        public void TheValidatorRefusesAMainPathBehindAnOptionalOrCombatAbility()
        {
            var graph = Graph(
                areas: Area("a", "z", altar: true) + "," + Area("b", "z"),
                connections: Link("c", "a", "b", ability: "shadow-dash"),
                abilities: "{\"_abilityId\":\"shadow-dash\",\"_kind\":2}");
            var problems = string.Join("\n", WorldGraphValidator.Validate(graph));

            Assert.That(problems, Does.Contain("Main-path area 'b'"));
            Assert.That(problems, Does.Contain("combat skill"));
            Object.DestroyImmediate(graph);
        }

        [Test]
        public void TheValidatorFindsADropWithNoWayBack()
        {
            var graph = Graph(
                areas: Area("a", "z", altar: true) + "," + Area("pit", "z"),
                connections: Link("drop", "a", "pit", type: ConnectionType.Drop, twoWay: false));
            var problems = string.Join("\n", WorldGraphValidator.Validate(graph));

            Assert.That(problems, Does.Contain("From 'pit' no altar can be reached again"));
            Object.DestroyImmediate(graph);
        }

        // ---------- save data ----------

        [Test]
        public void MapShortcutsAndTitleCardsSurviveARoundTrip()
        {
            var data = GameProgressData.CreateNew();
            data.MarkAreaDiscovered("room_tomb_entrance");
            data.MarkAreaDiscovered("room_tomb_burial_hall");
            data.UnlockShortcut("shortcut_tomb_crypt_bridge");
            data.MarkZoneIntroSeen("zone_forgotten_tomb");

            Assert.That(SaveSystem.Write(data), Is.True);
            var loaded = SaveSystem.Read();

            Assert.That(loaded.version, Is.EqualTo(GameProgressData.CurrentVersion));
            Assert.That(loaded.discoveredAreas, Is.EquivalentTo(
                new[] { "room_tomb_entrance", "room_tomb_burial_hall" }));
            Assert.That(loaded.IsShortcutUnlocked("shortcut_tomb_crypt_bridge"), Is.True);
            Assert.That(loaded.HasSeenZoneIntro("zone_forgotten_tomb"), Is.True);
        }

        [Test]
        public void UnlockingAShortcutTwiceRecordsItOnce()
        {
            var data = GameProgressData.CreateNew();
            data.UnlockShortcut("shortcut_forest_road_gate");
            data.UnlockShortcut("shortcut_forest_road_gate");
            data.unlockedShortcuts.Add("shortcut_forest_road_gate");
            data.Migrate();

            Assert.That(data.unlockedShortcuts.Count, Is.EqualTo(1));
        }

        [Test]
        public void APhaseTwentyFourSaveInTheTombGainsOnlyWhatItProves()
        {
            var data = LoadOldSave("{\"version\":9,\"checkpointId\":\"tomb-altar-02\","
                + "\"zoneId\":\"zone_forgotten_tomb\","
                + "\"activatedCheckpoints\":[\"tomb-altar-01\",\"tomb-altar-02\"]}");
            WorldDiscoveryMigration.Apply(data, _graph);

            Assert.That(data.discoveredAreas, Is.EquivalentTo(new[]
            {
                "room_tomb_entrance", "room_tomb_burial_hall", "room_tomb_guard_passage",
                "room_tomb_collapsed_crypt"
            }), "The walk from the entrance to the second altar, and nothing past it.");
            Assert.That(data.HasSeenZoneIntro("zone_forgotten_tomb"), Is.True);
            Assert.That(data.HasSeenZoneIntro("zone_withered_forest"), Is.False);
            Assert.That(data.checkpointId, Is.EqualTo("tomb-altar-02"), "The altar is untouched.");
        }

        [Test]
        public void APhaseTwentyFourSaveInTheForestKnowsTheRoadButNoSecrets()
        {
            var data = LoadOldSave("{\"version\":9,\"checkpointId\":\"royal-road-shrine\","
                + "\"zoneId\":\"zone_withered_forest\","
                + "\"activatedCheckpoints\":[\"tomb-altar-01\",\"tomb-altar-02\","
                + "\"forest-entrance\",\"royal-road-shrine\"],"
                + "\"defeatedBosses\":[\"guardian-of-the-tomb\"]}");
            WorldDiscoveryMigration.Apply(data, _graph);

            foreach (var id in new[] { "room_tomb_guardian_arena", "room_forest_root_tunnel",
                "room_forest_entrance", "room_forest_royal_road" })
            {
                Assert.That(data.IsAreaDiscovered(id), Is.True, id);
            }

            foreach (var id in new[] { "room_tomb_royal_ossuary", "room_tomb_bell_shaft",
                "room_tomb_reliquary", "room_forest_poison_grove", "room_forest_warden_arena" })
            {
                Assert.That(data.IsAreaDiscovered(id), Is.False, $"{id} was never proven visited.");
            }

            Assert.That(data.activatedCheckpoints.Count, Is.EqualTo(4), "Altars stay lit.");
            Assert.That(data.HasSeenZoneIntro("zone_withered_forest"), Is.True,
                "A player already in the forest is not greeted by its title again.");
        }

        [Test]
        public void MigrationIsSafeToRunTwiceAndOnAnEmptySave()
        {
            var data = LoadOldSave("{\"version\":9}");
            WorldDiscoveryMigration.Apply(data, _graph);
            var first = data.discoveredAreas.ToArray();
            WorldDiscoveryMigration.Apply(data, _graph);

            Assert.That(data.discoveredAreas, Is.EqualTo(first));
            Assert.That(first, Is.EqualTo(new[] { "room_tomb_entrance" }),
                "A save with nothing lit knows the room it starts in.");
        }

        [Test]
        public void AnUnknownCheckpointFromAFutureSaveRevealsNothing()
        {
            var data = LoadOldSave("{\"version\":9,\"checkpointId\":\"castle-gate\","
                + "\"activatedCheckpoints\":[\"castle-gate\"]}");
            WorldDiscoveryMigration.Apply(data, _graph);

            Assert.That(data.discoveredAreas, Is.EqualTo(new[] { "room_tomb_entrance" }));
        }

        // ---------- fast travel's rule ----------

        [Test]
        public void FastTravelOpensWithTheGuardianOrTheDebugOverride()
        {
            var data = GameProgressData.CreateNew();
            Assert.That(WorldRules.IsFastTravelUnlocked(_graph, new ProgressDataWorldQuery(data)),
                Is.False);

            data.MarkBossDefeated(Guardian);
            Assert.That(WorldRules.IsFastTravelUnlocked(_graph, new ProgressDataWorldQuery(data)),
                Is.True);

            var debug = GameProgressData.CreateNew();
            debug.GrantReward(_graph.FastTravelOverrideId);
            Assert.That(WorldRules.IsFastTravelUnlocked(_graph, new ProgressDataWorldQuery(debug)),
                Is.True);
        }

        // ---------- the map ----------

        [Test]
        public void ANewGameMapShowsTheFirstRoomAndOnlyASilhouetteBeyondIt()
        {
            var data = GameProgressData.CreateNew();
            data.MarkAreaDiscovered(_graph.StartAreaId);
            var view = Map(data, _graph.StartAreaId);

            var known = view.Nodes.Where(n => n.State == MapNodeState.Discovered).ToArray();
            Assert.That(known.Select(n => n.AreaId), Is.EqualTo(new[] { "room_tomb_entrance" }));
            Assert.That(known[0].IsCurrent, Is.True);
            Assert.That(known[0].Icons.Any(i => i.Type == MapIconType.Checkpoint && !i.IsActive),
                Is.True, "An unlit altar shows as unlit.");
            var silhouette = view.Node("room_tomb_burial_hall");
            Assert.That(silhouette.State, Is.EqualTo(MapNodeState.Unexplored));
            Assert.That(silhouette.Label, Is.Empty);
            Assert.That(view.Node("room_tomb_guardian_arena"), Is.Null);
            Assert.That(view.Node("room_tomb_bell_shaft"), Is.Null,
                "A secret next to a known room gets no silhouette.");
            Assert.That(view.ZoneHeader, Is.EqualTo("Забута Гробниця"));
        }

        [Test]
        public void TheBossIconAppearsOnlyWithTheArenaAndGoesQuietWhenHeFalls()
        {
            var data = Walked("room_tomb_entrance", "room_tomb_burial_hall",
                "room_tomb_guard_passage", "room_tomb_collapsed_crypt", "room_tomb_guardian_approach");
            var before = Map(data, "room_tomb_guardian_approach");
            Assert.That(before.Node("room_tomb_guardian_arena").State,
                Is.EqualTo(MapNodeState.Unexplored));
            Assert.That(before.Node("room_tomb_guardian_arena").Icons, Is.Empty,
                "No boss on the map before the arena is seen.");

            data.MarkAreaDiscovered("room_tomb_guardian_arena");
            var during = Map(data, "room_tomb_guardian_arena");
            Assert.That(during.Node("room_tomb_guardian_arena").Icons
                .Single(i => i.Type == MapIconType.Boss).IsActive, Is.True);

            data.MarkBossDefeated(Guardian);
            var after = Map(data, "room_tomb_guardian_arena");
            Assert.That(after.Node("room_tomb_guardian_arena").Icons
                .Single(i => i.Type == MapIconType.Boss).IsActive, Is.False);
        }

        [Test]
        public void AClosedGateNeverNamesAnAbilityThePlayerHasNotMet()
        {
            var data = Walked("room_tomb_entrance", "room_tomb_burial_hall", "room_tomb_guard_passage",
                "room_tomb_royal_ossuary");
            var view = Map(data, "room_tomb_guard_passage");
            var gate = view.Links.Single(l => l.Connection.ConnectionId == "conn_tomb_passage_ossuary");

            Assert.That(gate.State, Is.EqualTo(MapLinkState.Blocked));
            Assert.That(gate.Hint, Is.EqualTo(WorldMapBuilder.UnreachableHint));
            Assert.That(gate.Hint, Does.Not.Contain("стриб"));

            data.player.unlockedAbilities.Add(AbilityIds.DoubleJump);
            var later = Map(data, "room_tomb_guard_passage");
            Assert.That(later.Links.Single(l => l.Connection.ConnectionId == "conn_tomb_passage_ossuary")
                .State, Is.EqualTo(MapLinkState.Open));
        }

        [Test]
        public void AShortcutIsDrawnBrokenUntilItIsOpenedAndThenAsAnOrdinaryWay()
        {
            var data = Walked("room_tomb_guard_passage", "room_tomb_collapsed_crypt");
            var closed = Map(data, "room_tomb_collapsed_crypt").Links
                .Single(l => l.Connection.ConnectionId == "conn_tomb_crypt_bridge");
            Assert.That(closed.State, Is.EqualTo(MapLinkState.ShortcutLocked));

            data.UnlockShortcut("shortcut_tomb_crypt_bridge");
            var open = Map(data, "room_tomb_collapsed_crypt").Links
                .Single(l => l.Connection.ConnectionId == "conn_tomb_crypt_bridge");
            Assert.That(open.State, Is.EqualTo(MapLinkState.Open));
        }

        [Test]
        public void TheRoadBeyondTheCapitalIsSealedAndTheBorderIsDrawnHeavier()
        {
            var data = Walked("room_tomb_guardian_arena", "room_forest_root_tunnel",
                "room_forest_warden_arena", "room_forest_far_gate", "room_capital_ash_road",
                "room_capital_palace_approach", "room_capital_throne_hall");
            data.MarkBossDefeated(Guardian);
            data.MarkBossDefeated(Warden);
            data.MarkBossDefeated(King);
            var view = Map(data, "room_capital_throne_hall");

            var future = view.Links.Single(l => l.Connection.IsFuture);
            Assert.That(future.State, Is.EqualTo(MapLinkState.Sealed));
            Assert.That(view.Node("room_beyond_capital").Label, Is.Empty);
            Assert.That(view.Links.Single(l => l.Connection.ConnectionId == "conn_forest_to_capital")
                .IsZoneTransition, Is.True);
            Assert.That(view.Links.Single(l => l.Connection.ConnectionId == "conn_tomb_to_forest")
                .IsZoneTransition, Is.True);
        }

        [Test]
        public void AltarsBecomeTravelPointsOnceTravelIsOpen()
        {
            var data = Walked("room_tomb_entrance");
            data.MarkCheckpointActivated("tomb-altar-01");
            Assert.That(Map(data, "room_tomb_entrance").Node("room_tomb_entrance").Icons
                .Single(i => i.Id == "tomb-altar-01").Type, Is.EqualTo(MapIconType.Checkpoint));

            data.MarkBossDefeated(Guardian);
            Assert.That(Map(data, "room_tomb_entrance").Node("room_tomb_entrance").Icons
                .Single(i => i.Id == "tomb-altar-01").Type, Is.EqualTo(MapIconType.FastTravel));
        }

        [Test]
        public void CompletionCountsOrdinaryRoomsAndSecretsApart()
        {
            var data = Walked("room_tomb_entrance", "room_tomb_burial_hall", "room_tomb_bell_shaft");
            var completion = WorldMapBuilder.Completion(_graph, new ProgressDataWorldQuery(data),
                "zone_forgotten_tomb");

            Assert.That(completion.Discovered, Is.EqualTo(2));
            Assert.That(completion.SecretsFound, Is.EqualTo(1));
            Assert.That(completion.Total, Is.EqualTo(7), "Six rooms and the visible reliquary.");
            Assert.That(completion.SecretsTotal, Is.EqualTo(3), "The ossuary, the bell shaft and the mason's cache.");
        }

        [Test]
        public void TheViewportClampsZoomAndNeverLosesTheMap()
        {
            var viewport = new WorldMapViewport(0.5f, 3f);
            viewport.Configure(new Rect(0f, -1f, 30f, 3f), new Vector2(40f, 22f));

            viewport.ZoomBy(100f);
            Assert.That(viewport.Zoom, Is.EqualTo(3f));
            viewport.ZoomBy(0.0001f);
            Assert.That(viewport.Zoom, Is.EqualTo(0.5f));

            viewport.Pan(new Vector2(10000f, -10000f));
            Assert.That(viewport.Visible.Overlaps(viewport.Content), Is.True,
                "However far it is dragged, some of the map stays on screen.");
        }

        [Test]
        public void TheCursorStepsToTheNearestKnownRoomInTheDirectionPushed()
        {
            var data = Walked("room_tomb_entrance", "room_tomb_burial_hall", "room_tomb_guard_passage");
            var view = Map(data, "room_tomb_burial_hall");

            Assert.That(WorldMapViewport.Step(view, "room_tomb_burial_hall", Vector2.right).AreaId,
                Is.EqualTo("room_tomb_guard_passage"));
            Assert.That(WorldMapViewport.Step(view, "room_tomb_burial_hall", Vector2.left).AreaId,
                Is.EqualTo("room_tomb_entrance"));
            Assert.That(WorldMapViewport.Step(view, "room_tomb_entrance", Vector2.left).AreaId,
                Is.EqualTo("room_tomb_entrance"), "Nothing further west: the cursor stays.");
        }

        // ---------- helpers ----------

        private WorldMapView Map(GameProgressData data, string current)
        {
            var world = new ProgressDataWorldQuery(data);
            var area = _graph.Area(current);
            return WorldMapBuilder.Build(_graph, world, current, area != null ? area.ZoneId : "",
                WorldRules.IsFastTravelUnlocked(_graph, world));
        }

        private static GameProgressData Walked(params string[] areas)
        {
            var data = GameProgressData.CreateNew();
            foreach (var area in areas)
            {
                data.MarkAreaDiscovered(area);
            }

            return data;
        }

        private static GameProgressData Beaten(params string[] bosses)
        {
            var data = GameProgressData.CreateNew();
            foreach (var boss in bosses)
            {
                data.MarkBossDefeated(boss);
            }

            return data;
        }

        /// <summary>A file as PHASE 24 wrote it, loaded the way the game loads one.</summary>
        private static GameProgressData LoadOldSave(string json)
        {
            File.WriteAllText(SaveSystem.FilePath, json);
            var data = SaveSystem.Read();
            Assume.That(data.version, Is.EqualTo(GameProgressData.CurrentVersion));
            return data;
        }

        private static WorldGraph Graph(string areas, string connections, string abilities = "")
        {
            var graph = ScriptableObject.CreateInstance<WorldGraph>();
            JsonUtility.FromJsonOverwrite("{\"_zones\":[{\"_zoneId\":\"z\",\"_isImplemented\":true}],"
                + "\"_areas\":[" + areas + "],\"_connections\":[" + connections + "],"
                + "\"_checkpoints\":[{\"_checkpointId\":\"altar\",\"_zoneId\":\"z\",\"_areaId\":\"a\"}],"
                + "\"_abilities\":[" + abilities + "],\"_startAreaId\":\"a\"}", graph);
            return graph;
        }

        /// <summary>A room found by its trigger, so the synthetic graph needs no world bounds.</summary>
        private static string Area(string id, string zone, bool altar = false) =>
            "{\"_areaId\":\"" + id + "\",\"_zoneId\":\"" + zone + "\",\"_discoveryRule\":1}";

        private static string Link(string id, string from, string to, string ability = "",
            ConnectionType type = ConnectionType.Normal, bool twoWay = true) =>
            "{\"_connectionId\":\"" + id + "\",\"_fromAreaId\":\"" + from + "\",\"_toAreaId\":\"" + to
            + "\",\"_requiredAbilityId\":\"" + ability + "\",\"_type\":" + (int)type
            + ",\"_isBidirectional\":" + (twoWay ? "true" : "false") + "}";
    }
}
