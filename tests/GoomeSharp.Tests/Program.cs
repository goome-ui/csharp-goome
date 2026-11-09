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
            ["goome_bitmap"] = typeof(RawBitmap), ["goome_theme"] = typeof(RawTheme),
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
            var fi = t.GetField(f, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ?? t.GetField(parts[1], BindingFlags.NonPublic | BindingFlags.Instance);
            Check(fi != null, $"{t.Name} has {f}");
            if (fi == null) continue;
            int off = (int)Marshal.OffsetOf(t, fi.Name);
            if (off != want) Console.WriteLine($"  {parts[0]}.{parts[1]}: C {want}, C# {off}");
            Check(off == want, $"offset {parts[0]}.{parts[1]}");
            seen[t] = seen.GetValueOrDefault(t) + 1;
        }
        Check(lines > 90);
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
            c.DrawText(both[k], 13, 4, 2, "goome! Hié\nAVAWAY Ty.@ ☃", 0xFFFFFFFF);
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
}
