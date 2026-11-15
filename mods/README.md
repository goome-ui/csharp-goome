<h1 align="center">🧩 goome mod menus 🧩</h1>

<p align="center">
<b>goome's ui kit as a mod menu in Unity games, for BepInEx 5, BepInEx 6 and MelonLoader.</b>
</p>

<p align="center">
<code>GoomeMod/</code> is the helper. It runs goome offscreen, copies the rows that changed into a Texture2D and draws it in OnGUI, feeds it the mouse and keys, and keeps the game from getting clicks and keys while goome wants them.<br>
<code>templates/</code> has a ready mod for each loader with a sample menu in it: F8 opens it, theres tabs, toggles and sliders.
</p>

<p align="center">
⚠️ Dont use this in online games with anti-cheat. Harmony patches on Input are exactly what anti-cheat looks for and you <b>will</b> get banned. Single player and your own games only.
</p>

<details>
<summary align="center"><b>🔨 Building 🔨</b></summary>

<p align="center">
You need the .NET 8 SDK. From the repo root:
</p>

```sh
dotnet build mods/templates/BepInEx5 -c Release
dotnet build mods/templates/BepInEx6 -c Release
dotnet build mods/templates/MelonLoader -c Release
```

<p align="center">
Every build makes a <code>dist/</code> folder next to the dll thats laid out like the game folder, so you copy whats inside it straight into the game. <code>netstandard2.0</code> and <code>net472</code> are the Mono builds, <code>net6.0</code> is IL2CPP.<br> <br>
The native libs come from <code>unity/com.goome-ui.goome/Runtime/Plugins/</code> (Windows, Linux and both macs).<br> <br>
Everything builds against the Unity 2019.4 reference dlls from NuGet, pick another version with <code>-p:UnityVersion=2022.3.62</code>. IL2CPP games get their own generated dlls, so build the mod against those with <code>-p:GameDlls=path/to/BepInEx/interop</code> (or <code>MelonLoader/Il2CppAssemblies</code>). GoomeMod.dll itself always uses the NuGet ones and finds the real Unity methods by name when the game loads it. I havent tried that on a real IL2CPP game yet, so if it breaks please tell me which game.
</p>
</details>

<details>
<summary align="center"><b>🥄 BepInEx 5 🥄</b></summary>

<p align="center">
Mono games only.<br> <br>
1. Install BepInEx 5 and start the game once<br>
2. Copy <code>templates/BepInEx5/bin/Release/netstandard2.0/dist/BepInEx</code> into the game folder<br>
3. Start the game and press F8<br> <br>
Everything ends up in <code>BepInEx/plugins/GoomeMenu/</code>, the mod dll, GoomeMod.dll, GoomeSharp.dll, the System.Memory dlls and goome.dll, libgoome.so plus libgoome.dylib. The hotkey is in <code>BepInEx/config/com.you.goomemenu.cfg</code>.
</p>
</details>

<details>
<summary align="center"><b>🥄 BepInEx 6 🥄</b></summary>

<p align="center">
Mono: copy <code>templates/BepInEx6/bin/Release/netstandard2.0/dist/BepInEx</code> into the game folder.<br>
IL2CPP: start the game once so BepInEx makes <code>BepInEx/interop</code>, build with <code>-p:GameDlls=</code> pointing at it, then copy <code>bin/Release/net6.0/dist/BepInEx</code> in.<br> <br>
Same <code>BepInEx/plugins/GoomeMenu/</code> folder as BepInEx 5. On IL2CPP theres no System.Memory dlls, .NET 6 already has them.
</p>
</details>

<details>
<summary align="center"><b>🍈 MelonLoader 🍈</b></summary>

<p align="center">
Install MelonLoader 0.6 or newer and start the game once. Then copy the <code>Mods</code> and <code>UserLibs</code> folders from <code>templates/MelonLoader/bin/Release/net472/dist/</code> (Mono) or <code>net6.0/dist/</code> (IL2CPP) into the game folder.<br> <br>
Only the mod dll goes in <code>Mods/</code>. GoomeMod, GoomeSharp and the native libs go in <code>UserLibs/</code>, because MelonLoader tries to load every dll in Mods as a mod. The hotkey is in <code>UserData/MelonPreferences.cfg</code>.
</p>
</details>

<details>
<summary align="center"><b>🔌 How the native lib gets found 🔌</b></summary>

<p align="center">
<code>NativeLoader</code> loads <code>goome.dll</code>, <code>libgoome.so</code> or <code>libgoome.dylib</code> by its full path before anything calls into goome. It looks in the folders you pass to <code>new GoomeMod(...)</code> (the plugin folder for BepInEx, UserLibs for MelonLoader), then next to GoomeMod.dll and GoomeSharp.dll.<br> <br>
<b>IL2CPP (.NET 6)</b> - <code>NativeLibrary.Load</code> plus a DllImport resolver on GoomeSharp, so <code>[DllImport("goome")]</code> goes to that exact file<br>
<b>Mono on Windows</b> - <code>SetDllDirectory</code> on the folder and <code>LoadLibrary</code>, after that windows hands back the loaded goome.dll<br>
<b>Mono on Linux and mac</b> - <code>dlopen</code> with the full path. Mono also looks next to the dll that has the DllImport, so thats two ways in
</p>
</details>

<details>
<summary align="center"><b>🎮 Your own menu 🎮</b></summary>

```csharp
var mod = new GoomeMod("my.mod", "F8", pluginFolder);
mod.Draw += ui =>
{
    if (ui.Window("My mod", ref x, ref y, ref w, ref h, WindowFlags.Move))
    {
        ui.Toggle("Fly", ref fly);
        ui.Slider("Speed", ref speed, 1, 10);
    }
    ui.WindowEnd();
};
```

<p align="center">
Call <code>mod.Update()</code> from Update, <code>mod.OnGUI()</code> from OnGUI and <code>mod.Dispose()</code> when the mod unloads. The templates already do that.<br> <br>
<b>Open</b> - shows or hides the menu, the hotkey flips it<br>
<b>WantsMouse / WantsKeyboard</b> - on while the mouse is on the menu (or dragging something from it) and while a text box has the keys<br>
<b>BlockAllWhileOpen</b> - the game gets nothing at all while the menu is open<br>
<b>Scale</b> - makes the whole menu bigger or smaller<br>
<b>Redraw()</b> - draws a new frame. goome only draws when something changed, set <code>RedrawEveryFrame</code> if your menu shows live numbers
</p>
</details>

<details>
<summary align="center"><b>🚫 Input blocking 🚫</b></summary>

<p align="center">
Harmony prefixes on <code>UnityEngine.Input</code> (GetKey, GetKeyDown/Up, GetMouseButton*, GetButton*, GetAxis, GetAxisRaw, anyKey, anyKeyDown, mouseScrollDelta, inputString) return nothing while goome wants that input, and <code>mousePosition</code> stays put so mouse look doesnt move. Mouse axes and Mouse0-6 count as mouse, everything else as keyboard.<br> <br>
If the game uses the new Input System, every enabled InputAction on the mouse or keyboard gets turned off and exactly those get turned back on after. goome reads the devices itself so it keeps working.<br> <br>
It never leaves the game stuck: blocking stops right away when the game loses focus, on a scene change, when the mod unloads or gets disabled, and when <code>Update()</code> hasnt run for 2 frames (like when the mod crashed).<br> <br>
On IL2CPP the Input patches go through Il2CppInterop's native detours. Game code that got inlined by IL2CPP can skip them, theres nothing a mod can do about that.
</p>
</details>

<details>
<summary align="center"><b>🧪 Testing 🧪</b></summary>

<p align="center">
Theres no game in CI. <code>tests/ModTests</code> runs GoomeMod against fake UnityEngine types with real Harmony patches and checks when input gets blocked and given back, and that frames get uploaded and drawn:
</p>

```sh
./build.sh
dotnet run --project tests/ModTests -c Release
```

<p align="center">
The templates only get compiled in CI. If you change one, try it in a real game and say which game and loader in the PR.
</p>
</details>
