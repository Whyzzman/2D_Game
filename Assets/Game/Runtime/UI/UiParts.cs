using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>Small element builders the inventory window's pages share.</summary>
    internal static class UiParts
    {
        public static Label Text(string text, string className)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            return label;
        }

        public static VisualElement Marker(string className)
        {
            var marker = new VisualElement { pickingMode = PickingMode.Ignore };
            marker.AddToClassList(className);
            return marker;
        }

        public static void Show(VisualElement element, bool visible)
        {
            if (element != null)
            {
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>A "label ...... value" line of a stats table (ui.css .stats).</summary>
        public static void StatRow(VisualElement into, string label, string value)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("stat-row");
            row.Add(Text(label, "stat-row__label"));
            row.Add(Text(value, "stat-row__value"));
            into.Add(row);
        }

        /// <summary>The derived numbers both the equipment and the character pages show.</summary>
        public static void DerivedStats(VisualElement into, PlayerStats stats, bool withLoad)
        {
            into.Clear();
            if (stats == null)
            {
                return;
            }

            StatRow(into, "Атака", stats.ActiveWeaponAttack.ToString("0"));
            StatRow(into, "Захист", stats.PhysicalDefense.ToString("0.#"));
            StatRow(into, "Макс. здоров'я", stats.MaxHealth.ToString("0"));
            StatRow(into, "Макс. мана", stats.MaxMana.ToString("0"));
            StatRow(into, "Макс. витривалість", stats.MaxStamina.ToString("0"));
            StatRow(into, "Опір рівноваги", (stats.StaggerResistance * 100f).ToString("0") + "%");
            if (withLoad)
            {
                StatRow(into, "Межа навантаження", stats.MaxEquipLoad.ToString("0.0"));
            }
        }
    }
}
