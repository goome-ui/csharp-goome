using System;
using GoomeSharp;

(string Name, Ease Ease, uint Color)[] rows =
{
    ("linear", Ease.Linear, 0xFF7C8CFF), ("in", Ease.In, 0xFF4FB3F7), ("out", Ease.Out, 0xFF3FC8B4),
    ("in out", Ease.InOut, 0xFF6FDC5F), ("smooth", Ease.Smooth, 0xFFE8D85A), ("back", Ease.Back, 0xFFFFB443),
    ("spring", Ease.Spring, 0xFFFF7A59), ("bounce", Ease.Bounce, 0xFFFF6FA8),
};
uint[] moods = { 0xFF2D6CDF, 0xFFE0483E, 0xFF21A66B, 0xFFF2B33D, 0x00000000 };
string[] moodNames = { "blue", "red", "green", "gold", "see through" };

Goome g;
try
{
    g = new Goome(new GoomeOptions
    {
        Title = "Animations", Mode = Mode.Window, Width = 880, Height = 760, MinWidth = 620, MinHeight = 700,
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
    bool running = true, test = g.BackendName.StartsWith("test"), autoplay = true, on = false, kick = false;
    int side = 0, mood = 0, count = 0;
    long frame = 0, flipped = 0;
    var pace = new Pacer();
    while (running)
    {
        int wait = test || autoplay || u.Animating ? pace.Pace(60) : 500;
        while (g.Poll(out var ev, wait))
        {
            if (ev.Type == EventType.Quit || ev.Type == EventType.Close) running = false;
            u.Feed(ev);
            wait = 0;
        }
        if (!running) break;
        long now = test ? frame * 16 : Goome.NowMs;
        frame++;
        if (autoplay && now - flipped >= 1500)
        {
            flipped = now;
            side = 1 - side;
            if (side == 1)
            {
                mood = (mood + 1) % moods.Length;
                on = !on;
                count++;
                kick = true;
            }
        }
        if (u.Begin(0) == null) continue;
        u.App("Animations", AppFlags.StatusBar);
        u.Heading("Every ease, same 900 ms");
        if (u.Button("Swap sides"))
        {
            side = 1 - side;
            flipped = now;
        }
        u.SameLine();
        u.Toggle("Play by itself", ref autoplay);
        u.Space(4);
        EaseRows();
        u.Space(8);
        u.Heading("Color fade");
        ColorCard();
        u.Space(8);
        u.Heading("Spring and bounce");
        SpringToggle("springy");
        u.SameLine();
        u.Label(on ? "on, and it wobbled a bit" : "off");
        u.SameLine();
        u.Space(40);
        u.SameLine();
        Bell();
        u.StatusBar(u.Animating ? "moving" : "still");
        u.End();
    }

    void EaseRows()
    {
        var th = u.Theme;
        foreach (var row in rows)
        {
            u.Custom(row.Name, 0, 30, ItemFlags.NoHit, out var it);
            double k = u.Anim(row.Name, side, 900, row.Ease);
            var c = u.Canvas();
            if (c == null) continue;
            double tx = it.X + 86, tw = it.W - 96, bw = 46, run = (tw - bw) * 0.76, x = tx + (tw - bw - run) / 2 + k * run;
            TextAt(c, th.Font, 13, it.X + 4, it.Y + 15, row.Name, th.TextDim, false);
            c.DrawRRect(tx, it.Y + 3, tw, 24, Radii.All(12), 0x14FFFFFF);
            c.DrawRect(tx + (tw - bw - run) / 2 + bw / 2, it.Y + 14, 1, 2, 0x30FFFFFF);
            c.DrawRect(tx + (tw - bw - run) / 2 + run + bw / 2, it.Y + 14, 1, 2, 0x30FFFFFF);
            c.DrawShadow(x, it.Y + 8, bw, 16, 8, 8, Fade(row.Color, 0.45));
            c.DrawRRect(x, it.Y + 6, bw, 18, Radii.All(9), row.Color);
        }
    }

    void ColorCard()
    {
        var th = u.Theme;
        u.Custom("mood", 0, 92, ItemFlags.None, out var it);
        if (it.Clicked) mood = (mood + 1) % moods.Length;
        u.Tooltip("click for the next color");
        uint col = u.AnimColor("mood", moods[mood], 600, Ease.InOut);
        double lift = u.Anim("mood lift", it.Hovered ? 1 : 0, 160, Ease.Out);
        var c = u.Canvas();
        if (c == null) return;
        double y = it.Y - lift * 2;
        for (int i = 0; i < 12; i++) c.DrawRect(it.X + i * it.W / 12, it.Y + 8, it.W / 24, 76, 0x10FFFFFF);
        c.DrawShadow(it.X, y + 6, it.W, 84, 14, 18, Fade(col, 0.4 + lift * 0.2));
        c.DrawRRect(it.X, y, it.W, 84, Radii.All(14), col);
        c.DrawRing(it.X + 30, y + 42, 12, 2, 0x50FFFFFF);
        TextAt(c, th.Font, 15, it.X + 54, y + 42, $"{moodNames[mood]}  #{col:X8}", th.Text, false);
    }

    void SpringToggle(string id)
    {
        var th = u.Theme;
        u.Custom(id, 64, 36, ItemFlags.Focusable, out var it);
        int key = it.Key & 0xFF;
        if (it.Clicked || key == ' ' || key == Keys.Enter) on = !on;
        double k = u.Anim(id, on ? 1 : 0, 520, Ease.Spring);
        double squish = u.Anim("squish", it.Held ? 1 : 0, 220, Ease.Spring);
        uint track = u.AnimColor(id, on ? th.Accent : 0xFF3A3F49, 260, Ease.Out);
        var c = u.Canvas();
        if (c == null) return;
        double x = it.X + 2, y = it.Y + 4, w = 60, h = 28, r = 11, kw = 2 * r + squish * 8;
        if (it.Focused) c.DrawRRect(x - 3, y - 3, w + 6, h + 6, Radii.All(17), Fade(th.Focus != 0 ? th.Focus : th.Accent, 0.6));
        c.DrawRRect(x, y, w, h, Radii.All(14), track);
        double kx = x + 3 + k * (w - 6 - kw);
        c.DrawShadow(kx, y + 5, kw, 2 * r, r, 6, 0x60000000);
        c.DrawRRect(kx, y + 3, kw, 2 * r, Radii.All(r), 0xFFF4F5F7);
    }

    void Bell()
    {
        var th = u.Theme;
        u.Custom("bell", 140, 64, ItemFlags.None, out var it);
        if (it.Clicked)
        {
            count++;
            kick = true;
        }
        double drop = u.Anim("drop", kick ? -46 : 0, kick ? 0 : 900, Ease.Bounce);
        double pop = u.Anim("pop", kick ? 0.4 : 1, kick ? 0 : 500, Ease.Spring);
        double shown = u.Anim("shown", count > 0 ? 1 : 0, 200, Ease.Out);
        kick = false;
        var c = u.Canvas();
        if (c == null) return;
        uint bg = u.AnimColor("bell bg", it.Hovered ? 0xFF2E333C : 0xFF262A31, 140, Ease.Out);
        c.DrawRRect(it.X, it.Y + 8, 120, 52, Radii.All(12), bg);
        TextAt(c, th.Font, 14, it.X + 60, it.Y + 34, "notify me", th.Text, true);
        if (shown <= 0.01) return;
        double r = 13 * pop, cx = it.X + 116, cy = it.Y + 10 + drop;
        c.DrawShadow(cx - r, cy - r + 3, 2 * r, 2 * r, r, 8, Fade(0x90000000, shown));
        c.DrawCircle(cx, cy, r, Fade(0xFFE0483E, shown));
        if (pop > 0.6) TextAt(c, th.Font, 12 * pop, cx, cy, count.ToString(), Fade(0xFFFFFFFF, shown), true);
    }
}
return 0;

static uint Fade(uint c, double k)
{
    k = Math.Clamp(k, 0, 1);
    return (uint)(((c >> 24) & 255) * k + 0.5) << 24 | (c & 0xFFFFFF);
}

static void TextAt(Canvas c, Font f, double size, double x, double cy, string s, uint col, bool center)
{
    double w = f.Measure(size, s, out double h);
    if (h <= 0) h = f.LineHeight(size);
    c.DrawText(f, size, center ? x - w / 2 : x, (int)(cy - h / 2 + 0.5), s, col);
}
