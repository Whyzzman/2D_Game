using NUnit.Framework;
using UnityEditor;

namespace ForgottenKingdom.Tests
{
    /// <summary>Every boss in the bestiary has its line of help (2026-11-12).</summary>
    public sealed class BossHintsTests
    {
        [Test]
        public void EveryBossOfTheBestiaryHasAHint()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BestiaryCatalog>("Assets/Game/Data/BestiaryCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Bosses.Count, Is.GreaterThanOrEqualTo(6));
            foreach (var boss in catalog.Bosses)
            {
                Assert.That(BossHints.For(boss.BossId), Is.Not.Empty, boss.BossId);
            }

            Assert.That(BossHints.For("nobody"), Is.Empty);
        }
    }
}
