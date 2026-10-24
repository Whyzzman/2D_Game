using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Hero animation 2.0 on the player prefab (the user's choices of 2026-09-28): the lean into a
    /// speed change and the cold silver silhouettes a dash leaves. The smear, the hard landing and
    /// the breath are in the frames (<see cref="PlaceholderPlayerArt"/>); the squash and the fall
    /// stretch are <see cref="MovementFeedback"/>'s.
    /// </summary>
    public static partial class ContentBuilder
    {
        private static void BuildHeroMotion(GameObject root, PlayerAnimationController controller,
            SpriteRenderer body)
        {
            var lean = Ensure<HeroLean>(root);
            SetReference(lean, "_motor", root.GetComponent<PlayerMotor2D>());
            SetReference(lean, "_animation", controller);
            SetReference(lean, "_body", body.transform);

            var afterimages = Ensure<DashAfterimages>(root);
            SetReference(afterimages, "_animation", controller);
            SetReference(afterimages, "_body", body);
            SetReference(afterimages, "_material", HitFlashMaterial());

            // The death as a moment: slow, darken, come apart into shadows; gather at the altar.
            var death = Ensure<HeroDeath>(root);
            SetReference(death, "_player", root.GetComponent<PlayerCharacter>());
            SetReference(death, "_body", body);
            SetReference(death, "_material", HitFlashMaterial());
        }
    }
}
