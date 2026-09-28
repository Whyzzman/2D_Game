using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The settings panel shared by the title screen and the pause menu: the same five sliders and the picture-effects switch,
    /// written straight into <see cref="GameSettings"/> and saved to their own file on close, so
    /// a new game never touches them. Markup and look live in Options.uss and the two UXMLs.
    /// </summary>
    public sealed class OptionsPanel
    {
        private const string OpenClass = "options--open";
        private readonly VisualElement _panel;
        private readonly List<VisualElement> _items = new List<VisualElement>();
        private readonly System.Action _closed;
        private MenuInput.Repeater _repeater;
        private int _focus = -1;

        public OptionsPanel(VisualElement root, System.Action closed)
        {
            _panel = root?.Q("options-panel");
            _closed = closed;
            if (_panel == null)
            {
                return;
            }

            var settings = GameSettings.Current;
            BindSlider("opt-master", settings.masterVolume, v => GameSettings.Set(d => d.masterVolume = v));
            BindSlider("opt-effects", settings.effectsVolume, v => GameSettings.Set(d => d.effectsVolume = v));
            BindSlider("opt-shake", settings.screenShake, v => GameSettings.Set(d => d.screenShake = v));
            BindSlider("opt-flash", settings.flashIntensity, v => GameSettings.Set(d => d.flashIntensity = v));
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

            slider.SetValueWithoutNotify(value);
            slider.RegisterValueChangedCallback(e => write(e.newValue));
            _items.Add(slider);
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
        }
    }
}
