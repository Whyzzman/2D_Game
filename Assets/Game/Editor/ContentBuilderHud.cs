using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The runtime HUD: one UI Toolkit panel over the game, built from Hud.uxml / Hud.uss and fed
    /// by <see cref="HudController"/>. It replaces the "no HUD at all" of earlier phases, where the
    /// only on-screen state was the F1 debug readout.
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string UiFolder = "Assets/Game/UI";
        private const string ThemePath = UiFolder + "/GameTheme.tss";
        private const string PanelSettingsPath = UiFolder + "/GamePanelSettings.asset";
        private const string HudUxmlPath = UiFolder + "/Hud/Hud.uxml";

        /// <summary>Theme and panel settings; created once and updated in place to keep GUIDs.</summary>
        private static void BuildHudAssets()
        {
            IconArt.Generate();
            IconArt.GenerateFrames();
            EnsureFolder(UiFolder);

            const string theme = "@import url(\"unity-theme://default\");\n";
            var themeFile = Path.GetFullPath(ThemePath);
            if (!File.Exists(themeFile) || File.ReadAllText(themeFile) != theme)
            {
                File.WriteAllText(themeFile, theme);
                AssetDatabase.ImportAsset(ThemePath, ImportAssetOptions.ForceUpdate);
            }

            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            }

            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            // The HTML is drawn for 1920x1080; the panel scales with the screen the same way.
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.sortingOrder = 10;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static void BuildHud(GameObject systems, GameObject player, PlayerRespawner respawner,
            GameProgress progress, PlayerSpellbook spellbook)
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudUxmlPath);
            if (settings == null || tree == null)
            {
                Debug.LogError("The HUD panel settings or Hud.uxml are missing; the game would "
                    + "have no HUD.");
                return;
            }

            var hudObject = new GameObject("HUD");
            hudObject.transform.SetParent(systems.transform, false);
            var document = hudObject.AddComponent<UIDocument>();
            document.panelSettings = settings;
            document.visualTreeAsset = tree;

            var hud = hudObject.AddComponent<HudController>();
            SetReference(hud, "_player", player.GetComponent<PlayerCharacter>());
            SetReference(hud, "_mana", player.GetComponent<Mana>());
            SetReference(hud, "_equipment", player.GetComponent<PlayerEquipment>());
            SetReference(hud, "_skills", player.GetComponent<ActiveSkillController>());
            SetReference(hud, "_caster", player.GetComponent<SpellCaster>());
            SetReference(hud, "_interactor", player.GetComponentInChildren<PlayerInteractor>());
            SetReference(hud, "_spellbook", spellbook);
            SetReference(hud, "_respawner", respawner);
            SetReference(hud, "_progress", progress);
            SetReference(hud, "_world", systems.GetComponent<WorldState>());

            // Loaded by path here, after the new scene: handles from before it are dead.
            SetHudArt(hud, "barFrame", $"{IconArt.FrameRoot}/ui_bar_frame.png");
            SetHudArt(hud, "barFrameThin", $"{IconArt.FrameRoot}/ui_bar_frame_thin.png");
            SetHudArt(hud, "slotFrame", $"{IconArt.FrameRoot}/ui_slot_frame.png");
            SetHudArt(hud, "sheen", $"{IconArt.FrameRoot}/ui_sheen.png");
            SetHudArt(hud, "vignette", $"{IconArt.FrameRoot}/ui_vignette.png");
            SetHudArt(hud, "ornament", $"{IconArt.FrameRoot}/ui_ornament.png");
            foreach (var icon in new[] { "sword", "greatsword", "warrior", "hunter", "mage",
                "shadow", "fire", "frost", "arcane", "poison", "bleed", "currency" })
            {
                SetHudArt(hud, icon, $"{IconArt.Root}/icon_{icon}.png");
            }
        }

        private static void SetHudArt(HudController hud, string field, string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError($"HUD art '{path}' is missing.");
                return;
            }

            SetReference(hud, $"_art.{field}", sprite);
        }
    }
}
