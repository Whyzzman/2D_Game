using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The story's stones (item 5 of the big list, stage A): seven along the main path, each
    /// standing on its floor, read with interact, put down by walking away.
    /// </summary>
    public sealed class InscriptionTests
    {
        private PlayerCharacter _player;
        private PlayerMotor2D _motor;
        private HudController _hud;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.Delete();
            yield return TestWorld.Load();
            yield return TestWorld.LoadEveryZone();
            _player = Object.FindAnyObjectByType<PlayerCharacter>();
            _motor = _player.GetComponent<PlayerMotor2D>();
            _hud = Object.FindAnyObjectByType<HudController>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Inscription.Close();
            SaveSystem.Delete();
            yield return null;
        }

        private static IEnumerator Realtime(float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SevenStonesStandOnTheirFloorsWithWordsOnThem()
        {
            var stones = Object.FindObjectsByType<Inscription>().OrderBy(s => s.transform.position.x).ToArray();
            Assert.That(stones.Length, Is.EqualTo(7));
            foreach (var stone in stones)
            {
                var at = (Vector2)stone.transform.position;
                Assert.That(stone.Text, Is.Not.Empty, stone.name);
                var floor = Physics2D.RaycastAll(at + Vector2.up * 0.3f, Vector2.down, 0.7f)
                    .Where(h => !h.collider.isTrigger).ToArray();
                Assert.That(floor, Is.Not.Empty, $"{stone.name} at {at} stands on something solid.");
                var buried = Physics2D.OverlapPointAll(at + Vector2.up * 0.6f).Any(c => !c.isTrigger);
                Assert.That(buried, Is.False, $"{stone.name} at {at} is not inside a wall.");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ReadingShowsTheWordsAndWalkingAwayPutsThemDown()
        {
            var stone = Object.FindObjectsByType<Inscription>().OrderBy(s => s.transform.position.x).First();
            var interactor = _player.GetComponentInChildren<PlayerInteractor>();
            _motor.ResetTo((Vector2)stone.transform.position + new Vector2(0.6f, 0.9f));
            yield return Realtime(0.3f);
            Assert.That(interactor.CurrentPrompt, Is.EqualTo("Читати"));

            stone.Interact(_player);
            yield return null;
            Assert.That(Inscription.Current, Is.SameAs(stone));
            Assert.That(_hud.InscriptionPanel.IsOpen, Is.True);
            Assert.That(_hud.InscriptionPanel.ShownText, Is.EqualTo(stone.Text));
            Assert.That(Time.timeScale, Is.GreaterThan(0f), "Reading does not stop the world.");

            _motor.ResetTo((Vector2)stone.transform.position + new Vector2(Inscription.ReadRange + 2f, 0.9f));
            yield return Realtime(0.2f);
            Assert.That(Inscription.Current, Is.Null, "Walking away puts the stone down.");
            Assert.That(_hud.InscriptionPanel.IsOpen, Is.False);
        }
    }
}
