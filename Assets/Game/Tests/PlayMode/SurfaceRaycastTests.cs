using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The ground under the feet asked through physics (GAME_FEEL_IDEAS item 4). In Play Mode,
    /// where 2D queries see the colliders; Edit Mode ones found nothing under the plank.
    /// </summary>
    public sealed class SurfaceRaycastTests
    {
        [UnityTest]
        public IEnumerator ATaggedBlockUnderTheFeetWinsOverTheZone()
        {
            var block = new GameObject("Plank") { layer = 9 }; // Ground; the layers have no names
            block.transform.position = new Vector3(1000f, 1000f, 0f);
            block.AddComponent<BoxCollider2D>().size = new Vector2(4f, 1f);
            block.AddComponent<GroundSurface>().Set(SurfaceKind.Wood);
            try
            {
                yield return new WaitForFixedUpdate();
                Assert.That(Surfaces.Under(new Vector2(1000f, 1000.5f)), Is.EqualTo(SurfaceKind.Wood));
                Assert.That(Surfaces.Under(new Vector2(1010f, 1000.5f)), Is.EqualTo(SurfaceKind.Stone),
                    "Off the plank, over nothing, the zone's default (none here: stone).");
            }
            finally
            {
                Object.Destroy(block);
            }
        }
    }
}
