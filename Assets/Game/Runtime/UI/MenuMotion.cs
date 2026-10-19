using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The menus' entrances (design/ui/menus/menus.html, UI_UX_GUIDE.md §12). An element marked
    /// <c>motion</c> in USS starts low, faint and blurred and transitions in once it is given
    /// <see cref="Shown"/>; adding that class a frame after the element is displayed is what lets
    /// the transition run instead of snapping. Lists unfold item by item, 45 ms apart.
    /// With reduced motion everything is shown at once.
    /// </summary>
    public static class MenuMotion
    {
        public const string Shown = "motion--shown";
        private const long FirstFrameMs = 16;
        private const long StepMs = 45;

        /// <summary>Shows one element with its transition, from wherever it is now.</summary>
        public static void Reveal(VisualElement element, long delayMs = 0)
        {
            if (element == null)
            {
                return;
            }

            if (FeedbackSettings.ReducedMotion)
            {
                element.AddToClassList(Shown);
                return;
            }

            element.RemoveFromClassList(Shown);
            element.schedule.Execute(() => element.AddToClassList(Shown)).ExecuteLater(FirstFrameMs + delayMs);
        }

        /// <summary>Shows a list one item after another, starting after <paramref name="startMs"/>.</summary>
        public static void Stagger(IEnumerable<VisualElement> elements, long startMs = 120)
        {
            var delay = startMs;
            foreach (var element in elements)
            {
                Reveal(element, delay);
                delay += StepMs;
            }
        }

        /// <summary>Back to the hidden look, without a transition being waited for.</summary>
        public static void Hide(VisualElement element) => element?.RemoveFromClassList(Shown);
    }
}
