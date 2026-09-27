using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>How the camera's rooms are cut from the world graph and shaped by the level.</summary>
    public sealed class CameraRoomsTests
    {
        private static readonly Rect Hall = Rect.MinMaxRect(0f, -8f, 20f, 18f);

        private static Bounds Box(float xMin, float yMin, float xMax, float yMax)
        {
            var bounds = new Bounds();
            bounds.SetMinMax(new Vector3(xMin, yMin), new Vector3(xMax, yMax));
            return bounds;
        }

        [Test]
        public void TheViewSitsJustUnderTheFloorAndLeavesHeadroomOverTheHighestLedge()
        {
            var view = CameraRooms.ViewOf(Hall, new[] { Box(-5f, -1f, 25f, 0f), Box(4f, 3f, 8f, 3.5f) });
            Assert.That(view.xMin, Is.EqualTo(0f), "The sides are the area's.");
            Assert.That(view.xMax, Is.EqualTo(20f));
            Assert.That(view.yMin, Is.EqualTo(-1f - CameraRooms.FloorMargin).Within(0.001f));
            Assert.That(view.yMax, Is.EqualTo(3.5f + CameraRooms.Headroom).Within(0.001f));
        }

        [Test]
        public void GeometryReachingPastTheAreaIsCutToIt()
        {
            var view = CameraRooms.ViewOf(Hall, new[] { Box(0f, -30f, 1f, 40f) });
            Assert.That(view.yMin, Is.EqualTo(Hall.yMin));
            Assert.That(view.yMax, Is.EqualTo(Hall.yMax));
        }

        [Test]
        public void AnAreaWithNoGeometryKeepsItsOwnShape()
        {
            Assert.That(CameraRooms.ViewOf(Hall, new Bounds[0]), Is.EqualTo(Hall));
        }

        [Test]
        public void ANookInsideAHallBelongsToTheHallsView()
        {
            var areas = new List<(string, Rect)>
            {
                ("hall", Hall),
                ("nook", Rect.MinMaxRect(3f, 4f, 5f, 6f)),
                ("next", Rect.MinMaxRect(20f, -8f, 40f, 18f)),
                ("above", Rect.MinMaxRect(22f, 18f, 30f, 24f))
            };
            var rooms = CameraRooms.Build(areas, _ => new Bounds[0]);
            Assert.That(rooms.Count, Is.EqualTo(3));
            Assert.That(rooms.IndexOf("nook"), Is.EqualTo(-1));
            Assert.That(rooms.IndexOf("above"), Is.GreaterThanOrEqualTo(0), "Stacked rooms are rooms.");
        }

        [Test]
        public void RoomsNarrowerThanTheViewAreJoinedToTheirNarrowerNeighbour()
        {
            var areas = new List<(string, Rect)>
            {
                ("wide", Rect.MinMaxRect(-30f, -8f, 0f, 18f)),
                ("narrow", Rect.MinMaxRect(0f, -8f, 8f, 18f)),
                ("small", Rect.MinMaxRect(8f, -8f, 14f, 18f)),
                ("arena", Rect.MinMaxRect(14f, -8f, 30f, 18f))
            };
            var rooms = CameraRooms.Build(areas, _ => new Bounds[0], 20f, id => id == "arena");

            Assert.That(rooms.IndexOf("narrow"), Is.EqualTo(rooms.IndexOf("small")),
                "Two narrow rooms side by side become one.");
            Assert.That(rooms.Rooms[rooms.IndexOf("small")].View.width, Is.GreaterThanOrEqualTo(20f)
                .Or.EqualTo(14f), "Joined until wide enough, or as wide as its neighbours allow.");
            Assert.That(rooms.IndexOf("arena"), Is.Not.EqualTo(rooms.IndexOf("small")),
                "A boss arena keeps its own frame, however narrow.");
            Assert.That(rooms.Rooms[rooms.IndexOf("arena")].AreaIds, Is.EqualTo(new[] { "arena" }));
            Assert.That(rooms.IndexOf("wide"), Is.EqualTo(rooms.IndexOf("small")),
                "Still narrower than the view, the pair joins the wide room beside it.");
        }

        [Test]
        public void RoomsWideEnoughStayApart()
        {
            var areas = new List<(string, Rect)>
            {
                ("a", Rect.MinMaxRect(0f, -8f, 25f, 18f)),
                ("b", Rect.MinMaxRect(25f, -8f, 50f, 18f))
            };
            var rooms = CameraRooms.Build(areas, _ => new Bounds[0], 20f);
            Assert.That(rooms.Count, Is.EqualTo(2));
        }

        [Test]
        public void StandingOnASharedEdgeDoesNotSwingTheView()
        {
            var areas = new List<(string, Rect)> { ("hall", Hall), ("next", Rect.MinMaxRect(20f, -8f, 40f, 18f)) };
            var rooms = CameraRooms.Build(areas, _ => new Bounds[0]);
            var hall = rooms.IndexOf("hall");
            var next = rooms.IndexOf("next");

            Assert.That(rooms.RoomAt(new Vector2(20.3f, 0f), hall), Is.EqualTo(hall), "Just over the edge.");
            Assert.That(rooms.RoomAt(new Vector2(21f, 0f), hall), Is.EqualTo(next), "Clearly in the next one.");
            Assert.That(rooms.RoomAt(new Vector2(19.7f, 0f), next), Is.EqualTo(next), "And back again.");
            Assert.That(rooms.RoomAt(new Vector2(-50f, 0f), next), Is.EqualTo(next), "Out of every room.");
            Assert.That(rooms.RoomAt(new Vector2(5f, 0f), -1), Is.EqualTo(hall));
        }
    }
}
