using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Switches its object off in a release build.
    ///
    /// The level carries things that exist for making the game rather than for playing it: debug
    /// readouts on F1 to F12, training dummies, a sparring partner. They have to be in the real
    /// scene — the automated tests fight the dummies, and a debug overlay that only works in a
    /// separate test level is not much of a debug overlay. But a player who launches the game
    /// should never meet any of it.
    ///
    /// <c>Debug.isDebugBuild</c> is true in the Editor and in a development build, and false in a
    /// release one, which is exactly the line wanted here: tests keep their dummies, players do
    /// not get a straw man standing in the opening corridor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeveloperOnly : MonoBehaviour
    {
        [Tooltip("What this is for, so a later reader knows whether it may be removed.")]
        [SerializeField] private string _purpose = string.Empty;

        public string Purpose => _purpose;

        /// <summary>True when developer-only content should exist at all.</summary>
        public static bool IsDeveloperBuild => Debug.isDebugBuild || Application.isEditor;

        private void Awake()
        {
            if (!IsDeveloperBuild)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
