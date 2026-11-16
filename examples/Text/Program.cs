using System;
using GoomeSharp;

string[] lines =
{
    "goome \U0001F600 \U0001F389 \U0001F44D\U0001F3FD \U0001F468‍\U0001F469‍\U0001F467 \U0001F1E9\U0001F1EA #️⃣ ❤️ \U0001F431",
    "السلام عليكم، كيف حالك؟",
    "שלום עולם, מה שלומך?",
    "goome says مرحبا (123) to שלום \U0001F44B",
    "لا لأن محمد كتب 2024 [ok]",
};
string? shot = args.Length > 0 ? args[0] : null;
Goome g;
try
{
    g = new Goome(new GoomeOptions
    {
        Title = "goome text", Mode = shot != null ? Mode.Offscreen : Mode.Window, Width = 560, Height = 600, Scale = shot != null ? 2 : 0,
        WindowStyle = WindowStyle.Resizable | WindowStyle.Center,
    });
}
catch (GoomeException e)
{
    Console.Error.WriteLine("cant open: " + e.Message);
    return 1;
}
int bad = 0;
using (g)
using (var u = new Ui(g))
{
    if (shot != null)
    {
        u.Theme.AnimMs = 0;
        u.Config.BlinkMs = 0;
    }
    int dir = (int)TextDirection.Auto, frame = 0;
    bool run = true;
    var name = new byte[128];
    var notes = new byte[1024];
    Put(name, "العربية abc \U0001F600");
    Put(notes, "שלום! this line starts in hebrew\nmixed: abc عربي 42 \U0001F44D\U0001F3FD\nمرحبا بكم");
    while (run)
    {
        if (shot != null && frame == 3)
        {
            g.Push(Event.KeyPress(Keys.End));
            for (int i = 0; i < 6; i++) g.Push(Event.KeyPress(Keys.Right, Mods.Shift));
        }
        while (g.Poll(out var ev, shot != null ? 0 : 16))
        {
            if (ev.Type == EventType.Close || ev.Type == EventType.Quit) run = false;
            u.Feed(ev);
        }
        if (u.Begin(0) == null) continue;
        Font.Direction = (TextDirection)dir;
        if (u.App("goome text"))
        {
            u.Heading("Emoji, Arabic, Hebrew");
            foreach (var l in lines) u.Label(l);
            u.Separator();
            u.Row(3);
            u.Radio("Auto", ref dir, (int)TextDirection.Auto);
            u.Radio("Left to right", ref dir, (int)TextDirection.Ltr);
            u.Radio("Right to left", ref dir, (int)TextDirection.Rtl);
            u.Row(1);
            if (shot != null && frame == 2) u.SetFocus();
            u.Input("Name", name);
            u.TextEdit("notes", notes, 120, EditFlags.Wrap);
            u.AppEnd();
        }
        u.End();
        Font.Direction = TextDirection.Auto;
        if (shot != null && ++frame == 6) run = false;
    }
    if (shot != null) bad = Save(g, shot);
}
return bad;

static void Put(byte[] buf, string s)
{
    int n = System.Text.Encoding.UTF8.GetBytes(s, 0, s.Length, buf, 0);
    buf[n] = 0;
}

static int Save(Goome g, string path)
{
    if (!g.TryFrame(out var f)) return 1;
    var o = new uint[f.Width * f.Height];
    for (int y = 0; y < f.Height; y++)
        for (int x = 0; x < f.Width; x++)
        {
            uint s = f[x, y], a = s >> 24, v = 0xFF000000u;
            for (int k = 0; k < 24; k += 8) v |= (((s >> k) & 255) + (0x20 * (255 - a)) / 255) << k;
            o[y * f.Width + x] = v;
        }
    return Goome.SavePpm(path, o, f.Width, f.Height, f.Width) == GoomeError.Ok ? 0 : 1;
}
