using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The settings panel shared by the title screen and the pause menu (design/ui/menus/menus.html,
    /// UI_UX_GUIDE.md §12): six sliders in two groups and the picture-effects switch, written
    /// straight into <see cref="GameSettings"/> and saved to their own file on close, so a new
    /// game never touches them. Each slider shows its value ("Вимк." at zero), the line under the
    /// rows explains the focused one, and the shake and flash sliders preview their strength on
    /// the screen while they move — the player sees what they turn down. The panel and its rows
    /// come in with <see cref="MenuMotion"/>. Markup and look live in Options.uss and the two UXMLs.
    /// </summary>
    public sealed class OptionsPanel
    {
        private const string OpenClass = "options--open";
        private readonly VisualElement _panel;
        private readonly List<VisualElement> _items = new List<VisualElement>();
        private readonly System.Action _closed;
        private readonly Label _help;
        private readonly VisualElement _flash;
        private MenuInput.Repeater _repeater;
        private int _focus = -1;
        private float _shake;
        private float _flashLevel;
        private float _lastPreview = -1f;
        private IVisualElementScheduledItem _previewTick;

        /// <summary>The line under the rows for each focusable row, by element name.</summary>
        private static readonly Dictionary<string, string> Help = new Dictionary<string, string>
        {
            { "opt-master", "Керує всім звуком гри разом." },
            { "opt-music", "Музика зон і боїв. Звук бою й кімнати не змінюється." },
            { "opt-effects", "Удари, рух, магія. Шум кімнати й інтерфейс лишаються чутними." },
            { "opt-shake", "0 % вимикає повністю; бій лишається читабельним. Рухайте повзунок — екран труситься з обраною силою." },
            { "opt-flash", "Лише парирування, злам блоку, крит і моменти боса. 0 % вимикає повністю." },
            { "opt-hitstop", "Коротка пауза кадру на влучанні — вага удару. 0 % вимикає повністю." },
            { "opt-post", "Кольорокорекція й віньєтка зони. Вимкніть, якщо зображення здається затемним." },
            { "options-back", "Зберегти й повернутися. Нова гра налаштувань не стирає." },
        };

        public OptionsPanel(VisualElement root, System.Action closed)
        {
            _panel = root?.Q("options-panel");
            _closed = closed;
            if (_panel == null)
            {
                return;
            }

            _help = _panel.Q<Label>("options-help");
            _flash = new VisualElement { pickingMode = PickingMode.Ignore };
            _flash.AddToClassList("options__flash");
            _panel.parent?.Add(_flash);

            var settings = GameSettings.Current;
            BindSlider("opt-master", settings.masterVolume, v => GameSettings.Set(d => d.masterVolume = v));
            BindSlider("opt-music", settings.musicVolume, v => GameSettings.Set(d => d.musicVolume = v));
            BindSlider("opt-effects", settings.effectsVolume, v => GameSettings.Set(d => d.effectsVolume = v));
            BindSlider("opt-shake", settings.screenShake, v =>
            {
                GameSettings.Set(d => d.screenShake = v);
                Preview(v, 0f);
            });
            BindSlider("opt-flash", settings.flashIntensity, v =>
            {
                GameSettings.Set(d => d.flashIntensity = v);
                Preview(0f, v);
            });
            BindSlider("opt-hitstop", settings.hitStop, v => GameSettings.Set(d => d.hitStop = v));
            BindToggle("opt-post", settings.postEffects, v => GameSettings.Set(d => d.postEffects = v));
            var back = _panel.Q<Button>("options-back");
            if (back != null)
            {
                back.clicked += Close;
                _items.Add(back);
            }
        }

        public bool IsOpen => _panel != null && _panel.ClassListContains(OpenClass);

        public void Open()
        {
            if (_panel == null)
            {
                return;
            }

            _panel.AddToClassList(OpenClass);
            MenuMotion.Reveal(_panel);
            MenuMotion.Stagger(_panel.Query(className: "options__line").ToList());
            _focus = -1;
            MoveFocus(1);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            GameSettings.Save();
            _panel.RemoveFromClassList(OpenClass);
            MenuMotion.Hide(_panel);
            StopPreview();
            _closed?.Invoke();
        }

        /// <summary>
        /// Keyboard and pad while open: up and down move between rows, left and right move a
        /// slider, confirm on Back closes. Cancelling is left to the owner, which knows its keys.
        /// </summary>
        public void Tick()
        {
            if (!IsOpen)
            {
                return;
            }

            var step = _repeater.Step(MenuInput.ReadVertical());
            if (step != 0)
            {
                MoveFocus(step);
            }

            var focused = _focus >= 0 && _focus < _items.Count ? _items[_focus] : null;
            if (focused is Slider slider)
            {
                var horizontal = MenuInput.ReadHorizontal();
                if (horizontal != 0)
                {
                    slider.value = Mathf.Clamp01(
                        slider.value + horizontal * 0.6f * Time.unscaledDeltaTime);
                }
            }
            else if (focused is Toggle toggle && MenuInput.Confirmed())
            {
                toggle.value = !toggle.value;
            }
            else if (focused is Button && MenuInput.Confirmed())
            {
                Close();
            }
        }

        private void BindSlider(string name, float value, System.Action<float> write)
        {
            var slider = _panel.Q<Slider>(name);
            if (slider == null)
            {
                return;
            }

            var shown = new Label { pickingMode = PickingMode.Ignore };
            shown.AddToClassList("option__value");
            slider.Add(shown);
            slider.SetValueWithoutNotify(value);
            ShowValue(slider, shown, value);
            slider.RegisterValueChangedCallback(e =>
            {
                ShowValue(slider, shown, e.newValue);
                write(e.newValue);
            });
            _items.Add(slider);
        }

        /// <summary>The number beside the track; zero says plainly that the effect is off.</summary>
        private static void ShowValue(Slider slider, Label shown, float value)
        {
            var percent = Mathf.RoundToInt(value * 100f);
            shown.text = percent == 0 ? "Вимк." : percent.ToString();
            slider.EnableInClassList("option--zero", percent == 0);
        }

        /// <summary>
        /// Shows what the shake or flash slider sets, at its strength: the menu shakes up to 10 px
        /// and a warm flash rises to the combat ceiling of 0.35 (COMBAT_FEEDBACK.md §8). UI only —
        /// the camera and the feedback service are not touched. At most four times a second.
        /// </summary>
        private void Preview(float shake, float flash)
        {
            var now = Time.unscaledTime;
            if (now - _lastPreview < 0.25f && _lastPreview >= 0f)
            {
                return;
            }

            _lastPreview = now;
            _shake = Mathf.Max(_shake, shake);
            _flashLevel = Mathf.Max(_flashLevel, flash * 0.35f);

            _previewTick ??= _panel.schedule.Execute(TickPreview).Every(16);
            _previewTick.Resume();
        }

        private void TickPreview()
        {
            var dt = Time.unscaledDeltaTime;
            _shake = Mathf.Max(0f, _shake - dt * 3f);
            var host = _panel.parent;
            if (host != null)
            {
                host.style.translate = _shake > 0f
                    ? new Translate(Random.Range(-1f, 1f) * 10f * _shake, Random.Range(-1f, 1f) * 6f * _shake)
                    : new Translate(0f, 0f);
            }

            if (_flash != null)
            {
                _flash.style.opacity = _flashLevel;
            }

            // 0.12 s from the ceiling to nothing, as the combat flash fades.
            _flashLevel = Mathf.Max(0f, _flashLevel - dt / 0.12f * 0.35f);
            if (_shake <= 0f && _flashLevel <= 0f)
            {
                _previewTick.Pause();
            }
        }

        private void StopPreview()
        {
            _shake = 0f;
            _flashLevel = 0f;
            _previewTick?.Pause();
            if (_panel.parent != null) { _panel.parent.style.translate = new Translate(0f, 0f); }
            if (_flash != null) { _flash.style.opacity = 0f; }
        }

        private void BindToggle(string name, bool value, System.Action<bool> write)
        {
            var toggle = _panel.Q<Toggle>(name);
            if (toggle == null)
            {
                return;
            }

            toggle.SetValueWithoutNotify(value);
            toggle.RegisterValueChangedCallback(e => write(e.newValue));
            _items.Add(toggle);
        }

        private void MoveFocus(int step)
        {
            if (_items.Count == 0)
            {
                return;
            }

            _focus = (_focus + step + _items.Count) % _items.Count;
            for (var i = 0; i < _items.Count; i++)
            {
                var focused = i == _focus;
                _items[i].EnableInClassList("option--focused", focused);
                _items[i].EnableInClassList("menu-button--focused", focused);
            }

            if (_help != null && Help.TryGetValue(_items[_focus].name, out var text))
            {
                _help.text = text;
            }
        }
    }
}
