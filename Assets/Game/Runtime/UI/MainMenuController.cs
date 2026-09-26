using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private readonly List<VisualElement> _optionItems = new List<VisualElement>();
        private VisualElement _root;
        private VisualElement _fade;
        private VisualElement _options;
        private Button _continue;
        private int _focus;
        private int _optionFocus;
        private bool _isLeaving;
        private float _navigationCooldown;

        public bool IsOptionsOpen => _options != null && _options.ClassListContains("options--open");
        public bool CanContinue => _continue != null && _continue.enabledSelf;

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            if (_root == null)
            {
                return;
            }

            Time.timeScale = 1f;
            _fade = _root.Q("fade");
            _options = _root.Q("options-panel");
            _continue = _root.Q<Button>("continue");
            Dress();
            BindButtons();
            BindOptions();
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
            var back = _root.Q<Button>("options-back");
            if (back != null) { back.clicked += CloseOptions; }
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

        private void BindOptions()
        {
            var settings = GameSettings.Current;
            BindSlider("opt-master", settings.masterVolume, v => GameSettings.Set(d => d.masterVolume = v));
            BindSlider("opt-effects", settings.effectsVolume, v => GameSettings.Set(d => d.effectsVolume = v));
            BindSlider("opt-shake", settings.screenShake, v => GameSettings.Set(d => d.screenShake = v));
            BindSlider("opt-flash", settings.flashIntensity, v => GameSettings.Set(d => d.flashIntensity = v));
            BindSlider("opt-hitstop", settings.hitStop, v => GameSettings.Set(d => d.hitStop = v));
            var back = _root.Q("options-back");
            if (back != null) { _optionItems.Add(back); }
        }

        private void BindSlider(string name, float value, System.Action<float> write)
        {
            var slider = _root.Q<Slider>(name);
            if (slider == null)
            {
                return;
            }

            slider.SetValueWithoutNotify(value);
            slider.RegisterValueChangedCallback(e => write(e.newValue));
            _optionItems.Add(slider);
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

            _navigationCooldown -= Time.unscaledDeltaTime;
            var move = ReadVertical();
            if (move != 0 && _navigationCooldown <= 0f)
            {
                _navigationCooldown = 0.18f;
                if (IsOptionsOpen) { MoveOptionFocus(move); }
                else { MoveFocus(move); }
            }
            else if (move == 0)
            {
                _navigationCooldown = 0f;
            }

            if (IsOptionsOpen)
            {
                UpdateOptionKeys();
                return;
            }

            if (Confirmed() && _focus >= 0 && _focus < _items.Count && _items[_focus].enabledSelf)
            {
                _actions[_focus]();
            }
        }

        private void UpdateOptionKeys()
        {
            if (Cancelled())
            {
                CloseOptions();
                return;
            }

            var focused = _optionFocus >= 0 && _optionFocus < _optionItems.Count
                ? _optionItems[_optionFocus] : null;
            if (focused is Slider slider)
            {
                var horizontal = ReadHorizontal();
                if (horizontal != 0)
                {
                    slider.value = Mathf.Clamp01(slider.value + horizontal * 0.6f * Time.unscaledDeltaTime);
                }
            }
            else if (focused is Button && Confirmed())
            {
                CloseOptions();
            }
        }

        private static int ReadVertical()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            var up = keyboard != null && (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed);
            var down = keyboard != null && (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed);
            if (pad != null)
            {
                var y = pad.leftStick.ReadValue().y + pad.dpad.ReadValue().y;
                up |= y > 0.5f;
                down |= y < -0.5f;
            }

            return up == down ? 0 : up ? -1 : 1;
        }

        private static int ReadHorizontal()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            var left = keyboard != null && (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed);
            var right = keyboard != null && (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed);
            if (pad != null)
            {
                var x = pad.leftStick.ReadValue().x + pad.dpad.ReadValue().x;
                left |= x < -0.5f;
                right |= x > 0.5f;
            }

            return left == right ? 0 : left ? -1 : 1;
        }

        private static bool Confirmed()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame
                    || keyboard.spaceKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                || (pad != null && pad.buttonSouth.wasPressedThisFrame);
        }

        private static bool Cancelled()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                || (pad != null && pad.buttonEast.wasPressedThisFrame);
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

        private void MoveOptionFocus(int step)
        {
            if (_optionItems.Count == 0)
            {
                return;
            }

            _optionFocus = (_optionFocus + step + _optionItems.Count) % _optionItems.Count;
            for (var i = 0; i < _optionItems.Count; i++)
            {
                var focused = i == _optionFocus;
                _optionItems[i].EnableInClassList("option--focused", focused);
                _optionItems[i].EnableInClassList("menu-button--focused", focused);
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
            _options?.AddToClassList("options--open");
            _root.Q("front")?.style.SetOpacity(0.15f);
            _optionFocus = -1;
            MoveOptionFocus(1);
        }

        public void CloseOptions()
        {
            GameSettings.Save();
            _options?.RemoveFromClassList("options--open");
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
