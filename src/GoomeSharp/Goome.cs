using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GoomeSharp;

public sealed unsafe class Goome : IDisposable
{
    public const string Version = "0.1.0";

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
            };
            int err = 0;
            g = Native.Open(&o, &err);
            if (g == IntPtr.Zero)
            {
                var code = err == 0 ? GoomeError.NotSupported : (GoomeError)err;
                throw new GoomeException(code, "cant open an overlay: " + StrError(code));
            }
        }
        finally
        {
            foreach (var p in keep) Marshal.FreeCoTaskMem(p);
        }
    }

    static IntPtr Utf8(string? s, List<IntPtr> keep)
    {
        if (s == null) return IntPtr.Zero;
        var p = Marshal.StringToCoTaskMemUTF8(s);
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
        return r > 0;
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

    public bool Hotkey(string combo, int id) => Native.Hotkey(Handle, combo, id) == 0;

    public static bool ParseCombo(string combo, out int key, out Mods mods)
    {
        int k = 0, m = 0;
        bool ok = Native.ParseCombo(combo, &k, &m) == 0;
        key = k;
        mods = (Mods)m;
        return ok;
    }

    public string? ControlPath => Native.Str(Native.ControlPath(Handle));

    public static GoomeError Send(string stateDir, string message) => (GoomeError)Native.Send(stateDir, message);

    public static long NowMs => (long)Native.NowMs().Value;

    public static GoomeError SavePpm(string path, ReadOnlySpan<uint> px, int w, int h, int stride)
    {
        if (w < 0 || h < 0 || stride < w || (h > 0 && px.Length < (long)stride * (h - 1) + w)) throw new ArgumentException("pixels too short for that size");
        fixed (uint* p = px) return (GoomeError)Native.SavePpm(path, p, w, h, stride);
    }

    public static GoomeError SavePpm(string path, in Image img) => SavePpm(path, img.Pixels, img.Width, img.Height, img.Stride);
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

    public ReadOnlySpan<uint> Pixels => raw.Px == null ? default : new ReadOnlySpan<uint>(raw.Px, raw.H <= 0 ? 0 : (raw.H - 1) * raw.Stride + raw.W);

    public uint this[int x, int y] => (uint)x < (uint)raw.W && (uint)y < (uint)raw.H ? raw.Px[(long)y * raw.Stride + x] : throw new IndexOutOfRangeException();

    public uint[] ToArray()
    {
        var a = new uint[raw.W * raw.H];
        for (int y = 0; y < raw.H; y++) new ReadOnlySpan<uint>(raw.Px + (long)y * raw.Stride, raw.W).CopyTo(a.AsSpan(y * raw.W, raw.W));
        return a;
    }
}
