using System;
using System.IO;
using System.Linq;
using GoomeSharp;
using GoomeUI.ModKit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Fake = UnityEngine.Input.Fake;
using UInput = UnityEngine.Input;
using UEvent = UnityEngine.Event;

static class Program
{
    static int failed, passed;

    static void Check(bool ok, string what)
    {
        if (ok) passed++;
        else
        {
            failed++;
            Console.WriteLine("FAIL " + what);
        }
    }

    static void Frame(GoomeMod m)
    {
        Time.frameCount++;
        m.Update();
        UEvent.current = new UEvent { type = UnityEngine.EventType.Layout };
        m.OnGUI();
        UEvent.current = new UEvent { type = UnityEngine.EventType.Repaint };
        m.OnGUI();
    }

    static void Mouse(float x, float yFromTop) => Fake.position = new Vector3(x, Screen.height - yFromTop, 0);

    static void Tap(GoomeMod m, KeyCode k)
    {
        Fake.keys.Add(k);
        Frame(m);
        Fake.keys.Remove(k);
        Frame(m);
    }

    static void Reset()
    {
        Fake.keys.Clear();
        Array.Clear(Fake.mouse, 0, 3);
        Fake.typed = "";
        Fake.off = false;
        Application.isFocused = true;
        UnityEngine.InputSystem.Mouse.current = null;
        UnityEngine.InputSystem.Keyboard.current = null;
        InputSystem.actions.Clear();
    }

    static bool GameSeesClick => UInput.GetMouseButton(0);

    static bool GameSeesW => UInput.GetKey(KeyCode.W);

    static int Main(string[] args)
    {
        string os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
        string rid = os + "-" + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        string dir = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native");
        Check(NativeLoader.Load(Path.Combine(dir, "nope"), dir), "native lib loads from " + dir);
        Check(NativeLoader.Loaded != null && NativeLoader.Loaded.EndsWith(NativeLoader.FileName), "loaded path " + NativeLoader.Loaded);

        Legacy();
        Check(InputBlock.Patched == 0, "everything unpatched after dispose");
        Check(Harmony.GetPatchInfo(typeof(UInput).GetMethod("GetMouseButton")) is not { Prefixes: { Count: > 0 } }, "GetMouseButton has no prefix left");
        NewInputOnly();
        Crashes();
        Console.WriteLine($"{passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }

    static bool SameAsFrame(GoomeMod m)
    {
        if (m.Texture == null || !m.Goome!.TryFrame(out var f)) return false;
        for (int y = 0; y < f.Height; y++)
            for (int x = 0; x < f.Width; x++)
            {
                uint p = f[x, y], a = p >> 24, want = p;
                if (a == 0) want = 0;
                else if (a < 255)
                {
                    uint half = a / 2;
                    want = a << 24 | Math.Min(255u, ((p >> 16 & 255) * 255 + half) / a) << 16 | Math.Min(255u, ((p >> 8 & 255) * 255 + half) / a) << 8 | Math.Min(255u, ((p & 255) * 255 + half) / a);
                }
                if (m.Texture.Px(x, y) != want) return false;
            }
        return true;
    }

    static double sx = 40, sy = 40, sw = 320, sh = 0, speed = 1;
    static int tab;
    static bool god;
    static string text = "";

    static void Menu(Ui ui)
    {
        if (ui.Window("Mod menu", ref sx, ref sy, ref sw, ref sh, WindowFlags.Move))
        {
            ui.Tabs("tabs", new[] { "Main", "World" }, ref tab);
            ui.Toggle("God mode", ref god);
            ui.Slider("Speed", ref speed, 0, 5);
            ui.Input("Name", ref text, 32);
        }
        ui.WindowEnd();
    }

    static GoomeMod? current;

    static void CloseFromMenu(Ui ui)
    {
        if (current != null) current.Open = false;
    }

    static void Legacy()
    {
        Reset();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        using var m = new GoomeMod("legacy", "F8");
        current = m;
        m.Draw += Menu;
        Check(InputBlock.Patched >= 15, $"patched {InputBlock.Patched} Input getters");

        Mouse(100, 100);
        Fake.mouse[0] = true;
        Fake.keys.Add(KeyCode.W);
        Frame(m);
        Check(!m.Open && GameSeesClick && GameSeesW, "closed menu lets everything through");
        Check(m.Uploads == 0 && GUI.draws == 0, "closed menu draws nothing");
        Fake.mouse[0] = false;
        Fake.keys.Remove(KeyCode.W);

        Tap(m, KeyCode.F8);
        Check(m.Open, "F8 opens it");
        Check(m.Uploads > 0 && m.Texture != null && m.Texture.loads == m.Uploads, $"frames uploaded {m.Uploads}");
        Check(m.FramesDrawn > 0 && GUI.draws == m.FramesDrawn && GUI.last == m.Texture, $"texture drawn in OnGUI repaint only ({GUI.draws})");
        Check(m.Texture != null && m.Texture.width == Screen.width && m.Texture.height == Screen.height, $"texture is screen sized {m.Texture?.width}x{m.Texture?.height}");
        Check(m.Texture != null && m.Texture.Px(100, 100) >> 24 == 255 && m.Texture.Px(700, 500) >> 24 == 0, "window pixels where the window is, clear elsewhere");
        Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "cursor freed while open");
        Check(SameAsFrame(m), "texture is the frame unpremultiplied and flipped");
        int ups = m.Uploads;
        Frame(m);
        Frame(m);
        Check(m.Uploads == ups, $"nothing changed, nothing uploaded ({m.Uploads - ups})");
        Mouse(60, 140);
        Frame(m);
        Frame(m);
        Check(m.Uploads > ups && SameAsFrame(m), "hover redraw copies the dirty rows and still matches");

        Mouse(100, 100);
        Frame(m);
        Check(m.WantsMouse && m.PointerOverUi, "pointer over the window wants the mouse");
        Fake.mouse[0] = true;
        Fake.keys.Add(KeyCode.W);
        Check(!GameSeesClick && !UInput.GetMouseButtonDown(0) && UInput.GetAxis("Mouse X") == 0 && !UInput.GetButton("Fire1"), "game mouse blocked over the window");
        Check(GameSeesW && UInput.GetAxis("Vertical") == 1, "keys still go to the game");
        Check(!UInput.GetKey(KeyCode.Mouse0), "GetKey(Mouse0) blocked too");
        var before = UInput.mousePosition;
        Mouse(110, 110);
        Check(UInput.mousePosition.x == before.x, "mousePosition frozen while blocked");
        Fake.keys.Remove(KeyCode.W);

        Mouse(100, 100);
        Frame(m);
        Mouse(700, 500);
        Frame(m);
        Check(m.WantsMouse && !GameSeesClick, "drag that started on the window stays on it");
        Fake.mouse[0] = false;
        Frame(m);
        Frame(m);
        Fake.mouse[0] = true;
        Check(!m.WantsMouse && GameSeesClick, "off the window the game gets the mouse");
        Check(UInput.mousePosition.y == Screen.height - 500, "mousePosition moves again");
        Frame(m);
        Check(!m.WantsMouse, "click that started on the game isnt taken");
        Fake.mouse[0] = false;
        Frame(m);

        Mouse(100, 100);
        Frame(m);
        Fake.mouse[0] = true;
        Check(!GameSeesClick, "blocked again");
        Application.isFocused = false;
        Check(GameSeesClick, "focus loss unblocks right away");
        Frame(m);
        Check(!m.WantsMouse, "unfocused update drops wants");
        Application.isFocused = true;
        Fake.mouse[0] = false;
        Frame(m);
        Frame(m);
        Fake.mouse[0] = true;
        Check(!GameSeesClick, "blocked after focus is back");

        Time.frameCount += 2;
        Check(GameSeesClick, "no Update for 2 frames unblocks");
        Fake.mouse[0] = false;
        Frame(m);
        Frame(m);
        Fake.mouse[0] = true;
        Check(!GameSeesClick, "blocked once updates run again");

        m.SceneChanged();
        Check(GameSeesClick, "scene change unblocks");
        Fake.mouse[0] = false;
        Frame(m);
        Mouse(100, 100);
        Frame(m);
        Fake.mouse[0] = true;
        Frame(m);
        Mouse(700, 500);
        Frame(m);
        Check(m.WantsMouse, "dragging off the window");
        SceneManager.active = 2;
        Frame(m);
        Check(!m.WantsMouse && GameSeesClick, "new scene noticed by polling drops the drag");
        Fake.mouse[0] = false;
        Frame(m);

        bool hitName = false;
        for (int y = 60; y < 300 && !hitName; y += 4)
        {
            Mouse(120, y);
            Frame(m);
            Fake.mouse[0] = true;
            Frame(m);
            Fake.mouse[0] = false;
            Frame(m);
            hitName = m.WantsKeyboard;
        }
        Check(hitName, "clicking the text box takes the keyboard");
        Fake.keys.Add(KeyCode.W);
        Fake.typed = "hey";
        Check(!GameSeesW && UInput.inputString == "" && !UInput.anyKey, "game keys blocked while typing");
        Frame(m);
        Fake.typed = "";
        Fake.keys.Remove(KeyCode.W);
        Frame(m);
        Check(text == "hey", $"typed text reached goome ({text})");

        Tap(m, KeyCode.F8);
        Fake.mouse[0] = true;
        Fake.keys.Add(KeyCode.W);
        Check(!m.Open && GameSeesClick && GameSeesW, "closing with F8 gives everything back");
        Fake.mouse[0] = false;
        Fake.keys.Remove(KeyCode.W);
        Tap(m, KeyCode.F8);
        Mouse(100, 100);
        Frame(m);
        Check(m.WantsMouse, "open again and over the window");
        m.Draw += CloseFromMenu;
        m.Redraw();
        Frame(m);
        m.Draw -= CloseFromMenu;
        Fake.mouse[0] = true;
        Check(!m.Open && !m.WantsMouse && GameSeesClick, "menu closing itself mid draw doesnt leave a block");
        Fake.mouse[0] = false;
        Frame(m);
        Tap(m, KeyCode.F8);
        Tap(m, KeyCode.F8);
        Fake.mouse[0] = true;
        Fake.keys.Add(KeyCode.W);
        Check(!Cursor.visible && Cursor.lockState == CursorLockMode.Locked, "cursor put back");
        Fake.mouse[0] = false;
        Fake.keys.Remove(KeyCode.W);
        int draws = GUI.draws;
        Frame(m);
        Check(GUI.draws == draws, "closed menu stops drawing");

        m.BlockAllWhileOpen = true;
        Tap(m, KeyCode.F8);
        Mouse(700, 500);
        Frame(m);
        Fake.mouse[0] = true;
        Fake.keys.Add(KeyCode.W);
        Check(!GameSeesClick && !GameSeesW, "BlockAllWhileOpen blocks everything");
        m.Dispose();
        Check(GameSeesClick && GameSeesW, "dispose unblocks");
    }

    static void NewInputOnly()
    {
        Reset();
        Fake.off = true;
        var mouse = new UnityEngine.InputSystem.Mouse();
        var kb = new UnityEngine.InputSystem.Keyboard();
        UnityEngine.InputSystem.Mouse.current = mouse;
        UnityEngine.InputSystem.Keyboard.current = kb;
        Vector2 pos = new Vector2(100, Screen.height - 100);
        bool left = false;
        mouse.position.read = () => pos;
        mouse.leftButton.read = () => left;
        var look = new InputAction("look") { controls = { new InputControl { device = mouse } } };
        var move = new InputAction("move") { controls = { new InputControl { device = kb } } };
        InputSystem.actions.Add(look);
        InputSystem.actions.Add(move);

        var m = new GoomeMod("newinput", "F8");
        string log = "";
        m.Log = s => log += s;
        m.Draw += Menu;
        sx = sy = 40;
        kb.down.Add(Key.F8);
        Frame(m);
        kb.down.Remove(Key.F8);
        Frame(m);
        Check(m.Open && m.UsingInputSystem, $"F8 through the Input System opens it {m.Open} {m.UsingInputSystem} {log}");
        Frame(m);
        Check(m.WantsMouse && !look.enabled && move.enabled, "mouse actions off over the window, keyboard ones on");
        pos = new Vector2(700, 100);
        Frame(m);
        Frame(m);
        Check(!m.WantsMouse && look.enabled, "mouse actions back on off the window");
        pos = new Vector2(100, Screen.height - 100);
        Frame(m);
        Check(!look.enabled, "off again");
        m.FocusChanged(false);
        Check(look.enabled, "focus loss turns them back on");
        Frame(m);
        Check(!look.enabled, "and off again once focused");
        m.Dispose();
        Check(look.enabled && move.enabled, "dispose turns every action back on");
    }

    static void Crashes()
    {
        Reset();
        var m = new GoomeMod("crash", "F8");
        bool boom = false;
        m.Draw += ui =>
        {
            Menu(ui);
            if (boom) throw new Exception("menu bug");
        };
        string log = "";
        m.Log = s => log += s;
        Tap(m, KeyCode.F8);
        Mouse(100, 100);
        Frame(m);
        Check(m.WantsMouse, "blocked before the crash");
        boom = true;
        m.Redraw();
        Frame(m);
        Check(log.Contains("menu bug"), "menu exception logged");
        m.Dispose();
        Fake.mouse[0] = true;
        Check(GameSeesClick, "dispose after a crash unblocks");
        Check(InputBlock.Patched == 0, "unpatched");
    }
}
