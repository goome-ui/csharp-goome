using System.Text;
using GoomeSharp;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && GOOME_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace GoomeUI
{
    internal static class GoomeInput
    {
        internal static readonly int[] Codes =
        {
            Keys.Backspace, Keys.Tab, Keys.Enter, Keys.Enter, Keys.Esc, Keys.Delete, Keys.Left, Keys.Right, Keys.Up, Keys.Down,
            Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown, Keys.Insert,
            Keys.F1, Keys.F1 + 1, Keys.F1 + 2, Keys.F1 + 3, Keys.F1 + 4, Keys.F1 + 5, Keys.F1 + 6, Keys.F1 + 7, Keys.F1 + 8, Keys.F1 + 9, Keys.F1 + 10, Keys.F12,
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        };

        internal const int FirstShortcut = 27;

        static readonly StringBuilder typed = new StringBuilder();

#if ENABLE_INPUT_SYSTEM && GOOME_INPUT_SYSTEM
        static readonly Key[] keys =
        {
            Key.Backspace, Key.Tab, Key.Enter, Key.NumpadEnter, Key.Escape, Key.Delete, Key.LeftArrow, Key.RightArrow, Key.UpArrow, Key.DownArrow,
            Key.Home, Key.End, Key.PageUp, Key.PageDown, Key.Insert,
            Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12,
            Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J, Key.K, Key.L, Key.M, Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T, Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
            Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };

        static Keyboard hooked;
        static string composing = "";

        static void OnText(char c) => typed.Append(c);

        static void OnComposition(IMECompositionString s) => composing = s.ToString();

        static void Hook()
        {
            var k = Keyboard.current;
            if (k == hooked) return;
            if (hooked != null)
            {
                hooked.onTextInput -= OnText;
                hooked.onIMECompositionChange -= OnComposition;
            }
            hooked = k;
            composing = "";
            if (k == null) return;
            k.onTextInput += OnText;
            k.onIMECompositionChange += OnComposition;
        }

        internal static string Composition()
        {
            Hook();
            return composing;
        }

        internal static void Ime(bool on, Vector2 caret, bool hasCaret)
        {
            var k = Keyboard.current;
            if (k == null) return;
            k.SetIMEEnabled(on);
            if (on && hasCaret) k.SetIMECursorPosition(caret);
        }

        internal static bool Pointer(out Vector2 pos)
        {
            var m = Mouse.current;
            pos = m != null ? m.position.ReadValue() : Vector2.zero;
            return m != null;
        }

        internal static bool Button(int i)
        {
            var m = Mouse.current;
            if (m == null) return false;
            return i == 0 ? m.leftButton.isPressed : i == 1 ? m.rightButton.isPressed : m.middleButton.isPressed;
        }

        internal static float Scroll()
        {
            var m = Mouse.current;
            return m != null ? m.scroll.ReadValue().y : 0;
        }

        internal static Mods Modifiers()
        {
            var k = Keyboard.current;
            if (k == null) return GoomeSharp.Mods.None;
            var r = GoomeSharp.Mods.None;
            if (k.shiftKey.isPressed) r |= GoomeSharp.Mods.Shift;
            if (k.ctrlKey.isPressed) r |= GoomeSharp.Mods.Ctrl;
            if (k.altKey.isPressed) r |= GoomeSharp.Mods.Alt;
            if (k.leftMetaKey.isPressed || k.rightMetaKey.isPressed) r |= GoomeSharp.Mods.Super;
            return r;
        }

        internal static bool Held(int i)
        {
            var k = Keyboard.current;
            return k != null && k[keys[i]].isPressed;
        }

        internal static string TakeText()
        {
            Hook();
            return Drain();
        }
#else
        static readonly KeyCode[] keys =
        {
            KeyCode.Backspace, KeyCode.Tab, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Escape, KeyCode.Delete, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.DownArrow,
            KeyCode.Home, KeyCode.End, KeyCode.PageUp, KeyCode.PageDown, KeyCode.Insert,
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12,
            KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.N, KeyCode.O, KeyCode.P, KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.T, KeyCode.U, KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z,
            KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9,
        };

        internal static bool Pointer(out Vector2 pos)
        {
            pos = Input.mousePosition;
            return Input.mousePresent;
        }

        internal static bool Button(int i) => Input.GetMouseButton(i);

        internal static float Scroll() => Input.mouseScrollDelta.y;

        internal static Mods Modifiers()
        {
            var r = GoomeSharp.Mods.None;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) r |= GoomeSharp.Mods.Shift;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) r |= GoomeSharp.Mods.Ctrl;
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) r |= GoomeSharp.Mods.Alt;
            if (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) r |= GoomeSharp.Mods.Super;
            return r;
        }

        internal static bool Held(int i) => Input.GetKey(keys[i]);

        internal static string TakeText()
        {
            typed.Append(Input.inputString);
            return Drain();
        }

        internal static string Composition() => Input.compositionString ?? "";

        internal static void Ime(bool on, Vector2 caret, bool hasCaret)
        {
            Input.imeCompositionMode = on ? IMECompositionMode.On : IMECompositionMode.Auto;
            if (on && hasCaret) Input.compositionCursorPos = caret;
        }
#endif

        static string Drain()
        {
            if (typed.Length == 0) return "";
            var s = typed.ToString();
            typed.Clear();
            return s;
        }
    }
}
