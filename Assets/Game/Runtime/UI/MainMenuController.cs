using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The title screen: Continue, New Game, Settings, Quit over a lit view of the Tomb.
    ///
    /// Continue is disabled, not hidden, when there is no save (design/ui/menus). New Game deletes
    /// only the progress file — settings live in their own file and survive it. Keyboard and
    /// gamepad move a focus of their own, so the menu does not depend on an EventSystem; the
    /// mouse uses the buttons directly.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        public const string SceneName = "MainMenu";
        public const string GameSceneName = "TombTrial";

        [SerializeField] private Sprite _vignette;
        [SerializeField] private Sprite _ornament;
        [SerializeField] private string _buildLabel = "Вертикальний зріз";

        private readonly List<VisualElement> _items = new List<VisualElement>();
        private readonly List<System.Action> _actions = new List<System.Action>();
        private VisualElement _root;
        private VisualElement _fade;
        private OptionsPanel _options;
        private Button _continue;
        private int _focus;
        private bool _isLeaving;
        private MenuInput.Repeater _repeater;

        public bool IsOptionsOpen => _options != null && _options.IsOpen;
        public bool CanContinue => _continue != null && _continue.enabledSelf;

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            if (_root == null)
            {
                return;
            }

            GameTimeScale.Reset();
            _fade = _root.Q("fade");
            _continue = _root.Q<Button>("continue");
            Dress();
            BindButtons();
            _options = new OptionsPanel(_root, OnOptionsClosed);
            Refresh();
            // Opened by the next frame's layout, so the transition actually runs.
            _root.schedule.Execute(() => _fade?.AddToClassList("menu__fade--open")).ExecuteLater(50);
        }

        private void Dress()
        {
            if (_vignette != null)
            {
                var vignette = _root.Q("vignette");
                if (vignette != null) { vignette.style.backgroundImage = new StyleBackground(_vignette); }
            }

            if (_ornament != null)
            {
                _root.Query(className: "ornament").ForEach(
                    e => e.style.backgroundImage = new StyleBackground(_ornament));
            }

            if (HudFonts.Body != null)
            {
                _root.style.unityFontDefinition = FontDefinition.FromFont(HudFonts.Body);
            }

            if (HudFonts.Display != null)
            {
                _root.Query(className: "display").ForEach(
                    e => e.style.unityFontDefinition = FontDefinition.FromFont(HudFonts.Display));
            }

            var tag = _root.Q<Label>("build-tag");
            if (tag != null) { tag.text = $"{_buildLabel} · {Application.version}"; }
        }

        private void BindButtons()
        {
            Bind("continue", Continue);
            Bind("new-game", NewGame);
            Bind("options", OpenOptions);
            Bind("quit", Quit);
        }

        private void Bind(string name, System.Action action)
        {
            var button = _root.Q<Button>(name);
            if (button == null)
            {
                return;
            }

            button.clicked += action;
            var index = _items.Count;
            _items.Add(button);
            _actions.Add(action);
            button.RegisterCallback<PointerEnterEvent>(_ => { if (button.enabledSelf) { SetFocus(index); } });
        }

        /// <summary>Re-reads the save; Continue follows whether there is anything to continue.</summary>
        public void Refresh()
        {
            var hasSave = SaveSystem.HasValidSave();
            _continue?.SetEnabled(hasSave);
            var meta = _root?.Q<Label>("save-meta");
            if (meta != null)
            {
                meta.text = hasSave ? DescribeSave() : "Збереження не знайдено";
            }

            SetFocus(hasSave ? 0 : 1);
        }

        private static string DescribeSave()
        {
            var data = SaveSystem.Read();
            if (data == null)
            {
                return "Збереження знайдено";
            }

            var minutes = Mathf.FloorToInt(data.totalPlayTimeSeconds / 60f);
            return minutes >= 60
                ? $"Збереження · {minutes / 60} год {minutes % 60} хв"
                : $"Збереження · {minutes} хв";
        }

        private void Update()
        {
            if (_root == null || _isLeaving)
            {
                return;
            }

            if (IsOptionsOpen)
            {
                if (MenuInput.Cancelled())
                {
                    CloseOptions();
                    return;
                }

                _options.Tick();
                return;
            }

            var step = _repeater.Step(MenuInput.ReadVertical());
            if (step != 0)
            {
                MoveFocus(step);
            }

            if (MenuInput.Confirmed() && _focus >= 0 && _focus < _items.Count
                && _items[_focus].enabledSelf)
            {
                _actions[_focus]();
            }
        }

        private void MoveFocus(int step)
        {
            // Skips a disabled Continue rather than landing on it.
            for (var i = 1; i <= _items.Count; i++)
            {
                var next = (_focus + step * i + _items.Count * 4) % _items.Count;
                if (_items[next].enabledSelf)
                {
                    SetFocus(next);
                    return;
                }
            }
        }

        private void SetFocus(int index)
        {
            _focus = Mathf.Clamp(index, 0, Mathf.Max(0, _items.Count - 1));
            for (var i = 0; i < _items.Count; i++)
            {
                _items[i].EnableInClassList("menu-button--focused", i == _focus);
            }
        }

        public void Continue()
        {
            if (!SaveSystem.HasValidSave())
            {
                return;
            }

            StartCoroutine(Leave(false));
        }

        public void NewGame()
        {
            StartCoroutine(Leave(true));
        }

        public void OpenOptions()
        {
            _options?.Open();
            _root.Q("front")?.style.SetOpacity(0.15f);
        }

        public void CloseOptions()
        {
            _options?.Close();
        }

        /// <summary>The panel saved the settings on its way out; bring the title list back.</summary>
        private void OnOptionsClosed()
        {
            _root.Q("front")?.style.SetOpacity(1f);
        }

        public void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private IEnumerator Leave(bool newGame)
        {
            if (_isLeaving)
            {
                yield break;
            }

            _isLeaving = true;
            _fade?.AddToClassList("menu__fade--leaving");
            yield return new WaitForSecondsRealtime(0.95f);
            if (newGame)
            {
                // Progress only: accessibility settings are in their own file and stay.
                SaveSystem.Delete();
            }

            SceneManager.LoadScene(GameSceneName);
        }
    }

    internal static class StyleExtensions
    {
        public static void SetOpacity(this IStyle style, float value) => style.opacity = value;
    }
}
