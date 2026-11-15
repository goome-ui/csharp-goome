using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GoomeSharp;

static class Program
{
    static int fails, checks;
    static string root = "";

    static void Check(bool ok, [CallerArgumentExpression(nameof(ok))] string what = "", [CallerLineNumber] int line = 0)
    {
        checks++;
        if (ok) return;
        fails++;
        Console.WriteLine($"FAIL line {line}  {what}");
    }

    static uint Fnv(ReadOnlySpan<uint> px, int w, int h, int stride)
    {
        uint v = 2166136261u;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                uint p = px[y * stride + x];
                for (int k = 0; k < 4; k++) v = (v ^ ((p >> (k * 8)) & 255)) * 16777619u;
            }
        return v;
    }

    static int Main(string[] args)
    {
        root = FindRoot();
        var tests = new (string, Action)[]
        {
            ("layout", Layout), ("combos", Combos), ("pacer", PacerTest), ("drawing", Drawing), ("shapes", Shapes), ("fonts", Fonts),
            ("bitmaps", Bitmaps), ("scripted", Scripted), ("ui", UiRun), ("ui widgets", UiWidgets), ("disposal", Disposal), ("errors", Errors),
            ("offscreen", Offscreen), ("frame", FrameHash), ("events", Events), ("window", WindowMode), ("app", AppMode), ("kit", KitTest), ("widgets2", Widgets2), ("skins", Skins), ("popups", Popups), ("grab", GrabTest),
            ("style", StyleTest), ("pos", PosTest), ("custom", CustomTest), ("store", StoreTest), ("frame copy", FrameCopyTest), ("icons", IconsTest),
            ("looks", LooksTest), ("inputs", InputsTest), ("keymap", KeymapTest), ("config", ConfigTest),
        };
        foreach (var (name, run) in tests)
        {
            if (args.Length > 0 && !args.Contains(name)) continue;
            int before = fails;
            try
            {
                run();
            }
            catch (Exception e)
            {
                fails++;
                Console.WriteLine($"FAIL {name} threw {e}");
            }
            Console.WriteLine($"{(fails == before ? "ok  " : "FAIL")} {name}");
        }
        Console.WriteLine(fails > 0 ? $"{fails} failed out of {checks}" : $"all good, {checks} checks");
        return fails > 0 ? 1 : 0;
    }

    static string FindRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "native", "goome.h"))) return d.FullName;
        throw new Exception("cant find the repo root");
    }

    static string Data(string name) => Path.Combine(root, "tests", "data", name);

    static string Pascal(string c) => c switch
    {
        "tl" => "TopLeft",
        "tr" => "TopRight",
        "br" => "BottomRight",
        "bl" => "BottomLeft",
        _ => string.Concat(c.Split('_').Select(p => char.ToUpperInvariant(p[0]) + p[1..])),
    };

    static void Layout()
    {
        string exe = Environment.GetEnvironmentVariable("GOOME_LAYOUT") ?? Path.Combine(root, "build", OperatingSystem.IsWindows() ? "layout.exe" : "layout");
        var p = Process.Start(new ProcessStartInfo(exe) { RedirectStandardOutput = true, UseShellExecute = false })!;
        string text = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        Check(p.ExitCode == 0);
        var types = new Dictionary<string, Type>
        {
            ["goome_opts"] = typeof(RawOpts), ["goome_rect"] = typeof(Rect), ["goome_monitor"] = typeof(RawMonitor), ["goome_image"] = typeof(RawImage),
            ["goome_canvas"] = typeof(RawCanvas), ["goome_event"] = typeof(RawEvent), ["goome_pacer"] = typeof(Pacer), ["goome_radii"] = typeof(Radii),
            ["goome_bitmap"] = typeof(RawBitmap), ["goome_theme"] = typeof(RawTheme), ["goome_skin"] = typeof(RawSkin), ["goome_ui_item"] = typeof(Item),
            ["goome_combo"] = typeof(RawCombo), ["goome_ui_config"] = typeof(RawUiConfig),
        };
        var seen = new Dictionary<Type, int>();
        int lines = 0;
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split(' ');
            var t = types[parts[0]];
            int want = int.Parse(parts[2]);
            lines++;
            if (parts[1] == ".")
            {
                int got = (int)typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!.MakeGenericMethod(t).Invoke(null, null)!;
                if (got != want) Console.WriteLine($"  sizeof {parts[0]}: C {want}, C# {got}");
                Check(got == want, $"sizeof {parts[0]}");
                Check(Marshal.SizeOf(t) == want, $"Marshal.SizeOf {parts[0]}");
                continue;
            }
            string f = Pascal(parts[1]);
            var fi = t.GetField(f, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ?? t.GetField(parts[1], BindingFlags.NonPublic | BindingFlags.Instance)
                ?? t.GetField(char.ToLowerInvariant(f[0]) + f[1..], BindingFlags.NonPublic | BindingFlags.Instance);
            Check(fi != null, $"{t.Name} has {f}");
            if (fi == null) continue;
            int off = (int)Marshal.OffsetOf(t, fi.Name);
            if (off != want) Console.WriteLine($"  {parts[0]}.{parts[1]}: C {want}, C# {off}");
            Check(off == want, $"offset {parts[0]}.{parts[1]}");
            seen[t] = seen.GetValueOrDefault(t) + 1;
        }
        Check(lines > 110);
        foreach (var t in types.Values)
            Check(seen.GetValueOrDefault(t) == t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length, $"every field of {t.Name} checked");
    }

    static void Combos()
    {
        Check(Goome.ParseCombo("alt+h", out var k, out var m) && k == 'h' && m == Mods.Alt);
        Check(Goome.ParseCombo("Ctrl+Shift+F5", out k, out m) && k == Keys.F(5) && m == (Mods.Ctrl | Mods.Shift));
        Check(Goome.ParseCombo("alt+?", out k, out m) && k == '/' && m == (Mods.Alt | Mods.Shift));
        Check(Goome.ParseCombo("super+space", out k, out m) && k == ' ' && m == Mods.Super);
        Check(Goome.ParseCombo("cmd+left", out k, out m) && k == Keys.Left && m == Mods.Super);
        Check(Goome.ParseCombo("H", out k, out m) && k == 'h' && m == Mods.Shift);
        Check(!Goome.ParseCombo("alt+", out _, out _));
        Check(!Goome.ParseCombo("alt+f13", out _, out _));
        Check(!Goome.ParseCombo("", out _, out _));
    }

    static void PacerTest()
    {
        var p = new Pacer();
        Check(p.Pace(50) == 0);
        int w = p.Pace(50);
        Check(w > 0 && w <= 20);
        Check(p.Fps == 50);
        long a = Goome.NowMs;
        Check(Goome.NowMs >= a);
    }

    static void Drawing()
    {
        using var c = Canvas.Create(160, 100, 200, 1.5);
        c.DrawRoundRect(5, 5, 60, 40, 8, 0xC0203040);
        c.DrawLine(0, 0, 100, 60, 3, 0xFFFF0000);
        c.DrawTriangle(10, 50, 60, 50, 35, 10, 0x8000FF00);
        c.DrawText(8, 8, "goome!", true, 0xFFFFFFFF);
        c.DrawFrame(70, 10, 30, 30, 2, 0xFF00FFFF);
        var px = c.Pixels;
        bool past = true;
        for (int y = 0; y < 100; y++) past &= px[y * 200 + 170] == 0;
        Check(past);
        var d = c.Drawn;
        Check(d.X1 > d.X0 && d.Y1 > d.Y0);
        uint h = Fnv(px, 200, 100, 200);
        Console.WriteLine($"  draw hash {h:x8}");
        Check(h == 0xb93b60cfu);
        c.Clear();
        Check(Fnv(c.Pixels, 200, 100, 200) == Fnv(new uint[200 * 100], 200, 100, 200));
        Check(c.TextWidth("abc") > 0);
        Check(Canvas.BuiltInFontCount >= 1);
    }

    static readonly double[] Star = { 80, 5, 98, 85, 40, 35, 120, 35, 62, 85 };

    static void Shape(Canvas c, int k, Bitmap b)
    {
        switch (k)
        {
            case 0: c.DrawRRect(10.3, 8.6, 120, 70.2, new Radii(4, 20, 0, 33), 0xE02060C0); break;
            case 1: c.DrawRRectLine(10.3, 8.6, 120, 70.2, Radii.All(18), 2.5, 0xFF20A060); break;
            case 2: c.DrawRRectGrad(10, 10, 130, 70, 12, 0xFF102040, 0x8080C0FF); break;
            case 3: c.DrawRRectGrad(10, 10, 130, 70, 35, 0xFFFF4080, 0x004080FF, true); break;
            case 4: c.DrawShadow(30, 20, 90, 50, 10, 16, 0x80000000); break;
            case 5: c.DrawCircle(50.5, 50.2, 30.7, 0xFFFFC020); c.DrawCircle(120, 30, 1.3, 0xFF000000); break;
            case 6: c.DrawRing(80, 50, 30, 5, 0xC0208080); break;
            case 7: c.DrawArc(50, 50, 30, 6, 0.3, 4.5, 0xFF3060E0); c.DrawArc(120, 50, 20, 30, 2, 2.5, 0xFF905020); break;
            case 8: c.DrawPoly(Star, 0xFF2040A0); break;
            case 9: c.DrawBitmap(b, 10, 10, 100.5, 75.3, 20); break;
            case 10: c.DrawBitmap(b, 10, 10, 24, 16, 0, 0x80FF8080); break;
            case 11: c.DrawBitmap(b, 10, 10, 9, 5, 2); break;
            case 12: c.DrawBitmapPart(b, 4, 2, 8, 8, 100, 10, 40, 40); break;
            default:
                c.ClipPush(20, 15, 80, 50);
                c.DrawRRect(0, 0, 160, 100, 30, 0xFF4080FF);
                c.ClipPush(50, 0, 100, 40);
                c.DrawCircle(70, 40, 30, 0xFFFF4040);
                c.DrawText(30, 30, "clip", true, 0xFF000000);
                c.DrawShadow(40, 20, 60, 30, 8, 10, 0xFF000000);
                c.DrawBitmap(b, 40, 10, 48, 32, 6);
                c.ClipPop();
                c.DrawLine(0, 0, 160, 100, 3, 0xFF00A000);
                c.DrawPoly(Star, 0x80000000);
                c.ClipPop();
                c.DrawFrame(20, 15, 80, 50, 1, 0x80000000);
                break;
        }
    }

    static void Shapes()
    {
        uint[][] want =
        {
            new uint[] { 0xa7b3b4c5, 0x0fb29a82, 0x1a98cf16, 0x1e7b729f, 0x21fd3b05, 0x241ce7fd, 0x6910b0d5, 0x7180b28e, 0x6f33ac30, 0xb48adfa3, 0x796b9105, 0x2d93dc92, 0x1f908fad, 0x23a6cf0c },
            new uint[] { 0x73429a18, 0xd9bd97ab, 0xd031f56c, 0xd2d9fa9a, 0xd639a1a7, 0xa77c3e9e, 0x7bd3fead, 0xac76561e, 0x5bd0648d, 0xd0ac70e1, 0x0bb5dda0, 0xe7c304c7, 0xccef8860, 0xefc1f212 },
            new uint[] { 0x9175788f, 0x6e99ea87, 0x935cc8e1, 0x0b73f68d, 0x23891be5, 0xa28476c1, 0xea64f655, 0x87dc9430, 0x909d11b5, 0x0550bb7a, 0x6455ee86, 0xd8c08e2b, 0xfd0dd1fd, 0x28c27977 },
        };
        double[] sc = { 1, 1.25, 2 };
        var bp = new uint[27 * 16];
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 24; x++)
            {
                uint a = (uint)(x * 255 / 23), v = ((x / 4 + y / 4) & 1) != 0 ? 0xFFFFFFu : 0x2050C0u;
                bp[y * 27 + x] = a << 24 | ((v >> 16 & 255) * a / 255) << 16 | ((v >> 8 & 255) * a / 255) << 8 | (v & 255) * a / 255;
            }
        using var b = Bitmap.Wrap(bp, 24, 16, 27);
        Check(!b.Owned && b.Width == 24 && b.Stride == 27);
        for (int s = 0; s < 3; s++)
        {
            var got = new uint[14];
            int w = (int)(160 * sc[s]), h = (int)(100 * sc[s]);
            for (int k = 0; k < 14; k++)
            {
                using (var c = Canvas.Create(w, h, w + 3, sc[s]))
                {
                    Shape(c, k, b);
                    var px = c.Pixels;
                    got[k] = Fnv(px, c.Stride, c.Height, c.Stride);
                    Check(c.Drawn.X1 > c.Drawn.X0);
                    bool edge = true, pre = true;
                    for (int y = 0; y < h; y++)
                        for (int x = w; x < c.Stride; x++) edge &= px[y * c.Stride + x] == 0;
                    foreach (uint p in px)
                    {
                        uint a = p >> 24;
                        if ((p >> 16 & 255) > a || (p >> 8 & 255) > a || (p & 255) > a) pre = false;
                    }
                    Check(edge);
                    Check(pre, "premultiplied");
                }
                if (k != 13) continue;
                int x0 = (int)(20 * sc[s] + 0.5), y0 = (int)(15 * sc[s] + 0.5), x1 = (int)(100 * sc[s] + 0.5), y1 = (int)(65 * sc[s] + 0.5);
                using var cc = Canvas.Create(w, h, w + 3, sc[s]);
                cc.ClipPush(20, 15, 80, 50);
                Shape(cc, 8, b);
                Shape(cc, 4, b);
                Shape(cc, 9, b);
                cc.DrawText(0, 20, "outside", true, 0xFFFFFFFF);
                var dr = cc.Drawn;
                Check(dr.X0 >= x0 && dr.Y0 >= y0 && dr.X1 <= x1 && dr.Y1 <= y1 && dr.X1 > dr.X0);
                bool inside = true;
                var cp = cc.Pixels;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if ((x < x0 || x >= x1 || y < y0 || y >= y1) && cp[y * cc.Stride + x] != 0) inside = false;
                Check(inside, "drew outside the clip");
                Check(cc.ClipDepth == 1);
                cc.ClipPop();
                Check(cc.ClipDepth == 0);
            }
            Console.WriteLine($"  shape hashes x{sc[s]}: {string.Join(" ", got.Select(v => v.ToString("x8")))}");
            for (int k = 0; k < 14; k++) Check(got[k] == want[s][k], $"shape {k} at x{sc[s]}");
        }
    }

    static void Fonts()
    {
        byte[] ttf = File.ReadAllBytes(Data("sub.ttf")), otf = File.ReadAllBytes(Data("sub.otf"));
        Check(Throws<GoomeException>(() => Font.Load("nope, not a font"u8.ToArray())));
        Check(Throws<GoomeException>(() => Font.Load(ttf.AsSpan(0, 40))));
        using var f = Font.Load(ttf);
        using var o = Font.Load(otf);
        double w = f.Measure(20, "goome!", out double h);
        Check(w > 76.97 && w < 76.98 && h > 23.2 && h < 23.3);
        Check(f.Measure(20, "AV") < f.Measure(20, "A") + f.Measure(20, "V") - 1);
        Check(o.Measure(20, "AV") < o.Measure(20, "A") + o.Measure(20, "V") - 1);
        f.Measure(20, "a\nbb\n", out h);
        Check(h > 3 * 23.2 && h < 3 * 23.3);
        Check(f.LineHeight(20) > 20);
        uint[] want = { 0x412c299fu, 0xbcd42095u };
        Font[] both = { f, o };
        var bufs = new uint[2][];
        for (int k = 0; k < 2; k++)
        {
            using var c = Canvas.Create(200, 120, 240, 1.5);
            c.Raw.Kx1 = 200;
            c.Raw.Ky1 = 120;
            c.DrawText(both[k], 13, 4, 2, "goome! Hié\nAVAWAY Ty.@ \u2603", 0xFFFFFFFF);
            c.DrawText(both[k], 30, 2.3, 24, "Hig", 0xC0FF8040);
            c.Raw.Kx0 = 30;
            c.Raw.Ky0 = 60;
            c.Raw.Kx1 = 90;
            c.Raw.Ky1 = 100;
            c.Raw.Nclip = 1;
            c.DrawText(both[k], 40, 10, 50, "WoW", 0xFF2080FF);
            var px = c.Pixels;
            bool clean = true;
            for (int y = 0; y < 120; y++)
                for (int x = 200; x < 240; x++) clean &= px[y * 240 + x] == 0;
            for (int y = 100; y < 120; y++)
                for (int x = 0; x < 200; x++) clean &= px[y * 240 + x] == 0;
            Check(clean);
            var d = c.Drawn;
            Check(d.X0 >= 0 && d.X1 <= 200 && d.Y1 <= 100 && d.X1 > d.X0);
            uint hv = Fnv(px, 240, 120, 240);
            Console.WriteLine($"  font hash {k} {hv:x8}");
            Check(hv == want[k]);
        }
        for (int k = 0; k < 2; k++)
        {
            using var c = Canvas.Create(240, 120, 240, 1);
            c.Raw.Kx1 = 240;
            c.Raw.Ky1 = 120;
            c.DrawText(both[k], 31, 3, 3, "goome Hi\nmoge", 0xFFFFFFFF);
            bufs[k] = c.Pixels.ToArray();
        }
        long diff = 0, ink = 0;
        for (int i = 0; i < 240 * 120; i++)
        {
            ink += bufs[0][i] >> 24;
            int a = (int)(bufs[0][i] >> 24), bb = (int)(bufs[1][i] >> 24);
            diff += Math.Abs(a - bb);
        }
        Check(ink > 0 && diff * 50 < ink);
        using (var c = Canvas.Create(200, 120, 240, 1))
        {
            for (int cut = 0; cut < otf.Length; cut += 7)
            {
                var bad = (byte[])otf.Clone();
                bad[cut] ^= 0x5A;
                Font? bf = null;
                try
                {
                    bf = Font.Load(bad.AsSpan(0, (cut & 1) != 0 ? bad.Length : Math.Min(cut + 12, bad.Length)));
                }
                catch (GoomeException)
                {
                }
                c.DrawText(bf, 20, 0, 0, "goome! AV", 0xFFFFFFFF);
                bf?.Dispose();
            }
            Check(c.DrawText(Font.BuiltIn, 12, 0, 0, "ab\nabc", 0xFFFFFFFF) == Font.BuiltIn.Measure(12, "abc"));
        }
        var sys = Font.System();
        Console.WriteLine($"  system font: {(sys == null ? "none" : "found")}");
        sys?.Dispose();
    }

    static bool Throws<T>(Action a) where T : Exception
    {
        try
        {
            a();
        }
        catch (T)
        {
            return true;
        }
        return false;
    }

    static void Bitmaps()
    {
        string dir = Data("images");
        int n = 0;
        foreach (var line in File.ReadAllLines(Path.Combine(dir, "cases.txt")))
        {
            var p = line.Split(' ', 5);
            var data = File.ReadAllBytes(Path.Combine(dir, p[0]));
            int w = int.Parse(p[1]), h = int.Parse(p[2]);
            uint hash = Convert.ToUInt32(p[3], 16);
            n++;
            try
            {
                using var b = Bitmap.Load(data);
                uint hv = Fnv(b.Pixels, b.Width, b.Height, b.Stride);
                if (b.Width != w || b.Height != h || hv != hash || !b.Owned) Console.WriteLine($"  {p[4]}: {b.Width}x{b.Height} {hv:x8}");
                Check(b.Width == w && b.Height == h && hv == hash && b.Owned, p[4]);
            }
            catch (GoomeException e)
            {
                Check(false, $"{p[4]}: {e.Message}");
            }
            try
            {
                Bitmap.Load(data.AsSpan(0, data.Length / 2)).Dispose();
            }
            catch (GoomeException)
            {
            }
        }
        Check(n == 19);
        var arith = File.ReadAllBytes(Path.Combine(dir, "arith.bin"));
        var ex = Catch(() => Bitmap.Load(arith));
        Check(ex?.Code == GoomeError.NotSupported && ex.Message.Contains("arithmetic"));
        Check(Catch(() => Bitmap.Load("GIF89a......"u8))?.Code == GoomeError.NotSupported);
        Check(Catch(() => Bitmap.Load(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))?.Code is GoomeError c && c != GoomeError.Ok);
        Check(Catch(() => Bitmap.Load("no/such/file.png")) != null);
        uint[] argb = { 0xFFFF0000u, 0x80FFFFFFu, 0x00123456u, 0x40804020u };
        using var bm = Bitmap.FromArgb(argb, 2, 2);
        var px = bm.Pixels;
        Check(px[0] == 0xFFFF0000u && px[1] == 0x80808080u && px[2] == 0 && px[3] == 0x40201008u);
        Check(Throws<ArgumentException>(() => Bitmap.FromArgb(argb, 3, 2)));
    }

    static GoomeException? Catch(Func<Bitmap> f)
    {
        try
        {
            f().Dispose();
        }
        catch (GoomeException e)
        {
            return e;
        }
        return null;
    }

    static void Scripted()
    {
        using var g = new Goome(new GoomeOptions
        {
            Backend = Backend.Test,
            TestScript = "screen 640x360 2; 1 click 20,20; 1 click 300,300; 2 hotkey alt+j; 3 key Hi\\b; 4 send ping; 5 scroll 30,30 -1; 6 hold 25,25 8; end 10",
        });
        Check(g.BackendName.StartsWith("test"));
        var mons = g.Monitors;
        Check(mons.Length == 1 && mons[0].Area.W == 640 && mons[0].Scale == 2 && mons[0].PixelWidth == 1280);
        Check(g.Hotkey("alt+j", 7));
        Check(g.Keyboard(true));
        int downs = 0, ups = 0, hot = 0, keys = 0, ctl = 0, scroll = 0, held = 0, frames = 0;
        bool quit = false;
        string text = "";
        while (!quit && frames < 100)
        {
            var c = g.Begin(0);
            Check(c != null && c.Scale == 2 && c.Width == 1280);
            if (c == null) break;
            c.DrawRect(10, 10, 40, 40, 0xFF336699);
            g.End(c, new Rect(10, 10, 40, 40));
            frames++;
            if (g.Pointer(out _, out _, out int b) && b != 0) held++;
            while (g.Poll(out var ev, 1))
            {
                switch (ev.Type)
                {
                    case EventType.Down: downs++; break;
                    case EventType.Up: ups++; break;
                    case EventType.Hotkey when ev.Id == 7: hot++; break;
                    case EventType.Key: keys++; text += ev.Text; break;
                    case EventType.Control when ev.Message == "ping": ctl++; break;
                    case EventType.Scroll when ev.Dy == -1: scroll++; break;
                    case EventType.Quit: quit = true; break;
                }
            }
        }
        Check(downs == 2 && ups == 2);
        Check(hot == 1 && keys == 3 && text == "Hi" && ctl == 1 && scroll == 1 && quit && held >= 1);
        var img = g.Capture(0, new Rect(10, 10, 40, 40));
        Check(img.Width == 80 && img.Height == 80 && img.HasSelf);
        Check(img.Pixels[0] == 0xFF336699u && img[79, 79] == 0xFF336699u);
        var arr = img.ToArray();
        Check(arr.Length == 6400 && arr.All(p => p == 0xFF336699u));
        Check(g.AlphaAt(20, 20) == 255 && g.AlphaAt(200, 200) == 0);
        Check(g.TryCapture(5, null, out _) != GoomeError.Ok);
    }

    static void UiRun()
    {
        using var g = new Goome(new GoomeOptions
        {
            Backend = Backend.Test,
            TestScript = "screen 400x360 1; 1 click 35,71; 2 click 29,106; 3 hold 40,157 6; 4 hover 120,157; 7 click 100,213; 8 key ab; 9 key \\n; 10 key zz; " +
                         "11 click 120,273; 13 click 120,206; 15 click 300,20; 16 hold 100,20 20; 18 hover 150,40; end 22",
        });
        using var u = new Ui(g);
        u.Theme.Font = Font.BuiltIn;
        u.Theme.FontSize = 14;
        double x = 10, y = 10, w = 220, h = 0, val = 0;
        bool quit = false, check = false, kbSeen = false;
        int frames = 0, clicks = 0, pick = 0, downs = 0, keysOff = 0;
        string text = "";
        string[] items = { "one", "two", "three" };
        var hs = new uint[3];
        while (!quit && frames < 100)
        {
            while (g.Poll(out var ev))
            {
                if (ev.Type == EventType.Quit) quit = true;
                if (ev.Type == EventType.Down) downs++;
                if (ev.Type == EventType.Key && frames > 10) keysOff++;
                u.Feed(ev);
            }
            var c = u.Begin(0);
            Check(c != null);
            if (c == null) break;
            if (u.Window("test", ref x, ref y, ref w, ref h, WindowFlags.Move))
            {
                if (u.Button("Go")) clicks++;
                u.Checkbox("Check", ref check);
                u.Slider("Val", ref val, 0, 1);
                u.Input("Text", ref text, 16);
                u.Combo("Pick", ref pick, items);
            }
            u.WindowEnd();
            u.End();
            if (frames == 8) kbSeen = text.Length > 0;
            if (frames == 0 || frames == 12 || frames == 21) hs[frames == 0 ? 0 : frames == 12 ? 1 : 2] = Fnv(c.Pixels, c.Width, c.Height, c.Stride);
            frames++;
        }
        Console.WriteLine($"  ui hashes {hs[0]:x8} {hs[1]:x8} {hs[2]:x8}  val {val:0.000} x {x:0} y {y:0} text '{text}' pick {pick} downs {downs}");
        Check(clicks == 1 && check);
        Check(val > 0.45 && val < 0.55);
        Check(kbSeen && text == "ab" && keysOff == 0);
        Check(pick == 1);
        Check(x == 60 && y == 30);
        Check(downs == 7);
        Check(hs[0] == 0x51713294u && hs[1] == 0xb52f16c5u && hs[2] == 0xea01692cu);
    }

    static void UiWidgets()
    {
        using var g = new Goome(new GoomeOptions { Backend = Backend.Test, TestScript = "screen 500x500 1; end 6" });
        using var u = new Ui(g);
        var t = u.Theme;
        t.Light();
        uint lightBg = t.Bg;
        t.Dark();
        Check(t.Bg != lightBg);
        t.Radius = 3;
        Check(t.Raw.Radius == 3);
        using var bm = Bitmap.FromArgb(new uint[16 * 16], 16, 16);
        double x = 10, y = 10, w = 300, h = 0, v = 0.5;
        bool open = true, a = true, b = false, more = true;
        int r = 1, n = 5, tab = 0, sel = 0, frames = 0;
        uint col = 0xFF4FA3FF;
        string name = "goome";
        var buf = new byte[32];
        string[] tabs = { "One", "Two" }, items = { "a", "b" };
        bool quit = false;
        while (!quit && frames < 20)
        {
            while (g.Poll(out var ev))
            {
                if (ev.Type == EventType.Quit) quit = true;
                u.Feed(ev);
            }
            var c = u.Begin(0);
            if (c == null) break;
            if (u.Window("all", ref x, ref y, ref w, ref h, WindowFlags.Move | WindowFlags.Close | WindowFlags.Collapse, ref open))
            {
                u.Tabs("tabs", tabs, ref tab);
                u.PushId("p");
                u.ScrollBegin("s", 0);
                u.Heading("Heading");
                u.Row(2);
                u.Button("A");
                u.Button("B");
                u.Label("100% sure, %s %d stays as is");
                u.Wrapped("wrapped text that goes on for a bit so it has to wrap somewhere");
                u.Separator();
                u.Space(4);
                u.Width(100);
                u.Checkbox("a", ref a);
                u.SameLine();
                u.Toggle("b", ref b);
                u.Radio("r0", ref r, 0);
                u.Radio("r1", ref r, 1);
                u.Slider("v", ref v, 0, 1, "%.1f");
                u.SliderInt("n", ref n, 0, 10);
                u.Input("name", ref name);
                u.Input("buf", buf);
                u.Combo("c", ref sel, items);
                u.Selectable("sel", true);
                u.Tooltip("tip");
                _ = u.Hovered;
                if (u.Collapsing("more", ref more)) u.Progress(0.5, "half");
                u.Progress(-1);
                u.Color("col", ref col);
                u.Image(bm, 16, 16, 4);
                u.ImageButton("ib", bm, 16, 16);
                u.ScrollEnd();
                u.PopId();
            }
            u.WindowEnd();
            u.Hit(0, 0, 1, 1);
            u.End();
            _ = u.Animating;
            frames++;
        }
        Check(frames >= 6 && quit);
        Check(open && a && !b && r == 1 && n == 5 && name == "goome" && col == 0xFF4FA3FF);
    }

    static void Disposal()
    {
        var g = new Goome(new GoomeOptions { Backend = Backend.Test, TestScript = "screen 200x200 1; end 3" });
        var u = new Ui(g);
        var own = Font.Load(File.ReadAllBytes(Data("sub.ttf")));
        u.Theme.Font = own;
        Check(ReferenceEquals(u.Theme.Font, own));
        var c = g.Begin(0)!;
        g.End(c);
        g.Poll(out _);
        var again = g.Begin(0)!;
        Check(ReferenceEquals(again, c));
        g.End(again);
        g.Dispose();
        Check(u.IsDisposed && g.IsDisposed);
        g.Dispose();
        u.Dispose();
        Check(Throws<ObjectDisposedException>(() => _ = g.Monitors));
        Check(Throws<ObjectDisposedException>(() => u.Button("x")));
        Check(Throws<ObjectDisposedException>(() => c.DrawRect(0, 0, 1, 1, 0)));
        own.Dispose();
        own.Dispose();
        Check(Throws<ObjectDisposedException>(() => own.Measure(10, "x")));
        var bm = Bitmap.FromArgb(new uint[4], 2, 2);
        bm.Dispose();
        bm.Dispose();
        Check(bm.IsDisposed && Throws<ObjectDisposedException>(() => _ = bm.Width));
        var cv = Canvas.Create(4, 4);
        cv.Dispose();
        cv.Dispose();
        Check(Throws<ObjectDisposedException>(() => _ = cv.Pixels));
        for (int i = 0; i < 200; i++)
        {
            using var b2 = Bitmap.FromArgb(new uint[64 * 64], 64, 64);
            using var c2 = Canvas.Create(64, 64);
            c2.DrawBitmap(b2, 0, 0, 64, 64);
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    static void Errors()
    {
        var other = OperatingSystem.IsWindows() ? Backend.X11 : Backend.Win32;
        if (new[] { "GOOME_BACKEND", "GOOME_TEST_FRAMES", "GOOME_TEST_SCRIPT", "GOOME_TEST_SHOTS" }.All(v => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(v))))
        {
            try
            {
                using var g = new Goome(new GoomeOptions { Backend = other });
                Check(false, "opened a backend that cant exist here");
            }
            catch (GoomeException e)
            {
                Check(e.Code != GoomeError.Ok && e.Message.Length > 0);
            }
        }
        Check(Goome.StrError(GoomeError.Again).Length > 0);
        Check(Goome.Send(Path.Combine(root, "build", "no-such-dir"), "hi") != GoomeError.Ok);
        using var t = new Goome(new GoomeOptions { Backend = Backend.Test, TestScript = "end 1" });
        Check(!t.Hotkey("alt+f13", 1) && t.LastError.Contains("combo"));
        Check(Throws<ArgumentException>(() => Goome.SavePpm("x.ppm", new uint[3], 2, 2, 2)));
        string ppm = Path.Combine(root, "build", "test.ppm");
        Check(Goome.SavePpm(ppm, new uint[] { 0xFFFF0000, 0xFF00FF00, 0xFF0000FF, 0xFFFFFFFF }, 2, 2, 2) == GoomeError.Ok && File.Exists(ppm));
        using (var b = Bitmap.Load(ppm)) Check(b.Width == 2 && b.Pixels[0] == 0xFFFF0000 && b.Pixels[3] == 0xFFFFFFFF);
        File.Delete(ppm);
        Check(t.ControlPath == null);
    }

    static void Offscreen()
    {
        using var g = Goome.OpenOffscreen(200, 100, 2, "engine");
        Check(g.BackendName == "offscreen" && g.Mode == Mode.Offscreen);
        Check(g.Has(Caps.Clipboard | Caps.Cursor | Caps.Keyboard) && (g.Caps & (Caps.Capture | Caps.Window | Caps.GlobalPointer)) == 0);
        var m = g.Monitors;
        Check(m.Length == 1 && m[0].Area.W == 200 && m[0].Area.H == 100 && m[0].Scale == 2 && m[0].PixelWidth == 400 && m[0].Name == "engine");
        Check(!g.TryFrame(out _));
        Check(Catch(() => g.Frame()) == GoomeError.Again);
        var c = g.Begin(0);
        Check(c != null && c.Width == 400 && c.Height == 200 && c.Scale == 2);
        if (c == null) return;
        c.DrawRect(10, 10, 20, 20, 0x80FF0000);
        Check(g.End(c, new Rect(10, 10, 20, 20)) == GoomeError.Ok);
        Check(g.TryFrame(out var f) && f.Width == 400 && f.Height == 200 && f.Stride == 400 && f.Area.W == 200 && f.Pointer != IntPtr.Zero);
        Check(f[25, 25] == 0x80800000u && f[0, 0] == 0);
        Check(g.Hit(15, 15) && !g.Hit(30, 30) && !g.Hit(9.5, 15));
        Check(g.AlphaAt(15, 15) == 0x80);

        Check(g.Push(Event.Down(12, 13)) && g.Push(Event.KeyPress('q', Mods.None, "q")));
        Check(g.Poll(out var ev) && ev.Type == EventType.Down && ev.X == 12 && ev.Y == 13 && ev.Button == 1);
        Check(g.Poll(out ev) && ev.Type == EventType.Key && ev.Key == 'q' && ev.Text == "q");
        long t0 = Goome.NowMs;
        Check(!g.Poll(out _, 200));
        Check(Goome.NowMs - t0 < 100);
        int pushed = 0;
        while (pushed < 1000 && g.Push(new Event(EventType.Key))) pushed++;
        Check(pushed > 10 && pushed < 1000);
        while (g.Poll(out _)) { }

        Check(g.Resize(300, 150));
        Check(g.Poll(out ev) && ev.Type == EventType.Monitors);
        m = g.Monitors;
        Check(m[0].Area.W == 300 && m[0].PixelWidth == 600 && m[0].PixelHeight == 300);
        c = g.Begin(0);
        Check(c != null && c.Width == 600 && c.Pixels[25 * 600 + 25] == 0);
        if (c != null) g.End(c);
        Check(!g.Hit(15, 15));
        Check(g.Resize(300, 150, 1) && g.Monitors[0].PixelWidth == 300);
        Check(!g.Resize(0, 10, 1));
        Check(g.SetSize(320, 160) && g.Monitors[0].Area.W == 320);
        Check(g.SetTitle("renamed") && g.Monitors[0].Name == "renamed");
        Check(!g.GetPosition(out _, out _));

        Check(g.Clipboard == "" && g.GetClipboard() == "");
        Check(g.SetClipboard("hello clipboard \u2603") && g.Clipboard == "hello clipboard \u2603");
        g.Clipboard = "again";
        Check(g.GetClipboard() == "again");
        Check(g.Cursor == CursorShape.Arrow);
        g.Cursor = CursorShape.IBeam;
        g.Cursor = (CursorShape)99;
        Check(g.Cursor == CursorShape.IBeam);
        Check(!g.Hotkey("alt+h", 1));
        Check(g.TryCapture(0, new Rect(0, 0, 10, 10), out _) == GoomeError.NotSupported);

        using (var g2 = new Goome(new GoomeOptions { Backend = Backend.Offscreen }))
        {
            Check(g2.Mode == Mode.Offscreen && g2.Monitors[0].Area.W == 960 && g2.Monitors[0].Area.H == 640 && g2.Monitors[0].Scale == 1);
            Check(g2.Clipboard == "");
        }
        Check(Throws<GoomeException>(() => new Goome(new GoomeOptions { Mode = (Mode)7 })));
        Check(Throws<GoomeException>(() => Goome.OpenOffscreen(100000, 10)));
    }

    static GoomeError? Catch(Action a)
    {
        try
        {
            a();
        }
        catch (GoomeException e)
        {
            return e.Code;
        }
        return null;
    }

    static void FrameHash()
    {
        string exe = Path.Combine(root, "build", OperatingSystem.IsWindows() ? "frame.exe" : "frame");
        var p = Process.Start(new ProcessStartInfo(exe) { RedirectStandardOutput = true, UseShellExecute = false })!;
        string want = p.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit();
        Check(p.ExitCode == 0 && want.Length > 0);

        using var g = Goome.OpenOffscreen(360, 330, 2, "engine");
        using var u = new Ui(g);
        u.Theme.Font = Font.BuiltIn;
        u.Theme.FontSize = 14;
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
        uint hv = Fnv(f.Pixels, f.Width, f.Height, f.Stride);
        string got = $"{hv:x8} {f.Width}x{f.Height} check {(check ? 1 : 0)} volume {volume.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} name {name} hit {B(g.Hit(40, 109))}{B(g.Hit(30, 30))}{B(g.Hit(5, 320))} cursor {(int)g.Cursor}";
        Console.WriteLine($"  C:  {want}\n  C#: {got}");
        Check(got == want, "offscreen frame matches the C one");
        Check(check && name == "goome from an engine");
    }

    static int B(bool b) => b ? 1 : 0;

    static void Events()
    {
        using var g = Goome.OpenOffscreen(100, 100);
        Check(g.PushText("aB\u2603\U0001F600") == 4);
        Check(g.Poll(out var e) && e.Key == 'a' && e.Mods == Mods.None && e.Text == "a");
        Check(g.Poll(out e) && e.Key == 'b' && e.Mods == Mods.Shift && e.Text == "B");
        Check(g.Poll(out e) && e.Key == 0 && e.Text == "\u2603");
        Check(g.Poll(out e) && e.Text == "\U0001F600");
        g.Push(Event.Char('c', Mods.Ctrl));
        Check(g.Poll(out e) && e.Key == 'c' && e.Mods == Mods.Ctrl && e.Text == "");
        g.Push(Event.Scroll(5, 6, -2));
        Check(g.Poll(out e) && e.Type == EventType.Scroll && e.Dy == -2 && e.X == 5);
        g.Push(Event.Up(1, 2, 3, Mods.Alt));
        Check(g.Poll(out e) && e.Type == EventType.Up && e.Button == 3 && e.Mods == Mods.Alt);
        g.Push(Event.Focus(false));
        Check(g.Poll(out e) && e.Type == EventType.Focus && !e.Focused);
        g.Push(Event.Focus(true));
        Check(g.Poll(out e) && e.Focused);
        g.Push(Event.Close());
        Check(g.Poll(out e) && e.Type == EventType.Close);
        g.Push(Event.Control(new string('x', 100)));
        Check(g.Poll(out e) && e.Type == EventType.Control && e.Message == new string('x', 63));
        g.Push(Event.KeyPress(Keys.Left, Mods.Shift, "toolongtext"));
        Check(g.Poll(out e) && e.Key == Keys.Left && e.Text == "toolong");
        Check(Event.Focus(true).ToString() == "Focus gained");
    }

    static void WindowMode()
    {
        using var g = new Goome(new GoomeOptions
        {
            Backend = Backend.Test,
            Mode = Mode.Window,
            Title = "notes",
            Width = 300,
            Height = 200,
            MinWidth = 250,
            MinHeight = 120,
            TestScript = "screen 1x1 2; 1 click 250,150; 1 click 350,150; 2 resize 400x300; 3 close; 4 focus 0; 5 key a; 6 focus 1; 7 key b; " +
                         "8 drop 10,20 /home/someone/a/really/long/folder/name/that/goes/past/63/bytes/file.txt; 9 resize 100x100; end 11",
        });
        Check(g.Mode == Mode.Window && g.Has(Caps.Window));
        var m = g.Monitors[0];
        Check(m.Area.W == 300 && m.Area.H == 200 && m.Scale == 2 && m.PixelWidth == 600 && m.Name == "notes");
        int downs = 0, mons = 0, closes = 0, frames = 0, focus = 0, drops = 0, cw = 0;
        string keys = "", drop = "", full = "";
        bool quit = false;
        while (!quit && frames < 100)
        {
            var c = g.Begin(0);
            Check(c != null);
            if (c == null) break;
            if (frames == 3) cw = c.Width;
            c.DrawRect(0, 0, 300, 200, 0xFF202830);
            g.End(c);
            frames++;
            while (g.Poll(out var ev, 1))
            {
                switch (ev.Type)
                {
                    case EventType.Down: downs++; break;
                    case EventType.Monitors: mons++; break;
                    case EventType.Close: closes++; break;
                    case EventType.Focus: focus = focus * 10 + 1 + ev.Id; break;
                    case EventType.Key: keys += ev.Text; break;
                    case EventType.Drop:
                        drops++;
                        drop = ev.Message;
                        full = g.DropPath ?? "";
                        Check(ev.X == 10 && ev.Y == 20);
                        break;
                    case EventType.Quit: quit = true; break;
                }
            }
        }
        Check(downs == 1 && mons == 2 && closes == 1 && focus == 12 && keys == "b" && drops == 1 && cw == 800);
        Check(drop.Length == 63 && full.StartsWith(drop) && full.Length > 63 && full.EndsWith("/file.txt"));
        m = g.Monitors[0];
        Check(m.Area.W == 250 && m.Area.H == 120);
        Check(g.TryFrame(out var f) && f.Width == 500 && f.Pixels[0] == 0xFF202830u);
        Check(!g.Hit(5, 5));
        Check(g.SetTitle("renamed") && g.Monitors[0].Name == "renamed");
        Check(g.SetSize(500, 400) && g.Monitors[0].Area.W == 500 && g.Monitors[0].PixelWidth == 1000);
        Check(g.SetPosition(30, 40) && g.GetPosition(out int x, out int y) && x == 30 && y == 40);
        Check(g.Maximize() && g.State == WindowState.Maximized && g.Restore() && g.State == WindowState.Normal);
        Check(!g.SetState((WindowState)9));
        Check(g.Drag(Edges.Left) && g.Show() && g.Hide());
        Check(g.SetClipboard("copied") && g.Clipboard == "copied");
        g.Cursor = CursorShape.ResizeEW;
        Check(g.Cursor == CursorShape.ResizeEW);
        Check(!g.Resize(10, 10, 1));

        using var o = new Goome(new GoomeOptions { Backend = Backend.Test, TestScript = "end 1" });
        Check(o.Mode == Mode.Overlay && !o.Has(Caps.Window));
        Check(!o.SetTitle("x") && o.State == WindowState.Normal && o.DropPath == null);
        if (OperatingSystem.IsLinux())
            Check(Catch(() => new Goome(new GoomeOptions { Backend = Backend.Gnome, Mode = Mode.Window }).Dispose()) == GoomeError.NotSupported);
    }

    static void AppMode()
    {
        int frames = 0, closes = 0, events = 0;
        using (var app = new GoomeApp(new GoomeOptions
        {
            Backend = Backend.Test,
            Mode = Mode.Window,
            Title = "notes",
            Width = 400,
            Height = 300,
            TestScript = "screen 1x1 1; 2 click 20,20; 3 close; 5 key hi; 6 close; end 30",
        }))
        {
            app.Flags = AppFlags.MenuBar | AppFlags.StatusBar;
            app.OnClose = () => ++closes >= 2;
            app.OnEvent += _ => events++;
            Check(app.Goome.Mode == Mode.Window && app.Title == "notes");
            string text = "";
            app.Run(ui =>
            {
                frames++;
                if (ui.MenuBarBegin())
                {
                    if (ui.MenuBegin("File"))
                    {
                        if (ui.MenuItem("Quit", "ctrl+q")) app.Quit();
                        ui.MenuEnd();
                    }
                    ui.MenuBarEnd();
                }
                ui.Input("Text", ref text);
                ui.StatusBar($"frame {frames}");
            });
            Check(!app.Running && closes == 2 && events >= 4);
            Check(frames >= 6 && frames < 30 && app.Frames == frames);
            app.Invalidate();
            app.SetTitle("renamed");
            Check(app.Goome.Monitors[0].Name == "renamed");
        }

        using var quit = new GoomeApp(new GoomeOptions { Backend = Backend.Test, Mode = Mode.Window, TestScript = "end 4" });
        int n = 0;
        quit.Run(_ => n++);
        Check(n >= 3 && n <= 5);
    }

    static Dictionary<string, string>? cOut;

    static string CLine(string prefix)
    {
        if (cOut == null)
        {
            cOut = new Dictionary<string, string>();
            string exe = Path.Combine(root, "build", OperatingSystem.IsWindows() ? "kit.exe" : "kit");
            var p = Process.Start(new ProcessStartInfo(exe) { RedirectStandardOutput = true, UseShellExecute = false })!;
            string text = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            Check(p.ExitCode == 0, "the C kit program passed its own checks");
            foreach (var l in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                int sp = l.IndexOf(' ', l.IndexOf(' ') + 1);
                if (sp > 0) cOut[l[..sp]] = l[(sp + 1)..];
            }
        }
        return cOut.GetValueOrDefault(prefix, "missing");
    }

    static void Push(Goome g, EventType t, double x, double y, int button, int key, Mods mods, string? text)
    {
        var r = Event.KeyPress(key, mods, text).Raw;
        r.Type = (int)t;
        r.X = x;
        r.Y = y;
        r.Button = button;
        g.Push(new Event(r));
    }

    static string Str(byte[] b)
    {
        int n = Array.IndexOf(b, (byte)0);
        return System.Text.Encoding.UTF8.GetString(b, 0, n < 0 ? b.Length : n);
    }

    static void Set(byte[] b, string s)
    {
        Array.Clear(b);
        var e = System.Text.Encoding.UTF8.GetBytes(s);
        Array.Copy(e, b, Math.Min(e.Length, b.Length - 1));
    }

    sealed class Kit
    {
        public Goome G = null!;
        public Ui U = null!;
        public int Opens, Deep, A, B, Ok, Ctx, Closes;
        public bool Chk, Mopen, Cb, FocusA, FocusIn, FocusB;
        public byte[] Name = new byte[32];
        public string? Toast;
        public int ToastMs;

        public void Ev(EventType t, double x, double y, int button, int key = 0, Mods mods = Mods.None, string? text = null) => Push(G, t, x, y, button, key, mods, text);

        public void Click(double x, double y, int button)
        {
            Ev(EventType.Motion, x, y, 0);
            Ev(EventType.Down, x, y, button);
            Ev(EventType.Up, x, y, button);
        }

        public void Key(int key, Mods mods = Mods.None)
        {
            string? t = key >= 32 && key < 127 && (mods & (Mods.Ctrl | Mods.Alt | Mods.Super)) == 0 ? ((char)key).ToString() : null;
            Ev(EventType.Key, 0, 0, 0, key, mods, t);
        }

        public void Type(string s)
        {
            foreach (char c in s) Key(c);
        }

        public uint Frame()
        {
            while (G.Poll(out var ev))
            {
                if (ev.Type == EventType.Close) Closes++;
                U.Feed(ev);
            }
            var c = U.Begin(0);
            if (c == null) return 0;
            if (Toast != null)
            {
                U.Toast(Toast, ToastMs);
                Toast = null;
            }
            U.App("Kit", AppFlags.TitleBar | AppFlags.MenuBar | AppFlags.StatusBar);
            if (U.MenuBarBegin())
            {
                if (U.MenuBegin("File"))
                {
                    if (U.MenuItem("Open", "Ctrl+O")) Opens++;
                    U.MenuItem("Check", null, ref Chk);
                    U.MenuSeparator();
                    if (U.MenuBegin("More"))
                    {
                        if (U.MenuItem("Deep")) Deep++;
                        U.MenuEnd();
                    }
                    U.MenuEnd();
                }
                if (U.MenuBegin("Edit"))
                {
                    U.MenuItem("Copy", "Ctrl+C");
                    U.MenuEnd();
                }
                U.MenuBarEnd();
            }
            if (U.Button("A")) A++;
            FocusA = U.Focused;
            U.Input("Name", Name);
            FocusIn = U.Focused;
            if (U.Button("B")) B++;
            FocusB = U.Focused;
            if (U.ContextBegin("bctx"))
            {
                if (U.MenuItem("Ctx")) Ctx++;
                U.ContextEnd();
            }
            U.Checkbox("C", ref Cb);
            if (U.ModalBegin("Dialog", ref Mopen))
            {
                U.Label("sure?");
                if (U.Button("OK"))
                {
                    Ok++;
                    U.PopupClose();
                }
                U.ModalEnd();
            }
            U.StatusBar("ready");
            U.AppEnd();
            U.End();
            return Fnv(c.Pixels, c.Width, c.Height, c.Stride);
        }

        public void Frames(int n)
        {
            for (int i = 0; i < n; i++) Frame();
        }

        public double Ink(int x0, int y0, int x1, int y1)
        {
            if (!G.TryFrame(out var f)) return -1;
            double s = 0;
            for (int y = y0; y < y1 && y < f.Height; y++)
                for (int x = x0; x < x1 && x < f.Width; x++) s += (f[x, y] >> 8) & 255;
            return s;
        }
    }

    static void KitTest()
    {
        using var g = new Goome(new GoomeOptions
        {
            Backend = Backend.Test, Mode = Mode.Window, Title = "kit", Width = 420, Height = 300,
            WindowStyle = WindowStyle.Borderless | WindowStyle.Resizable, TestScript = "end 100000",
        });
        using var u = new Ui(g);
        u.Theme.Font = Font.BuiltIn;
        u.Theme.FontSize = 14;
        var k = new Kit { G = g, U = u };
        var hs = new uint[6];
        k.Frames(3);
        hs[0] = k.Frame();

        k.Click(30, 53, 1);
        k.Frames(11);
        hs[1] = k.Frame();
        Check(g.Hit(100, 89));
        k.Click(60, 89, 1);
        k.Frames(2);
        Check(k.Opens == 1);
        k.Click(30, 53, 1);
        k.Frames(3);
        k.Key(Keys.Esc);
        k.Frames(2);
        k.Click(60, 89, 1);
        k.Frames(2);
        Check(k.Opens == 1);

        k.Click(30, 53, 1);
        k.Frames(3);
        k.Key(Keys.Down); k.Key(Keys.Down); k.Key(Keys.Enter);
        k.Frames(2);
        Check(k.Chk && k.Opens == 1);

        k.Click(30, 53, 1);
        k.Frames(3);
        k.Ev(EventType.Motion, 60, 152, 0);
        k.Frames(12);
        hs[2] = k.Frame();
        k.Click(240, 152, 1);
        k.Frames(2);
        Check(k.Deep == 1);
        k.Click(30, 53, 1);
        k.Frames(3);
        k.Key(Keys.Up); k.Key(Keys.Right);
        k.Frames(3);
        k.Key(Keys.Esc);
        k.Frames(2);
        k.Key(Keys.Right);
        k.Frames(3);
        k.Key(Keys.Enter);
        k.Frames(2);
        Check(k.Deep == 2 && k.Opens == 1 && k.Chk);
        k.Click(30, 53, 1);
        k.Frames(2);
        k.Key(Keys.Right);
        k.Frames(3);
        k.Key(Keys.Left);
        k.Frames(3);
        k.Key(Keys.Enter);
        k.Frames(2);
        Check(k.Opens == 1);
        k.Key(Keys.Down); k.Key(Keys.Enter);
        k.Frames(2);
        Check(k.Opens == 2);

        k.Mopen = true;
        k.Ev(EventType.Motion, 300, 250, 0);
        k.Frames(13);
        hs[3] = k.Frame();
        k.Click(32, 98, 1);
        k.Frames(2);
        Check(k.A == 0 && k.Mopen);
        k.Key(Keys.Esc);
        k.Frames(2);
        Check(!k.Mopen);
        k.Mopen = true;
        k.Frames(12);
        k.Click(64, 167, 1);
        k.Frames(2);
        Check(k.Ok == 1 && !k.Mopen);

        k.Key(Keys.Tab);
        k.Frames(2);
        Check(k.FocusA && !k.FocusIn);
        k.Key(Keys.Tab);
        k.Frames(2);
        Check(k.FocusIn && !k.FocusA);
        k.Key(Keys.Tab);
        k.Frames(2);
        Check(k.FocusB);
        k.Key(Keys.Tab, Mods.Shift);
        k.Frames(2);
        Check(k.FocusIn && !k.FocusB);
        k.Key(Keys.Tab);
        k.Frames(2);
        k.Key(Keys.Space);
        k.Frames(1);
        hs[4] = k.Frame();
        Check(k.B == 1 && k.FocusB);
        k.Key(Keys.Tab); k.Key(Keys.Enter);
        k.Frames(2);
        Check(!k.Cb && k.B == 2);
        k.Key(Keys.Space);
        k.Frames(2);
        Check(k.Cb);
        k.Key(Keys.Tab);
        k.Frames(2);
        Check(k.FocusA);
        k.Key(Keys.Esc);
        k.Frames(2);
        Check(!k.FocusA);

        k.Click(210, 159, 1);
        k.Frames(1);
        Check(g.Cursor == CursorShape.IBeam);
        k.Type("hello world");
        k.Frames(1);
        k.Key('a', Mods.Ctrl); k.Key('c', Mods.Ctrl);
        k.Frames(1);
        Check(g.Clipboard == "hello world");
        k.Key(Keys.End);
        for (int i = 0; i < 5; i++) k.Key(Keys.Left, Mods.Shift);
        k.Frames(1);
        hs[5] = k.Frame();
        k.Key('x', Mods.Ctrl);
        k.Frames(1);
        Check(Str(k.Name) == "hello " && g.Clipboard == "world");
        k.Key('v', Mods.Ctrl); k.Key('v', Mods.Ctrl);
        k.Frames(1);
        Check(Str(k.Name) == "hello worldworld");
        k.Key('z', Mods.Ctrl);
        k.Frames(1);
        Check(Str(k.Name) == "hello world");
        g.Clipboard = "a\nb\tc";
        k.Key(Keys.Home); k.Key('v', Mods.Ctrl);
        k.Frames(1);
        Check(Str(k.Name) == "a b chello world");
        k.Key(Keys.Backspace, Mods.Ctrl); k.Key(Keys.Delete);
        k.Frames(1);
        Check(Str(k.Name) == "a b ello world");
        for (int i = 0; i < 2; i++)
        {
            k.Ev(EventType.Down, 70, 159, 1);
            k.Ev(EventType.Up, 70, 159, 1);
        }
        k.Frames(1);
        k.Type("hi");
        k.Frames(1);
        Check(Str(k.Name) == "a b hi world");
        k.Key(Keys.Enter);
        k.Frames(1);

        k.Ev(EventType.Motion, 418, 150, 0);
        k.Frames(1);
        Check(g.Cursor == CursorShape.ResizeEW);
        k.Ev(EventType.Motion, 418, 298, 0);
        k.Frames(1);
        Check(g.Cursor == CursorShape.ResizeNWSE);
        k.Ev(EventType.Motion, 100, 250, 0);
        k.Frames(1);
        Check(g.Cursor == CursorShape.Arrow);

        k.Click(32, 196, 3);
        k.Frames(12);
        k.Click(60, 216, 1);
        k.Frames(2);
        Check(k.Ctx == 1 && k.B == 2);

        double b0 = k.Ink(220, 200, 420, 272);
        var ink = new double[4];
        k.Toast = "saved";
        k.ToastMs = 400;
        int[] at = { 2, 10, 22, 30 };
        for (int i = 0, f = 0; i <= 30; i++)
        {
            k.Frame();
            if (f < 4 && i == at[f]) ink[f++] = k.Ink(220, 200, 420, 272) - b0;
        }
        string inks = string.Join(" ", ink.Select(v => v.ToString("0", System.Globalization.CultureInfo.InvariantCulture)));
        Check(inks == CLine("toast ink"), $"toast ink {inks} vs C {CLine("toast ink")}");
        Check(ink[0] > 0 && ink[1] > ink[0] && ink[2] < ink[1] && ink[2] > 0 && ink[3] == 0);

        for (int i = 0; i < 2; i++)
        {
            k.Ev(EventType.Down, 150, 18, 1);
            k.Ev(EventType.Up, 150, 18, 1);
        }
        k.Frames(2);
        Check(g.State == WindowState.Maximized);
        k.Click(397, 18, 1);
        k.Frames(2);
        Check(k.Closes == 1);

        string got = string.Join(" ", hs.Select(h => h.ToString("x8")));
        Console.WriteLine($"  kit C:  {CLine("kit hashes")}\n  kit C#: {got}");
        Check(got == CLine("kit hashes"), "kit frames match the C ones");
        Check(got == "aef05fe5 26bd1b58 bcba3bdd bbbab1e5 863c70c5 3f3f63e1", "kit frames match c-goome's unit test");
    }

    sealed class W2
    {
        public Goome G = null!;
        public Ui U = null!;
        public int Page, SortCol, SortChanges, LcFirst, LcLast, Links, Ival, Dropped;
        public EditFlags Flags;
        public bool TreeOpen, SortDesc;
        public byte[] Ed = new byte[256], Ro = new byte[64], Small = new byte[12], Got = new byte[32];
        public double Fv, Nv, Frac;
        public uint Hash;

        static readonly string[] names = { "pear", "apple", "fig", "kiwi", "plum" };
        static readonly int[] sizes = { 3, 9, 1, 7, 5 };
        static readonly float[] line = { 2, 5, 3, 8, 6, 9, 4, 7, 5, 10, 6, 3 };

        public void Ev(EventType t, double x, double y, int key = 0, Mods mods = Mods.None, string? text = null) => Push(G, t, x, y, 1, key, mods, text);

        public void Click(double x, double y)
        {
            Ev(EventType.Motion, x, y);
            Ev(EventType.Down, x, y);
            Ev(EventType.Up, x, y);
        }

        public void Key(int key, Mods mods = Mods.None)
        {
            string? t = key >= 32 && key < 127 && (mods & (Mods.Ctrl | Mods.Alt | Mods.Super)) == 0 ? ((char)key).ToString() : null;
            Ev(EventType.Key, 0, 0, key, mods, t);
        }

        public void Scroll(double x, double y, int dy) => G.Push(Event.Scroll(x, y, dy));

        public void Type(string s)
        {
            foreach (char c in s) Key(c);
        }

        public void Frame()
        {
            while (G.Poll(out var ev)) U.Feed(ev);
            var c = U.Begin(0);
            if (c == null) return;
            U.App("W");
            if (Page == 0)
            {
                U.TextEdit("ed", Ed, 200, Flags);
                U.TextEdit("ro", Ro, 60, EditFlags.ReadOnly);
                U.TextEdit("small", Small, 50);
            }
            else if (Page == 1)
            {
                int[] order = { 0, 1, 2, 3, 4 };
                for (int i = 0; i < 5; i++)
                    for (int j = i + 1; j < 5; j++)
                    {
                        int a = order[i], b = order[j];
                        bool sw = SortCol == 1 ? sizes[a] > sizes[b] : string.CompareOrdinal(names[a], names[b]) > 0;
                        if (SortDesc) sw = !sw;
                        if (sw)
                        {
                            order[i] = b;
                            order[j] = a;
                        }
                    }
                if (U.TableBegin("tab", 2, 0, TableFlags.Sortable | TableFlags.Stripes | TableFlags.Borders | TableFlags.Resizable))
                {
                    U.TableColumn("Name");
                    U.TableColumn("Size", 120);
                    if (U.TableSort(ref SortCol, ref SortDesc)) SortChanges++;
                    for (int i = 0; i < 5; i++)
                    {
                        U.TableRow();
                        U.Label(names[order[i]]);
                        U.TableNext();
                        U.Label(sizes[order[i]].ToString());
                    }
                }
                U.TableEnd();
                if (U.Tree("root", ref TreeOpen))
                {
                    U.Label("child a");
                    U.Label("child b");
                    U.TreeEnd();
                }
                U.ScrollBegin("lc", 100);
                U.ListClip(1000, 0, out LcFirst, out LcLast);
                for (int i = LcFirst; i < LcLast; i++) U.Selectable($"row {i}", false);
                U.ScrollEnd();
            }
            else if (Page == 2)
            {
                U.Row(2);
                U.DragFloat("Float", ref Fv, 0.1, 0, 100, "%.1f");
                U.DragInt("Int", ref Ival, 0.5, -10, 10);
                U.Number("Num", ref Nv, 2, "%.1f");
                if (U.Link("a link")) Links++;
                U.SameLine();
                U.Badge("new", 0);
                U.SameLine();
                U.Badge("light", 0xFFF2C94C);
                U.Row(2);
                U.PlotLines("Lines", line, 0, 0, 60);
                U.PlotBars("Bars", line, 0, 12, 60);
            }
            else
            {
                U.SplitBegin("sp", false, ref Frac);
                U.Selectable("drag me", false);
                U.DragSource("thing", "payload");
                U.SplitNext();
                U.Button("drop here");
                int n = U.DropTarget("thing", Got);
                if (n != 0) Dropped = n;
                U.Button("wrong type");
                U.DropTarget("other", Span<byte>.Empty);
                U.SplitEnd();
            }
            U.AppEnd();
            U.End();
            Hash = Fnv(c.Pixels, c.Width, c.Height, c.Stride);
        }

        public void Frames(int n)
        {
            for (int i = 0; i < n; i++) Frame();
        }
    }

    static void Widgets2()
    {
        using var g = new Goome(new GoomeOptions { Mode = Mode.Offscreen, Title = "w2", Width = 480, Height = 360 });
        using var u = new Ui(g);
        u.Theme.Font = Font.BuiltIn;
        u.Theme.FontSize = 14;
        u.Theme.AnimMs = 0;
        var k = new W2 { G = g, U = u, Flags = EditFlags.LineNumbers, TreeOpen = true, Frac = 0.4 };
        Set(k.Ro, "read only text");
        var hs = new uint[8];
        string ed() => Str(k.Ed);

        k.Frames(2);
        k.Click(100, 25);
        k.Frames(1);
        k.Type("hello world");
        k.Key(Keys.Enter);
        k.Type("  foo");
        k.Key(Keys.Enter);
        k.Type("bar");
        k.Frames(2);
        Check(ed() == "hello world\n  foo\n  bar");
        k.Key('z', Mods.Ctrl);
        k.Frames(1);
        Check(ed() == "hello world\n  foo\n  ");
        k.Key('z', Mods.Ctrl);
        k.Frames(1);
        Check(ed() == "hello world\n  foo");
        k.Key('y', Mods.Ctrl);
        k.Frames(1);
        Check(ed() == "hello world\n  foo\n  ");
        k.Key('z', Mods.Ctrl | Mods.Shift);
        k.Frames(1);
        Check(ed() == "hello world\n  foo\n  bar");
        k.Key(Keys.Home, Mods.Ctrl); k.Key(Keys.End, Mods.Shift); k.Key('c', Mods.Ctrl);
        k.Frames(1);
        Check(g.Clipboard == "hello world");
        k.Key(Keys.End, Mods.Ctrl); k.Key(Keys.Enter); k.Key('v', Mods.Ctrl);
        k.Frames(1);
        Check(ed() == "hello world\n  foo\n  bar\n  hello world");
        k.Key(Keys.Up, Mods.Shift); k.Key(Keys.Left, Mods.Shift | Mods.Ctrl);
        k.Frames(3);
        hs[0] = k.Hash;
        k.Key('x', Mods.Ctrl);
        k.Frames(1);
        Check(ed() == "hello world\n  foo\n  " && g.Clipboard == "bar\n  hello world");
        k.Key('z', Mods.Ctrl);
        k.Frames(1);
        Check(ed() == "hello world\n  foo\n  bar\n  hello world");

        for (int i = 0; i < 2; i++)
        {
            k.Ev(EventType.Down, 75, 46);
            k.Ev(EventType.Up, 75, 46);
        }
        k.Frames(1);
        k.Type("baz");
        k.Frames(1);
        Check(ed() == "hello world\n  baz\n  bar\n  hello world");
        for (int i = 0; i < 3; i++)
        {
            k.Ev(EventType.Down, 90, 25);
            k.Ev(EventType.Up, 90, 25);
            k.Frames(1);
        }
        k.Key(Keys.Delete);
        k.Frames(1);
        Check(ed() == "  baz\n  bar\n  hello world");
        k.Key('a', Mods.Ctrl); k.Key(Keys.Tab);
        k.Frames(1);
        Check(ed() == "      baz\n      bar\n      hello world");
        k.Key(Keys.Tab, Mods.Shift); k.Key(Keys.Tab, Mods.Shift);
        k.Frames(1);
        Check(ed() == "baz\nbar\nhello world");
        k.Key(Keys.Home, Mods.Ctrl); k.Key(Keys.Tab); k.Key(Keys.End);
        k.Key(Keys.Home); k.Key(Keys.Delete); k.Key(Keys.Home); k.Key('q');
        k.Frames(1);
        Check(ed() == "q    az\nbar\nhello world");
        k.Ed[0] = 0;
        k.Frames(1);
        for (int i = 0; i < 40; i++)
        {
            k.Type(i.ToString());
            if (i < 39) k.Key(Keys.Enter);
        }
        k.Frames(2);
        k.Key(Keys.Home, Mods.Ctrl); k.Key(Keys.PageDown); k.Key(Keys.PageDown); k.Key('#');
        k.Frames(12);
        hs[1] = k.Hash;
        Check(ed().Contains("\n#14\n"));
        k.Key(Keys.PageUp); k.Key('@');
        k.Frames(1);
        Check(ed().Contains("\n7@\n"));
        Set(k.Ed, "a long line of words that has to wrap around inside the box because it is wider than it\nshort");
        k.Flags = EditFlags.Wrap;
        k.Frames(2);
        k.Key(Keys.Home, Mods.Ctrl); k.Key(Keys.Down); k.Key('!');
        k.Frames(12);
        hs[2] = k.Hash;
        int bang = ed().IndexOf('!');
        Check(bang >= 0 && bang < ed().IndexOf('\n'));
        k.Click(100, 245);
        k.Frames(1);
        k.Type("zzz");
        k.Key('a', Mods.Ctrl); k.Key('c', Mods.Ctrl); k.Key(Keys.Backspace);
        k.Frames(1);
        Check(Str(k.Ro) == "read only text" && g.Clipboard.Length == 14);
        k.Click(100, 300);
        k.Frames(1);
        k.Type("0123456789abcdef");
        k.Frames(1);
        Check(Str(k.Small) == "0123456789a");
        g.Clipboard = "\u20ac\u20ac";
        k.Key(Keys.Home);
        for (int i = 0; i < 4; i++) k.Key(Keys.Delete);
        k.Key('v', Mods.Ctrl);
        k.Frames(1);
        Check(Str(k.Small) == "\u20ac456789a");
        k.Key(Keys.Esc);
        k.Frames(1);

        k.Page = 1;
        k.Frames(3);
        Check(k.LcFirst == 0 && k.LcLast > 1 && k.LcLast < 10);
        hs[3] = k.Hash;
        k.Click(400, 28);
        k.Frames(2);
        Check(k.SortCol == 1 && !k.SortDesc && k.SortChanges == 1);
        k.Click(400, 28);
        k.Frames(2);
        Check(k.SortCol == 1 && k.SortDesc && k.SortChanges == 2);
        k.Frames(10);
        hs[4] = k.Hash;
        k.Scroll(100, 320, 2);
        k.Frames(30);
        Check(k.LcFirst > 0 && k.LcLast > k.LcFirst && k.LcLast - k.LcFirst < 10);
        k.Click(60, 203);
        k.Frames(2);
        Check(!k.TreeOpen);

        k.Page = 2;
        k.Fv = 5;
        k.Nv = 1;
        k.Frames(3);
        k.Ev(EventType.Motion, 100, 45); k.Ev(EventType.Down, 100, 45);
        k.Frames(1);
        k.Ev(EventType.Motion, 150, 45);
        k.Frames(1);
        k.Ev(EventType.Up, 150, 45);
        k.Frames(1);
        Check(k.Fv > 9.9 && k.Fv < 10.1);
        k.Ev(EventType.Motion, 300, 45); k.Ev(EventType.Down, 300, 45);
        k.Frames(1);
        k.Ev(EventType.Motion, 280, 45);
        k.Frames(1);
        k.Ev(EventType.Up, 280, 45);
        k.Frames(1);
        Check(k.Ival == -10);
        k.Click(460, 106);
        k.Frames(1);
        k.Click(460, 106);
        k.Frames(1);
        Check(k.Nv == 5);
        k.Click(28, 106);
        k.Frames(1);
        Check(k.Nv == 3);
        k.Click(200, 106);
        k.Frames(1);
        k.Key('a', Mods.Ctrl); k.Type("42.5"); k.Key(Keys.Enter);
        k.Frames(2);
        Check(k.Nv == 42.5);
        k.Click(200, 106);
        k.Frames(1);
        k.Key(Keys.Up);
        k.Frames(1);
        k.Key(Keys.Esc);
        k.Frames(1);
        Check(k.Nv == 44.5);
        for (int i = 0; i < 2; i++)
        {
            k.Ev(EventType.Down, 100, 45);
            k.Ev(EventType.Up, 100, 45);
        }
        k.Frames(1);
        k.Type("7.5"); k.Key(Keys.Enter);
        k.Frames(2);
        Check(k.Fv == 7.5);
        k.Click(40, 140);
        k.Frames(1);
        Check(k.Links == 1);
        k.Ev(EventType.Motion, 300, 200);
        k.Frames(4);
        hs[5] = k.Hash;

        k.Page = 3;
        k.Frames(3);
        hs[6] = k.Hash;
        k.Ev(EventType.Motion, 192, 200); k.Ev(EventType.Down, 192, 200);
        k.Frames(1);
        k.Ev(EventType.Motion, 240, 200);
        k.Frames(1);
        k.Ev(EventType.Up, 240, 200);
        k.Frames(1);
        Check(k.Frac > 0.49 && k.Frac < 0.51);
        k.Ev(EventType.Motion, 60, 24); k.Ev(EventType.Down, 60, 24);
        k.Frames(1);
        k.Ev(EventType.Motion, 80, 30);
        k.Frames(1);
        k.Ev(EventType.Motion, 300, 70);
        k.Frames(1);
        k.Ev(EventType.Motion, 300, 26);
        k.Frames(3);
        hs[7] = k.Hash;
        Check(g.Cursor == CursorShape.Move);
        k.Ev(EventType.Up, 300, 26);
        k.Frames(2);
        Check(k.Dropped == 8 && Str(k.Got) == "payload");

        string got = string.Join(" ", hs.Select(h => h.ToString("x8")));
        Console.WriteLine($"  widgets C:  {CLine("widget hashes")}\n  widgets C#: {got}");
        Check(got == CLine("widget hashes"), "offscreen widget frames match the C ones");

        var bp = new uint[12 * 12];
        for (int y = 0; y < 12; y++)
            for (int x = 0; x < 12; x++) bp[y * 12 + x] = x < 4 || y < 4 || x >= 8 || y >= 8 ? 0xFF2050C0u : 0x80404040u;
        using var b = Bitmap.Wrap(bp, 12, 12);
        var nh = new uint[2];
        for (int s = 0; s < 2; s++)
        {
            using var c = Canvas.Create(120, 90, 120, s == 1 ? 1.5 : 1);
            c.DrawBitmapNine(b, 3.3, 2.7, 70, 50, 4, 4, 4, 4);
            c.DrawBitmapNine(b, 2, 56, 5, 5, 4, 4, 4, 4);
            var px = c.Pixels;
            nh[s] = Fnv(px, 120, 90, 120);
            if (s == 0) Check(px[2 * 120 + 3] == 0 && px[3 * 120 + 3] == 0xFF2050C0u && px[30 * 120 + 40] == 0x80404040u && px[30 * 120 + 72] == 0xFF2050C0u);
        }
        Check($"{nh[0]:x8} {nh[1]:x8}" == CLine("nine hashes") && nh[0] == 0x638fa7a8u && nh[1] == 0xdd482365u);
    }

    static void Skins()
    {
        var bp = new uint[12 * 12];
        for (int r = 0; r < 12; r++)
            for (int q = 0; q < 12; q++) bp[r * 12 + q] = q < 4 || r < 4 || q >= 8 || r >= 8 ? 0xFF2050C0u : 0x80404040u;
        using var b = Bitmap.Wrap(bp, 12, 12);
        using var g = new Goome(new GoomeOptions { Mode = Mode.Offscreen, Title = "skin", Width = 260, Height = 150 });
        using var u = new Ui(g);
        var t = u.Theme;
        t.Font = Font.BuiltIn;
        t.FontSize = 14;
        t.AnimMs = 0;
        t.ButtonSkin = new Skin(b, 4);
        t.PanelSkin = new Skin(b, 4, 4, 4, 4);
        t.InputSkin = new Skin(b, 3);
        Check(t.ButtonSkin.Bitmap == b && t.Raw.SkinInput.L == 3 && t.Raw.SkinPanel.B == 4);
        double x = 10, y = 10, w = 240, h = 0;
        var name = new byte[16];
        Set(name, "skinned");
        uint hash = 0;
        for (int f = 0; f < 3; f++)
        {
            var c = u.Begin(0);
            if (c == null) continue;
            if (u.Window("Skin", ref x, ref y, ref w, ref h, WindowFlags.Move))
            {
                u.Button("Button");
                u.Input("Name", name);
            }
            u.WindowEnd();
            u.End();
            hash = Fnv(c.Pixels, c.Width, c.Height, c.Stride);
        }
        Check($"{hash:x8}" == CLine("skin hash"), $"skin hash {hash:x8} vs C {CLine("skin hash")}");
        t.ButtonSkin = null;
        Check(t.ButtonSkin == null && t.Raw.SkinButton.L == 0);
        unsafe { Check(t.Raw.SkinButton.Bm == null); }
    }

    static uint StyleFrame(Ui u, Skin sk, bool styled, bool leave)
    {
        double x = 8, y = 8, w = 280, h = 0, v = 0.4;
        bool on = true;
        var c = u.Begin(0);
        if (c == null) return 0;
        if (u.Window("Style", ref x, ref y, ref w, ref h))
        {
            using (styled ? u.Style(radius: 0) : default) u.Button("Square");
            if (styled) u.PushRadii(new Radii(12, 2, 12, 2));
            u.SameLine();
            u.Button("Leaf");
            if (styled) u.Pop();
            using (styled ? u.Style(accent: 0xFFFF6FA8, pad: 14) : default)
            {
                u.Toggle("Pink", ref on);
                u.Slider("Amount", ref v, 0, 1, "%.2f");
            }
            if (styled)
            {
                u.PushSkin(SkinKind.Button, sk);
                u.PushNum(Style.FontSize, 19);
            }
            u.Button("Skinned");
            if (styled)
            {
                u.Pop(2);
                var t = new Theme(u.Theme);
                t.Light();
                t.Radius = 4;
                u.PushTheme(t);
            }
            u.Button("Light");
            if (styled && !leave) u.Pop();
            u.Button("After");
        }
        u.WindowEnd();
        u.End();
        return Fnv(c.Pixels, c.Width, c.Height, c.Stride);
    }

    static void StyleTest()
    {
        var bp = new uint[12 * 12];
        for (int i = 0; i < bp.Length; i++) bp[i] = i % 12 < 3 || i % 12 >= 9 ? 0xFFC05020u : 0xFF303030u;
        using var b = Bitmap.Wrap(bp, 12, 12);
        var sk = new Skin(b, 3);
        using var g = new Goome(new GoomeOptions { Mode = Mode.Offscreen, Title = "style", Width = 300, Height = 240 });
        using var u = new Ui(g);
        u.Theme.Font = Font.BuiltIn;
        u.Theme.FontSize = 14;
        u.Theme.AnimMs = 0;
        var h = new uint[4];
        StyleFrame(u, sk, false, false);
        h[0] = StyleFrame(u, sk, false, false);
        StyleFrame(u, sk, true, false);
        h[1] = StyleFrame(u, sk, true, false);
        Check(!g.LastError.Contains("style stack"), "balanced pushes dont warn");
        h[2] = StyleFrame(u, sk, true, true);
        Check(g.LastError.Contains("style stack"), $"unbalanced push warns: {g.LastError}");
        StyleFrame(u, sk, false, false);
        h[3] = StyleFrame(u, sk, false, false);
        Check(h[0] != h[1] && h[1] != h[2] && h[0] == h[3], "pushes change pixels and popping puts them back");
        Check(u.Theme.Radius == 10 && u.Theme.Accent == 0xFF6FDC5F && u.Theme.FontSize == 14, "theme is back after the frame");
        string got = string.Join(" ", h.Select(x => x.ToString("x8")));
        Check(got == CLine("style hashes"), $"style hashes {got} vs C {CLine("style hashes")}");

        using var u2 = new Ui(g);
        u2.Begin(0);
        u2.Pop(3);
        u2.End();
        Check(g.LastError.Contains("pop of 3"), $"popping too much is caught: {g.LastError}");
        Check(Throws<ArgumentException>(() => u.PushColor(Style.Radius, 0)) && Throws<ArgumentException>(() => u.PushNum(Style.Accent, 1)), "colors and numbers cant mix");
    }

    static uint LookFrame(Ui u, Theme? th)
    {
        double x = 8, y = 8, w = 300, h = 0, v = 0.4;
        bool on = true;
        int lvl = 1, tab = 0, mode = 1;
        string name = "sev";
        var c = u.Begin(0);
        if (c == null) return 0;
        if (th != null) u.PushTheme(th);
        if (u.Window("Look", ref x, ref y, ref w, ref h))
        {
            u.Tabs("t", new[] { "Play", "Stats" }, ref tab);
            u.Heading("Player");
            u.Input("Name", ref name, 32);
            u.Slider("Volume", ref v, 0, 1, "%.2f");
            u.Row(2);
            u.Checkbox("Sound", ref on);
            u.Toggle("Online", ref on);
            u.Radio("Easy", ref lvl, 0);
            u.SameLine();
            u.Radio("Hard", ref lvl, 1);
            u.Combo("Mode", ref mode, new[] { "Story", "Arena" });
            u.Progress(0.6, "loading");
            u.Row(3);
            u.Badge("new", 0);
            u.Link("goome");
            u.Button("Start");
        }
        u.WindowEnd();
        if (th != null) u.Pop();
        u.End();
        return Fnv(c.Pixels, c.Width, c.Height, c.Stride);
    }

    static unsafe uint ThemeBytes(Theme t)
    {
        uint v = 2166136261u;
        fixed (RawTheme* p = &t.Raw)
        {
            var b = (byte*)p;
            for (int i = 0; i < sizeof(RawTheme); i++) v = (v ^ b[i]) * 16777619u;
        }
        return v;
    }

    static void LooksTest()
    {
        var makes = new (uint Accent, bool Dark, double R)[] { (0xFF8B5CF6, true, 12), (0xFF2F7BF6, false, 20), (0xFFFFD60A, true, 0), (0x00E5484D, false, 6) };
        using var g = new Goome(new GoomeOptions { Mode = Mode.Offscreen, Title = "looks", Width = 320, Height = 400 });
        using var u = new Ui(g);
        var t = u.Theme;
        t.Font = Font.BuiltIn;
        t.FontSize = 14;
        t.AnimMs = 0;
        u.Config.BlinkMs = 0;
        var h = new uint[12];
        var b = new uint[11];
        LookFrame(u, null);
        h[0] = LookFrame(u, null);
        for (int i = 0; i < 11; i++)
        {
            var p = i <= (int)Preset.Rose ? Theme.Preset((Preset)i) : Theme.Make(makes[i - 7].Accent, makes[i - 7].Dark, makes[i - 7].R);
            b[i] = ThemeBytes(p);
            p.FontSize = 14;
            p.AnimMs = 0;
            LookFrame(u, p);
            h[1 + i] = LookFrame(u, p);
        }
        string got = string.Join(" ", h.Select(x => x.ToString("x8")));
        Check(got == CLine("look hashes"), $"look hashes {got} vs C {CLine("look hashes")}");
        string bytes = string.Join(" ", b.Select(x => x.ToString("x8")));
        Check(bytes == CLine("theme bytes"), $"theme bytes {bytes} vs C {CLine("theme bytes")}");

        var mint = Theme.Preset(Preset.Mint);
        Check(mint.Accent == 0xFF5FE0A8 && mint.Radius == 12 && mint.CheckSize == 18 && mint.HoverMs < 0 && mint.Align == Align.Auto && mint.Ease == Ease.Smooth);
        var made = Theme.Make(0x00E5484D, false, 6);
        Check(made.Accent == 0xFFE5484D && made.Radius == 6 && made.AccentText == 0xFFFFFFFF && made.CheckRadius == 3);
        var font = Font.Load(Data("sub.ttf"));
        var mine = new Theme(t) { FontMono = font, Radius = 3 };
        var kept = Theme.Preset(Preset.Rose, mine);
        Check(kept.FontMono == font && kept.Radius == 14 && kept.Font.IsBuiltIn && kept.FontHeading == null, "presets keep the fonts of the theme they start from");
        mine.Make(0xFF8B5CF6, true, 12);
        Check(mine.FontMono == font && mine.Radius == 12 && mine.Accent == 0xFF8B5CF6);
        Check(Throws<ArgumentOutOfRangeException>(() => mine.Preset((Preset)7)));

        u.Scale(1.25);
        Check(t.FontSize == 17.5 && t.Pad == 12.5 && t.CheckSize == 18 && t.ScrollW == 5);
        Check(CLine("scaled theme") == $"{t.FontSize:0.00} {t.Pad:0.00} {t.CheckSize:0.00} {t.ScrollW:0.00}");
        LookFrame(u, null);
        uint big = LookFrame(u, null);
        u.Scale(0.8);
        LookFrame(u, null);
        uint back = LookFrame(u, null);
        Check(big != h[0] && back == h[0], "scale up changes pixels and scaling back puts them back");
        Check($"{big:x8} {back:x8}" == CLine("scale hashes"), $"scale hashes {big:x8} {back:x8} vs C {CLine("scale hashes")}");
        u.Scale(0);
        u.Scale(-1);
        Check(t.FontSize == 14);
    }

    static uint InputFrame(Ui u, ref string text, InputFlags flags, string? hint, bool focus, ref int ret)
    {
        double x = 8, y = 8, w = 280, h = 0;
        var c = u.Begin(0);
        if (c == null) return 0;
        if (u.Window("Input", ref x, ref y, ref w, ref h))
        {
            u.Button("Hi");
            if (focus) u.SetFocus();
            int r = (flags & InputFlags.Password) != 0 && hint == "pw" ? u.Password("in", ref text, null, flags & ~InputFlags.Password, 24) : u.InputEx("in", ref text, flags, hint, 24);
            if (r != 0) ret = r;
        }
        u.WindowEnd();
        u.End();
        return Fnv(c.Pixels, c.Width, c.Height, c.Stride);
    }

    static uint InputStill(Ui u, string text, InputFlags flags, string? hint)
    {
        int r = 0;
        InputFrame(u, ref text, flags, hint, false, ref r);
        return InputFrame(u, ref text, flags, hint, false, ref r);
    }

    static string InputTyped(Ui u, string text, InputFlags flags, string type, out int ret)
    {
        ret = 0;
        InputFrame(u, ref text, flags, null, true, ref ret);
        foreach (char ch in type)
        {
            u.Feed(ch == '\n' ? Event.KeyPress(Keys.Enter) : Event.Char(ch));
            InputFrame(u, ref text, flags, null, false, ref ret);
        }
        return text;
    }

    static void InputsTest()
    {
        using var g = new Goome(new GoomeOptions { Mode = Mode.Offscreen, Title = "inputs", Width = 300, Height = 160 });
        var h = new uint[6];
        using (var u = new Ui(g))
        {
            u.Theme.Font = Font.BuiltIn;
            u.Theme.FontSize = 14;
            u.Theme.AnimMs = 0;
            u.Config.BlinkMs = 0;
            h[0] = InputStill(u, "abc", InputFlags.None, null);
            h[1] = InputStill(u, "abc", InputFlags.Password, null);
            h[2] = InputStill(u, "xyz", InputFlags.Password, null);
            h[3] = InputStill(u, "", InputFlags.None, null);
            h[4] = InputStill(u, "", InputFlags.None, "type here");
            u.Config.Mask = "#";
            Check(u.Config.Mask == "#");
            h[5] = InputStill(u, "abc", InputFlags.Password, null);
            Check(InputStill(u, "abc", InputFlags.Password, "pw") == h[5], "Password() is input_ex with the password flag");
            u.Config.Mask = null;
            Check(u.Config.Mask == null && InputStill(u, "abc", InputFlags.Password, "pw") == h[1]);
            Check(h[0] != h[1] && h[1] == h[2] && h[3] != h[4] && h[5] != h[1], "password masks every letter, hints show when empty");
        }
        string got = string.Join(" ", h.Select(x => x.ToString("x8")));
        Check(got == CLine("input hashes"), $"input hashes {got} vs C {CLine("input hashes")}");
        using var u2 = new Ui(g);
        u2.Theme.Font = Font.BuiltIn;
        u2.Theme.FontSize = 14;
        u2.Theme.AnimMs = 0;
        string num = InputTyped(u2, "", InputFlags.Number, "a1-.x2", out _);
        string sel = InputTyped(u2, "ab", InputFlags.None, "z", out _);
        string keep = InputTyped(u2, "ab", InputFlags.NoSelectOnFocus, "z", out _);
        InputTyped(u2, "ab", InputFlags.EnterReturns, "\n", out int ret);
        InputTyped(u2, "ab", InputFlags.None, "\n", out int plain);
        string texts = $"{num}|{sel}|{keep}|{ret}|{plain}";
        Check(texts == CLine("input texts") && ret == 2 && plain == 0, $"input texts {texts} vs C {CLine("input texts")}");
    }

    static string KeyFrame(Goome g, Ui u, ref string text, params Event[] evs)
    {
        while (g.Poll(out var ev)) u.Feed(ev);
        foreach (var e in evs) u.Feed(e);
        if (u.Begin(0) == null) return text;
        u.Input("in", ref text, 32);
        u.End();
        return text;
    }

    static void KeymapTest()
    {
        using var g = new Goome(new GoomeOptions { Backend = Backend.Test, Mode = Mode.Window, Title = "keys", Width = 300, Height = 200, TestScript = "end 100000" });
        using var u = new Ui(g);
        u.Theme.Font = Font.BuiltIn;
        u.Theme.FontSize = 14;
        var cfg = u.Config;
        Check(cfg.Combos(KeyAction.Copy).SequenceEqual(new[] { ((int)'c', Mods.Ctrl), ('c', Mods.Super) }));
        Check(cfg.Combos(KeyAction.Redo).Length == 4 && cfg.Combos(KeyAction.Redo)[1] == ('z', Mods.Ctrl | Mods.Shift));
        string text = "hello you";
        if (u.Begin(0) != null)
        {
            u.SetFocus();
            u.Input("in", ref text, 32);
            u.End();
        }
        KeyFrame(g, u, ref text, Event.KeyPress('c', Mods.Ctrl));
        Check(g.Clipboard == "hello you", $"ctrl+c copies: '{g.Clipboard}'");
        Check(cfg.Bind(KeyAction.Copy, "ctrl+k") && cfg.Bind(KeyAction.SelectAll, "alt+l, ctrl+q"));
        Check(cfg.Combos(KeyAction.SelectAll).SequenceEqual(new[] { ((int)'l', Mods.Alt), ('q', Mods.Ctrl) }));
        Check(!cfg.Bind(KeyAction.Undo, "ctrl+nope") && Throws<ArgumentOutOfRangeException>(() => cfg.Bind((KeyAction)12, "ctrl+k")));
        Check(cfg.Bind(KeyAction.Paste, null) && cfg.Combos(KeyAction.Paste).Length == 0);
        g.Clipboard = "zz";
        KeyFrame(g, u, ref text, Event.KeyPress('c', Mods.Ctrl));
        Check(g.Clipboard == "zz", "old copy key does nothing now");
        KeyFrame(g, u, ref text, Event.KeyPress('k', Mods.Ctrl));
        Check(g.Clipboard == "hello you", "new copy key works");
        KeyFrame(g, u, ref text, Event.KeyPress('v', Mods.Ctrl));
        Check(text == "hello you", "paste has no keys");
        KeyFrame(g, u, ref text, Event.KeyPress(Keys.Left), Event.KeyPress(Keys.Left), Event.KeyPress('q', Mods.Ctrl), Event.Char('x'));
        Check(text == "x", $"ctrl+q selects all: '{text}'");
        cfg.Bind(KeyAction.WordLeft, "alt+left");
        text = "ab cd";
        KeyFrame(g, u, ref text, Event.KeyPress(Keys.End), Event.KeyPress(Keys.Left, Mods.Alt), Event.Char('_'));
        Check(text == "ab _cd", $"alt+left jumps a word: '{text}'");
        cfg.Reset();
        Check(cfg.Combos(KeyAction.Paste).Length == 2 && cfg.Combos(KeyAction.Copy)[0] == ('c', Mods.Ctrl), "reset puts the keys back");
    }

    static void ConfigTest()
    {
        using var g = new Goome(new GoomeOptions { Backend = Backend.Test, Mode = Mode.Window, Title = "cfg", Width = 300, Height = 200, TestScript = "end 100000" });
        using var u = new Ui(g);
        var cfg = u.Config;
        Check(cfg.TipDelayMs == 500 && cfg.DoubleClickMs == 450 && cfg.DoubleClickPx == 5 && cfg.DragPx == 4 && cfg.RepeatDelayMs == 380 && cfg.RepeatMs == 55);
        Check(cfg.BlinkMs == 530 && cfg.MenuDelayMs == 120 && cfg.ToastMs == 3000 && cfg.ScrollLines == 3);
        Check(cfg.RaiseOnClick && cfg.EscCloses && cfg.TabNav && !cfg.FocusOnMouse && cfg.Mask == null && cfg.Ease == null);
        Check(cfg[CursorKind.Input] == CursorShape.IBeam && cfg[CursorKind.Button] == CursorShape.Arrow && cfg[CursorKind.DragDrop] == CursorShape.Move);
        cfg.TipDelayMs = 200;
        cfg.ScrollLines = 5;
        cfg.EscCloses = false;
        cfg[CursorKind.Button] = CursorShape.Hand;
        Check(cfg.Raw.TipDelayMs == 200 && cfg.Raw.ScrollLines == 5 && cfg.Raw.EscCloses == 0);
        unsafe { Check(cfg.Raw.Cursor[(int)CursorKind.Button] == (int)CursorShape.Hand); }
        Check(Throws<ArgumentOutOfRangeException>(() => cfg[(CursorKind)12] = CursorShape.Hand));

        u.Theme.Font = Font.BuiltIn;
        u.Theme.FontSize = 14;
        double x = 10, y = 10, w = 200, h = 0;
        void Frame(params Event[] evs)
        {
            while (g.Poll(out var ev)) u.Feed(ev);
            foreach (var e in evs) u.Feed(e);
            if (u.Begin(0) == null) return;
            if (u.Window("w", ref x, ref y, ref w, ref h)) u.Button("Hover me");
            u.WindowEnd();
            u.End();
        }
        Frame();
        Frame(Event.Motion(40, 60));
        Frame();
        Check(g.Cursor == CursorShape.Hand, $"buttons ask for the hand: {g.Cursor}");

        int calls = 0;
        var seen = new List<double>();
        SetEase(cfg, t =>
        {
            calls++;
            seen.Add(t);
            return t * t;
        });
        Collect();
        u.Theme.AnimMs = 200;
        u.Theme.Ease = Ease.Custom;
        Frame(Event.Motion(300, 190));
        for (int i = 0; i < 4; i++) Frame();
        Frame(Event.Motion(40, 60));
        for (int i = 0; i < 6; i++) Frame();
        Check(calls > 0 && seen.All(v => v >= 0 && v <= 1), $"custom ease gets called with 0..1, {calls} calls");
        cfg.Ease = _ => throw new InvalidOperationException("boom");
        Frame(Event.Motion(300, 190));
        Frame();
        cfg.Ease = null;
        unsafe { Check(cfg.Raw.Ease == IntPtr.Zero && cfg.Raw.EaseUser == IntPtr.Zero); }
        int before = calls;
        Frame(Event.Motion(40, 60));
        Frame();
        Check(calls == before);
        cfg.Mask = "*";
        cfg.Reset();
        Check(cfg.Mask == null && cfg.TipDelayMs == 500 && cfg[CursorKind.Button] == CursorShape.Arrow);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SetEase(UiConfig cfg, Func<double, double> f) => cfg.Ease = new Func<double, double>(t => f(t));

    static Bitmap MakeBitmap(int w, int h, Func<int, int, uint> px)
    {
        var a = new uint[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++) a[y * w + x] = px(x, y);
        return Bitmap.FromArgb(a, w, h);
    }

    static Bitmap Dots() => MakeBitmap(8, 8, (x, y) => ((x + y) & 1) != 0 ? 0xFFFFFFFFu : 0);
    static Bitmap Heart() => MakeBitmap(6, 6, (x, y) => x >= y ? 0xFFE0567Au : 0);
    static Skin Frame() => new Skin(MakeBitmap(12, 12, (x, y) => x < 4 || y < 4 || x >= 8 || y >= 8 ? 0xFF40A060u : 0xFF202830u), 4);

    static readonly List<Bitmap> junk = new List<Bitmap>();

    static void Collect()
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        for (int i = 0; i < 4; i++)
            foreach (int n in new[] { 6, 8, 12 }) junk.Add(MakeBitmap(n, n, (x, y) => 0xFF00FF00u));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PushHeart(Ui u)
    {
        u.PushIcon(Icon.Check, Heart());
        u.PushSkin(SkinKind.Toggle, Frame());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Pushed StyleHeart(Ui u)
    {
        var st = u.Style(icons: new Dictionary<Icon, Bitmap?> { [Icon.Check] = Heart() });
        u.PushSkin(SkinKind.Toggle, Frame());
        return st;
    }

    static uint IconFrame(Ui u, int push, Func<Theme>? themed = null)
    {
        double x = 8, y = 8, w = 300, h = 250, v = 0.6;
        bool a = true, b = true, open = true, wopen = true;
        int r = 1, sel = 0, tab = 1;
        var c = u.Begin(0);
        if (c == null) return 0;
        if (themed != null)
        {
            u.PushTheme(themed());
            Collect();
        }
        if (u.Window("Icons", ref x, ref y, ref w, ref h, WindowFlags.Resize | WindowFlags.Collapse | WindowFlags.Close, ref wopen))
        {
            u.Tabs("t", new[] { "One", "Two" }, ref tab);
            Pushed st = default;
            if (push == 1)
            {
                PushHeart(u);
                Collect();
            }
            else if (push == 2)
            {
                st = StyleHeart(u);
                Collect();
            }
            u.Checkbox("Check", ref a);
            u.Toggle("Toggle", ref b);
            if (push == 1) u.Pop(2);
            else if (push == 2)
            {
                u.Pop();
                st.Dispose();
            }
            if (push != 0) Collect();
            u.Checkbox("After", ref a);
            u.Radio("Radio", ref r, 1);
            u.Slider("Slide", ref v, 0, 1, "%.1f");
            u.Combo("Pick", ref sel, new[] { "Red", "Green" });
            u.Collapsing("Fold", ref open);
        }
        u.WindowEnd();
        if (themed != null) u.Pop();
        u.End();
        return Fnv(c.Pixels, c.Width, c.Height, c.Stride);
    }

    static void DressAll(Theme t)
    {
        var d = Dots();
        for (var i = Icon.ArrowDown; i <= Icon.Picker; i++) t.Icons[i] = d;
        var sk = Frame();
        foreach (var k in new[] { SkinKind.Checkbox, SkinKind.Radio, SkinKind.Toggle, SkinKind.SliderTrack, SkinKind.SliderFill, SkinKind.Tab, SkinKind.Titlebar, SkinKind.Scrollbar })
            t.Skins[k] = sk;
    }

    static void IconsTest()
    {
        using var g = new Goome(new GoomeOptions { Mode = Mode.Offscreen, Title = "icons", Width = 320, Height = 270 });
        using var u = new Ui(g);
        var t = u.Theme;
        t.Font = Font.BuiltIn;
        t.FontSize = 14;
        t.AnimMs = 0;
        var h = new uint[6];
        IconFrame(u, 0);
        h[0] = IconFrame(u, 0);
        h[1] = IconFrame(u, 1);
        h[2] = IconFrame(u, 0);
        Check(IconFrame(u, 2) == h[1], "style(icons:) draws the same as push_icon");
        Check(IconFrame(u, 0) == h[0], "and pops back the same way");
        unsafe { Check(t.Raw.Icon[Icon.Check] == null && t.Icons[Icon.Check] == null && t.ToggleSkin == null, "pushes leave the theme alone"); }
        DressAll(t);
        Collect();
        Check(t.Icons[Icon.Close] != null && t.Icons[Icon.Close] == t.Icons[Icon.Picker] && t.Skins[SkinKind.Tab] == t.TabSkin && t.Raw.SkinSlider.L == 4);
        unsafe { Check(t.Raw.Icon[Icon.Resize] != null && t.Raw.SkinTitlebar.Bm != null); }
        h[3] = IconFrame(u, 0);
        t.IconTint = 1;
        h[4] = IconFrame(u, 0);
        for (var i = Icon.ArrowDown; i <= Icon.Picker; i++) t.Icons[i] = null;
        t.IconTint = 0;
        foreach (var k in new[] { SkinKind.Checkbox, SkinKind.Radio, SkinKind.Toggle, SkinKind.SliderTrack, SkinKind.SliderFill, SkinKind.Tab, SkinKind.Titlebar, SkinKind.Scrollbar })
            t.Skins[k] = null;
        h[5] = IconFrame(u, 0);
        Check(h[0] != h[1] && h[0] == h[2] && h[3] != h[0] && h[4] != h[3] && h[5] == h[0], "icons and skins change pixels and clearing them puts them back");
        string got = string.Join(" ", h.Select(x => x.ToString("x8")));
        Check(got == CLine("icon hashes"), $"icon hashes {got} vs C {CLine("icon hashes")}");

        Check(IconFrame(u, 0, () =>
        {
            var x = new Theme(t);
            DressAll(x);
            x.IconTint = 1;
            return x;
        }) == h[4], "a pushed theme keeps its icons alive");
        Check(IconFrame(u, 0) == h[0]);
        foreach (var j in junk) j.Dispose();
        junk.Clear();
        Check(Throws<ArgumentOutOfRangeException>(() => u.PushIcon((Icon)22, null)) && Throws<ArgumentOutOfRangeException>(() => t.Icons[(Icon)(-1)] = null));
    }

    sealed class Frames
    {
        readonly Goome g;
        readonly Ui u;

        public Frames(Goome goome, Ui ui)
        {
            g = goome;
            u = ui;
        }

        public void Run(Action<Ui> draw, params Event[] evs)
        {
            foreach (var e in evs) g.Push(e);
            while (g.Poll(out var ev)) u.Feed(ev);
            if (u.Begin(0) == null) return;
            draw(u);
            u.End();
        }
    }

    static void PosTest()
    {
        using var g = Goome.OpenOffscreen(400, 300);
        using var u = new Ui(g);
        var f = new Frames(g, u);
        Item o = default, p = default, after = default, plain = default, sized = default;
        double x = 20, y = 30, w = 300, h = 220;
        for (int i = 0; i < 2; i++)
            f.Run(ui =>
            {
                if (ui.Window("Pos", ref x, ref y, ref w, ref h))
                {
                    ui.Pos(0, 0);
                    ui.Custom("o", 1, 1, ItemFlags.NoHit, out o);
                    ui.Custom("plain", 50, 20, ItemFlags.NoHit, out plain);
                    ui.Pos(137, 61);
                    ui.Custom("p", 40, 20, ItemFlags.None, out p);
                    ui.Custom("after", 50, 20, ItemFlags.NoHit, out after);
                    ui.Size(90, 33);
                    ui.Custom("sized", 10, 10, ItemFlags.NoHit, out sized);
                }
                ui.WindowEnd();
            });
        Check(p.X - o.X == 137 && p.Y - o.Y == 61 && p.W == 40 && p.H == 20, $"pos puts it at 137,61 from the top left, got {p.X - o.X},{p.Y - o.Y}");
        Check(after.X == plain.X && after.Y > plain.Y && after.Y < p.Y, $"the flow carries on under the last flowed one ({after.Y} vs {plain.Y})");
        Check(sized.W == 90 && sized.H == 33, $"size wins over the asked size, got {sized.W}x{sized.H}");
        Check(g.Hit(p.X + 5, p.Y + 5), "the pinned one has a hit rect");
    }

    static void CustomTest()
    {
        using var g = Goome.OpenOffscreen(400, 300);
        using var u = new Ui(g);
        var f = new Frames(g, u);
        Item hx = default, free = default;
        bool any = false;
        double total = 0;
        void Draw(Ui ui)
        {
            ui.Pos(20, 20);
            any = ui.Custom("hx", 30, 30, ItemFlags.DragX | ItemFlags.Focusable, out hx);
            if (hx.Held) total += hx.Dx;
            ui.Pos(200, 20);
            ui.Custom("free", 30, 30, ItemFlags.Drag, out free);
            var c = ui.Canvas();
            c?.DrawRect(hx.X, hx.Y, hx.W, hx.H, hx.Held ? 0xFFFF0000 : 0xFF00FF00);
        }
        f.Run(Draw);
        var h0 = hx;
        Check(h0.W == 30 && h0.H == 30 && !h0.Hovered && !h0.Held, "custom item reserved, nothing on it yet");
        Check(g.TryFrame(out var fr) && fr[(int)h0.X + 2, (int)h0.Y + 2] == 0xFF00FF00, "canvas draws land in the frame");
        double cx = h0.X + 15, cy = h0.Y + 15;
        f.Run(Draw, Event.Motion(cx, cy), Event.Down(cx, cy));
        Check(hx.Hovered && hx.Pressed && hx.Held && any && hx.Focused, "press grabs it and focuses it");
        Check(u.WantsMouse, "the ui takes the mouse during the drag");
        f.Run(Draw, Event.Motion(cx + 30, cy + 20));
        Check(hx.Held && hx.Dx == 30 && hx.Dy == 0 && hx.DragX == 30 && hx.DragY == 0, $"x lock: dx {hx.Dx} dy {hx.Dy}");
        Check(hx.Mx == cx + 30 && hx.My == cy + 20, "pointer still reported as is");
        f.Run(Draw, Event.Motion(cx + 50, cy - 40));
        Check(hx.Held && hx.Dx == 20 && hx.Dy == 0 && hx.DragX == 50 && hx.DragY == 0 && total == 50, $"keeps adding up ({hx.DragX}, {total})");
        Check(g.TryFrame(out fr) && fr[(int)hx.X + 2, (int)hx.Y + 2] == 0xFFFF0000, "held color drawn");
        f.Run(Draw, Event.Up(cx + 50, cy - 40));
        Check(hx.Released && !hx.Held && !hx.Clicked, "let go off the item: released, not clicked");
        f.Run(Draw);
        Check(!hx.Released && !hx.Held && !any, "quiet after that");
        f.Run(Draw, Event.KeyPress(Keys.Right));
        Check(hx.Key == Keys.Right && hx.Focused && any, $"focused item gets keys ({hx.Key:x})");
        f.Run(Draw, Event.KeyPress(Keys.Enter));
        Check(hx.Clicked, "enter clicks it");

        double fx = free.X + 15, fy = free.Y + 15;
        f.Run(Draw, Event.Motion(fx, fy), Event.Down(fx, fy));
        f.Run(Draw, Event.Motion(fx - 12, fy + 7));
        Check(free.Held && free.Dx == -12 && free.Dy == 7 && free.DragX == -12 && free.DragY == 7, $"both axes free: {free.Dx},{free.Dy}");
        f.Run(Draw, Event.Motion(fx, fy), Event.Up(fx, fy));
        Check(free.Released && free.Clicked && !hx.Held, "back on it and let go clicks");
        f.Run(Draw, Event.Down(fx, fy), Event.Up(fx, fy), Event.Down(fx, fy), Event.Up(fx, fy));
        Check(free.DoubleClicked, "double click");
    }

    static void StoreTest()
    {
        using var g = Goome.OpenOffscreen(200, 100);
        using var u = new Ui(g);
        var f = new Frames(g, u);
        double first = -1, seen = -1;
        int len = 0;
        f.Run(ui =>
        {
            var v = ui.Store("knob", 3);
            len = v.Length;
            first = v[0] + v[1] + v[2];
            v[0] = 0.25;
            v[2] = 7;
        });
        Check(len == 3 && first == 0, "store starts out zeroed");
        f.Run(ui =>
        {
            var v = ui.Store("knob", 3);
            seen = v[0] + v[2];
            ui.PushId("other");
            var o = ui.Store("knob", 3);
            first = o[0];
            ui.PopId();
        });
        Check(seen == 7.25, $"store keeps values between frames ({seen})");
        Check(first == 0, "same name under another id is its own store");
        f.Run(ui =>
        {
            var v = ui.Store("knob", 5);
            seen = v[0] + v[2] + v[3] + v[4];
            len = v.Length;
        });
        Check(len == 5 && seen == 7.25, "growing it keeps the old values and zeros the rest");
        Check(Throws<ArgumentException>(() => u.Store("x", 0)));
    }

    static void FrameCopyTest()
    {
        using var g = Goome.OpenOffscreen(64, 32);
        Check(!g.FrameDirty(out _), "nothing dirty before a frame");
        var c = g.Begin(0)!;
        c.Clear();
        c.DrawRect(4, 4, 8, 8, 0x80FF4020);
        c.DrawRect(20, 4, 4, 4, 0xFF102030);
        g.End(c);
        Check(g.FrameDirty(out var r) && r == new Rect(0, 0, 64, 32), $"first one is the whole frame, got {r}");
        Check(!g.FrameDirty(out r), "and then nothing");
        var buf = new byte[64 * 32 * 4];
        g.FrameCopy(buf, 64 * 4, Format.BgraPremul);
        var f = g.Frame();
        bool same = true;
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 64; x++) same &= BitConverter.ToUInt32(buf, (y * 64 + x) * 4) == f[x, y];
        Check(same, "bgra premul is the frame as is");
        g.FrameCopy(buf, 64 * 4, Format.Rgba);
        int at = (6 * 64 + 6) * 4;
        Check(buf[at] == 0xFF && buf[at + 1] == 0x40 && buf[at + 2] == 0x20 && buf[at + 3] == 0x80, $"rgba unpremultiplied {buf[at]:x2}{buf[at + 1]:x2}{buf[at + 2]:x2}{buf[at + 3]:x2}");
        at = (5 * 64 + 21) * 4;
        g.FrameCopy(buf, 64 * 4, Format.RgbaPremul);
        Check(buf[at] == 0x10 && buf[at + 1] == 0x20 && buf[at + 2] == 0x30 && buf[at + 3] == 0xFF, "rgba premul swaps r and b");

        c = g.Begin(0)!;
        c.DrawRect(40, 10, 6, 5, 0xFFFFFFFF);
        g.End(c);
        Check(g.FrameDirty(out r) && r.X <= 40 && r.Y <= 10 && r.X + r.W >= 46 && r.Y + r.H >= 15 && r.W * r.H < 64 * 32, $"just the changed bit is dirty, got {r}");
        var part = new byte[r.H * (r.W * 4 + 4)];
        g.FrameCopy(part, r.W * 4 + 4, Format.BgraPremul, r);
        f = g.Frame();
        Check(BitConverter.ToUInt32(part, (10 - r.Y) * (r.W * 4 + 4) + (40 - r.X) * 4) == f[40, 10], "rect copy lands at dst's top left with the stride");
        Check(Throws<ArgumentException>(() => g.FrameCopy(part, r.W * 4 - 4, Format.Bgra, r)), "short stride throws before the native call");
        Check(Throws<ArgumentException>(() => g.FrameCopy(new byte[100], 64 * 4, Format.Bgra)), "short buffer throws");
        Check(Catch(() => g.FrameCopy(buf, 64 * 4, Format.Bgra, new Rect(60, 0, 10, 10))) == GoomeError.BadArgument, "rect off the frame comes back from goome");
        g.Resize(32, 16);
        c = g.Begin(0)!;
        g.End(c);
        Check(g.FrameDirty(out r) && r == new Rect(0, 0, 32, 16), $"whole frame again after a resize, got {r}");
    }

    static void Popups()
    {
        using var g = Goome.OpenOffscreen(400, 300);
        using var u = new Ui(g);
        u.Theme.AnimMs = 0;
        bool shown = false, modal = true, plain = false;
        string text = "", notes = "a";
        int spun = 0;
        for (int f = 0; f < 12; f++)
        {
            if (f == 4) g.PushText("xy");
            if (f == 6) g.Push(Event.KeyPress(Keys.Esc));
            while (g.Poll(out var ev)) u.Feed(ev);
            if (u.Begin(0) == null) continue;
            u.App("pop");
            if (f == 1) u.PopupOpen("p");
            if (f == 8) u.PopupOpen("p");
            shown = u.PopupBegin("p");
            if (shown)
            {
                u.Label("inside");
                if (f == 3) u.PopupClose();
                u.PopupEnd();
            }
            if (f < 7)
            {
                u.SetFocus();
                u.Input("Text", ref text, 32);
            }
            else if (f < 11)
            {
                u.SetFocus();
                u.TextEdit("notes", ref notes, 60);
            }
            if (u.ModalBegin("Gone", ref plain)) u.ModalEnd();
            u.Spinner(16);
            spun++;
            u.AppEnd();
            u.End();
            if (f == 2) Check(shown, "popup opens");
            if (f == 5) Check(!shown, "popup closes");
            if (f == 9) Check(shown, "popup opens again");
        }
        Check(text == "xy" && notes == "a" && spun == 12 && modal && !plain);
        using var g2 = Goome.OpenOffscreen(300, 200);
        using var u2 = new Ui(g2);
        u2.Theme.AnimMs = 0;
        string edit = "";
        for (int f = 0; f < 4; f++)
        {
            if (f == 1) g2.PushText("oneé");
            while (g2.Poll(out var ev)) u2.Feed(ev);
            if (u2.Begin(0) == null) continue;
            u2.SetFocus();
            u2.TextEdit("e", ref edit, 80, EditFlags.Mono);
            Check(f == 0 || u2.Focused);
            u2.End();
        }
        Check(edit == "oneé");
    }
    static void GrabTest()
    {
        string header = File.ReadAllText(Path.Combine(root, "native", "goome.h"));
        var consts = System.Text.RegularExpressions.Regex.Matches(header, @"\bGOOME_(CAP|WANT)_([A-Z_]+) = (\d+)");
        int caps = 0, wants = 0;
        foreach (System.Text.RegularExpressions.Match m in consts)
        {
            var t = m.Groups[1].Value == "CAP" ? typeof(Caps) : typeof(Want);
            string name = Pascal(m.Groups[2].Value.ToLowerInvariant());
            Check(Enum.TryParse(t, name, out var v) && Convert.ToInt32(v) == int.Parse(m.Groups[3].Value), $"{t.Name}.{name} = {m.Groups[3].Value}");
            if (t == typeof(Caps)) caps++;
            else wants++;
        }
        Check(caps == Enum.GetValues(typeof(Caps)).Length - 1 && caps >= 9);
        Check(wants == 2 && Enum.GetValues(typeof(Want)).Length == 3);
        foreach (var f in new[] { "goome_grab", "goome_grabbed", "goome_ui_wants", "goome_ui_auto_grab" })
        {
            Check(header.Contains(" " + f + "("), $"goome.h has {f}");
            Check(typeof(Goome).Assembly.GetType("GoomeSharp.Native")!.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Any(mi => mi.GetCustomAttribute<DllImportAttribute>()?.EntryPoint == f), $"Native binds {f}");
        }

        using var g = Goome.OpenOffscreen(400, 360);
        Check(!g.Has(Caps.Grab) && !g.Grabbed);
        Check(g.Grab(true) && g.Grabbed);
        Check(g.Grab(false) && !g.Grabbed);
        using var u = new Ui(g);
        u.Theme.Font = Font.BuiltIn;
        u.Theme.FontSize = 14;
        u.Theme.AnimMs = 0;
        Check(u.AutoGrab == (Want.Mouse | Want.Keyboard));
        double x = 10, y = 10, w = 220, h = 0, val = 0;
        bool check = false;
        string text = "";
        var seen = new List<(Want, bool)>();
        for (int f = 0; f < 14; f++)
        {
            switch (f)
            {
            case 1: g.Push(Event.Motion(120, 71)); break;
            case 2: g.Push(Event.Motion(40, 157)); g.Push(Event.Down(40, 157)); break;
            case 3: g.Push(Event.Motion(390, 350)); break;
            case 4: g.Push(Event.Up(390, 350)); break;
            case 5: g.Push(Event.Motion(100, 213)); g.Push(Event.Down(100, 213)); g.Push(Event.Up(100, 213)); break;
            case 6: g.Push(Event.Motion(390, 350)); g.Push(Event.Down(390, 350)); g.Push(Event.Up(390, 350)); break;
            case 7: u.AutoGrab = Want.None; break;
            case 8: g.Push(Event.Motion(40, 157)); g.Push(Event.Down(40, 157)); break;
            case 9: g.Push(Event.Motion(390, 350)); break;
            case 10: g.Push(Event.Up(390, 350)); break;
            }
            while (g.Poll(out var ev)) u.Feed(ev);
            if (u.Begin(0) == null) break;
            if (u.Window("grab", ref x, ref y, ref w, ref h, WindowFlags.Move))
            {
                u.Button("Go");
                u.Checkbox("Check", ref check);
                u.Slider("Val", ref val, 0, 1);
                u.Input("Text", ref text, 16);
            }
            u.WindowEnd();
            u.End();
            seen.Add((u.Wants, g.Grabbed));
        }
        Console.WriteLine("  grab " + string.Join(" ", seen.Select(s => $"{(int)s.Item1}{(s.Item2 ? "g" : "")}")) + $"  val {val:0.00}");
        Check(seen[0] == (Want.None, false));
        Check(seen[1] == (Want.Mouse, false), "hover gives mouse");
        Check(seen[2] == (Want.Mouse, true), "slider press grabs");
        Check(seen[3] == (Want.Mouse, true), "drag outside stays grabbed");
        Check(val == 1, "drag outside the slider moves it");
        Check(seen[4] == (Want.None, false), "release lets go");
        Check(seen[5].Item1.HasFlag(Want.Keyboard) && !seen[5].Item2, "text field wants keyboard");
        Check(seen[6] == (Want.None, false), "click away clears it");
        Check(seen[8] == (Want.Mouse, false) && seen[9] == (Want.Mouse, false), "no auto grab, still wants");
        Check(seen[10] == (Want.None, false) && !g.Grabbed);
        Check(u.WantsMouse == seen[13].Item1.HasFlag(Want.Mouse) && u.WantsKeyboard == seen[13].Item1.HasFlag(Want.Keyboard));
    }
}
