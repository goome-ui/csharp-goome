using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace GoomeUI.ModKit
{
    public static class InputBlock
    {
        const int MouseFirst = (int)KeyCode.Mouse0, MouseLast = (int)KeyCode.Mouse6;

        static Harmony? harmony;
        static int users, raw;
        static Vector3 lastMouse;

        public static int Patched { get; private set; }

        public static bool Mouse => raw == 0 && GoomeMod.Blocks(true);

        public static bool Keyboard => raw == 0 && GoomeMod.Blocks(false);

        internal static void Install(Action<string>? log)
        {
            if (users++ > 0) return;
            harmony = new Harmony("goome-ui.inputblock");
            Patched = 0;
            foreach (var m in typeof(Input).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                string? prefix = PrefixFor(m), postfix = m.Name == "get_mousePosition" ? nameof(MousePosition) : null;
                if (prefix == null && postfix == null) continue;
                try
                {
                    harmony.Patch(m, prefix: prefix == null ? null : new HarmonyMethod(typeof(InputBlock), prefix), postfix: postfix == null ? null : new HarmonyMethod(typeof(InputBlock), postfix));
                    Patched++;
                }
                catch (Exception e)
                {
                    log?.Invoke($"goome: cant patch Input.{m.Name}, {e.Message}");
                }
            }
        }

        internal static void Uninstall()
        {
            if (users == 0 || --users > 0) return;
            try
            {
                harmony?.UnpatchSelf();
            }
            finally
            {
                harmony = null;
                Patched = 0;
            }
        }

        static string? PrefixFor(MethodInfo m)
        {
            var ps = m.GetParameters();
            var arg = ps.Length == 1 ? ps[0].ParameterType : null;
            switch (m.Name)
            {
                case "GetKey":
                case "GetKeyDown":
                case "GetKeyUp":
                    return arg == typeof(KeyCode) ? nameof(KeyByCode) : arg == typeof(string) ? nameof(KeyByName) : null;
                case "GetMouseButton":
                case "GetMouseButtonDown":
                case "GetMouseButtonUp":
                    return arg == typeof(int) ? nameof(MouseButton) : null;
                case "GetButton":
                case "GetButtonDown":
                case "GetButtonUp":
                    return arg == typeof(string) ? nameof(AnyBool) : null;
                case "GetAxis":
                case "GetAxisRaw":
                    return arg == typeof(string) ? nameof(Axis) : null;
                case "get_anyKey":
                case "get_anyKeyDown":
                    return nameof(AnyBool);
                case "get_mouseScrollDelta":
                    return nameof(Scroll);
                case "get_inputString":
                    return nameof(Typed);
            }
            return null;
        }

        internal struct Raw : IDisposable
        {
            public void Dispose() => raw--;
        }

        internal static Raw Read()
        {
            raw++;
            return default;
        }

        static bool KeyByCode(KeyCode __0, ref bool __result)
        {
            int k = (int)__0;
            if (!(k >= MouseFirst && k <= MouseLast ? Mouse : Keyboard)) return true;
            __result = false;
            return false;
        }

        static bool KeyByName(ref bool __result)
        {
            if (!Keyboard) return true;
            __result = false;
            return false;
        }

        static bool MouseButton(ref bool __result)
        {
            if (!Mouse) return true;
            __result = false;
            return false;
        }

        static bool AnyBool(ref bool __result)
        {
            if (!Mouse && !Keyboard) return true;
            __result = false;
            return false;
        }

        static bool Axis(string __0, ref float __result)
        {
            bool mouseAxis = __0 != null && __0.StartsWith("Mouse", StringComparison.OrdinalIgnoreCase);
            if (!(mouseAxis ? Mouse : Keyboard)) return true;
            __result = 0;
            return false;
        }

        static bool Scroll(ref Vector2 __result)
        {
            if (!Mouse) return true;
            __result = Vector2.zero;
            return false;
        }

        static bool Typed(ref string __result)
        {
            if (!Keyboard) return true;
            __result = "";
            return false;
        }

        static void MousePosition(ref Vector3 __result)
        {
            if (raw > 0) return;
            if (Mouse) __result = lastMouse;
            else lastMouse = __result;
        }

        static readonly List<object> offMouse = new List<object>(), offKeys = new List<object>();

        internal static void Actions(InputSystemBridge? bridge, bool mouse, bool keys)
        {
            if (bridge == null) return;
            if (!mouse) Restore(bridge, offMouse);
            if (!keys) Restore(bridge, offKeys);
            if (!mouse && !keys) return;
            foreach (var a in bridge.EnabledActions())
            {
                if (mouse && bridge.UsesPointer(a)) TurnOff(bridge, a, offMouse);
                else if (keys && bridge.UsesKeyboard(a)) TurnOff(bridge, a, offKeys);
            }
        }

        static void TurnOff(InputSystemBridge bridge, object a, List<object> off)
        {
            bridge.SetEnabled(a, false);
            if (!off.Contains(a)) off.Add(a);
        }

        static void Restore(InputSystemBridge bridge, List<object> off)
        {
            if (off.Count == 0) return;
            var was = off.ToArray();
            off.Clear();
            foreach (var a in was)
            {
                try
                {
                    if (!bridge.IsEnabled(a)) bridge.SetEnabled(a, true);
                }
                catch (Exception)
                {
                }
            }
        }

        internal static void RestoreAll(InputSystemBridge? bridge)
        {
            if (bridge == null) return;
            Restore(bridge, offMouse);
            Restore(bridge, offKeys);
        }
    }
}
