using System;
using System.Runtime.InteropServices;

namespace GoomeSharp
{
    /// <summary>Which backend goome uses. Auto picks by itself, forcing one makes it fail instead of falling back</summary>
    public enum Backend
    {
        /// <summary>Picks the best one for this desktop</summary>
        Auto,
        /// <summary>Wayland layer-shell overlay</summary>
        Wayland,
        /// <summary>GNOME, XWayland window with Mutter screen grabs</summary>
        Gnome,
        /// <summary>Wayland with screen grabs through the desktop portal</summary>
        Portal,
        /// <summary>X11</summary>
        X11,
        /// <summary>Windows</summary>
        Win32,
        /// <summary>macOS</summary>
        MacOS,
        /// <summary>A scripted backend with no display, used for tests</summary>
        Test,
        /// <summary>No window, just a pixel buffer</summary>
        Offscreen,
    }

    /// <summary>What kind of surface goome opens</summary>
    public enum Mode
    {
        /// <summary>A see-through layer over every monitor, only hit rects take clicks</summary>
        Overlay,
        /// <summary>One normal window with a title bar</summary>
        Window,
        /// <summary>No window, just a pixel buffer you read</summary>
        Offscreen,
    }

    /// <summary>Options for a window</summary>
    [Flags]
    public enum WindowStyle
    {
        /// <summary>Nothing special</summary>
        None = 0,
        /// <summary>The user can resize it</summary>
        Resizable = 1,
        /// <summary>No os title bar or border, draw your own</summary>
        Borderless = 2,
        /// <summary>The canvas alpha becomes real per pixel window alpha</summary>
        Transparent = 4,
        /// <summary>Stays above other windows</summary>
        OnTop = 8,
        /// <summary>Starts hidden, show it with Goome.Show</summary>
        Hidden = 16,
        /// <summary>Opens centered on the screen</summary>
        Center = 32,
        /// <summary>No title bar at all</summary>
        NoTitleBar = 64,
        /// <summary>Windows only, closes the console of a console exe so you only see your window</summary>
        NoConsole = 128,
    }

    /// <summary>A mouse cursor shape</summary>
    public enum CursorShape
    {
        /// <summary>The normal arrow</summary>
        Arrow,
        /// <summary>Text cursor</summary>
        IBeam,
        /// <summary>Pointing hand</summary>
        Hand,
        /// <summary>Four way move</summary>
        Move,
        /// <summary>Resize left and right</summary>
        ResizeEW,
        /// <summary>Resize up and down</summary>
        ResizeNS,
        /// <summary>Resize along the diagonal from top left to bottom right</summary>
        ResizeNWSE,
        /// <summary>Resize along the diagonal from top right to bottom left</summary>
        ResizeNESW,
        /// <summary>Busy</summary>
        Wait,
        /// <summary>Not allowed</summary>
        No,
        /// <summary>No cursor at all</summary>
        Hidden,
    }

    /// <summary>Window edges, for Goome.Drag</summary>
    [Flags]
    public enum Edges
    {
        /// <summary>No edge, Drag moves the window</summary>
        None = 0,
        /// <summary>The left edge</summary>
        Left = 1,
        /// <summary>The right edge</summary>
        Right = 2,
        /// <summary>The top edge</summary>
        Top = 4,
        /// <summary>The bottom edge</summary>
        Bottom = 8,
    }

    /// <summary>How big a window is</summary>
    public enum WindowState
    {
        /// <summary>Normal size</summary>
        Normal,
        /// <summary>Minimized</summary>
        Minimized,
        /// <summary>Maximized</summary>
        Maximized,
        /// <summary>Fullscreen</summary>
        Fullscreen,
    }

    /// <summary>Options for Ui.App</summary>
    [Flags]
    public enum AppFlags
    {
        /// <summary>Nothing special</summary>
        None = 0,
        /// <summary>Draws its own title bar: drag to move, double click to maximize, min, max and close buttons</summary>
        TitleBar = 1,
        /// <summary>Room for a menu bar at the top</summary>
        MenuBar = 2,
        /// <summary>A status bar at the bottom, set its text with Ui.StatusBar</summary>
        StatusBar = 4,
        /// <summary>No padding around the content</summary>
        NoPad = 8,
    }

    /// <summary>Options for Ui.TextEdit</summary>
    [Flags]
    public enum EditFlags
    {
        /// <summary>Nothing special</summary>
        None = 0,
        /// <summary>You can select and copy, not type</summary>
        ReadOnly = 1,
        /// <summary>A gutter with line numbers</summary>
        LineNumbers = 2,
        /// <summary>Wraps long lines instead of scrolling sideways</summary>
        Wrap = 4,
        /// <summary>Monospace, for code and logs</summary>
        Mono = 8,
    }

    /// <summary>Options for Ui.TableBegin</summary>
    [Flags]
    public enum TableFlags
    {
        /// <summary>Nothing special</summary>
        None = 0,
        /// <summary>Click a header to sort by it, again to flip</summary>
        Sortable = 1,
        /// <summary>Drag between headers to resize columns</summary>
        Resizable = 2,
        /// <summary>Every other row a bit darker</summary>
        Stripes = 4,
        /// <summary>Lines between cells</summary>
        Borders = 8,
    }

    /// <summary>What went wrong</summary>
    public enum GoomeError
    {
        /// <summary>No error</summary>
        Ok = 0,
        /// <summary>This backend cant do that</summary>
        NotSupported = -1,
        /// <summary>Not allowed, a permission is missing or the user said no</summary>
        Denied = -2,
        /// <summary>No display to draw on</summary>
        NoDisplay = -3,
        /// <summary>That monitor is gone</summary>
        Gone = -4,
        /// <summary>Nothing yet, try again in a bit</summary>
        Again = -5,
        /// <summary>Out of memory</summary>
        NoMemory = -6,
        /// <summary>Bad argument</summary>
        BadArgument = -7,
    }

    /// <summary>What works on this desktop, from Goome.Caps</summary>
    [Flags]
    public enum Caps
    {
        /// <summary>Nothing</summary>
        None = 0,
        /// <summary>Capture works</summary>
        Capture = 1,
        /// <summary>Pointer knows where the mouse is everywhere, also outside your hit rects</summary>
        GlobalPointer = 2,
        /// <summary>Hotkey works</summary>
        Hotkeys = 4,
        /// <summary>HideSelf really keeps your overlay out of grabs</summary>
        HideSelf = 8,
        /// <summary>Keyboard can take the keyboard</summary>
        Keyboard = 16,
        /// <summary>Window mode works here</summary>
        Window = 32,
        /// <summary>The clipboard reaches the real clipboard</summary>
        Clipboard = 64,
        /// <summary>Cursor changes the real cursor</summary>
        Cursor = 128,
        /// <summary>Grab gets a real pointer grab</summary>
        Grab = 256,
    }

    /// <summary>What the ui wants from the game, see Ui.Wants</summary>
    [Flags]
    public enum Want
    {
        /// <summary>Nothing</summary>
        None = 0,
        /// <summary>The mouse is over the ui, a press or drag is going, or a popup or modal is open</summary>
        Mouse = 1,
        /// <summary>A widget has keyboard focus or a modal is open</summary>
        Keyboard = 2,
    }

    /// <summary>What kind of event it is</summary>
    public enum EventType
    {
        /// <summary>Nothing</summary>
        None,
        /// <summary>Mouse button pressed</summary>
        Down,
        /// <summary>Mouse button released</summary>
        Up,
        /// <summary>Mouse moved</summary>
        Motion,
        /// <summary>Mouse wheel, Dy is in notches and positive goes down</summary>
        Scroll,
        /// <summary>Key pressed</summary>
        Key,
        /// <summary>A combo you registered with Goome.Hotkey, Id says which</summary>
        Hotkey,
        /// <summary>A line from the control fifo, in Message</summary>
        Control,
        /// <summary>The monitor list changed, ask Goome.Monitors again</summary>
        Monitors,
        /// <summary>Time to go, the session is ending or the test script hit its end</summary>
        Quit,
        /// <summary>The window close button, alt-f4 or cmd-w. The app decides what to do</summary>
        Close,
        /// <summary>The window gained or lost focus, see Event.Focused</summary>
        Focus,
        /// <summary>A file was dropped on the window, the path is in Message and X,Y is where</summary>
        Drop,
        /// <summary>An ime, dead key or compose finished some text, it is in Message</summary>
        Text,
        /// <summary>The text being composed changed. Empty means composing ended</summary>
        Preedit,
    }

    /// <summary>Modifier keys</summary>
    [Flags]
    public enum Mods
    {
        /// <summary>None held</summary>
        None = 0,
        /// <summary>Shift</summary>
        Shift = 0x100,
        /// <summary>Ctrl</summary>
        Ctrl = 0x200,
        /// <summary>Alt</summary>
        Alt = 0x400,
        /// <summary>The windows or command key</summary>
        Super = 0x800,
    }

    /// <summary>Key codes for the ones that arent a letter or digit. Letters are lowercase ascii</summary>
    public static class Keys
    {
        /// <summary>Backspace</summary>
        public const int Backspace = 8;
        /// <summary>Tab</summary>
        public const int Tab = 9;
        /// <summary>Enter</summary>
        public const int Enter = 13;
        /// <summary>Escape</summary>
        public const int Esc = 27;
        /// <summary>Space</summary>
        public const int Space = 32;
        /// <summary>Delete</summary>
        public const int Delete = 127;
        /// <summary>F1, the other F keys count up from here</summary>
        public const int F1 = 0x81;
        /// <summary>F12</summary>
        public const int F12 = 0x8C;
        /// <summary>Left arrow</summary>
        public const int Left = 0x90;
        /// <summary>Right arrow</summary>
        public const int Right = 0x91;
        /// <summary>Up arrow</summary>
        public const int Up = 0x92;
        /// <summary>Down arrow</summary>
        public const int Down = 0x93;
        /// <summary>Home</summary>
        public const int Home = 0x94;
        /// <summary>End</summary>
        public const int End = 0x95;
        /// <summary>Page up</summary>
        public const int PageUp = 0x96;
        /// <summary>Page down</summary>
        public const int PageDown = 0x97;
        /// <summary>Insert</summary>
        public const int Insert = 0x98;

        /// <summary>The code for F1 to F12</summary>
        /// <param name="n">1 to 12</param>
        public static int F(int n) => n >= 1 && n <= 12 ? F1 + n - 1 : throw new ArgumentOutOfRangeException(nameof(n));
    }

    /// <summary>Options for Ui.Window</summary>
    [Flags]
    public enum WindowFlags
    {
        /// <summary>Nothing special</summary>
        None = 0,
        /// <summary>Drag it by the title bar</summary>
        Move = 1,
        /// <summary>A resize grip in the bottom right corner</summary>
        Resize = 2,
        /// <summary>The little arrow that folds it to just the title</summary>
        Collapse = 4,
        /// <summary>No title bar at all</summary>
        NoTitle = 8,
        /// <summary>An x that sets open to false</summary>
        Close = 16,
    }

    /// <summary>A theme value you can push with Ui.PushColor or Ui.PushNum. Same names as on Theme</summary>
    public enum Style
    {
        /// <summary>Behind everything, app mode fills with it</summary>
        Bg,
        /// <summary>Window background</summary>
        Panel,
        /// <summary>Window and widget outlines</summary>
        Border,
        /// <summary>Normal text</summary>
        Text,
        /// <summary>Quieter text</summary>
        TextDim,
        /// <summary>Checked boxes, slider fill and the active tab</summary>
        Accent,
        /// <summary>Text on top of the accent</summary>
        AccentText,
        /// <summary>Widget background while hovered</summary>
        Hover,
        /// <summary>Widget background while pressed</summary>
        Active,
        /// <summary>Inside inputs, sliders and combos</summary>
        InputBg,
        /// <summary>Window shadow color</summary>
        Shadow,
        /// <summary>Corner radius</summary>
        Radius,
        /// <summary>Space inside windows</summary>
        Pad,
        /// <summary>Space between widgets</summary>
        Gap,
        /// <summary>Outline width</summary>
        BorderWidth,
        /// <summary>Text size, checkboxes and knobs grow with it</summary>
        FontSize,
        /// <summary>How soft shadows are</summary>
        ShadowBlur,
        /// <summary>How long hovers and toggles take, 0 turns animation off</summary>
        AnimMs,
        /// <summary>Focus ring and the input glow, 0 uses the accent</summary>
        Focus,
        /// <summary>Selected text color, 0 uses the accent faded</summary>
        Select,
        /// <summary>Text caret color, 0 uses the accent</summary>
        Caret,
        /// <summary>Link color, 0 uses the accent</summary>
        Link,
        /// <summary>Badge color when none is passed, 0 uses the accent</summary>
        Badge,
        /// <summary>Toast background, 0 mixes panel and hover</summary>
        Toast,
        /// <summary>Tooltip background, 0 mixes bg and hover</summary>
        Tooltip,
        /// <summary>The red close buttons, 0 uses the built in red</summary>
        Danger,
        /// <summary>Slider and toggle knobs, 0 = white</summary>
        Knob,
        /// <summary>Color behind modals, 0 = 0x73000000</summary>
        Dim,
        /// <summary>Checkbox and radio box size</summary>
        CheckSize,
        /// <summary>Checkbox corner radius</summary>
        CheckRadius,
        /// <summary>Toggle track width</summary>
        ToggleW,
        /// <summary>Toggle track height</summary>
        ToggleH,
        /// <summary>Slider height</summary>
        SliderH,
        /// <summary>Slider track thickness</summary>
        SliderTrack,
        /// <summary>Slider knob radius</summary>
        KnobSize,
        /// <summary>Scrollbar width</summary>
        ScrollW,
        /// <summary>Shortest a scrollbar thumb gets</summary>
        ScrollMin,
        /// <summary>Height of every widget row, 0 = line height plus pad</summary>
        ItemH,
        /// <summary>Height of menu rows, 0 comes from the font</summary>
        MenuItemH,
        /// <summary>Tab height, 0 comes from the font</summary>
        TabH,
        /// <summary>Window title bar height, 0 comes from the font</summary>
        WinTitleH,
        /// <summary>App mode title bar height, 0 comes from the font</summary>
        AppTitleH,
        /// <summary>App menu bar height, 0 comes from the font</summary>
        MenubarH,
        /// <summary>App status bar height, 0 comes from the font</summary>
        StatusbarH,
        /// <summary>Focus ring width</summary>
        FocusW,
        /// <summary>Space between the widget and its focus ring</summary>
        FocusGap,
        /// <summary>Separator thickness</summary>
        SepW,
        /// <summary>Window resize grip size</summary>
        Grip,
        /// <summary>Tree indent</summary>
        Indent,
        /// <summary>Plot line width</summary>
        PlotW,
        /// <summary>Spinner size when Ui.Spinner gets 0, 0 = line height</summary>
        Spinner,
        /// <summary>Caret width, 0 = thin. Go wide for a block caret</summary>
        CaretW,
        /// <summary>Smallest width a window resizes to</summary>
        WinMinW,
        /// <summary>Smallest height a window resizes to</summary>
        WinMinH,
        /// <summary>Space between a combo or popup and what opened it</summary>
        PopGap,
        /// <summary>Offset of submenus</summary>
        MenuGap,
        /// <summary>Horizontal padding in tooltips</summary>
        TipPadX,
        /// <summary>Vertical padding in tooltips</summary>
        TipPadY,
        /// <summary>Tooltip x spot from the mouse</summary>
        TipX,
        /// <summary>Tooltip y spot from the mouse</summary>
        TipY,
        /// <summary>Tooltip corner radius</summary>
        TipRadius,
        /// <summary>Shadow x offset</summary>
        ShadowX,
        /// <summary>Shadow y offset</summary>
        ShadowY,
        /// <summary>How button and label text is lined up, Auto is what each widget normally does</summary>
        Align,
        /// <summary>Size of Ui.Heading text, 0 = 1.3 times the font size</summary>
        HeadingSize,
        /// <summary>Size of badges and the status bar text, 0 = a bit under the font size</summary>
        SmallSize,
        /// <summary>The curve for the ui animations</summary>
        Ease,
        /// <summary>Hover fade time, below 0 follows AnimMs</summary>
        HoverMs,
        /// <summary>Press fade time, below 0 follows AnimMs</summary>
        PressMs,
        /// <summary>Time popups and menus take to open, below 0 follows AnimMs</summary>
        PopupMs,
        /// <summary>Time anything that folds open or shut takes, below 0 follows AnimMs</summary>
        CollapseMs,
        /// <summary>Time toasts take to fade out, below 0 follows AnimMs</summary>
        ToastFadeMs,
    }

    /// <summary>How button and label text is lined up</summary>
    public enum Align
    {
        /// <summary>What each widget normally does</summary>
        Auto,
        /// <summary>Left</summary>
        Left,
        /// <summary>Centered</summary>
        Center,
        /// <summary>Right</summary>
        Right,
    }

    /// <summary>The curve for an animation</summary>
    public enum Ease
    {
        /// <summary>Smoothstep, what the widgets use by default</summary>
        Smooth,
        /// <summary>Same speed the whole way</summary>
        Linear,
        /// <summary>Starts fast, slows down at the end. Good for most ui stuff</summary>
        Out,
        /// <summary>Slow, fast, slow</summary>
        InOut,
        /// <summary>Goes a little past and comes back</summary>
        Back,
        /// <summary>Your own function from UiConfig.Ease</summary>
        Custom,
        /// <summary>Starts slow, ends fast</summary>
        In,
        /// <summary>A damped spring, about 16% over and then it settles</summary>
        Spring,
        /// <summary>Drops and bounces like a ball</summary>
        Bounce,
    }

    /// <summary>Easing for Ease values</summary>
    public static class Easing
    {
        /// <summary>Gets the eased value at t. Ease.Custom needs UiConfig.Ease</summary>
        /// <param name="t">0 to 1</param>
        public static double At(this Ease ease, double t) => Native.Ease((int)ease, t);
    }

    /// <summary>Paragraph direction for text</summary>
    public enum TextDirection
    {
        /// <summary>Each line goes the way its first strong letter goes</summary>
        Auto,
        /// <summary>Every line is left to right</summary>
        Ltr,
        /// <summary>Every line is right to left</summary>
        Rtl,
    }

    /// <summary>What a widget is, for screen readers</summary>
    public enum Role
    {
        /// <summary>No role</summary>
        None,
        /// <summary>A window</summary>
        Window,
        /// <summary>A button</summary>
        Button,
        /// <summary>A checkbox or toggle</summary>
        Checkbox,
        /// <summary>A radio button</summary>
        Radio,
        /// <summary>A slider</summary>
        Slider,
        /// <summary>Plain text</summary>
        Text,
        /// <summary>A text input</summary>
        Input,
        /// <summary>A drop down</summary>
        Combo,
        /// <summary>A list</summary>
        List,
        /// <summary>A list item</summary>
        Item,
        /// <summary>A tab</summary>
        Tab,
        /// <summary>A menu</summary>
        Menu,
        /// <summary>A menu entry</summary>
        MenuItem,
        /// <summary>A table</summary>
        Table,
        /// <summary>A tree</summary>
        Tree,
        /// <summary>A progress bar</summary>
        Progress,
        /// <summary>An image</summary>
        Image,
        /// <summary>A group of widgets</summary>
        Group,
        /// <summary>A dialog</summary>
        Dialog,
    }

    /// <summary>Flags for the state of an accessibility node</summary>
    [Flags]
    public enum A11yState
    {
        /// <summary>Nothing</summary>
        None = 0,
        /// <summary>Has keyboard focus</summary>
        Focused = 1,
        /// <summary>Checked or on</summary>
        Checked = 2,
        /// <summary>Disabled</summary>
        Disabled = 4,
        /// <summary>Open</summary>
        Expanded = 8,
        /// <summary>Selected</summary>
        Selected = 16,
    }

    /// <summary>What an assistive tool can ask a widget to do</summary>
    public enum A11yAction
    {
        /// <summary>Press it</summary>
        Press,
        /// <summary>Set its text or value</summary>
        Set,
        /// <summary>Open it</summary>
        Expand,
        /// <summary>Give it focus</summary>
        Focus,
    }

    /// <summary>A ready made theme look, see Theme.Preset</summary>
    public enum Preset
    {
        /// <summary>The default</summary>
        Dark,
        /// <summary>Same as Theme.Light</summary>
        Light,
        /// <summary>Dark blue, radius 8</summary>
        Midnight,
        /// <summary>Warm light paper with an orange accent, radius 6</summary>
        Paper,
        /// <summary>Dark green with a mint accent, radius 12</summary>
        Mint,
        /// <summary>Pure black and white, yellow accent, 2px borders, no shadow</summary>
        HighContrast,
        /// <summary>Dark with a pink accent, radius 14</summary>
        Rose,
    }

    /// <summary>An editing action you can rebind with UiConfig.Bind</summary>
    public enum KeyAction
    {
        /// <summary>Copy</summary>
        Copy,
        /// <summary>Cut</summary>
        Cut,
        /// <summary>Paste</summary>
        Paste,
        /// <summary>Undo</summary>
        Undo,
        /// <summary>Redo</summary>
        Redo,
        /// <summary>Select all</summary>
        SelectAll,
        /// <summary>Move a word left</summary>
        WordLeft,
        /// <summary>Move a word right</summary>
        WordRight,
        /// <summary>Delete the word to the left</summary>
        DeleteWordLeft,
        /// <summary>Delete the word to the right</summary>
        DeleteWordRight,
        /// <summary>Go to the start of the text</summary>
        DocStart,
        /// <summary>Go to the end of the text</summary>
        DocEnd,
    }

    /// <summary>A kind of widget that asks for a cursor, see UiConfig</summary>
    public enum CursorKind
    {
        /// <summary>Buttons</summary>
        Button,
        /// <summary>Text inputs</summary>
        Input,
        /// <summary>Links</summary>
        Link,
        /// <summary>Drag numbers</summary>
        DragValue,
        /// <summary>A side by side split divider</summary>
        SplitX,
        /// <summary>A top and bottom split divider</summary>
        SplitY,
        /// <summary>The window resize grip</summary>
        Grip,
        /// <summary>Table column edges</summary>
        Column,
        /// <summary>While dragging something</summary>
        DragDrop,
        /// <summary>A custom widget with ItemFlags.DragX</summary>
        CustomX,
        /// <summary>A custom widget with ItemFlags.DragY</summary>
        CustomY,
        /// <summary>A custom widget with both drag flags</summary>
        CustomXY,
    }

    /// <summary>Options for Ui.InputEx</summary>
    [Flags]
    public enum InputFlags
    {
        /// <summary>Nothing special</summary>
        None = 0,
        /// <summary>Draws UiConfig.Mask per letter, copy and cut do nothing</summary>
        Password = 1,
        /// <summary>No typing or pasting, you can still select and copy</summary>
        ReadOnly = 2,
        /// <summary>Only takes digits, + - and </summary>
        Number = 4,
        /// <summary>Tabbing in puts the caret at the end instead of selecting it all</summary>
        NoSelectOnFocus = 8,
        /// <summary>The call returns 2 on enter, handy for login and search boxes</summary>
        EnterReturns = 16,
    }

    /// <summary>Which widget a skin is for</summary>
    public enum SkinKind
    {
        /// <summary>Buttons</summary>
        Button,
        /// <summary>Windows</summary>
        Panel,
        /// <summary>Text inputs, the text editor and drag numbers</summary>
        Input,
        /// <summary>The checkbox box</summary>
        Checkbox,
        /// <summary>The radio circle</summary>
        Radio,
        /// <summary>The track behind the toggle knob</summary>
        Toggle,
        /// <summary>The slider track</summary>
        SliderTrack,
        /// <summary>The filled part of the slider</summary>
        SliderFill,
        /// <summary>The scrollbar thumb</summary>
        Scrollbar,
        /// <summary>Tabs</summary>
        Tab,
        /// <summary>Window and app title bars</summary>
        Titlebar,
        /// <summary>Tooltips</summary>
        Tooltip,
        /// <summary>Menus and the open combo list</summary>
        Menu,
        /// <summary>Popups and modals, the color picker too</summary>
        Popup,
    }

    /// <summary>One of the little pictures the ui draws, you can swap them</summary>
    public enum Icon
    {
        /// <summary>Combo arrow, menubar menus, open collapsing headers and tree nodes</summary>
        ArrowDown,
        /// <summary>Closed collapsing headers and tree nodes, submenus, right side of drag numbers</summary>
        ArrowRight,
        /// <summary>Combo and menubar arrow while it is open</summary>
        ArrowUp,
        /// <summary>Left side of drag numbers</summary>
        ArrowLeft,
        /// <summary>Checkboxes, the picked combo item, checked menu items</summary>
        Check,
        /// <summary>The dot in a picked radio</summary>
        RadioDot,
        /// <summary>Window and modal close X, app title bar close</summary>
        Close,
        /// <summary>App title bar button</summary>
        Minimize,
        /// <summary>App title bar button</summary>
        Maximize,
        /// <summary>App title bar button</summary>
        Restore,
        /// <summary>The window resize grip</summary>
        Resize,
        /// <summary>Sorted table column, ascending</summary>
        SortUp,
        /// <summary>Sorted table column, descending</summary>
        SortDown,
        /// <summary>Number box button</summary>
        Plus,
        /// <summary>Number box button</summary>
        Minus,
        /// <summary>Slider knob, also the bars in the color picker</summary>
        SliderKnob,
        /// <summary>Toggle knob</summary>
        ToggleKnob,
        /// <summary>The window collapse button</summary>
        Collapse,
        /// <summary>The window collapse button when collapsed</summary>
        Expand,
        /// <summary>Split pane handle</summary>
        DragHandle,
        /// <summary>The mark on toasts</summary>
        Toast,
        /// <summary>The color picker ring</summary>
        Picker,
    }

    /// <summary>Options for Ui.Custom</summary>
    [Flags]
    public enum ItemFlags
    {
        /// <summary>Nothing special</summary>
        None = 0,
        /// <summary>Draggable, on its own it only moves sideways</summary>
        DragX = 1,
        /// <summary>Draggable, on its own only up and down</summary>
        DragY = 2,
        /// <summary>Free drag, both directions</summary>
        Drag = DragX | DragY,
        /// <summary>Gets tab focus and the keys in Item.Key</summary>
        Focusable = 4,
        /// <summary>Never hovered or pressed, for a background you put other widgets on</summary>
        NoHit = 8,
        /// <summary>Clicked fires on the press and then keeps firing while held</summary>
        Repeat = 16,
    }

    /// <summary>Byte order of pixels you copy out with Goome.FrameCopy</summary>
    public enum Format
    {
        /// <summary>B G R A, premultiplied. What goome has inside</summary>
        BgraPremul,
        /// <summary>R G B A, premultiplied</summary>
        RgbaPremul,
        /// <summary>B G R A, straight alpha</summary>
        Bgra,
        /// <summary>R G B A, straight alpha</summary>
        Rgba,
    }

    /// <summary>What happened to a custom widget this frame, filled by Ui.Custom</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Item
    {
        /// <summary>Where it ended up, logical</summary>
        public double X;
        /// <summary>Where it ended up, logical</summary>
        public double Y;
        /// <summary>Width, logical</summary>
        public double W;
        /// <summary>Height, logical</summary>
        public double H;
        int hovered, pressed, held, released, clicked, doubleClicked, rightClicked, focused;
        /// <summary>Pointer x, logical</summary>
        public double Mx;
        /// <summary>Pointer y, logical</summary>
        public double My;
        /// <summary>Pointer movement this frame while held, after the axis lock</summary>
        public double Dx;
        /// <summary>Pointer movement this frame while held, after the axis lock</summary>
        public double Dy;
        /// <summary>Movement since the press, after the axis lock</summary>
        public double DragX;
        /// <summary>Movement since the press, after the axis lock</summary>
        public double DragY;
        /// <summary>Key pressed this frame while focused with the mods ored in, 0 = none</summary>
        public int Key;

        /// <summary>The mouse is over it</summary>
        public bool Hovered => hovered != 0;
        /// <summary>The mouse went down on it this frame</summary>
        public bool Pressed => pressed != 0;
        /// <summary>The mouse is held down on it</summary>
        public bool Held => held != 0;
        /// <summary>The mouse was let go this frame</summary>
        public bool Released => released != 0;
        /// <summary>It was clicked this frame</summary>
        public bool Clicked => clicked != 0;
        /// <summary>It was double clicked this frame</summary>
        public bool DoubleClicked => doubleClicked != 0;
        /// <summary>It was right clicked this frame</summary>
        public bool RightClicked => rightClicked != 0;
        /// <summary>It has keyboard focus</summary>
        public bool Focused => focused != 0;
    }

    /// <summary>Thrown when a goome call fails</summary>
    public class GoomeException : Exception
    {
        /// <summary>The error code</summary>
        public GoomeError Code { get; }

        /// <summary>Makes the exception. An empty message becomes the text for the code</summary>
        public GoomeException(GoomeError code, string? message) : base(string.IsNullOrEmpty(message) ? Goome.StrError(code) : message) => Code = code;
    }

    /// <summary>An integer rectangle</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect : IEquatable<Rect>
    {
        /// <summary>Left</summary>
        public int X;
        /// <summary>Top</summary>
        public int Y;
        /// <summary>Width</summary>
        public int W;
        /// <summary>Height</summary>
        public int H;

        /// <summary>Makes a rect</summary>
        public Rect(int x, int y, int w, int h) => (X, Y, W, H) = (x, y, w, h);

        /// <summary>True if the point is inside the rect</summary>
        public bool Contains(double x, double y) => x >= X && y >= Y && x < X + W && y < Y + H;
        /// <summary>Compares the four numbers</summary>
        public bool Equals(Rect o) => X == o.X && Y == o.Y && W == o.W && H == o.H;
        /// <summary>Compares the four numbers</summary>
        public override bool Equals(object? o) => o is Rect r && Equals(r);
        #if NETSTANDARD2_0
        /// <summary>Hash of the four numbers</summary>
        public override int GetHashCode() => ((X * 397 ^ Y) * 397 ^ W) * 397 ^ H;
#else
        /// <summary>Hash of the four numbers</summary>
        public override int GetHashCode() => HashCode.Combine(X, Y, W, H);
#endif
        /// <summary>Compares the four numbers</summary>
        public static bool operator ==(Rect a, Rect b) => a.Equals(b);
        /// <summary>Compares the four numbers</summary>
        public static bool operator !=(Rect a, Rect b) => !a.Equals(b);
        /// <summary>Looks like "10,20 300x200"</summary>
        public override string ToString() => $"{X},{Y} {W}x{H}";
    }

    /// <summary>A radius for each corner</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Radii
    {
        /// <summary>Top left radius</summary>
        public double TopLeft;
        /// <summary>Top right radius</summary>
        public double TopRight;
        /// <summary>Bottom right radius</summary>
        public double BottomRight;
        /// <summary>Bottom left radius</summary>
        public double BottomLeft;

        /// <summary>Makes radii from the four corners, clockwise from the top left</summary>
        public Radii(double tl, double tr, double br, double bl) => (TopLeft, TopRight, BottomRight, BottomLeft) = (tl, tr, br, bl);

        /// <summary>Same radius on every corner</summary>
        public static Radii All(double r) => Native.Radius(r);
        /// <summary>A plain number turns into the same radius on every corner</summary>
        public static implicit operator Radii(double r) => new(r, r, r, r);
    }

    /// <summary>Keeps a loop at a steady frame rate. Start with a default one</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Pacer
    {
#if NET6_0_OR_GREATER
        CLong next;
        int fps;

        /// <summary>The rate it was last asked for</summary>
        public int Fps => fps;
#else
        long a, b;

        /// <summary>The rate it was last asked for</summary>
        public int Fps
        {
            get
            {
                fixed (Pacer* p = &this) return ((int*)p)[Os.Windows ? 1 : 2];
            }
        }
#endif

        /// <summary>Says how long until the next frame is due, 0 means draw now. It doesnt sleep, pass the result to Poll as the timeout</summary>
        /// <param name="fps">Frames per second, 0 or less = 60</param>
        /// <returns>Milliseconds to wait</returns>
        public unsafe int Pace(int fps)
        {
            fixed (Pacer* p = &this) return Native.Pace(p, fps);
        }
    }

    /// <summary>Raw c struct goome_opts, GoomeOptions is the one to use</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawOpts
    {
        /// <summary>Window title</summary>
        public IntPtr Title;
        /// <summary>Backend number</summary>
        public int Backend;
        /// <summary>Where the portal token and control fifo live</summary>
        public IntPtr StateDir;
        /// <summary>Overlay only, never asks for screen access</summary>
        public int NoCapture;
        /// <summary>Frame rate of gnome and portal streams, 0 = 5</summary>
        public int CaptureFps;
        /// <summary>Keep our overlay out of grabs</summary>
        public int HideSelf;
        /// <summary>Test backend only</summary>
        public IntPtr TestFrames;
        /// <summary>Test backend only</summary>
        public IntPtr TestScript;
        /// <summary>Test backend only</summary>
        public IntPtr TestShots;
        /// <summary>Mode number</summary>
        public int Mode;
        /// <summary>Window or offscreen width, logical</summary>
        public int Width;
        /// <summary>Window or offscreen height, logical</summary>
        public int Height;
        /// <summary>Smallest window width</summary>
        public int MinWidth;
        /// <summary>Smallest window height</summary>
        public int MinHeight;
        /// <summary>WindowStyle bits</summary>
        public int WindowFlags;
        /// <summary>Offscreen only, real px per logical px</summary>
        public double Scale;
    }

    /// <summary>Raw c struct goome_monitor, Monitor is the one to use</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawMonitor
    {
        /// <summary>Monitor id</summary>
        public int Id;
        /// <summary>Name, 64 bytes</summary>
        public fixed byte Name[64];
        /// <summary>Logical desktop coords</summary>
        public Rect Area;
        /// <summary>Real px per logical px</summary>
        public double Scale;
        /// <summary>Width in real px</summary>
        public int Pw;
        /// <summary>Height in real px</summary>
        public int Ph;
        /// <summary>0, 90, 180 or 270, plus 360 if mirrored</summary>
        public int Rotation;
    }

    /// <summary>Raw c struct goome_image, Image is the one to use</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawImage
    {
        /// <summary>Pixels as 0xFFRRGGBB, goome owns them</summary>
        public uint* Px;
        /// <summary>Width in real px</summary>
        public int W;
        /// <summary>Height in real px</summary>
        public int H;
        /// <summary>Row length in pixels</summary>
        public int Stride;
        /// <summary>Logical rect it covers</summary>
        public Rect Area;
        /// <summary>Monitor id</summary>
        public int Mon;
        /// <summary>Nonzero if our overlay is in it</summary>
        public int HasSelf;
        /// <summary>Nonzero if the mouse got baked in</summary>
        public int HasCursor;
        /// <summary>Logical rect of the baked in mouse</summary>
        public Rect Cursor;
    }

    /// <summary>Raw c struct goome_canvas, Canvas is the one to use</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawCanvas
    {
        /// <summary>Premultiplied argb pixels</summary>
        public uint* Px;
        /// <summary>Width in real px</summary>
        public int W;
        /// <summary>Height in real px</summary>
        public int H;
        /// <summary>Row length in pixels</summary>
        public int Stride;
        /// <summary>Logical x of the first pixel</summary>
        public double Ox;
        /// <summary>Logical y of the first pixel</summary>
        public double Oy;
        /// <summary>Real px per logical px</summary>
        public double Scale;
        /// <summary>Monitor id</summary>
        public int Mon;
        /// <summary>Left of what was drawn since the clear</summary>
        public int Dx0;
        /// <summary>Top of what was drawn since the clear</summary>
        public int Dy0;
        /// <summary>Right of what was drawn since the clear</summary>
        public int Dx1;
        /// <summary>Bottom of what was drawn since the clear</summary>
        public int Dy1;
        /// <summary>Left of what the clear wiped</summary>
        public int Cx0;
        /// <summary>Top of what the clear wiped</summary>
        public int Cy0;
        /// <summary>Right of what the clear wiped</summary>
        public int Cx1;
        /// <summary>Bottom of what the clear wiped</summary>
        public int Cy1;
        /// <summary>Left of the clip in real px</summary>
        public int Kx0;
        /// <summary>Top of the clip in real px</summary>
        public int Ky0;
        /// <summary>Right of the clip in real px</summary>
        public int Kx1;
        /// <summary>Bottom of the clip in real px</summary>
        public int Ky1;
        /// <summary>How many clips are pushed</summary>
        public int Nclip;
        /// <summary>The clip stack, 16 boxes of 4 numbers</summary>
        public fixed int Clips[16 * 4];
    }

    /// <summary>Raw c struct goome_event, Event is the one to use</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawEvent
    {
        /// <summary>EventType number</summary>
        public int Type;
        /// <summary>Logical x</summary>
        public double X;
        /// <summary>Logical y</summary>
        public double Y;
        /// <summary>Monitor id</summary>
        public int Mon;
        /// <summary>1 left, 2 middle, 3 right</summary>
        public int Button;
        /// <summary>Scroll notches, positive goes down</summary>
        public int Dy;
        /// <summary>Lowercase ascii or a Keys value</summary>
        public int Key;
        /// <summary>Mods bits</summary>
        public int Mods;
        /// <summary>Utf-8 it typed, can be empty</summary>
        public fixed byte Text[8];
        /// <summary>Hotkey id, focus flag or preedit caret byte offset</summary>
        public int Id;
        /// <summary>Control line, dropped path or ime text, cut at 63 bytes</summary>
        public fixed byte Msg[64];
    }

    /// <summary>Raw c struct goome_a11y_node, A11yNode is the one to use</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawA11yNode
    {
        /// <summary>Role number</summary>
        public int Role;
        /// <summary>Index of the parent</summary>
        public int Parent;
        /// <summary>A11yState bits</summary>
        public int States;
        /// <summary>Logical x</summary>
        public double X;
        /// <summary>Logical y</summary>
        public double Y;
        /// <summary>Logical width</summary>
        public double W;
        /// <summary>Logical height</summary>
        public double H;
        /// <summary>Label, 96 bytes</summary>
        public fixed byte Label[96];
        /// <summary>Value text, 64 bytes</summary>
        public fixed byte Value[64];
        /// <summary>Node id</summary>
        public uint Id;
        /// <summary>Number value</summary>
        public double Num;
        /// <summary>Lowest value</summary>
        public double Min;
        /// <summary>Highest value</summary>
        public double Max;
        /// <summary>Caret position</summary>
        public int Caret;
    }

    /// <summary>One node of the accessibility tree</summary>
    public sealed class A11yNode
    {
        /// <summary>Position in the tree</summary>
        public int Index { get; }
        /// <summary>What it is</summary>
        public Role Role { get; }
        /// <summary>Index of the parent node</summary>
        public int Parent { get; }
        /// <summary>Focused, checked and the rest</summary>
        public A11yState States { get; }
        /// <summary>Logical x</summary>
        public double X { get; }
        /// <summary>Logical y</summary>
        public double Y { get; }
        /// <summary>Logical width</summary>
        public double Width { get; }
        /// <summary>Logical height</summary>
        public double Height { get; }
        /// <summary>Its label</summary>
        public string Label { get; }
        /// <summary>Its text value</summary>
        public string Value { get; }
        /// <summary>Id to use with Ui.A11yPerform</summary>
        public uint Id { get; }
        /// <summary>Its number, like a slider value</summary>
        public double Number { get; }
        /// <summary>Lowest value</summary>
        public double Min { get; }
        /// <summary>Highest value</summary>
        public double Max { get; }
        /// <summary>Caret position in a text input</summary>
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

        /// <summary>Has keyboard focus</summary>
        public bool Focused => (States & A11yState.Focused) != 0;
        /// <summary>Is checked</summary>
        public bool Checked => (States & A11yState.Checked) != 0;
        /// <summary>Is disabled</summary>
        public bool Disabled => (States & A11yState.Disabled) != 0;
        /// <summary>Is open</summary>
        public bool Expanded => (States & A11yState.Expanded) != 0;
        /// <summary>Is selected</summary>
        public bool Selected => (States & A11yState.Selected) != 0;

        /// <summary>A short description</summary>
        public override string ToString() => $"{Role} '{Label}'{(Value.Length > 0 ? $" = '{Value}'" : "")} {States}";
    }

    /// <summary>Raw c struct goome_bitmap, Bitmap is the one to use</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawBitmap
    {
        /// <summary>Premultiplied argb pixels</summary>
        public uint* Px;
        /// <summary>Width in px</summary>
        public int W;
        /// <summary>Height in px</summary>
        public int H;
        /// <summary>Row length in pixels</summary>
        public int Stride;
        /// <summary>Nonzero if goome frees the pixels</summary>
        public int Owned;
    }

    /// <summary>Raw c struct goome_theme, Theme is the one to use</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawTheme
    {
        /// <summary>Behind everything, app mode fills with it</summary>
        public uint Bg;
        /// <summary>Window background</summary>
        public uint Panel;
        /// <summary>Window and widget outlines</summary>
        public uint Border;
        /// <summary>Normal text</summary>
        public uint Text;
        /// <summary>Quieter text</summary>
        public uint TextDim;
        /// <summary>Checked boxes, slider fill and the active tab</summary>
        public uint Accent;
        /// <summary>Text on top of the accent</summary>
        public uint AccentText;
        /// <summary>Widget background while hovered</summary>
        public uint Hover;
        /// <summary>Widget background while pressed</summary>
        public uint Active;
        /// <summary>Inside inputs, sliders and combos</summary>
        public uint InputBg;
        /// <summary>Window shadow color</summary>
        public uint Shadow;
        /// <summary>Corner radius</summary>
        public double Radius;
        /// <summary>Space inside windows</summary>
        public double Pad;
        /// <summary>Space between widgets</summary>
        public double Gap;
        /// <summary>Outline width</summary>
        public double BorderWidth;
        /// <summary>Text size, checkboxes and knobs grow with it</summary>
        public double FontSize;
        /// <summary>How soft shadows are</summary>
        public double ShadowBlur;
        /// <summary>How long hovers and toggles take, 0 turns animation off</summary>
        public double AnimMs;
        /// <summary>Font pointer, 0 = built in</summary>
        public IntPtr Font;
        /// <summary>Skin for buttons</summary>
        public RawSkin SkinButton;
        /// <summary>Skin for windows</summary>
        public RawSkin SkinPanel;
        /// <summary>Skin for inputs</summary>
        public RawSkin SkinInput;
        /// <summary>Icon bitmaps</summary>
        public RawIcons Icon;
        /// <summary>Icon color, 0 draws them as they are</summary>
        public uint IconTint;
        /// <summary>Skin for checkboxes</summary>
        public RawSkin SkinCheckbox;
        /// <summary>Skin for radio buttons</summary>
        public RawSkin SkinRadio;
        /// <summary>Skin for toggles</summary>
        public RawSkin SkinToggle;
        /// <summary>Skin for slider tracks</summary>
        public RawSkin SkinSlider;
        /// <summary>Skin for the filled part of sliders</summary>
        public RawSkin SkinSliderFill;
        /// <summary>Skin for scrollbars</summary>
        public RawSkin SkinScrollbar;
        /// <summary>Skin for tabs</summary>
        public RawSkin SkinTab;
        /// <summary>Skin for title bars</summary>
        public RawSkin SkinTitlebar;
        /// <summary>Skin for tooltips</summary>
        public RawSkin SkinTooltip;
        /// <summary>Skin for menus</summary>
        public RawSkin SkinMenu;
        /// <summary>Skin for popups</summary>
        public RawSkin SkinPopup;
        /// <summary>Focus ring and the input glow, 0 uses the accent</summary>
        public uint Focus;
        /// <summary>Selected text color, 0 uses the accent faded</summary>
        public uint Select;
        /// <summary>Text caret color, 0 uses the accent</summary>
        public uint Caret;
        /// <summary>Link color, 0 uses the accent</summary>
        public uint Link;
        /// <summary>Badge color when none is passed, 0 uses the accent</summary>
        public uint Badge;
        /// <summary>Toast background, 0 mixes panel and hover</summary>
        public uint Toast;
        /// <summary>Tooltip background, 0 mixes bg and hover</summary>
        public uint Tooltip;
        /// <summary>The red close buttons, 0 uses the built in red</summary>
        public uint Danger;
        /// <summary>Slider and toggle knobs, 0 = white</summary>
        public uint Knob;
        /// <summary>Color behind modals, 0 = 0x73000000</summary>
        public uint Dim;
        /// <summary>Checkbox and radio box size</summary>
        public double CheckSize;
        /// <summary>Checkbox corner radius</summary>
        public double CheckRadius;
        /// <summary>Toggle track width</summary>
        public double ToggleW;
        /// <summary>Toggle track height</summary>
        public double ToggleH;
        /// <summary>Slider height</summary>
        public double SliderH;
        /// <summary>Slider track thickness</summary>
        public double SliderTrack;
        /// <summary>Slider knob radius</summary>
        public double KnobSize;
        /// <summary>Scrollbar width</summary>
        public double ScrollW;
        /// <summary>Shortest a scrollbar thumb gets</summary>
        public double ScrollMin;
        /// <summary>Height of every widget row, 0 = line height plus pad</summary>
        public double ItemH;
        /// <summary>Height of menu rows, 0 comes from the font</summary>
        public double MenuItemH;
        /// <summary>Tab height, 0 comes from the font</summary>
        public double TabH;
        /// <summary>Window title bar height, 0 comes from the font</summary>
        public double WinTitleH;
        /// <summary>App mode title bar height, 0 comes from the font</summary>
        public double AppTitleH;
        /// <summary>App menu bar height, 0 comes from the font</summary>
        public double MenubarH;
        /// <summary>App status bar height, 0 comes from the font</summary>
        public double StatusbarH;
        /// <summary>Focus ring width</summary>
        public double FocusW;
        /// <summary>Space between the widget and its focus ring</summary>
        public double FocusGap;
        /// <summary>Separator thickness</summary>
        public double SepW;
        /// <summary>Window resize grip size</summary>
        public double Grip;
        /// <summary>Tree indent</summary>
        public double Indent;
        /// <summary>Plot line width</summary>
        public double PlotW;
        /// <summary>Spinner size when Ui.Spinner gets 0, 0 = line height</summary>
        public double Spinner;
        /// <summary>Caret width, 0 = thin. Go wide for a block caret</summary>
        public double CaretW;
        /// <summary>Smallest width a window resizes to</summary>
        public double WinMinW;
        /// <summary>Smallest height a window resizes to</summary>
        public double WinMinH;
        /// <summary>Space between a combo or popup and what opened it</summary>
        public double PopGap;
        /// <summary>Offset of submenus</summary>
        public double MenuGap;
        /// <summary>Horizontal padding in tooltips</summary>
        public double TipPadX;
        /// <summary>Vertical padding in tooltips</summary>
        public double TipPadY;
        /// <summary>Tooltip x spot from the mouse</summary>
        public double TipX;
        /// <summary>Tooltip y spot from the mouse</summary>
        public double TipY;
        /// <summary>Tooltip corner radius</summary>
        public double TipRadius;
        /// <summary>Shadow x offset</summary>
        public double ShadowX;
        /// <summary>Shadow y offset</summary>
        public double ShadowY;
        /// <summary>How button and label text is lined up, Auto is what each widget normally does</summary>
        public double Align;
        /// <summary>Size of Ui.Heading text, 0 = 1.3 times the font size</summary>
        public double HeadingSize;
        /// <summary>Size of badges and the status bar text, 0 = a bit under the font size</summary>
        public double SmallSize;
        /// <summary>The curve for the ui animations</summary>
        public double Ease;
        /// <summary>Hover fade time, below 0 follows AnimMs</summary>
        public double HoverMs;
        /// <summary>Press fade time, below 0 follows AnimMs</summary>
        public double PressMs;
        /// <summary>Time popups and menus take to open, below 0 follows AnimMs</summary>
        public double PopupMs;
        /// <summary>Time anything that folds open or shut takes, below 0 follows AnimMs</summary>
        public double CollapseMs;
        /// <summary>Time toasts take to fade out, below 0 follows AnimMs</summary>
        public double ToastFadeMs;
        /// <summary>Heading font pointer, 0 = Font</summary>
        public IntPtr FontHeading;
        /// <summary>Mono font pointer, 0 = built in mono</summary>
        public IntPtr FontMono;
        /// <summary>Small font pointer, 0 = Font</summary>
        public IntPtr FontSmall;
    }

    /// <summary>Raw c struct goome_combo, a key and its mods</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawCombo
    {
        /// <summary>Key code</summary>
        public int Key;
        /// <summary>Mods bits</summary>
        public int Mods;
    }

    /// <summary>Raw c struct goome_ui_config, UiConfig is the one to use</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawUiConfig
    {
        /// <summary>How many key actions there are</summary>
        public const int Actions = 12, Cursors = 12, PerAction = 4;
        /// <summary>Hover time before a tooltip shows</summary>
        public int TipDelayMs;
        /// <summary>Longest time between the clicks of a double click</summary>
        public int DblClickMs;
        /// <summary>How far the mouse can move between them</summary>
        public int DblClickPx;
        /// <summary>How far before a press turns into a drag</summary>
        public int DragPx;
        /// <summary>Hold time before repeating starts</summary>
        public int RepeatDelayMs;
        /// <summary>Time between repeats</summary>
        public int RepeatMs;
        /// <summary>Caret blink time, 0 = no blink</summary>
        public int BlinkMs;
        /// <summary>Hover time before a submenu opens</summary>
        public int MenuDelayMs;
        /// <summary>How long a toast stays by default</summary>
        public int ToastMs;
        /// <summary>Lines per wheel step</summary>
        public double ScrollLines;
        /// <summary>Clicking a window raises it</summary>
        public int RaiseOnClick;
        /// <summary>Esc closes the top popup or modal</summary>
        public int EscCloses;
        /// <summary>Tab moves focus</summary>
        public int TabNav;
        /// <summary>Clicking focuses a widget</summary>
        public int FocusOnMouse;
        /// <summary>Cursor shape per CursorKind</summary>
        public fixed int Cursor[Cursors];
        /// <summary>Key map, key and mods per combo, PerAction combos per action</summary>
        public fixed int Keys[Actions * PerAction * 2];
        /// <summary>Function pointer for Ease.Custom</summary>
        public IntPtr Ease;
        /// <summary>Passed to the ease function</summary>
        public IntPtr EaseUser;
        /// <summary>Password mask, utf-8</summary>
        public IntPtr Mask;
    }

    /// <summary>The icon pointers of goome_theme, indexed by Icon</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawIcons
    {
        /// <summary>Number of icons</summary>
        public const int Count = 22;
        IntPtr p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16, p17, p18, p19, p20, p21;

        /// <summary>The bitmap pointer for an icon</summary>
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

    /// <summary>Raw c struct goome_skin, Skin is the one to use</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawSkin
    {
        /// <summary>The bitmap</summary>
        public RawBitmap* Bm;
        /// <summary>Left border in bitmap px</summary>
        public double L;
        /// <summary>Top border in bitmap px</summary>
        public double T;
        /// <summary>Right border in bitmap px</summary>
        public double R;
        /// <summary>Bottom border in bitmap px</summary>
        public double B;
    }

    /// <summary>A nine slice bitmap for a widget, the borders stay the same size when it stretches</summary>
    public sealed class Skin
    {
        /// <summary>Makes a skin</summary>
        /// <param name="left">Left border in bitmap px</param>
        /// <param name="top">Top border in bitmap px</param>
        /// <param name="right">Right border in bitmap px</param>
        /// <param name="bottom">Bottom border in bitmap px</param>
        public Skin(Bitmap bitmap, double left, double top, double right, double bottom)
        {
            Bitmap = bitmap;
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>Makes a skin</summary>
        /// <param name="border">All four borders in bitmap px</param>
        public Skin(Bitmap bitmap, double border) : this(bitmap, border, border, border, border)
        {
        }

        /// <summary>The picture</summary>
        public Bitmap Bitmap { get; }
        /// <summary>Left border in bitmap px</summary>
        public double Left { get; }
        /// <summary>Top border in bitmap px</summary>
        public double Top { get; }
        /// <summary>Right border in bitmap px</summary>
        public double Right { get; }
        /// <summary>Bottom border in bitmap px</summary>
        public double Bottom { get; }
    }

    /// <summary>One monitor</summary>
    public readonly struct Monitor
    {
        /// <summary>Monitor id, the one you pass to Begin and Capture</summary>
        public int Id { get; }
        /// <summary>Name from the os</summary>
        public string Name { get; }
        /// <summary>Where it sits on the desktop, logical coords</summary>
        public Rect Area { get; }
        /// <summary>Real px per logical px</summary>
        public double Scale { get; }
        /// <summary>Width in real px</summary>
        public int PixelWidth { get; }
        /// <summary>Height in real px</summary>
        public int PixelHeight { get; }
        /// <summary>0, 90, 180 or 270, plus 360 if mirrored. Grabs and canvases already undo it</summary>
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

        /// <summary>Name, size, position and scale</summary>
        public override string ToString() => $"{Name} {Area.W}x{Area.H} at {Area.X},{Area.Y} scale {Scale:0.00}";
    }

    /// <summary>An input event from Poll. You can also make your own and Push them</summary>
    public struct Event
    {
        internal RawEvent raw;
        internal string? full;

        /// <summary>Makes an event. The one that takes a RawEvent wraps a c event</summary>
        public Event(EventType type)
        {
            raw = default;
            full = null;
            raw.Type = (int)type;
        }

        /// <summary>Makes an event. The one that takes a RawEvent wraps a c event</summary>
        public Event(EventType type, double x, double y)
        {
            raw = default;
            full = null;
            raw.Type = (int)type;
            raw.X = x;
            raw.Y = y;
        }

        /// <summary>Makes an event. The one that takes a RawEvent wraps a c event</summary>
        public Event(in RawEvent r)
        {
            raw = r;
            full = null;
        }

        /// <summary>The raw c struct</summary>
        public RawEvent Raw => raw;
        /// <summary>What kind of event it is</summary>
        public EventType Type => (EventType)raw.Type;
        /// <summary>Logical x</summary>
        public double X => raw.X;
        /// <summary>Logical y</summary>
        public double Y => raw.Y;
        /// <summary>Monitor id</summary>
        public int Monitor => raw.Mon;
        /// <summary>Mouse button, 1 left, 2 middle, 3 right</summary>
        public int Button => raw.Button;
        /// <summary>Scroll notches, positive goes down</summary>
        public int Dy => raw.Dy;
        /// <summary>Lowercase ascii, or a Keys value</summary>
        public int Key => raw.Key;
        /// <summary>Modifier keys held</summary>
        public Mods Mods => (Mods)raw.Mods;
        /// <summary>The hotkey id, or the caret byte offset for a Preedit</summary>
        public int Id => raw.Id;
        /// <summary>True if the window gained focus, false if it lost it. For Focus events</summary>
        public bool Focused => raw.Id != 0;

        /// <summary>The utf-8 the key typed, can be empty</summary>
        public unsafe string Text
        {
            get
            {
                fixed (byte* p = raw.Text) return p[0] == 0 ? "" : Native.Str(p) ?? "";
            }
        }

        /// <summary>The control line, the dropped path or the ime text. Long ones are cut at 63 bytes, Goome.EventText has all of it</summary>
        public unsafe string Message
        {
            get
            {
                if (full != null) return full;
                fixed (byte* p = raw.Msg) return p[0] == 0 ? "" : Native.Str(p) ?? "";
            }
        }

        /// <summary>True for a finished ime or compose Text event</summary>
        public bool IsText => Type == EventType.Text;
        /// <summary>True for a Preedit event</summary>
        public bool IsPreedit => Type == EventType.Preedit;
        /// <summary>Caret position in Message, in chars. Preedit events only</summary>
        public int Caret => Type == EventType.Preedit ? Utf8.CharIndex(Message, raw.Id) : 0;

        /// <summary>Makes a mouse press</summary>
        public static Event Down(double x, double y, int button = 1, Mods mods = Mods.None) => Mouse(EventType.Down, x, y, button, mods);
        /// <summary>Makes a mouse release</summary>
        public static Event Up(double x, double y, int button = 1, Mods mods = Mods.None) => Mouse(EventType.Up, x, y, button, mods);
        /// <summary>Makes a mouse move</summary>
        public static Event Motion(double x, double y) => new Event(EventType.Motion, x, y);

        /// <summary>Makes a wheel event</summary>
        /// <param name="dy">Notches, positive goes down</param>
        public static Event Scroll(double x, double y, int dy)
        {
            var e = new Event(EventType.Scroll, x, y);
            e.raw.Dy = dy;
            return e;
        }

        /// <summary>Makes a key press</summary>
        /// <param name="key">Lowercase ascii or a Keys value</param>
        /// <param name="text">What it typed</param>
        public static Event KeyPress(int key, Mods mods = Mods.None, string? text = null)
        {
            var e = new Event(EventType.Key);
            e.raw.Key = key;
            e.raw.Mods = (int)mods;
            if (text != null) e.SetText(text);
            return e;
        }

        /// <summary>Makes the key press for a character, with the text when it types one</summary>
        /// <param name="codepoint">A unicode code point</param>
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

        /// <summary>Makes a focus event</summary>
        /// <param name="gained">True for gained, false for lost</param>
        public static Event Focus(bool gained)
        {
            var e = new Event(EventType.Focus);
            e.raw.Id = gained ? 1 : 0;
            return e;
        }

        /// <summary>Makes a close event</summary>
        public static Event Close() => new Event(EventType.Close);

        /// <summary>Makes a control line event</summary>
        public static Event Control(string message)
        {
            var e = new Event(EventType.Control);
            e.SetMessage(message);
            return e;
        }

        /// <summary>Makes a finished ime text event</summary>
        public static Event Commit(string text)
        {
            var e = new Event(EventType.Text);
            e.SetMessage(text);
            return e;
        }

        /// <summary>Makes an ime composing event</summary>
        /// <param name="caret">Caret in chars, -1 = the end</param>
        public static Event Preedit(string text, int caret = -1)
        {
            var e = new Event(EventType.Preedit);
            e.SetMessage(text);
            string kept = e.Message;
            e.raw.Id = Utf8.ByteIndex(kept, caret < 0 ? kept.Length : Math.Min(caret, kept.Length));
            return e;
        }

        /// <summary>Makes the control event for an accessibility action</summary>
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

        /// <summary>A short description</summary>
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

    /// <summary>Everything Goome can be opened with</summary>
    public sealed class GoomeOptions
    {
        /// <summary>Window title, null = "goome"</summary>
        public string? Title { get; set; }
        /// <summary>Force a backend or leave Auto. The GOOME_BACKEND environment variable wins</summary>
        public Backend Backend { get; set; }
        /// <summary>Where the portal token and control fifo live, null = neither</summary>
        public string? StateDir { get; set; }
        /// <summary>Overlay only, never asks for screen access</summary>
        public bool NoCapture { get; set; }
        /// <summary>Frame rate of gnome and portal streams, 0 = 5</summary>
        public int CaptureFps { get; set; }
        /// <summary>Keeps the overlay out of grabs where the os lets us</summary>
        public bool HideSelf { get; set; }
        /// <summary>Test backend only, see the GOOME_TEST_FRAMES variable</summary>
        public string? TestFrames { get; set; }
        /// <summary>Test backend only, see the GOOME_TEST_SCRIPT variable</summary>
        public string? TestScript { get; set; }
        /// <summary>Test backend only, see the GOOME_TEST_SHOTS variable</summary>
        public string? TestShots { get; set; }
        /// <summary>Overlay, window or offscreen</summary>
        public Mode Mode { get; set; }
        /// <summary>Window or offscreen width, logical. 0 = 960</summary>
        public int Width { get; set; }
        /// <summary>Window or offscreen height, logical. 0 = 640</summary>
        public int Height { get; set; }
        /// <summary>Smallest window width, window only, 0 = none</summary>
        public int MinWidth { get; set; }
        /// <summary>Smallest window height, window only, 0 = none</summary>
        public int MinHeight { get; set; }
        /// <summary>Window options</summary>
        public WindowStyle WindowStyle { get; set; }
        /// <summary>Offscreen only, real px per logical px, 0 = 1</summary>
        public double Scale { get; set; }
    }
}
