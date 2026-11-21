using System;

namespace GoomeSharp
{
    /// <summary>A normal window with a ui and the main loop already done for you</summary>
    public sealed class GoomeApp : IDisposable
    {
        int extra = 2;
        long lastDraw;
        readonly bool test;

        /// <summary>Opens the window and the ui</summary>
        public GoomeApp(string title = "goome", int width = 960, int height = 640) : this(Goome.OpenApp(title, width, height), title)
        {
        }

        /// <summary>Opens the window and the ui</summary>
        /// <param name="options">What goome gets opened with</param>
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

        /// <summary>The goome behind it</summary>
        public Goome Goome { get; }
        /// <summary>The ui it draws</summary>
        public Ui Ui { get; }
        /// <summary>The window title</summary>
        public string Title { get; private set; }
        /// <summary>App flags like the menu bar and status bar</summary>
        public AppFlags Flags { get; set; }
        /// <summary>Frame rate cap while it keeps redrawing</summary>
        public int Fps { get; set; } = 60;
        /// <summary>Draw at least this often even if nothing happened, 0 = only when an event comes or something animates</summary>
        public int RedrawEveryMs { get; set; }
        /// <summary>True while the loop runs</summary>
        public bool Running { get; private set; }
        /// <summary>Frames drawn so far</summary>
        public long Frames { get; private set; }

        /// <summary>Raised for every event before the ui sees it</summary>
        public event Action<Event>? OnEvent;
        /// <summary>Called when the user closes the window. Return false to keep it open</summary>
        public Func<bool>? OnClose { get; set; }

        /// <summary>Runs the loop. Every frame draw gets the ui, already inside the app root made from Title and Flags. The static overloads open the window first</summary>
        public static void Run(string title, int width, int height, Action<Ui> draw) => Run(title, width, height, AppFlags.None, draw);

        /// <summary>Runs the loop. Every frame draw gets the ui, already inside the app root made from Title and Flags. The static overloads open the window first</summary>
        public static void Run(string title, int width, int height, AppFlags flags, Action<Ui> draw)
        {
            using var app = new GoomeApp(title, width, height) { Flags = flags };
            app.Run(draw);
        }

        /// <summary>Changes the window title</summary>
        public void SetTitle(string title)
        {
            Title = title;
            Goome.SetTitle(title);
            Invalidate();
        }

        /// <summary>Asks for a redraw</summary>
        public void Invalidate() => extra = Math.Max(extra, 2);

        /// <summary>Stops the loop</summary>
        public void Quit() => Running = false;

        /// <summary>Runs the loop. Every frame draw gets the ui, already inside the app root made from Title and Flags. The static overloads open the window first</summary>
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

        /// <summary>Closes the window and the ui</summary>
        public void Dispose()
        {
            Ui.Dispose();
            Goome.Dispose();
        }
    }
}
