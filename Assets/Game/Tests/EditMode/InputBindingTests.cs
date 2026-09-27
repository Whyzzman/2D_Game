using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The player's bindings, read from the real asset. One button must never start two actions:
    /// on the pad the guard shared LB with the first skill, so raising a block fired the skill.
    /// Skills and spells now sit under the LT modifier, and the buttons they borrow must belong
    /// to actions <see cref="PlayerInputReader"/> silences while LT is held.
    /// </summary>
    public sealed class InputBindingTests
    {
        private const string AssetPath = "Assets/Settings/InputSystem_Actions.inputactions";
        private const string Modifier = "<Gamepad>/leftTrigger";

        /// <summary>Every action <see cref="PlayerInputReader"/> reads; template leftovers are not played.</summary>
        private static readonly string[] GameActions =
        {
            "Move", "Jump", "LightAttack", "HeavyAttack", "Dash", "Block", "Heal",
            "Interact", "Inventory", "Map", "SwitchWeapon", "ActiveSkill1", "ActiveSkill2",
            "CastSpell1", "CastSpell2", "CastSpell3", "AbilityModifier", "Pause"
        };

        /// <summary>The plain actions the reader ignores while the modifier is held.</summary>
        private static readonly string[] SilencedUnderModifier =
        {
            "LightAttack", "HeavyAttack", "Interact", "Block"
        };

        private InputActionMap _player;

        [SetUp]
        public void SetUp()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            Assert.That(asset, Is.Not.Null);
            _player = asset.FindActionMap("Player", true);
        }

        [Test]
        public void NoTwoGameActionsShareTheSameTrigger()
        {
            var owners = new Dictionary<string, HashSet<string>>();
            foreach (var name in GameActions)
            {
                foreach (var trigger in Triggers(_player.FindAction(name, true)))
                {
                    if (!owners.TryGetValue(trigger, out var set))
                    {
                        owners[trigger] = set = new HashSet<string>();
                    }

                    set.Add(name);
                }
            }

            var clashes = owners.Where(pair => pair.Value.Count > 1)
                .Select(pair => $"{pair.Key}: {string.Join(", ", pair.Value)}")
                .ToList();
            Assert.That(clashes, Is.Empty);
        }

        /// <summary>
        /// Control is left alone on the keyboard: on macOS Ctrl with the arrows — which also
        /// move the player — switches desktops and opens Mission Control mid-fight.
        /// </summary>
        [Test]
        public void NoGameActionIsBoundToControl()
        {
            foreach (var name in GameActions)
            {
                foreach (var trigger in Triggers(_player.FindAction(name, true)))
                {
                    Assert.That(trigger, Does.Not.Contain("Ctrl").IgnoreCase, name);
                }
            }
        }

        /// <summary>
        /// Every action fires on the press itself. A leftover Hold on Interact from the template
        /// made the player hold E for 0.4 s at every altar and door, which nothing asked for.
        /// The guard is held by reading the button, not by a Hold interaction.
        /// </summary>
        [Test]
        public void NoGameActionWaitsForAHold()
        {
            foreach (var name in GameActions)
            {
                var action = _player.FindAction(name, true);
                Assert.That(action.interactions, Does.Not.Contain("Hold"), name);
                foreach (var binding in action.bindings)
                {
                    Assert.That(binding.interactions ?? string.Empty, Does.Not.Contain("Hold"), name);
                }
            }
        }

        /// <summary>
        /// The map holds what the game reads and nothing else. The template's Attack, Look,
        /// Crouch, Previous and Next shared 1, 2, B and the d-pad with real actions.
        /// </summary>
        [Test]
        public void ThePlayerMapHoldsOnlyGameActions()
        {
            var names = _player.actions.Select(action => action.name).ToList();
            Assert.That(names, Is.EquivalentTo(GameActions));
        }

        /// <summary>
        /// One keyboard preset: WASD and the left hand's keys, J/K/L under the right. Arrows
        /// moving the player put movement and the attacks under the same hand.
        /// </summary>
        [Test]
        public void TheArrowKeysAreNotPartOfThePreset()
        {
            foreach (var name in GameActions)
            {
                foreach (var trigger in Triggers(_player.FindAction(name, true)))
                {
                    Assert.That(trigger, Does.Not.Contain("Arrow"), name);
                }
            }
        }

        [Test]
        public void EveryPadSkillAndSpellIsUnderTheModifier()
        {
            foreach (var name in new[]
                { "ActiveSkill1", "ActiveSkill2", "CastSpell1", "CastSpell2", "CastSpell3" })
            {
                var pad = Triggers(_player.FindAction(name, true))
                    .Where(trigger => trigger.Contains("<Gamepad>"))
                    .ToList();
                Assert.That(pad, Has.Count.EqualTo(1), name);
                Assert.That(pad[0], Does.StartWith(Modifier + " + "), name);
            }
        }

        [Test]
        public void ButtonsBorrowedByTheModifierBelongOnlyToSilencedActions()
        {
            var borrowed = new HashSet<string>();
            foreach (var action in _player.actions)
            {
                foreach (var trigger in Triggers(action).Where(t => t.StartsWith(Modifier + " + ")))
                {
                    borrowed.Add(trigger.Substring(Modifier.Length + 3));
                }
            }

            foreach (var name in GameActions.Except(SilencedUnderModifier))
            {
                foreach (var trigger in Triggers(_player.FindAction(name, true)))
                {
                    Assert.That(borrowed, Does.Not.Contain(trigger),
                        $"{name} would fire together with a skill or spell.");
                }
            }

            foreach (var trigger in GameActions.Where(name => name != "AbilityModifier")
                .SelectMany(name => Triggers(_player.FindAction(name, true))))
            {
                Assert.That(trigger, Is.Not.EqualTo(Modifier), "LT is the modifier only.");
            }
        }

        /// <summary>
        /// What physically starts an action: a plain control, one part of a vector composite, or
        /// "modifier + button" for a modifier composite.
        /// </summary>
        private static IEnumerable<string> Triggers(InputAction action)
        {
            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding.isComposite && binding.path.StartsWith("OneModifier"))
                {
                    string modifier = null;
                    string button = null;
                    for (var j = i + 1; j < bindings.Count && bindings[j].isPartOfComposite; j++)
                    {
                        if (bindings[j].name == "modifier") { modifier = bindings[j].path; }
                        if (bindings[j].name == "binding") { button = bindings[j].path; }
                    }

                    yield return $"{modifier} + {button}";
                    continue;
                }

                if (binding.isComposite)
                {
                    continue;
                }

                var isModifierPart = binding.isPartOfComposite && i > 0
                    && PartOfModifierComposite(bindings, i);
                // An empty binding is a free second key for the controls screen, not a trigger.
                if (!isModifierPart && !string.IsNullOrEmpty(binding.path))
                {
                    yield return binding.path;
                }
            }
        }

        private static bool PartOfModifierComposite(IReadOnlyList<InputBinding> bindings, int index)
        {
            for (var i = index - 1; i >= 0; i--)
            {
                if (bindings[i].isComposite)
                {
                    return bindings[i].path.StartsWith("OneModifier");
                }
            }

            return false;
        }
    }
}
