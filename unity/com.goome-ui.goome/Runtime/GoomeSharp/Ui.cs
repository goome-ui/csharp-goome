#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using S = GoomeSharp.Style;

namespace GoomeSharp
{
    public sealed unsafe class Ui : IDisposable
    {
        IntPtr u;
        readonly Goome g;
        readonly List<object?> pushed = new List<object?>();
        readonly List<object?> held = new List<object?>();
        byte[] edit = new byte[4096];
        Want autoGrab = Want.Mouse | Want.Keyboard;

        public Ui(Goome goome)
        {
            g = goome;
            u = Native.UiNew(goome.Handle);
            if (u == IntPtr.Zero) throw new GoomeException(GoomeError.NoMemory, "no memory for the ui");
            goome.AddUi(this);
            Theme = new Theme(this);
            Config = new UiConfig(this);
        }

        internal IntPtr Handle => u != IntPtr.Zero ? u : throw new ObjectDisposedException(nameof(Ui));

        public Goome Goome => g;
        public Theme Theme { get; }
        public UiConfig Config { get; }
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
            pushed.Clear();
            held.Clear();
            Theme.Drop();
            Config.Drop();
        }

        public bool Feed(in Event ev)
        {
            RawEvent r = ev.raw;
            return Native.UiFeed(Handle, &r) != 0;
        }

        public Canvas? Begin(int monitor = 0)
        {
            var p = Native.UiBegin(Handle, monitor);
            pushed.Clear();
            held.Clear();
            return p == null ? null : g.Wrap(p);
        }

        public void End()
        {
            Native.UiEnd(Handle);
            pushed.Clear();
            held.Clear();
        }

        internal void Hold(object? o)
        {
            if (o != null) held.Add(o);
        }
        public void Hit(double x, double y, double w, double h) => Native.UiHit(Handle, x, y, w, h);
        public bool Animating => Native.UiAnimating(Handle) != 0;
        public Want Wants => (Want)Native.UiWants(Handle);
        public bool WantsMouse => (Wants & Want.Mouse) != 0;
        public bool WantsKeyboard => (Wants & Want.Keyboard) != 0;

        public Want AutoGrab
        {
            get => autoGrab;
            set
            {
                Native.UiAutoGrab(Handle, (int)value);
                autoGrab = value;
            }
        }

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

        public int InputEx(string label, byte[] buffer, InputFlags flags = InputFlags.None, string? hint = null)
        {
            if (buffer.Length == 0) return 0;
            var hu = Handle;
            fixed (byte* s = U8.A(label), h = U8.B(hint))
            fixed (byte* p = buffer) return Native.UiInputEx(hu, s, p, buffer.Length, (int)flags, h);
        }

        public int InputEx(string label, ref string text, InputFlags flags = InputFlags.None, string? hint = null, int maxBytes = 256)
        {
            if (maxBytes < 2) throw new ArgumentOutOfRangeException(nameof(maxBytes));
            var hu = Handle;
            Span<byte> span = maxBytes <= 1024 ? stackalloc byte[maxBytes] : new byte[maxBytes];
            int n = Utf8.Fit(text, span.Slice(0, maxBytes - 1));
            span[n] = 0;
            fixed (byte* s = U8.A(label), h = U8.B(hint))
            fixed (byte* buf = span)
            {
                int r = Native.UiInputEx(hu, s, buf, maxBytes, (int)flags, h);
                if (r != 0) text = Native.Str(buf) ?? "";
                return r;
            }
        }

        public int Password(string label, ref string text, string? hint = null, InputFlags flags = InputFlags.EnterReturns, int maxBytes = 256) =>
            InputEx(label, ref text, flags | InputFlags.Password, hint, maxBytes);

        public void Scale(double s) => Native.UiScale(Handle, s);

        sealed class Strings : IDisposable
        {
            public readonly byte** P;
            readonly int n;

            public Strings(IReadOnlyList<string> items)
            {
                n = items.Count;
                P = (byte**)Mem.Alloc((long)Math.Max(1, n) * sizeof(byte*));
                for (int i = 0; i < n; i++) P[i] = (byte*)U8.CoTask(items[i]);
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

        public void Pos(double x, double y) => Native.UiPos(Handle, x, y);
        public void Size(double w, double h) => Native.UiSize(Handle, w, h);

        public void PushColor(Style what, uint argb)
        {
            if (!IsColor(what)) throw new ArgumentOutOfRangeException(nameof(what), what, "thats a number, use PushNum");
            Native.UiPushColor(Handle, (int)what, argb);
            pushed.Add(null);
        }

        public void PushNum(Style what, double v)
        {
            if (IsColor(what) || what < S.Bg || what > S.ToastFadeMs) throw new ArgumentOutOfRangeException(nameof(what), what, "thats a color, use PushColor");
            Native.UiPushNum(Handle, (int)what, v);
            pushed.Add(null);
        }

        static bool IsColor(Style what) => (what >= S.Bg && what <= S.Shadow) || (what >= S.Focus && what <= S.Dim);

        public void PushRadii(Radii r)
        {
            Native.UiPushRadii(Handle, r);
            pushed.Add(null);
        }

        public void PushFont(Font? font)
        {
            Native.UiPushFont(Handle, font?.Handle ?? IntPtr.Zero);
            pushed.Add(font);
        }

        public void PushSkin(SkinKind kind, Skin? skin)
        {
            var hu = Handle;
            if (skin == null) Native.UiPushSkin(hu, (int)kind, null);
            else
            {
                var r = new RawSkin { Bm = skin.Bitmap.Ptr, L = skin.Left, T = skin.Top, R = skin.Right, B = skin.Bottom };
                Native.UiPushSkin(hu, (int)kind, &r);
            }
            pushed.Add(skin);
        }

        public void PushIcon(Icon icon, Bitmap? bitmap)
        {
            if ((uint)icon >= RawIcons.Count) throw new ArgumentOutOfRangeException(nameof(icon));
            Native.UiPushIcon(Handle, (int)icon, bitmap == null ? null : bitmap.Ptr);
            pushed.Add(bitmap);
        }

        public void PushTheme(Theme theme)
        {
            Native.UiPushTheme(Handle, theme.Ptr);
            pushed.Add(theme);
        }

        public void Pop(int n = 1)
        {
            Native.UiPop(Handle, n);
            if (n <= 0) return;
            int k = Math.Min(n, pushed.Count);
            for (int i = pushed.Count - k; i < pushed.Count; i++) Hold(pushed[i]);
            pushed.RemoveRange(pushed.Count - k, k);
        }

        public Pushed Style(Theme? theme = null, Font? font = null, double? radius = null, Radii? radii = null,
            uint? bg = null, uint? panel = null, uint? border = null, uint? text = null, uint? textDim = null, uint? accent = null, uint? accentText = null,
            uint? hover = null, uint? active = null, uint? inputBg = null, uint? shadow = null,
            double? pad = null, double? gap = null, double? borderWidth = null, double? fontSize = null, double? shadowBlur = null, double? animMs = null,
            Skin? buttonSkin = null, Skin? panelSkin = null, Skin? inputSkin = null,
            (Icon, Bitmap?)? icon = null, IEnumerable<KeyValuePair<Icon, Bitmap?>>? icons = null,
            uint? focus = null, uint? select = null, uint? caret = null, uint? link = null, uint? badge = null, uint? toast = null, uint? tooltip = null,
            uint? danger = null, uint? knob = null, uint? dim = null,
            double? checkSize = null, double? checkRadius = null, double? toggleW = null, double? toggleH = null, double? sliderH = null, double? sliderTrack = null,
            double? knobSize = null, double? scrollW = null, double? scrollMin = null, double? itemH = null, double? menuItemH = null, double? tabH = null,
            double? winTitleH = null, double? appTitleH = null, double? menubarH = null, double? statusbarH = null, double? focusW = null, double? focusGap = null,
            double? sepW = null, double? grip = null, double? indent = null, double? plotW = null, double? spinner = null, double? caretW = null,
            double? winMinW = null, double? winMinH = null, double? popGap = null, double? menuGap = null, double? tipPadX = null, double? tipPadY = null,
            double? tipX = null, double? tipY = null, double? tipRadius = null, double? shadowX = null, double? shadowY = null, Align? align = null,
            double? headingSize = null, double? smallSize = null, Ease? ease = null,
            double? hoverMs = null, double? pressMs = null, double? popupMs = null, double? collapseMs = null, double? toastFadeMs = null)
        {
            int before = pushed.Count;
            if (theme != null) PushTheme(theme);
            if (font != null) PushFont(font);
            Color(S.Bg, bg);
            Color(S.Panel, panel);
            Color(S.Border, border);
            Color(S.Text, text);
            Color(S.TextDim, textDim);
            Color(S.Accent, accent);
            Color(S.AccentText, accentText);
            Color(S.Hover, hover);
            Color(S.Active, active);
            Color(S.InputBg, inputBg);
            Color(S.Shadow, shadow);
            Num(S.Radius, radius);
            Num(S.Pad, pad);
            Num(S.Gap, gap);
            Num(S.BorderWidth, borderWidth);
            Num(S.FontSize, fontSize);
            Num(S.ShadowBlur, shadowBlur);
            Num(S.AnimMs, animMs);
            Color(S.Focus, focus);
            Color(S.Select, select);
            Color(S.Caret, caret);
            Color(S.Link, link);
            Color(S.Badge, badge);
            Color(S.Toast, toast);
            Color(S.Tooltip, tooltip);
            Color(S.Danger, danger);
            Color(S.Knob, knob);
            Color(S.Dim, dim);
            Num(S.CheckSize, checkSize);
            Num(S.CheckRadius, checkRadius);
            Num(S.ToggleW, toggleW);
            Num(S.ToggleH, toggleH);
            Num(S.SliderH, sliderH);
            Num(S.SliderTrack, sliderTrack);
            Num(S.KnobSize, knobSize);
            Num(S.ScrollW, scrollW);
            Num(S.ScrollMin, scrollMin);
            Num(S.ItemH, itemH);
            Num(S.MenuItemH, menuItemH);
            Num(S.TabH, tabH);
            Num(S.WinTitleH, winTitleH);
            Num(S.AppTitleH, appTitleH);
            Num(S.MenubarH, menubarH);
            Num(S.StatusbarH, statusbarH);
            Num(S.FocusW, focusW);
            Num(S.FocusGap, focusGap);
            Num(S.SepW, sepW);
            Num(S.Grip, grip);
            Num(S.Indent, indent);
            Num(S.PlotW, plotW);
            Num(S.Spinner, spinner);
            Num(S.CaretW, caretW);
            Num(S.WinMinW, winMinW);
            Num(S.WinMinH, winMinH);
            Num(S.PopGap, popGap);
            Num(S.MenuGap, menuGap);
            Num(S.TipPadX, tipPadX);
            Num(S.TipPadY, tipPadY);
            Num(S.TipX, tipX);
            Num(S.TipY, tipY);
            Num(S.TipRadius, tipRadius);
            Num(S.ShadowX, shadowX);
            Num(S.ShadowY, shadowY);
            Num(S.Align, (double?)align);
            Num(S.HeadingSize, headingSize);
            Num(S.SmallSize, smallSize);
            Num(S.Ease, (double?)ease);
            Num(S.HoverMs, hoverMs);
            Num(S.PressMs, pressMs);
            Num(S.PopupMs, popupMs);
            Num(S.CollapseMs, collapseMs);
            Num(S.ToastFadeMs, toastFadeMs);
            if (radii is Radii r) PushRadii(r);
            if (buttonSkin != null) PushSkin(SkinKind.Button, buttonSkin);
            if (panelSkin != null) PushSkin(SkinKind.Panel, panelSkin);
            if (inputSkin != null) PushSkin(SkinKind.Input, inputSkin);
            if (icon is { } one) PushIcon(one.Item1, one.Item2);
            if (icons != null)
                foreach (var kv in icons) PushIcon(kv.Key, kv.Value);
            return new Pushed(this, pushed.Count - before);
        }

        void Color(Style what, uint? v)
        {
            if (v is uint c) PushColor(what, c);
        }

        void Num(Style what, double? v)
        {
            if (v is double d) PushNum(what, d);
        }

        public bool Custom(string id, double w, double h, ItemFlags flags, out Item item)
        {
            var hu = Handle;
            Item it;
            int r;
            fixed (byte* s = U8.A(id)) r = Native.UiCustom(hu, s, w, h, (int)flags, &it);
            item = it;
            return r != 0;
        }

        public Canvas? Canvas()
        {
            var p = Native.UiCanvas(Handle);
            return p == null ? null : g.Wrap(p);
        }

        public Span<double> Store(string id, int n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
            var hu = Handle;
            double* p;
            fixed (byte* s = U8.A(id)) p = Native.UiStore(hu, s, n);
            return p == null ? Span<double>.Empty : new Span<double>(p, n);
        }
    }

    public readonly struct Pushed : IDisposable
    {
        readonly Ui? ui;

        internal Pushed(Ui u, int n)
        {
            ui = u;
            Count = n;
        }

        public int Count { get; }

        public void Dispose()
        {
            if (ui != null && !ui.IsDisposed && Count > 0) ui.Pop(Count);
        }
    }

    public sealed unsafe class Theme
    {
        readonly Ui? ui;
        RawTheme* own;
        Font? userFont, headingFont, monoFont, smallFont;
        readonly Skin?[] skins = new Skin?[(int)SkinKind.Popup + 1];
        readonly Bitmap?[] icons = new Bitmap?[RawIcons.Count];

        internal Theme(Ui u)
        {
            ui = u;
            Icons = new ThemeIcons(this);
            Skins = new ThemeSkins(this);
        }

        public Theme(bool light = false)
        {
            own = (RawTheme*)Mem.Alloc(sizeof(RawTheme));
            if (light) Native.UiThemeLight(own);
            else Native.UiThemeDark(own);
            Icons = new ThemeIcons(this);
            Skins = new ThemeSkins(this);
        }

        public Theme(Theme from)
        {
            own = (RawTheme*)Mem.Alloc(sizeof(RawTheme));
            *own = *from.T;
            userFont = from.userFont;
            headingFont = from.headingFont;
            monoFont = from.monoFont;
            smallFont = from.smallFont;
            Array.Copy(from.skins, skins, skins.Length);
            Array.Copy(from.icons, icons, icons.Length);
            Icons = new ThemeIcons(this);
            Skins = new ThemeSkins(this);
        }

        ~Theme()
        {
            Mem.Free(own);
            own = null;
        }

        internal void Drop()
        {
            userFont = headingFont = monoFont = smallFont = null;
            Array.Clear(skins, 0, skins.Length);
            Array.Clear(icons, 0, icons.Length);
        }

        internal RawTheme* Ptr => T;

        RawTheme* T => ui != null ? Native.UiTheme(ui.Handle) : own;

        public ThemeIcons Icons { get; }
        public ThemeSkins Skins { get; }
        public uint IconTint { get => T->IconTint; set => T->IconTint = value; }

        public Skin? ButtonSkin { get => skins[0]; set => SetSkin(SkinKind.Button, value); }
        public Skin? PanelSkin { get => skins[1]; set => SetSkin(SkinKind.Panel, value); }
        public Skin? InputSkin { get => skins[2]; set => SetSkin(SkinKind.Input, value); }
        public Skin? CheckboxSkin { get => skins[3]; set => SetSkin(SkinKind.Checkbox, value); }
        public Skin? RadioSkin { get => skins[4]; set => SetSkin(SkinKind.Radio, value); }
        public Skin? ToggleSkin { get => skins[5]; set => SetSkin(SkinKind.Toggle, value); }
        public Skin? SliderTrackSkin { get => skins[6]; set => SetSkin(SkinKind.SliderTrack, value); }
        public Skin? SliderFillSkin { get => skins[7]; set => SetSkin(SkinKind.SliderFill, value); }
        public Skin? ScrollbarSkin { get => skins[8]; set => SetSkin(SkinKind.Scrollbar, value); }
        public Skin? TabSkin { get => skins[9]; set => SetSkin(SkinKind.Tab, value); }
        public Skin? TitlebarSkin { get => skins[10]; set => SetSkin(SkinKind.Titlebar, value); }
        public Skin? TooltipSkin { get => skins[11]; set => SetSkin(SkinKind.Tooltip, value); }
        public Skin? MenuSkin { get => skins[12]; set => SetSkin(SkinKind.Menu, value); }
        public Skin? PopupSkin { get => skins[13]; set => SetSkin(SkinKind.Popup, value); }

        internal Skin? GetSkin(SkinKind kind) => skins[(int)kind];

        internal Bitmap? GetIcon(Icon i) => icons[(int)i];

        internal void SetIcon(Icon i, Bitmap? b)
        {
            ui?.Hold(icons[(int)i]);
            T->Icon[i] = b == null ? null : b.Ptr;
            icons[(int)i] = b;
        }

        RawSkin* SkinPtr(SkinKind kind)
        {
            var t = T;
            switch (kind)
            {
            case SkinKind.Button: return &t->SkinButton;
            case SkinKind.Panel: return &t->SkinPanel;
            case SkinKind.Input: return &t->SkinInput;
            case SkinKind.Checkbox: return &t->SkinCheckbox;
            case SkinKind.Radio: return &t->SkinRadio;
            case SkinKind.Toggle: return &t->SkinToggle;
            case SkinKind.SliderTrack: return &t->SkinSlider;
            case SkinKind.SliderFill: return &t->SkinSliderFill;
            case SkinKind.Scrollbar: return &t->SkinScrollbar;
            case SkinKind.Tab: return &t->SkinTab;
            case SkinKind.Titlebar: return &t->SkinTitlebar;
            case SkinKind.Tooltip: return &t->SkinTooltip;
            case SkinKind.Menu: return &t->SkinMenu;
            case SkinKind.Popup: return &t->SkinPopup;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        internal void SetSkin(SkinKind kind, Skin? v)
        {
            var s = SkinPtr(kind);
            ui?.Hold(skins[(int)kind]);
            s->Bm = v == null ? null : v.Bitmap.Ptr;
            s->L = v?.Left ?? 0;
            s->T = v?.Top ?? 0;
            s->R = v?.Right ?? 0;
            s->B = v?.Bottom ?? 0;
            skins[(int)kind] = v;
        }

        public ref RawTheme Raw => ref *T;

        public void Dark() => Native.UiThemeDark(T);
        public void Light() => Native.UiThemeLight(T);
        public void Make(uint accent, bool dark = true, double radius = 10) => Native.UiThemeMake(T, accent, dark ? 1 : 0, radius);

        public void Preset(GoomeSharp.Preset preset)
        {
            if ((uint)preset > (uint)GoomeSharp.Preset.Rose) throw new ArgumentOutOfRangeException(nameof(preset));
            Native.UiThemePreset(T, (int)preset);
        }

        public static Theme Make(uint accent, bool dark = true, double radius = 10, Theme? from = null)
        {
            var t = from != null ? new Theme(from) : new Theme();
            t.Make(accent, dark, radius);
            return t;
        }

        public static Theme Preset(GoomeSharp.Preset preset, Theme? from = null)
        {
            var t = from != null ? new Theme(from) : new Theme();
            t.Preset(preset);
            return t;
        }

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
        public uint Focus { get => T->Focus; set => T->Focus = value; }
        public uint Select { get => T->Select; set => T->Select = value; }
        public uint Caret { get => T->Caret; set => T->Caret = value; }
        public uint Link { get => T->Link; set => T->Link = value; }
        public uint Badge { get => T->Badge; set => T->Badge = value; }
        public uint Toast { get => T->Toast; set => T->Toast = value; }
        public uint Tooltip { get => T->Tooltip; set => T->Tooltip = value; }
        public uint Danger { get => T->Danger; set => T->Danger = value; }
        public uint Knob { get => T->Knob; set => T->Knob = value; }
        public uint Dim { get => T->Dim; set => T->Dim = value; }
        public double CheckSize { get => T->CheckSize; set => T->CheckSize = value; }
        public double CheckRadius { get => T->CheckRadius; set => T->CheckRadius = value; }
        public double ToggleW { get => T->ToggleW; set => T->ToggleW = value; }
        public double ToggleH { get => T->ToggleH; set => T->ToggleH = value; }
        public double SliderH { get => T->SliderH; set => T->SliderH = value; }
        public double SliderTrack { get => T->SliderTrack; set => T->SliderTrack = value; }
        public double KnobSize { get => T->KnobSize; set => T->KnobSize = value; }
        public double ScrollW { get => T->ScrollW; set => T->ScrollW = value; }
        public double ScrollMin { get => T->ScrollMin; set => T->ScrollMin = value; }
        public double ItemH { get => T->ItemH; set => T->ItemH = value; }
        public double MenuItemH { get => T->MenuItemH; set => T->MenuItemH = value; }
        public double TabH { get => T->TabH; set => T->TabH = value; }
        public double WinTitleH { get => T->WinTitleH; set => T->WinTitleH = value; }
        public double AppTitleH { get => T->AppTitleH; set => T->AppTitleH = value; }
        public double MenubarH { get => T->MenubarH; set => T->MenubarH = value; }
        public double StatusbarH { get => T->StatusbarH; set => T->StatusbarH = value; }
        public double FocusW { get => T->FocusW; set => T->FocusW = value; }
        public double FocusGap { get => T->FocusGap; set => T->FocusGap = value; }
        public double SepW { get => T->SepW; set => T->SepW = value; }
        public double Grip { get => T->Grip; set => T->Grip = value; }
        public double Indent { get => T->Indent; set => T->Indent = value; }
        public double PlotW { get => T->PlotW; set => T->PlotW = value; }
        public double Spinner { get => T->Spinner; set => T->Spinner = value; }
        public double CaretW { get => T->CaretW; set => T->CaretW = value; }
        public double WinMinW { get => T->WinMinW; set => T->WinMinW = value; }
        public double WinMinH { get => T->WinMinH; set => T->WinMinH = value; }
        public double PopGap { get => T->PopGap; set => T->PopGap = value; }
        public double MenuGap { get => T->MenuGap; set => T->MenuGap = value; }
        public double TipPadX { get => T->TipPadX; set => T->TipPadX = value; }
        public double TipPadY { get => T->TipPadY; set => T->TipPadY = value; }
        public double TipX { get => T->TipX; set => T->TipX = value; }
        public double TipY { get => T->TipY; set => T->TipY = value; }
        public double TipRadius { get => T->TipRadius; set => T->TipRadius = value; }
        public double ShadowX { get => T->ShadowX; set => T->ShadowX = value; }
        public double ShadowY { get => T->ShadowY; set => T->ShadowY = value; }
        public Align Align { get => (Align)(int)T->Align; set => T->Align = (int)value; }
        public double HeadingSize { get => T->HeadingSize; set => T->HeadingSize = value; }
        public double SmallSize { get => T->SmallSize; set => T->SmallSize = value; }
        public Ease Ease { get => (Ease)(int)T->Ease; set => T->Ease = (int)value; }
        public double HoverMs { get => T->HoverMs; set => T->HoverMs = value; }
        public double PressMs { get => T->PressMs; set => T->PressMs = value; }
        public double PopupMs { get => T->PopupMs; set => T->PopupMs = value; }
        public double CollapseMs { get => T->CollapseMs; set => T->CollapseMs = value; }
        public double ToastFadeMs { get => T->ToastFadeMs; set => T->ToastFadeMs = value; }

        public Font? FontHeading { get => Slot(T->FontHeading, headingFont); set => T->FontHeading = Keep(ref headingFont, value); }
        public Font? FontMono { get => Slot(T->FontMono, monoFont); set => T->FontMono = Keep(ref monoFont, value); }
        public Font? FontSmall { get => Slot(T->FontSmall, smallFont); set => T->FontSmall = Keep(ref smallFont, value); }

        static Font? Slot(IntPtr h, Font? mine)
        {
            if (h == IntPtr.Zero) return null;
            return mine != null && !mine.IsBuiltIn && mine.Handle == h ? mine : Font.Borrow(h);
        }

        IntPtr Keep(ref Font? slot, Font? value)
        {
            ui?.Hold(slot);
            slot = value;
            return value?.Handle ?? IntPtr.Zero;
        }

        public Font Font
        {
            get
            {
                var h = T->Font;
                if (userFont != null && !userFont.IsBuiltIn && userFont.Handle == h) return userFont;
                return Font.Borrow(h);
            }
            set
            {
                var h = value?.Handle ?? IntPtr.Zero;
                T->Font = h;
                ui?.Hold(userFont);
                userFont = value;
            }
        }
    }

    public sealed class ThemeIcons
    {
        readonly Theme theme;

        internal ThemeIcons(Theme t) => theme = t;

        public int Count => RawIcons.Count;

        public Bitmap? this[Icon icon]
        {
            get => theme.GetIcon(Check(icon));
            set => theme.SetIcon(Check(icon), value);
        }

        static Icon Check(Icon i) => (uint)i < RawIcons.Count ? i : throw new ArgumentOutOfRangeException(nameof(i));
    }

    public sealed class ThemeSkins
    {
        readonly Theme theme;

        internal ThemeSkins(Theme t) => theme = t;

        public Skin? this[SkinKind kind]
        {
            get => (uint)kind <= (uint)SkinKind.Popup ? theme.GetSkin(kind) : throw new ArgumentOutOfRangeException(nameof(kind));
            set => theme.SetSkin(kind, value);
        }
    }

    public sealed unsafe class UiConfig
    {
        readonly Ui ui;
        Func<double, double>? ease;
        GCHandle self;
        IntPtr mask;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate double EaseFn(double t, IntPtr user);

        static readonly EaseFn run = Run;
        static IntPtr runPtr;

        internal UiConfig(Ui u) => ui = u;

        RawUiConfig* C => Native.UiCfg(ui.Handle);

        public ref RawUiConfig Raw => ref *C;

        public int TipDelayMs { get => C->TipDelayMs; set => C->TipDelayMs = value; }
        public int DoubleClickMs { get => C->DblClickMs; set => C->DblClickMs = value; }
        public int DoubleClickPx { get => C->DblClickPx; set => C->DblClickPx = value; }
        public int DragPx { get => C->DragPx; set => C->DragPx = value; }
        public int RepeatDelayMs { get => C->RepeatDelayMs; set => C->RepeatDelayMs = value; }
        public int RepeatMs { get => C->RepeatMs; set => C->RepeatMs = value; }
        public int BlinkMs { get => C->BlinkMs; set => C->BlinkMs = value; }
        public int MenuDelayMs { get => C->MenuDelayMs; set => C->MenuDelayMs = value; }
        public int ToastMs { get => C->ToastMs; set => C->ToastMs = value; }
        public double ScrollLines { get => C->ScrollLines; set => C->ScrollLines = value; }
        public bool RaiseOnClick { get => C->RaiseOnClick != 0; set => C->RaiseOnClick = value ? 1 : 0; }
        public bool EscCloses { get => C->EscCloses != 0; set => C->EscCloses = value ? 1 : 0; }
        public bool TabNav { get => C->TabNav != 0; set => C->TabNav = value ? 1 : 0; }
        public bool FocusOnMouse { get => C->FocusOnMouse != 0; set => C->FocusOnMouse = value ? 1 : 0; }

        public CursorShape this[CursorKind kind]
        {
            get => (CursorShape)C->Cursor[CheckKind(kind)];
            set => C->Cursor[CheckKind(kind)] = (int)value;
        }

        static int CheckKind(CursorKind k) => (uint)k < RawUiConfig.Cursors ? (int)k : throw new ArgumentOutOfRangeException(nameof(k));

        static int CheckAction(KeyAction a) => (uint)a < RawUiConfig.Actions ? (int)a : throw new ArgumentOutOfRangeException(nameof(a));

        public bool Bind(KeyAction action, string? combos)
        {
            var hu = ui.Handle;
            int a = CheckAction(action);
            fixed (byte* s = U8.A(combos)) return Native.UiBind(hu, a, s) == 0;
        }

        public (int Key, Mods Mods)[] Combos(KeyAction action)
        {
            int a = CheckAction(action);
            var c = C;
            var list = new List<(int, Mods)>();
            for (int i = 0; i < RawUiConfig.PerAction; i++)
            {
                int at = (a * RawUiConfig.PerAction + i) * 2;
                if (c->Keys[at] != 0) list.Add((c->Keys[at], (Mods)c->Keys[at + 1]));
            }
            return list.ToArray();
        }

        public string? Mask
        {
            get => Native.Str((byte*)C->Mask);
            set
            {
                var old = mask;
                mask = value == null ? IntPtr.Zero : U8.CoTask(value);
                C->Mask = mask;
                if (old != IntPtr.Zero) Marshal.FreeCoTaskMem(old);
            }
        }

        public Func<double, double>? Ease
        {
            get => ease;
            set
            {
                var c = C;
                ease = value;
                if (value == null)
                {
                    c->Ease = IntPtr.Zero;
                    c->EaseUser = IntPtr.Zero;
                    if (self.IsAllocated) self.Free();
                    return;
                }
                if (!self.IsAllocated) self = GCHandle.Alloc(this);
                if (runPtr == IntPtr.Zero) runPtr = Marshal.GetFunctionPointerForDelegate(run);
                c->Ease = runPtr;
                c->EaseUser = GCHandle.ToIntPtr(self);
            }
        }

        [MonoPInvokeCallback(typeof(EaseFn))]
        static double Run(double t, IntPtr user)
        {
            try
            {
                return GCHandle.FromIntPtr(user).Target is UiConfig c && c.ease != null ? c.ease(t) : t;
            }
            catch
            {
                return t;
            }
        }

        public void Reset()
        {
            Native.UiConfigDefault(C);
            Drop();
        }

        internal void Drop()
        {
            ease = null;
            if (self.IsAllocated) self.Free();
            if (mask != IntPtr.Zero) Marshal.FreeCoTaskMem(mask);
            mask = IntPtr.Zero;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class MonoPInvokeCallbackAttribute : Attribute
    {
        public MonoPInvokeCallbackAttribute(Type type) => Type = type;

        public Type Type { get; }
    }
}
