using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Withered Forest as a place: its geometry, its dressing, where its enemies stand and what
    /// is hidden in it.
    ///
    /// It is built into the same scene as the Tomb and joined to it by a tunnel the player walks
    /// through, rather than loaded. That was a decision, not an omission: the brief asks for a
    /// visible transition and forbids teleporting the player to an unrelated map, and with no
    /// interface in the game there is nothing to show during a load. The zone is still recorded
    /// properly through <see cref="WorldGraph"/>, so the day it needs its own scene the change is
    /// one field and one load call rather than an excavation.
    ///
    /// Layout runs left to right from x = 63 to x = 300. Everything the player stands on has its
    /// top at y = 0, the same as the Tomb, so the two halves join without a step.
    /// </summary>
    public static partial class ContentBuilder
    {
        private static Dictionary<string, Sprite> _forest;

        /// <summary>Which tile a piece of forest is surfaced with; purely visual.</summary>
        private enum ForestSurface { Ground, Road, Cliff, Plank, BrokenPlank, RootWall }

        private static Sprite ForestArt(string name) =>
            _forest != null && _forest.TryGetValue(name, out var sprite) ? sprite : null;

        private static Sprite ForestTileFor(ForestSurface surface)
        {
            switch (surface)
            {
                case ForestSurface.Road: return ForestArt("env_forest_road_01");
                case ForestSurface.Cliff: return ForestArt("env_forest_cliff_01");
                case ForestSurface.Plank: return ForestArt("env_forest_platform_01");
                case ForestSurface.BrokenPlank: return ForestArt("env_forest_platform_broken_01");
                case ForestSurface.RootWall: return ForestArt("env_forest_rootwall_01");
                default: return ForestArt("env_forest_ground_01");
            }
        }

        /// <summary>
        /// A solid piece of forest. Same rule as the Tomb's <c>Block</c>: the collider is made from
        /// the gameplay size first and the art is fitted to it, never the other way round.
        /// </summary>
        private static GameObject ForestBlock(Transform parent, string name, Vector2 centre,
            Vector2 size, ForestSurface surface = ForestSurface.Ground)
        {
            var block = new GameObject(name);
            block.transform.SetParent(parent);
            block.transform.position = centre;
            block.layer = GroundLayer;
            var collider = block.AddComponent<BoxCollider2D>();
            collider.size = size;

            var visual = FindOrCreateChild(block, "Visual");
            var renderer = visual.AddComponent<SpriteRenderer>();
            var tile = ForestTileFor(surface);
            if (tile != null)
            {
                renderer.sprite = tile;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = size;
            }
            else
            {
                visual.transform.localScale = new Vector3(size.x, size.y, 1f);
                renderer.sprite = _sprite;
                renderer.sharedMaterial = _material;
                renderer.color = new Color(0.18f, 0.2f, 0.17f);
            }

            SetSorting(renderer, "GameplayEnvironment", 0);
            return block;
        }

        /// <summary>Dressing. Never carries a collider, exactly as in the Tomb.</summary>
        private static GameObject ForestProp(Transform parent, string spriteName, Vector2 position,
            string layer, int order, float scale = 1f, bool flip = false, float alpha = 1f)
        {
            var sprite = ForestArt(spriteName);
            if (sprite == null)
            {
                return null;
            }

            var prop = new GameObject(spriteName);
            prop.transform.SetParent(parent);
            prop.transform.position = position;
            prop.transform.localScale = new Vector3(flip ? -scale : scale, scale, 1f);
            var renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 1f, 1f, alpha);
            SetSorting(renderer, layer, order);
            return prop;
        }

        // ---------- the level ----------

        private static void BuildForestLevel(Transform level, ForestContent forest,
            GameObject checkpointPrefab, PlayerRespawner respawner, GameProgress progress,
            PlayerProgressionService progression, GameObject player, GameObject pickupPrefab,
            CombatDebugOverlay debug, GameObject zombiePrefab, GameObject guardPrefab,
            GameObject tombExit, Transform camera)
        {
            _forest = PlaceholderForestArt.Generate();
            var root = new GameObject("WitheredForest").transform;
            root.SetParent(level);

            BuildForestTransition(root, forest, progress, tombExit);
            BuildForestEntrance(root, forest, checkpointPrefab, respawner, progress, player,
                pickupPrefab, zombiePrefab);
            BuildRoyalRoad(root, forest, checkpointPrefab, respawner, progress, player,
                pickupPrefab);
            BuildPoisonGrove(root, forest, pickupPrefab, progress);
            BuildWatchtowerRuins(root, forest, pickupPrefab, progress);
            BuildDeepForest(root, forest, checkpointPrefab, respawner, progress, player,
                pickupPrefab, guardPrefab);
            BuildBossApproachForest(root, forest, checkpointPrefab, respawner, progress, player,
                pickupPrefab);
            BuildWardenArena(root, forest, respawner, progress, progression, pickupPrefab);
            BuildForestParallax(root, camera);
        }

        /// <summary>
        /// The tunnel out of the Tomb. Short, dark, and it ends in daylight — the point is that the
        /// change of place is something the player walks through rather than something that happens
        /// to them between screens.
        /// </summary>
        private static void BuildForestTransition(Transform parent, ForestContent forest,
            GameProgress progress, GameObject tombExit)
        {
            var section = Section(parent, "ForestTransition");
            ForestBlock(section, "TunnelFloor", new Vector2(72f, -0.5f), new Vector2(20f, 1f));
            ForestBlock(section, "TunnelRoof", new Vector2(70f, 5.5f), new Vector2(16f, 1f),
                ForestSurface.Cliff);
            ForestBlock(section, "TunnelWallBack", new Vector2(68f, 8f), new Vector2(12f, 4f),
                ForestSurface.Cliff);

            // The Tomb's far wall is the gate. It opens once the Guardian is recorded as beaten —
            // including for a save that beat it before this zone existed — and stays open, so the
            // way back into the Tomb is the same door the player came out of.
            PlaceDoor(section, "TombExitGate", new Vector2(62.5f, 3f),
                RequirementKind.BossDefeated, "guardian-of-the-tomb", tombExit, progress);

            // No trigger for the border any more: the world graph's areas say which zone the
            // player stands in, and the tunnel is the forest's first area, so the music and the
            // room change as the player steps into it — well before the trees are visible.

            ForestProp(section, "env_forest_roots_01", new Vector2(78f, 0f), "EnvironmentBack", 2);
            ForestProp(section, "env_forest_fog_01", new Vector2(79f, 2.2f), "Foreground", 1,
                0.7f, alpha: 0.55f);
        }

        /// <summary>
        /// First daylight. Wide, quiet and almost empty on purpose: after a corridor crypt the
        /// player should feel the space before anything asks them to fight in it.
        /// </summary>
        private static void BuildForestEntrance(Transform parent, ForestContent forest,
            GameObject checkpointPrefab, PlayerRespawner respawner, GameProgress progress,
            GameObject player, GameObject pickupPrefab, GameObject zombiePrefab)
        {
            var section = Section(parent, "ForestEntrance");
            ForestBlock(section, "EntranceFloor", new Vector2(97f, -0.5f), new Vector2(34f, 1f));
            // A step onto, not under: at 1.7, with room to spare under the 1.91 a jump reaches.
            ForestBlock(section, "EntranceRise", new Vector2(106f, 1.4f), new Vector2(6f, 0.6f),
                ForestSurface.Ground);

            PlaceCheckpoint(checkpointPrefab, new Vector2(83f, 0.8f), "forest-entrance",
                "Вівтар узлісся", respawner, progress, player);

            // Two hounds and one Tomb zombie that wandered out: the zone opens by teaching the new
            // threat next to a known one, so the difference in how they behave is the lesson.
            SpawnEnemy(forest.Hound, new Vector2(93f, 0.8f));
            SpawnEnemy(forest.Hound, new Vector2(99f, 0.8f));
            SpawnEnemy(zombiePrefab, new Vector2(104f, 0.9f));

            PlacePickup(pickupPrefab, new Vector2(88f, 0.6f), progress,
                string.Empty, true, 40, "forest-purse-01", true);
            PlacePickup(pickupPrefab, new Vector2(106f, 2.3f), progress,
                "venom_charm", false, 1, "forest-venom-charm", false);

            ForestProp(section, "env_forest_trunk_01", new Vector2(86f, 0f), "EnvironmentBack", 1);
            ForestProp(section, "env_forest_trunk_dead_01", new Vector2(95f, 0f),
                "EnvironmentBack", 1);
            ForestProp(section, "env_forest_trunk_01", new Vector2(103f, 0f), "EnvironmentBack", 1,
                1.1f, flip: true);
            ForestProp(section, "env_forest_stump_01", new Vector2(90f, 0f),
                "GameplayEnvironment", 1);
            ForestProp(section, "env_forest_roots_01", new Vector2(100f, 0f),
                "GameplayEnvironment", 1);
            ForestProp(section, "env_forest_decal_moss_01", new Vector2(92f, 0.05f),
                "GameplayEnvironment", 1, 1f, alpha: 0.6f);
            ForestProp(section, "env_forest_decal_puddle_01", new Vector2(97f, 0.05f),
                "GameplayEnvironment", 1, 1f, alpha: 0.8f);
        }

        /// <summary>
        /// The road the kingdom's armies used. Broken, sunken and overgrown, and it is where the
        /// archer is introduced — on a ledge, at a distance, with cover available on the ground.
        /// </summary>
        private static void BuildRoyalRoad(Transform parent, ForestContent forest,
            GameObject checkpointPrefab, PlayerRespawner respawner, GameProgress progress,
            GameObject player, GameObject pickupPrefab)
        {
            var section = Section(parent, "BrokenRoyalRoad");
            ForestBlock(section, "RoadFloor", new Vector2(129f, -0.5f), new Vector2(40f, 1f),
                ForestSurface.Road);
            // PHASE 25 moved both of these. The step used to hang between 1.5 and 2.1 — too low to
            // walk under and above the 1.91 a jump actually reaches — and the ledge 2.3 above it.
            // The road could not be crossed at all. Now the step is a jump from the floor and the
            // ledge a jump from the step, each 1.7 — the same as the Tomb's first step.
            ForestBlock(section, "RoadLedge", new Vector2(130f, 3.15f), new Vector2(7f, 0.5f),
                ForestSurface.Ground);
            // A metre and a half clear of the ledge: pressed against its east face heading back
            // west, a jump used to hit the ledge overhead and never reach the step's top.
            ForestBlock(section, "RoadStep", new Vector2(123f, 1.4f), new Vector2(4f, 0.6f),
                ForestSurface.Ground);
            // The old road's arch, barred. The first time through, the way east is over the
            // archer's ledge and down past the bars; the winch on the far side lifts them for
            // good, which is what makes the road walkable back towards the Tomb.
            BuildRoadGate(section, progress);

            PlaceCheckpoint(checkpointPrefab, new Vector2(115f, 0.8f), "royal-road-shrine",
                "Придорожня святиня", respawner, progress, player);

            SpawnEnemy(forest.Hound, new Vector2(122f, 0.8f));
            // The first archer stands above and behind cover, so the answer is to close or to
            // break line of sight rather than to out-shoot it.
            SpawnEnemy(forest.Archer, new Vector2(130f, 4.0f));
            SpawnEnemy(forest.Spearman, new Vector2(142f, 0.9f));

            // Secret one: the milestone. It is only environmental storytelling, and it is off the
            // path behind the arch, so finding it is a decision to look.
            PlacePickup(pickupPrefab, new Vector2(119f, 0.6f), progress,
                "royal_road_marker", false, 1, "forest-road-marker", false);

            ForestProp(section, "env_forest_milestone_01", new Vector2(119f, 0f),
                "GameplayEnvironment", 2);
            ForestProp(section, "env_forest_arch_ruin_01", new Vector2(137f, 0f),
                "EnvironmentBack", 1);
            ForestProp(section, "env_forest_statue_broken_01", new Vector2(127f, 0f),
                "EnvironmentBack", 2);
            ForestProp(section, "env_forest_trunk_dead_01", new Vector2(112f, 0f),
                "EnvironmentBack", 1, 1.15f);
            ForestProp(section, "env_forest_trunk_01", new Vector2(145f, 0f), "EnvironmentBack", 1,
                1.05f, flip: true);
            ForestProp(section, "env_forest_decal_moss_01", new Vector2(126f, 0.05f),
                "GameplayEnvironment", 1, 1.2f, alpha: 0.55f);
        }

        /// <summary>
        /// The grove. This is where the environment itself becomes a threat, so the fights are
        /// deliberately light: the player is learning a plant's rhythm, and doing that while three
        /// things are biting them would teach nothing.
        /// </summary>
        private static void BuildPoisonGrove(Transform parent, ForestContent forest,
            GameObject pickupPrefab, GameProgress progress)
        {
            var section = Section(parent, "PoisonGrove");
            ForestBlock(section, "GroveFloor", new Vector2(165f, -0.5f), new Vector2(36f, 1f));
            ForestBlock(section, "GroveLedge", new Vector2(172f, 5.6f), new Vector2(5f, 0.6f),
                ForestSurface.Ground);
            ForestBlock(section, "GroveStep", new Vector2(166f, 2.6f), new Vector2(3.4f, 0.6f),
                ForestSurface.Ground);

            // Three plants across the width, spaced so there is always standing room between two
            // of them. Nothing here can catch a player who is watching.
            PlacePlant(section, forest.PoisonPlant, new Vector2(156f, 0.3f));
            PlacePlant(section, forest.PoisonPlant, new Vector2(163f, 0.3f));
            PlacePlant(section, forest.PoisonPlant, new Vector2(176f, 0.3f));

            SpawnEnemy(forest.Hound, new Vector2(159f, 0.8f));
            SpawnEnemy(forest.Hound, new Vector2(169f, 0.8f));
            SpawnEnemy(forest.Archer, new Vector2(179f, 0.9f));

            // Secret two: an air-dash ledge. Optional, and visible from the ground — the route is
            // the puzzle, not the discovery.
            PlacePickup(pickupPrefab, new Vector2(172f, 6.1f), progress,
                "ancient_bark", false, 2, "forest-grove-bark", false);
            PlacePickup(pickupPrefab, new Vector2(161f, 0.6f), progress,
                "toxic_root", false, 2, "forest-grove-root", true);

            ForestProp(section, "env_forest_flora_01", new Vector2(156f, 0f),
                "GameplayEnvironment", 3);
            ForestProp(section, "env_forest_flora_01", new Vector2(163f, 0f),
                "GameplayEnvironment", 3);
            ForestProp(section, "env_forest_flora_01", new Vector2(176f, 0f),
                "GameplayEnvironment", 3);
            ForestProp(section, "env_forest_trunk_dead_01", new Vector2(152f, 0f),
                "EnvironmentBack", 1, 1.2f);
            ForestProp(section, "env_forest_trunk_dead_01", new Vector2(168f, 0f),
                "EnvironmentBack", 1, 1.1f, flip: true);
            ForestProp(section, "env_forest_roots_01", new Vector2(174f, 0f), "EnvironmentBack", 2);
            ForestProp(section, "env_forest_decal_spores_01", new Vector2(160f, 1.2f),
                "VFXBack", 0, 1.4f, alpha: 0.5f);
            ForestProp(section, "env_forest_canopy_01", new Vector2(165f, 9f),
                "Foreground", 0, 1f, alpha: 0.85f);
        }

        /// <summary>
        /// The one vertical stretch. It is where the forest asks for the movement the Tomb taught:
        /// a wall to run, ledges to air-dash between, and a drop back down that turns into a
        /// shortcut once it has been opened from the top.
        /// </summary>
        private static void BuildWatchtowerRuins(Transform parent, ForestContent forest,
            GameObject pickupPrefab, GameProgress progress)
        {
            var section = Section(parent, "WatchtowerRuins");
            ForestBlock(section, "RuinFloor", new Vector2(196f, -0.5f), new Vector2(32f, 1f));
            // Hangs from 2.5 up. It used to stand on the floor, eleven units tall, across the only
            // way east — the Warden could not be reached. It is still the wall the climb runs up.
            ForestBlock(section, "RuinWall", new Vector2(186f, 6.5f), new Vector2(0.8f, 8f),
                ForestSurface.Cliff);
            ForestBlock(section, "RuinLanding1", new Vector2(190f, 3.4f), new Vector2(4f, 0.5f),
                ForestSurface.Plank);
            ForestBlock(section, "RuinLanding2", new Vector2(196f, 6.2f), new Vector2(4f, 0.5f),
                ForestSurface.BrokenPlank);
            ForestBlock(section, "RuinLanding3", new Vector2(190.5f, 9f), new Vector2(4f, 0.5f),
                ForestSurface.Plank);
            ForestBlock(section, "RuinTop", new Vector2(197f, 11.8f), new Vector2(7f, 0.6f),
                ForestSurface.Ground);

            // An archer at the top of the climb: it turns the platforming into a decision about
            // when to move, which is the point of putting one there.
            SpawnEnemy(forest.Archer, new Vector2(198f, 12.6f));
            SpawnEnemy(forest.Hound, new Vector2(192f, 0.8f));
            SpawnEnemy(forest.Spearman, new Vector2(206f, 0.9f));

            // Secret three, and the zone's best weapon: the Thornblade, at the top of the tower.
            PlacePickup(pickupPrefab, new Vector2(199.5f, 12.3f), progress,
                "thornblade", false, 1, "forest-thornblade", false);

            // Shortcut: a rotten platform at the top that drops the player back to the floor in
            // one move, so the climb is never repeated after a death further on.
            ForestBlock(section, "RuinShortcutLip", new Vector2(201.5f, 11.8f),
                new Vector2(2f, 0.4f), ForestSurface.BrokenPlank);

            // The double jump's preview: a ledge that is visibly out of reach with everything the
            // player owns before the Warden. Nothing on the main path is behind it. From the top
            // a jump reaches 14.0 and a second one 15.4; the ledge is at 14.9, and its faces
            // give no grip, so a wall run up its edge cannot stand in for the second jump.
            var perch = ForestBlock(section, "RuinHighLedge", new Vector2(203f, 14.65f),
                new Vector2(4f, 0.5f), ForestSurface.Ground);
            Ensure<NonClimbableSurface>(perch);
            PlacePickup(pickupPrefab, new Vector2(203f, 15.4f), progress,
                "hunter_sigil", false, 1, "forest-hunter-sigil", false);
            // Narrower than the ledge, so pressing against its edge from below is not "inside".
            AreaTrigger(section, ForestHuntersPerch, new Vector2(203f, 15.9f),
                new Vector2(3.6f, 1.6f));
            // Kept clear of the Thornblade: the nearest thing is what the interact button uses.
            PlaceAbilityGate(section, "PerchHint", new Vector2(196.5f, 12.9f),
                AbilityIds.DoubleJump, "conn_forest_top_perch", new Vector2(203f, 15.3f),
                progress);

            ForestProp(section, "env_forest_watchtower_01", new Vector2(193f, 0f),
                "EnvironmentBack", 1);
            ForestProp(section, "env_forest_bridge_01", new Vector2(193.5f, 3.4f),
                "GameplayEnvironment", 2);
            ForestProp(section, "env_forest_roots_01", new Vector2(186f, 0f),
                "GameplayEnvironment", 1);
            ForestProp(section, "env_forest_trunk_01", new Vector2(209f, 0f), "EnvironmentBack", 1,
                1.3f);
            ForestProp(section, "env_forest_fog_01", new Vector2(196f, 7f), "Foreground", 1,
                1.1f, alpha: 0.4f);
        }

        /// <summary>
        /// Where the three enemy roles finally meet. The shaman is introduced alone and at
        /// distance, then once with a hound, so the player learns what it does before learning
        /// what it does while something is biting them.
        /// </summary>
        private static void BuildDeepForest(Transform parent, ForestContent forest,
            GameObject checkpointPrefab, PlayerRespawner respawner, GameProgress progress,
            GameObject player, GameObject pickupPrefab, GameObject guardPrefab)
        {
            var section = Section(parent, "DeepForest");
            ForestBlock(section, "DeepFloorWest", new Vector2(220f, -0.5f), new Vector2(20f, 1f));
            // A real gap, crossed on a rotten bridge that is narrow enough to fall off.
            // Flush with the floors on both sides. It sat a quarter unit low, and the motor has no
            // step-up: walking out of the dip meant jumping.
            ForestBlock(section, "DeepBridge", new Vector2(232f, -0.25f), new Vector2(4.5f, 0.5f),
                ForestSurface.BrokenPlank);
            ForestBlock(section, "DeepFloorEast", new Vector2(241f, -0.5f), new Vector2(14f, 1f));

            var pit = new GameObject("ForestKillZone");
            pit.transform.SetParent(section);
            pit.transform.position = new Vector3(232f, -6f, 0f);
            var pitTrigger = pit.AddComponent<BoxCollider2D>();
            pitTrigger.isTrigger = true;
            pitTrigger.size = new Vector2(7f, 3f);
            SetReference(pit.AddComponent<KillZone>(), "_respawner", respawner);

            PlaceCheckpoint(checkpointPrefab, new Vector2(216f, 0.8f), "deep-forest-shrine",
                "Святиня глибокого лісу", respawner, progress, player);

            // Alone first, on the far side of the room.
            SpawnEnemy(forest.Shaman, new Vector2(226f, 0.9f));
            SpawnEnemy(forest.Hound, new Vector2(222f, 0.8f));
            // Then the pairing the section is really about, past the bridge.
            SpawnEnemy(forest.Shaman, new Vector2(244f, 0.9f));
            SpawnEnemy(forest.Archer, new Vector2(240f, 0.9f));
            // One Tomb guard, still at its post. Continuity, and a familiar shape to fight beside
            // the new ones.
            SpawnEnemy(guardPrefab, new Vector2(237f, 0.9f));

            PlacePickup(pickupPrefab, new Vector2(212f, 0.6f), progress,
                "warden_greatblade", false, 1, "forest-greatblade", false);
            PlacePickup(pickupPrefab, new Vector2(246f, 0.6f), progress,
                string.Empty, true, 70, "forest-purse-02", true);

            PlacePlant(section, forest.PoisonPlant, new Vector2(219f, 0.3f));
            PlacePlant(section, forest.PoisonPlant, new Vector2(243f, 0.3f));

            ForestProp(section, "env_forest_bridge_01", new Vector2(232f, -0.8f),
                "GameplayEnvironment", 2);
            ForestProp(section, "env_forest_trunk_dead_01", new Vector2(214f, 0f),
                "EnvironmentBack", 1, 1.35f);
            ForestProp(section, "env_forest_trunk_dead_01", new Vector2(228f, 0f),
                "EnvironmentBack", 1, 1.25f, flip: true);
            ForestProp(section, "env_forest_roots_01", new Vector2(240f, 0f), "EnvironmentBack", 2);
            ForestProp(section, "env_forest_flora_01", new Vector2(219f, 0f),
                "GameplayEnvironment", 3);
            ForestProp(section, "env_forest_flora_01", new Vector2(243f, 0f),
                "GameplayEnvironment", 3);
            ForestProp(section, "env_forest_canopy_01", new Vector2(226f, 9.5f),
                "Foreground", 0, 1.2f, alpha: 0.9f);
            ForestProp(section, "env_forest_fog_01", new Vector2(232f, 1f), "VFXBack", 0, 1.3f,
                alpha: 0.45f);
        }

        /// <summary>
        /// The approach. One fight, then a shrine, then quiet — the brief is explicit that the
        /// player should not arrive at a boss already spent, and the last thirty metres are
        /// deliberately empty so the arena announces itself.
        /// </summary>
        private static void BuildBossApproachForest(Transform parent, ForestContent forest,
            GameObject checkpointPrefab, PlayerRespawner respawner, GameProgress progress,
            GameObject player, GameObject pickupPrefab)
        {
            var section = Section(parent, "WardenApproach");
            ForestBlock(section, "ApproachFloor", new Vector2(260f, -0.5f), new Vector2(26f, 1f),
                ForestSurface.Road);
            ForestBlock(section, "ApproachLedge", new Vector2(253f, 3f), new Vector2(4f, 0.5f),
                ForestSurface.Ground);

            // The one hard encounter of the section: a spear holding the middle, an archer behind
            // it and a hound to break the player's stance. Three roles, one at a time if the
            // player picks their ground — the ledge is there for exactly that.
            SpawnEnemy(forest.Spearman, new Vector2(252f, 0.9f));
            SpawnEnemy(forest.Archer, new Vector2(257f, 0.9f));
            SpawnEnemy(forest.Hound, new Vector2(255f, 0.8f));

            PlacePickup(pickupPrefab, new Vector2(253f, 3.5f), progress,
                "warden_pauldron", false, 1, "forest-pauldron", false);

            // The last altar, close enough that a death costs a walk rather than a journey.
            PlaceCheckpoint(checkpointPrefab, new Vector2(264f, 0.8f), "warden-shrine",
                "Святиня Вартового", respawner, progress, player);

            ForestProp(section, "env_forest_statue_broken_01", new Vector2(260f, 0f),
                "EnvironmentBack", 2);
            ForestProp(section, "env_forest_statue_broken_01", new Vector2(266f, 0f),
                "EnvironmentBack", 2, 1f, flip: true);
            ForestProp(section, "env_forest_arch_ruin_01", new Vector2(268f, 0f),
                "EnvironmentBack", 1);
            ForestProp(section, "env_forest_trunk_dead_01", new Vector2(249f, 0f),
                "EnvironmentBack", 1, 1.4f);
            ForestProp(section, "env_forest_fog_01", new Vector2(262f, 1.4f), "VFXBack", 0, 1.5f,
                alpha: 0.5f);
        }

        /// <summary>
        /// The clearing. Flat in the middle, because a boss fight is not a platforming fight, with
        /// the elevation kept to the background where it reads as depth and never as a ledge.
        /// </summary>
        private static void BuildWardenArena(Transform parent, ForestContent forest,
            PlayerRespawner respawner, GameProgress progress, PlayerProgressionService progression,
            GameObject pickupPrefab)
        {
            var section = Section(parent, "WardenArena");
            ForestBlock(section, "WardenArenaFloor", new Vector2(288f, -0.5f), new Vector2(30f, 1f));

            var barrier = ForestBlock(section, "WardenBarrier", new Vector2(272.5f, 3.5f),
                new Vector2(1f, 9f), ForestSurface.RootWall);
            barrier.SetActive(false);

            var warden = (GameObject)PrefabUtility.InstantiatePrefab(forest.Warden);
            warden.transform.SetParent(section);
            warden.transform.position = new Vector2(296f, 1.2f);

            var encounterObject = new GameObject("WardenEncounter");
            encounterObject.transform.SetParent(section);
            encounterObject.transform.position = new Vector2(276f, 2f);
            var encounterTrigger = encounterObject.AddComponent<BoxCollider2D>();
            encounterTrigger.isTrigger = true;
            encounterTrigger.size = new Vector2(3f, 6f);
            var encounter = encounterObject.AddComponent<BossEncounter>();
            SetReference(encounter, "_boss", warden.GetComponent<BossController>());
            SetReference(encounter, "_barrier", barrier);
            SetReference(encounter, "_progress", progress);
            SetReference(encounter, "_respawner", respawner);
            SetReference(encounter, "_progression", progression);
            SetString(encounter, "_rewardId", "warden_sigil");
            SetString(encounter, "_rewardName", "Печать Вартового");
            SetInt(encounter, "_rewardCurrency", 900);
            SetInt(encounter, "_rewardExperience", 1400);
            // The reward that matters: the second jump. Granted inside the same once-only guard as
            // the item and the coins, so it can never be handed out twice.
            SetString(encounter, "_rewardAbilityId", AbilityIds.DoubleJump);

            // The way on to the next region, opened by the Warden's death and not before.
            var exitWall = ForestBlock(section, "ForestExitWall", new Vector2(302f, 4f),
                new Vector2(1f, 10f), ForestSurface.RootWall);
            PlaceDoor(section, "ForestExitGate", new Vector2(302f, 4f),
                RequirementKind.BossDefeated, WardenBossId, exitWall, progress);

            // Past the gate: a ledge that only the Warden's own reward reaches. It is the first
            // thing the second jump is for, and it is past the fight, never before it. It stood
            // at 5.5 in PHASE 24, higher than even the second jump reaches; now 2.8, which a
            // jump (1.9) cannot make and a second jump (3.3) can, with smooth faces to stop a
            // wall run doing the same.
            var overlook = ForestBlock(section, "ForestExitLedge", new Vector2(307f, 2.55f),
                new Vector2(5f, 0.5f), ForestSurface.Ground);
            Ensure<NonClimbableSurface>(overlook);
            PlacePickup(pickupPrefab, new Vector2(307.5f, 3.3f), progress, "verdant_heart",
                false, 1, "forest-verdant-heart", false);
            // Above the shelf only, and inset from its ends: a jump beside it does not count.
            AreaTrigger(section, ForestOverlook, new Vector2(307f, 3.8f), new Vector2(4f, 1.6f));
            PlaceAbilityGate(section, "OverlookHint", new Vector2(303.6f, 0.8f),
                AbilityIds.DoubleJump, "conn_forest_far_overlook", new Vector2(307f, 3.4f),
                progress);
            ForestBlock(section, "ForestExitFloor", new Vector2(308f, -0.5f), new Vector2(11f, 1f));

            // The road on. Sealed by something that is not a Warden or a lever: the next region is
            // not built, and the map shows a closed way east with no name on it. Solid, so nobody
            // walks off the end of the world.
            var future = ForestBlock(section, "FutureRoadSeal", new Vector2(313.5f, 4f),
                new Vector2(1f, 10f), ForestSurface.RootWall);
            Ensure<NonClimbableSurface>(future);
            PlaceDoor(section, "FutureRoadGate", new Vector2(313.5f, 4f),
                RequirementKind.Unavailable, "zone_unknown_east", future, progress);

            ForestProp(section, "env_forest_arena_backdrop", new Vector2(288f, -1f),
                "EnvironmentBack", 0, 1f);
            ForestProp(section, "env_forest_arena_crest", new Vector2(292f, 0.1f),
                "GameplayEnvironment", 1, 1f, alpha: 0.8f);
            ForestProp(section, "env_forest_roots_01", new Vector2(280f, 0f), "EnvironmentBack", 2);
            ForestProp(section, "env_forest_roots_01", new Vector2(299f, 0f), "EnvironmentBack", 2,
                1f, flip: true);
            ForestProp(section, "env_forest_fog_01", new Vector2(288f, 0.8f), "VFXBack", 0, 2f,
                alpha: 0.4f);

        }

        /// <summary>
        /// The Forest's shortcut: bars across the old road's arch, lifted by a winch on the east
        /// side. Heading east for the first time the player goes over the archer's ledge and drops
        /// past the bars; the winch is right there on landing. Heading west the bars are the first
        /// thing met, with the winch beside them, so the road back can never be a trap.
        ///
        /// The bars reach 3.9: above a jump (1.9) and a second jump (3.3) from the floor alike, so
        /// the shortcut is the only way west through the arch. From the ledge (3.4) they are a
        /// small hop, which is the way east.
        /// </summary>
        private static void BuildRoadGate(Transform section, GameProgress progress)
        {
            var bars = ForestBlock(section, "RoadGate", new Vector2(134f, 1.7f),
                new Vector2(1.2f, 4.4f), ForestSurface.RootWall);
            Ensure<NonClimbableSurface>(bars);
            var art = Object.Instantiate(bars.transform.Find("Visual").gameObject, section);
            art.name = "RoadGateBars";
            art.transform.position = bars.transform.position;
            bars.transform.Find("Visual").gameObject.SetActive(false);

            var winch = new GameObject("RoadGateWinch");
            winch.transform.SetParent(section);
            winch.transform.position = new Vector2(136f, 0.9f);
            var trigger = winch.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.8f;
            var handle = ForestProp(winch.transform, "env_forest_stump_01", new Vector2(136f, 0f),
                "GameplayEnvironment", 3, 0.6f);
            var shortcut = winch.AddComponent<Shortcut>();
            SetString(shortcut, "_shortcutId", ForestRoadGateShortcut);
            SetString(shortcut, "_displayName", "лебідка воріт");
            SetReference(shortcut, "_progress", progress);
            // Open, the bars sit up in the arch; closed, 4.2 lower, across the road.
            art.transform.position += new Vector3(0f, 4.2f, 0f);
            ConfigureShortcut(shortcut, System.Array.Empty<GameObject>(), new[] { bars },
                art.transform, new Vector3(0f, -4.2f, 0f), handle != null ? handle.transform : null);
        }

        /// <summary>
        /// A hazard rooted in the level. Its cycle runs from the moment the scene loads, so its
        /// rhythm can be watched from outside its reach before anyone steps into it.
        /// </summary>
        private static void PlacePlant(Transform parent, GameObject prefab, Vector2 position)
        {
            if (prefab == null)
            {
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent);
            instance.transform.position = position;
        }

        /// <summary>
        /// The forest's own backdrop. It moves faster than the Tomb's, because trees at different
        /// depths separate far more than walls in a corridor do, and a still forest reads as a
        /// painted flat.
        /// </summary>
        private static void BuildForestParallax(Transform parent, Transform camera)
        {
            var layers = new[]
            {
                // Dropped below the floor and stretched upward, so the treelines fill the frame
                // from the ground to the sky instead of floating over an empty band.
                ("env_forest_bg_far_01", "BackgroundFar", 0.12f, 0.04f, -4f, 1f),
                ("env_forest_bg_mid_01", "BackgroundMid", 0.32f, 0.09f, -4f, 1f),
                ("env_forest_bg_near_01", "BackgroundNear", 0.58f, 0.16f, -4f, 1f)
            };

            var root = Section(parent, "ForestParallax");
            SetString(root.gameObject.AddComponent<ZoneBackdrop>(), "_zoneId", ForestZoneId);
            foreach (var (spriteName, layer, factor, vertical, height, alpha) in layers)
            {
                var art = ForestArt(spriteName);
                if (art == null)
                {
                    continue;
                }

                var strip = new GameObject(spriteName);
                strip.transform.SetParent(root);
                // Centred on the zone rather than on the origin, so the strip covers it without
                // needing to be absurdly wide.
                strip.transform.position = new Vector3(185f, height, 0f);
                strip.transform.localScale = new Vector3(1f, 1.45f, 1f);
                var renderer = strip.AddComponent<SpriteRenderer>();
                if (layer != "BackgroundNear")
                {
                    renderer.sharedMaterial = _material;
                }
                renderer.sprite = art;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = new Vector2(art.bounds.size.x * 16f, art.bounds.size.y);
                renderer.color = new Color(1f, 1f, 1f, alpha);
                SetSorting(renderer, layer, 1);

                var parallax = strip.AddComponent<ParallaxLayer>();
                SetFloat(parallax, "_factor", factor);
                SetFloat(parallax, "_verticalFactor", vertical);
                SetReference(parallax, "_camera", camera);
            }
        }

        /// <summary>
        /// Gives the ambience and the music a palette per region. Without this the forest would
        /// sound exactly like the crypt, which would undo most of the work of building it.
        /// </summary>
        private static void ConfigureZoneAudio(GameObject systems)
        {
            var ambience = systems.GetComponent<AmbienceController>();
            if (ambience != null)
            {
                var serialized = new SerializedObject(ambience);
                var palettes = serialized.FindProperty("_palettes");
                palettes.arraySize = 2;

                var tomb = palettes.GetArrayElementAtIndex(0);
                tomb.FindPropertyRelative("_zoneId").stringValue = TombZoneId;
                tomb.FindPropertyRelative("_bed").enumValueIndex = (int)AudioId.AmbienceTombBase;
                var tombDetails = tomb.FindPropertyRelative("_details");
                tombDetails.arraySize = 4;
                tombDetails.GetArrayElementAtIndex(0).enumValueIndex =
                    (int)AudioId.AmbienceStoneSettle;
                tombDetails.GetArrayElementAtIndex(1).enumValueIndex = (int)AudioId.AmbienceChain;
                tombDetails.GetArrayElementAtIndex(2).enumValueIndex = (int)AudioId.AmbienceDrip;
                tombDetails.GetArrayElementAtIndex(3).enumValueIndex = (int)AudioId.AmbienceWind;
                tomb.FindPropertyRelative("_minimumGap").floatValue = 7f;
                tomb.FindPropertyRelative("_maximumGap").floatValue = 21f;

                var forest = palettes.GetArrayElementAtIndex(1);
                forest.FindPropertyRelative("_zoneId").stringValue = ForestZoneId;
                forest.FindPropertyRelative("_bed").enumValueIndex = (int)AudioId.AmbienceForestBase;
                var forestDetails = forest.FindPropertyRelative("_details");
                forestDetails.arraySize = 5;
                forestDetails.GetArrayElementAtIndex(0).enumValueIndex = (int)AudioId.AmbienceLeaves;
                forestDetails.GetArrayElementAtIndex(1).enumValueIndex = (int)AudioId.AmbienceBranch;
                forestDetails.GetArrayElementAtIndex(2).enumValueIndex =
                    (int)AudioId.AmbienceInsects;
                forestDetails.GetArrayElementAtIndex(3).enumValueIndex =
                    (int)AudioId.AmbienceDistantBeast;
                forestDetails.GetArrayElementAtIndex(4).enumValueIndex = (int)AudioId.AmbienceWind;
                // A living wood is busier than a sealed crypt, so its details come closer together.
                forest.FindPropertyRelative("_minimumGap").floatValue = 5f;
                forest.FindPropertyRelative("_maximumGap").floatValue = 15f;

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var music = systems.GetComponent<MusicDirector>();
            if (music == null)
            {
                return;
            }

            // Two palettes, both empty. The states and the crossfades are real and tested; there
            // is still no music, and this does not pretend otherwise.
            var musicData = new SerializedObject(music);
            var musicPalettes = musicData.FindProperty("_palettes");
            musicPalettes.arraySize = 2;
            musicPalettes.GetArrayElementAtIndex(0).FindPropertyRelative("_zoneId").stringValue =
                TombZoneId;
            musicPalettes.GetArrayElementAtIndex(1).FindPropertyRelative("_zoneId").stringValue =
                ForestZoneId;
            musicData.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
