using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// "An ability was just earned in the world" — a boss reward or a pickup, never a load, a
    /// debug key or a test granting it through <see cref="GameProgress"/>. The HUD answers with
    /// the ability card (design/ui/tutorial/tutorial.html), which pauses the game; announcing
    /// from UnlockAbility itself would freeze every test that grants an ability directly.
    /// </summary>
    public static class AbilityAnnouncements
    {
        public static event Action<string> Announced;

        public static void Announce(string abilityId)
        {
            if (!string.IsNullOrEmpty(abilityId))
            {
                Announced?.Invoke(abilityId);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => Announced = null;
    }
}
