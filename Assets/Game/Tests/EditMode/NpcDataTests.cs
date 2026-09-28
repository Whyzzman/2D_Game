using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The first people of the world as data (item 70, 2026-09-28): who they are and what they
    /// say as the story moves, what the merchant sells and at what price, what a bought map,
    /// rumour and quill put on the map, and a flask shard's extra flask.
    /// </summary>
    public sealed class NpcDataTests
    {
        private const string Guardian = "guardian-of-the-tomb";
        private const string Warden = "hollow-warden";
        private const string Tomb = "zone_forgotten_tomb";
        private const string Forest = "zone_withered_forest";

        /// <summary>What one pass of each zone pays today (enemies, purses and the boss).</summary>
        private const float TombIncome = 630f;
        private const float ForestIncome = 1350f;

        private GameObject _host;
        private GameProgress _progress;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("NpcDataTests");
            _progress = _host.AddComponent<GameProgress>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_host);

        private static NpcDefinition Load(string name) =>
            AssetDatabase.LoadAssetAtPath<NpcDefinition>($"Assets/Game/Data/Npc/{name}.asset");

        private static MerchantStock Stock =>
            AssetDatabase.LoadAssetAtPath<MerchantStock>("Assets/Game/Data/Npc/MerchantStock.asset");

        [Test]
        public void ThreePeopleHaveWordsAndOnlyTheMerchantTrades()
        {
            var gnat = Load("Npc_Gnat");
            var myroslava = Load("Npc_Myroslava");
            var severyn = Load("Npc_Severyn");
            foreach (var npc in new[] { gnat, myroslava, severyn })
            {
                Assert.That(npc, Is.Not.Null);
                Assert.That(npc.DisplayName, Is.Not.Empty);
                Assert.That(npc.Topics.Length, Is.GreaterThanOrEqualTo(3), npc.DisplayName);
                Assert.That(npc.Topics.All(t => t.pages.Length > 0 && t.pages.All(p => !string.IsNullOrEmpty(p))),
                    Is.True, npc.DisplayName);
                Assert.That(npc.Topics.Select(t => t.id).Distinct().Count(), Is.EqualTo(npc.Topics.Length));
            }

            Assert.That(gnat.IsMerchant, Is.True);
            Assert.That(myroslava.IsMerchant || severyn.IsMerchant, Is.False);
        }

        [Test]
        public void WhatACharacterSaysFollowsTheStory()
        {
            var gnat = Load("Npc_Gnat");
            Assert.That(gnat.Pick(_progress).id, Is.EqualTo("meet"));
            _progress.GrantReward(gnat.HeardId(gnat.Pick(_progress)));
            Assert.That(gnat.Pick(_progress).id, Is.EqualTo("again"), "Heard once, then the plain greeting.");
            _progress.MarkBossDefeated(Guardian);
            Assert.That(gnat.Pick(_progress).id, Is.EqualTo("moved"), "The Guardian's fall is news.");

            var severyn = Load("Npc_Severyn");
            var fresh = new GameObject("Fresh");
            try
            {
                Assert.That(severyn.Pick(fresh.AddComponent<GameProgress>()).id, Is.EqualTo("silent"),
                    "The ghost says nothing while the Guardian stands.");
            }
            finally
            {
                Object.DestroyImmediate(fresh);
            }

            Assert.That(severyn.Pick(_progress).id, Is.EqualTo("night"));

            var myroslava = Load("Npc_Myroslava");
            _progress.MarkBossDefeated(Warden);
            Assert.That(myroslava.Pick(_progress).id, Is.EqualTo("end"));
            _progress.GrantReward(myroslava.HeardId(myroslava.Pick(_progress)));
            Assert.That(myroslava.LatestNote(_progress), Does.Contain("дорога скінчилась"));
        }

        [Test]
        public void TheCartIsPricedToChooseButNeverToGrind()
        {
            var goods = Stock.Goods;
            Assert.That(goods.Select(g => g.id).Distinct().Count(), Is.EqualTo(goods.Length));
            Assert.That(goods.All(g => g.price > 0), Is.True);

            void Check(IEnumerable<MerchantGood> zone, float income, string name)
            {
                var list = zone.ToList();
                var total = list.Sum(g => g.price);
                var essentials = list.Where(g => g.kind == GoodKind.ZoneMap || g.kind == GoodKind.FlaskShard)
                    .Sum(g => g.price);
                Assert.That(total / income, Is.InRange(1.2f, 1.6f), $"{name}: all of it is a choice, not a given.");
                Assert.That(essentials / income, Is.InRange(0.35f, 0.6f), $"{name}: the map and a flask come easily.");
            }

            Check(goods.Where(g => string.IsNullOrEmpty(g.requiresBoss)), TombIncome, "Tomb");
            Check(goods.Where(g => g.requiresBoss == Guardian), ForestIncome, "Forest");
        }

        [Test]
        public void EveryGoodPointsAtSomethingThatExists()
        {
            var graph = AssetDatabase.LoadAssetAtPath<WorldGraph>("Assets/Game/Data/WorldGraph.asset");
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Game/Data/ItemDatabase.asset");
            foreach (var good in Stock.Goods)
            {
                switch (good.kind)
                {
                    case GoodKind.ZoneMap:
                        Assert.That(graph.Zone(good.zoneId), Is.Not.Null, good.id);
                        break;
                    case GoodKind.Rumour:
                        var area = graph.Area(good.target);
                        Assert.That(area, Is.Not.Null, good.id);
                        Assert.That(area.ZoneId, Is.EqualTo(good.zoneId), good.id);
                        Assert.That(good.quote, Is.Not.Empty, good.id);
                        break;
                    case GoodKind.Relic:
                    case GoodKind.Lockpick:
                        Assert.That(items.GetItem(good.target), Is.Not.Null, good.id);
                        break;
                }
            }
        }

        [Test]
        public void ABoughtMapChartsTheZoneAndARumourItsSecret()
        {
            var graph = AssetDatabase.LoadAssetAtPath<WorldGraph>("Assets/Game/Data/WorldGraph.asset");
            var before = WorldMapBuilder.Build(graph, _progress, string.Empty, Tomb, false);
            Assert.That(before.Nodes.Any(n => n.State == MapNodeState.Charted), Is.False);

            _progress.GrantReward(MerchantStock.MapRewardId(Tomb));
            _progress.GrantReward(MerchantStock.RumourRewardId("room_tomb_royal_ossuary"));
            var after = WorldMapBuilder.Build(graph, _progress, string.Empty, Tomb, false);
            foreach (var area in graph.AreasIn(Tomb))
            {
                var node = after.Node(area.AreaId);
                if (area.IsSecret && area.AreaId != "room_tomb_royal_ossuary")
                {
                    Assert.That(node, Is.Null, $"{area.AreaId}: a map never gives a secret away.");
                    continue;
                }

                Assert.That(node, Is.Not.Null, area.AreaId);
                Assert.That(node.State, Is.EqualTo(MapNodeState.Charted), area.AreaId);
            }

            Assert.That(after.Node("room_tomb_royal_ossuary").Icons.Any(i => i.Type == MapIconType.Rumour), Is.True);
            Assert.That(graph.AreasIn(Forest).All(a => after.Node(a.AreaId) == null), Is.True,
                "The Tomb's map does not chart the Forest.");
            Assert.That(after.Links.Count, Is.GreaterThan(3), "Charted rooms are joined as on a map.");
        }

        [Test]
        public void TheQuillMarksFiveRoomsAndTakesAMarkBack()
        {
            Assert.That(_progress.ToggleMapMark("a"), Is.False, "No quill, no marks.");
            _progress.GrantReward(MerchantStock.QuillRewardId);
            for (var i = 0; i < MapMark.Colors; i++)
            {
                Assert.That(_progress.ToggleMapMark("room" + i), Is.True);
            }

            Assert.That(_progress.ToggleMapMark("room-six"), Is.False, "Five colours, five marks.");
            Assert.That(_progress.MapMarks.Select(m => m.color).Distinct().Count(), Is.EqualTo(MapMark.Colors));
            Assert.That(_progress.ToggleMapMark("room2"), Is.True);
            Assert.That(_progress.MapMarks.Any(m => m.areaId == "room2"), Is.False, "Marking again takes it off.");
            Assert.That(_progress.ToggleMapMark("room-six"), Is.True, "Its colour is free again.");
        }

        [Test]
        public void AFlaskShardAddsAFullFlask()
        {
            var flasks = new HealingFlasks();
            Assert.That(flasks.MaxCharges, Is.EqualTo(3));
            flasks.SetBonusCharges(1);
            Assert.That(flasks.MaxCharges, Is.EqualTo(4));
            Assert.That(flasks.Charges, Is.EqualTo(4));
            flasks.SetBonusCharges(0);
            Assert.That(flasks.Charges, Is.EqualTo(3), "A shard lost with an older save takes its flask along.");
        }
    }
}
