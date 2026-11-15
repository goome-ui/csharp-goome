using System;
using GoomeSharp;

string path = args.Length > 0 ? args[0] : "offscreen.ppm";
Goome g;
try
{
    g = Goome.OpenOffscreen(360, 330, 2);
}
catch (GoomeException e)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}
using (g)
using (var u = new Ui(g))
{
    u.Theme.AnimMs = 0;
    double wx = 20, wy = 20, ww = 320, wh = 0, volume = 0.25;
    bool check = false;
    string name = "goome";
    for (int frame = 0; frame < 10; frame++)
    {
        switch (frame)
        {
            case 2: g.Push(Event.Down(40, 109)); g.Push(Event.Up(40, 109)); break;
            case 3: g.Push(Event.Down(60, 163)); break;
            case 4: g.Push(Event.Motion(250, 163)); break;
            case 5: g.Push(Event.Up(250, 163)); break;
            case 6: g.Push(Event.Down(150, 223)); g.Push(Event.Up(150, 223)); break;
            case 7: g.PushText(" from an engine"); break;
            case 8: g.Push(Event.Motion(80, 279)); break;
        }
        while (g.Poll(out var ev)) u.Feed(ev);
        if (u.Begin(0) == null) continue;
        if (u.Window("offscreen", ref wx, ref wy, ref ww, ref wh, WindowFlags.Move))
        {
            u.Label("no window, the engine owns the screen");
            u.Checkbox("Pushed a click", ref check);
            u.Slider("Volume", ref volume, 0, 1, "%.2f");
            u.Input("Name", ref name, 64);
            u.Progress(volume);
            u.Button("Hover me");
        }
        u.WindowEnd();
        u.End();
    }

    var f = g.Frame();
    var px = f.Pixels;
    var out_ = new uint[f.Width * f.Height];
    for (int y = 0; y < f.Height; y++)
        for (int x = 0; x < f.Width; x++)
        {
            uint s = px[y * f.Stride + x], a = s >> 24, v = 0xFF000000u;
            uint bg = ((x / 16 + y / 16) & 1) != 0 ? 0xFF3A4A5Au : 0xFF2E3C4Au;
            for (int k = 0; k < 24; k += 8) v |= (((s >> k) & 255) + ((bg >> k) & 255) * (255 - a) / 255) << k;
            out_[y * f.Width + x] = v;
        }
    var err = Goome.SavePpm(path, out_, f.Width, f.Height, f.Width);
    Console.WriteLine($"check {(check ? 1 : 0)} volume {volume:0.00} name '{name}' cursor {(int)g.Cursor} -> {(err == GoomeError.Ok ? path : "failed")}");
    return err == GoomeError.Ok ? 0 : 1;
}
