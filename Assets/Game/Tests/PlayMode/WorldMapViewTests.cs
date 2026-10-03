using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The world map of design/ui/world-map (item 2 of the 15-zone list): it opens on the world with
    /// the hero's region chosen, shows what roads lead to as silhouettes and nothing past them, goes
    /// into a known region and back out; and it is the first tab of the pause's window.
    /// </summary>
    public sealed class WorldMapViewTests
    {
        private WorldMapScreen _map;
        private HudController _hud;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            _map = Object.FindAnyObjectByType<WorldMapScreen>();
            _hud = Object.FindAnyObjectByType<HudController>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_hud != null && _hud.Inventory.IsOpen) { _hud.Inventory.Close(); }
            if (_map != null && _map.IsOpen) { _map.Close(); }
            if (_hud != null && _hud.Pause.IsOpen) { _hud.Pause.Resume(); }
            SaveSystem.Delete();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheMapOpensOnTheWorldAndGoesIntoARegion()
        {
            Assert.That(_hud.Map.Open(), Is.True);
            yield return null;
            Assert.That(_map.Level, Is.EqualTo(MapLevel.World));
            Assert.That(_map.FocusZoneId, Is.EqualTo("zone_forgotten_tomb"));
            var atlas = _map.Atlas;
            Assert.That(atlas.Zone("zone_forgotten_tomb").State, Is.EqualTo(ZoneMapState.Known));
            Assert.That(atlas.Zone("zone_forgotten_tomb").IsCurrent, Is.True);
            Assert.That(atlas.Zone("zone_withered_forest").State, Is.EqualTo(ZoneMapState.Silhouette), "A road leads there.");
            Assert.That(atlas.Zone("zone_ashen_capital").State, Is.EqualTo(ZoneMapState.Hidden), "Past that, the fog.");
            Assert.That(_map.EnterZone("zone_withered_forest"), Is.False, "Only a known region opens.");

            var root = _hud.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(root.Q<Label>("map-zone").text, Is.EqualTo("Карта світу"));
            Assert.That(root.Q<Label>("map-info-name").text, Is.EqualTo("Забута Гробниця"), "The chosen region's card.");
            Assert.That(root.Q("map-card").resolvedStyle.backgroundImage.texture, Is.Not.Null, "With its picture.");

            Assert.That(_map.EnterZone(), Is.True);
            yield return null;
            Assert.That(_map.Level, Is.EqualTo(MapLevel.Zone));
            Assert.That(_map.SelectedAreaId, Is.EqualTo("room_tomb_entrance"), "The cursor starts on the hero.");
            Assert.That(root.Q<Label>("map-zone").text, Is.EqualTo("Забута Гробниця"));
            _map.MoveCursor(Vector2.right);
            Assert.That(_map.View.Node(_map.SelectedAreaId).Area.ZoneId, Is.EqualTo("zone_forgotten_tomb"),
                "The cursor stays in the region.");

            _hud.Map.Back();
            Assert.That(_map.IsOpen, Is.True);
            Assert.That(_map.Level, Is.EqualTo(MapLevel.World), "Back from a region is the world.");
            _hud.Map.Back();
            Assert.That(_map.IsOpen, Is.False, "Back from the world closes.");
            Assert.That(GamePause.IsPaused, Is.False);
        }

        [UnityTest]
        public IEnumerator TheMapIsTheFirstTabOfThePausesWindow()
        {
            _hud.Pause.Open();
            _hud.Pause.OpenMap();
            yield return null;
            Assert.That(_map.IsOpen, Is.True);

            _hud.Map.Turn(+1);
            yield return null;
            Assert.That(_map.IsOpen, Is.False);
            Assert.That(_hud.Inventory.IsOpen, Is.True);
            Assert.That(_hud.Inventory.CurrentPage, Is.EqualTo(InventoryView.Page.Items), "After the map, the items.");
            Assert.That(_hud.Pause.IsOpen, Is.False, "One window at a time.");

            _hud.Inventory.TurnPage(-1);
            yield return null;
            Assert.That(_hud.Inventory.IsOpen, Is.False);
            Assert.That(_map.IsOpen, Is.True, "Before the items, the map.");
            Assert.That(GamePause.IsPaused, Is.True);

            _hud.Map.Turn(-1);
            yield return null;
            Assert.That(_hud.Inventory.CurrentPage, Is.EqualTo(InventoryView.Page.Bestiary), "Before the map, the bestiary.");
            _hud.Inventory.Close();
            yield return null;
            Assert.That(_hud.Pause.IsOpen, Is.True, "Closed, the window still returns to the pause it came from.");
        }
    }
}
