using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The «Люди» page of the inventory window, from design/ui/dialogue/dialogue.html (frame 5):
    /// everyone the hero has met, where they stand now and the last thing they said, newest
    /// meeting first. A character not met yet is not listed at all.
    /// </summary>
    public sealed class PeoplePage
    {
        private readonly VisualElement _page;
        private readonly ScrollView _list;
        private GameProgress _progress;

        internal PeoplePage(VisualElement page)
        {
            _page = page;
            _list = page?.Q<ScrollView>("ppl-list");
        }

        /// <summary>The names listed now, for tests.</summary>
        public List<string> Shown { get; } = new List<string>();

        public void Attach(GameProgress progress) => _progress = progress;

        public void Draw()
        {
            Shown.Clear();
            if (_list == null)
            {
                return;
            }

            _list.Clear();
            var seen = new HashSet<NpcDefinition>();
            foreach (var npc in Object.FindObjectsByType<Npc>(FindObjectsInactive.Include))
            {
                var definition = npc.Definition;
                if (definition == null || _progress == null || !_progress.HasReward(definition.MetId))
                {
                    continue;
                }

                // Several placements of one character: the one standing somewhere now names the place.
                if (!npc.IsPresent && HasPresentPlacement(definition))
                {
                    continue;
                }

                if (!seen.Add(definition))
                {
                    continue;
                }

                var person = new VisualElement { pickingMode = PickingMode.Ignore };
                person.AddToClassList("ppl__person");
                person.Add(UiParts.Text(npc.IsPresent ? npc.PlaceName.ToUpperInvariant() : "НЕВІДОМО ДЕ", "ppl__place"));
                person.Add(UiParts.Text(definition.DisplayName, "ppl__name"));
                var note = definition.LatestNote(_progress);
                person.Add(UiParts.Text(string.IsNullOrEmpty(note) ? definition.Epithet : note, "ppl__note"));
                _list.Add(person);
                Shown.Add(definition.DisplayName);
            }

            _page?.EnableInClassList("ppl--empty", Shown.Count == 0);
        }

        private static bool HasPresentPlacement(NpcDefinition definition)
        {
            foreach (var npc in Object.FindObjectsByType<Npc>(FindObjectsInactive.Include))
            {
                if (npc.Definition == definition && npc.IsPresent)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
