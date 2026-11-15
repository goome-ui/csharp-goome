using GoomeSharp;
using GoomeUI.ModKit;
using UnityEngine;

namespace GoomeMenu
{
    public sealed class Menu
    {
        static readonly string[] tabs = { "Player", "World", "Menu" };

        readonly GoomeMod mod;
        double x = 40, y = 40, w = 340, h = 0, speed = 1, timeScale = 1, scale = 1;
        int tab, jumps;
        bool godMode, noFall, blockAll;

        public Menu(GoomeMod mod)
        {
            this.mod = mod;
            mod.Draw += Draw;
        }

        void Draw(Ui ui)
        {
            bool open = true;
            if (ui.Window("Goome Menu", ref x, ref y, ref w, ref h, WindowFlags.Move | WindowFlags.Collapse | WindowFlags.Close, ref open))
            {
                ui.Tabs("tabs", tabs, ref tab);
                if (tab == 0)
                {
                    ui.Toggle("God mode", ref godMode);
                    ui.Toggle("No fall damage", ref noFall);
                    ui.Slider("Move speed", ref speed, 0.5, 5, "%.1fx");
                    ui.SliderInt("Extra jumps", ref jumps, 0, 5);
                }
                else if (tab == 1)
                {
                    if (ui.Slider("Time scale", ref timeScale, 0.1, 3, "%.2f")) Time.timeScale = (float)timeScale;
                    if (ui.Button("Normal speed"))
                    {
                        timeScale = 1;
                        Time.timeScale = 1;
                    }
                }
                else
                {
                    ui.Label($"{mod.Hotkey} opens and closes this");
                    if (ui.Slider("Menu size", ref scale, 0.75, 2, "%.2f")) mod.Scale = scale;
                    if (ui.Toggle("Block all game input while open", ref blockAll)) mod.BlockAllWhileOpen = blockAll;
                }
            }
            ui.WindowEnd();
            if (!open) mod.Open = false;
        }
    }
}
