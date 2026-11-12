using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Automated runs of a build (<c>-perfProbe</c>, <c>-screenshotTour</c>) and any run started with
    /// <c>-mute</c> make no sound: they run on the user's Mac while they work (user's request,
    /// 2026-11-12). Only the listener's volume is touched — the mix and the saved settings stay as they are,
    /// and nothing else in the game writes <see cref="AudioListener.volume"/>.
    /// </summary>
    public static class SilentRun
    {
        private static readonly string[] Flags = { "-mute", "-perfProbe", "-screenshotTour" };

        public static bool Requested(string[] args)
        {
            foreach (var flag in Flags)
            {
                if (Array.IndexOf(args, flag) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void MuteIfRequested()
        {
            if (Requested(Environment.GetCommandLineArgs()))
            {
                AudioListener.volume = 0f;
            }
        }
    }
}
