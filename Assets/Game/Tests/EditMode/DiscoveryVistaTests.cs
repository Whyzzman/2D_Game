using NUnit.Framework;
using UnityEditor;

namespace ForgottenKingdom.Tests
{
    /// <summary>The first look out from a height has its own phrase (GAME_FEEL_IDEAS item 16, 2026-11-12).</summary>
    public sealed class DiscoveryVistaTests
    {
        [Test]
        public void EveryVistaIsARealOpenRoomAndEarnsItsPhrase()
        {
            var graph = AssetDatabase.LoadAssetAtPath<WorldGraph>("Assets/Game/Data/WorldGraph.asset");
            Assert.That(graph, Is.Not.Null);
            Assert.That(DiscoveryStingers.Vistas.Count, Is.GreaterThanOrEqualTo(4));
            foreach (var id in DiscoveryStingers.Vistas)
            {
                var area = graph.Area(id);
                Assert.That(area, Is.Not.Null, $"{id} is a room of the world.");
                Assert.That(area.IsSecret, Is.False, $"{id}: a secret room has its own phrase.");
                Assert.That(DiscoveryStingers.SoundFor(WorldFlag.AreaDiscovered, id, graph),
                    Is.EqualTo(AudioId.DiscoveryVista), id);
            }

            Assert.That(DiscoveryStingers.SoundFor(WorldFlag.AreaDiscovered, "room_tomb_entrance", graph),
                Is.Null, "An ordinary room is quiet.");
        }
    }
}
