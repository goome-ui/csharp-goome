using System;
using GoomeSharp;

Goome g;
try
{
    g = new Goome(new GoomeOptions { Title = "goome hello" });
}
catch (GoomeException e)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}
using (g)
{
    var mons = g.Monitors;
    Console.WriteLine($"backend: {g.BackendName}");
    for (int i = 0; i < mons.Length; i++)
        Console.WriteLine($"monitor {i}: {mons[i].Name} {mons[i].Area.W}x{mons[i].Area.H} at {mons[i].Area.X},{mons[i].Area.Y} scale {mons[i].Scale:0.00}");
    if (!g.Hotkey("alt+h", 1)) Console.WriteLine($"no global hotkeys here: {g.LastError}");

    double bx = (mons.Length > 0 ? mons[0].Area.X : 0) + 80, by = (mons.Length > 0 ? mons[0].Area.Y : 0) + 80;
    int clicks = 0;
    bool hidden = false, typing = false, running = true;
    string typed = "";
    var pace = new Pacer();
    while (running)
    {
        int wait = pace.Pace(30);
        while (g.Poll(out var ev, wait))
        {
            if (ev.Type == EventType.Quit) running = false;
            if (ev.Type == EventType.Down)
            {
                Console.WriteLine($"click {ev.X:0},{ev.Y:0}");
                if (ev.X > bx + 250 && ev.Y < by + 30) running = false;
                else if (ev.Y > by + 80) g.Keyboard(typing = !typing);
                else clicks++;
            }
            if (ev.Type == EventType.Key)
            {
                Console.WriteLine($"key {ev.Key} mods {(int)ev.Mods:x} text '{ev.Text}'");
                if (ev.Key == Keys.Esc) running = false;
                else if (ev.Key == Keys.Backspace && typed.Length > 0) typed = typed[..^1];
                else if (ev.Text.Length > 0 && typed.Length + ev.Text.Length < 40) typed += ev.Text;
            }
            if (ev.Type == EventType.Hotkey || (ev.Type == EventType.Control && ev.Message == "hide")) hidden = !hidden;
            if (ev.Type == EventType.Scroll) by += ev.Dy * 10;
            wait = pace.Pace(30);
        }
        var c = g.Begin(0);
        if (c == null) continue;
        if (!hidden)
        {
            c.DrawRoundRect(bx, by, 280, 120, 14, 0xE0202838);
            c.DrawFrame(bx, by, 280, 120, 2, (clicks & 1) != 0 ? 0xFF6FDC5F : 0xFF8090A0);
            c.DrawText(bx + 16, by + 12, "hello from goome", true, 0xFFFFFFFF);
            c.DrawLine(bx + 256, by + 10, bx + 268, by + 22, 2, 0xFFFF6B8B);
            c.DrawLine(bx + 268, by + 10, bx + 256, by + 22, 2, 0xFFFF6B8B);
            c.DrawText(bx + 16, by + 46, $"clicks: {clicks}", false, 0xFFC0C8D0);
            c.DrawRoundRect(bx + 12, by + 80, 256, 28, 8, typing ? 0xFF2E4A30 : 0xFF303848);
            c.DrawText(bx + 22, by + 86, typing ? (typed.Length > 0 ? typed : "type something...") : "click here to type", false, 0xFFFFFFFF);
            g.End(c, new Rect((int)bx, (int)by, 280, 120));
        }
        else
        {
            g.End(c);
        }
    }
}
return 0;
