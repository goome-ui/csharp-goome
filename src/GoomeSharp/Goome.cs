using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GoomeSharp
{
    /// <summary>An open goome: an overlay, a window or an offscreen surface. Dispose it when you are done</summary>
    public sealed unsafe class Goome : IDisposable
    {
        /// <summary>The goome version these bindings were built for</summary>
        public const string Version = "0.5.6";

        IntPtr g;
        readonly Dictionary<IntPtr, Canvas> canvases = new();
        readonly List<WeakReference<Ui>> uis = new();
        readonly RawMonitor[] monBuf = new RawMonitor[16];

        /// <summary>Opens goome with the given options. Throws GoomeException if it cant open</summary>
        /// <param name="options">null opens a plain overlay</param>
        public Goome(GoomeOptions? options = null)
        {
            options ??= new GoomeOptions();
            var keep = new List<IntPtr>();
            try
            {
                RawOpts o = new()
                {
                    Title = Utf8(options.Title, keep),
                    Backend = (int)options.Backend,
                    StateDir = Utf8(options.StateDir, keep),
                    NoCapture = options.NoCapture ? 1 : 0,
                    CaptureFps = options.CaptureFps,
                    HideSelf = options.HideSelf ? 1 : 0,
                    TestFrames = Utf8(options.TestFrames, keep),
                    TestScript = Utf8(options.TestScript, keep),
                    TestShots = Utf8(options.TestShots, keep),
                    Mode = (int)options.Mode,
                    Width = options.Width,
                    Height = options.Height,
                    MinWidth = options.MinWidth,
                    MinHeight = options.MinHeight,
                    WindowFlags = (int)options.WindowStyle,
                    Scale = options.Scale,
                };
                int err = 0;
                g = Native.Open(&o, &err);
                if (g == IntPtr.Zero)
                {
                    var code = err == 0 ? GoomeError.NotSupported : (GoomeError)err;
                    throw new GoomeException(code, (options.Mode == Mode.Window ? "cant open a window: " : options.Mode == Mode.Offscreen ? "cant open offscreen: " : "cant open an overlay: ") + StrError(code));
                }
            }
            finally
            {
                foreach (var p in keep) Marshal.FreeCoTaskMem(p);
            }
        }

        Goome(IntPtr handle) => g = handle;

        /// <summary>Opens a normal resizable window, centered</summary>
        /// <param name="title">null uses "goome"</param>
        /// <param name="width">Logical client width</param>
        /// <param name="height">Logical client height</param>
        public static Goome OpenApp(string? title = null, int width = 960, int height = 640)
        {
            int err = 0;
            IntPtr h;
            fixed (byte* t = U8.A(title)) h = Native.OpenApp(t, width, height, &err);
            if (h == IntPtr.Zero)
            {
                var code = err == 0 ? GoomeError.NotSupported : (GoomeError)err;
                throw new GoomeException(code, "cant open a window: " + StrError(code));
            }
            return new Goome(h);
        }

        /// <summary>Opens an offscreen surface with no window, you read the pixels with Frame</summary>
        /// <param name="scale">Real px per logical px</param>
        public static Goome OpenOffscreen(int width, int height, double scale = 1, string? title = null) =>
            new Goome(new GoomeOptions { Mode = Mode.Offscreen, Width = width, Height = height, Scale = scale, Title = title });

        static IntPtr Utf8(string? s, List<IntPtr> keep)
        {
            if (s == null) return IntPtr.Zero;
            var p = U8.CoTask(s);
            keep.Add(p);
            return p;
        }

        internal IntPtr Handle => g != IntPtr.Zero ? g : throw new ObjectDisposedException(nameof(Goome));

        /// <summary>True once Dispose has run</summary>
        public bool IsDisposed => g == IntPtr.Zero;

        /// <summary>Closes goome and everything made from it</summary>
        public void Dispose()
        {
            if (g == IntPtr.Zero) return;
            foreach (var w in uis)
                if (w.TryGetTarget(out var u)) u.Dispose();
            uis.Clear();
            foreach (var c in canvases.Values) c.Detach();
            canvases.Clear();
            Native.Close(g);
            g = IntPtr.Zero;
        }

        internal void AddUi(Ui u)
        {
            uis.RemoveAll(w => !w.TryGetTarget(out _));
            uis.Add(new WeakReference<Ui>(u));
        }

        /// <summary>Why the last call failed, in words</summary>
        public string LastError => Native.Str(Native.Error(Handle)) ?? "";
        /// <summary>Name of the backend goome picked</summary>
        public string BackendName => Native.Str(Native.BackendName(Handle)) ?? "";
        /// <summary>What works on this desktop, see Has</summary>
        public Caps Caps => (Caps)Native.Caps(Handle);
        /// <summary>Overlay, window or offscreen</summary>
        public Mode Mode => (Mode)Native.Mode(Handle);
        /// <summary>True if every cap in c works here</summary>
        public bool Has(Caps c) => (Caps & c) == c;

        /// <summary>Short text for an error code</summary>
        public static string StrError(GoomeError e) => Native.Str(Native.StrError((int)e)) ?? e.ToString();

        /// <summary>Every monitor, in logical desktop coords</summary>
        public Monitor[] Monitors
        {
            get
            {
                int n;
                fixed (RawMonitor* p = monBuf) n = Native.Monitors(Handle, p, monBuf.Length);
                if (n < 0) throw Fail(n);
                n = Math.Min(n, monBuf.Length);
                var r = new Monitor[n];
                for (int i = 0; i < n; i++) r[i] = new Monitor(monBuf[i]);
                return r;
            }
        }

        GoomeException Fail(int code) => new((GoomeError)code, Native.Str(Native.Error(g)));

        /// <summary>Grabs the screen, throws on failure. The pixels are good until the next grab of that monitor</summary>
        /// <param name="monitor">Monitor id</param>
        /// <param name="area">Logical rect to grab, null = the whole monitor</param>
        public Image Capture(int monitor = 0, Rect? area = null)
        {
            var e = TryCapture(monitor, area, out var img);
            if (e != GoomeError.Ok) throw Fail((int)e);
            return img;
        }

        /// <summary>Same as Capture but hands back the error code instead of throwing</summary>
        public GoomeError TryCapture(int monitor, Rect? area, out Image image)
        {
            RawImage raw = default;
            int e;
            if (area is Rect r) e = Native.Capture(Handle, monitor, &r, &raw);
            else e = Native.Capture(Handle, monitor, null, &raw);
            image = e == 0 ? new Image(raw) : default;
            return (GoomeError)e;
        }

        /// <summary>Alpha of our own overlay at x,y, use it to mask out the overlay in a grab</summary>
        public int AlphaAt(double x, double y) => Native.AlphaAt(Handle, x, y);

        /// <summary>Starts a frame on that monitor. Null means there is nothing to draw on, skip the frame</summary>
        /// <returns>A canvas that is already cleared</returns>
        public Canvas? Begin(int monitor = 0)
        {
            var p = Native.Begin(Handle, monitor);
            return p == null ? null : Wrap(p);
        }

        internal Canvas Wrap(RawCanvas* p)
        {
            if (!canvases.TryGetValue((IntPtr)p, out var c)) canvases[(IntPtr)p] = c = new Canvas(p);
            return c;
        }

        /// <summary>Sends the frame. Only the hit rects take clicks, everything else goes through to what is under the overlay</summary>
        public GoomeError End(Canvas canvas, ReadOnlySpan<Rect> hits)
        {
            fixed (Rect* h = hits) return (GoomeError)Native.End(Handle, canvas.Ptr, h, hits.Length);
        }

        /// <summary>Sends the frame. Only the hit rects take clicks, everything else goes through to what is under the overlay</summary>
        public GoomeError End(Canvas canvas, Rect hit) => End(canvas, new ReadOnlySpan<Rect>(&hit, 1));
        /// <summary>Sends the frame. Only the hit rects take clicks, everything else goes through to what is under the overlay</summary>
        public GoomeError End(Canvas canvas) => End(canvas, ReadOnlySpan<Rect>.Empty);

        /// <summary>Waits up to timeoutMs for the next event</summary>
        /// <param name="timeoutMs">0 returns right away</param>
        /// <returns>True if ev was filled</returns>
        public bool Poll(out Event ev, int timeoutMs = 0)
        {
            ev = default;
            int r;
            fixed (RawEvent* p = &ev.raw) r = Native.Poll(Handle, p, timeoutMs);
            if (r < 0) throw Fail(r);
            if (r > 0 && (ev.Type == EventType.Text || ev.Type == EventType.Preedit)) ev.full = EventText;
            return r > 0;
        }

        /// <summary>Full text of the last Text or Preedit event, good until the next poll</summary>
        public string EventText => Native.Str(Native.EventText(Handle)) ?? "";
        /// <summary>Caret position in EventText after the last Preedit event, in chars</summary>
        public int PreeditCursor => GoomeSharp.Utf8.CharIndex(EventText, Native.PreeditCursor(Handle));

        /// <summary>Turns the ime on or off</summary>
        /// <param name="caret">Logical rect where the candidate window goes</param>
        public bool TextInput(bool on, Rect? caret = null)
        {
            var h = Handle;
            if (caret is Rect r) return Native.TextInput(h, on ? 1 : 0, &r) == 0;
            return Native.TextInput(h, on ? 1 : 0, null) == 0;
        }

        /// <summary>Where the mouse is and which buttons are down. Only knows this everywhere with Caps.GlobalPointer</summary>
        /// <returns>True if the position is known</returns>
        public bool Pointer(out double x, out double y, out int buttons)
        {
            double a, b;
            int k;
            int r = Native.Pointer(Handle, &a, &b, &k);
            x = a;
            y = b;
            buttons = k;
            return r != 0;
        }

        /// <summary>Takes or gives back the keyboard. Needs Caps.Keyboard</summary>
        public bool Keyboard(bool on) => Native.Keyboard(Handle, on ? 1 : 0) == 0;
        /// <summary>Overlay mode: focus and pointer capture until Grab(false). Window mode: drags keep going outside the window</summary>
        public bool Grab(bool on) => Native.Grab(Handle, on ? 1 : 0) == 0;
        /// <summary>True while a Grab is on</summary>
        public bool Grabbed => Native.Grabbed(Handle) != 0;

        /// <summary>Registers a global hotkey, like "alt+h". It comes back as a Hotkey event with this id</summary>
        /// <param name="combo">Keys joined with +, like "ctrl+shift+k"</param>
        /// <param name="id">Number you get back in Event.Id</param>
        public bool Hotkey(string combo, int id)
        {
            var h = Handle;
            fixed (byte* s = U8.A(combo)) return Native.Hotkey(h, s, id) == 0;
        }

        /// <summary>Parses a combo string into a key and its mods</summary>
        /// <returns>False if it didnt parse</returns>
        public static bool ParseCombo(string combo, out int key, out Mods mods)
        {
            int k = 0, m = 0;
            bool ok;
            fixed (byte* s = U8.A(combo)) ok = Native.ParseCombo(s, &k, &m) == 0;
            key = k;
            mods = (Mods)m;
            return ok;
        }

        /// <summary>Path of the control fifo, null if there isnt one</summary>
        public string? ControlPath => Native.Str(Native.ControlPath(Handle));

        /// <summary>Sends a line to the control fifo of another goome app</summary>
        /// <param name="stateDir">The state dir that app was opened with</param>
        public static GoomeError Send(string stateDir, string message)
        {
            fixed (byte* d = U8.A(stateDir), m = U8.B(message)) return (GoomeError)Native.Send(d, m);
        }

        /// <summary>Milliseconds on a steady clock, good for timing frames</summary>
        public static long NowMs => Native.NowMs();

        /// <summary>Writes pixels to a ppm file</summary>
        /// <param name="stride">Row length in pixels, not bytes</param>
        public static GoomeError SavePpm(string path, ReadOnlySpan<uint> px, int w, int h, int stride)
        {
            if (w < 0 || h < 0 || stride < w || (h > 0 && px.Length < (long)stride * (h - 1) + w)) throw new ArgumentException("pixels too short for that size");
            fixed (byte* s = U8.A(path))
            fixed (uint* p = px) return (GoomeError)Native.SavePpm(s, p, w, h, stride);
        }

        /// <summary>Writes pixels to a ppm file</summary>
        public static GoomeError SavePpm(string path, in Image img) => SavePpm(path, img.Pixels, img.Width, img.Height, img.Stride);

        /// <summary>Changes the window title</summary>
        public bool SetTitle(string title)
        {
            var h = Handle;
            fixed (byte* s = U8.A(title)) return Native.WindowTitle(h, s) == 0;
        }

        /// <summary>Changes the client size, in logical px</summary>
        public bool SetSize(int width, int height) => Native.WindowSize(Handle, width, height) == 0;

        /// <summary>Reads the window position. Fails where the os wont say, like wayland</summary>
        public bool GetPosition(out int x, out int y)
        {
            int a = 0, b = 0;
            bool ok = Native.WindowPos(Handle, &a, &b, 0) == 0;
            x = a;
            y = b;
            return ok;
        }

        /// <summary>Moves the window</summary>
        public bool SetPosition(int x, int y) => Native.WindowPos(Handle, &x, &y, 1) == 0;

        /// <summary>Normal, minimized, maximized or fullscreen</summary>
        public WindowState State
        {
            get
            {
                int s = Native.WindowGetState(Handle);
                return s < 0 ? WindowState.Normal : (WindowState)s;
            }
        }

        /// <summary>Minimizes, maximizes, restores or fullscreens the window</summary>
        public bool SetState(WindowState state) => Native.WindowState(Handle, (int)state) == 0;
        /// <summary>Minimizes the window</summary>
        public bool Minimize() => SetState(WindowState.Minimized);
        /// <summary>Maximizes the window</summary>
        public bool Maximize() => SetState(WindowState.Maximized);
        /// <summary>Brings the window back to normal size</summary>
        public bool Restore() => SetState(WindowState.Normal);
        /// <summary>Starts a native move, or a resize from the edges, from the mouse press happening right now</summary>
        /// <param name="edges">None moves it, edge bits resize it</param>
        public bool Drag(Edges edges = Edges.None) => Native.WindowDrag(Handle, (int)edges) == 0;
        /// <summary>Shows or hides the window</summary>
        public bool Show(bool visible = true) => Native.WindowShow(Handle, visible ? 1 : 0) == 0;
        /// <summary>Hides the window</summary>
        public bool Hide() => Show(false);

        /// <summary>The mouse cursor shape. Needs Caps.Cursor to change the real one</summary>
        public CursorShape Cursor
        {
            get => (CursorShape)Native.CursorShape(Handle);
            set => Native.Cursor(Handle, (int)value);
        }

        /// <summary>The clipboard text. Reading gives an empty string if it cant</summary>
        public string Clipboard
        {
            get => GetClipboard() ?? "";
            set => SetClipboard(value);
        }

        /// <summary>Reads the clipboard, null on error. Can block for about 250ms on x11 and wayland</summary>
        public string? GetClipboard()
        {
            var h = Handle;
            int n = Native.ClipboardGet(h, null, 0);
            if (n < 0) return null;
            if (n == 0) return "";
            var buf = new byte[n + 1];
            fixed (byte* p = buf)
            {
                int m = Native.ClipboardGet(h, p, buf.Length);
                if (m < 0) return null;
                return Native.Str(p) ?? "";
            }
        }

        /// <summary>Puts text on the clipboard</summary>
        public bool SetClipboard(string text)
        {
            var h = Handle;
            fixed (byte* s = U8.A(text)) return Native.ClipboardSet(h, s) == 0;
        }

        /// <summary>Full path of the last file dropped on the window, good until the next poll</summary>
        public string? DropPath => Native.Str(Native.DropPath(Handle));

        /// <summary>Queues an event for Poll, in any mode</summary>
        public bool Push(in Event ev)
        {
            RawEvent r = ev.raw;
            return Native.Push(Handle, &r) == 0;
        }

        /// <summary>Queues finished ime text as Text events, split to fit</summary>
        /// <returns>Chars queued</returns>
        public int PushCommit(string text)
        {
            int n = 0;
            while (text.Length > 0)
            {
                var e = Event.Commit(text);
                string sent = e.Message;
                if (sent.Length == 0 || !Push(e)) break;
                n += sent.Length;
                text = text.Substring(sent.Length);
            }
            return n;
        }

        /// <summary>Queues the text being composed</summary>
        /// <param name="caret">Caret in chars, -1 = the end</param>
        public bool PushPreedit(string text, int caret = -1) => Push(Event.Preedit(text, caret));

        /// <summary>Types text by queueing a key event per character</summary>
        /// <returns>Chars queued</returns>
        public int PushText(string text, Mods mods = Mods.None)
        {
            int n = 0;
            for (int i = 0; i < text.Length; i++)
            {
                int cp = char.IsSurrogatePair(text, i) ? char.ConvertToUtf32(text[i], text[++i]) : text[i];
                if (!Push(Event.Char(cp, mods))) break;
                n++;
            }
            return n;
        }

        /// <summary>Resizes an offscreen surface. Offscreen only</summary>
        /// <param name="scale">Real px per logical px, 0 keeps the current one</param>
        public bool Resize(int width, int height, double scale = 0) => Native.Resize(Handle, width, height, scale) == 0;

        /// <summary>Gets the last frame End sent. Good until the next Begin</summary>
        /// <returns>False if there is none</returns>
        public bool TryFrame(out Image frame)
        {
            RawImage raw = default;
            int e = Native.Frame(Handle, &raw);
            frame = e == 0 ? new Image(raw) : default;
            return e == 0;
        }

        /// <summary>Same as TryFrame but throws. Pixels are premultiplied argb</summary>
        public Image Frame()
        {
            RawImage raw = default;
            int e = Native.Frame(Handle, &raw);
            if (e != 0) throw Fail(e);
            return new Image(raw);
        }

        /// <summary>True if x,y is inside the hit rects of the last End</summary>
        public bool Hit(double x, double y) => Native.Hit(Handle, x, y) != 0;

        /// <summary>Gets the real px rect that changed since the last call. The first one after open or resize is the whole frame</summary>
        /// <returns>False when nothing changed</returns>
        public bool FrameDirty(out Rect area)
        {
            Rect r = default;
            bool any = Native.FrameDirty(Handle, &r) != 0;
            area = any ? r : default;
            return any;
        }

        /// <summary>Copies part of the last frame into your own buffer</summary>
        /// <param name="dst">Starts at the top left of area</param>
        /// <param name="stride">Bytes per row of dst</param>
        /// <param name="format">Byte order of the copy</param>
        /// <param name="area">Real px rect, null = the whole frame</param>
        public void FrameCopy(Span<byte> dst, int stride, Format format, Rect? area = null)
        {
            var h = Handle;
            Rect r;
            if (area is Rect a) r = a;
            else
            {
                var f = Frame();
                r = new Rect(0, 0, f.Width, f.Height);
            }
            if (r.W <= 0 || r.H <= 0) throw new ArgumentException("empty rect", nameof(area));
            if (stride < r.W * 4) throw new ArgumentException($"stride {stride} is too small for {r.W} px", nameof(stride));
            if (dst.Length < (long)(r.H - 1) * stride + r.W * 4) throw new ArgumentException($"dst needs {(long)(r.H - 1) * stride + r.W * 4} bytes, got {dst.Length}", nameof(dst));
            int e;
            fixed (byte* p = dst) e = Native.FrameCopy(h, p, stride, (int)format, &r);
            if (e != 0) throw Fail(e);
        }
    }

    /// <summary>A screen grab or frame. The pixels belong to goome and stay valid until the next grab of that monitor</summary>
    public readonly unsafe struct Image
    {
        readonly RawImage raw;

        internal Image(in RawImage r) => raw = r;

        /// <summary>The raw c struct</summary>
        public RawImage Raw => raw;
        /// <summary>Width in real px</summary>
        public int Width => raw.W;
        /// <summary>Height in real px</summary>
        public int Height => raw.H;
        /// <summary>Row length in pixels</summary>
        public int Stride => raw.Stride;
        /// <summary>Logical desktop rect this image covers</summary>
        public Rect Area => raw.Area;
        /// <summary>Monitor id it came from</summary>
        public int Monitor => raw.Mon;
        /// <summary>True if our own overlay is in it, mask it with Goome.AlphaAt</summary>
        public bool HasSelf => raw.HasSelf != 0;
        /// <summary>True if the mouse got drawn into it</summary>
        public bool HasCursor => raw.HasCursor != 0;
        /// <summary>Logical rect of the baked in mouse</summary>
        public Rect Cursor => raw.Cursor;
        /// <summary>True if there are no pixels</summary>
        public bool IsEmpty => raw.Px == null;
        /// <summary>Address of the first pixel</summary>
        public IntPtr Pointer => (IntPtr)raw.Px;

        /// <summary>The pixels as 0xFFRRGGBB</summary>
        public ReadOnlySpan<uint> Pixels => raw.Px == null ? default : new ReadOnlySpan<uint>(raw.Px, raw.H <= 0 ? 0 : (raw.H - 1) * raw.Stride + raw.W);

        /// <summary>Pixel at x,y. Throws if it is outside the image</summary>
        public uint this[int x, int y] => (uint)x < (uint)raw.W && (uint)y < (uint)raw.H ? raw.Px[(long)y * raw.Stride + x] : throw new IndexOutOfRangeException();

        /// <summary>Copies the pixels into your own array, tightly packed</summary>
        public uint[] ToArray()
        {
            var a = new uint[raw.W * raw.H];
            for (int y = 0; y < raw.H; y++) new ReadOnlySpan<uint>(raw.Px + (long)y * raw.Stride, raw.W).CopyTo(a.AsSpan(y * raw.W, raw.W));
            return a;
        }
    }
}
