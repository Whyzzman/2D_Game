using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>The icon set and frame textures the HUD dresses itself with.</summary>
    [Serializable]
    public sealed class HudArt
    {
        public Sprite barFrame;
        public Sprite barFrameThin;
        public Sprite slotFrame;
        public Sprite sheen;
        public Sprite vignette;
        public Sprite ornament;
        public Sprite sword;
        public Sprite greatsword;
        public Sprite warrior;
        public Sprite hunter;
        public Sprite mage;
        public Sprite shadow;
        public Sprite fire;
        public Sprite frost;
        public Sprite arcane;
        public Sprite poison;
        public Sprite bleed;
        public Sprite currency;
    }

    /// <summary>
    /// The in-game HUD: reads the player, the boss and the world every frame and writes it into
    /// the UI Toolkit tree from Hud.uxml. It only reads — nothing in gameplay waits on the HUD,
    /// and a missing reference simply leaves its part of the screen empty.
    ///
    /// Layout and values follow design/ui/hud/hud.html. Time is unscaled so a hit-stop or the
    /// pause never freezes a fade half-way.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudController : MonoBehaviour
    {
        private const float LossDelay = 0.45f;
        private const float LossSpeed = 0.9f;
        private const float LowShare = 0.3f;
        /// <summary>A change smaller than this (a twentieth of a percent) is not worth a relayout.</summary>
        private const float ShownEpsilon = 0.0005f;
        private const float TitleIn = 0.9f;
        private const float TitleHold = 2.6f;
        private const float TitleOut = 1.1f;

        [SerializeField] private PlayerCharacter _player;
        [SerializeField] private Mana _mana;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private ActiveSkillController _skills;
        [SerializeField] private PlayerSpellbook _spellbook;
        [SerializeField] private SpellCaster _caster;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private PlayerRespawner _respawner;
        [SerializeField] private GameProgress _progress;
        [SerializeField] private WorldState _world;
        [SerializeField] private HudArt _art = new HudArt();

        private readonly Meter _health = new Meter();
        private readonly Meter _manaMeter = new Meter();
        private readonly Meter _stamina = new Meter();
        private readonly Meter _bossMeter = new Meter();
        private readonly Slot[] _skillSlots = new Slot[ActiveSkillController.SlotCount];
        private readonly Slot[] _spellSlots = new Slot[PlayerSpellbook.SlotCount];
        private readonly VisualElement[] _chips = new VisualElement[4];
        private readonly VisualElement[] _chipFills = new VisualElement[4];
        private BossEncounter[] _encounters = Array.Empty<BossEncounter>();
        private VisualElement _root;
        private VisualElement _boss;
        private VisualElement _bossMark;
        private Label _bossName;
        private Label _bossPhase;
        private Label _bossValue;
        private Label _hpValue;
        private Label _purse;
        private VisualElement _prompt;
        private Label _promptText;
        private VisualElement _zoneTitle;
        private Label _zoneTitleText;
        private VisualElement _fade;
        private VisualElement _death;
        private Slot _weaponSlot;
        private BossController _shownBoss;
        private float _fadeValue;
        private float _fadeTarget;
        private float _fadeSpeed = 1f;
        private float _titleClock = -1f;
        private int _shownCurrency = -1;
        // What the labels and fills last showed. UI Toolkit re-lays out on every style or text
        // write, so the HUD writes only when a shown value actually changes.
        private int _shownHp = -1;
        private int _shownHpMax = -1;
        private int _shownBossHp = -1;
        private int _shownBossHpMax = -1;
        private int _shownBossPhase = -1;
        private float _shownFade = -1f;
        private readonly float[] _shownChipFill = { -1f, -1f, -1f, -1f };
        private string _shownPrompt;

        private PauseMenu _pause;

        /// <summary>The pause over the game; it shares this document.</summary>
        public PauseMenu Pause => _pause;

        /// <summary>For tests and tools: whether the boss bar is currently up.</summary>
        public bool IsBossBarVisible => _boss != null && _boss.ClassListContains("boss--visible");
        public bool IsDeathShown => _death != null && _death.ClassListContains("death--visible");
        public float FadeAmount => _fadeValue;
        public string ZoneTitleText => _zoneTitleText != null ? _zoneTitleText.text : string.Empty;

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            if (_root == null)
            {
                return;
            }

            ResolveMissingReferences();
            Bind();
            _pause = new PauseMenu(_root);
            _pause.Attach(_player, _progress);
            Dress();
            Subscribe(true);
        }

        private void OnDisable()
        {
            Subscribe(false);
            _pause?.Detach();
        }

        private void Start()
        {
            _encounters = FindObjectsByType<BossEncounter>(FindObjectsInactive.Exclude);
        }

        private void ResolveMissingReferences()
        {
            // The scene builder wires every reference; these are for a HUD dropped into a test
            // scene by hand, where a blank bar would hide the real problem.
            if (_player == null) { _player = FindAnyObjectByType<PlayerCharacter>(); }
            if (_player != null)
            {
                if (_mana == null) { _mana = _player.GetComponent<Mana>(); }
                if (_equipment == null) { _equipment = _player.GetComponent<PlayerEquipment>(); }
                if (_skills == null) { _skills = _player.GetComponent<ActiveSkillController>(); }
                if (_caster == null) { _caster = _player.GetComponent<SpellCaster>(); }
                if (_interactor == null) { _interactor = _player.GetComponentInChildren<PlayerInteractor>(); }
            }

            if (_spellbook == null) { _spellbook = FindAnyObjectByType<PlayerSpellbook>(); }
            if (_respawner == null) { _respawner = FindAnyObjectByType<PlayerRespawner>(); }
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_world == null) { _world = FindAnyObjectByType<WorldState>(); }
        }

        private void Bind()
        {
            _health.Bind(_root, "hp");
            _manaMeter.Bind(_root, "mana");
            _stamina.Bind(_root, "stamina");
            _bossMeter.Bind(_root, "boss");
            _boss = _root.Q("boss");
            _bossMark = _root.Q("boss-mark");
            _bossName = _root.Q<Label>("boss-name");
            _bossPhase = _root.Q<Label>("boss-phase");
            _bossValue = _root.Q<Label>("boss-value");
            _hpValue = _root.Q<Label>("hp-value");
            _purse = _root.Q<Label>("purse-value");
            _prompt = _root.Q("prompt");
            _promptText = _root.Q<Label>("prompt-text");
            _zoneTitle = _root.Q("zone-title");
            _zoneTitleText = _root.Q<Label>("zone-title-text");
            _fade = _root.Q("fade");
            _death = _root.Q("death");
            _weaponSlot = new Slot(_root.Q("slot-weapon"));
            for (var i = 0; i < _skillSlots.Length; i++)
            {
                _skillSlots[i] = new Slot(_root.Q($"slot-skill-{i}"));
            }

            for (var i = 0; i < _spellSlots.Length; i++)
            {
                _spellSlots[i] = new Slot(_root.Q($"slot-spell-{i}"));
            }

            string[] chipNames = { "status-bleed", "status-poison", "status-burn", "status-frost" };
            for (var i = 0; i < chipNames.Length; i++)
            {
                _chips[i] = _root.Q(chipNames[i]);
                _chipFills[i] = _chips[i]?.Q(className: "chip__meter-fill");
            }
        }

        /// <summary>Puts the textures and fonts on the tree; USS cannot name generated art.</summary>
        private void Dress()
        {
            SetImage(_root.Q("vignette"), _art.vignette);
            _root.Query(className: "bar__sheen").ForEach(e => SetImage(e, _art.sheen));
            _root.Query(className: "bar__frame").ForEach(e => SetImage(e, _art.barFrame));
            SetImage(_root.Q("mana-frame")?.Q(className: "bar__frame"), _art.barFrameThin);
            SetImage(_root.Q("stamina-frame")?.Q(className: "bar__frame"), _art.barFrameThin);
            _root.Query(className: "slot__frame").ForEach(e => SetImage(e, _art.slotFrame));
            _root.Query(className: "ornament").ForEach(e => SetImage(e, _art.ornament));
            SetImage(_root.Q("purse-icon"), _art.currency);
            SetImage(_chips[0]?.Q(className: "chip__icon"), _art.bleed);
            SetImage(_chips[1]?.Q(className: "chip__icon"), _art.poison);
            SetImage(_chips[2]?.Q(className: "chip__icon"), _art.fire);
            SetImage(_chips[3]?.Q(className: "chip__icon"), _art.frost);

            var body = HudFonts.Body;
            if (body != null)
            {
                _root.style.unityFontDefinition = FontDefinition.FromFont(body);
            }

            var display = HudFonts.Display;
            if (display != null)
            {
                _root.Query(className: "display").ForEach(
                    e => e.style.unityFontDefinition = FontDefinition.FromFont(display));
            }
        }

        private void Subscribe(bool on)
        {
            if (_respawner != null)
            {
                if (on)
                {
                    _respawner.FadeOutRequested += OnFadeOut;
                    _respawner.FadeInRequested += OnFadeIn;
                    _respawner.PlayerDied += OnPlayerDied;
                    _respawner.PlayerRespawned += OnPlayerRespawned;
                }
                else
                {
                    _respawner.FadeOutRequested -= OnFadeOut;
                    _respawner.FadeInRequested -= OnFadeIn;
                    _respawner.PlayerDied -= OnPlayerDied;
                    _respawner.PlayerRespawned -= OnPlayerRespawned;
                }
            }

            if (_world != null)
            {
                if (on) { _world.ZoneTitleRequested += OnZoneTitle; }
                else { _world.ZoneTitleRequested -= OnZoneTitle; }
            }
        }

        private void Update()
        {
            if (_root == null)
            {
                return;
            }

            _pause?.Tick();
            var dt = Time.unscaledDeltaTime;
            UpdateResources(dt);
            UpdateBoss(dt);
            UpdateLoadout();
            UpdateConditions();
            UpdatePurseAndPrompt();
            UpdateFade(dt);
            UpdateZoneTitle(dt);
        }

        private void UpdateResources(float dt)
        {
            if (_player == null || _player.Health == null)
            {
                return;
            }

            var health = _player.Health;
            var share = Share(health.Current, health.Max);
            _health.Set(share, dt);
            _health.Fill?.EnableInClassList("bar__fill--low", share < LowShare);
            var hp = Mathf.CeilToInt(health.Current);
            var hpMax = Mathf.CeilToInt(health.Max);
            if (_hpValue != null && (hp != _shownHp || hpMax != _shownHpMax))
            {
                _shownHp = hp;
                _shownHpMax = hpMax;
                _hpValue.text = $"{hp} / {hpMax}";
            }

            if (_mana != null)
            {
                _manaMeter.Set(Share(_mana.Current, _mana.Max), dt);
            }

            var stamina = _player.Stamina;
            if (stamina != null)
            {
                var staminaShare = Share(stamina.Current, stamina.Max);
                _stamina.Set(staminaShare, dt);
                _stamina.Fill?.EnableInClassList("bar__fill--low", staminaShare < LowShare);
            }
        }

        private void UpdateBoss(float dt)
        {
            var boss = ActiveBoss();
            _boss?.EnableInClassList("boss--visible", boss != null);
            if (boss == null)
            {
                _shownBoss = null;
                return;
            }

            var health = boss.Actor.Health;
            var share = Share(health.Current, health.Max);
            if (boss != _shownBoss)
            {
                // A new fight starts full, not sliding in from the last boss's value.
                _shownBoss = boss;
                _shownBossHp = -1;
                _shownBossPhase = -1;
                _bossMeter.Snap(share);
                if (_bossName != null) { _bossName.text = boss.DisplayName; }
                if (_bossMark != null)
                {
                    _bossMark.style.left = Length.Percent(boss.PhaseTwoHealthFraction * 100f);
                    _bossMark.style.display = boss.TotalPhases > 1 ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            _bossMeter.Set(share, dt);
            _bossMark?.EnableInClassList("boss__mark--passed", boss.Phase > 1);
            if (_bossPhase != null && boss.Phase != _shownBossPhase)
            {
                _shownBossPhase = boss.Phase;
                _bossPhase.text = $"Фаза {Roman(boss.Phase)}";
                _bossPhase.EnableInClassList("boss__phase--two", boss.Phase > 1);
            }

            var bossHp = Mathf.CeilToInt(health.Current);
            var bossHpMax = Mathf.CeilToInt(health.Max);
            if (_bossValue != null && (bossHp != _shownBossHp || bossHpMax != _shownBossHpMax))
            {
                _shownBossHp = bossHp;
                _shownBossHpMax = bossHpMax;
                _bossValue.text = $"{bossHp} / {bossHpMax}";
            }
        }

        private BossController ActiveBoss()
        {
            foreach (var encounter in _encounters)
            {
                if (encounter == null || !encounter.IsFightActive)
                {
                    continue;
                }

                var boss = encounter.Boss;
                if (boss != null && boss.Actor != null && boss.Actor.IsAlive)
                {
                    return boss;
                }
            }

            return null;
        }

        private void UpdateLoadout()
        {
            var weapon = _equipment != null ? _equipment.ActiveWeaponDefinition : null;
            _weaponSlot.Show(weapon == null ? null
                : weapon.WeaponType == WeaponType.Greatsword ? _art.greatsword : _art.sword, 0f, 0f);

            for (var i = 0; i < _skillSlots.Length; i++)
            {
                var skill = _skills != null ? _skills.SlotSkill(i) : null;
                _skillSlots[i].Show(skill == null ? null : skill.Icon != null ? skill.Icon : BranchIcon(skill.Branch),
                    skill == null ? 0f : _skills.CooldownNormalized(i),
                    skill == null ? 0f : _skills.CooldownRemaining(i));
            }

            for (var i = 0; i < _spellSlots.Length; i++)
            {
                var spell = _spellbook != null ? _spellbook.SlotSpell(i) : null;
                var hasCaster = spell != null && _caster != null;
                _spellSlots[i].Show(spell == null ? null : spell.Icon != null ? spell.Icon : ElementIcon(spell.Element),
                    hasCaster ? _caster.CooldownNormalized(i) : 0f,
                    hasCaster ? _caster.CooldownRemaining(i) : 0f);
            }
        }

        private void UpdateConditions()
        {
            var statuses = _player != null && _player.Actor != null ? _player.Actor.Statuses : null;
            for (var i = 0; i < _chips.Length; i++)
            {
                if (_chips[i] == null)
                {
                    continue;
                }

                var type = (StatusType)i;
                var rule = statuses != null ? statuses.Rule(type) : null;
                var buildup = statuses != null ? statuses.Buildup(type) : 0f;
                var remaining = statuses != null ? statuses.Remaining(type) : 0f;
                var active = remaining > 0f;
                _chips[i].EnableInClassList("chip--visible", active || buildup > 0.5f);
                _chips[i].EnableInClassList("chip--active", active);
                if (_chipFills[i] != null && rule != null)
                {
                    var meter = active && rule.duration > 0f
                        ? remaining / rule.duration
                        : buildup / Mathf.Max(1f, rule.threshold);
                    var fill = Mathf.Clamp01(meter);
                    if (Mathf.Abs(fill - _shownChipFill[i]) > ShownEpsilon)
                    {
                        _shownChipFill[i] = fill;
                        _chipFills[i].style.width = Length.Percent(fill * 100f);
                    }
                }
            }
        }

        private void UpdatePurseAndPrompt()
        {
            var currency = _progress != null ? _progress.Currency : 0;
            if (_purse != null && currency != _shownCurrency)
            {
                _shownCurrency = currency;
                _purse.text = currency.ToString("N0").Replace(',', ' ');
            }

            var prompt = _interactor != null ? _interactor.CurrentPrompt : string.Empty;
            var dead = _player != null && !_player.IsAlive;
            if (dead)
            {
                prompt = string.Empty;
            }

            if (prompt != _shownPrompt)
            {
                _shownPrompt = prompt;
                var visible = !string.IsNullOrEmpty(prompt);
                if (visible && _promptText != null)
                {
                    _promptText.text = prompt;
                }

                _prompt?.EnableInClassList("prompt--visible", visible);
            }
        }

        private void UpdateFade(float dt)
        {
            _fadeValue = Mathf.MoveTowards(_fadeValue, _fadeTarget, _fadeSpeed * dt);
            if (_fade != null && !Mathf.Approximately(_fadeValue, _shownFade))
            {
                _shownFade = _fadeValue;
                _fade.style.opacity = _fadeValue;
            }
        }

        private void UpdateZoneTitle(float dt)
        {
            if (_titleClock < 0f || _zoneTitle == null)
            {
                return;
            }

            _titleClock += dt;
            float alpha;
            if (_titleClock < TitleIn)
            {
                alpha = _titleClock / TitleIn;
            }
            else if (_titleClock < TitleIn + TitleHold)
            {
                alpha = 1f;
            }
            else
            {
                alpha = 1f - (_titleClock - TitleIn - TitleHold) / TitleOut;
            }

            if (alpha <= 0f && _titleClock > TitleIn)
            {
                alpha = 0f;
                _titleClock = -1f;
            }

            _zoneTitle.style.opacity = Mathf.Clamp01(alpha);
        }

        private void OnFadeOut(float duration)
        {
            _fadeTarget = 1f;
            _fadeSpeed = 1f / Mathf.Max(0.05f, duration);
        }

        private void OnFadeIn(float duration)
        {
            _fadeTarget = 0f;
            _fadeSpeed = 1f / Mathf.Max(0.05f, duration);
            _death?.EnableInClassList("death--visible", false);
        }

        private void OnPlayerDied()
        {
            // Death owns the centre of the screen: a zone card still fading must not sit on it.
            _titleClock = -1f;
            if (_zoneTitle != null) { _zoneTitle.style.opacity = 0f; }
            _death?.EnableInClassList("death--visible", true);
        }

        private void OnPlayerRespawned()
        {
            _death?.EnableInClassList("death--visible", false);
        }

        private void OnZoneTitle(ZoneDefinition zone)
        {
            if (zone == null || _zoneTitleText == null)
            {
                return;
            }

            _zoneTitleText.text = zone.DisplayName.ToUpperInvariant();
            _titleClock = 0f;
        }

        private Sprite BranchIcon(SkillBranch branch)
        {
            switch (branch)
            {
                case SkillBranch.Hunter: return _art.hunter;
                case SkillBranch.Mage: return _art.mage;
                case SkillBranch.Shadow: return _art.shadow;
                default: return _art.warrior;
            }
        }

        private Sprite ElementIcon(DamageType element)
        {
            switch (element)
            {
                case DamageType.Fire: return _art.fire;
                case DamageType.Frost: return _art.frost;
                case DamageType.Poison: return _art.poison;
                case DamageType.Bleed: return _art.bleed;
                case DamageType.Dark: return _art.shadow;
                default: return _art.arcane;
            }
        }

        private static float Share(float current, float max) =>
            max > 0f ? Mathf.Clamp01(current / max) : 0f;

        private static string Roman(int value)
        {
            switch (value)
            {
                case 1: return "I";
                case 2: return "II";
                case 3: return "III";
                case 4: return "IV";
                default: return value.ToString();
            }
        }

        private static void SetImage(VisualElement element, Sprite sprite)
        {
            if (element != null && sprite != null)
            {
                element.style.backgroundImage = new StyleBackground(sprite);
            }
        }

        /// <summary>A bar with a delayed pale trail showing what was just lost.</summary>
        private sealed class Meter
        {
            private VisualElement _loss;
            private float _shown = -1f;
            private float _trail;
            private float _trailWait;
            private float _writtenShown = -1f;
            private float _writtenTrail = -1f;

            public VisualElement Fill { get; private set; }

            public void Bind(VisualElement root, string prefix)
            {
                Fill = root.Q($"{prefix}-fill");
                _loss = root.Q($"{prefix}-loss");
            }

            public void Snap(float share)
            {
                _shown = share;
                _trail = share;
                _trailWait = 0f;
                Write();
            }

            public void Set(float share, float dt)
            {
                if (_shown < 0f)
                {
                    Snap(share);
                    return;
                }

                if (share < _shown)
                {
                    _trailWait = LossDelay;
                }

                _shown = share;
                if (_trail < share)
                {
                    _trail = share;
                }
                else if (_trailWait > 0f)
                {
                    _trailWait -= dt;
                }
                else
                {
                    _trail = Mathf.MoveTowards(_trail, share, LossSpeed * dt);
                }

                Write();
            }

            private void Write()
            {
                if (Fill != null && Mathf.Abs(_shown - _writtenShown) > ShownEpsilon)
                {
                    _writtenShown = _shown;
                    Fill.style.width = Length.Percent(_shown * 99f);
                    Fill.style.borderRightWidth = _shown > 0.005f && _shown < 0.995f ? 2f : 0f;
                }

                if (_loss != null && Mathf.Abs(_trail - _writtenTrail) > ShownEpsilon)
                {
                    _writtenTrail = _trail;
                    _loss.style.width = Length.Percent(_trail * 99f);
                }
            }
        }

        private sealed class Slot
        {
            private readonly VisualElement _root;
            private readonly VisualElement _icon;
            private readonly VisualElement _cooldown;
            private readonly Label _timer;
            private Sprite _sprite;
            private bool _initialised;
            private int _shownTenths = -1;
            private float _shownCooldown = -1f;

            public Slot(VisualElement root)
            {
                _root = root;
                _icon = root?.Q(className: "slot__icon");
                _cooldown = root?.Q(className: "slot__cooldown");
                _timer = root?.Q<Label>(className: "slot__timer");
            }

            public void Show(Sprite sprite, float cooldownShare, float remaining)
            {
                if (_root == null)
                {
                    return;
                }

                if (!_initialised || sprite != _sprite)
                {
                    _initialised = true;
                    _sprite = sprite;
                    _root.EnableInClassList("slot--empty", sprite == null);
                    if (_icon != null)
                    {
                        _icon.style.backgroundImage = sprite != null
                            ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
                    }
                }

                var cooldown = Mathf.Clamp01(cooldownShare);
                if (_cooldown != null && Mathf.Abs(cooldown - _shownCooldown) > ShownEpsilon)
                {
                    _shownCooldown = cooldown;
                    _cooldown.style.height = Length.Percent(cooldown * 100f);
                }

                var tenths = remaining > 0.05f ? Mathf.CeilToInt(remaining * 10f) : 0;
                if (_timer != null && tenths != _shownTenths)
                {
                    _shownTenths = tenths;
                    _timer.text = tenths == 0 ? string.Empty
                        : remaining >= 10f ? Mathf.CeilToInt(remaining).ToString()
                        : (tenths / 10f).ToString("0.0");
                }
            }
        }
    }

    /// <summary>
    /// The HUD's typefaces. Georgia stands in for the display serif of the HTML design (Cinzel)
    /// until a licensed font is bundled; both carry Cyrillic. Created once, on first use.
    /// </summary>
    public static class HudFonts
    {
        private static Font _display;
        private static Font _body;
        private static bool _created;

        public static Font Display
        {
            get { Create(); return _display; }
        }

        public static Font Body
        {
            get { Create(); return _body; }
        }

        private static void Create()
        {
            if (_created)
            {
                return;
            }

            _created = true;
            try
            {
                _display = Font.CreateDynamicFontFromOSFont(
                    new[] { "Georgia", "Palatino Linotype", "Times New Roman" }, 32);
                // Arial first: it ships with both Windows and macOS. A face missing on this OS at
                // the head of the list makes UI Toolkit warn on every panel update.
                _body = Font.CreateDynamicFontFromOSFont(
                    new[] { "Arial", "Segoe UI", "Liberation Sans" }, 24);
            }
            catch (Exception)
            {
                // No OS fonts (a headless runner): the theme's own font is used instead.
                _display = null;
                _body = null;
            }
        }
    }
}
