using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The pause over the game, from design/ui/menus/menus.html and map.html (frame 6): Continue, Map, Equipment, Character,
    /// Skills, Settings, Back to the title. Escape or Start opens and closes it; the pad's back
    /// button closes it too.
    ///
    /// Map opens the map (<see cref="MapView"/>). Equipment, Character and Skills open the
    /// inventory window (<see cref="InventoryView"/>) on
    /// their page; it hands back to the pause when it closes. The diamond beside Character and
    /// Skills tells the player there are points to spend.
    /// Settings are the title screen's own panel. Leaving for the title saves first, the same as
    /// quitting the application does.
    ///
    /// The scrim fades in, the panel rises and the words unfold one after another
    /// (<see cref="MenuMotion"/>); the focused word carries the same light and flame as the title
    /// (<see cref="MenuFocus"/>), as in design/ui/menus/menus.html (2026-10-19).
    ///
    /// It lives in the HUD's document and is ticked by <see cref="HudController"/>, so the scene
    /// needs no object of its own. Holding the pause freezes time through <see cref="GamePause"/>
    /// and silences gameplay input, so nothing reaches the player while it is open.
    /// </summary>
    public sealed class PauseMenu
    {
        private const string OpenClass = "pause--open";
        private readonly VisualElement _overlay;
        private readonly OptionsPanel _options;
        private readonly List<Button> _items = new List<Button>();
        private readonly List<System.Action> _actions = new List<System.Action>();
        private readonly VisualElement _characterPip;
        private readonly ControlsPanel _controls;
        private readonly VisualElement _skillsPip;
        private readonly InventoryView _inventory;
        private readonly MapView _map;
        private readonly VisualElement _panel;
        private readonly MenuFocus _focusMarks;
        private readonly IHudOverlay[] _overlays;
        private PlayerCharacter _player;
        private PlayerInputReader _input;
        private GameProgress _progress;
        private PlayerProgressionService _progression;
        private PlayerSkills _skills;
        private MenuInput.Repeater _repeater;
        private int _focus;
        private bool _isLeaving;

        /// <param name="overlays">Every other window of the document; Escape backs out of these first.</param>
        public PauseMenu(VisualElement root, InventoryView inventory = null, MapView map = null,
            params IHudOverlay[] overlays)
        {
            _overlay = root?.Q("pause");
            if (_overlay == null)
            {
                return;
            }

            _inventory = inventory;
            _map = map;
            var all = new List<IHudOverlay>();
            if (inventory != null) { all.Add(inventory); }
            if (map != null) { all.Add(map); }
            all.AddRange(overlays ?? System.Array.Empty<IHudOverlay>());
            _overlays = all.ToArray();

            _options = new OptionsPanel(_overlay, OnOptionsClosed);
            _panel = _overlay.Q("pause-panel");
            _focusMarks = new MenuFocus(_overlay.Q(className: "pause__body"));
            _characterPip = _overlay.Q("pause-character-pip");
            _skillsPip = _overlay.Q("pause-skills-pip");
            Bind("pause-resume", Resume, true);
            Bind("pause-map", OpenMap, _map != null);
            Bind("pause-equipment", OpenEquipment, _inventory != null);
            Bind("pause-character", () => OpenWindow(InventoryView.Page.Character, "pause-character"),
                _inventory != null);
            Bind("pause-skills", () => OpenWindow(InventoryView.Page.Skills, "pause-skills"),
                _inventory != null);
            Bind("pause-options", OpenOptions, true);
            Bind("pause-controls", OpenControls, true);
            Bind("pause-main-menu", ReturnToMainMenu, true);
            _controls = new ControlsPanel(root, OnControlsClosed);
        }

        public bool IsOpen => _overlay != null && _overlay.ClassListContains(OpenClass);
        public bool IsOptionsOpen => _options != null && _options.IsOpen;

        /// <summary>The controls screen (controls.html) is showing over the pause.</summary>
        public bool IsControlsOpen => _controls != null && _controls.IsOpen;

        public ControlsPanel Controls => _controls;

        /// <summary>Which buttons can be pressed, in order; for tests and tools.</summary>
        public IEnumerable<string> EnabledButtons
        {
            get
            {
                foreach (var item in _items)
                {
                    if (item.enabledSelf) { yield return item.name; }
                }
            }
        }

        public void Attach(PlayerCharacter player, GameProgress progress)
        {
            if (_input != null)
            {
                _input.PauseToggled -= Toggle;
            }

            _player = player;
            _progress = progress;
            _input = player != null ? player.GetComponent<PlayerInputReader>() : null;
            _progression = progress != null ? progress.GetComponent<PlayerProgressionService>() : null;
            if (_progression == null) { _progression = Object.FindAnyObjectByType<PlayerProgressionService>(); }
            _skills = Object.FindAnyObjectByType<PlayerSkills>();
            if (_input != null)
            {
                _input.PauseToggled += Toggle;
            }
        }

        public void Detach()
        {
            if (_input != null)
            {
                _input.PauseToggled -= Toggle;
            }

            if (IsOpen)
            {
                Resume();
            }
        }

        /// <summary>Escape or Start: backs out of the settings first, then out of the pause.</summary>
        public void Toggle()
        {
            if (_overlay == null || _isLeaving)
            {
                return;
            }

            var overlay = System.Array.Find(_overlays ?? System.Array.Empty<IHudOverlay>(), o => o.IsOpen);
            if (overlay != null)
            {
                // A window opened from here hands back to the pause itself when it closes.
                overlay.Back();
            }
            else if (IsOptionsOpen)
            {
                _options.Close();
            }
            else if (IsControlsOpen)
            {
                // The Escape that cancels waiting for a key must not also leave the screen.
                _controls.Back();
            }
            else if (IsOpen)
            {
                Resume();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            // Not over a death, and not over another screen that already holds the game still.
            if (_overlay == null || IsOpen || (_player != null && !_player.IsAlive)
                || GamePause.IsPaused)
            {
                return;
            }

            _overlay.AddToClassList(OpenClass);
            MenuMotion.Reveal(_overlay);
            MenuMotion.Reveal(_panel);
            MenuMotion.Stagger(_items.ConvertAll(item => (VisualElement)item), 80);
            GamePause.Push();
            _input?.SetGameplayEnabled(false);
            RefreshPips();
            SetFocus(0);
        }

        public void Resume()
        {
            if (!IsOpen)
            {
                return;
            }

            _options?.Close();
            CloseControls();
            _overlay.RemoveFromClassList(OpenClass);
            MenuMotion.Hide(_overlay);
            MenuMotion.Hide(_panel);
            GamePause.Pop();
            _input?.SetGameplayEnabled(true);
        }

        /// <summary>
        /// Saves and goes to the title. The save is what quitting the application already does,
        /// so leaving through the menu loses nothing that closing the window would keep.
        /// </summary>
        public void ReturnToMainMenu()
        {
            if (_isLeaving)
            {
                return;
            }

            _isLeaving = true;
            _progress?.SaveNow();
            GamePause.Clear();
            GameTimeScale.Reset();
            SceneManager.LoadScene(MainMenuController.SceneName);
        }

        /// <summary>Keyboard and pad navigation; run every frame by the HUD, on unscaled time.</summary>
        public void Tick()
        {
            if (!IsOpen || _isLeaving)
            {
                return;
            }

            if (IsOptionsOpen)
            {
                if (MenuInput.PadBackPressed())
                {
                    _options.Close();
                    return;
                }

                _options.Tick();
                return;
            }

            if (IsControlsOpen)
            {
                if (MenuInput.PadBackPressed() && !_controls.SwallowsCancel)
                {
                    _controls.Back();
                    return;
                }

                _controls.Tick();
                return;
            }

            if (MenuInput.PadBackPressed())
            {
                Resume();
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
                _actions[_focus]?.Invoke();
            }
        }

        private void Bind(string name, System.Action action, bool enabled)
        {
            var button = _overlay.Q<Button>(name);
            if (button == null)
            {
                return;
            }

            button.SetEnabled(enabled);
            if (action != null)
            {
                button.clicked += action;
            }

            var index = _items.Count;
            _items.Add(button);
            _actions.Add(action);
            button.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (button.enabledSelf) { SetFocus(index); }
            });
        }

        /// <summary>The pause steps aside for the inventory window and returns when it closes.</summary>
        public void OpenEquipment() => OpenWindow(InventoryView.Page.Items, "pause-equipment");

        public void OpenCharacter() => OpenWindow(InventoryView.Page.Character, "pause-character");

        public void OpenSkills() => OpenWindow(InventoryView.Page.Skills, "pause-skills");

        private void OpenWindow(InventoryView.Page page, string button)
        {
            if (_inventory == null || !IsOpen)
            {
                return;
            }

            Resume();
            if (!_inventory.Open(page, () => ReturnTo(button)))
            {
                Open();
            }
        }

        /// <summary>The map over the frozen game; closing it comes back to the pause.</summary>
        public void OpenMap()
        {
            if (_map == null || !IsOpen)
            {
                return;
            }

            Resume();
            if (!_map.Open(() => ReturnTo("pause-map")))
            {
                Open();
            }
        }

        private void ReturnTo(string button)
        {
            Open();
            SetFocus(_items.FindIndex(item => item.name == button));
        }

        public void OpenControls()
        {
            if (_controls == null)
            {
                return;
            }

            _overlay.AddToClassList("pause--controls");
            _controls.Open();
        }

        public void CloseControls()
        {
            _controls?.Close();
        }

        private void OnControlsClosed()
        {
            _overlay.RemoveFromClassList("pause--controls");
            SetFocus(_items.FindIndex(item => item.name == "pause-controls"));
        }

        public void OpenOptions()
        {
            _overlay.AddToClassList("pause--options");
            _options.Open();
        }

        private void OnOptionsClosed()
        {
            _overlay.RemoveFromClassList("pause--options");
            SetFocus(_items.FindIndex(item => item.name == "pause-options"));
        }

        private void RefreshPips()
        {
            _characterPip?.EnableInClassList("pip--visible",
                _progression != null && _progression.StatPoints > 0);
            _skillsPip?.EnableInClassList("pip--visible",
                _skills != null && _skills.AvailableSkillPoints > 0);
        }

        private void MoveFocus(int step)
        {
            // Skips the disabled screens rather than landing on them.
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
                _items[i].EnableInClassList("btn--focused", i == _focus);
            }

            if (_focus < _items.Count)
            {
                _focusMarks?.MoveTo(_items[_focus], _items[_focus].ClassListContains("btn--danger"));
            }
        }
    }
}
