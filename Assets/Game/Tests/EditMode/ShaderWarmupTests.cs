using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForgottenKingdom.Tests
{
    /// <summary>KI-012: the game warms only a collection recorded on its own graphics API and platform.</summary>
    public sealed class ShaderWarmupTests
    {
        [Test]
        public void OnlyACollectionForThisApiAndPlatformIsWarmed()
        {
            Assert.That(ShaderWarmup.Pick(null, GraphicsDeviceType.Metal, RuntimePlatform.OSXPlayer), Is.Null);
            Assert.That(ShaderWarmup.Pick(new GraphicsStateCollection[0], GraphicsDeviceType.Metal,
                RuntimePlatform.OSXPlayer), Is.Null);

            var recorded = new GraphicsStateCollection();
            var picked = ShaderWarmup.Pick(new[] { recorded }, GraphicsDeviceType.Direct3D12,
                RuntimePlatform.WindowsPlayer);
            Assert.That(picked == null || picked.graphicsDeviceType == GraphicsDeviceType.Direct3D12, Is.True,
                "A collection from another API is never picked.");
        }
    }
}
