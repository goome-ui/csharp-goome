using System;
using System.Collections.Generic;
using System.Text;
using GoomeSharp;
using UnityEngine;
using UnityEngine.SceneManagement;
using Event = GoomeSharp.Event;

namespace GoomeUI.ModKit
{
    public sealed class GoomeMod : IDisposable
    {
        static readonly List<GoomeMod> live = new List<GoomeMod>();

        static readonly string[] keyNames =
        {
            "Backspace", "Tab", "Return", "KeypadEnter", "Escape", "Delete", "LeftArrow", "RightArrow", "UpArrow", "DownArrow",
            "Home", "End", "PageUp", "PageDown", "Insert",
            "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            "Alpha0", "Alpha1", "Alpha2", "Alpha3", "Alpha4", "Alpha5", "Alpha6", "Alpha7", "Alpha8", "Alpha9",
        };

        static readonly int[] codes =
        {
            Keys.Backspace, Keys.Tab, Keys.Enter, Keys.Enter, Keys.Esc, Keys.Delete, Keys.Left, Keys.Right, Keys.Up, Keys.Down,
            Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown, Keys.Insert,
            Keys.F1, Keys.F1 + 1, Keys.F1 + 2, Keys.F1 + 3, Keys.F1 + 4, Keys.F1 + 5, Keys.F1 + 6, Keys.F1 + 7, Keys.F1 + 8, Keys.F1 + 9, Keys.F1 + 10, Keys.F12,
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        };

        const int FirstShortcut = 27;

        readonly string name;
        readonly float[] heldFor = new float[codes.Length];
        readonly int[] keys = new int[codes.Length];
        readonly StringBuilder typed = new StringBuilder();
        InputSystemBridge? bridge;
        bool legacy, inputChecked, disposed, open, dirty = true, captured, pressedOnUi, hotkeyWas, checkClip, cursorSaved, cursorWasVisible;
        CursorLockMode cursorWasLock;
        int lastFrame = -10, w, h, buttons, hotkey = -1, scene;
        float gx, gy;
        string hotkeyName;
        string clip = "";
        Texture2D? tex;
        byte[]? upload;

        public GoomeMod(string name, string hotkey = "F8", params string?[] nativeDirs)
        {
            this.name = name;
            hotkeyName = hotkey;
            var dirs = new List<string?>(nativeDirs) { NativeLoader.DirOf(typeof(GoomeMod).Assembly), NativeLoader.DirOf(typeof(Goome).Assembly) };
            NativeLoader.Load(dirs.ToArray());
            InputBlock.Install(s => Log?.Invoke(s));
            live.Add(this);
        }

        public event Action<Ui>? Draw;

        public Action<string>? Log { get; set; }

        public Goome? Goome { get; private set; }

        public Ui? Ui { get; private set; }

        public double Scale { get; set; } = 1;

        public bool ShowCursor { get; set; } = true;

        public bool BlockAllWhileOpen { get; set; }

        public bool RedrawEveryFrame { get; set; }

        public bool WantsMouse { get; private set; }

        public bool WantsKeyboard { get; private set; }

        public bool PointerOverUi { get; private set; }

        public int Uploads { get; private set; }

        public int FramesDrawn { get; private set; }

        public Texture2D? Texture => tex;

        public bool UsingInputSystem => bridge != null && !legacy;

        public string Hotkey
        {
            get => hotkeyName;
            set
            {
                hotkeyName = value;
                hotkey = -1;
                inputChecked = false;
            }
        }

        public bool Open
        {
            get => open;
            set
            {
                if (open == value) return;
                open = value;
                dirty = true;
                if (!open) Close();
            }
        }

        public void Redraw() => dirty = true;

        public void Toggle() => Open = !Open;

        internal static bool Blocks(bool mouse)
        {
            if (live.Count == 0 || !Application.isFocused) return false;
            int frame = Time.frameCount;
            foreach (var m in live)
                if (m.open && frame - m.lastFrame <= 1 && (mouse ? m.WantsMouse : m.WantsKeyboard)) return true;
            return false;
        }

        public static void ReleaseAll()
        {
            foreach (var m in live.ToArray()) m.Release();
        }

        public void Update()
        {
            if (disposed) return;
            try
            {
                Step();
            }
            catch (Exception e)
            {
                Release();
                Log?.Invoke("goome: " + e);
            }
        }

        void Step()
        {
            lastFrame = Time.frameCount;
            int s = SceneManager.GetActiveScene().handle;
            if (s != scene)
            {
                if (scene != 0) SceneChanged();
                scene = s;
            }
            if (!Application.isFocused)
            {
                Release();
                return;
            }
            using (InputBlock.Read())
            {
                PickInput();
                bool hk = HeldKey(hotkey);
                if (hk && !hotkeyWas) Toggle();
                hotkeyWas = hk;
                if (!open)
                {
                    Release();
                    return;
                }
                if (!OpenGoome()) return;
                if (ShowCursor) KeepCursor();
                Pump();
            }
            bool any = false;
            while (Goome!.Poll(out var ev, 0))
            {
                Ui!.Feed(ev);
                any = true;
            }
            if (any || dirty || RedrawEveryFrame || Ui!.Animating) Render();
            if (!open)
            {
                Release();
                return;
            }
            PointerOverUi = Goome.Hit(gx, gy);
            var wants = Ui!.Wants;
            WantsMouse = BlockAllWhileOpen || ((wants & Want.Mouse) != 0 && (buttons == 0 || pressedOnUi));
            WantsKeyboard = BlockAllWhileOpen || (wants & Want.Keyboard) != 0;
            InputBlock.Actions(bridge, WantsMouse, WantsKeyboard);
            SyncClipboard();
        }

        public void OnGUI()
        {
            if (disposed || !open || tex == null) return;
            var e = UnityEngine.Event.current;
            if (e == null || e.type != UnityEngine.EventType.Repaint) return;
            GUI.depth = -10000;
            GUI.DrawTexture(new UnityEngine.Rect(0, 0, Screen.width, Screen.height), tex);
            FramesDrawn++;
        }

        public void SceneChanged()
        {
            Release();
            buttons = 0;
            captured = pressedOnUi = false;
            dirty = true;
        }

        public void FocusChanged(bool focused)
        {
            if (!focused) Release();
            hotkeyWas = true;
        }

        public void Release()
        {
            WantsMouse = WantsKeyboard = false;
            InputBlock.RestoreAll(bridge);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Release();
            Close();
            live.Remove(this);
            InputBlock.Uninstall();
            bridge?.UnhookText();
            Ui?.Dispose();
            Goome?.Dispose();
            Ui = null;
            Goome = null;
            if (tex != null) UnityEngine.Object.Destroy(tex);
            tex = null;
            upload = null;
        }

        void Close()
        {
            Release();
            buttons = 0;
            captured = pressedOnUi = false;
            PointerOverUi = false;
            if (cursorSaved)
            {
                cursorSaved = false;
                Cursor.visible = cursorWasVisible;
                Cursor.lockState = cursorWasLock;
            }
        }

        void KeepCursor()
        {
            if (!cursorSaved)
            {
                cursorSaved = true;
                cursorWasVisible = Cursor.visible;
                cursorWasLock = Cursor.lockState;
            }
            if (!Cursor.visible) Cursor.visible = true;
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        }

        void PickInput()
        {
            if (inputChecked) return;
            inputChecked = true;
            try
            {
                _ = Input.mousePosition;
                legacy = true;
            }
            catch (InvalidOperationException)
            {
                legacy = false;
            }
            bridge ??= InputSystemBridge.Find();
            if (!legacy && bridge == null) Log?.Invoke("goome: the old Input class is off and the Input System isnt there, cant read input");
            for (int i = 0; i < keys.Length; i++) keys[i] = ParseKey(keyNames[i]);
            hotkey = ParseKey(hotkeyName);
            if (hotkey < 0) Log?.Invoke($"goome: dont know the key {hotkeyName}");
        }

        int ParseKey(string k)
        {
            if (!legacy) return bridge?.Parse(k) ?? -1;
            try
            {
                return (int)(KeyCode)Enum.Parse(typeof(KeyCode), k, true);
            }
            catch (ArgumentException)
            {
                return -1;
            }
        }

        bool HeldKey(int k)
        {
            if (k < 0) return false;
            if (legacy) return Input.GetKey((KeyCode)k);
            return bridge != null && bridge.Held(k);
        }

        bool Pointer(out float x, out float y)
        {
            if (legacy)
            {
                var p = Input.mousePosition;
                x = p.x;
                y = p.y;
                return true;
            }
            x = y = 0;
            return bridge != null && bridge.Pointer(out x, out y);
        }

        bool Button(int i) => legacy ? Input.GetMouseButton(i) : bridge != null && bridge.Button(i);

        float Scroll() => legacy ? Input.mouseScrollDelta.y : bridge?.Scroll() ?? 0;

        Mods Modifiers()
        {
            var r = Mods.None;
            if (legacy)
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) r |= Mods.Shift;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) r |= Mods.Ctrl;
                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) r |= Mods.Alt;
                if (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) r |= Mods.Super;
                return r;
            }
            if (bridge == null) return r;
            if (bridge.Shift) r |= Mods.Shift;
            if (bridge.Ctrl) r |= Mods.Ctrl;
            if (bridge.Alt) r |= Mods.Alt;
            if (bridge.Super) r |= Mods.Super;
            return r;
        }

        string TakeText()
        {
            if (legacy) typed.Append(Input.inputString);
            else bridge?.HookText(c => typed.Append(c));
            if (typed.Length == 0) return "";
            var s = typed.ToString();
            typed.Clear();
            return s;
        }

        bool OpenGoome()
        {
            double sc = Math.Max(0.25, Scale);
            int nw = Math.Max(1, (int)(Screen.width / sc)), nh = Math.Max(1, (int)(Screen.height / sc));
            if (Goome == null)
            {
                try
                {
                    Goome = Goome.OpenOffscreen(nw, nh, sc, name);
                    Ui = new Ui(Goome);
                }
                catch (Exception e)
                {
                    Log?.Invoke("goome: cant start the offscreen ui, " + e.Message);
                    Goome?.Dispose();
                    Goome = null;
                    open = false;
                    return false;
                }
                w = nw;
                h = nh;
                dirty = true;
            }
            else if (nw != w || nh != h)
            {
                w = nw;
                h = nh;
                Goome.Resize(w, h, sc);
                dirty = true;
            }
            return true;
        }

        void Pump()
        {
            var g = Goome!;
            var mods = Modifiers();
            if (Pointer(out float px, out float py))
            {
                double sc = Math.Max(0.25, Scale);
                float x = (float)(px / sc), y = (float)((Screen.height - py) / sc);
                if (x != gx || y != gy)
                {
                    gx = x;
                    gy = y;
                    g.Push(Event.Motion(x, y));
                }
            }
            for (int i = 0; i < 3; i++)
            {
                int bit = 1 << i, button = i == 0 ? 1 : i == 1 ? 3 : 2;
                bool down = Button(i);
                if (down && (buttons & bit) == 0)
                {
                    if (buttons == 0) pressedOnUi = g.Hit(gx, gy);
                    buttons |= bit;
                    captured = true;
                    g.Push(Event.Down(gx, gy, button, mods));
                }
                else if (!down && (buttons & bit) != 0)
                {
                    buttons &= ~bit;
                    if (captured) g.Push(Event.Up(gx, gy, button, mods));
                    checkClip = true;
                }
            }
            if (buttons == 0) captured = false;
            float wheel = Scroll();
            if (wheel != 0) g.Push(Event.Scroll(gx, gy, wheel > 0 ? -1 : 1));
            PumpKeys(g, mods);
        }

        void PumpKeys(Goome g, Mods mods)
        {
            bool combo = (mods & (Mods.Ctrl | Mods.Alt | Mods.Super)) != 0;
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < codes.Length; i++)
            {
                if (keys[i] == hotkey || !HeldKey(keys[i]))
                {
                    heldFor[i] = 0;
                    continue;
                }
                if (i >= FirstShortcut && !combo) continue;
                float before = heldFor[i];
                heldFor[i] += dt;
                bool fire = before == 0 || (heldFor[i] > 0.45f && (int)((heldFor[i] - 0.45f) / 0.035f) != (int)((before - 0.45f) / 0.035f));
                if (!fire) continue;
                int code = codes[i];
                if (combo && code == 'v')
                {
                    string sys = GUIUtility.systemCopyBuffer ?? "";
                    if (sys != clip)
                    {
                        clip = sys;
                        g.SetClipboard(sys);
                    }
                }
                else if (combo && (code == 'c' || code == 'x'))
                {
                    checkClip = true;
                }
                g.Push(Event.KeyPress(code, mods));
            }
            string text = TakeText();
            if (combo) return;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < 32 || c == 127) continue;
                int cp = char.IsSurrogatePair(text, i) ? char.ConvertToUtf32(c, text[++i]) : c;
                g.Push(Event.Char(cp, mods & Mods.Shift));
            }
        }

        void SyncClipboard()
        {
            if (!checkClip || Goome == null) return;
            checkClip = false;
            string? now = Goome.GetClipboard();
            if (now != null && now != clip)
            {
                clip = now;
                GUIUtility.systemCopyBuffer = now;
            }
        }

        void Render()
        {
            dirty = false;
            var ui = Ui!;
            if (ui.Begin(0) == null) return;
            try
            {
                Draw?.Invoke(ui);
            }
            catch (Exception e)
            {
                Log?.Invoke("goome: the menu threw " + e);
            }
            ui.End();
            var g = Goome!;
            if (!g.TryFrame(out var f)) return;
            bool fresh = tex == null || upload == null || tex.width != f.Width || tex.height != f.Height;
            if (fresh)
            {
                if (tex != null) UnityEngine.Object.Destroy(tex);
                tex = new Texture2D(f.Width, f.Height, TextureFormat.BGRA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Point;
                tex.hideFlags = HideFlags.HideAndDontSave;
                upload = new byte[f.Width * f.Height * 4];
            }
            if (!g.FrameDirty(out var r) && !fresh) return;
            if (fresh) r = new GoomeSharp.Rect(0, 0, f.Width, f.Height);
            var buf = upload!;
            int stride = f.Width * 4;
            for (int y = r.Y; y < r.Y + r.H; y++)
                g.FrameCopy(buf.AsSpan((f.Height - 1 - y) * stride + r.X * 4, r.W * 4), stride, GoomeSharp.Format.Bgra, new GoomeSharp.Rect(r.X, y, r.W, 1));
            unsafe
            {
                fixed (byte* p = buf) tex!.LoadRawTextureData((IntPtr)p, buf.Length);
            }
            tex.Apply(false);
            Uploads++;
        }
    }
}
