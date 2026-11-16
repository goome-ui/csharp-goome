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
        Grab = 256,
    }

    [Flags]
    public enum Want
    {
        None = 0,
        Mouse = 1,
        Keyboard = 2,
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
        Text,
        Preedit,
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

    public enum Style
    {
        Bg,
        Panel,
        Border,
        Text,
        TextDim,
        Accent,
        AccentText,
        Hover,
        Active,
        InputBg,
        Shadow,
        Radius,
        Pad,
        Gap,
        BorderWidth,
        FontSize,
        ShadowBlur,
        AnimMs,
        Focus,
        Select,
        Caret,
        Link,
        Badge,
        Toast,
        Tooltip,
        Danger,
        Knob,
        Dim,
        CheckSize,
        CheckRadius,
        ToggleW,
        ToggleH,
        SliderH,
        SliderTrack,
        KnobSize,
        ScrollW,
        ScrollMin,
        ItemH,
        MenuItemH,
        TabH,
        WinTitleH,
        AppTitleH,
        MenubarH,
        StatusbarH,
        FocusW,
        FocusGap,
        SepW,
        Grip,
        Indent,
        PlotW,
        Spinner,
        CaretW,
        WinMinW,
        WinMinH,
        PopGap,
        MenuGap,
        TipPadX,
        TipPadY,
        TipX,
        TipY,
        TipRadius,
        ShadowX,
        ShadowY,
        Align,
        HeadingSize,
        SmallSize,
        Ease,
        HoverMs,
        PressMs,
        PopupMs,
        CollapseMs,
        ToastFadeMs,
    }

    public enum Align
    {
        Auto,
        Left,
        Center,
        Right,
    }

    public enum Ease
    {
        Smooth,
        Linear,
        Out,
        InOut,
        Back,
        Custom,
        In,
        Spring,
        Bounce,
    }

    public static class Easing
    {
        public static double At(this Ease ease, double t) => Native.Ease((int)ease, t);
    }

    public enum TextDirection
    {
        Auto,
        Ltr,
        Rtl,
    }

    public enum Role
    {
        None,
        Window,
        Button,
        Checkbox,
        Radio,
        Slider,
        Text,
        Input,
        Combo,
        List,
        Item,
        Tab,
        Menu,
        MenuItem,
        Table,
        Tree,
        Progress,
        Image,
        Group,
        Dialog,
    }

    [Flags]
    public enum A11yState
    {
        None = 0,
        Focused = 1,
        Checked = 2,
        Disabled = 4,
        Expanded = 8,
        Selected = 16,
    }

    public enum A11yAction
    {
        Press,
        Set,
        Expand,
        Focus,
    }

    public enum Preset
    {
        Dark,
        Light,
        Midnight,
        Paper,
        Mint,
        HighContrast,
        Rose,
    }

    public enum KeyAction
    {
        Copy,
        Cut,
        Paste,
        Undo,
        Redo,
        SelectAll,
        WordLeft,
        WordRight,
        DeleteWordLeft,
        DeleteWordRight,
        DocStart,
        DocEnd,
    }

    public enum CursorKind
    {
        Button,
        Input,
        Link,
        DragValue,
        SplitX,
        SplitY,
        Grip,
        Column,
        DragDrop,
        CustomX,
        CustomY,
        CustomXY,
    }

    [Flags]
    public enum InputFlags
    {
        None = 0,
        Password = 1,
        ReadOnly = 2,
        Number = 4,
        NoSelectOnFocus = 8,
        EnterReturns = 16,
    }

    public enum SkinKind
    {
        Button,
        Panel,
        Input,
        Checkbox,
        Radio,
        Toggle,
        SliderTrack,
        SliderFill,
        Scrollbar,
        Tab,
        Titlebar,
        Tooltip,
        Menu,
        Popup,
    }

    public enum Icon
    {
        ArrowDown,
        ArrowRight,
        ArrowUp,
        ArrowLeft,
        Check,
        RadioDot,
        Close,
        Minimize,
        Maximize,
        Restore,
        Resize,
        SortUp,
        SortDown,
        Plus,
        Minus,
        SliderKnob,
        ToggleKnob,
        Collapse,
        Expand,
        DragHandle,
        Toast,
        Picker,
    }

    [Flags]
    public enum ItemFlags
    {
        None = 0,
        DragX = 1,
        DragY = 2,
        Drag = DragX | DragY,
        Focusable = 4,
        NoHit = 8,
        Repeat = 16,
    }

    public enum Format
    {
        BgraPremul,
        RgbaPremul,
        Bgra,
        Rgba,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Item
    {
        public double X, Y, W, H;
        int hovered, pressed, held, released, clicked, doubleClicked, rightClicked, focused;
        public double Mx, My;
        public double Dx, Dy;
        public double DragX, DragY;
        public int Key;

        public bool Hovered => hovered != 0;
        public bool Pressed => pressed != 0;
        public bool Held => held != 0;
        public bool Released => released != 0;
        public bool Clicked => clicked != 0;
        public bool DoubleClicked => doubleClicked != 0;
        public bool RightClicked => rightClicked != 0;
        public bool Focused => focused != 0;
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
        #if NETSTANDARD2_0
        public override int GetHashCode() => ((X * 397 ^ Y) * 397 ^ W) * 397 ^ H;
#else
        public override int GetHashCode() => HashCode.Combine(X, Y, W, H);
#endif
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
    public unsafe struct RawA11yNode
    {
        public int Role, Parent, States;
        public double X, Y, W, H;
        public fixed byte Label[96];
        public fixed byte Value[64];
        public uint Id;
        public double Num, Min, Max;
        public int Caret;
    }

    public sealed class A11yNode
    {
        public int Index { get; }
        public Role Role { get; }
        public int Parent { get; }
        public A11yState States { get; }
        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }
        public string Label { get; }
        public string Value { get; }
        public uint Id { get; }
        public double Number { get; }
        public double Min { get; }
        public double Max { get; }
        public int Caret { get; }

        internal unsafe A11yNode(int index, RawA11yNode* n)
        {
            Index = index;
            Role = (Role)n->Role;
            Parent = n->Parent;
            States = (A11yState)n->States;
            X = n->X;
            Y = n->Y;
            Width = n->W;
            Height = n->H;
            Label = Native.Str(n->Label) ?? "";
            Value = Native.Str(n->Value) ?? "";
            Id = n->Id;
            Number = n->Num;
            Min = n->Min;
            Max = n->Max;
            Caret = n->Caret;
        }

        public bool Focused => (States & A11yState.Focused) != 0;
        public bool Checked => (States & A11yState.Checked) != 0;
        public bool Disabled => (States & A11yState.Disabled) != 0;
        public bool Expanded => (States & A11yState.Expanded) != 0;
        public bool Selected => (States & A11yState.Selected) != 0;

        public override string ToString() => $"{Role} '{Label}'{(Value.Length > 0 ? $" = '{Value}'" : "")} {States}";
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
        public RawIcons Icon;
        public uint IconTint;
        public RawSkin SkinCheckbox, SkinRadio, SkinToggle, SkinSlider, SkinSliderFill, SkinScrollbar, SkinTab, SkinTitlebar, SkinTooltip, SkinMenu, SkinPopup;
        public uint Focus, Select, Caret, Link, Badge, Toast, Tooltip, Danger, Knob, Dim;
        public double CheckSize, CheckRadius, ToggleW, ToggleH, SliderH, SliderTrack, KnobSize;
        public double ScrollW, ScrollMin, ItemH, MenuItemH, TabH, WinTitleH, AppTitleH, MenubarH, StatusbarH;
        public double FocusW, FocusGap, SepW, Grip, Indent, PlotW, Spinner, CaretW;
        public double WinMinW, WinMinH, PopGap, MenuGap, TipPadX, TipPadY, TipX, TipY, TipRadius;
        public double ShadowX, ShadowY, Align, HeadingSize, SmallSize, Ease;
        public double HoverMs, PressMs, PopupMs, CollapseMs, ToastFadeMs;
        public IntPtr FontHeading, FontMono, FontSmall;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawCombo
    {
        public int Key, Mods;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawUiConfig
    {
        public const int Actions = 12, Cursors = 12, PerAction = 4;
        public int TipDelayMs, DblClickMs, DblClickPx, DragPx, RepeatDelayMs, RepeatMs, BlinkMs, MenuDelayMs, ToastMs;
        public double ScrollLines;
        public int RaiseOnClick, EscCloses, TabNav, FocusOnMouse;
        public fixed int Cursor[Cursors];
        public fixed int Keys[Actions * PerAction * 2];
        public IntPtr Ease, EaseUser, Mask;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawIcons
    {
        public const int Count = 22;
        IntPtr p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16, p17, p18, p19, p20, p21;

        public RawBitmap* this[Icon i]
        {
            get
            {
                if ((uint)i >= Count) throw new ArgumentOutOfRangeException(nameof(i));
                fixed (RawIcons* p = &this) return ((RawBitmap**)p)[(int)i];
            }
            set
            {
                if ((uint)i >= Count) throw new ArgumentOutOfRangeException(nameof(i));
                fixed (RawIcons* p = &this) ((RawBitmap**)p)[(int)i] = value;
            }
        }
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
        internal string? full;

        public Event(EventType type)
        {
            raw = default;
            full = null;
            raw.Type = (int)type;
        }

        public Event(EventType type, double x, double y)
        {
            raw = default;
            full = null;
            raw.Type = (int)type;
            raw.X = x;
            raw.Y = y;
        }

        public Event(in RawEvent r)
        {
            raw = r;
            full = null;
        }

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
                if (full != null) return full;
                fixed (byte* p = raw.Msg) return p[0] == 0 ? "" : Native.Str(p) ?? "";
            }
        }

        public bool IsText => Type == EventType.Text;
        public bool IsPreedit => Type == EventType.Preedit;
        public int Caret => Type == EventType.Preedit ? Utf8.CharIndex(Message, raw.Id) : 0;

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

        public static Event Commit(string text)
        {
            var e = new Event(EventType.Text);
            e.SetMessage(text);
            return e;
        }

        public static Event Preedit(string text, int caret = -1)
        {
            var e = new Event(EventType.Preedit);
            e.SetMessage(text);
            string kept = e.Message;
            e.raw.Id = Utf8.ByteIndex(kept, caret < 0 ? kept.Length : Math.Min(caret, kept.Length));
            return e;
        }

        public static Event A11y(A11yAction action, uint id, string? text = null)
        {
            string verb = action switch
            {
                A11yAction.Set => "set",
                A11yAction.Expand => "expand",
                A11yAction.Focus => "focus",
                _ => "press",
            };
            return Control(text == null ? $"a11y {verb} {id}" : $"a11y {verb} {id} {text}");
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
            EventType.Text => $"Text '{Message}'",
            EventType.Preedit => $"Preedit '{Message}' caret {Caret}",
            _ => Type.ToString(),
        };
    }

    internal static class Utf8
    {
        internal static int CharIndex(string s, int bytes)
        {
            int n = 0, i = 0;
            while (i < s.Length && n < bytes)
            {
                bool pair = char.IsSurrogatePair(s, i);
                n += pair ? 4 : s[i] < 0x80 ? 1 : s[i] < 0x800 ? 2 : 3;
                i += pair ? 2 : 1;
            }
            return i;
        }

        internal static int ByteIndex(string s, int chars)
        {
            int n = 0;
            for (int i = 0; i < chars && i < s.Length; i++)
            {
                if (char.IsSurrogatePair(s, i))
                {
                    n += 4;
                    i++;
                }
                else n += s[i] < 0x80 ? 1 : s[i] < 0x800 ? 2 : 3;
            }
            return n;
        }

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
