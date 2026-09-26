using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    public enum EquipResult
    {
        Equipped,
        /// <summary>Nothing changed because that item was already in that slot.</summary>
        AlreadyEquipped,
        UnknownItem,
        /// <summary>The item exists but cannot be worn at all.</summary>
        NotEquippable,
        /// <summary>The item is equipment, but not for that slot.</summary>
        WrongSlot,
        NotOwned,
        /// <summary>Only one copy is owned and the other slot already holds it.</summary>
        AlreadyInOtherSlot,
        /// <summary>The character is not strong or quick enough to wield it yet.</summary>
        RequirementsNotMet
    }

    /// <summary>
    /// The live weapon in the player's hand. Kept apart from <see cref="WeaponDefinition"/> so a
    /// later upgrade pass has somewhere to put per-instance state without reworking the asset.
    /// </summary>
    public sealed class EquippedWeapon
    {
        public EquippedWeapon(string itemId, WeaponDefinition definition, int upgradeLevel = 0)
        {
            ItemId = itemId;
            Definition = definition;
            UpgradeLevel = upgradeLevel;
        }

        public string ItemId { get; }
        public WeaponDefinition Definition { get; }
        /// <summary>Always 0 for now; weapon upgrades are a later phase.</summary>
        public int UpgradeLevel { get; }
    }

    /// <summary>
    /// What the player is currently using. It owns the slots and the rules about them; it never
    /// decides what the player owns (that is the inventory) nor how a weapon swings (that is
    /// <see cref="PlayerCombat"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [SerializeField] private GameProgress _progress;
        [SerializeField] private PlayerStats _stats;
        [Tooltip("Granted and equipped when a save has no usable weapon at all.")]
        [SerializeField] private string _starterWeaponId = ItemIds.StarterWeapon;
        private EquipmentLoadout _loadout = new EquipmentLoadout();
        private EquippedWeapon _activeWeapon;

        /// <summary>Raised after any slot changes; stats, visuals and the screen all listen.</summary>
        public event Action EquipmentChanged;

        /// <summary>Raised only when the weapon in hand changes, including on a switch.</summary>
        public event Action<EquippedWeapon> ActiveWeaponChanged;

        public EquipmentLoadout Loadout => _loadout;
        public EquippedWeapon ActiveWeapon => _activeWeapon;
        public WeaponDefinition ActiveWeaponDefinition => _activeWeapon?.Definition;
        public bool IsSecondaryActive => _loadout.isSecondaryActive;
        public EquipmentSlot ActiveWeaponSlot => _loadout.isSecondaryActive
            ? EquipmentSlot.SecondaryWeapon
            : EquipmentSlot.MainWeapon;
        public bool HasSecondaryWeapon => !_loadout.IsEmpty(EquipmentSlot.SecondaryWeapon);
        public ItemDatabase Items => _progress != null ? _progress.Items : null;

        private void Awake()
        {
            if (_progress == null)
            {
                _progress = FindAnyObjectByType<GameProgress>();
            }
            if (_stats == null)
            {
                _stats = GetComponent<PlayerStats>();
            }
        }

        /// <summary>True once a saved loadout has been read; keeps it from being applied twice.</summary>
        public bool HasLoadedLoadout { get; private set; }

        private void Start()
        {
            if (!HasLoadedLoadout)
            {
                // Nobody claimed the order, so equipment settles itself before the first frame.
                ApplySavedLoadout();
            }
        }

        /// <summary>
        /// Reads the saved loadout, throws out anything the player cannot actually wear, and makes
        /// sure a weapon ends up in hand. Runs on load and on New Game.
        /// </summary>
        public void ApplySavedLoadout()
        {
            if (_progress == null)
            {
                return;
            }

            HasLoadedLoadout = true;
            _loadout = _progress.Data.equipment.Clone();
            _loadout.Normalize();
            foreach (var slot in EquipmentSlots.All)
            {
                var itemId = _loadout.Get(slot);
                if (string.IsNullOrEmpty(itemId))
                {
                    continue;
                }

                var reason = Check(itemId, slot, ignoreOtherSlot: true);
                if (reason == EquipResult.Equipped || reason == EquipResult.AlreadyEquipped)
                {
                    continue;
                }

                // A slot that cannot be honoured is emptied rather than left lying: an unknown id
                // from another build must never reach combat or the screen.
                Debug.LogWarning($"Saved equipment '{itemId}' in {slot} is not usable ({reason}); "
                    + "the slot was cleared.");
                _loadout.Set(slot, string.Empty);
            }

            EnsureMainWeapon();
            Commit(raiseWeaponChange: true);
        }

        /// <summary>
        /// The main hand is never empty: unarmed combat does not exist yet, so an empty main slot
        /// falls back to the secondary, then to any owned weapon, then to the starter weapon.
        /// </summary>
        private void EnsureMainWeapon()
        {
            if (!_loadout.IsEmpty(EquipmentSlot.MainWeapon))
            {
                return;
            }

            if (!_loadout.IsEmpty(EquipmentSlot.SecondaryWeapon))
            {
                _loadout.Set(EquipmentSlot.MainWeapon, _loadout.Get(EquipmentSlot.SecondaryWeapon));
                _loadout.Set(EquipmentSlot.SecondaryWeapon, string.Empty);
                _loadout.isSecondaryActive = false;
                return;
            }

            var owned = FirstOwnedWeapon();
            if (!string.IsNullOrEmpty(owned))
            {
                _loadout.Set(EquipmentSlot.MainWeapon, owned);
                return;
            }

            // A save from before weapons existed, or one whose weapon was removed from the build.
            if (_progress.Items != null && _progress.Items.Contains(_starterWeaponId))
            {
                _progress.AddItem(_starterWeaponId);
                _loadout.Set(EquipmentSlot.MainWeapon, _starterWeaponId);
            }
        }

        private string FirstOwnedWeapon()
        {
            var catalogue = _progress.Items;
            if (catalogue == null)
            {
                return string.Empty;
            }

            foreach (var stack in _progress.Inventory.GetItems())
            {
                if (catalogue.TryGetItem(stack.itemId, out var definition)
                    && definition.Weapon != null)
                {
                    return stack.itemId;
                }
            }

            return string.Empty;
        }

        // ---------- queries ----------

        public string GetEquipped(EquipmentSlot slot)
        {
            return _loadout.Get(slot);
        }

        public ItemDefinition GetEquippedItem(EquipmentSlot slot)
        {
            var catalogue = Items;
            var itemId = _loadout.Get(slot);
            return catalogue != null && !string.IsNullOrEmpty(itemId)
                ? catalogue.GetItem(itemId)
                : null;
        }

        public bool IsEquipped(string itemId)
        {
            return _loadout.Contains(itemId);
        }

        public bool IsEquippedIn(string itemId, EquipmentSlot slot)
        {
            return !string.IsNullOrEmpty(itemId) && _loadout.Get(slot) == itemId;
        }

        public bool CanEquip(string itemId, EquipmentSlot slot)
        {
            return Check(itemId, slot, ignoreOtherSlot: false) == EquipResult.Equipped;
        }

        /// <summary>The slot this item would go into if the player just pressed "equip".</summary>
        public EquipmentSlot DefaultSlotFor(string itemId)
        {
            var catalogue = Items;
            if (catalogue == null || !catalogue.TryGetItem(itemId, out var definition)
                || definition.Equipment == null)
            {
                return EquipmentSlot.MainWeapon;
            }

            var preferred = definition.Equipment.DefaultSlot;
            // A free accessory finger beats overwriting the one already wearing something.
            if (preferred == EquipmentSlot.Accessory1 && !_loadout.IsEmpty(EquipmentSlot.Accessory1)
                && _loadout.IsEmpty(EquipmentSlot.Accessory2))
            {
                return EquipmentSlot.Accessory2;
            }

            if (preferred == EquipmentSlot.MainWeapon
                && !_loadout.IsEmpty(EquipmentSlot.MainWeapon)
                && _loadout.IsEmpty(EquipmentSlot.SecondaryWeapon))
            {
                return EquipmentSlot.SecondaryWeapon;
            }

            return preferred;
        }

        /// <summary>Everything owned that could go in this slot; what the screen lists.</summary>
        public List<string> OwnedItemsFor(EquipmentSlot slot)
        {
            var matches = new List<string>();
            var catalogue = Items;
            if (catalogue == null || _progress == null)
            {
                return matches;
            }

            foreach (var stack in _progress.Inventory.GetItems())
            {
                if (catalogue.TryGetItem(stack.itemId, out var definition)
                    && definition.Equipment != null && definition.Equipment.FitsSlot(slot))
                {
                    matches.Add(stack.itemId);
                }
            }

            return matches;
        }

        /// <summary>Every modifier currently worn, with the item each one came from.</summary>
        public void CollectModifiers(List<StatModifier> into, List<string> sources = null)
        {
            var catalogue = Items;
            if (into == null || catalogue == null)
            {
                return;
            }

            foreach (var slot in EquipmentSlots.All)
            {
                // The sheathed weapon is carried, not used: it grants nothing.
                if (EquipmentSlots.IsWeapon(slot) && slot != ActiveWeaponSlot)
                {
                    continue;
                }

                var itemId = _loadout.Get(slot);
                if (string.IsNullOrEmpty(itemId)
                    || !catalogue.TryGetItem(itemId, out var definition)
                    || definition.Equipment == null)
                {
                    continue;
                }

                foreach (var modifier in definition.Equipment.Modifiers)
                {
                    if (modifier == null)
                    {
                        continue;
                    }

                    into.Add(modifier);
                    sources?.Add(itemId);
                }
            }
        }

        /// <summary>
        /// Whether the character can wield this weapon. The final attributes count, so a ring
        /// that grants strength really does open a heavier weapon up.
        /// </summary>
        public bool MeetsRequirements(WeaponDefinition weapon)
        {
            if (weapon == null || !weapon.HasRequirements || _stats == null)
            {
                return true;
            }

            return weapon.MeetsRequirements(_stats.GetFinalStat(CharacterStat.Strength),
                _stats.GetFinalStat(CharacterStat.Dexterity));
        }

        public ArmorDefinition ArmorIn(EquipmentSlot slot)
        {
            return GetEquippedItem(slot)?.Equipment as ArmorDefinition;
        }

        // ---------- changing slots ----------

        public EquipResult Equip(string itemId, EquipmentSlot slot)
        {
            var reason = Check(itemId, slot, ignoreOtherSlot: false);
            if (reason != EquipResult.Equipped)
            {
                return reason;
            }

            // Moving a weapon from one hand to the other must not leave a copy behind.
            foreach (var other in EquipmentSlots.All)
            {
                if (other != slot && _loadout.Get(other) == itemId)
                {
                    _loadout.Set(other, string.Empty);
                }
            }

            _loadout.Set(slot, itemId);
            if (_loadout.IsEmpty(EquipmentSlot.SecondaryWeapon))
            {
                _loadout.isSecondaryActive = false;
            }

            Commit(raiseWeaponChange: EquipmentSlots.IsWeapon(slot));
            return EquipResult.Equipped;
        }

        /// <summary>Equips into whichever slot suits the item; what the inventory button does.</summary>
        public EquipResult Equip(string itemId)
        {
            return Equip(itemId, DefaultSlotFor(itemId));
        }

        public bool Unequip(EquipmentSlot slot)
        {
            if (_loadout.IsEmpty(slot))
            {
                return false;
            }

            if (slot == EquipmentSlot.MainWeapon)
            {
                // There is no unarmed moveset, so the main hand cannot simply be emptied.
                Debug.LogWarning("The main weapon cannot be unequipped; equip another one instead.");
                return false;
            }

            var wasActive = slot == ActiveWeaponSlot;
            _loadout.Set(slot, string.Empty);
            if (slot == EquipmentSlot.SecondaryWeapon)
            {
                _loadout.isSecondaryActive = false;
            }

            Commit(raiseWeaponChange: wasActive || EquipmentSlots.IsWeapon(slot));
            return true;
        }

        /// <summary>Swaps the hands. Does nothing at all when there is no second weapon.</summary>
        public bool SwitchWeapon()
        {
            if (!HasSecondaryWeapon)
            {
                return false;
            }

            _loadout.isSecondaryActive = !_loadout.isSecondaryActive;
            Commit(raiseWeaponChange: true);
            return true;
        }

        private EquipResult Check(string itemId, EquipmentSlot slot, bool ignoreOtherSlot)
        {
            var catalogue = Items;
            if (string.IsNullOrEmpty(itemId) || catalogue == null
                || !catalogue.TryGetItem(itemId, out var definition))
            {
                return EquipResult.UnknownItem;
            }

            if (definition.Equipment == null)
            {
                return EquipResult.NotEquippable;
            }

            if (!definition.Equipment.FitsSlot(slot))
            {
                return EquipResult.WrongSlot;
            }

            if (_progress == null || !_progress.HasItem(itemId))
            {
                return EquipResult.NotOwned;
            }

            if (_loadout.Get(slot) == itemId)
            {
                return EquipResult.AlreadyEquipped;
            }

            if (!ignoreOtherSlot && !CanSpare(itemId, slot, definition))
            {
                return EquipResult.AlreadyInOtherSlot;
            }

            // Requirements gate picking a weapon up, not keeping it: a loadout loaded from disk
            // passes through (ignoreOtherSlot), so a save can never become unloadable because a
            // requirement was added or a ring was taken off.
            if (!ignoreOtherSlot && !MeetsRequirements(definition.Weapon))
            {
                return EquipResult.RequirementsNotMet;
            }

            return EquipResult.Equipped;
        }

        /// <summary>
        /// One copy cannot be in two places. Accessories are stricter on purpose: the same ring in
        /// both slots would double its bonus, so it is refused even with two copies owned.
        /// </summary>
        private bool CanSpare(string itemId, EquipmentSlot slot, ItemDefinition definition)
        {
            var usedElsewhere = 0;
            foreach (var other in EquipmentSlots.All)
            {
                if (other != slot && _loadout.Get(other) == itemId)
                {
                    usedElsewhere++;
                }
            }

            if (usedElsewhere == 0)
            {
                return true;
            }

            if (EquipmentSlots.IsWeapon(slot))
            {
                // Moving a weapon into the main hand is a move, not a copy: the other hand is
                // emptied and nothing is lost. Moving it out of the main hand is refused, because
                // the main hand must never end up empty.
                return _loadout.Get(EquipmentSlot.MainWeapon) != itemId;
            }

            // Two of the same ring would pay their bonus twice, so a second copy does not help.
            if (EquipmentSlots.IsAccessory(slot))
            {
                return false;
            }

            return _progress.GetItemAmount(itemId) > usedElsewhere;
        }

        /// <summary>Writes the loadout back into progression and tells everyone what changed.</summary>
        private void Commit(bool raiseWeaponChange)
        {
            if (_progress != null)
            {
                _progress.SetLoadout(_loadout);
            }

            var previous = _activeWeapon;
            ResolveActiveWeapon();
            EquipmentChanged?.Invoke();
            if (raiseWeaponChange || previous?.ItemId != _activeWeapon?.ItemId)
            {
                ActiveWeaponChanged?.Invoke(_activeWeapon);
            }
        }

        private void ResolveActiveWeapon()
        {
            var itemId = _loadout.Get(ActiveWeaponSlot);
            var catalogue = Items;
            if (string.IsNullOrEmpty(itemId) || catalogue == null
                || !catalogue.TryGetItem(itemId, out var definition) || definition.Weapon == null)
            {
                _activeWeapon = null;
                return;
            }

            _activeWeapon = new EquippedWeapon(itemId, definition.Weapon);
        }
    }
}
