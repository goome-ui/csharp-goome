using System;
using System.Globalization;
using System.Text;
using GoomeSharp;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

const int Retro = 0, Soft = 1, Hud = 2, Dense = 3, Looks = 4;

string[] tabs = { "Play", "Stats", "Gear" };
string[] modes = { "Story", "Arena", "Sandbox" };
string[] starts = { "theme_make", "Dark", "Light", "Midnight", "Paper", "Mint", "High contrast", "Rose" };
string[] code = { "var t = Theme.Make(0xFF8B5CF6, true, 12, u.Theme);", "u.PushTheme(t);", "u.Scale(1.25);" };
string[] names = { "guest_01", "Mia", "NOVA-7", "render_pass_b" };

Bitmap Sprite(int w, int h, Action<Canvas> draw)
{
    using var c = Canvas.Create(w, h);
    draw(c);
    return Bitmap.Wrap(c.Pixels.ToArray(), w, h);
}

void Diamond(Canvas c, double cx, double cy, double r, uint col) => c.DrawPoly(new[] { cx, cy - r, cx + r, cy, cx, cy + r, cx - r, cy }, col);

Bitmap[] term =
{
    Sprite(12, 12, c => c.DrawRect(2, 2, 8, 8, 0xFFFFFFFF)),
    Sprite(10, 10, c => c.DrawRect(2, 2, 6, 6, 0xFFFFFFFF)),
    Sprite(12, 12, c => c.DrawRect(0, 0, 12, 12, 0xFFFFFFFF)),
    Sprite(12, 12, c =>
    {
        c.DrawRect(1, 3, 10, 2, 0xFFFFFFFF);
        c.DrawRect(3, 5, 6, 2, 0xFFFFFFFF);
        c.DrawRect(5, 7, 2, 2, 0xFFFFFFFF);
    }),
    Sprite(12, 12, c =>
    {
        c.DrawRect(3, 1, 2, 10, 0xFFFFFFFF);
        c.DrawRect(5, 3, 2, 6, 0xFFFFFFFF);
        c.DrawRect(7, 5, 2, 2, 0xFFFFFFFF);
    }),
    Sprite(12, 12, c =>
    {
        c.DrawRect(0, 0, 12, 12, 0xFFFFFFFF);
        c.DrawRect(3, 3, 6, 6, 0xFF000000);
    }),
    Sprite(12, 12, c =>
    {
        c.DrawRect(0, 0, 12, 12, 0xFF1FA64C);
        c.DrawRect(1, 1, 10, 10, 0xFF031408);
    }),
};

Bitmap[] hud =
{
    Sprite(16, 16, c => Diamond(c, 8, 8, 6, 0xFFFFFFFF)),
    Sprite(16, 16, c => Diamond(c, 8, 8, 5, 0xFFFFFFFF)),
    Sprite(20, 20, c =>
    {
        Diamond(c, 10, 10, 9, 0xFFFFFFFF);
        Diamond(c, 10, 10, 5.5, 0xFF0A1A24);
    }),
    Sprite(12, 12, c => c.DrawTriangle(1, 3, 11, 3, 6, 10, 0xFFFFFFFF)),
    Sprite(12, 12, c => c.DrawTriangle(3, 1, 10, 6, 3, 11, 0xFFFFFFFF)),
    Sprite(12, 12, c =>
    {
        c.DrawLine(2, 2, 10, 10, 1.6, 0xFFFFFFFF);
        c.DrawLine(10, 2, 2, 10, 1.6, 0xFFFFFFFF);
    }),
    Sprite(18, 18, c =>
    {
        c.DrawRect(0, 0, 18, 18, 0xC0081C28);
        c.DrawFrame(0.5, 0.5, 17, 17, 1.2, 0xFF39E1FF);
    }),
    Sprite(18, 18, c =>
    {
        Diamond(c, 9, 9, 8.6, 0xFF39E1FF);
        Diamond(c, 9, 9, 7.2, 0xF0081C28);
    }),
};

var frame = Sprite(24, 24, c =>
{
    c.DrawRect(0, 0, 24, 24, 0xA00A1622);
    c.DrawFrame(0.5, 0.5, 23, 23, 1, 0x5539E1FF);
    c.DrawRect(0, 0, 8, 2, 0xFF39E1FF);
    c.DrawRect(0, 0, 2, 8, 0xFF39E1FF);
    c.DrawRect(16, 22, 8, 2, 0xFF39E1FF);
    c.DrawRect(22, 16, 2, 8, 0xFF39E1FF);
});
var plate = Sprite(24, 24, c =>
{
    c.DrawPoly(new double[] { 6, 0, 24, 0, 24, 18, 18, 24, 0, 24, 0, 6 }, 0xC0103346);
    c.DrawPoly(new[] { 6.8, 1, 23, 1, 23, 17.6, 17.6, 23, 1, 23, 1, 6.8 }, 0xFF0D2533);
    c.DrawLine(6, 0.5, 23.5, 0.5, 1, 0xFF39E1FF);
    c.DrawLine(0.5, 6, 0.5, 23.5, 1, 0x9939E1FF);
    c.DrawLine(0.5, 6, 6, 0.5, 1, 0xFF39E1FF);
});
var tabArt = Sprite(24, 24, c => c.DrawPoly(new double[] { 5, 0, 24, 0, 24, 19, 19, 24, 0, 24, 0, 5 }, 0xFF39E1FF));

Goome g;
try
{
    g = new Goome(new GoomeOptions
    {
        Title = "goome can look like anything", Mode = Mode.Window, Width = 1320, Height = 940, MinWidth = 900, MinHeight = 600,
        WindowStyle = WindowStyle.Resizable | WindowStyle.Center,
    });
}
catch (GoomeException e)
{
    Console.Error.WriteLine("cant open a window: " + e.Message);
    return 1;
}

using (g)
using (var u = new Ui(g))
{
    var mono = Font.System("monospace");
    var bold = Font.System("sans-serif:bold");

    Theme RetroLook()
    {
        var t = new Theme();
        t.Bg = 0xFF000000; t.Panel = 0xFF020A04; t.Border = 0xFF1F7A3A; t.Text = 0xFF3DFF7A; t.TextDim = 0xFF1FA64C;
        t.Accent = 0xFF3DFF7A; t.AccentText = 0xFF001A08; t.Hover = 0xFF0A2A14; t.Active = 0xFF124422; t.InputBg = 0xFF031408;
        t.Shadow = 0x5533FF77; t.ShadowBlur = 18; t.ShadowY = 0;
        t.Radius = 0; t.CheckRadius = 0; t.TipRadius = 0; t.BorderWidth = 1; t.AnimMs = 0; t.Ease = Ease.Linear;
        t.Font = mono!; t.FontMono = mono; t.FontHeading = mono; t.FontSize = 15; t.HeadingSize = 17;
        t.CaretW = 9; t.Caret = 0xB03DFF7A; t.Select = 0x663DFF7A; t.Knob = 0xFF3DFF7A; t.FocusW = 1; t.FocusGap = 2;
        t.ToggleW = 34; t.ToggleH = 18; t.SliderTrack = 2; t.KnobSize = 6;
        t.Icons[Icon.Check] = term[0];
        t.Icons[Icon.RadioDot] = term[1];
        t.Icons[Icon.SliderKnob] = term[2];
        t.Icons[Icon.ToggleKnob] = term[5];
        t.Icons[Icon.ArrowDown] = term[3];
        t.Icons[Icon.ArrowRight] = term[4];
        var box = new Skin(term[6], 3);
        t.CheckboxSkin = t.RadioSkin = t.ToggleSkin = box;
        return t;
    }

    Theme SoftLook()
    {
        var t = Theme.Make(0xFF2F7BF6, false, 20);
        t.Bg = 0xFFF2F4F8; t.Panel = 0xFFFFFFFF; t.Border = 0x14000000; t.InputBg = 0xFFEEF1F6; t.Hover = 0xFFE3E8F0;
        t.FontSize = 17; t.HeadingSize = 22; t.Pad = 14; t.Gap = 10; t.ItemH = 44; t.TabH = 42; t.WinTitleH = 48;
        t.CheckSize = 22; t.CheckRadius = 11; t.ToggleW = 46; t.ToggleH = 28; t.SliderH = 28; t.KnobSize = 12; t.SliderTrack = 8;
        t.Shadow = 0x22203060; t.ShadowBlur = 34; t.ShadowY = 10; t.FocusW = 3; t.FocusGap = 4;
        t.AnimMs = 260; t.HoverMs = 200; t.PressMs = 160; t.PopupMs = 320; t.Ease = Ease.Back;
        return t;
    }

    Theme HudLook()
    {
        var t = new Theme();
        t.Bg = 0x00000000; t.Panel = 0x8A06121C; t.Border = 0x6639E1FF; t.Text = 0xFFE6FBFF; t.TextDim = 0xFF7FC4D6;
        t.Accent = 0xFF39E1FF; t.AccentText = 0xFF041820; t.Hover = 0xFF12384A; t.Active = 0xFF1A4C63; t.InputBg = 0xB0081C28;
        t.Shadow = 0x5539E1FF; t.ShadowBlur = 26; t.ShadowY = 0; t.Radius = 0; t.CheckRadius = 0; t.TipRadius = 0;
        if (bold != null) t.Font = bold;
        t.FontSize = 16; t.HeadingSize = 22; t.ItemH = 38; t.WinTitleH = 42;
        t.Knob = 0xFF39E1FF; t.KnobSize = 9; t.SliderTrack = 4; t.CheckSize = 18; t.Ease = Ease.Out; t.AnimMs = 160;
        t.PanelSkin = new Skin(frame, 8);
        t.ButtonSkin = new Skin(plate, 8);
        t.InputSkin = new Skin(plate, 8);
        t.TabSkin = new Skin(tabArt, 7);
        t.CheckboxSkin = new Skin(hud[6], 3);
        t.RadioSkin = new Skin(hud[7], 0);
        t.Icons[Icon.Check] = hud[0];
        t.Icons[Icon.RadioDot] = hud[1];
        t.Icons[Icon.SliderKnob] = hud[2];
        t.Icons[Icon.ToggleKnob] = hud[2];
        t.Icons[Icon.ArrowDown] = hud[3];
        t.Icons[Icon.ArrowRight] = hud[4];
        t.Icons[Icon.Close] = hud[5];
        return t;
    }

    Theme DenseLook()
    {
        var t = new Theme();
        t.Bg = 0xFF1B1C1F; t.Panel = 0xFF232428; t.Border = 0xFF34363C; t.Text = 0xFFD5D7DC; t.TextDim = 0xFF8A8F98;
        t.Accent = 0xFF4C8DF6; t.AccentText = 0xFFFFFFFF; t.Hover = 0xFF2E3036; t.Active = 0xFF383B42; t.InputBg = 0xFF1A1B1E;
        t.Shadow = 0x66000000; t.ShadowBlur = 10; t.ShadowY = 3; t.Radius = 3; t.CheckRadius = 2; t.TipRadius = 3;
        t.FontSize = 12; t.HeadingSize = 13; t.Pad = 6; t.Gap = 6; t.ItemH = 24; t.TabH = 24; t.WinTitleH = 26;
        t.CheckSize = 13; t.ToggleW = 26; t.ToggleH = 14; t.SliderH = 14; t.KnobSize = 5; t.SliderTrack = 3;
        t.ScrollW = 3; t.FocusW = 1; t.FocusGap = 1; t.AnimMs = 70; t.TipPadX = 6; t.TipPadY = 4;
        return t;
    }

    var look = new[] { RetroLook(), SoftLook(), HudLook(), DenseLook() };
    foreach (var t in look)
        if (t.Raw.Font == IntPtr.Zero) t.Font = u.Theme.Font;

    var cards = new Card[Looks + 1];
    for (int i = 0; i <= Looks; i++)
    {
        cards[i] = new Card { Sound = true, Online = i != Dense, Level = 1 + (i & 1), Volume = 35 + i * 12, Mode = i % 3, Tab = i % 3 };
        Encoding.UTF8.GetBytes(i < Looks ? names[i] : "sev").CopyTo(cards[i].Name, 0);
    }
    var presets = new Theme[starts.Length - 1];
    for (int i = 0; i < presets.Length; i++) presets[i] = Theme.Preset((Preset)i, u.Theme);
    int start = 0, made = -1;
    Theme simpleTheme = u.Theme;
    string pass = "hunter22";
    var room = new byte[8];
    var server = new byte[16];
    Encoding.UTF8.GetBytes("eu-west-2").CopyTo(server, 0);
    double zoom = 1;
    bool focusOnce = true;

    u.Config.TipDelayMs = 300;
    u.Config[CursorKind.Button] = CursorShape.Hand;
    u.Config.Bind(KeyAction.SelectAll, "ctrl+a super+a ctrl+l");

    void Widgets(Card c, bool big, bool focus)
    {
        u.Tabs("tabs", tabs, ref c.Tab);
        u.Heading("Player one");
        if (focus) u.SetFocus();
        u.Input("Name", c.Name);
        u.Slider("Volume", ref c.Volume, 0, 100, "%.0f");
        u.Row(2);
        u.Checkbox("Sound", ref c.Sound);
        u.Toggle("Online", ref c.Online);
        u.Radio("Easy", ref c.Level, 0);
        u.SameLine();
        u.Radio("Normal", ref c.Level, 1);
        u.SameLine();
        u.Radio("Hard", ref c.Level, 2);
        if (!big)
        {
            u.Combo("Mode", ref c.Mode, modes);
            u.Progress(c.Volume / 100, "loading");
        }
        u.Row(2);
        u.Button("Start");
        u.Button("Quit");
    }

    void Scene(double x, double y, double w, double h)
    {
        var c = u.Canvas();
        if (c == null) return;
        c.DrawRRectGrad(x, y, w, h, Radii.All(14), 0xFF1B2747, 0xFFE0794F);
        c.DrawCircle(x + w * 0.72, y + h * 0.62, 46, 0xFFFFC27A);
        c.DrawPoly(new[] { x, y + h, x, y + h * 0.7, x + w * 0.22, y + h * 0.56, x + w * 0.45, y + h * 0.74, x + w * 0.7, y + h * 0.6, x + w, y + h * 0.72, x + w, y + h }, 0xFF1A1F38);
        c.DrawPoly(new[] { x, y + h, x, y + h * 0.86, x + w * 0.3, y + h * 0.8, x + w * 0.62, y + h * 0.9, x + w, y + h * 0.82, x + w, y + h }, 0xFF0D1022);
    }

    void Look(int k, string title, double x, double y, double w, double h)
    {
        u.PushTheme(look[k]);
        u.Scale(zoom);
        u.PushId(title);
        if (u.Window(title, ref x, ref y, ref w, ref h))
        {
            Widgets(cards[k], k == Soft || k == Hud, k == Retro && focusOnce);
            if (k == Retro) focusOnce = false;
        }
        u.WindowEnd();
        u.PopId();
        u.Pop();
    }

    void Simple(double x, double y, double w, double h)
    {
        if (made != start)
        {
            simpleTheme = start == 0 ? Theme.Make(0xFF8B5CF6, true, 12, u.Theme) : Theme.Preset((Preset)(start - 1), u.Theme);
            made = start;
        }
        u.PushTheme(simpleTheme);
        u.Scale(zoom);
        if (u.Window("The simple path", ref x, ref y, ref w, ref h))
        {
            u.Wrapped("three lines, one accent color, dark or light and a corner radius:");
            using (u.Style(font: mono, fontSize: 12))
                foreach (var line in code) u.Label(line);
            u.Space(4);
            u.Combo("Start from", ref start, starts);
            u.Slider("UI size", ref zoom, 0.8, 1.3, "%.2fx");
            u.Separator();
            var c = cards[Looks];
            u.Row(2);
            u.Input("Name", c.Name);
            if (u.Password("Password", ref pass, "8 letters or more", maxBytes: 32) == 2) u.Toast("logged in", 1500);
            u.Row(2);
            u.InputEx("Room code", room, InputFlags.Number, "numbers only");
            u.InputEx("Server", server, InputFlags.ReadOnly);
            u.Row(2);
            u.Checkbox("Sound", ref c.Sound);
            u.Toggle("Online", ref c.Online);
            u.Separator();
            u.Label("one push changes one widget");
            u.Row(3);
            using (u.Style(radius: 0)) u.Button("Square");
            using (u.Style(radius: 99, align: Align.Left)) u.Button("Pill");
            using (u.Style(accent: 0xFFF59E0B, checkSize: 24, checkRadius: 12)) u.Checkbox("Big", ref c.Sound);
            u.Separator();
            u.Label("every preset, same widgets");
            for (int i = 0; i < presets.Length; i++)
            {
                u.PushTheme(presets[i]);
                u.PushId(starts[i + 1]);
                u.Row(3);
                u.Badge(starts[i + 1], 0);
                u.Button("Open");
                bool on = (i & 1) != 0;
                u.Toggle("##t", ref on);
                u.PopId();
                u.Pop();
            }
        }
        u.WindowEnd();
        u.Pop();
    }

    bool running = true, test = g.BackendName.StartsWith("test");
    var pace = new Pacer();
    while (running)
    {
        int wait = test || u.Animating ? pace.Pace(60) : 500;
        while (g.Poll(out var ev, wait))
        {
            if (ev.Type == EventType.Quit || ev.Type == EventType.Close) running = false;
            u.Feed(ev);
            wait = 0;
        }
        if (!running) break;
        if (u.Begin(0) == null) continue;
        u.App("anything", AppFlags.NoPad);
        Scene(452, 496, 420, 428);
        u.AppEnd();
        Look(Retro, "> retro terminal_", 16, 16, 420, 0);
        Look(Soft, "Soft and round", 452, 16, 420, 0);
        Look(Hud, "GAME HUD", 476, 520, 372, 0);
        Look(Dense, "dense pro tool", 16, 456, 420, 0);
        Simple(888, 16, 416, 908);
        u.End();
    }
    mono?.Dispose();
    bold?.Dispose();
}
return 0;

sealed class Card
{
    public int Tab, Level, Mode;
    public bool Sound, Online;
    public double Volume;
    public byte[] Name = new byte[48];
}
