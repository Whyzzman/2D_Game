using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenKingdom
{
    /// <summary>
    /// Keyboard and gamepad reading shared by the title screen and the pause menu. The menus move
    /// a focus of their own rather than depending on an EventSystem; the mouse uses the buttons
    /// directly.
    /// </summary>
    public static class MenuInput
    {
        /// <summary>-1 for up, 1 for down, 0 for neither or both.</summary>
        public static int ReadVertical()
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

        /// <summary>-1 for left, 1 for right, 0 for neither or both.</summary>
        public static int ReadHorizontal()
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

        public static bool Confirmed()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame
                    || keyboard.spaceKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                || (pad != null && pad.buttonSouth.wasPressedThisFrame);
        }

        /// <summary>Escape or the pad's back button.</summary>
        public static bool Cancelled()
        {
            var keyboard = Keyboard.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || PadBackPressed();
        }

        /// <summary>
        /// The pad's back button alone. The pause menu hears Escape through its own action, so it
        /// asks only for this one to avoid acting twice on the same press.
        /// </summary>
        public static bool PadBackPressed()
        {
            var pad = Gamepad.current;
            return pad != null && pad.buttonEast.wasPressedThisFrame;
        }

        /// <summary>
        /// Turns a held direction into one step per repeat interval, so a held stick scrolls a
        /// list at a readable pace. Runs on unscaled time: menus live while the game is paused.
        /// </summary>
        public struct Repeater
        {
            private const float Interval = 0.18f;
            private float _cooldown;

            public int Step(int direction)
            {
                _cooldown -= Time.unscaledDeltaTime;
                if (direction == 0)
                {
                    _cooldown = 0f;
                    return 0;
                }

                if (_cooldown > 0f)
                {
                    return 0;
                }

                _cooldown = Interval;
                return direction;
            }
        }
    }
}
