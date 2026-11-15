using System;
using System.Collections.Generic;
using GoomeSharp;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && GOOME_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;
using Canvas = UnityEngine.Canvas;
using RawImage = UnityEngine.UI.RawImage;
using Event = GoomeSharp.Event;

namespace GoomeUI
{
    [DisallowMultipleComponent]
    public class GoomeView : MonoBehaviour, ICanvasRaycastFilter
    {
        [Tooltip("Size of the ui when its on a mesh. On a RawImage it follows the RectTransform")]
        public Vector2Int size = new Vector2Int(960, 640);
        [Tooltip("Texture px per ui px when its on a mesh. On a RawImage it uses the canvas scale")]
        public float pixelScale = 1;
        [Tooltip("Camera that looks at the mesh, empty = Camera.main")]
        public Camera worldCamera;
        public bool redrawEveryFrame = true;
        public bool takeKeyboard = true;
        public bool syncClipboard = true;
        public bool lightTheme;
#if ENABLE_INPUT_SYSTEM && GOOME_INPUT_SYSTEM
        [Tooltip("While goome wants the mouse or keyboard, turn off every InputAction bound to it and turn them back on after")]
        public bool blockGameInput = true;
#endif
        [Tooltip("Optional cursor pictures, one per GoomeSharp.CursorShape (Arrow, IBeam, Hand, Move, ResizeEW, ResizeNS, ResizeNWSE, ResizeNESW, Wait, No)")]
        public Texture2D[] cursors = new Texture2D[10];
        public Shader shader;

        public event Action<Ui> Draw;

        public Goome Goome { get; private set; }
        public Ui Ui { get; private set; }
        public Texture2D Texture => tex;
        public bool PointerOverUi { get; private set; }
        public bool PointerInside { get; private set; }
        public bool WantsMouse { get; private set; }
        public bool WantsKeyboard { get; private set; }

        public static bool AnyWantsMouse
        {
            get
            {
                foreach (var v in live)
                    if (v.WantsMouse) return true;
                return false;
            }
        }

        public static bool AnyWantsKeyboard
        {
            get
            {
                foreach (var v in live)
                    if (v.WantsKeyboard) return true;
                return false;
            }
        }

        static readonly List<GoomeView> live = new List<GoomeView>();
        static bool appFocused = true;

        RawImage image;
        Renderer rend;
        Canvas canvas;
        Texture2D tex;
        Material mat;
        byte[] px;
        int w, h, buttons, shape = -1;
        double scale;
        float gx, gy;
        bool dirty = true, captured, checkClip, pressedOnUi;
        string clip = "";
        readonly float[] heldFor = new float[GoomeInput.Codes.Length];

        void OnEnable()
        {
            image = GetComponent<RawImage>();
            rend = image == null ? GetComponent<Renderer>() : null;
            canvas = GetComponentInParent<Canvas>();
            Measure(out w, out h, out scale);
            try
            {
                Goome = Goome.OpenOffscreen(w, h, scale, name);
                Ui = new Ui(Goome);
            }
            catch (Exception e)
            {
                Debug.LogError("goome: cant start the offscreen ui, " + e.Message);
                Goome?.Dispose();
                Goome = null;
                enabled = false;
                return;
            }
            if (lightTheme) Ui.Theme.Light();
            if (shader == null) shader = Shader.Find("GoomeUI/Premultiplied");
            mat = new Material(shader);
            if (image != null)
            {
                mat.SetFloat("_ZTest", 8);
                image.material = mat;
                image.uvRect = new UnityEngine.Rect(0, 1, 1, -1);
            }
            else if (rend != null)
            {
                rend.material = mat;
                mat.mainTextureScale = new Vector2(1, -1);
                mat.mainTextureOffset = new Vector2(0, 1);
            }
            dirty = true;
            live.Add(this);
        }

        void OnDisable()
        {
            live.Remove(this);
            WantsMouse = WantsKeyboard = false;
            Block();
            Ui?.Dispose();
            Goome?.Dispose();
            Ui = null;
            Goome = null;
            if (tex != null) Destroy(tex);
            if (mat != null) Destroy(mat);
            tex = null;
            mat = null;
            if (shape > 0) SetCursor(0);
            shape = -1;
        }

        void OnDestroy()
        {
            live.Remove(this);
            Block();
        }

        void OnApplicationFocus(bool focus)
        {
            appFocused = focus;
            Block();
        }

        public void Redraw() => dirty = true;

        public bool Push(in Event ev) => Goome != null && Goome.Push(ev);

        public bool IsRaycastLocationValid(Vector2 screen, Camera cam) => Goome != null && ToUi(screen, cam, out float x, out float y) && Goome.Hit(x, y);

        void Update()
        {
            if (Goome == null) return;
            Measure(out int nw, out int nh, out double ns);
            if (nw != w || nh != h || Math.Abs(ns - scale) > 0.001)
            {
                w = nw;
                h = nh;
                scale = ns;
                Goome.Resize(w, h, scale);
            }
            Pump();
            bool any = false;
            while (Goome.Poll(out var ev, 0))
            {
                Ui.Feed(ev);
                any = true;
            }
            if (any || dirty || redrawEveryFrame || Ui.Animating) Render();
            PointerOverUi = PointerInside && Goome.Hit(gx, gy);
            var wants = Ui.Wants;
            WantsMouse = (wants & Want.Mouse) != 0 && (buttons != 0 ? pressedOnUi : PointerInside);
            WantsKeyboard = takeKeyboard && (wants & Want.Keyboard) != 0;
            Block();
            Sync();
        }

        void Render()
        {
            dirty = false;
            if (Ui.Begin(0) == null) return;
            try
            {
                Draw?.Invoke(Ui);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            Ui.End();
            if (!Goome.TryFrame(out var f)) return;
            bool fresh = tex == null || tex.width != f.Width || tex.height != f.Height;
            if (fresh)
            {
                if (tex != null) Destroy(tex);
                tex = new Texture2D(f.Width, f.Height, TextureFormat.BGRA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                if (image != null) image.texture = tex;
                else if (mat != null) mat.mainTexture = tex;
                px = new byte[f.Width * f.Height * 4];
            }
            if (!Goome.FrameDirty(out var r) && !fresh) return;
            if (fresh) r = new GoomeSharp.Rect(0, 0, f.Width, f.Height);
            int stride = f.Width * 4;
            Goome.FrameCopy(px.AsSpan(r.Y * stride + r.X * 4), stride, GoomeSharp.Format.BgraPremul, r);
            tex.SetPixelData(px, 0);
            tex.Apply(false);
        }

        void Measure(out int mw, out int mh, out double ms)
        {
            if (image != null)
            {
                var r = image.rectTransform.rect;
                mw = Math.Max(1, Mathf.RoundToInt(r.width));
                mh = Math.Max(1, Mathf.RoundToInt(r.height));
                ms = canvas != null ? Math.Max(0.25, canvas.scaleFactor) : 1;
                return;
            }
            mw = Math.Max(1, size.x);
            mh = Math.Max(1, size.y);
            ms = Math.Max(0.25, pixelScale);
        }

        Camera EventCamera()
        {
            if (image != null) return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return worldCamera != null ? worldCamera : Camera.main;
        }

        bool ToUi(Vector2 screen, Camera cam, out float x, out float y)
        {
            x = y = 0;
            if (image != null)
            {
                var rt = image.rectTransform;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, cam, out var local)) return false;
                var r = rt.rect;
                x = (local.x - r.xMin) * w / r.width;
                y = (r.yMax - local.y) * h / r.height;
                return x >= 0 && y >= 0 && x < w && y < h;
            }
            if (cam == null) cam = EventCamera();
            if (cam == null || rend == null) return false;
            if (!Physics.Raycast(cam.ScreenPointToRay(screen), out var hit) || hit.transform != transform) return false;
            x = hit.textureCoord.x * w;
            y = (1 - hit.textureCoord.y) * h;
            return true;
        }

        void Pump()
        {
            if (!GoomeInput.Pointer(out var screen)) return;
            var mods = GoomeInput.Modifiers();
            bool inside = ToUi(screen, EventCamera(), out float x, out float y);
            PointerInside = inside;
            if (inside || captured)
            {
                if (x != gx || y != gy)
                {
                    gx = x;
                    gy = y;
                    Goome.Push(Event.Motion(x, y));
                }
            }
            for (int i = 0; i < 3; i++)
            {
                int bit = 1 << i, button = i == 0 ? 1 : i == 1 ? 3 : 2;
                bool down = GoomeInput.Button(i);
                if (down && (buttons & bit) == 0)
                {
                    if (buttons == 0) pressedOnUi = inside && Goome.Hit(x, y);
                    buttons |= bit;
                    if (inside)
                    {
                        captured = true;
                        Goome.Push(Event.Down(x, y, button, mods));
                    }
                }
                else if (!down && (buttons & bit) != 0)
                {
                    buttons &= ~bit;
                    if (captured) Goome.Push(Event.Up(gx, gy, button, mods));
                    checkClip = true;
                }
            }
            if (buttons == 0) captured = false;
            float wheel = GoomeInput.Scroll();
            if (inside && wheel != 0) Goome.Push(Event.Scroll(x, y, wheel > 0 ? -1 : 1));
            if (takeKeyboard) PumpKeys(mods);
        }

        void PumpKeys(Mods mods)
        {
            bool combo = (mods & (Mods.Ctrl | Mods.Alt | Mods.Super)) != 0;
            for (int i = 0; i < GoomeInput.Codes.Length; i++)
            {
                if (!GoomeInput.Held(i))
                {
                    heldFor[i] = 0;
                    continue;
                }
                if (i >= GoomeInput.FirstShortcut && !combo) continue;
                float before = heldFor[i];
                heldFor[i] += Time.unscaledDeltaTime;
                bool fire = before == 0 || (heldFor[i] > 0.45f && (int)((heldFor[i] - 0.45f) / 0.035f) != (int)((before - 0.45f) / 0.035f));
                if (!fire) continue;
                int code = GoomeInput.Codes[i];
                if (syncClipboard && combo && (code == 'v' || code == 'x' || code == 'c'))
                {
                    if (code == 'v') PasteIn();
                    else checkClip = true;
                }
                Goome.Push(Event.KeyPress(code, mods));
            }
            string text = GoomeInput.TakeText();
            if (combo) return;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < 32 || c == 127) continue;
                int cp = char.IsSurrogatePair(text, i) ? char.ConvertToUtf32(c, text[++i]) : c;
                Goome.Push(Event.Char(cp, mods & Mods.Shift));
            }
        }

        void PasteIn()
        {
            string sys = GUIUtility.systemCopyBuffer ?? "";
            if (sys == clip) return;
            clip = sys;
            Goome.SetClipboard(sys);
        }

        void Sync()
        {
            if (syncClipboard && checkClip)
            {
                checkClip = false;
                string now = Goome.GetClipboard();
                if (now != null && now != clip)
                {
                    clip = now;
                    GUIUtility.systemCopyBuffer = now;
                }
            }
            int want = PointerInside || captured ? (int)Goome.Cursor : 0;
            if (want != shape) SetCursor(want);
        }

        void SetCursor(int s)
        {
            shape = s;
            UnityEngine.Cursor.visible = s != (int)CursorShape.Hidden;
            var pic = s > 0 && s < cursors.Length ? cursors[s] : null;
            UnityEngine.Cursor.SetCursor(pic, pic != null ? new Vector2(pic.width / 2f, pic.height / 2f) : Vector2.zero, CursorMode.Auto);
        }

#if ENABLE_INPUT_SYSTEM && GOOME_INPUT_SYSTEM
        static readonly List<InputAction> offMouse = new List<InputAction>(), offKeys = new List<InputAction>(), enabledNow = new List<InputAction>();

        static void Block()
        {
            bool mouse = false, keys = false;
            if (appFocused)
                foreach (var v in live)
                {
                    if (!v.blockGameInput) continue;
                    mouse |= v.WantsMouse;
                    keys |= v.WantsKeyboard;
                }
            if (!mouse) Restore(offMouse);
            if (!keys) Restore(offKeys);
            if (!mouse && !keys) return;
            enabledNow.Clear();
            InputSystem.ListEnabledActions(enabledNow);
            foreach (var a in enabledNow)
            {
                if (mouse && Uses<Pointer>(a)) TurnOff(a, offMouse);
                else if (keys && Uses<Keyboard>(a)) TurnOff(a, offKeys);
            }
            enabledNow.Clear();
        }

        static bool Uses<T>(InputAction a) where T : InputDevice
        {
            foreach (var c in a.controls)
                if (c.device is T) return true;
            return false;
        }

        static void TurnOff(InputAction a, List<InputAction> off)
        {
            a.Disable();
            if (!off.Contains(a)) off.Add(a);
        }

        static void Restore(List<InputAction> off)
        {
            if (off.Count == 0) return;
            var was = off.ToArray();
            off.Clear();
            foreach (var a in was)
                if (!a.enabled) a.Enable();
        }
#else
        static void Block() { }
#endif
    }
}
