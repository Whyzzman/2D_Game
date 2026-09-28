using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Loads the game the way the menu does and waits for the world to stand: since item 3 the
    /// level is in zone scenes that <see cref="ZoneStreamer"/> loads a frame after the world scene,
    /// and the player is placed only once they are in.
    /// </summary>
    internal static class TestWorld
    {
        public static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(MainMenuController.GameSceneName);
            yield return WaitReady();
        }

        /// <summary>Until the zones are in and settled; at once for a scene without a streamer.</summary>
        public static IEnumerator WaitReady(float timeout = 15f)
        {
            var deadline = Time.realtimeSinceStartup + timeout;
            while (ZoneStreamer.Current != null && (!ZoneStreamer.Current.IsReady || ZoneStreamer.Current.IsBusy)
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(ZoneStreamer.Current == null || ZoneStreamer.Current.IsReady, Is.True,
                "The zones never finished loading.");
        }
    }
}
