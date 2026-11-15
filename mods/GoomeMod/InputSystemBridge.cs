using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GoomeUI.ModKit
{
    internal sealed class InputSystemBridge
    {
        readonly Type keyboardType, keyType, pointerType;
        readonly PropertyInfo mouseCurrent, keyboardCurrent;
        readonly MethodInfo listEnabled;
        readonly Dictionary<string, MemberInfo?> members = new Dictionary<string, MemberInfo?>();
        readonly Dictionary<int, object> keyBoxes = new Dictionary<int, object>();
        object? hookedKeyboard;
        Action<char>? onText;

        InputSystemBridge(Type input, Type mouse, Type keyboard, Type key, Type pointer)
        {
            keyboardType = keyboard;
            keyType = key;
            pointerType = pointer;
            mouseCurrent = mouse.GetProperty("current", BindingFlags.Public | BindingFlags.Static)!;
            keyboardCurrent = keyboard.GetProperty("current", BindingFlags.Public | BindingFlags.Static)!;
            listEnabled = input.GetMethod("ListEnabledActions", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)!;
            if (mouseCurrent == null || keyboardCurrent == null || listEnabled == null) throw new MissingMemberException("UnityEngine.InputSystem");
        }

        internal static InputSystemBridge? Find()
        {
            Type? input = null, mouse = null, keyboard = null, key = null, pointer = null;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                input ??= a.GetType("UnityEngine.InputSystem.InputSystem", false);
                mouse ??= a.GetType("UnityEngine.InputSystem.Mouse", false);
                keyboard ??= a.GetType("UnityEngine.InputSystem.Keyboard", false);
                key ??= a.GetType("UnityEngine.InputSystem.Key", false);
                pointer ??= a.GetType("UnityEngine.InputSystem.Pointer", false);
            }
            if (input == null || mouse == null || keyboard == null || key == null || pointer == null) return null;
            try
            {
                return new InputSystemBridge(input, mouse, keyboard, key, pointer);
            }
            catch (Exception)
            {
                return null;
            }
        }

        object? Get(object? o, string name)
        {
            if (o == null) return null;
            var t = o.GetType();
            string id = t.FullName + "." + name;
            if (!members.TryGetValue(id, out var m))
            {
                m = (MemberInfo?)t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) ?? t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
                members[id] = m;
            }
            return m is PropertyInfo p ? p.GetValue(o, null) : m is FieldInfo f ? f.GetValue(o) : null;
        }

        object? Call(object? o, string name, params object[] args)
        {
            if (o == null) return null;
            var types = new Type[args.Length];
            for (int i = 0; i < args.Length; i++) types[i] = args[i].GetType();
            var m = o.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, types, null);
            return m?.Invoke(o, args);
        }

        bool Pressed(object? button) => Get(button, "isPressed") is bool b && b;

        object? Mouse => mouseCurrent.GetValue(null, null);

        object? Keyboard => keyboardCurrent.GetValue(null, null);

        internal bool Pointer(out float x, out float y)
        {
            x = y = 0;
            var pos = Call(Get(Mouse, "position"), "ReadValue");
            if (!(pos is Vector2 v)) return false;
            x = v.x;
            y = v.y;
            return true;
        }

        internal bool Button(int i) => Pressed(Get(Mouse, i == 0 ? "leftButton" : i == 1 ? "rightButton" : "middleButton"));

        internal float Scroll() => Call(Get(Mouse, "scroll"), "ReadValue") is Vector2 v ? v.y : 0;

        internal bool Shift => Pressed(Get(Keyboard, "shiftKey"));

        internal bool Ctrl => Pressed(Get(Keyboard, "ctrlKey"));

        internal bool Alt => Pressed(Get(Keyboard, "altKey"));

        internal bool Super => Pressed(Get(Keyboard, "leftMetaKey")) || Pressed(Get(Keyboard, "rightMetaKey"));

        internal int Parse(string name)
        {
            foreach (var n in new[] { name, name.Replace("Alpha", "Digit").Replace("Return", "Enter").Replace("KeypadEnter", "NumpadEnter").Replace("BackQuote", "Backquote") })
            {
                try
                {
                    return Convert.ToInt32(Enum.Parse(keyType, n, true));
                }
                catch (ArgumentException)
                {
                }
            }
            return -1;
        }

        internal bool Held(int key)
        {
            if (key < 0) return false;
            var kb = Keyboard;
            if (kb == null) return false;
            if (!keyBoxes.TryGetValue(key, out var box)) keyBoxes[key] = box = Enum.ToObject(keyType, key);
            return Pressed(Call(kb, "get_Item", box));
        }

        internal void HookText(Action<char> handler)
        {
            var kb = Keyboard;
            if (kb == hookedKeyboard) return;
            var ev = keyboardType.GetEvent("onTextInput");
            try
            {
                if (hookedKeyboard != null && onText != null) ev?.RemoveEventHandler(hookedKeyboard, onText);
                hookedKeyboard = kb;
                onText = handler;
                if (kb != null) ev?.AddEventHandler(kb, handler);
            }
            catch (Exception)
            {
                hookedKeyboard = kb;
            }
        }

        internal void UnhookText()
        {
            try
            {
                if (hookedKeyboard != null && onText != null) keyboardType.GetEvent("onTextInput")?.RemoveEventHandler(hookedKeyboard, onText);
            }
            catch (Exception)
            {
            }
            hookedKeyboard = null;
        }

        internal IEnumerable<object> EnabledActions()
        {
            var list = listEnabled.Invoke(null, null);
            var all = new List<object>();
            foreach (var a in Items(list))
                if (a != null) all.Add(a);
            return all;
        }

        internal bool UsesPointer(object action) => Uses(action, pointerType);

        internal bool UsesKeyboard(object action) => Uses(action, keyboardType);

        bool Uses(object action, Type device)
        {
            foreach (var c in Items(Get(action, "controls")))
            {
                var d = Get(c, "device");
                if (d != null && device.IsInstanceOfType(d)) return true;
            }
            return false;
        }

        internal bool IsEnabled(object action) => Get(action, "enabled") is bool b && b;

        internal void SetEnabled(object action, bool on) => Call(action, on ? "Enable" : "Disable");

        IEnumerable<object?> Items(object? list)
        {
            if (list == null) yield break;
            if (list is IEnumerable e)
            {
                foreach (var x in e) yield return x;
                yield break;
            }
            var t = list.GetType();
            var count = t.GetProperty("Count");
            var item = t.GetMethod("get_Item", new[] { typeof(int) });
            if (count == null || item == null) yield break;
            int n = Convert.ToInt32(count.GetValue(list, null));
            for (int i = 0; i < n; i++) yield return item.Invoke(list, new object[] { i });
        }
    }
}
