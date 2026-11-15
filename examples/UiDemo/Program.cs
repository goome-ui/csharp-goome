using System;
using System.IO;
using GoomeSharp;

Goome g;
try
{
    g = new Goome(new GoomeOptions { Title = "goome ui", NoCapture = true });
}
catch (GoomeException e)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}
using (g)
using (var u = new Ui(g))
{
    var mons = g.Monitors;
    var area = mons.Length > 0 ? mons[0].Area : new Rect(0, 0, 1280, 720);

    bool haveLogo = false;
    Bitmap? logo = null;
    foreach (var p in new[] { "docs/logo.png", "../docs/logo.png", Path.Combine(AppContext.BaseDirectory, "logo.png") })
    {
        if (!File.Exists(p)) continue;
        try
        {
            logo = Bitmap.Load(p);
            haveLogo = true;
            break;
        }
        catch (GoomeException)
        {
        }
    }
    logo ??= MakeGradient();

    double wx = area.X + 40, wy = area.Y + 40, ww = 440, wh = 620;
    double sx = area.X + 500, sy = area.Y + 40, sw = 280, sh = 0;
    bool statsOpen = true, grid = true, wifi = true, notify = false, more = true, light = false, ownAccent = false, solid = false, always = false, running = true;
    int tab = 0, clicks = 0, mode = 1, count = 42, fruit = 2, picked = -1, imgClicks = 0;
    bool test = g.BackendName == "test";
    double volume = 0.65, radius = 10, anim = 120;
    uint highlight = 0xFF4FA3FF, accent = 0xFF6FDC5F;
    string name = "goome", search = "";
    string[] fruits = { "Mango", "Banana", "Cherry", "Kiwi", "Apple", "Lemon", "Grape", "Fig", "Dragonfruit", "Pear" };
    string[] tabs = { "Widgets", "Images", "Theme", "About" };
    string[] items = new string[30];
    for (int i = 0; i < 30; i++) items[i] = $"Item {i + 1}";

    long spent = 0, tframes = 0;
    double fps = 0, frameMs = 0, accMs = 0;
    int accN = 0, fpsN = 0;
    long fpsT = Goome.NowMs;
    var pace = new Pacer();
    bool dirty = true;
    while (running)
    {
        int wait = dirty || always || test || u.Animating ? pace.Pace(60) : 500;
        while (g.Poll(out var ev, wait))
        {
            if (ev.Type == EventType.Quit) running = false;
            u.Feed(ev);
            dirty = true;
            wait = 0;
        }
        if (!running) break;
        if (!dirty && !always && !test && !u.Animating) continue;
        dirty = false;

        long t0 = Goome.NowMs;
        var c = u.Begin(0);
        if (c == null) continue;
        if (u.Window("goome ui", ref wx, ref wy, ref ww, ref wh, WindowFlags.Move | WindowFlags.Resize | WindowFlags.Collapse))
        {
            u.Tabs("tabs", tabs, ref tab);
            u.PushId(tabs[tab]);
            u.ScrollBegin("page");
            if (tab == 0)
            {
                u.Heading("Buttons");
                u.Row(3);
                if (u.Button("Click me")) clicks++;
                if (u.Button("Reset")) clicks = 0;
                u.Button("Hover me");
                u.Tooltip("hi :)");
                u.Label(clicks == 0 ? "not clicked yet" : clicks == 1 ? "clicked once" : $"clicked {clicks} times");
                u.Separator();
                u.Heading("Switches");
                u.Checkbox("Show grid", ref grid);
                u.SameLine();
                u.Checkbox("Notifications", ref notify);
                u.Toggle("Wi-Fi", ref wifi);
                u.Radio("Small", ref mode, 0);
                u.SameLine();
                u.Radio("Medium", ref mode, 1);
                u.SameLine();
                u.Radio("Large", ref mode, 2);
                u.Separator();
                u.Heading("Values");
                u.Slider("Volume", ref volume, 0, 1, "%.2f");
                u.SliderInt("Count", ref count, 0, 100);
                u.Progress(volume, $"{(int)(volume * 100 + 0.5)}%");
                u.Progress(-1);
                u.Row(2);
                u.Input("Name", ref name, 64);
                u.Input("Search", ref search, 64);
                u.Combo("Fruit", ref fruit, fruits);
                u.Color("Highlight", ref highlight);
                if (u.Collapsing("More stuff", ref more))
                {
                    u.Wrapped("a long list so theres something to scroll. pick one, it shows up under it.");
                    u.ScrollBegin("list", 150);
                    for (int i = 0; i < 30; i++)
                        if (u.Selectable(items[i], picked == i)) picked = i;
                    u.ScrollEnd();
                    u.Label(picked >= 0 ? $"picked item {picked + 1}" : "nothing picked");
                }
            }
            else if (tab == 1)
            {
                u.Heading(haveLogo ? "docs/logo.png" : "Generated gradient");
                u.Image(logo, 120, 120, 18);
                u.SameLine();
                u.Image(logo, 120, 120, 60);
                u.SameLine();
                u.Image(logo, 120, 120, 0);
                u.Label("image buttons");
                for (int i = 0; i < 4; i++)
                {
                    if (i > 0) u.SameLine();
                    if (u.ImageButton($"img{i}", logo, 56, 56)) imgClicks++;
                }
                u.Label(imgClicks == 0 ? "no image clicks yet" : imgClicks == 1 ? "one image click" : $"{imgClicks} image clicks");
                u.Image(logo, 0, 90, 10);
            }
            else if (tab == 2)
            {
                u.Heading("Theme");
                int lv = light ? 1 : 0, was = lv;
                u.Radio("Dark", ref lv, 0);
                u.SameLine();
                u.Radio("Light", ref lv, 1);
                light = lv == 1;
                bool ch = lv != was;
                if (u.Color("Accent", ref accent)) ownAccent = ch = true;
                ch |= u.Slider("Corner radius", ref radius, 0, 20, "%.0f px");
                ch |= u.Slider("Animation", ref anim, 0, 400, "%.0f ms");
                ch |= u.Toggle("Solid panels", ref solid);
                if (u.Button("Reset theme"))
                {
                    light = ownAccent = solid = false;
                    radius = 10;
                    anim = 120;
                    accent = 0xFF6FDC5F;
                    ch = true;
                }
                if (ch) SetTheme(u.Theme, light, accent, ownAccent, radius, anim, solid);
                if (!ownAccent) accent = u.Theme.Accent;
            }
            else
            {
                u.Heading("About");
                u.Wrapped("made with goome. try clicking next to this window, your desktop still gets it.");
                u.Label($"goome {Goome.Version} on {g.BackendName}");
                u.Label($"{fps:0} fps, {frameMs:0.00} ms a frame");
                if (!statsOpen && u.Button("Show stats")) statsOpen = true;
            }
            u.ScrollEnd();
            u.PopId();
        }
        u.WindowEnd();

        if (u.Window("Stats", ref sx, ref sy, ref sw, ref sh, WindowFlags.Move | WindowFlags.Collapse | WindowFlags.Close | WindowFlags.Resize, ref statsOpen))
        {
            u.Label($"{fps:0} fps");
            u.Label($"{frameMs:0.00} ms a frame");
            u.Progress(frameMs / 16.7, $"{(int)(frameMs * 100 / 16.7 + 0.5)}% of a 60 fps frame");
            u.Toggle("Redraw every frame", ref always);
            if (u.Button("Quit")) running = false;
        }
        u.WindowEnd();
        u.End();

        long t1 = Goome.NowMs;
        spent += t1 - t0;
        tframes++;
        accMs += t1 - t0;
        accN++;
        fpsN++;
        if (t1 - fpsT >= 500)
        {
            fps = fpsN * 1000.0 / (t1 - fpsT);
            frameMs = accMs / accN;
            fpsT = t1;
            fpsN = accN = 0;
            accMs = 0;
        }
    }
    if (tframes > 0) Console.WriteLine($"{tframes} frames, {(double)spent / tframes:0.00} ms a frame");
    logo.Dispose();
}
return 0;

static Bitmap MakeGradient()
{
    var px = new uint[128 * 128];
    for (int y = 0; y < 128; y++)
        for (int x = 0; x < 128; x++)
        {
            int r = 0x2a + x * (0x6f - 0x2a) / 127, gr = 0x40 + y * (0xdc - 0x40) / 127, bl = 0x9a - (x + y) * 0x50 / 254;
            px[y * 128 + x] = 0xFF000000u | (uint)r << 16 | (uint)gr << 8 | (uint)bl;
        }
    return Bitmap.FromArgb(px, 128, 128);
}

static void SetTheme(Theme t, bool light, uint accent, bool ownAccent, double radius, double anim, bool solid)
{
    if (light) t.Light();
    else t.Dark();
    if (ownAccent) t.Accent = accent;
    t.Radius = radius;
    t.AnimMs = anim;
    if (solid) t.Panel |= 0xFF000000u;
}
