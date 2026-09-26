using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// One debug panel open at a time, and a panel's keys work only while it is open.
    ///
    /// The six developer panels grew their hotkeys separately and ended up sharing them: F2 sent
    /// the player to the altar and toggled the animation panel, F11 deleted the save and switched
    /// parallax, and with every panel closed 1 to 5 still handed out gold and items on the same
    /// keys as the skills and spells. Now a closed panel reads only its own open key, an open one
    /// reads its keys, and while one is open the others do not listen at all — not even to their
    /// open keys, which is what would otherwise collide with the open panel's actions.
    /// </summary>
    public static class DebugPanels
    {
        private static Object _open;

        /// <summary>The panel that currently owns the debug keys, if any.</summary>
        public static Object Open => _open;

        /// <summary>A panel may read its open key only when no other panel is open.</summary>
        public static bool MayToggle(Object panel) => _open == null || _open == panel;

        /// <summary>Every panel reports its state here when it opens, closes or goes away.</summary>
        public static void Report(Object panel, bool isOpen)
        {
            if (isOpen)
            {
                _open = panel;
            }
            else if (_open == panel)
            {
                _open = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => _open = null;
    }
}
