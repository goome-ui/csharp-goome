using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GoomeSharp;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string[] iconFiles =
{
    "arrow_down", "arrow_right", "arrow_up", "arrow_left", "check", "radio_dot", "close", "minimize", "maximize", "restore", "resize",
    "sort_up", "sort_down", "plus", "minus", "slider_knob", "toggle_knob", "collapse", "expand", "drag_handle", "toast", "picker",
};
(SkinKind Kind, string File, double L, double T, double R, double B)[] skinFiles =
{
    (SkinKind.Button, "button", 8, 8, 8, 8),
    (SkinKind.Input, "input", 8, 8, 8, 8),
    (SkinKind.Checkbox, "checkbox", 18, 18, 18, 18),
    (SkinKind.Radio, "radio", 18, 18, 18, 18),
    (SkinKind.Toggle, "toggle", 38, 22, 38, 22),
    (SkinKind.SliderTrack, "slider", 3, 3, 3, 3),
    (SkinKind.SliderFill, "slider_fill", 3, 3, 3, 3),
    (SkinKind.Scrollbar, "scrollbar", 3, 3, 3, 3),
    (SkinKind.Tab, "tab", 8, 8, 8, 8),
    (SkinKind.Titlebar, "titlebar", 12, 12, 12, 3),
    (SkinKind.Tooltip, "tooltip", 8, 8, 8, 8),
    (SkinKind.Menu, "menu", 10, 10, 10, 10),
    (SkinKind.Popup, "menu", 10, 10, 10, 10),
};
(string Name, string Kind, int Size)[] files =
{
    ("sketch.png", "image", 482), ("notes.txt", "text", 12), ("palette.gpl", "colors", 3), ("cover.jpg", "image", 1210),
    ("draft.psd", "layers", 8840), ("todo.md", "text", 4), ("brushes.zip", "archive", 2304),
};
string[] tabs = { "General", "Sound", "About" };
string[] brushes = { "Round", "Pencil", "Ink pen", "Marker", "Charcoal" };

string? dir = null;
foreach (var d in new[] { "examples/assets", "assets", "../examples/assets", "../../examples/assets" })
    if (File.Exists(Path.Combine(d, "arrow_down.png")))
    {
        dir = d;
        break;
    }
if (dir == null)
{
    Console.Error.WriteLine("cant find examples/assets, run it from the repo folder");
    return 1;
}

Bitmap? Load(string name)
{
    string path = Path.Combine(dir, name + ".png");
    try
    {
        return Bitmap.Load(path);
    }
    catch (GoomeException e)
    {
        Console.Error.WriteLine($"cant load {path}: {e.Message}");
        return null;
    }
}

Goome g;
try
{
    g = new Goome(new GoomeOptions
    {
        Title = "Skinned", Mode = Mode.Window, Width = 980, Height = 680, MinWidth = 760, MinHeight = 520,
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
    var t = u.Theme;
    t.Bg = 0xFFEFE8DD; t.Panel = 0xFFFAF6F0; t.Border = 0x2E2B1E35;
    t.Text = 0xFF2B2233; t.TextDim = 0xFF85798C;
    t.Accent = 0xFF7B4FD0; t.AccentText = 0xFFFFFFFF;
    t.Hover = 0xFFE7DEF0; t.Active = 0xFFDACDEA; t.InputBg = 0xFFEDE6DE; t.Shadow = 0x3A281838;
    t.Radius = 9; t.Pad = 10; t.Gap = 9; t.BorderWidth = 1; t.ShadowBlur = 24; t.AnimMs = 120;
    for (int i = 0; i < iconFiles.Length; i++) t.Icons[(Icon)i] = Load(iconFiles[i]);
    foreach (var s in skinFiles)
        if (Load(s.File) is Bitmap b) t.Skins[s.Kind] = new Skin(b, s.L, s.T, s.R, s.B);
    var heart = Load("heart");
    var appBar = Load("titlebar_app");
    var appSkin = appBar == null ? null : new Skin(appBar, 4, 4, 4, 3);

    int tab = 0, size = 1, brush = 0, sortCol = 2;
    bool snap = true, hints = false, fav = true, corners = true, autosave = false, grid = true, sortDesc = true, layers = true;
    bool bgOpen = true, sketchOpen = false, inspector = true, outline = false;
    double volume = 64, copies = 3, opacity = 0.8, frac = 0.5, wx = 610, wy = 300, ww = 330, wh = 268, zoom = 1;
    uint tint = 0xFFE0567A;
    var name = new byte[64];
    Encoding.UTF8.GetBytes("cozy cat sketch").CopyTo(name, 0);
    bool running = true, test = g.BackendName.StartsWith("test"), first = true;
    var pace = new Pacer();

    void Menus()
    {
        if (!u.MenuBarBegin()) return;
        if (u.MenuBegin("File"))
        {
            u.MenuItem("New", "Ctrl+N");
            u.MenuItem("Open", "Ctrl+O");
            u.MenuSeparator();
            if (u.MenuItem("Quit", "Ctrl+Q")) running = false;
            u.MenuEnd();
        }
        if (u.MenuBegin("View"))
        {
            u.MenuItem("Grid", null, ref grid);
            u.MenuItem("Outline", null, ref outline);
            if (u.MenuBegin("Zoom"))
            {
                if (u.MenuItem("50%")) zoom = 0.5;
                if (u.MenuItem("100%")) zoom = 1;
                if (u.MenuItem("200%")) zoom = 2;
                u.MenuEnd();
            }
            if (u.MenuItem("Inspector", null, ref inspector)) wh = wh > 0 ? wh : 300;
            u.MenuEnd();
        }
        u.MenuBarEnd();
    }

    void LeftPane()
    {
        u.Heading("Look");
        u.Tabs("tabs", tabs, ref tab);
        u.Space(2);
        u.Row(2);
        u.Checkbox("Snap to grid", ref snap);
        u.Checkbox("Show hints", ref hints);
        using (u.Style(accent: 0xFFE0567A, icon: heart == null ? null : (Icon.Check, heart))) u.Checkbox("Favorite", ref fav);
        u.Tooltip("this one gets a heart, pushed with Ui.PushIcon");
        u.Radio("Small", ref size, 0);
        u.SameLine();
        u.Radio("Medium", ref size, 1);
        u.SameLine();
        u.Radio("Large", ref size, 2);
        u.Row(2);
        u.Toggle("Soft corners", ref corners);
        u.Toggle("Autosave", ref autosave);
        u.Slider("Volume", ref volume, 0, 100, "%.0f%%");
        u.Row(2);
        u.Combo("Brush", ref brush, brushes);
        u.Color("Tint", ref tint);
        u.Row(2);
        u.Number("Copies", ref copies, 1, "%.0f");
        u.DragFloat("Opacity", ref opacity, 0.005, 0, 1, "%.2f");
        if (u.Collapsing("Layers", ref layers))
        {
            if (u.Tree("Background", ref bgOpen))
            {
                u.Label("paper texture");
                u.Label("warm fill");
                u.TreeEnd();
            }
            if (u.Tree("Sketch", ref sketchOpen))
            {
                u.Label("lines");
                u.TreeEnd();
            }
        }
    }

    int ByCol(int x, int y, int col)
    {
        if (col == 2) return files[x].Size - files[y].Size;
        return col == 1 ? string.CompareOrdinal(files[x].Kind, files[y].Kind) : string.CompareOrdinal(files[x].Name, files[y].Name);
    }

    void RightPane()
    {
        u.Heading("Files");
        var order = new int[files.Length];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        if (u.TableBegin("files", 3, 0, TableFlags.Sortable | TableFlags.Stripes))
        {
            u.TableColumn("Name");
            u.TableColumn("Kind", 90);
            u.TableColumn("KB", 70);
            u.TableSort(ref sortCol, ref sortDesc);
            for (int i = 1; i < order.Length; i++)
                for (int j = i; j > 0; j--)
                {
                    int c = sortCol >= 0 ? ByCol(order[j - 1], order[j], sortCol) : 0;
                    if (sortDesc ? c >= 0 : c <= 0) break;
                    (order[j], order[j - 1]) = (order[j - 1], order[j]);
                }
            foreach (int i in order)
            {
                u.TableRow();
                u.Label(files[i].Name);
                u.TableNext();
                u.Label(files[i].Kind);
                u.TableNext();
                u.Label(files[i].Size.ToString());
            }
        }
        u.TableEnd();
        u.Space(4);
        u.Input("Name", name);
        u.Progress(volume / 100, "exporting");
        u.Row(3);
        if (u.Button("Export")) u.Toast("exported, check your folder", 2500);
        u.Tooltip("saves every layer as its own png");
        u.Button("Share");
        if (u.Button("Inspector")) inspector = true;
    }

    void Inspector()
    {
        if (u.Window("Inspector", ref wx, ref wy, ref ww, ref wh, WindowFlags.Move | WindowFlags.Resize | WindowFlags.Collapse | WindowFlags.Close, ref inspector))
        {
            u.Label("Brush settings");
            u.Slider("Flow", ref opacity, 0, 1, "%.2f");
            u.Checkbox("Pressure", ref snap);
            u.Separator();
            for (int i = 0; i < 14; i++) u.Label($"stroke {i + 1}, {2 + i * 3} px");
        }
        u.WindowEnd();
    }

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
        if (first)
        {
            u.Toast("skin loaded, every icon is a png now", 4000);
            first = false;
        }
        if (appSkin != null) u.PushSkin(SkinKind.Titlebar, appSkin);
        u.App("Skinned", AppFlags.TitleBar | AppFlags.MenuBar | AppFlags.StatusBar | AppFlags.NoPad);
        if (appSkin != null) u.Pop();
        Menus();
        u.SplitBegin("main", false, ref frac);
        LeftPane();
        u.SplitNext();
        RightPane();
        u.SplitEnd();
        u.StatusBar($"{files.Length} files, zoom {zoom * 100:F0}%, every glyph and part here is a png from examples/assets");
        u.AppEnd();
        if (inspector) Inspector();
        u.End();
    }
}
return 0;
