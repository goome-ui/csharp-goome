#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GoomeSharp
{
    public sealed unsafe class Ui : IDisposable
    {
        IntPtr u;
        readonly Goome g;
        internal Font? userFont;
        internal readonly Skin?[] skins = new Skin?[3];
        byte[] edit = new byte[4096];

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
            foreach (var sl in slots.Values) Mem.Free(sl.P);
            foreach (var sl in modalSlots.Values) Mem.Free(sl.P);
            slots.Clear();
            modalSlots.Clear();
            userFont = null;
            Array.Clear(skins, 0, skins.Length);
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

        sealed class Slot
        {
            public readonly double* P = (double*)Mem.Alloc(48);
            public readonly double[] Seen = new double[4];
            public int SeenOpen;
            public int* Open => (int*)(P + 4);
        }

        readonly Dictionary<string, Slot> slots = new Dictionary<string, Slot>(), modalSlots = new Dictionary<string, Slot>();

        Slot Get(Dictionary<string, Slot> slots, string key)
        {
            if (!slots.TryGetValue(key, out var sl))
            {
                sl = new Slot();
                slots[key] = sl;
                for (int i = 0; i < 4; i++) sl.Seen[i] = double.NaN;
                sl.SeenOpen = -1;
            }
            return sl;
        }

        static void SyncIn(Slot sl, int i, double v)
        {
            if (!v.Equals(sl.Seen[i])) sl.P[i] = v;
        }

        static double SyncOut(Slot sl, int i) => sl.Seen[i] = sl.P[i];

        static void SyncIn(Slot sl, bool open)
        {
            int o = open ? 1 : 0;
            if (o != sl.SeenOpen) *sl.Open = o;
        }

        static bool SyncOut(Slot sl) => (sl.SeenOpen = *sl.Open) != 0;

        int WindowCall(string title, ref double x, ref double y, ref double w, ref double h, WindowFlags flags, bool hasOpen, ref bool open)
        {
            var hu = Handle;
            var sl = Get(slots, title);
            SyncIn(sl, 0, x);
            SyncIn(sl, 1, y);
            SyncIn(sl, 2, w);
            SyncIn(sl, 3, h);
            if (hasOpen) SyncIn(sl, open);
            int r;
            fixed (byte* t = U8.A(title)) r = Native.UiWindow(hu, t, sl.P, sl.P + 1, sl.P + 2, sl.P + 3, (int)flags, hasOpen ? sl.Open : null);
            x = SyncOut(sl, 0);
            y = SyncOut(sl, 1);
            w = SyncOut(sl, 2);
            h = SyncOut(sl, 3);
            if (hasOpen) open = SyncOut(sl);
            return r;
        }

        public bool Window(string title, ref double x, ref double y, ref double w, ref double h, WindowFlags flags = WindowFlags.None)
        {
            bool open = true;
            return WindowCall(title, ref x, ref y, ref w, ref h, flags, false, ref open) != 0;
        }

        public bool Window(string title, ref double x, ref double y, ref double w, ref double h, WindowFlags flags, ref bool open) =>
            WindowCall(title, ref x, ref y, ref w, ref h, flags, true, ref open) != 0;

        public void WindowEnd() => Native.UiWindowEnd(Handle);
        public void Row(int cols) => Native.UiRow(Handle, cols);
        public void SameLine() => Native.UiSameLine(Handle);
        public void Space(double px) => Native.UiSpace(Handle, px);
        public void Separator() => Native.UiSeparator(Handle);
        public void Width(double w) => Native.UiWidth(Handle, w);

        public void Label(string text)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiLabel(hu, s);
        }

        public void Heading(string text)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiHeading(hu, s);
        }

        public void Wrapped(string text)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiWrapped(hu, s);
        }

        public bool Button(string label)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label)) return Native.UiButton(hu, s) != 0;
        }

        public bool ImageButton(string id, Bitmap b, double w, double h)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) return Native.UiImageButton(hu, s, b.Ptr, w, h) != 0;
        }

        public void Image(Bitmap b, double w, double h, double radius = 0) => Native.UiImage(Handle, b.Ptr, w, h, radius);

        public bool Checkbox(string label, ref bool v)
        {
            var hu = Handle;
            int i = v ? 1 : 0;
            int r;
            fixed (byte* s = U8.A(label)) r = Native.UiCheckbox(hu, s, &i);
            v = i != 0;
            return r != 0;
        }

        public bool Toggle(string label, ref bool v)
        {
            var hu = Handle;
            int i = v ? 1 : 0;
            int r;
            fixed (byte* s = U8.A(label)) r = Native.UiToggle(hu, s, &i);
            v = i != 0;
            return r != 0;
        }

        public bool Radio(string label, ref int v, int value)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (int* p = &v) return Native.UiRadio(hu, s, p, value) != 0;
        }

        public bool Slider(string label, ref double v, double min, double max, string? format = null)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label), f = U8.B(format))
            fixed (double* p = &v) return Native.UiSlider(hu, s, p, min, max, f) != 0;
        }

        public bool SliderInt(string label, ref int v, int min, int max)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (int* p = &v) return Native.UiSliderInt(hu, s, p, min, max) != 0;
        }

        public bool Input(string label, byte[] buffer)
        {
            if (buffer.Length == 0) return false;
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (byte* p = buffer) return Native.UiInput(hu, s, p, buffer.Length) != 0;
        }

        public bool Input(string label, ref string text, int maxBytes = 256)
        {
            if (maxBytes < 2) throw new ArgumentOutOfRangeException(nameof(maxBytes));
            var hu = Handle;
            Span<byte> span = maxBytes <= 1024 ? stackalloc byte[maxBytes] : new byte[maxBytes];
            int n = Utf8.Fit(text, span.Slice(0, maxBytes - 1));
            span[n] = 0;
            fixed (byte* s = U8.A(label))
            fixed (byte* buf = span)
            {
                int changed = Native.UiInput(hu, s, buf, maxBytes);
                if (changed != 0) text = Native.Str(buf) ?? "";
                return changed != 0;
            }
        }

        sealed class Strings : IDisposable
        {
            public readonly byte** P;
            readonly int n;

            public Strings(IReadOnlyList<string> items)
            {
                n = items.Count;
                P = (byte**)Mem.Alloc((long)Math.Max(1, n) * sizeof(byte*));
                for (int i = 0; i < n; i++) P[i] = (byte*)Marshal.StringToCoTaskMemUTF8(items[i]);
            }

            public void Dispose()
            {
                for (int i = 0; i < n; i++) Marshal.FreeCoTaskMem((IntPtr)P[i]);
                Mem.Free(P);
            }
        }

        public bool Combo(string label, ref int selected, IReadOnlyList<string> items)
        {
            var hu = Handle;
            using var list = new Strings(items);
            fixed (byte* s = U8.A(label))
            fixed (int* p = &selected) return Native.UiCombo(hu, s, p, list.P, items.Count) != 0;
        }

        public bool Selectable(string label, bool selected)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label)) return Native.UiSelectable(hu, s, selected ? 1 : 0) != 0;
        }

        public bool Tabs(string id, IReadOnlyList<string> names, ref int selected)
        {
            var hu = Handle;
            using var list = new Strings(names);
            fixed (byte* s = U8.A(id))
            fixed (int* p = &selected) return Native.UiTabs(hu, s, list.P, names.Count, p) != 0;
        }

        public bool Collapsing(string label, ref bool open)
        {
            var hu = Handle;
            int o = open ? 1 : 0;
            int r;
            fixed (byte* s = U8.A(label)) r = Native.UiCollapsing(hu, s, &o);
            open = o != 0;
            return r != 0;
        }

        public void Progress(double fraction, string? text = null)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiProgress(hu, fraction, s);
        }

        public bool Color(string label, ref uint argb)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (uint* p = &argb) return Native.UiColor(hu, s, p) != 0;
        }

        public void Tooltip(string text)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiTooltip(hu, s);
        }

        public bool Hovered => Native.UiHovered(Handle) != 0;

        public bool ScrollBegin(string id, double height = 0)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) return Native.UiScrollBegin(hu, s, height) != 0;
        }

        public void ScrollEnd() => Native.UiScrollEnd(Handle);

        public void PushId(string id)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) Native.UiPushId(hu, s);
        }

        public void PopId() => Native.UiPopId(Handle);

        public bool App(string title, AppFlags flags = AppFlags.None)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(title)) return Native.UiApp(hu, s, (int)flags) != 0;
        }

        public void AppEnd() => Native.UiAppEnd(Handle);
        public bool MenuBarBegin() => Native.UiMenubarBegin(Handle) != 0;
        public void MenuBarEnd() => Native.UiMenubarEnd(Handle);

        public bool MenuBegin(string label)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label)) return Native.UiMenuBegin(hu, s) != 0;
        }

        public void MenuEnd() => Native.UiMenuEnd(Handle);

        public bool MenuItem(string label, string? shortcut = null)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label), k = U8.B(shortcut)) return Native.UiMenuItem(hu, s, k, null) != 0;
        }

        public bool MenuItem(string label, string? shortcut, ref bool check)
        {
            var hu = Handle;
            int c = check ? 1 : 0;
            int r;
            fixed (byte* s = U8.A(label), k = U8.B(shortcut)) r = Native.UiMenuItem(hu, s, k, &c);
            check = c != 0;
            return r != 0;
        }

        public void MenuSeparator() => Native.UiMenuSeparator(Handle);

        public bool ContextBegin(string id)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) return Native.UiContextBegin(hu, s) != 0;
        }

        public void ContextEnd() => Native.UiContextEnd(Handle);

        public void PopupOpen(string id)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) Native.UiPopupOpen(hu, s);
        }

        public bool PopupBegin(string id)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) return Native.UiPopupBegin(hu, s) != 0;
        }

        public void PopupEnd() => Native.UiPopupEnd(Handle);
        public void PopupClose() => Native.UiPopupClose(Handle);

        public bool ModalBegin(string title)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(title)) return Native.UiModalBegin(hu, s, null) != 0;
        }

        public bool ModalBegin(string title, ref bool open)
        {
            var hu = Handle;
            var sl = Get(modalSlots, title);
            SyncIn(sl, open);
            int r;
            fixed (byte* s = U8.A(title)) r = Native.UiModalBegin(hu, s, sl.Open);
            open = SyncOut(sl);
            return r != 0;
        }

        public void ModalEnd() => Native.UiModalEnd(Handle);

        public void Toast(string text, int ms = 3000)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiToast(hu, s, ms);
        }

        public void StatusBar(string text)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiStatusbar(hu, s);
        }

        public bool Focused => Native.UiFocused(Handle) != 0;
        public void SetFocus() => Native.UiSetFocus(Handle);

        public bool TextEdit(string id, byte[] buffer, double height = 0, EditFlags flags = EditFlags.None)
        {
            if (buffer.Length == 0) return false;
            var hu = Handle;
            fixed (byte* s = U8.A(id))
            fixed (byte* p = buffer) return Native.UiTextEdit(hu, s, p, buffer.Length, height, (int)flags) != 0;
        }

        public bool TextEdit(string id, ref string text, double height = 0, EditFlags flags = EditFlags.None, int maxBytes = 16384)
        {
            if (maxBytes < 2) throw new ArgumentOutOfRangeException(nameof(maxBytes));
            var hu = Handle;
            if (edit.Length < maxBytes) edit = new byte[maxBytes];
            var span = edit.AsSpan(0, maxBytes);
            int n = Utf8.Fit(text, span.Slice(0, maxBytes - 1));
            span[n] = 0;
            fixed (byte* s = U8.A(id))
            fixed (byte* buf = edit)
            {
                int changed = Native.UiTextEdit(hu, s, buf, maxBytes, height, (int)flags);
                if (changed != 0) text = Native.Str(buf) ?? "";
                return changed != 0;
            }
        }

        public bool TableBegin(string id, int columns, double height = 0, TableFlags flags = TableFlags.None)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) return Native.UiTableBegin(hu, s, columns, height, (int)flags) != 0;
        }

        public void TableColumn(string name, double width = 0)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(name)) Native.UiTableColumn(hu, s, width);
        }

        public bool TableSort(ref int column, ref bool descending)
        {
            int d = descending ? 1 : 0;
            int r;
            fixed (int* c = &column) r = Native.UiTableSort(Handle, c, &d);
            descending = d != 0;
            return r != 0;
        }

        public void TableRow() => Native.UiTableRow(Handle);
        public void TableNext() => Native.UiTableNext(Handle);
        public void TableEnd() => Native.UiTableEnd(Handle);

        public bool Tree(string label, ref bool open)
        {
            var hu = Handle;
            int o = open ? 1 : 0;
            int r;
            fixed (byte* s = U8.A(label)) r = Native.UiTree(hu, s, &o);
            open = o != 0;
            return r != 0;
        }

        public void TreeEnd() => Native.UiTreeEnd(Handle);

        public bool ListClip(int count, double rowHeight, out int first, out int last)
        {
            int a = 0, b = 0;
            bool r = Native.UiListClip(Handle, count, rowHeight, &a, &b) != 0;
            first = a;
            last = b;
            return r;
        }

        public bool DragFloat(string label, ref double v, double speed = 0.01, double min = 0, double max = 0, string? format = null)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label), f = U8.B(format))
            fixed (double* p = &v) return Native.UiDragFloat(hu, s, p, speed, min, max, f) != 0;
        }

        public bool DragInt(string label, ref int v, double speed = 0.2, int min = 0, int max = 0)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (int* p = &v) return Native.UiDragInt(hu, s, p, speed, min, max) != 0;
        }

        public bool Number(string label, ref double v, double step = 1, string? format = null)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label), f = U8.B(format))
            fixed (double* p = &v) return Native.UiNumber(hu, s, p, step, f) != 0;
        }

        public void PlotLines(string label, ReadOnlySpan<float> values, double min, double max, double height = 0)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (float* p = values) Native.UiPlotLines(hu, s, p, values.Length, min, max, height);
        }

        public void PlotBars(string label, ReadOnlySpan<float> values, double min, double max, double height = 0)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (float* p = values) Native.UiPlotBars(hu, s, p, values.Length, min, max, height);
        }

        public bool SplitBegin(string id, bool vertical, ref double fraction)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id))
            fixed (double* p = &fraction) return Native.UiSplitBegin(hu, s, vertical ? 1 : 0, p) != 0;
        }

        public void SplitNext() => Native.UiSplitNext(Handle);
        public void SplitEnd() => Native.UiSplitEnd(Handle);

        public bool DragSource(string type, ReadOnlySpan<byte> data)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(type))
            fixed (byte* p = data) return Native.UiDragSource(hu, s, p, data.Length) != 0;
        }

        public bool DragSource(string type, string text)
        {
            var hu = Handle;
            var b = U8.B(text)!;
            fixed (byte* s = U8.A(type))
            fixed (byte* p = b) return Native.UiDragSource(hu, s, p, System.Text.Encoding.UTF8.GetByteCount(text) + 1) != 0;
        }

        public int DropTarget(string type, Span<byte> buffer)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(type))
            fixed (byte* p = buffer) return Native.UiDropTarget(hu, s, p, buffer.Length);
        }

        public bool DropTarget(string type, out string text, int maxBytes = 1024)
        {
            Span<byte> buf = maxBytes <= 1024 ? stackalloc byte[maxBytes] : new byte[maxBytes];
            int n = DropTarget(type, buf);
            if (n <= 0)
            {
                text = "";
                return false;
            }
            n = Math.Min(n, buf.Length);
            while (n > 0 && buf[n - 1] == 0) n--;
            fixed (byte* p = buf) text = System.Text.Encoding.UTF8.GetString(p, n);
            return true;
        }

        public void Spinner(double size = 0) => Native.UiSpinner(Handle, size);

        public bool Link(string text)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) return Native.UiLink(hu, s) != 0;
        }

        public void Badge(string text, uint argb)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiBadge(hu, s, argb);
        }
    }

    public sealed unsafe class Theme
    {
        public Skin? ButtonSkin { get => ui.skins[0]; set => SetSkin(0, &T->SkinButton, value); }
        public Skin? PanelSkin { get => ui.skins[1]; set => SetSkin(1, &T->SkinPanel, value); }
        public Skin? InputSkin { get => ui.skins[2]; set => SetSkin(2, &T->SkinInput, value); }

        void SetSkin(int i, RawSkin* s, Skin? v)
        {
            s->Bm = v == null ? null : v.Bitmap.Ptr;
            s->L = v?.Left ?? 0;
            s->T = v?.Top ?? 0;
            s->R = v?.Right ?? 0;
            s->B = v?.Bottom ?? 0;
            ui.skins[i] = v;
        }

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
}
