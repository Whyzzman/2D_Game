using System;
using NUnit.Framework;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The rules of the controls screen (controls.html): a taken key swaps places, no action is
    /// left without a key, fixed keys are refused with a reason, and the choice is kept.
    /// </summary>
    public sealed class KeyBindingTests
    {
        private BindingEditor _editor;

        [SetUp]
        public void SetUp()
        {
            KeyBindings.ResetAll();
            _editor = new BindingEditor();
            Assume.That(KeyBindings.Template, Is.Not.Null, "The project-wide actions are set.");
        }

        [TearDown]
        public void TearDown()
        {
            _editor?.Dispose();
            KeyBindings.ResetAll();
        }

        private static BindingRow Row(string label) =>
            Array.Find(KeyBindings.Rows, row => row.Label == label);

        [Test]
        public void TheDefaultsAreTheAuthoredKeys()
        {
            Assert.That(_editor.Glyph(Row("Атака"), KeyBindings.PrimarySlot), Is.EqualTo("J"));
            Assert.That(_editor.Glyph(Row("Атака"), KeyBindings.AlternativeSlot), Is.EqualTo("ЛКМ"));
            Assert.That(_editor.Glyph(Row("Атака"), KeyBindings.PadSlot), Is.EqualTo("X"));
            Assert.That(_editor.Glyph(Row("Вгору"), KeyBindings.PrimarySlot), Is.EqualTo("W"));
            Assert.That(_editor.Glyph(Row("Вгору"), KeyBindings.AlternativeSlot), Is.Empty);
            Assert.That(_editor.Glyph(Row("Спорядження"), KeyBindings.AlternativeSlot), Is.EqualTo("Tab"));
            Assert.That(_editor.Glyph(Row("Уміння 1"), KeyBindings.PadSlot), Is.EqualTo("LB"),
                "Under LT only the second button of the pair is the slot.");
        }

        [Test]
        public void AFreeKeyChangesOnlyItsSlotAndIsKept()
        {
            var outcome = _editor.Assign(Row("Ривок"), KeyBindings.PrimarySlot, "<Keyboard>/leftShift");
            Assert.That(outcome.Change, Is.EqualTo(BindingChange.Assigned));
            Assert.That(_editor.Glyph(Row("Ривок"), KeyBindings.PrimarySlot), Is.EqualTo("Shift"));
            Assert.That(KeyBindings.Glyph("Dash", gamepad: false), Is.EqualTo("Shift"), "Glyphs follow.");

            using (var again = new BindingEditor())
            {
                Assert.That(again.Glyph(Row("Ривок"), KeyBindings.PrimarySlot), Is.EqualTo("Shift"),
                    "Stored with the settings, so a fresh copy has it.");
            }

            KeyBindings.ResetAll();
            Assert.That(KeyBindings.Glyph("Dash", gamepad: false), Is.EqualTo("L"));
        }

        [Test]
        public void ATakenKeySwapsPlaces()
        {
            var outcome = _editor.Assign(Row("Стрибок"), KeyBindings.PrimarySlot, "<Keyboard>/j");
            Assert.That(outcome.Change, Is.EqualTo(BindingChange.Swapped));
            Assert.That(outcome.Other, Is.SameAs(Row("Атака")));
            Assert.That(_editor.Glyph(Row("Стрибок"), KeyBindings.PrimarySlot), Is.EqualTo("J"));
            Assert.That(_editor.Glyph(Row("Атака"), KeyBindings.PrimarySlot), Is.EqualTo("Space"));
            Assert.That(outcome.Message, Does.Contain("поміняно"));
        }

        [Test]
        public void NoActionIsLeftWithoutItsMainKey()
        {
            // The second slot of Jump is empty; taking Attack's J there would leave Attack bare.
            var outcome = _editor.Assign(Row("Стрибок"), KeyBindings.AlternativeSlot, "<Keyboard>/j");
            Assert.That(outcome.Change, Is.EqualTo(BindingChange.Refused));
            Assert.That(_editor.Glyph(Row("Атака"), KeyBindings.PrimarySlot), Is.EqualTo("J"));
            Assert.That(outcome.Message, Does.Contain("Атака"));
        }

        [Test]
        public void FixedKeysAreRefusedWithAReason()
        {
            Assert.That(_editor.Assign(Row("Стрибок"), KeyBindings.PrimarySlot, "<Keyboard>/escape").Change,
                Is.EqualTo(BindingChange.Refused));
            Assert.That(_editor.Assign(Row("Стрибок"), KeyBindings.PadSlot, "<Gamepad>/start").Change,
                Is.EqualTo(BindingChange.Refused));
            var trigger = _editor.Assign(Row("Блок / парирування"), KeyBindings.PadSlot, "<Gamepad>/leftTrigger");
            Assert.That(trigger.Change, Is.EqualTo(BindingChange.Refused));
            Assert.That(trigger.Message, Does.Contain("LT"));
            Assert.That(_editor.Assign(Row("Вгору"), KeyBindings.PadSlot, "<Gamepad>/buttonSouth").Change,
                Is.EqualTo(BindingChange.Refused), "Moving with the stick is fixed.");
            Assert.That(_editor.Assign(Row("Пауза"), KeyBindings.PrimarySlot, "<Keyboard>/p").Change,
                Is.EqualTo(BindingChange.Refused));
            Assert.That(_editor.Assign(Row("Стрибок"), KeyBindings.PrimarySlot, "<Gamepad>/buttonSouth").Change,
                Is.EqualTo(BindingChange.Refused), "A pad button does not go into a keyboard cell.");
        }

        [Test]
        public void AButtonUnderLtCompetesOnlyWithButtonsUnderLt()
        {
            // Y is Interact on its own and Spell 2 under LT; a plain slot swaps with Interact only.
            var outcome = _editor.Assign(Row("Блок / парирування"), KeyBindings.PadSlot, "<Gamepad>/buttonNorth");
            Assert.That(outcome.Change, Is.EqualTo(BindingChange.Swapped));
            Assert.That(outcome.Other, Is.SameAs(Row("Взаємодія")));
            Assert.That(_editor.Glyph(Row("Взаємодія"), KeyBindings.PadSlot), Is.EqualTo("LB"));
            Assert.That(_editor.Glyph(Row("Закляття 2"), KeyBindings.PadSlot), Is.EqualTo("Y"));
        }

        [Test]
        public void OnlyTheSecondKeyCanBeEmptied()
        {
            Assert.That(_editor.ClearAlternative(Row("Атака")), Is.True);
            Assert.That(_editor.Glyph(Row("Атака"), KeyBindings.AlternativeSlot), Is.Empty);
            Assert.That(_editor.Glyph(Row("Атака"), KeyBindings.PrimarySlot), Is.EqualTo("J"));
            Assert.That(_editor.ClearAlternative(Row("Атака")), Is.False, "Nothing left to clear.");
        }

        [Test]
        public void AnyPadIsStoredAsTheGenericPad()
        {
            Assert.That(KeyBindings.Normalize("<Gamepad>/buttonSouth"), Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(KeyBindings.Normalize("<XInputController>/buttonSouth"), Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(KeyBindings.Display("<Keyboard>/leftShift"), Is.EqualTo("Shift"));
            Assert.That(KeyBindings.Display("<Gamepad>/dpad/up"), Is.EqualTo("▲"));
        }
    }
}
