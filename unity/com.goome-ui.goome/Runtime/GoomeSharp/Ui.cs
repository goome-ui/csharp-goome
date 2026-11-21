#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using S = GoomeSharp.Style;

namespace GoomeSharp
{
    /// <summary>The immediate mode ui kit. Keep your own variables and call the widgets every frame between Begin and End</summary>
    public sealed unsafe class Ui : IDisposable
    {
        IntPtr u;
        readonly Goome g;
        readonly List<object?> pushed = new List<object?>();
        readonly List<object?> held = new List<object?>();
        byte[] edit = new byte[4096];
        Want autoGrab = Want.Mouse | Want.Keyboard;

        /// <summary>Makes a ui for a goome. Dispose it before the goome</summary>
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

        /// <summary>The goome this ui draws on</summary>
        public Goome Goome => g;
        /// <summary>The colors, sizes and pictures this ui draws with</summary>
        public Theme Theme { get; }
        /// <summary>Behavior settings like tooltip delay, double click time and the key map</summary>
        public UiConfig Config { get; }
        /// <summary>True once Dispose has run</summary>
        public bool IsDisposed => u == IntPtr.Zero;

        /// <summary>Frees the ui</summary>
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

        /// <summary>Gives the ui an event</summary>
        /// <returns>True if the ui used it</returns>
        public bool Feed(in Event ev)
        {
            RawEvent r = ev.raw;
            return Native.UiFeed(Handle, &r) != 0;
        }

        /// <summary>Starts a ui frame on that monitor</summary>
        /// <returns>The canvas, or null if there is nothing to draw on, skip the frame</returns>
        public Canvas? Begin(int monitor = 0)
        {
            var p = Native.UiBegin(Handle, monitor);
            pushed.Clear();
            held.Clear();
            return p == null ? null : g.Wrap(p);
        }

        /// <summary>Ends the frame and sends it, with every widgets hit area</summary>
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
        /// <summary>Adds a hit area by hand, for stuff you draw yourself</summary>
        public void Hit(double x, double y, double w, double h) => Native.UiHit(Handle, x, y, w, h);
        /// <summary>True while something still moves, draw again soon</summary>
        public bool Animating => Native.UiAnimating(Handle) != 0;
        /// <summary>What the ui wants from the game right now, good after Feed and End</summary>
        public Want Wants => (Want)Native.UiWants(Handle);
        /// <summary>True when the mouse is over the ui, a press or drag is going, or a popup or modal is open</summary>
        public bool WantsMouse => (Wants & Want.Mouse) != 0;
        /// <summary>True when a widget has keyboard focus or a modal is open</summary>
        public bool WantsKeyboard => (Wants & Want.Keyboard) != 0;

        /// <summary>What the ui takes from the os by itself, both by default. None means your app does it</summary>
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

        /// <summary>A window you can move and resize. Always call WindowEnd, even when this returns false</summary>
        /// <param name="x">Position, updated when the user drags it</param>
        /// <param name="y">Position, updated when the user drags it</param>
        /// <param name="w">Size, updated when the user resizes it</param>
        /// <param name="h">Height, 0 fits the content</param>
        /// <returns>True while the window is showing, false when collapsed or closed</returns>
        public bool Window(string title, ref double x, ref double y, ref double w, ref double h, WindowFlags flags = WindowFlags.None)
        {
            bool open = true;
            return WindowCall(title, ref x, ref y, ref w, ref h, flags, false, ref open) != 0;
        }

        /// <summary>A window you can move and resize. Always call WindowEnd, even when this returns false</summary>
        /// <param name="x">Position, updated when the user drags it</param>
        /// <param name="y">Position, updated when the user drags it</param>
        /// <param name="w">Size, updated when the user resizes it</param>
        /// <param name="h">Height, 0 fits the content</param>
        /// <param name="open">The close button sets it to false</param>
        /// <returns>True while the window is showing, false when collapsed or closed</returns>
        public bool Window(string title, ref double x, ref double y, ref double w, ref double h, WindowFlags flags, ref bool open) =>
            WindowCall(title, ref x, ref y, ref w, ref h, flags, true, ref open) != 0;

        /// <summary>Ends a window, call it every time</summary>
        public void WindowEnd() => Native.UiWindowEnd(Handle);
        /// <summary>Puts the next widgets side by side, sharing the width</summary>
        /// <param name="cols">How many widgets</param>
        public void Row(int cols) => Native.UiRow(Handle, cols);
        /// <summary>Puts the next widget right of the last one</summary>
        public void SameLine() => Native.UiSameLine(Handle);
        /// <summary>Adds a gap</summary>
        /// <param name="px">Gap size</param>
        public void Space(double px) => Native.UiSpace(Handle, px);
        /// <summary>Draws a line</summary>
        public void Separator() => Native.UiSeparator(Handle);
        /// <summary>Sets the width of the next widget</summary>
        /// <param name="w">0 fills the rest</param>
        public void Width(double w) => Native.UiWidth(Handle, w);

        /// <summary>Draws a line of text</summary>
        public void Label(string text)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiLabel(hu, s);
        }

        /// <summary>Draws a bigger line of text</summary>
        public void Heading(string text)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiHeading(hu, s);
        }

        /// <summary>Draws text that wraps at the width</summary>
        public void Wrapped(string text)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiWrapped(hu, s);
        }

        /// <summary>A button</summary>
        /// <returns>True on the frame it was clicked</returns>
        public bool Button(string label)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label)) return Native.UiButton(hu, s) != 0;
        }

        /// <summary>A button that shows a bitmap</summary>
        /// <param name="id">Id for the button, no label to take it from</param>
        /// <returns>True on the frame it was clicked</returns>
        public bool ImageButton(string id, Bitmap b, double w, double h)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) return Native.UiImageButton(hu, s, b.Ptr, w, h) != 0;
        }

        /// <summary>Draws a bitmap</summary>
        /// <param name="radius">Corner radius</param>
        public void Image(Bitmap b, double w, double h, double radius = 0) => Native.UiImage(Handle, b.Ptr, w, h, radius);

        /// <summary>A checkbox</summary>
        /// <returns>True when it changed</returns>
        public bool Checkbox(string label, ref bool v)
        {
            var hu = Handle;
            int i = v ? 1 : 0;
            int r;
            fixed (byte* s = U8.A(label)) r = Native.UiCheckbox(hu, s, &i);
            v = i != 0;
            return r != 0;
        }

        /// <summary>A switch</summary>
        /// <returns>True when it changed</returns>
        public bool Toggle(string label, ref bool v)
        {
            var hu = Handle;
            int i = v ? 1 : 0;
            int r;
            fixed (byte* s = U8.A(label)) r = Native.UiToggle(hu, s, &i);
            v = i != 0;
            return r != 0;
        }

        /// <summary>A radio button. It is picked when v equals value</summary>
        /// <param name="v">The shared choice, set to value when picked</param>
        /// <param name="value">What this button stands for</param>
        /// <returns>True when it was picked this frame</returns>
        public bool Radio(string label, ref int v, int value)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (int* p = &v) return Native.UiRadio(hu, s, p, value) != 0;
        }

        /// <summary>A slider for a double</summary>
        /// <param name="format">printf style, like "%.2f"</param>
        /// <returns>True when it changed</returns>
        public bool Slider(string label, ref double v, double min, double max, string? format = null)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label), f = U8.B(format))
            fixed (double* p = &v) return Native.UiSlider(hu, s, p, min, max, f) != 0;
        }

        /// <summary>A slider for an int</summary>
        /// <returns>True when it changed</returns>
        public bool SliderInt(string label, ref int v, int min, int max)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (int* p = &v) return Native.UiSliderInt(hu, s, p, min, max) != 0;
        }

        /// <summary>A single line text box</summary>
        /// <param name="buffer">utf-8 bytes, zero ended, its length is the most it can hold</param>
        /// <returns>True when the text changed</returns>
        public bool Input(string label, byte[] buffer)
        {
            if (buffer.Length == 0) return false;
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (byte* p = buffer) return Native.UiInput(hu, s, p, buffer.Length) != 0;
        }

        /// <summary>A single line text box</summary>
        /// <param name="maxBytes">Most bytes the text can take, with the zero</param>
        /// <returns>True when the text changed</returns>
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

        /// <summary>A text box with flags and a hint</summary>
        /// <param name="buffer">utf-8 bytes, zero ended, its length is the most it can hold</param>
        /// <param name="hint">Dim text shown while empty</param>
        /// <returns>0 nothing, 1 changed, 2 enter was pressed with EnterReturns</returns>
        public int InputEx(string label, byte[] buffer, InputFlags flags = InputFlags.None, string? hint = null)
        {
            if (buffer.Length == 0) return 0;
            var hu = Handle;
            fixed (byte* s = U8.A(label), h = U8.B(hint))
            fixed (byte* p = buffer) return Native.UiInputEx(hu, s, p, buffer.Length, (int)flags, h);
        }

        /// <summary>A text box with flags and a hint</summary>
        /// <param name="hint">Dim text shown while empty</param>
        /// <returns>0 nothing, 1 changed, 2 enter was pressed with EnterReturns</returns>
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

        /// <summary>A text box that draws a mask instead of the letters, see UiConfig.Mask</summary>
        /// <returns>0 nothing, 1 changed, 2 enter was pressed</returns>
        public int Password(string label, ref string text, string? hint = null, InputFlags flags = InputFlags.EnterReturns, int maxBytes = 256) =>
            InputEx(label, ref text, flags | InputFlags.Password, hint, maxBytes);

        /// <summary>Multiplies every size that isnt em, plus the old sizes</summary>
        /// <param name="s">1 is normal</param>
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

        /// <summary>A drop down</summary>
        /// <param name="selected">Index of the picked item</param>
        /// <returns>True when the pick changed</returns>
        public bool Combo(string label, ref int selected, IReadOnlyList<string> items)
        {
            var hu = Handle;
            using var list = new Strings(items);
            fixed (byte* s = U8.A(label))
            fixed (int* p = &selected) return Native.UiCombo(hu, s, p, list.P, items.Count) != 0;
        }

        /// <summary>A row you can click, drawn highlighted when selected</summary>
        /// <returns>True when it was clicked</returns>
        public bool Selectable(string label, bool selected)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label)) return Native.UiSelectable(hu, s, selected ? 1 : 0) != 0;
        }

        /// <summary>A tab bar</summary>
        /// <param name="id">Id for the tab bar</param>
        /// <param name="selected">Index of the open tab</param>
        /// <returns>True when the tab changed</returns>
        public bool Tabs(string id, IReadOnlyList<string> names, ref int selected)
        {
            var hu = Handle;
            using var list = new Strings(names);
            fixed (byte* s = U8.A(id))
            fixed (int* p = &selected) return Native.UiTabs(hu, s, list.P, names.Count, p) != 0;
        }

        /// <summary>A header that folds the stuff under it</summary>
        /// <param name="open">Open or shut</param>
        /// <returns>True while open</returns>
        public bool Collapsing(string label, ref bool open)
        {
            var hu = Handle;
            int o = open ? 1 : 0;
            int r;
            fixed (byte* s = U8.A(label)) r = Native.UiCollapsing(hu, s, &o);
            open = o != 0;
            return r != 0;
        }

        /// <summary>A progress bar</summary>
        /// <param name="fraction">0 to 1, below 0 shows a busy bar that slides back and forth</param>
        /// <param name="text">Shown on the bar</param>
        public void Progress(double fraction, string? text = null)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiProgress(hu, fraction, s);
        }

        /// <summary>A color picker</summary>
        /// <param name="argb">The color as 0xAARRGGBB</param>
        /// <returns>True when it changed</returns>
        public bool Color(string label, ref uint argb)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (uint* p = &argb) return Native.UiColor(hu, s, p) != 0;
        }

        /// <summary>Sets the tooltip of the widget right before it</summary>
        public void Tooltip(string text)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiTooltip(hu, s);
        }

        /// <summary>True if the mouse is over the widget right before it</summary>
        public bool Hovered => Native.UiHovered(Handle) != 0;

        /// <summary>Starts a scroll area, always call ScrollEnd</summary>
        /// <param name="height">0 fills the rest of the window</param>
        public bool ScrollBegin(string id, double height = 0)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) return Native.UiScrollBegin(hu, s, height) != 0;
        }

        /// <summary>Ends a scroll area</summary>
        public void ScrollEnd() => Native.UiScrollEnd(Handle);

        /// <summary>Pushes an id. Use it with the index when the same labels repeat in a loop</summary>
        public void PushId(string id)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) Native.UiPushId(hu, s);
        }

        /// <summary>Pops the id from PushId</summary>
        public void PopId() => Native.UiPopId(Handle);

        /// <summary>Root panel that fills monitor 0, call it after Begin</summary>
        /// <returns>True while it is showing</returns>
        public bool App(string title, AppFlags flags = AppFlags.None)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(title)) return Native.UiApp(hu, s, (int)flags) != 0;
        }

        /// <summary>Ends the app root</summary>
        public void AppEnd() => Native.UiAppEnd(Handle);
        /// <summary>Starts the menu bar. Needs AppFlags.MenuBar</summary>
        /// <returns>True when there is one to fill</returns>
        public bool MenuBarBegin() => Native.UiMenubarBegin(Handle) != 0;
        /// <summary>Ends the menu bar</summary>
        public void MenuBarEnd() => Native.UiMenubarEnd(Handle);

        /// <summary>Starts a menu, nest them for submenus</summary>
        /// <returns>True while open, then call MenuEnd</returns>
        public bool MenuBegin(string label)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label)) return Native.UiMenuBegin(hu, s) != 0;
        }

        /// <summary>Ends a menu</summary>
        public void MenuEnd() => Native.UiMenuEnd(Handle);

        /// <summary>A menu entry</summary>
        /// <param name="shortcut">Text shown on the right</param>
        /// <returns>True when clicked</returns>
        public bool MenuItem(string label, string? shortcut = null)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label), k = U8.B(shortcut)) return Native.UiMenuItem(hu, s, k, null) != 0;
        }

        /// <summary>A menu entry</summary>
        /// <param name="shortcut">Text shown on the right</param>
        /// <param name="check">Shows a check mark you flip</param>
        /// <returns>True when clicked</returns>
        public bool MenuItem(string label, string? shortcut, ref bool check)
        {
            var hu = Handle;
            int c = check ? 1 : 0;
            int r;
            fixed (byte* s = U8.A(label), k = U8.B(shortcut)) r = Native.UiMenuItem(hu, s, k, &c);
            check = c != 0;
            return r != 0;
        }

        /// <summary>A line between menu entries</summary>
        public void MenuSeparator() => Native.UiMenuSeparator(Handle);

        /// <summary>Right click menu for the widget before it, or empty space</summary>
        /// <returns>True while open, then call ContextEnd</returns>
        public bool ContextBegin(string id)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) return Native.UiContextBegin(hu, s) != 0;
        }

        /// <summary>Ends a context menu</summary>
        public void ContextEnd() => Native.UiContextEnd(Handle);

        /// <summary>Opens the popup with this id</summary>
        public void PopupOpen(string id)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) Native.UiPopupOpen(hu, s);
        }

        /// <summary>Starts a popup</summary>
        /// <returns>True while open, then call PopupEnd</returns>
        public bool PopupBegin(string id)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) return Native.UiPopupBegin(hu, s) != 0;
        }

        /// <summary>Ends a popup</summary>
        public void PopupEnd() => Native.UiPopupEnd(Handle);
        /// <summary>Closes the innermost open popup, menu or modal</summary>
        public void PopupClose() => Native.UiPopupClose(Handle);

        /// <summary>A centered dialog that dims the rest, esc closes it</summary>
        /// <returns>True while open, then call ModalEnd</returns>
        public bool ModalBegin(string title)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(title)) return Native.UiModalBegin(hu, s, null) != 0;
        }

        /// <summary>A centered dialog that dims the rest, esc closes it</summary>
        /// <param name="open">The close button sets it to false</param>
        /// <returns>True while open, then call ModalEnd</returns>
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

        /// <summary>Ends a modal</summary>
        public void ModalEnd() => Native.UiModalEnd(Handle);

        /// <summary>Shows a message for a moment</summary>
        /// <param name="ms">How long, 0 uses UiConfig.ToastMs</param>
        public void Toast(string text, int ms = 3000)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiToast(hu, s, ms);
        }

        /// <summary>Sets the status bar text. Needs AppFlags.StatusBar</summary>
        public void StatusBar(string text)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiStatusbar(hu, s);
        }

        /// <summary>True if the widget right before it has keyboard focus</summary>
        public bool Focused => Native.UiFocused(Handle) != 0;
        /// <summary>Gives keyboard focus to the next widget</summary>
        public void SetFocus() => Native.UiSetFocus(Handle);

        /// <summary>A multiline text editor</summary>
        /// <param name="id">Id for the editor</param>
        /// <param name="buffer">utf-8 bytes, zero ended, its length is the most it can hold</param>
        /// <param name="height">0 fills the rest</param>
        /// <returns>True when the text changed</returns>
        public bool TextEdit(string id, byte[] buffer, double height = 0, EditFlags flags = EditFlags.None)
        {
            if (buffer.Length == 0) return false;
            var hu = Handle;
            fixed (byte* s = U8.A(id))
            fixed (byte* p = buffer) return Native.UiTextEdit(hu, s, p, buffer.Length, height, (int)flags) != 0;
        }

        /// <summary>A multiline text editor</summary>
        /// <param name="id">Id for the editor</param>
        /// <param name="height">0 fills the rest</param>
        /// <param name="maxBytes">Most bytes the text can take</param>
        /// <returns>True when the text changed</returns>
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

        /// <summary>Starts a table, add the columns next</summary>
        /// <param name="columns">Number of columns</param>
        /// <param name="height">0 fills</param>
        /// <returns>True while it shows, then call TableEnd</returns>
        public bool TableBegin(string id, int columns, double height = 0, TableFlags flags = TableFlags.None)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) return Native.UiTableBegin(hu, s, columns, height, (int)flags) != 0;
        }

        /// <summary>Adds a column header</summary>
        /// <param name="width">0 shares the rest</param>
        public void TableColumn(string name, double width = 0)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(name)) Native.UiTableColumn(hu, s, width);
        }

        /// <summary>Call it after the columns. goome doesnt sort your data, you do</summary>
        /// <param name="column">Sorted column</param>
        /// <param name="descending">Sort direction</param>
        /// <returns>True on the frame the user clicked a header</returns>
        public bool TableSort(ref int column, ref bool descending)
        {
            int d = descending ? 1 : 0;
            int r;
            fixed (int* c = &column) r = Native.UiTableSort(Handle, c, &d);
            descending = d != 0;
            return r != 0;
        }

        /// <summary>Starts a row, you are in its first cell</summary>
        public void TableRow() => Native.UiTableRow(Handle);
        /// <summary>Moves to the next cell</summary>
        public void TableNext() => Native.UiTableNext(Handle);
        /// <summary>Ends a table</summary>
        public void TableEnd() => Native.UiTableEnd(Handle);

        /// <summary>A tree node</summary>
        /// <returns>True while open, then draw the children and call TreeEnd</returns>
        public bool Tree(string label, ref bool open)
        {
            var hu = Handle;
            int o = open ? 1 : 0;
            int r;
            fixed (byte* s = U8.A(label)) r = Native.UiTree(hu, s, &o);
            open = o != 0;
            return r != 0;
        }

        /// <summary>Ends an open tree node</summary>
        public void TreeEnd() => Native.UiTreeEnd(Handle);

        /// <summary>In a scroll area or table, tells you which rows are visible so you only draw those</summary>
        /// <param name="rowHeight">0 uses the selectable height</param>
        /// <param name="first">First visible row</param>
        /// <param name="last">One past the last visible row</param>
        /// <returns>True if there is anything to draw</returns>
        public bool ListClip(int count, double rowHeight, out int first, out int last)
        {
            int a = 0, b = 0;
            bool r = Native.UiListClip(Handle, count, rowHeight, &a, &b) != 0;
            first = a;
            last = b;
            return r;
        }

        /// <summary>A number you drag sideways to change</summary>
        /// <param name="speed">Change per px</param>
        /// <param name="format">printf style, like "%.2f"</param>
        /// <returns>True when it changed</returns>
        public bool DragFloat(string label, ref double v, double speed = 0.01, double min = 0, double max = 0, string? format = null)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label), f = U8.B(format))
            fixed (double* p = &v) return Native.UiDragFloat(hu, s, p, speed, min, max, f) != 0;
        }

        /// <summary>A whole number you drag sideways to change</summary>
        /// <param name="speed">Change per px</param>
        /// <returns>True when it changed</returns>
        public bool DragInt(string label, ref int v, double speed = 0.2, int min = 0, int max = 0)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (int* p = &v) return Native.UiDragInt(hu, s, p, speed, min, max) != 0;
        }

        /// <summary>A number box with plus and minus buttons</summary>
        /// <param name="step">What the buttons add</param>
        /// <returns>True when it changed</returns>
        public bool Number(string label, ref double v, double step = 1, string? format = null)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label), f = U8.B(format))
            fixed (double* p = &v) return Native.UiNumber(hu, s, p, step, f) != 0;
        }

        /// <summary>A line graph of the values</summary>
        /// <param name="min">Value at the bottom, min &gt;= max fits the range to the data</param>
        /// <param name="max">Value at the top</param>
        /// <param name="height">0 is about three lines</param>
        public void PlotLines(string label, ReadOnlySpan<float> values, double min, double max, double height = 0)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (float* p = values) Native.UiPlotLines(hu, s, p, values.Length, min, max, height);
        }

        /// <summary>A bar graph of the values</summary>
        /// <param name="min">Value at the bottom, min &gt;= max fits the range to the data</param>
        /// <param name="max">Value at the top</param>
        /// <param name="height">0 is about three lines</param>
        public void PlotBars(string label, ReadOnlySpan<float> values, double min, double max, double height = 0)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label))
            fixed (float* p = values) Native.UiPlotBars(hu, s, p, values.Length, min, max, height);
        }

        /// <summary>Starts two panes with a draggable divider. Call SplitNext between them</summary>
        /// <param name="vertical">True stacks them top and bottom</param>
        /// <param name="fraction">Size of the first pane, 0 to 1</param>
        public bool SplitBegin(string id, bool vertical, ref double fraction)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id))
            fixed (double* p = &fraction) return Native.UiSplitBegin(hu, s, vertical ? 1 : 0, p) != 0;
        }

        /// <summary>Moves on to the second pane</summary>
        public void SplitNext() => Native.UiSplitNext(Handle);
        /// <summary>Ends the split</summary>
        public void SplitEnd() => Native.UiSplitEnd(Handle);

        /// <summary>Makes the widget before it draggable with your data</summary>
        /// <param name="type">Name that decides what can drop where</param>
        /// <returns>True while it is being dragged</returns>
        public bool DragSource(string type, ReadOnlySpan<byte> data)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(type))
            fixed (byte* p = data) return Native.UiDragSource(hu, s, p, data.Length) != 0;
        }

        /// <summary>Makes the widget before it draggable with your data</summary>
        /// <param name="type">Name that decides what can drop where</param>
        /// <returns>True while it is being dragged</returns>
        public bool DragSource(string type, string text)
        {
            var hu = Handle;
            var b = U8.B(text)!;
            fixed (byte* s = U8.A(type))
            fixed (byte* p = b) return Native.UiDragSource(hu, s, p, System.Text.Encoding.UTF8.GetByteCount(text) + 1) != 0;
        }

        /// <summary>Makes the widget before it take drops of that type</summary>
        /// <param name="type">Must match the drag source</param>
        /// <param name="buffer">Where the dropped bytes go</param>
        /// <returns>Bytes dropped, 0 if nothing dropped this frame</returns>
        public int DropTarget(string type, Span<byte> buffer)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(type))
            fixed (byte* p = buffer) return Native.UiDropTarget(hu, s, p, buffer.Length);
        }

        /// <summary>Makes the widget before it take drops of that type</summary>
        /// <param name="type">Must match the drag source</param>
        /// <returns>Bytes dropped, 0 if nothing dropped this frame</returns>
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

        /// <summary>A busy spinner</summary>
        /// <param name="size">0 uses the line height</param>
        public void Spinner(double size = 0) => Native.UiSpinner(Handle, size);

        /// <summary>A text link</summary>
        /// <returns>True when clicked</returns>
        public bool Link(string text)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) return Native.UiLink(hu, s) != 0;
        }

        /// <summary>A small colored tag</summary>
        /// <param name="argb">Tag color, 0 uses the theme badge color</param>
        public void Badge(string text, uint argb)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(text)) Native.UiBadge(hu, s, argb);
        }

        /// <summary>Puts the next widget at x,y from the top left of the content, scroll included. The flow doesnt move</summary>
        public void Pos(double x, double y) => Native.UiPos(Handle, x, y);
        /// <summary>Sets the size of the next widget</summary>
        /// <param name="w">0 is its own width</param>
        /// <param name="h">0 is its own height</param>
        public void Size(double w, double h) => Native.UiSize(Handle, w, h);

        /// <summary>Pushes a color for what is drawn next, until popped</summary>
        /// <param name="what">A Style color</param>
        public void PushColor(Style what, uint argb)
        {
            if (!IsColor(what)) throw new ArgumentOutOfRangeException(nameof(what), what, "thats a number, use PushNum");
            Native.UiPushColor(Handle, (int)what, argb);
            pushed.Add(null);
        }

        /// <summary>Pushes a number for what is drawn next, until popped</summary>
        /// <param name="what">A Style number, like Radius</param>
        public void PushNum(Style what, double v)
        {
            if (IsColor(what) || what < S.Bg || what > S.ToastFadeMs) throw new ArgumentOutOfRangeException(nameof(what), what, "thats a color, use PushColor");
            Native.UiPushNum(Handle, (int)what, v);
            pushed.Add(null);
        }

        static bool IsColor(Style what) => (what >= S.Bg && what <= S.Shadow) || (what >= S.Focus && what <= S.Dim);

        /// <summary>Pushes a radius per corner, beats Style.Radius until popped</summary>
        public void PushRadii(Radii r)
        {
            Native.UiPushRadii(Handle, r);
            pushed.Add(null);
        }

        /// <summary>Pushes a font</summary>
        /// <param name="font">null is the built in font</param>
        public void PushFont(Font? font)
        {
            Native.UiPushFont(Handle, font?.Handle ?? IntPtr.Zero);
            pushed.Add(font);
        }

        /// <summary>Pushes a nine slice skin</summary>
        /// <param name="skin">null means no skin</param>
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

        /// <summary>Pushes your own picture for an icon</summary>
        public void PushIcon(Icon icon, Bitmap? bitmap)
        {
            if ((uint)icon >= RawIcons.Count) throw new ArgumentOutOfRangeException(nameof(icon));
            Native.UiPushIcon(Handle, (int)icon, bitmap == null ? null : bitmap.Ptr);
            pushed.Add(bitmap);
        }

        /// <summary>Pushes a whole theme in one go</summary>
        public void PushTheme(Theme theme)
        {
            Native.UiPushTheme(Handle, theme.Ptr);
            pushed.Add(theme);
        }

        /// <summary>Pops the last pushes of any kind</summary>
        /// <param name="n">How many</param>
        public void Pop(int n = 1)
        {
            Native.UiPop(Handle, n);
            if (n <= 0) return;
            int k = Math.Min(n, pushed.Count);
            for (int i = pushed.Count - k; i < pushed.Count; i++) Hold(pushed[i]);
            pushed.RemoveRange(pushed.Count - k, k);
        }

        /// <summary>Pushes any mix of theme values in one go, each one that isnt null. Dispose the result to pop them again, so it works with using</summary>
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

        /// <summary>A widget you draw yourself. It takes a spot in the layout and handles the mouse and tab focus like a built in one</summary>
        /// <param name="w">0 is the normal widget width</param>
        /// <param name="h">0 is one line</param>
        /// <returns>True if anything happened this frame</returns>
        public bool Custom(string id, double w, double h, ItemFlags flags, out Item item)
        {
            var hu = Handle;
            Item it;
            int r;
            fixed (byte* s = U8.A(id)) r = Native.UiCustom(hu, s, w, h, (int)flags, &it);
            item = it;
            return r != 0;
        }

        /// <summary>The canvas for drawing right in the ui, clipped like the widgets</summary>
        public Canvas? Canvas()
        {
            var p = Native.UiCanvas(Handle);
            return p == null ? null : g.Wrap(p);
        }

        /// <summary>Animates a number toward target, keeps its state by id</summary>
        /// <param name="ms">How long it takes</param>
        /// <returns>The current value</returns>
        public double Anim(string id, double target, double ms, Ease ease = Ease.Smooth)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) return Native.UiAnim(hu, s, target, ms, (int)ease);
        }

        /// <summary>Animates a color toward target, keeps its state by id</summary>
        /// <param name="ms">How long it takes</param>
        /// <returns>The current color</returns>
        public uint AnimColor(string id, uint target, double ms, Ease ease = Ease.Smooth)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(id)) return Native.UiAnimColor(hu, s, target, ms, (int)ease);
        }

        /// <summary>Turns the accessibility tree on or off</summary>
        public void A11y(bool on) => Native.UiA11y(Handle, on ? 1 : 0);

        /// <summary>Sets the role and label of the widget right before it</summary>
        public void A11yRole(Role role, string label)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(label)) Native.UiA11yRole(hu, (int)role, s);
        }

        /// <summary>Sets the value text of the widget right before it</summary>
        public void A11yValue(string value)
        {
            var hu = Handle;
            fixed (byte* s = U8.A(value)) Native.UiA11yValue(hu, s);
        }

        RawA11yNode[] tree = new RawA11yNode[64];

        /// <summary>The accessibility tree of the last frame</summary>
        public A11yNode[] AccessibilityTree()
        {
            var hu = Handle;
            int n;
            while (true)
            {
                fixed (RawA11yNode* p = tree) n = Native.A11yTree(hu, p, tree.Length);
                if (n <= tree.Length || tree.Length >= 65536) break;
                tree = new RawA11yNode[Math.Max(n, tree.Length * 2)];
            }
            n = Math.Min(n, tree.Length);
            var o = new A11yNode[Math.Max(0, n)];
            fixed (RawA11yNode* p = tree)
                for (int i = 0; i < o.Length; i++) o[i] = new A11yNode(i, p + i);
            return o;
        }

        /// <summary>Does an accessibility action on a node, like a screen reader would</summary>
        /// <param name="id">Id of the node</param>
        /// <param name="text">New text, for Set</param>
        /// <returns>True if it was queued</returns>
        public bool A11yPerform(A11yAction action, uint id, string? text = null) => g.Push(Event.A11y(action, id, text));

        /// <summary>Keeps doubles per id between frames, all 0 the first time</summary>
        /// <param name="n">How many doubles</param>
        public Span<double> Store(string id, int n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
            var hu = Handle;
            double* p;
            fixed (byte* s = U8.A(id)) p = Native.UiStore(hu, s, n);
            return p == null ? Span<double>.Empty : new Span<double>(p, n);
        }
    }

    /// <summary>What Ui.Style returns. Dispose it to pop everything that call pushed</summary>
    public readonly struct Pushed : IDisposable
    {
        readonly Ui? ui;

        internal Pushed(Ui u, int n)
        {
            ui = u;
            Count = n;
        }

        /// <summary>How many pushes it holds</summary>
        public int Count { get; }

        /// <summary>Pops them</summary>
        public void Dispose()
        {
            if (ui != null && !ui.IsDisposed && Count > 0) ui.Pop(Count);
        }
    }

    /// <summary>Colors, sizes, fonts, skins and icons of a ui. Change them on Ui.Theme or push a whole one</summary>
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

        /// <summary>Makes a standalone theme, dark or light, or a copy of another one</summary>
        /// <param name="light">True for the light colors</param>
        public Theme(bool light = false)
        {
            own = (RawTheme*)Mem.Alloc(sizeof(RawTheme));
            if (light) Native.UiThemeLight(own);
            else Native.UiThemeDark(own);
            Icons = new ThemeIcons(this);
            Skins = new ThemeSkins(this);
        }

        /// <summary>Makes a standalone theme, dark or light, or a copy of another one</summary>
        /// <param name="from">Theme to copy</param>
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

        /// <summary>Frees the theme</summary>
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

        /// <summary>Your own pictures for the little icons</summary>
        public ThemeIcons Icons { get; }
        /// <summary>Your own nine slice pictures for the widgets</summary>
        public ThemeSkins Skins { get; }
        /// <summary>Color the icons are drawn with, 0 draws them as they are</summary>
        public uint IconTint { get => T->IconTint; set => T->IconTint = value; }

        /// <summary>Skin for buttons</summary>
        public Skin? ButtonSkin { get => skins[0]; set => SetSkin(SkinKind.Button, value); }
        /// <summary>Skin for windows</summary>
        public Skin? PanelSkin { get => skins[1]; set => SetSkin(SkinKind.Panel, value); }
        /// <summary>Skin for text inputs, the text editor and drag numbers</summary>
        public Skin? InputSkin { get => skins[2]; set => SetSkin(SkinKind.Input, value); }
        /// <summary>Skin for the checkbox box</summary>
        public Skin? CheckboxSkin { get => skins[3]; set => SetSkin(SkinKind.Checkbox, value); }
        /// <summary>Skin for the radio circle</summary>
        public Skin? RadioSkin { get => skins[4]; set => SetSkin(SkinKind.Radio, value); }
        /// <summary>Skin for the track behind the toggle knob</summary>
        public Skin? ToggleSkin { get => skins[5]; set => SetSkin(SkinKind.Toggle, value); }
        /// <summary>Skin for the slider track</summary>
        public Skin? SliderTrackSkin { get => skins[6]; set => SetSkin(SkinKind.SliderTrack, value); }
        /// <summary>Skin for the filled part of the slider</summary>
        public Skin? SliderFillSkin { get => skins[7]; set => SetSkin(SkinKind.SliderFill, value); }
        /// <summary>Skin for the scrollbar thumb</summary>
        public Skin? ScrollbarSkin { get => skins[8]; set => SetSkin(SkinKind.Scrollbar, value); }
        /// <summary>Skin for tabs</summary>
        public Skin? TabSkin { get => skins[9]; set => SetSkin(SkinKind.Tab, value); }
        /// <summary>Skin for window and app title bars</summary>
        public Skin? TitlebarSkin { get => skins[10]; set => SetSkin(SkinKind.Titlebar, value); }
        /// <summary>Skin for tooltips</summary>
        public Skin? TooltipSkin { get => skins[11]; set => SetSkin(SkinKind.Tooltip, value); }
        /// <summary>Skin for menus and the open combo list</summary>
        public Skin? MenuSkin { get => skins[12]; set => SetSkin(SkinKind.Menu, value); }
        /// <summary>Skin for popups and modals, the color picker too</summary>
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

        /// <summary>The raw c struct</summary>
        public ref RawTheme Raw => ref *T;

        /// <summary>Sets the dark colors, leaves the font alone</summary>
        public void Dark() => Native.UiThemeDark(T);
        /// <summary>Sets the light colors, only the first 11 colors change</summary>
        public void Light() => Native.UiThemeLight(T);
        /// <summary>Makes a theme from one accent color. The instance version changes this theme, the static one returns a new theme</summary>
        /// <param name="accent">Accent color</param>
        /// <param name="dark">Dark or light</param>
        /// <param name="radius">Corner radius</param>
        public void Make(uint accent, bool dark = true, double radius = 10) => Native.UiThemeMake(T, accent, dark ? 1 : 0, radius);

        /// <summary>Sets one of the ready made looks. The static overload returns a new theme</summary>
        public void Preset(GoomeSharp.Preset preset)
        {
            if ((uint)preset > (uint)GoomeSharp.Preset.Rose) throw new ArgumentOutOfRangeException(nameof(preset));
            Native.UiThemePreset(T, (int)preset);
        }

        /// <summary>Makes a theme from one accent color. The instance version changes this theme, the static one returns a new theme</summary>
        /// <param name="accent">Accent color</param>
        /// <param name="dark">Dark or light</param>
        /// <param name="radius">Corner radius</param>
        /// <param name="from">Theme to start from</param>
        public static Theme Make(uint accent, bool dark = true, double radius = 10, Theme? from = null)
        {
            var t = from != null ? new Theme(from) : new Theme();
            t.Make(accent, dark, radius);
            return t;
        }

        /// <summary>Sets one of the ready made looks. The static overload returns a new theme</summary>
        /// <param name="from">Theme to start from</param>
        public static Theme Preset(GoomeSharp.Preset preset, Theme? from = null)
        {
            var t = from != null ? new Theme(from) : new Theme();
            t.Preset(preset);
            return t;
        }

        /// <summary>Behind everything, app mode fills with it</summary>
        public uint Bg { get => T->Bg; set => T->Bg = value; }
        /// <summary>Window background</summary>
        public uint Panel { get => T->Panel; set => T->Panel = value; }
        /// <summary>Window and widget outlines</summary>
        public uint Border { get => T->Border; set => T->Border = value; }
        /// <summary>Normal text</summary>
        public uint Text { get => T->Text; set => T->Text = value; }
        /// <summary>Quieter text</summary>
        public uint TextDim { get => T->TextDim; set => T->TextDim = value; }
        /// <summary>Checked boxes, slider fill and the active tab</summary>
        public uint Accent { get => T->Accent; set => T->Accent = value; }
        /// <summary>Text on top of the accent</summary>
        public uint AccentText { get => T->AccentText; set => T->AccentText = value; }
        /// <summary>Widget background while hovered</summary>
        public uint Hover { get => T->Hover; set => T->Hover = value; }
        /// <summary>Widget background while pressed</summary>
        public uint Active { get => T->Active; set => T->Active = value; }
        /// <summary>Inside inputs, sliders and combos</summary>
        public uint InputBg { get => T->InputBg; set => T->InputBg = value; }
        /// <summary>Window shadow color</summary>
        public uint Shadow { get => T->Shadow; set => T->Shadow = value; }
        /// <summary>Corner radius</summary>
        public double Radius { get => T->Radius; set => T->Radius = value; }
        /// <summary>Space inside windows</summary>
        public double Pad { get => T->Pad; set => T->Pad = value; }
        /// <summary>Space between widgets</summary>
        public double Gap { get => T->Gap; set => T->Gap = value; }
        /// <summary>Outline width</summary>
        public double BorderWidth { get => T->BorderWidth; set => T->BorderWidth = value; }
        /// <summary>Text size, checkboxes and knobs grow with it</summary>
        public double FontSize { get => T->FontSize; set => T->FontSize = value; }
        /// <summary>How soft shadows are</summary>
        public double ShadowBlur { get => T->ShadowBlur; set => T->ShadowBlur = value; }
        /// <summary>How long hovers and toggles take, 0 turns animation off</summary>
        public double AnimMs { get => T->AnimMs; set => T->AnimMs = value; }
        /// <summary>Focus ring and the input glow, 0 uses the accent</summary>
        public uint Focus { get => T->Focus; set => T->Focus = value; }
        /// <summary>Selected text color, 0 uses the accent faded</summary>
        public uint Select { get => T->Select; set => T->Select = value; }
        /// <summary>Text caret color, 0 uses the accent</summary>
        public uint Caret { get => T->Caret; set => T->Caret = value; }
        /// <summary>Link color, 0 uses the accent</summary>
        public uint Link { get => T->Link; set => T->Link = value; }
        /// <summary>Badge color when none is passed, 0 uses the accent</summary>
        public uint Badge { get => T->Badge; set => T->Badge = value; }
        /// <summary>Toast background, 0 mixes panel and hover</summary>
        public uint Toast { get => T->Toast; set => T->Toast = value; }
        /// <summary>Tooltip background, 0 mixes bg and hover</summary>
        public uint Tooltip { get => T->Tooltip; set => T->Tooltip = value; }
        /// <summary>The red close buttons, 0 uses the built in red</summary>
        public uint Danger { get => T->Danger; set => T->Danger = value; }
        /// <summary>Slider and toggle knobs, 0 = white</summary>
        public uint Knob { get => T->Knob; set => T->Knob = value; }
        /// <summary>Color behind modals, 0 = 0x73000000</summary>
        public uint Dim { get => T->Dim; set => T->Dim = value; }
        /// <summary>Checkbox and radio box size</summary>
        public double CheckSize { get => T->CheckSize; set => T->CheckSize = value; }
        /// <summary>Checkbox corner radius</summary>
        public double CheckRadius { get => T->CheckRadius; set => T->CheckRadius = value; }
        /// <summary>Toggle track width</summary>
        public double ToggleW { get => T->ToggleW; set => T->ToggleW = value; }
        /// <summary>Toggle track height</summary>
        public double ToggleH { get => T->ToggleH; set => T->ToggleH = value; }
        /// <summary>Slider height</summary>
        public double SliderH { get => T->SliderH; set => T->SliderH = value; }
        /// <summary>Slider track thickness</summary>
        public double SliderTrack { get => T->SliderTrack; set => T->SliderTrack = value; }
        /// <summary>Slider knob radius</summary>
        public double KnobSize { get => T->KnobSize; set => T->KnobSize = value; }
        /// <summary>Scrollbar width</summary>
        public double ScrollW { get => T->ScrollW; set => T->ScrollW = value; }
        /// <summary>Shortest a scrollbar thumb gets</summary>
        public double ScrollMin { get => T->ScrollMin; set => T->ScrollMin = value; }
        /// <summary>Height of every widget row, 0 = line height plus pad</summary>
        public double ItemH { get => T->ItemH; set => T->ItemH = value; }
        /// <summary>Height of menu rows, 0 comes from the font</summary>
        public double MenuItemH { get => T->MenuItemH; set => T->MenuItemH = value; }
        /// <summary>Tab height, 0 comes from the font</summary>
        public double TabH { get => T->TabH; set => T->TabH = value; }
        /// <summary>Window title bar height, 0 comes from the font</summary>
        public double WinTitleH { get => T->WinTitleH; set => T->WinTitleH = value; }
        /// <summary>App mode title bar height, 0 comes from the font</summary>
        public double AppTitleH { get => T->AppTitleH; set => T->AppTitleH = value; }
        /// <summary>App menu bar height, 0 comes from the font</summary>
        public double MenubarH { get => T->MenubarH; set => T->MenubarH = value; }
        /// <summary>App status bar height, 0 comes from the font</summary>
        public double StatusbarH { get => T->StatusbarH; set => T->StatusbarH = value; }
        /// <summary>Focus ring width</summary>
        public double FocusW { get => T->FocusW; set => T->FocusW = value; }
        /// <summary>Space between the widget and its focus ring</summary>
        public double FocusGap { get => T->FocusGap; set => T->FocusGap = value; }
        /// <summary>Separator thickness</summary>
        public double SepW { get => T->SepW; set => T->SepW = value; }
        /// <summary>Window resize grip size</summary>
        public double Grip { get => T->Grip; set => T->Grip = value; }
        /// <summary>Tree indent</summary>
        public double Indent { get => T->Indent; set => T->Indent = value; }
        /// <summary>Plot line width</summary>
        public double PlotW { get => T->PlotW; set => T->PlotW = value; }
        /// <summary>Spinner size when Ui.Spinner gets 0, 0 = line height</summary>
        public double Spinner { get => T->Spinner; set => T->Spinner = value; }
        /// <summary>Caret width, 0 = thin. Go wide for a block caret</summary>
        public double CaretW { get => T->CaretW; set => T->CaretW = value; }
        /// <summary>Smallest width a window resizes to</summary>
        public double WinMinW { get => T->WinMinW; set => T->WinMinW = value; }
        /// <summary>Smallest height a window resizes to</summary>
        public double WinMinH { get => T->WinMinH; set => T->WinMinH = value; }
        /// <summary>Space between a combo or popup and what opened it</summary>
        public double PopGap { get => T->PopGap; set => T->PopGap = value; }
        /// <summary>Offset of submenus</summary>
        public double MenuGap { get => T->MenuGap; set => T->MenuGap = value; }
        /// <summary>Horizontal padding in tooltips</summary>
        public double TipPadX { get => T->TipPadX; set => T->TipPadX = value; }
        /// <summary>Vertical padding in tooltips</summary>
        public double TipPadY { get => T->TipPadY; set => T->TipPadY = value; }
        /// <summary>Tooltip x spot from the mouse</summary>
        public double TipX { get => T->TipX; set => T->TipX = value; }
        /// <summary>Tooltip y spot from the mouse</summary>
        public double TipY { get => T->TipY; set => T->TipY = value; }
        /// <summary>Tooltip corner radius</summary>
        public double TipRadius { get => T->TipRadius; set => T->TipRadius = value; }
        /// <summary>Shadow x offset</summary>
        public double ShadowX { get => T->ShadowX; set => T->ShadowX = value; }
        /// <summary>Shadow y offset</summary>
        public double ShadowY { get => T->ShadowY; set => T->ShadowY = value; }
        /// <summary>How button and label text is lined up, Auto is what each widget normally does</summary>
        public Align Align { get => (Align)(int)T->Align; set => T->Align = (int)value; }
        /// <summary>Size of Ui.Heading text, 0 = 1.3 times the font size</summary>
        public double HeadingSize { get => T->HeadingSize; set => T->HeadingSize = value; }
        /// <summary>Size of badges and the status bar text, 0 = a bit under the font size</summary>
        public double SmallSize { get => T->SmallSize; set => T->SmallSize = value; }
        /// <summary>The curve for the ui animations</summary>
        public Ease Ease { get => (Ease)(int)T->Ease; set => T->Ease = (int)value; }
        /// <summary>Hover fade time, below 0 follows AnimMs</summary>
        public double HoverMs { get => T->HoverMs; set => T->HoverMs = value; }
        /// <summary>Press fade time, below 0 follows AnimMs</summary>
        public double PressMs { get => T->PressMs; set => T->PressMs = value; }
        /// <summary>Time popups and menus take to open, below 0 follows AnimMs</summary>
        public double PopupMs { get => T->PopupMs; set => T->PopupMs = value; }
        /// <summary>Time anything that folds open or shut takes, below 0 follows AnimMs</summary>
        public double CollapseMs { get => T->CollapseMs; set => T->CollapseMs = value; }
        /// <summary>Time toasts take to fade out, below 0 follows AnimMs</summary>
        public double ToastFadeMs { get => T->ToastFadeMs; set => T->ToastFadeMs = value; }

        /// <summary>Font for headings, null = Font</summary>
        public Font? FontHeading { get => Slot(T->FontHeading, headingFont); set => T->FontHeading = Keep(ref headingFont, value); }
        /// <summary>Font for the text editor with EditFlags.Mono, null = the built in mono font</summary>
        public Font? FontMono { get => Slot(T->FontMono, monoFont); set => T->FontMono = Keep(ref monoFont, value); }
        /// <summary>Font for badges and the status bar, null = Font</summary>
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

        /// <summary>The main font. Null means the built in one</summary>
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

    /// <summary>Your pictures for the little icons, by Icon</summary>
    public sealed class ThemeIcons
    {
        readonly Theme theme;

        internal ThemeIcons(Theme t) => theme = t;

        /// <summary>How many icons there are</summary>
        public int Count => RawIcons.Count;

        /// <summary>The picture for an icon, null = the built in one</summary>
        public Bitmap? this[Icon icon]
        {
            get => theme.GetIcon(Check(icon));
            set => theme.SetIcon(Check(icon), value);
        }

        static Icon Check(Icon i) => (uint)i < RawIcons.Count ? i : throw new ArgumentOutOfRangeException(nameof(i));
    }

    /// <summary>Your nine slice skins, by SkinKind</summary>
    public sealed class ThemeSkins
    {
        readonly Theme theme;

        internal ThemeSkins(Theme t) => theme = t;

        /// <summary>The skin of a widget, null = no skin</summary>
        public Skin? this[SkinKind kind]
        {
            get => (uint)kind <= (uint)SkinKind.Popup ? theme.GetSkin(kind) : throw new ArgumentOutOfRangeException(nameof(kind));
            set => theme.SetSkin(kind, value);
        }
    }

    /// <summary>Behavior of the ui, like timings and the key map. There is one per ui and it cant be pushed</summary>
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

        /// <summary>The raw c struct</summary>
        public ref RawUiConfig Raw => ref *C;

        /// <summary>Hover time before a tooltip shows</summary>
        public int TipDelayMs { get => C->TipDelayMs; set => C->TipDelayMs = value; }
        /// <summary>Longest time between the clicks of a double click</summary>
        public int DoubleClickMs { get => C->DblClickMs; set => C->DblClickMs = value; }
        /// <summary>How far the mouse can move between them</summary>
        public int DoubleClickPx { get => C->DblClickPx; set => C->DblClickPx = value; }
        /// <summary>How far the mouse goes before a press turns into a drag</summary>
        public int DragPx { get => C->DragPx; set => C->DragPx = value; }
        /// <summary>How long a key or ItemFlags.Repeat is held before it starts repeating</summary>
        public int RepeatDelayMs { get => C->RepeatDelayMs; set => C->RepeatDelayMs = value; }
        /// <summary>Time between repeats</summary>
        public int RepeatMs { get => C->RepeatMs; set => C->RepeatMs = value; }
        /// <summary>Caret blink time, 0 = no blink</summary>
        public int BlinkMs { get => C->BlinkMs; set => C->BlinkMs = value; }
        /// <summary>Hover time before a submenu opens</summary>
        public int MenuDelayMs { get => C->MenuDelayMs; set => C->MenuDelayMs = value; }
        /// <summary>How long a toast stays when you pass 0</summary>
        public int ToastMs { get => C->ToastMs; set => C->ToastMs = value; }
        /// <summary>Lines per wheel step</summary>
        public double ScrollLines { get => C->ScrollLines; set => C->ScrollLines = value; }
        /// <summary>Clicking a window brings it to the front</summary>
        public bool RaiseOnClick { get => C->RaiseOnClick != 0; set => C->RaiseOnClick = value ? 1 : 0; }
        /// <summary>Esc closes the top popup or modal</summary>
        public bool EscCloses { get => C->EscCloses != 0; set => C->EscCloses = value ? 1 : 0; }
        /// <summary>Tab and shift+tab move focus</summary>
        public bool TabNav { get => C->TabNav != 0; set => C->TabNav = value ? 1 : 0; }
        /// <summary>Clicking a widget focuses it and shows the focus ring</summary>
        public bool FocusOnMouse { get => C->FocusOnMouse != 0; set => C->FocusOnMouse = value ? 1 : 0; }

        /// <summary>The cursor shape each kind of widget asks for</summary>
        public CursorShape this[CursorKind kind]
        {
            get => (CursorShape)C->Cursor[CheckKind(kind)];
            set => C->Cursor[CheckKind(kind)] = (int)value;
        }

        static int CheckKind(CursorKind k) => (uint)k < RawUiConfig.Cursors ? (int)k : throw new ArgumentOutOfRangeException(nameof(k));

        static int CheckAction(KeyAction a) => (uint)a < RawUiConfig.Actions ? (int)a : throw new ArgumentOutOfRangeException(nameof(a));

        /// <summary>Sets the keys of an action, replacing the old ones</summary>
        /// <param name="combos">Up to 4 combos split by spaces, like "ctrl+a super+a". null or empty takes them all off</param>
        /// <returns>False if none of them parsed</returns>
        public bool Bind(KeyAction action, string? combos)
        {
            var hu = ui.Handle;
            int a = CheckAction(action);
            fixed (byte* s = U8.A(combos)) return Native.UiBind(hu, a, s) == 0;
        }

        /// <summary>The keys bound to an action right now</summary>
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

        /// <summary>What a password input draws per letter, null = "*"</summary>
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

        /// <summary>Your own easing for Ease.Custom, t goes from 0 to 1</summary>
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

        /// <summary>Goes back to the defaults</summary>
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
