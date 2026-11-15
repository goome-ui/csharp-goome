# GoomeSharp

C# bindings for [goome](https://github.com/goome-ui/c-goome). See-through always on top overlays, normal app windows and offscreen ui you can draw into a game engine texture, all with the same immediate mode ui kit. Works on Linux (X11 and Wayland), Windows and macOS, the native lib for each one is inside the package.

```sh
dotnet add package GoomeSharp
```

## A normal app

```csharp
using GoomeSharp;

string name = "";
GoomeApp.Run("My app", 960, 640, ui =>
{
    ui.Heading("Hello");
    ui.Input("Name", ref name);
    if (ui.Button("Say hi")) ui.Toast($"hi {name}");
});
```

## An overlay on top of everything

```csharp
using GoomeSharp;

using var g = new Goome();
using var ui = new Ui(g);
double x = 40, y = 40, w = 260, h = 0;
for (bool on = true; on;)
{
    while (g.Poll(out var ev, 16)) { if (ev.Type == EventType.Quit) on = false; ui.Feed(ev); }
    if (ui.Begin(0) == null) continue;
    if (ui.Window("Hi", ref x, ref y, ref w, ref h, WindowFlags.Move) && ui.Button("Click me")) Console.WriteLine("hi");
    ui.WindowEnd();
    ui.End();
}
```

Docs are at [goome-ui.github.io/docs](https://goome-ui.github.io/docs/), the code and the Unity package are at [goome-ui/csharp-goome](https://github.com/goome-ui/csharp-goome). GPL-3.0.
