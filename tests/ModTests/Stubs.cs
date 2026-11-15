#pragma warning disable CS0067, CS1591
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    public class Object
    {
        public HideFlags hideFlags;
        public bool destroyed;
        public static void Destroy(Object o) => o.destroyed = true;
    }

    [Flags]
    public enum HideFlags { None = 0, HideAndDontSave = 61 }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) => (this.x, this.y) = (x, y);
        public static Vector2 zero => default;
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) => (this.x, this.y, this.z) = (x, y, z);
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) => (this.x, this.y, width, height) = (x, y, w, h);
    }

    public enum TextureFormat { RGBA32 = 4, BGRA32 = 14 }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum CursorLockMode { None, Locked, Confined }

    public class Texture : Object
    {
        public int width, height;
        public TextureWrapMode wrapMode;
        public FilterMode filterMode;
    }

    public class Texture2D : Texture
    {
        public byte[] data;
        public int loads, applies;

        public Texture2D(int w, int h, TextureFormat f, bool mips)
        {
            width = w;
            height = h;
            data = new byte[w * h * 4];
        }

        public void LoadRawTextureData(IntPtr p, int size)
        {
            if (size != data.Length) throw new ArgumentException("size");
            Marshal.Copy(p, data, 0, size);
            loads++;
        }

        public void Apply(bool mips) => applies++;

        public uint Px(int x, int yFromTop)
        {
            int i = ((height - 1 - yFromTop) * width + x) * 4;
            return BitConverter.ToUInt32(data, i);
        }
    }

    public static class Time
    {
        public static int frameCount;
        public static float unscaledDeltaTime = 1 / 60f;
    }

    public static class Application
    {
        public static bool isFocused = true;
    }

    public static class Screen
    {
        public static int width = 800, height = 600;
    }

    public static class Cursor
    {
        public static bool visible;
        public static CursorLockMode lockState;
    }

    public static class GUIUtility
    {
        public static string systemCopyBuffer = "";
    }

    public enum EventType { MouseDown, Repaint = 7, Layout = 8 }

    public class Event
    {
        public EventType type;
        public static Event current = null!;
    }

    public static class GUI
    {
        public static int depth;
        public static int draws;
        public static Texture? last;

        public static void DrawTexture(Rect r, Texture t)
        {
            draws++;
            last = t;
        }
    }

    public enum KeyCode
    {
        None = 0, Backspace = 8, Tab = 9, Return = 13, Escape = 27, Space = 32,
        Alpha0 = 48, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,
        A = 97, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Delete = 127, KeypadEnter = 271, UpArrow = 273, DownArrow, RightArrow, LeftArrow, Insert, Home, End, PageUp, PageDown,
        F1 = 282, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        RightShift = 303, LeftShift, RightControl, LeftControl, RightAlt, LeftAlt, RightCommand, LeftCommand,
        Mouse0 = 323, Mouse1, Mouse2, Mouse3, Mouse4, Mouse5, Mouse6,
    }

    public static class Input
    {
        public static class Fake
        {
            public static readonly HashSet<KeyCode> keys = new HashSet<KeyCode>();
            public static readonly bool[] mouse = new bool[3];
            public static Vector3 position;
            public static Vector2 scroll;
            public static string typed = "";
            public static bool off;
        }

        static void Check()
        {
            if (Fake.off) throw new InvalidOperationException("You are trying to read Input using the UnityEngine.Input class, but you have switched active Input handling to Input System package in Player Settings.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)] public static bool GetKey(KeyCode k) { Check(); return Fake.keys.Contains(k) || (k >= KeyCode.Mouse0 && k <= KeyCode.Mouse2 && Fake.mouse[k - KeyCode.Mouse0]); }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool GetKey(string k) { Check(); return Fake.keys.Contains((KeyCode)Enum.Parse(typeof(KeyCode), k, true)); }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool GetKeyDown(KeyCode k) { Check(); return Fake.keys.Contains(k); }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool GetKeyDown(string k) { Check(); return GetKey(k); }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool GetKeyUp(KeyCode k) { Check(); return false; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool GetKeyUp(string k) { Check(); return false; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool GetMouseButton(int b) { Check(); return Fake.mouse[b]; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool GetMouseButtonDown(int b) { Check(); return Fake.mouse[b]; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool GetMouseButtonUp(int b) { Check(); return false; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool GetButton(string b) { Check(); return Fake.mouse[0]; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool GetButtonDown(string b) { Check(); return Fake.mouse[0]; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool GetButtonUp(string b) { Check(); return false; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static float GetAxis(string a) { Check(); return a.StartsWith("Mouse") ? 1 : Fake.keys.Contains(KeyCode.W) ? 1 : 0; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static float GetAxisRaw(string a) { Check(); return GetAxis(a); }

        public static bool anyKey { [MethodImpl(MethodImplOptions.NoInlining)] get { Check(); return Fake.keys.Count > 0 || Fake.mouse[0]; } }
        public static bool anyKeyDown { [MethodImpl(MethodImplOptions.NoInlining)] get { Check(); return Fake.keys.Count > 0 || Fake.mouse[0]; } }
        public static Vector3 mousePosition { [MethodImpl(MethodImplOptions.NoInlining)] get { Check(); return Fake.position; } }
        public static Vector2 mouseScrollDelta { [MethodImpl(MethodImplOptions.NoInlining)] get { Check(); return Fake.scroll; } }
        public static string inputString { [MethodImpl(MethodImplOptions.NoInlining)] get { Check(); return Fake.typed; } }
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        public int handle;
    }

    public static class SceneManager
    {
        public static int active = 1;
        public static Scene GetActiveScene() => new Scene { handle = active };
    }
}

namespace UnityEngine.InputSystem
{
    public class InputControl
    {
        public InputDevice device = null!;
    }

    public class InputDevice : InputControl { }
    public class Pointer : InputDevice { }

    public class ButtonControl : InputControl
    {
        public Func<bool> read = () => false;
        public bool isPressed => read();
    }

    public class Vector2Control : InputControl
    {
        public Func<Vector2> read = () => default;
        public Vector2 ReadValue() => read();
    }

    public enum Key { None, Space, Enter, Tab, Backspace, Escape, Delete, LeftArrow, RightArrow, UpArrow, DownArrow, Home, End, PageUp, PageDown, Insert, NumpadEnter,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Digit0, Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12 }

    public class Mouse : Pointer
    {
        public static Mouse? current { get; set; }
        public Vector2Control position = new Vector2Control(), scroll = new Vector2Control();
        public ButtonControl leftButton = new ButtonControl(), rightButton = new ButtonControl(), middleButton = new ButtonControl();
    }

    public class Keyboard : InputDevice
    {
        public static Keyboard? current { get; set; }
        public readonly HashSet<Key> down = new HashSet<Key>();
        public ButtonControl shiftKey = new ButtonControl(), ctrlKey = new ButtonControl(), altKey = new ButtonControl(), leftMetaKey = new ButtonControl(), rightMetaKey = new ButtonControl();
        public ButtonControl this[Key k] => new ButtonControl { read = () => down.Contains(k) };
        public event Action<char>? onTextInput;
        public void Type(string s)
        {
            foreach (var c in s) onTextInput?.Invoke(c);
        }
    }

    public class InputAction
    {
        public string name;
        public bool enabled = true;
        public List<InputControl> controls = new List<InputControl>();
        public InputAction(string name) => this.name = name;
        public void Enable() => enabled = true;
        public void Disable() => enabled = false;
    }

    public static class InputSystem
    {
        public static readonly List<InputAction> actions = new List<InputAction>();

        public static List<InputAction> ListEnabledActions()
        {
            var r = new List<InputAction>();
            foreach (var a in actions)
                if (a.enabled) r.Add(a);
            return r;
        }
    }
}
