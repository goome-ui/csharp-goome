#nullable enable
using System;

namespace GoomeSharp
{
    public sealed class GoomeApp : IDisposable
    {
        int extra = 2;
        long lastDraw;
        readonly bool test;

        public GoomeApp(string title = "goome", int width = 960, int height = 640) : this(Goome.OpenApp(title, width, height), title)
        {
        }

        public GoomeApp(GoomeOptions options) : this(new Goome(options), options.Title ?? "goome")
        {
        }

        GoomeApp(Goome g, string title)
        {
            Goome = g;
            Title = title;
            try
            {
                Ui = new Ui(g);
            }
            catch
            {
                g.Dispose();
                throw;
            }
            test = g.BackendName.StartsWith("test");
        }

        public Goome Goome { get; }
        public Ui Ui { get; }
        public string Title { get; private set; }
        public AppFlags Flags { get; set; }
        public int Fps { get; set; } = 60;
        public int RedrawEveryMs { get; set; }
        public bool Running { get; private set; }
        public long Frames { get; private set; }

        public event Action<Event>? OnEvent;
        public Func<bool>? OnClose { get; set; }

        public static void Run(string title, int width, int height, Action<Ui> draw) => Run(title, width, height, AppFlags.None, draw);

        public static void Run(string title, int width, int height, AppFlags flags, Action<Ui> draw)
        {
            using var app = new GoomeApp(title, width, height) { Flags = flags };
            app.Run(draw);
        }

        public void SetTitle(string title)
        {
            Title = title;
            Goome.SetTitle(title);
            Invalidate();
        }

        public void Invalidate() => extra = Math.Max(extra, 2);

        public void Quit() => Running = false;

        public void Run(Action<Ui> draw)
        {
            var pace = new Pacer();
            Running = true;
            while (Running)
            {
                int wait = Busy ? pace.Pace(Fps) : IdleWait();
                while (Goome.Poll(out var ev, wait))
                {
                    wait = 0;
                    OnEvent?.Invoke(ev);
                    if (ev.Type == EventType.Quit)
                    {
                        Running = false;
                        break;
                    }
                    if (ev.Type == EventType.Close)
                    {
                        if (OnClose?.Invoke() ?? true)
                        {
                            Running = false;
                            break;
                        }
                        continue;
                    }
                    Ui.Feed(ev);
                    Invalidate();
                }
                if (!Running) break;
                if (RedrawEveryMs > 0 && Goome.NowMs - lastDraw >= RedrawEveryMs) Invalidate();
                if (!Busy) continue;
                if (extra > 0) extra--;
                if (Ui.Begin(0) == null) continue;
                Ui.App(Title, Flags);
                draw(Ui);
                Ui.AppEnd();
                Ui.End();
                lastDraw = Goome.NowMs;
                Frames++;
            }
        }

        bool Busy => extra > 0 || test || Ui.Animating;

        int IdleWait()
        {
            if (RedrawEveryMs <= 0) return 1000;
            return (int)Math.Max(1, Math.Min(1000, RedrawEveryMs - (Goome.NowMs - lastDraw)));
        }

        public void Dispose()
        {
            Ui.Dispose();
            Goome.Dispose();
        }
    }
}
