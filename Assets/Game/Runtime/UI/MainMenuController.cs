using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The title screen: Continue, New Game, Settings, Quit over a lit view of the Tomb
    /// (design/ui/menus/menus.html, 2026-10-19). The title and the list unfold one line after
    /// another (<see cref="MenuMotion"/>), the focused word carries the light and the crown's
    /// flame (<see cref="MenuFocus"/>), and the camera behind breathes (<see cref="MenuCameraDrift"/>).
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
        [SerializeField] private string _buildLabel = string.Empty;

        private readonly List<VisualElement> _items = new List<VisualElement>();
        private readonly List<System.Action> _actions = new List<System.Action>();
        private VisualElement _root;
        private VisualElement _fade;
        private VisualElement _front;
        private MenuFocus _focusMarks;
        private OptionsPanel _options;
        private ControlsPanel _controls;
        private Button _continue;
        private int _focus;
        private bool _isLeaving;
        private MenuInput.Repeater _repeater;
        private VisualElement _confirm;
        private readonly List<Button> _confirmButtons = new List<Button>();
        private int _confirmFocus;
        private bool _confirmPlus;

        public bool IsOptionsOpen => _options != null && _options.IsOpen;
        public bool IsControlsOpen => _controls != null && _controls.IsOpen;
        public ControlsPanel Controls => _controls;
        public bool CanContinue => _continue != null && _continue.enabledSelf;
        public bool CanStartNewGamePlus => _root?.Q<Button>("new-game-plus")?.enabledSelf == true;

        /// <summary>The "erase the save?" window over the title is up.</summary>
        public bool IsConfirmOpen => _confirm != null && _confirm.ClassListContains("confirm--open");

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            if (_root == null)
            {
                return;
            }

            GameTimeScale.Reset();
            _fade = _root.Q("fade");
            _front = _root.Q("front");
            _continue = _root.Q<Button>("continue");
            _focusMarks = new MenuFocus(_root.Q("list"));
            Dress();
            Breathe();
            BindButtons();
            _options = new OptionsPanel(_root, OnOptionsClosed);
            _controls = new ControlsPanel(_root, OnControlsClosed);
            BindConfirm();
            Refresh();
            // Opened by the next frame's layout, so the transition actually runs.
            _root.schedule.Execute(() => _fade?.AddToClassList("menu__fade--open")).ExecuteLater(50);
            // The lines unfold while the black lifts, as in the HTML's 2.2 s intro.
            if (_front != null)
            {
                MenuMotion.Stagger(_front.Query(className: "motion").ToList(), 400);
            }
        }

        /// <summary>The camera behind the menu drifts a little; added here so the scene stays as built.</summary>
        private static void Breathe()
        {
            var camera = Camera.main;
            if (camera != null && camera.GetComponent<MenuCameraDrift>() == null)
            {
                camera.gameObject.AddComponent<MenuCameraDrift>();
            }
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
            if (tag != null)
            {
                tag.text = string.IsNullOrEmpty(_buildLabel)
                    ? $"v{Application.version}"
                    : $"{_buildLabel} · {Application.version}";
            }
        }

        private void BindButtons()
        {
            Bind("continue", Continue);
            Bind("new-game-plus", NewGamePlusRun);
            Bind("new-game", NewGame);
            Bind("options", OpenOptions);
            Bind("controls", OpenControls);
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
            var plus = _root?.Q<Button>("new-game-plus");
            if (plus != null)
            {
                var completed = hasSave && SaveSystem.Read()?.completed == true;
                plus.SetEnabled(completed);
                plus.style.display = completed ? DisplayStyle.Flex : DisplayStyle.None;
            }

            var meta = _root?.Q<Label>("save-meta");
            if (meta != null)
            {
                meta.text = hasSave ? DescribeSave() : "Збереження не знайдено";
            }

            SetFocus(hasSave ? 0 : _items.FindIndex(b => b.name == "new-game"));
        }

        private static string DescribeSave()
        {
            var data = SaveSystem.Read();
            if (data == null)
            {
                return "Збереження знайдено";
            }

            var minutes = Mathf.FloorToInt(data.totalPlayTimeSeconds / 60f);
            var text = minutes >= 60
                ? $"Збереження · {minutes / 60} год {minutes % 60} хв"
                : $"Збереження · {minutes} хв";
            if (data.newGamePlus > 0) { text += data.newGamePlus > 1 ? $" · NG+{data.newGamePlus}" : " · NG+"; }
            if (data.completed) { text += " · ♛ Пройдено"; }
            return text;
        }

        private void Update()
        {
            if (_root == null || _isLeaving)
            {
                return;
            }

            if (IsConfirmOpen)
            {
                UpdateConfirmKeys();
                return;
            }

            if (IsControlsOpen)
            {
                // The Escape that cancels waiting for a key must not also leave the screen.
                if (MenuInput.Cancelled() && !_controls.SwallowsCancel)
                {
                    _controls.Back();
                    return;
                }

                _controls.Tick();
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

            if (_focus < _items.Count)
            {
                _focusMarks?.MoveTo(_items[_focus]);
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

        /// <summary>
        /// Starts over. With a save on disk it asks first (menus.html, frame 3): the save is
        /// erased for good, so a single stray press must not do it. Without one there is nothing
        /// to lose and it goes straight in.
        /// </summary>
        public void NewGame()
        {
            if (SaveSystem.HasValidSave())
            {
                OpenConfirm();
                return;
            }

            StartCoroutine(Leave(true));
        }

        /// <summary>
        /// «Нова гра+» (design/ui/ending/ending.html frames 6–7): only on a completed save, and
        /// always asked first — the finished run is replaced by the next one.
        /// </summary>
        public void NewGamePlusRun()
        {
            var data = SaveSystem.HasValidSave() ? SaveSystem.Read() : null;
            if (data == null || !data.completed)
            {
                return;
            }

            _confirmPlus = true;
            OpenConfirm();
        }

        /// <summary>The window's "erase and start" — the only way a save is deleted from here.</summary>
        public void ConfirmNewGame()
        {
            var plus = _confirmPlus;
            CloseConfirm();
            StartCoroutine(Leave(true, plus));
        }

        public void CancelNewGame() => CloseConfirm();

        private void BindConfirm()
        {
            _confirm = _root.Q("confirm-new");
            if (_confirm == null)
            {
                return;
            }

            BindConfirmButton("confirm-cancel", CancelNewGame);
            BindConfirmButton("confirm-erase", ConfirmNewGame);
        }

        private void BindConfirmButton(string name, System.Action action)
        {
            var button = _confirm.Q<Button>(name);
            if (button == null)
            {
                return;
            }

            button.clicked += action;
            var index = _confirmButtons.Count;
            _confirmButtons.Add(button);
            button.RegisterCallback<PointerEnterEvent>(_ => SetConfirmFocus(index));
        }

        private void OpenConfirm()
        {
            if (_confirm == null)
            {
                StartCoroutine(Leave(true));
                return;
            }

            var save = _confirm.Q<Label>("confirm-save");
            if (save != null) { save.text = DescribeSave(); }
            SetConfirmText("confirm-title", _confirmPlus ? "ПОЧАТИ «НОВУ ГРУ+»?" : "ПОЧАТИ НОВУ ГРУ?");
            SetConfirmText("confirm-text", _confirmPlus
                ? "Залишаться: рівень, навички, спорядження, речі, золото.\n"
                    + "Почнуться заново: здібності руху, карта, боси, секрети, бестіарій.\n"
                    + "Вороги й боси — +50% здоров'я і шкоди."
                : "Поточне проходження буде стерто назавжди.");
            SetConfirmText("confirm-erase-text", _confirmPlus ? "Почати" : "Стерти й почати");
            _confirm.AddToClassList("confirm--open");
            MenuMotion.Reveal(_confirm);
            MenuMotion.Reveal(_confirm.Q(className: "confirm__panel"));
            Dim(true);
            // Cancel first, as in the mock-up: confirming by reflex keeps the save.
            SetConfirmFocus(0);
        }

        private void CloseConfirm()
        {
            _confirmPlus = false;
            _confirm?.RemoveFromClassList("confirm--open");
            MenuMotion.Hide(_confirm);
            MenuMotion.Hide(_confirm?.Q(className: "confirm__panel"));
            Dim(false);
        }

        private void SetConfirmText(string name, string text)
        {
            var label = _confirm.Q<Label>(name);
            if (label != null) { label.text = text; }
        }

        private void UpdateConfirmKeys()
        {
            if (MenuInput.Cancelled())
            {
                CloseConfirm();
                return;
            }

            var step = _repeater.Step(MenuInput.ReadHorizontal() + MenuInput.ReadVertical());
            if (step != 0 && _confirmButtons.Count > 0)
            {
                SetConfirmFocus((_confirmFocus + 1) % _confirmButtons.Count);
            }

            if (MenuInput.Confirmed() && _confirmFocus >= 0 && _confirmFocus < _confirmButtons.Count)
            {
                if (_confirmFocus == 0) { CancelNewGame(); }
                else { ConfirmNewGame(); }
            }
        }

        private void SetConfirmFocus(int index)
        {
            _confirmFocus = index;
            for (var i = 0; i < _confirmButtons.Count; i++)
            {
                _confirmButtons[i].EnableInClassList("btn--focused", i == index);
            }
        }

        public void OpenOptions()
        {
            _options?.Open();
            Dim(true);
        }

        /// <summary>The title dims to 18 % under a panel rather than vanishing (menus.html).</summary>
        private void Dim(bool dimmed)
        {
            _front?.style.SetOpacity(dimmed ? 0.18f : 1f);
        }

        /// <summary>The same controls screen as in the pause, to set the keys before playing.</summary>
        public void OpenControls()
        {
            _controls?.Open();
            Dim(true);
        }

        private void OnControlsClosed()
        {
            Dim(false);
        }

        public void CloseOptions()
        {
            _options?.Close();
        }

        /// <summary>The panel saved the settings on its way out; bring the title list back.</summary>
        private void OnOptionsClosed()
        {
            Dim(false);
        }

        public void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private IEnumerator Leave(bool newGame, bool plus = false)
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
                var next = plus ? NewGamePlus.From(SaveSystem.Read()) : null;
                SaveSystem.Delete();
                if (next != null) { SaveSystem.Write(next); }
            }

            SceneManager.LoadScene(GameSceneName);
        }
    }

    internal static class StyleExtensions
    {
        public static void SetOpacity(this IStyle style, float value) => style.opacity = value;
    }
}
