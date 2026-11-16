using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GoomeSharp
{
    public sealed unsafe class Goome : IDisposable
    {
        public const string Version = "0.5.1";

        IntPtr g;
        readonly Dictionary<IntPtr, Canvas> canvases = new();
        readonly List<WeakReference<Ui>> uis = new();
        readonly RawMonitor[] monBuf = new RawMonitor[16];

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

        public bool IsDisposed => g == IntPtr.Zero;

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

        public string LastError => Native.Str(Native.Error(Handle)) ?? "";
        public string BackendName => Native.Str(Native.BackendName(Handle)) ?? "";
        public Caps Caps => (Caps)Native.Caps(Handle);
        public Mode Mode => (Mode)Native.Mode(Handle);
        public bool Has(Caps c) => (Caps & c) == c;

        public static string StrError(GoomeError e) => Native.Str(Native.StrError((int)e)) ?? e.ToString();

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

        public Image Capture(int monitor = 0, Rect? area = null)
        {
            var e = TryCapture(monitor, area, out var img);
            if (e != GoomeError.Ok) throw Fail((int)e);
            return img;
        }

        public GoomeError TryCapture(int monitor, Rect? area, out Image image)
        {
            RawImage raw = default;
            int e;
            if (area is Rect r) e = Native.Capture(Handle, monitor, &r, &raw);
            else e = Native.Capture(Handle, monitor, null, &raw);
            image = e == 0 ? new Image(raw) : default;
            return (GoomeError)e;
        }

        public int AlphaAt(double x, double y) => Native.AlphaAt(Handle, x, y);

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

        public GoomeError End(Canvas canvas, ReadOnlySpan<Rect> hits)
        {
            fixed (Rect* h = hits) return (GoomeError)Native.End(Handle, canvas.Ptr, h, hits.Length);
        }

        public GoomeError End(Canvas canvas, Rect hit) => End(canvas, new ReadOnlySpan<Rect>(&hit, 1));
        public GoomeError End(Canvas canvas) => End(canvas, ReadOnlySpan<Rect>.Empty);

        public bool Poll(out Event ev, int timeoutMs = 0)
        {
            ev = default;
            int r;
            fixed (RawEvent* p = &ev.raw) r = Native.Poll(Handle, p, timeoutMs);
            if (r < 0) throw Fail(r);
            if (r > 0 && (ev.Type == EventType.Text || ev.Type == EventType.Preedit)) ev.full = EventText;
            return r > 0;
        }

        public string EventText => Native.Str(Native.EventText(Handle)) ?? "";
        public int PreeditCursor => GoomeSharp.Utf8.CharIndex(EventText, Native.PreeditCursor(Handle));

        public bool TextInput(bool on, Rect? caret = null)
        {
            var h = Handle;
            if (caret is Rect r) return Native.TextInput(h, on ? 1 : 0, &r) == 0;
            return Native.TextInput(h, on ? 1 : 0, null) == 0;
        }

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

        public bool Keyboard(bool on) => Native.Keyboard(Handle, on ? 1 : 0) == 0;
        public bool Grab(bool on) => Native.Grab(Handle, on ? 1 : 0) == 0;
        public bool Grabbed => Native.Grabbed(Handle) != 0;

        public bool Hotkey(string combo, int id)
        {
            var h = Handle;
            fixed (byte* s = U8.A(combo)) return Native.Hotkey(h, s, id) == 0;
        }

        public static bool ParseCombo(string combo, out int key, out Mods mods)
        {
            int k = 0, m = 0;
            bool ok;
            fixed (byte* s = U8.A(combo)) ok = Native.ParseCombo(s, &k, &m) == 0;
            key = k;
            mods = (Mods)m;
            return ok;
        }

        public string? ControlPath => Native.Str(Native.ControlPath(Handle));

        public static GoomeError Send(string stateDir, string message)
        {
            fixed (byte* d = U8.A(stateDir), m = U8.B(message)) return (GoomeError)Native.Send(d, m);
        }

        public static long NowMs => Native.NowMs();

        public static GoomeError SavePpm(string path, ReadOnlySpan<uint> px, int w, int h, int stride)
        {
            if (w < 0 || h < 0 || stride < w || (h > 0 && px.Length < (long)stride * (h - 1) + w)) throw new ArgumentException("pixels too short for that size");
            fixed (byte* s = U8.A(path))
            fixed (uint* p = px) return (GoomeError)Native.SavePpm(s, p, w, h, stride);
        }

        public static GoomeError SavePpm(string path, in Image img) => SavePpm(path, img.Pixels, img.Width, img.Height, img.Stride);

        public bool SetTitle(string title)
        {
            var h = Handle;
            fixed (byte* s = U8.A(title)) return Native.WindowTitle(h, s) == 0;
        }

        public bool SetSize(int width, int height) => Native.WindowSize(Handle, width, height) == 0;

        public bool GetPosition(out int x, out int y)
        {
            int a = 0, b = 0;
            bool ok = Native.WindowPos(Handle, &a, &b, 0) == 0;
            x = a;
            y = b;
            return ok;
        }

        public bool SetPosition(int x, int y) => Native.WindowPos(Handle, &x, &y, 1) == 0;

        public WindowState State
        {
            get
            {
                int s = Native.WindowGetState(Handle);
                return s < 0 ? WindowState.Normal : (WindowState)s;
            }
        }

        public bool SetState(WindowState state) => Native.WindowState(Handle, (int)state) == 0;
        public bool Minimize() => SetState(WindowState.Minimized);
        public bool Maximize() => SetState(WindowState.Maximized);
        public bool Restore() => SetState(WindowState.Normal);
        public bool Drag(Edges edges = Edges.None) => Native.WindowDrag(Handle, (int)edges) == 0;
        public bool Show(bool visible = true) => Native.WindowShow(Handle, visible ? 1 : 0) == 0;
        public bool Hide() => Show(false);

        public CursorShape Cursor
        {
            get => (CursorShape)Native.CursorShape(Handle);
            set => Native.Cursor(Handle, (int)value);
        }

        public string Clipboard
        {
            get => GetClipboard() ?? "";
            set => SetClipboard(value);
        }

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

        public bool SetClipboard(string text)
        {
            var h = Handle;
            fixed (byte* s = U8.A(text)) return Native.ClipboardSet(h, s) == 0;
        }

        public string? DropPath => Native.Str(Native.DropPath(Handle));

        public bool Push(in Event ev)
        {
            RawEvent r = ev.raw;
            return Native.Push(Handle, &r) == 0;
        }

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

        public bool PushPreedit(string text, int caret = -1) => Push(Event.Preedit(text, caret));

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

        public bool Resize(int width, int height, double scale = 0) => Native.Resize(Handle, width, height, scale) == 0;

        public bool TryFrame(out Image frame)
        {
            RawImage raw = default;
            int e = Native.Frame(Handle, &raw);
            frame = e == 0 ? new Image(raw) : default;
            return e == 0;
        }

        public Image Frame()
        {
            RawImage raw = default;
            int e = Native.Frame(Handle, &raw);
            if (e != 0) throw Fail(e);
            return new Image(raw);
        }

        public bool Hit(double x, double y) => Native.Hit(Handle, x, y) != 0;

        public bool FrameDirty(out Rect area)
        {
            Rect r = default;
            bool any = Native.FrameDirty(Handle, &r) != 0;
            area = any ? r : default;
            return any;
        }

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

    public readonly unsafe struct Image
    {
        readonly RawImage raw;

        internal Image(in RawImage r) => raw = r;

        public RawImage Raw => raw;
        public int Width => raw.W;
        public int Height => raw.H;
        public int Stride => raw.Stride;
        public Rect Area => raw.Area;
        public int Monitor => raw.Mon;
        public bool HasSelf => raw.HasSelf != 0;
        public bool HasCursor => raw.HasCursor != 0;
        public Rect Cursor => raw.Cursor;
        public bool IsEmpty => raw.Px == null;
        public IntPtr Pointer => (IntPtr)raw.Px;

        public ReadOnlySpan<uint> Pixels => raw.Px == null ? default : new ReadOnlySpan<uint>(raw.Px, raw.H <= 0 ? 0 : (raw.H - 1) * raw.Stride + raw.W);

        public uint this[int x, int y] => (uint)x < (uint)raw.W && (uint)y < (uint)raw.H ? raw.Px[(long)y * raw.Stride + x] : throw new IndexOutOfRangeException();

        public uint[] ToArray()
        {
            var a = new uint[raw.W * raw.H];
            for (int y = 0; y < raw.H; y++) new ReadOnlySpan<uint>(raw.Px + (long)y * raw.Stride, raw.W).CopyTo(a.AsSpan(y * raw.W, raw.W));
            return a;
        }
    }
}
