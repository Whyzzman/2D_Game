using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The interface's typefaces (UI_UX_GUIDE.md §3): Cormorant SC for titles and names, Alegreya
    /// Sans for everything read. Both are bundled under the OFL in Resources/Fonts and carry
    /// Cyrillic; Cinzel of the first HTML design has none. When a font is missing (an old
    /// checkout) the OS serif and sans stand in, and on a headless runner with neither the
    /// theme's own font is used. Created once, on first use.
    /// </summary>
    public static class HudFonts
    {
        private const string DisplayResource = "Fonts/CormorantSC-Bold";
        private const string BodyResource = "Fonts/AlegreyaSans-Regular";

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
            _display = Resources.Load<Font>(DisplayResource);
            _body = Resources.Load<Font>(BodyResource);
            if (_display != null && _body != null)
            {
                return;
            }

            try
            {
                _display ??= Font.CreateDynamicFontFromOSFont(
                    new[] { "Georgia", "Palatino Linotype", "Times New Roman" }, 32);
                // Arial first: it ships with both Windows and macOS. A face missing on this OS at
                // the head of the list makes UI Toolkit warn on every panel update.
                _body ??= Font.CreateDynamicFontFromOSFont(
                    new[] { "Arial", "Segoe UI", "Liberation Sans" }, 24);
            }
            catch (Exception)
            {
                // No OS fonts (a headless runner): the theme's own font stands in for the missing one.
            }
        }
    }
}
