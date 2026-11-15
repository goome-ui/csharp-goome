using System;
using System.Globalization;
using GoomeSharp;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

uint[] swatches = { 0xFF6FDC5F, 0xFF3FC8B4, 0xFF4FB3F7, 0xFF7C8CFF, 0xFFB57CFF, 0xFFFF6FA8, 0xFFFF7A59, 0xFFFFB443, 0xFFE8D85A };
(string Name, string Info, uint Color, double X0, double Y0)[] nodes =
{
    ("Noise", "seed 42", 0xFFB57CFF, 20, 40),
    ("Blur", "radius 6", 0xFF4FB3F7, 240, 120),
    ("Output", "1280x720", 0xFFFF7A59, 460, 40),
};
string[] hz = { "60", "150", "400", "1k", "2.4k", "6k", "15k" };
double[] eqStart = { 0.7, 0.62, 0.45, 0.5, 0.58, 0.68, 0.74 };
double[] keyTimes = { 1.2, 2.6, 5.0, 6.4, 7.7 };
string[] segs = { "Day", "Week", "Month", "Year" };
string[] tabs = { "Files", "Search", "Git" };

Goome g;
try
{
    g = new Goome(new GoomeOptions
    {
        Title = "Custom", Mode = Mode.Window, Width = 1120, Height = 800, MinWidth = 800, MinHeight = 560,
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
    var light = new Theme(u.Theme);
    light.Light();
    light.Radius = 8;
    light.Shadow = 0x60000000;

    bool running = true, test = g.BackendName.StartsWith("test"), on = true, chk = true;
    int seg = 1, sel = 0, pick = 1, tab = 0;
    double frac = 0.4, vol = 0.62, amt = 0.7;
    string name = "living room", find = "search, but round";
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
        u.App("Custom", AppFlags.StatusBar | AppFlags.NoPad);
        u.SplitBegin("main", false, ref frac);
        ButtonRows();
        u.Space(4);
        MoreWidgets();
        u.Space(6);
        LightCard();
        u.SplitNext();

        u.Pos(0, 0);
        u.Custom("##origin", 1, 1, ItemFlags.NoHit, out var o);
        u.PushColor(Style.Accent, swatches[sel]);
        u.Heading("Custom widgets");
        u.Pos(176, 3);
        u.Badge("pinned with Ui.Pos", 0x26FFFFFF);
        u.Space(2);
        Knob("Gain", 0.62, v => $"{v * 12:F1} dB");
        u.SameLine();
        Knob("Tone", 0.35, v => $"{v * 100:F0}%");
        u.SameLine();
        Knob("Mix", 0.8, v => $"{v * 100:F0}%");
        u.SameLine();
        Knob("Width", 0.5, v => $"{v * 200:F0}%");
        u.Space(4);
        Timeline(o.X, o.Y);
        u.Space(4);
        SwatchStrip();
        u.Space(4);
        NodeArea(o.X, o.Y);
        u.Space(4);
        Faders();
        u.SameLine();
        XyPad();
        double ph = u.Store("playhead", 2)[0], gain = u.Store("Gain", 2)[0];
        u.Pop();
        u.SplitEnd();

        u.StatusBar($"gain {gain * 12:F1} dB   playhead {ph:F1}s");
        u.AppEnd();
        u.End();
    }

    void Knob(string id, double def, Func<double, string> label)
    {
        var v = u.Store(id, 2);
        if (v[1] == 0)
        {
            v[0] = def;
            v[1] = 1;
        }
        u.Custom(id, 92, 112, ItemFlags.DragY | ItemFlags.Focusable, out var it);
        int key = it.Key & 0xFF;
        if (it.Held) v[0] = Clamp(v[0] - it.Dy / 180, 0, 1);
        if (key == Keys.Up || key == Keys.Right) v[0] = Clamp(v[0] + 0.05, 0, 1);
        if (key == Keys.Down || key == Keys.Left) v[0] = Clamp(v[0] - 0.05, 0, 1);
        if (it.DoubleClicked) v[0] = def;
        u.Tooltip("drag up or down, arrows work too, double click resets it");

        var th = u.Theme;
        var c = u.Canvas();
        if (c == null) return;
        double cx = it.X + it.W / 2, cy = it.Y + 40, r = 31, ri = 21;
        double a0 = 2.35619449, a1 = 7.06858347, at = a0 + (a1 - a0) * v[0];
        bool hot = it.Hovered || it.Held;
        if (hot) c.DrawCircle(cx, cy, r + 9, Fade(th.Accent, it.Held ? 0.16 : 0.09));
        c.DrawArc(cx, cy, r, 6, a0, a1, Fade(th.Text, 0.09));
        if (v[0] > 0.004) c.DrawArc(cx, cy, r, 6, a0, at, th.Accent);
        c.DrawShadow(cx - ri, cy - ri + 4, 2 * ri, 2 * ri, ri, 10, 0x70000000);
        c.DrawRRectGrad(cx - ri, cy - ri, 2 * ri, 2 * ri, Radii.All(ri), 0xFF3A3F49, 0xFF24272E);
        c.DrawRing(cx, cy, ri - 0.5, 1, 0x1AFFFFFF);
        double px = cx + Cos(at) * (ri - 7), py = cy + Sin(at) * (ri - 7);
        c.DrawLine(cx + Cos(at) * (ri - 15), cy + Sin(at) * (ri - 15), px, py, 2.6, hot ? th.Text : Mix(th.TextDim, th.Text, 0.5));
        var f = th.Font;
        TextAt(c, f, 14, cx, it.Y + 84, label(v[0]), th.Text, true);
        TextAt(c, f, 12.5, cx, it.Y + 103, id, th.TextDim, true);
    }

    void Timeline(double ox, double oy)
    {
        var t = u.Store("playhead", 2);
        if (t[1] == 0)
        {
            t[0] = 2.4;
            t[1] = 1;
        }
        var th = u.Theme;
        var f = th.Font;
        u.Custom("track", 0, 92, ItemFlags.NoHit, out var tr);
        double x0 = tr.X + 14, x1 = tr.X + tr.W - 14, secs = 10, per = (x1 - x0) / secs, ly0 = tr.Y + 34, ly1 = tr.Y + 61;
        var c = u.Canvas();
        if (c == null) return;
        c.DrawRRect(tr.X, tr.Y, tr.W, tr.H, 10, 0xFF1E2127);
        c.DrawRRectLine(tr.X, tr.Y, tr.W, tr.H, 10, 1, th.Border);
        for (int i = 0; i <= 20; i++)
        {
            double x = x0 + i * per / 2;
            c.DrawRect(x - 0.5, tr.Y + (i % 2 == 1 ? 18 : 12), 1, i % 2 == 1 ? 5 : 11, Fade(th.Text, i % 2 == 1 ? 0.14 : 0.28));
            if (i % 2 == 0 && i < 20) c.DrawText(f, 11, x + 4, tr.Y + 9, $"{i / 2}s", th.TextDim);
        }
        c.DrawRRect(x0 + 0.4 * per, ly0, 3.6 * per, 22, 6, Fade(0xFFB57CFF, 0.30));
        c.DrawRRectLine(x0 + 0.4 * per, ly0, 3.6 * per, 22, 6, 1, Fade(0xFFB57CFF, 0.65));
        c.DrawText(f, 12, x0 + 0.4 * per + 9, ly0 + 4, "intro.wav", 0xFFE6DAFF);
        c.DrawRRect(x0 + 3.1 * per, ly1, 5.3 * per, 22, 6, Fade(0xFF4FB3F7, 0.28));
        c.DrawRRectLine(x0 + 3.1 * per, ly1, 5.3 * per, 22, 6, 1, Fade(0xFF4FB3F7, 0.6));
        c.DrawText(f, 12, x0 + 3.1 * per + 9, ly1 + 4, "drums.wav", 0xFFD6EEFF);

        u.Pos(x0 + t[0] * per - 9 - ox, tr.Y + 4 - oy);
        Item hd;
        using (u.Style(radius: 6)) u.Custom("handle", 18, tr.H - 8, ItemFlags.DragX | ItemFlags.Focusable, out hd);
        int key = hd.Key & 0xFF;
        if (hd.Held && per > 0) t[0] = Clamp(t[0] + hd.Dx / per, 0, secs);
        if (key == Keys.Left || key == Keys.Right) t[0] = Clamp(Snap(t[0], 0.5) + (key == Keys.Right ? 0.5 : -0.5), 0, secs);
        if (hd.Released) t[0] = Snap(t[0], 0.1);
        u.Tooltip("this one only moves sideways");
        double hx = x0 + t[0] * per;

        c = u.Canvas();
        if (c == null) return;
        for (int i = 0; i < keyTimes.Length; i++)
        {
            double kx = x0 + keyTimes[i] * per, ky = ly1 + 11, d = 4.5;
            c.DrawPoly(new[] { kx, ky - d, kx + d, ky, kx, ky + d, kx - d, ky }, keyTimes[i] <= t[0] ? th.Accent : 0xFFD6EEFF);
        }
        c.DrawRect(x0, tr.Y + tr.H - 5, hx - x0, 2, Fade(th.Accent, 0.7));
        bool hot = hd.Hovered || hd.Held;
        c.DrawRect(hx - 1, tr.Y + 20, 2, tr.H - 25, th.Accent);
        uint hc = hot ? Mix(th.Accent, 0xFFFFFFFF, 0.25) : th.Accent;
        c.DrawShadow(hx - 8, tr.Y + 7, 16, 13, 4, 6, 0x60000000);
        c.DrawRRect(hx - 8, tr.Y + 5, 16, 13, 4, hc);
        c.DrawTriangle(hx - 6, tr.Y + 17, hx + 6, tr.Y + 17, hx, tr.Y + 23, hc);
        string s = $"{t[0]:F1}s";
        double w = f.Measure(11.5, s);
        double bx = Clamp(hx + 12, tr.X + 6, tr.X + tr.W - w - 18);
        c.DrawRRect(bx, tr.Y + 4, w + 10, 16, 5, Fade(th.Accent, 0.18));
        c.DrawText(f, 11.5, bx + 5, tr.Y + 5, s, th.Accent);
    }

    void SwatchStrip()
    {
        for (int i = 0; i < swatches.Length; i++)
        {
            if (i > 0) u.SameLine();
            u.Custom($"sw{i}", 34, 34, ItemFlags.Focusable, out var it);
            if (it.Clicked) sel = i;
            var c = u.Canvas();
            if (c == null) continue;
            double lift = it.Hovered && !it.Held ? 2 : 0;
            if (i == sel) c.DrawRRectLine(it.X - 4, it.Y - 4 - lift, it.W + 8, it.H + 8, 13, 2, 0xFFFFFFFF);
            c.DrawShadow(it.X, it.Y + 3 - lift, it.W, it.H, 9, 8 + lift * 2, 0x66000000);
            c.DrawRRectGrad(it.X, it.Y - lift, it.W, it.H, 9, Mix(swatches[i], 0xFFFFFFFF, 0.12), Mix(swatches[i], 0xFF000000, 0.12));
            c.DrawRRectLine(it.X, it.Y - lift, it.W, it.H, 9, 1, 0x26FFFFFF);
            if (i == sel)
            {
                double cx = it.X + it.W / 2, cy = it.Y + it.H / 2 - lift;
                c.DrawLine(cx - 6, cy, cx - 2, cy + 4, 2.4, 0xE0141618);
                c.DrawLine(cx - 2, cy + 4, cx + 6.5, cy - 5, 2.4, 0xE0141618);
            }
        }
    }

    void Faders()
    {
        var v = u.Store("eq", 8);
        if (v[7] == 0)
        {
            eqStart.CopyTo(v);
            v[7] = 1;
        }
        for (int i = 0; i < 7; i++)
        {
            if (i > 0) u.SameLine();
            u.Custom($"eq{i}", 28, 138, ItemFlags.DragY | ItemFlags.Focusable, out var it);
            double top = it.Y + 8, bot = it.Y + it.H - 26, len = bot - top;
            int key = it.Key & 0xFF;
            if (it.Pressed && (it.My < top + (1 - v[i]) * len - 8 || it.My > top + (1 - v[i]) * len + 8)) v[i] = Clamp(1 - (it.My - top) / len, 0, 1);
            else if (it.Held) v[i] = Clamp(v[i] - it.Dy / len, 0, 1);
            if (key == Keys.Up || key == Keys.Down) v[i] = Clamp(v[i] + (key == Keys.Up ? 0.05 : -0.05), 0, 1);
            if (it.DoubleClicked) v[i] = 0.5;
            var th = u.Theme;
            var c = u.Canvas();
            if (c == null) continue;
            double cx = it.X + it.W / 2, ty = top + (1 - v[i]) * len, mid = top + len / 2;
            c.DrawRRect(cx - 2, top, 4, len, 2, Fade(th.Text, 0.09));
            c.DrawRect(cx - 6, mid - 0.5, 12, 1, Fade(th.Text, 0.2));
            c.DrawRRect(cx - 2, ty < mid ? ty : mid, 4, ty < mid ? mid - ty : ty - mid, 2, th.Accent);
            bool hot = it.Hovered || it.Held;
            c.DrawShadow(cx - 11, ty - 5 + 2, 22, 10, 5, 6, 0x70000000);
            c.DrawRRectGrad(cx - 11, ty - 5, 22, 10, 5, hot ? 0xFFFFFFFF : 0xFFE4E7EC, hot ? 0xFFDDE1E7 : 0xFFB9BEC7);
            c.DrawRect(cx - 5, ty - 0.5, 10, 1, Fade(th.Accent, 0.9));
            TextAt(c, th.Font, 11, cx, it.Y + it.H - 9, hz[i], hot ? th.Text : th.TextDim, true);
        }
    }

    void XyPad()
    {
        var v = u.Store("xy", 3);
        if (v[2] == 0)
        {
            v[0] = 0.62;
            v[1] = 0.38;
            v[2] = 1;
        }
        u.Custom("xy", 0, 138, ItemFlags.Drag, out var it);
        if (it.Held)
        {
            v[0] = Clamp((it.Mx - it.X - 8) / (it.W - 16), 0, 1);
            v[1] = Clamp((it.My - it.Y - 8) / (it.H - 16), 0, 1);
        }
        var th = u.Theme;
        var c = u.Canvas();
        if (c == null) return;
        double px = it.X + 8 + v[0] * (it.W - 16), py = it.Y + 8 + v[1] * (it.H - 16);
        c.DrawRRect(it.X, it.Y, it.W, it.H, 12, 0xFF1B1E23);
        c.DrawRRectGrad(it.X, it.Y, it.W, it.H, 12, Fade(th.Accent, 0.0), Fade(th.Accent, 0.10), true);
        for (int i = 1; i < 4; i++)
        {
            c.DrawRect(it.X + it.W * i / 4, it.Y + 6, 1, it.H - 12, 0x0FFFFFFF);
            c.DrawRect(it.X + 6, it.Y + it.H * i / 4, it.W - 12, 1, 0x0FFFFFFF);
        }
        c.DrawRect(px - 0.5, it.Y + 4, 1, it.H - 8, Fade(th.Accent, 0.35));
        c.DrawRect(it.X + 4, py - 0.5, it.W - 8, 1, Fade(th.Accent, 0.35));
        c.DrawCircle(px, py, it.Held ? 18 : 14, Fade(th.Accent, it.Held ? 0.22 : 0.14));
        c.DrawCircle(px, py, 7, th.Accent);
        c.DrawRing(px, py, 7, 2, 0xFFFFFFFF);
        c.DrawText(th.Font, 11.5, it.X + 12, it.Y + 9, $"cutoff {v[0]:F2}  res {1 - v[1]:F2}", th.TextDim);
        c.DrawRRectLine(it.X, it.Y, it.W, it.H, 12, 1, it.Hovered || it.Held ? Fade(th.Accent, 0.6) : th.Border);
    }

    void NodeArea(double ox, double oy)
    {
        u.Custom("area", 0, 232, ItemFlags.NoHit, out var ar);
        var its = new Item[nodes.Length];
        var xs = new double[nodes.Length];
        var ys = new double[nodes.Length];
        int top = -1;
        for (int i = 0; i < nodes.Length; i++)
        {
            var p = u.Store(nodes[i].Name, 3);
            if (p[2] == 0)
            {
                p[0] = nodes[i].X0;
                p[1] = nodes[i].Y0;
                p[2] = 1;
            }
            u.Pos(ar.X - ox + p[0], ar.Y - oy + p[1]);
            u.Custom(nodes[i].Name, 150, 58, ItemFlags.Drag, out its[i]);
            ref var it = ref its[i];
            if (it.Held)
            {
                p[0] += it.Dx;
                p[1] += it.Dy;
                top = i;
            }
            p[0] = Clamp(p[0], 8, ar.W - it.W - 8);
            p[1] = Clamp(p[1], 8, ar.H - it.H - 8);
            if (it.Released)
            {
                p[0] = Clamp(Snap(p[0], 20), 0, ar.W - it.W);
                p[1] = Clamp(Snap(p[1], 20), 0, ar.H - it.H);
            }
            xs[i] = p[0];
            ys[i] = p[1];
        }
        var th = u.Theme;
        var c = u.Canvas();
        if (c == null) return;
        c.DrawRRect(ar.X, ar.Y, ar.W, ar.H, 12, 0xFF17191E);
        c.ClipPush(ar.X + 1, ar.Y + 1, ar.W - 2, ar.H - 2);
        for (double y = ar.Y + 20; y < ar.Y + ar.H; y += 20)
            for (double x = ar.X + 20; x < ar.X + ar.W; x += 20) c.DrawRect(x - 1, y - 1, 2, 2, 0x16FFFFFF);
        if (top >= 0)
        {
            double sx = Clamp(Snap(xs[top], 20), 0, ar.W - its[top].W), sy = Clamp(Snap(ys[top], 20), 0, ar.H - its[top].H);
            c.DrawRRect(ar.X + sx, ar.Y + sy, its[top].W, its[top].H, 12, Fade(th.Accent, 0.08));
            c.DrawRRectLine(ar.X + sx, ar.Y + sy, its[top].W, its[top].H, 12, 1.5, Fade(th.Accent, 0.45));
        }
        for (int i = 0; i + 1 < nodes.Length; i++)
            Wire(c, ar.X + xs[i] + its[i].W, ar.Y + ys[i] + 41, ar.X + xs[i + 1], ar.Y + ys[i + 1] + 41, nodes[i].Color, nodes[i + 1].Color);
        for (int i = 0; i < nodes.Length; i++)
            if (i != top) NodeBox(c, th, i, its[i], ar.X + xs[i], ar.Y + ys[i], false);
        if (top >= 0) NodeBox(c, th, top, its[top], ar.X + xs[top], ar.Y + ys[top], true);
        c.DrawText(th.Font, 12, ar.X + 14, ar.Y + ar.H - 24, "drag the boxes, they snap to the grid when you let go", Fade(th.TextDim, 0.8));
        c.ClipPop();
        c.DrawRRectLine(ar.X, ar.Y, ar.W, ar.H, 12, 1, th.Border);
    }

    void NodeBox(Canvas c, Theme th, int n, in Item it, double x, double y, bool top)
    {
        var d = nodes[n];
        double w = it.W, h = it.H;
        c.DrawShadow(x, y + (top ? 10 : 5), w, h, 12, top ? 22 : 12, top ? 0x90000000 : 0x70000000);
        c.DrawRRect(x, y, w, h, 12, 0xFF2A2E36);
        c.DrawRRectGrad(x, y, w, 26, new Radii(12, 12, 0, 0), Mix(d.Color, 0xFF2A2E36, 0.25), Mix(d.Color, 0xFF2A2E36, 0.55), true);
        c.DrawCircle(x + 15, y + 13, 4, 0xFFFFFFFF);
        c.DrawText(th.Font, 13.5, x + 26, y + 5, d.Name, 0xFFFFFFFF);
        c.DrawText(th.Font, 12, x + 12, y + 35, d.Info, th.TextDim);
        bool hot = it.Held || it.Hovered;
        uint edge = hot ? Mix(th.Accent, 0xFFFFFFFF, it.Held ? 0.2 : 0) : 0x22FFFFFF;
        c.DrawRRectLine(x, y, w, h, 12, hot ? 1.5 : 1, edge);
        c.DrawCircle(x, y + 41, 5.5, 0xFF1A1C21);
        c.DrawRing(x, y + 41, 5, 2, d.Color);
        c.DrawCircle(x + w, y + 41, 5.5, 0xFF1A1C21);
        c.DrawRing(x + w, y + 41, 5, 2, d.Color);
    }

    void ButtonRows()
    {
        u.Heading("Buttons your way");
        u.Wrapped("push a radius, a color or per corner radii, then pop it. only the widgets in between change.");
        u.Button("Normal");
        u.SameLine();
        u.PushNum(Style.Radius, 0);
        u.Button("Square");
        u.Pop();
        u.SameLine();
        using (u.Style(radius: 999)) u.Button("Pill");
        u.SameLine();
        using (u.Style(radii: new Radii(16, 3, 16, 3))) u.Button("Leaf");

        using (u.Style(gap: 1))
            for (int i = 0; i < segs.Length; i++)
            {
                if (i > 0) u.SameLine();
                var r = new Radii(i == 0 ? 9 : 2, i == 3 ? 9 : 2, i == 3 ? 9 : 2, i == 0 ? 9 : 2);
                using (seg == i ? u.Style(radii: r, inputBg: 0xFF3B4250, border: 0x40FFFFFF) : u.Style(radii: r, text: 0xFF969CA6, border: 0))
                {
                    u.Size(74, 0);
                    if (u.Button(segs[i])) seg = i;
                }
            }
        u.Space(8);

        using (u.Style(inputBg: 0xFF6FDC5F, hover: 0xFF86E878, active: 0xFF58C24A, text: 0xFF0F1D0C, border: 0, radius: 999)) u.Button("Save it");
        u.SameLine();
        using (u.Style(inputBg: 0, hover: 0x24E5534B, active: 0x40E5534B, text: 0xFFFF7A72, border: 0x80E5534B, borderWidth: 1.5)) u.Button("Delete");
        u.SameLine();
        using (u.Style(fontSize: 19, pad: 14, radii: new Radii(4, 18, 4, 18))) u.Button("Big one");

        using (u.Style(radii: new Radii(12, 12, 0, 0), gap: 2))
            for (int i = 0; i < tabs.Length; i++)
            {
                if (i > 0) u.SameLine();
                bool cur = tab == i;
                using (u.Style(inputBg: cur ? 0xFF2F333C : 0xFF1F2228, border: cur ? 0x30FFFFFF : 0u, text: cur ? 0xFFE9EBEE : 0xFF8C929C))
                    if (u.Button(tabs[i])) tab = i;
            }
    }

    void MoreWidgets()
    {
        u.Heading("Every widget reads it");
        using (u.Style(accent: 0xFFFF6FA8, accentText: 0xFF2A0614))
        {
            u.Row(2);
            u.Checkbox("Pink", ref chk);
            u.Toggle("Also pink", ref chk);
            u.Slider("Amount", ref amt, 0, 1, "%.2f");
        }
        using (u.Style(radius: 999)) u.Input("##find", ref find, 64);
        using (u.Style(radius: 0, accent: 0xFF4FB3F7, accentText: 0xFF06202E)) u.Progress(amt, "square corners");
    }

    void LightCard()
    {
        u.Custom("light-spot", 0, 262, ItemFlags.NoHit, out var at);
        u.PushTheme(light);
        double x = at.X, y = at.Y, w = at.W, h = at.H;
        if (u.Window("A whole other theme", ref x, ref y, ref w, ref h))
        {
            u.Toggle("Wifi", ref on);
            u.Slider("Volume", ref vol, 0, 1, "%.2f");
            u.Row(2);
            u.Radio("Small", ref pick, 0);
            u.Radio("Large", ref pick, 1);
            u.Input("##name", ref name, 64);
            using (u.Style(radius: 999))
            {
                u.Button("Cancel");
                u.SameLine();
                using (u.Style(inputBg: light.Accent, hover: 0xFF55C944, active: 0xFF3AA12B, text: 0xFFFFFFFF, border: 0)) u.Button("Apply");
            }
        }
        u.WindowEnd();
        u.Pop();
    }
}
return 0;

static double Clamp(double v, double a, double b) => v < a ? a : v > b ? b : v;

static double Snap(double v, double step)
{
    double k = v / step + 0.5;
    long i = (long)k;
    if (i > k) i--;
    return i * step;
}

static double Sin(double x)
{
    while (x > 3.14159265358979) x -= 6.28318530717959;
    while (x < -3.14159265358979) x += 6.28318530717959;
    double x2 = x * x;
    return x * (1 - x2 / 6 * (1 - x2 / 20 * (1 - x2 / 42 * (1 - x2 / 72 * (1 - x2 / 110)))));
}

static double Cos(double x) => Sin(x + 1.5707963267949);

static uint Fade(uint c, double k) => (uint)(((c >> 24) & 255) * Clamp(k, 0, 1) + 0.5) << 24 | (c & 0xFFFFFF);

static uint Mix(uint a, uint b, double t)
{
    uint o = 0;
    for (int s = 0; s < 32; s += 8)
    {
        double x = (a >> s) & 255, y = (b >> s) & 255;
        o |= (uint)(x + (y - x) * Clamp(t, 0, 1) + 0.5) << s;
    }
    return o;
}

static void Wire(Canvas c, double ax, double ay, double bx, double by, uint from, uint to)
{
    double px = ax, py = ay, pull = Clamp((bx - ax) * 0.5, 40, 200);
    for (int i = 1; i <= 32; i++)
    {
        double t = i / 32.0, s = 1 - t;
        double x = s * s * s * ax + 3 * s * s * t * (ax + pull) + 3 * s * t * t * (bx - pull) + t * t * t * bx;
        double y = s * s * s * ay + 3 * s * s * t * ay + 3 * s * t * t * by + t * t * t * by;
        c.DrawLine(px, py, x, y, 2.5, Mix(from, to, t - 1 / 64.0));
        px = x;
        py = y;
    }
}

static void TextAt(Canvas c, Font f, double size, double x, double cy, string s, uint col, bool center)
{
    double w = f.Measure(size, s, out double h);
    if (h <= 0) h = f.LineHeight(size);
    c.DrawText(f, size, center ? x - w / 2 : x, (int)(cy - h / 2 + 0.5), s, col);
}
