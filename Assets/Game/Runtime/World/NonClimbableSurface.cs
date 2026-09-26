using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Marks a piece of level geometry the player cannot grip: no wall slide, wall jump or wall
    /// run off its sides. It is still solid and still something to stand on.
    ///
    /// This exists for ability gates. A high ledge meant for the second jump has short vertical
    /// faces, and a wall run up a face barely taller than the player would reach it long before the
    /// Warden hands the second jump over. Polished marble and wet rotten wood say "no purchase"
    /// visually; this makes the physics agree.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class NonClimbableSurface : MonoBehaviour
    {
    }
}
