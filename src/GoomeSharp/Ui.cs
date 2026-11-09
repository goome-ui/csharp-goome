using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GoomeSharp;

public sealed unsafe class Ui : IDisposable
{
    IntPtr u;
    readonly Goome g;
    internal Font? userFont;

    public Ui(Goome goome)
    {
        g = goome;
        u = Native.UiNew(goome.Handle);
        if (u == IntPtr.Zero) throw new GoomeException(GoomeError.NoMemory, "no memory for the ui");
        goome.AddUi(this);
        Theme = new Theme(this);
    }

    internal IntPtr Handle => u != IntPtr.Zero ? u : throw new ObjectDisposedException(nameof(Ui));

    public Goome Goome => g;
    public Theme Theme { get; }
    public bool IsDisposed => u == IntPtr.Zero;

    public void Dispose()
    {
        if (u == IntPtr.Zero) return;
        Native.UiFree(u);
        u = IntPtr.Zero;
        userFont = null;
    }

    public bool Feed(in Event ev)
    {
        RawEvent r = ev.raw;
        return Native.UiFeed(Handle, &r) != 0;
    }

    public Canvas? Begin(int monitor = 0)
    {
        var p = Native.UiBegin(Handle, monitor);
        return p == null ? null : g.Wrap(p);
    }

    public void End() => Native.UiEnd(Handle);
    public void Hit(double x, double y, double w, double h) => Native.UiHit(Handle, x, y, w, h);
    public bool Animating => Native.UiAnimating(Handle) != 0;

    public bool Window(string title, ref double x, ref double y, ref double w, ref double h, WindowFlags flags = WindowFlags.None)
    {
        fixed (double* px = &x, py = &y, pw = &w, ph = &h) return Native.UiWindow(Handle, title, px, py, pw, ph, (int)flags, null) != 0;
    }

    public bool Window(string title, ref double x, ref double y, ref double w, ref double h, WindowFlags flags, ref bool open)
    {
        int o = open ? 1 : 0;
        int r;
        fixed (double* px = &x, py = &y, pw = &w, ph = &h) r = Native.UiWindow(Handle, title, px, py, pw, ph, (int)flags, &o);
        open = o != 0;
        return r != 0;
    }

    public void WindowEnd() => Native.UiWindowEnd(Handle);
    public void Row(int cols) => Native.UiRow(Handle, cols);
    public void SameLine() => Native.UiSameLine(Handle);
    public void Space(double px) => Native.UiSpace(Handle, px);
    public void Separator() => Native.UiSeparator(Handle);
    public void Width(double w) => Native.UiWidth(Handle, w);

    public void Label(string text) => Native.UiLabel(Handle, text);
    public void Heading(string text) => Native.UiHeading(Handle, text);
    public void Wrapped(string text) => Native.UiWrapped(Handle, text);
    public bool Button(string label) => Native.UiButton(Handle, label) != 0;
    public bool ImageButton(string id, Bitmap b, double w, double h) => Native.UiImageButton(Handle, id, b.Ptr, w, h) != 0;
    public void Image(Bitmap b, double w, double h, double radius = 0) => Native.UiImage(Handle, b.Ptr, w, h, radius);

    public bool Checkbox(string label, ref bool v)
    {
        int i = v ? 1 : 0;
        int r = Native.UiCheckbox(Handle, label, &i);
        v = i != 0;
        return r != 0;
    }

    public bool Toggle(string label, ref bool v)
    {
        int i = v ? 1 : 0;
        int r = Native.UiToggle(Handle, label, &i);
        v = i != 0;
        return r != 0;
    }

    public bool Radio(string label, ref int v, int value)
    {
        fixed (int* p = &v) return Native.UiRadio(Handle, label, p, value) != 0;
    }

    public bool Slider(string label, ref double v, double min, double max, string? format = null)
    {
        fixed (double* p = &v) return Native.UiSlider(Handle, label, p, min, max, format) != 0;
    }

    public bool SliderInt(string label, ref int v, int min, int max)
    {
        fixed (int* p = &v) return Native.UiSliderInt(Handle, label, p, min, max) != 0;
    }

    public bool Input(string label, byte[] buffer)
    {
        if (buffer.Length == 0) return false;
        fixed (byte* p = buffer) return Native.UiInput(Handle, label, p, buffer.Length) != 0;
    }

    public bool Input(string label, ref string text, int maxBytes = 256)
    {
        if (maxBytes < 2) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        Span<byte> span = maxBytes <= 1024 ? stackalloc byte[maxBytes] : new byte[maxBytes];
        int n = Utf8Fit(text, span[..(maxBytes - 1)]);
        span[n] = 0;
        fixed (byte* buf = span)
        {
            int changed = Native.UiInput(Handle, label, buf, maxBytes);
            if (changed != 0) text = Marshal.PtrToStringUTF8((IntPtr)buf) ?? "";
            return changed != 0;
        }
    }

    static int Utf8Fit(string s, Span<byte> dst)
    {
        if (Encoding.UTF8.GetByteCount(s) <= dst.Length) return Encoding.UTF8.GetBytes(s, dst);
        int n = 0;
        foreach (var rune in s.EnumerateRunes())
        {
            if (n + rune.Utf8SequenceLength > dst.Length) break;
            n += rune.EncodeToUtf8(dst[n..]);
        }
        return n;
    }

    sealed class Strings : IDisposable
    {
        public readonly byte** P;
        readonly int n;

        public Strings(IReadOnlyList<string> items)
        {
            n = items.Count;
            P = (byte**)NativeMemory.AllocZeroed((nuint)Math.Max(1, n), (nuint)sizeof(byte*));
            for (int i = 0; i < n; i++) P[i] = (byte*)Marshal.StringToCoTaskMemUTF8(items[i]);
        }

        public void Dispose()
        {
            for (int i = 0; i < n; i++) Marshal.FreeCoTaskMem((IntPtr)P[i]);
            NativeMemory.Free(P);
        }
    }

    public bool Combo(string label, ref int selected, IReadOnlyList<string> items)
    {
        using var s = new Strings(items);
        fixed (int* p = &selected) return Native.UiCombo(Handle, label, p, s.P, items.Count) != 0;
    }

    public bool Selectable(string label, bool selected) => Native.UiSelectable(Handle, label, selected ? 1 : 0) != 0;

    public bool Tabs(string id, IReadOnlyList<string> names, ref int selected)
    {
        using var s = new Strings(names);
        fixed (int* p = &selected) return Native.UiTabs(Handle, id, s.P, names.Count, p) != 0;
    }

    public bool Collapsing(string label, ref bool open)
    {
        int o = open ? 1 : 0;
        int r = Native.UiCollapsing(Handle, label, &o);
        open = o != 0;
        return r != 0;
    }

    public void Progress(double fraction, string? text = null) => Native.UiProgress(Handle, fraction, text);

    public bool Color(string label, ref uint argb)
    {
        fixed (uint* p = &argb) return Native.UiColor(Handle, label, p) != 0;
    }

    public void Tooltip(string text) => Native.UiTooltip(Handle, text);
    public bool Hovered => Native.UiHovered(Handle) != 0;
    public bool ScrollBegin(string id, double height = 0) => Native.UiScrollBegin(Handle, id, height) != 0;
    public void ScrollEnd() => Native.UiScrollEnd(Handle);
    public void PushId(string id) => Native.UiPushId(Handle, id);
    public void PopId() => Native.UiPopId(Handle);
}

public sealed unsafe class Theme
{
    readonly Ui ui;

    internal Theme(Ui u) => ui = u;

    RawTheme* T => Native.UiTheme(ui.Handle);

    public ref RawTheme Raw => ref *T;

    public void Dark() => Native.UiThemeDark(T);
    public void Light() => Native.UiThemeLight(T);

    public uint Bg { get => T->Bg; set => T->Bg = value; }
    public uint Panel { get => T->Panel; set => T->Panel = value; }
    public uint Border { get => T->Border; set => T->Border = value; }
    public uint Text { get => T->Text; set => T->Text = value; }
    public uint TextDim { get => T->TextDim; set => T->TextDim = value; }
    public uint Accent { get => T->Accent; set => T->Accent = value; }
    public uint AccentText { get => T->AccentText; set => T->AccentText = value; }
    public uint Hover { get => T->Hover; set => T->Hover = value; }
    public uint Active { get => T->Active; set => T->Active = value; }
    public uint InputBg { get => T->InputBg; set => T->InputBg = value; }
    public uint Shadow { get => T->Shadow; set => T->Shadow = value; }
    public double Radius { get => T->Radius; set => T->Radius = value; }
    public double Pad { get => T->Pad; set => T->Pad = value; }
    public double Gap { get => T->Gap; set => T->Gap = value; }
    public double BorderWidth { get => T->BorderWidth; set => T->BorderWidth = value; }
    public double FontSize { get => T->FontSize; set => T->FontSize = value; }
    public double ShadowBlur { get => T->ShadowBlur; set => T->ShadowBlur = value; }
    public double AnimMs { get => T->AnimMs; set => T->AnimMs = value; }

    public Font Font
    {
        get
        {
            var h = T->Font;
            if (ui.userFont != null && !ui.userFont.IsBuiltIn && ui.userFont.Handle == h) return ui.userFont;
            return Font.Borrow(h);
        }
        set
        {
            var h = value?.Handle ?? IntPtr.Zero;
            T->Font = h;
            ui.userFont = value;
        }
    }
}
