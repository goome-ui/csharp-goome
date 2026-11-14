<h1 align="center">🫠 goome for Unity 🫠</h1>

<p align="center">
<b>goome's ui kit inside your game, plus real desktop overlays from a standalone build.</b>
</p>

<p align="center">
This is the C# binding from <a href="https://github.com/goome-ui/csharp-goome">csharp-goome</a> packed up for Unity 2021.3 or newer, with the native lib already in <code>Runtime/Plugins</code> for Linux x64, Windows x64 plus macOS (arm64 and intel in one file).
</p>

<h3 align="center">⬇️ Install</h3>

<p align="center">
Window > Package Manager > the + button > Add package from git URL, and paste:
</p>

```
https://github.com/goome-ui/csharp-goome.git?path=/unity/com.goome-ui.goome
```

<p align="center">
Want it pinned so it never changes under you? Put <code>#</code> and a commit hash on the end. The samples are in the Package Manager under goome > Samples.
</p>

<details>
<summary align="center"><b>🎮 In Game UI 🎮</b></summary>

<br>

<p align="center">
Put a <b>GoomeView</b> on a RawImage (or on a mesh with a MeshCollider) and hook <code>Draw</code>:
</p>

```csharp
using GoomeSharp;
using GoomeUI;
using UnityEngine;

public class Hud : MonoBehaviour
{
    public GoomeView view;
    double x = 20, y = 20, w = 260, h = 0, volume = 0.5;

    void OnEnable() => view.Draw += ui =>
    {
        if (ui.Window("Settings", ref x, ref y, ref w, ref h, WindowFlags.Move))
        {
            ui.Slider("Volume", ref volume, 0, 1, "%.2f");
            if (ui.Button("Respawn")) Debug.Log("respawn");
        }
        ui.WindowEnd();
    };
}
```

<p align="center">
goome draws into its own pixel buffer (offscreen mode, no window, no threads), GoomeView uploads it to a Texture2D and shows it. It feeds goome the mouse and scroll wheel, plus keys and typed text, from the new Input System if your project has it and the old <code>Input</code> class if not.<br> <br>
Clicks only land on the ui where goome actually drew something that takes clicks. Everywhere else they go through to your game, and <code>view.PointerOverUi</code> tells your own code when the mouse is on the ui so you dont shoot through a menu.<br> <br>
Copy and paste go through <code>GUIUtility.systemCopyBuffer</code>, so ctrl+c in a goome input lands in the real clipboard. The cursor shape goome asks for (a text beam, resize arrows, that kind of thing) gets set from the <code>cursors</code> pictures on the component if you give it some, and hidden when goome wants it hidden.<br> <br>
The pixels are premultiplied BGRA, so it comes with its own <code>GoomeUI/Premultiplied</code> shader (blend One OneMinusSrcAlpha), GoomeView sets it up on its own.
</p>
</details>

<details>
<summary align="center"><b>🖥️ Desktop Overlay 🖥️</b></summary>

<br>

<p align="center">
Put a <b>GoomeOverlay</b> on any GameObject and hook <code>Draw</code> the same way. In a standalone build it opens a real goome overlay, see-through and on top of every other app. Clicks only land on the windows you draw, the rest goes to whatever is under it. Tick <code>appWindow</code> to get a normal second window instead.<br> <br>
It stays off in the editor unless you tick <code>runInEditor</code>, an overlay over the editor gets in the way pretty fast.
</p>
</details>

<h3 align="center">📝 Good to know</h3>

- Desktop only (Linux x64, Windows x64, macOS). No consoles, phones or WebGL
- Everything goome has is in there, the [csharp-goome README](https://github.com/goome-ui/csharp-goome#readme) and the [docs](https://goome-ui.github.io/docs/) cover it
- [goome-ui/examples](https://github.com/goome-ui/examples) has a Unity project skeleton with both setups
- Call goome from the main thread, Update is fine
- In a linear color space project soft edges can look a tiny bit off, goome blends in sRGB
- `Runtime/GoomeSharp` is a copy made by `tools/sync_unity.sh`, change the real one in `src/GoomeSharp`

GPL-3.0, same as goome. If you ship a game with it, your game has to be GPL too.
