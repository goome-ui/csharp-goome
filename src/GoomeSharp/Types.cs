using System;
using System.Runtime.InteropServices;

namespace GoomeSharp
{
    public enum Backend
    {
        Auto,
        Wayland,
        Gnome,
        Portal,
        X11,
        Win32,
        MacOS,
        Test,
        Offscreen,
    }

    public enum Mode
    {
        Overlay,
        Window,
        Offscreen,
    }

    [Flags]
    public enum WindowStyle
    {
        None = 0,
        Resizable = 1,
        Borderless = 2,
        Transparent = 4,
        OnTop = 8,
        Hidden = 16,
        Center = 32,
    }

    public enum CursorShape
    {
        Arrow,
        IBeam,
        Hand,
        Move,
        ResizeEW,
        ResizeNS,
        ResizeNWSE,
        ResizeNESW,
        Wait,
        No,
        Hidden,
    }

    [Flags]
    public enum Edges
    {
        None = 0,
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8,
    }

    public enum WindowState
    {
        Normal,
        Minimized,
        Maximized,
        Fullscreen,
    }

    [Flags]
    public enum AppFlags
    {
        None = 0,
        TitleBar = 1,
        MenuBar = 2,
        StatusBar = 4,
        NoPad = 8,
    }

    [Flags]
    public enum EditFlags
    {
        None = 0,
        ReadOnly = 1,
        LineNumbers = 2,
        Wrap = 4,
        Mono = 8,
    }

    [Flags]
    public enum TableFlags
    {
        None = 0,
        Sortable = 1,
        Resizable = 2,
        Stripes = 4,
        Borders = 8,
    }

    public enum GoomeError
    {
        Ok = 0,
        NotSupported = -1,
        Denied = -2,
        NoDisplay = -3,
        Gone = -4,
        Again = -5,
        NoMemory = -6,
        BadArgument = -7,
    }

    [Flags]
    public enum Caps
    {
        None = 0,
        Capture = 1,
        GlobalPointer = 2,
        Hotkeys = 4,
        HideSelf = 8,
        Keyboard = 16,
        Window = 32,
        Clipboard = 64,
        Cursor = 128,
    }

    public enum EventType
    {
        None,
        Down,
        Up,
        Motion,
        Scroll,
        Key,
        Hotkey,
        Control,
        Monitors,
        Quit,
        Close,
        Focus,
        Drop,
    }

    [Flags]
    public enum Mods
    {
        None = 0,
        Shift = 0x100,
        Ctrl = 0x200,
        Alt = 0x400,
        Super = 0x800,
    }

    public static class Keys
    {
        public const int Backspace = 8;
        public const int Tab = 9;
        public const int Enter = 13;
        public const int Esc = 27;
        public const int Space = 32;
        public const int Delete = 127;
        public const int F1 = 0x81;
        public const int F12 = 0x8C;
        public const int Left = 0x90;
        public const int Right = 0x91;
        public const int Up = 0x92;
        public const int Down = 0x93;
        public const int Home = 0x94;
        public const int End = 0x95;
        public const int PageUp = 0x96;
        public const int PageDown = 0x97;
        public const int Insert = 0x98;

        public static int F(int n) => n >= 1 && n <= 12 ? F1 + n - 1 : throw new ArgumentOutOfRangeException(nameof(n));
    }

    [Flags]
    public enum WindowFlags
    {
        None = 0,
        Move = 1,
        Resize = 2,
        Collapse = 4,
        NoTitle = 8,
        Close = 16,
    }

    public class GoomeException : Exception
    {
        public GoomeError Code { get; }

        public GoomeException(GoomeError code, string? message) : base(string.IsNullOrEmpty(message) ? Goome.StrError(code) : message) => Code = code;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect : IEquatable<Rect>
    {
        public int X, Y, W, H;

        public Rect(int x, int y, int w, int h) => (X, Y, W, H) = (x, y, w, h);

        public bool Contains(double x, double y) => x >= X && y >= Y && x < X + W && y < Y + H;
        public bool Equals(Rect o) => X == o.X && Y == o.Y && W == o.W && H == o.H;
        public override bool Equals(object? o) => o is Rect r && Equals(r);
        public override int GetHashCode() => HashCode.Combine(X, Y, W, H);
        public static bool operator ==(Rect a, Rect b) => a.Equals(b);
        public static bool operator !=(Rect a, Rect b) => !a.Equals(b);
        public override string ToString() => $"{X},{Y} {W}x{H}";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Radii
    {
        public double TopLeft, TopRight, BottomRight, BottomLeft;

        public Radii(double tl, double tr, double br, double bl) => (TopLeft, TopRight, BottomRight, BottomLeft) = (tl, tr, br, bl);

        public static Radii All(double r) => Native.Radius(r);
        public static implicit operator Radii(double r) => new(r, r, r, r);
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Pacer
    {
#if NET6_0_OR_GREATER
        CLong next;
        int fps;

        public int Fps => fps;
#else
        long a, b;

        public int Fps
        {
            get
            {
                fixed (Pacer* p = &this) return ((int*)p)[Os.Windows ? 1 : 2];
            }
        }
#endif

        public unsafe int Pace(int fps)
        {
            fixed (Pacer* p = &this) return Native.Pace(p, fps);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawOpts
    {
        public IntPtr Title;
        public int Backend;
        public IntPtr StateDir;
        public int NoCapture;
        public int CaptureFps;
        public int HideSelf;
        public IntPtr TestFrames, TestScript, TestShots;
        public int Mode;
        public int Width, Height;
        public int MinWidth, MinHeight;
        public int WindowFlags;
        public double Scale;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawMonitor
    {
        public int Id;
        public fixed byte Name[64];
        public Rect Area;
        public double Scale;
        public int Pw, Ph;
        public int Rotation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawImage
    {
        public uint* Px;
        public int W, H, Stride;
        public Rect Area;
        public int Mon;
        public int HasSelf;
        public int HasCursor;
        public Rect Cursor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawCanvas
    {
        public uint* Px;
        public int W, H, Stride;
        public double Ox, Oy, Scale;
        public int Mon;
        public int Dx0, Dy0, Dx1, Dy1;
        public int Cx0, Cy0, Cx1, Cy1;
        public int Kx0, Ky0, Kx1, Ky1;
        public int Nclip;
        public fixed int Clips[16 * 4];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawEvent
    {
        public int Type;
        public double X, Y;
        public int Mon;
        public int Button;
        public int Dy;
        public int Key;
        public int Mods;
        public fixed byte Text[8];
        public int Id;
        public fixed byte Msg[64];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawBitmap
    {
        public uint* Px;
        public int W, H, Stride;
        public int Owned;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawTheme
    {
        public uint Bg, Panel, Border, Text, TextDim, Accent, AccentText, Hover, Active, InputBg, Shadow;
        public double Radius, Pad, Gap, BorderWidth, FontSize, ShadowBlur, AnimMs;
        public IntPtr Font;
        public RawSkin SkinButton, SkinPanel, SkinInput;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawSkin
    {
        public RawBitmap* Bm;
        public double L, T, R, B;
    }

    public sealed class Skin
    {
        public Skin(Bitmap bitmap, double left, double top, double right, double bottom)
        {
            Bitmap = bitmap;
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public Skin(Bitmap bitmap, double border) : this(bitmap, border, border, border, border)
        {
        }

        public Bitmap Bitmap { get; }
        public double Left { get; }
        public double Top { get; }
        public double Right { get; }
        public double Bottom { get; }
    }

    public readonly struct Monitor
    {
        public int Id { get; }
        public string Name { get; }
        public Rect Area { get; }
        public double Scale { get; }
        public int PixelWidth { get; }
        public int PixelHeight { get; }
        public int Rotation { get; }

        internal unsafe Monitor(RawMonitor m)
        {
            Id = m.Id;
            Name = Native.Str(m.Name) ?? "";
            Area = m.Area;
            Scale = m.Scale;
            PixelWidth = m.Pw;
            PixelHeight = m.Ph;
            Rotation = m.Rotation;
        }

        public override string ToString() => $"{Name} {Area.W}x{Area.H} at {Area.X},{Area.Y} scale {Scale:0.00}";
    }

    public struct Event
    {
        internal RawEvent raw;

        public Event(EventType type)
        {
            raw = default;
            raw.Type = (int)type;
        }

        public Event(EventType type, double x, double y)
        {
            raw = default;
            raw.Type = (int)type;
            raw.X = x;
            raw.Y = y;
        }

        public Event(in RawEvent r) => raw = r;

        public RawEvent Raw => raw;
        public EventType Type => (EventType)raw.Type;
        public double X => raw.X;
        public double Y => raw.Y;
        public int Monitor => raw.Mon;
        public int Button => raw.Button;
        public int Dy => raw.Dy;
        public int Key => raw.Key;
        public Mods Mods => (Mods)raw.Mods;
        public int Id => raw.Id;
        public bool Focused => raw.Id != 0;

        public unsafe string Text
        {
            get
            {
                fixed (byte* p = raw.Text) return p[0] == 0 ? "" : Native.Str(p) ?? "";
            }
        }

        public unsafe string Message
        {
            get
            {
                fixed (byte* p = raw.Msg) return p[0] == 0 ? "" : Native.Str(p) ?? "";
            }
        }

        public static Event Down(double x, double y, int button = 1, Mods mods = Mods.None) => Mouse(EventType.Down, x, y, button, mods);
        public static Event Up(double x, double y, int button = 1, Mods mods = Mods.None) => Mouse(EventType.Up, x, y, button, mods);
        public static Event Motion(double x, double y) => new Event(EventType.Motion, x, y);

        public static Event Scroll(double x, double y, int dy)
        {
            var e = new Event(EventType.Scroll, x, y);
            e.raw.Dy = dy;
            return e;
        }

        public static Event KeyPress(int key, Mods mods = Mods.None, string? text = null)
        {
            var e = new Event(EventType.Key);
            e.raw.Key = key;
            e.raw.Mods = (int)mods;
            if (text != null) e.SetText(text);
            return e;
        }

        public static Event Char(int codepoint, Mods mods = Mods.None)
        {
            int key = codepoint < 128 ? codepoint : 0;
            if (key >= 'A' && key <= 'Z')
            {
                key += 32;
                mods |= Mods.Shift;
            }
            var e = KeyPress(key, mods);
            if (codepoint >= 32 && codepoint != 127 && (mods & (Mods.Ctrl | Mods.Alt | Mods.Super)) == 0) e.SetText(char.ConvertFromUtf32(codepoint));
            return e;
        }

        public static Event Focus(bool gained)
        {
            var e = new Event(EventType.Focus);
            e.raw.Id = gained ? 1 : 0;
            return e;
        }

        public static Event Close() => new Event(EventType.Close);

        public static Event Control(string message)
        {
            var e = new Event(EventType.Control);
            e.SetMessage(message);
            return e;
        }

        static Event Mouse(EventType t, double x, double y, int button, Mods mods)
        {
            var e = new Event(t, x, y);
            e.raw.Button = button;
            e.raw.Mods = (int)mods;
            return e;
        }

        unsafe void SetText(string s)
        {
            fixed (byte* p = raw.Text) Fit(s, new Span<byte>(p, 8));
        }

        unsafe void SetMessage(string s)
        {
            fixed (byte* p = raw.Msg) Fit(s, new Span<byte>(p, 64));
        }

        static void Fit(string s, Span<byte> dst)
        {
            dst.Clear();
            int n = Utf8.Fit(s, dst.Slice(0, dst.Length - 1));
            dst[n] = 0;
        }

        public override string ToString() => Type switch
        {
            EventType.Down or EventType.Up or EventType.Motion => $"{Type} {X:0},{Y:0} button {Button}",
            EventType.Scroll => $"Scroll {X:0},{Y:0} {Dy}",
            EventType.Key => $"Key {Key} mods {Mods} '{Text}'",
            EventType.Hotkey => $"Hotkey {Id}",
            EventType.Control => $"Control '{Message}'",
            EventType.Focus => Focused ? "Focus gained" : "Focus lost",
            EventType.Drop => $"Drop {X:0},{Y:0} '{Message}'",
            _ => Type.ToString(),
        };
    }

    internal static class Utf8
    {
        internal static int Fit(string s, Span<byte> dst)
        {
            var all = U8.C(s)!;
            int n = System.Text.Encoding.UTF8.GetByteCount(s);
            if (n > dst.Length)
            {
                n = dst.Length;
                while (n > 0 && (all[n] & 0xC0) == 0x80) n--;
            }
            all.AsSpan(0, n).CopyTo(dst);
            return n;
        }
    }

    public sealed class GoomeOptions
    {
        public string? Title { get; set; }
        public Backend Backend { get; set; }
        public string? StateDir { get; set; }
        public bool NoCapture { get; set; }
        public int CaptureFps { get; set; }
        public bool HideSelf { get; set; }
        public string? TestFrames { get; set; }
        public string? TestScript { get; set; }
        public string? TestShots { get; set; }
        public Mode Mode { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MinWidth { get; set; }
        public int MinHeight { get; set; }
        public WindowStyle WindowStyle { get; set; }
        public double Scale { get; set; }
    }
}
