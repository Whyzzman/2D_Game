using NUnit.Framework;

namespace ForgottenKingdom.Tests
{
    /// <summary>Vessel shards as ids (item 4 of the big list): what counts as which.</summary>
    public sealed class VesselShardsTests
    {
        [Test]
        public void ShardIdsNameTheirVessel()
        {
            Assert.That(VesselShards.TryKind(VesselShards.HealthPrefix + "x", out var health), Is.True);
            Assert.That(health, Is.EqualTo(VesselKind.Health));
            Assert.That(VesselShards.TryKind(VesselShards.ManaPrefix + "x", out var mana), Is.True);
            Assert.That(mana, Is.EqualTo(VesselKind.Mana));
            Assert.That(VesselShards.TryKind(MerchantStock.FlaskShardPrefix + "x", out _), Is.False);
            Assert.That(VesselShards.TryKind(string.Empty, out _), Is.False);
        }

        [Test]
        public void AVesselIsFourShardsOfHealthOrThreeOfMana()
        {
            Assert.That(VesselShards.PerVessel(VesselKind.Health), Is.EqualTo(4));
            Assert.That(VesselShards.PerVessel(VesselKind.Mana), Is.EqualTo(3));
            Assert.That(VesselShards.Multiplier(null, VesselKind.Health), Is.EqualTo(1f));
        }
    }
}
