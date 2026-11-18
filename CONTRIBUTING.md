<h1 align="center">🛠 Contributing to csharp-goome 🛠</h1>

<p align="center">
Small PRs that fix one thing are way easier for me to check, so those get merged a lot faster.<br>
This repo is only the C# side. If the bug is in goome itself (drawing, a backend, the ui kit) it goes to <a href="https://github.com/goome-ui/c-goome">c-goome</a>. Docs fixes go to the <a href="https://goome.wtf/docs/">docs</a> and new example projects to <a href="https://github.com/goome-ui/examples">goome-ui/examples</a>.
</p>

<details>
<summary align="center"><b>📋 The short version 📋</b></summary>

<p align="center">
• One thing per PR. One fix or one feature, if it does two things it gets closed<br>
• Open your PR against the <b>dev</b> branch, not main<br>
• Change the C# in <code>src/</code>, <code>examples/</code>, <code>tests/</code>, the unity bits in <code>unity/com.goome-ui.goome/Runtime/</code> and the mod helper and templates in <code>mods/</code>, never <code>native/goome.h</code><br>
• Dont touch the protected files, a bot closes the PR if you do<br>
• Test it and say how you tested it, and on which OS<br>
• CI has to pass<br>
• AI is fine, but say you used it, and you need to know what every line does
</p>
</details>

<details>
<summary align="center"><b>💬 Before you start 💬</b></summary>

<p align="center">
Small fixes can go straight to a PR. For a new feature or anything big, <a href="https://github.com/goome-ui/csharp-goome/issues">open an issue</a> first so we can talk about it.<br> <br>
Same for new public types or methods, always an issue first. Once people start using them they cant really change anymore, so Id rather get the name and shape right before its in.
</p>
</details>

<details>
<summary align="center"><b>🌿 Branches 🌿</b></summary>

<p align="center">
You only ever PR into <b>dev</b>, I move stuff up to beta and main myself once its been around for a bit.
</p>
</details>

<details>
<summary align="center"><b>🚫 What gets closed automatically 🚫</b></summary>

<p align="center">
A bot checks every PR and closes it with a comment if it hits any of these. Dont take it personally, fix it and open a new one.<br> <br>
• PRs that arent against <b>dev</b><br>
• Anything in <code>.github/</code><br>
• <code>native/goome.h</code> or <code>native/VERSION</code>, fix goome in c-goome instead<br>
• <code>build.sh</code>, <code>tools/</code>, <code>runtimes/</code>, any <code>.csproj</code>, <code>.props</code>, <code>.targets</code>, <code>.sln</code>, <code>nuget.config</code> or <code>global.json</code>. Those run on peoples computers the second they hit build, before anyone even looks at the code<br>
• The prebuilt libs in <code>unity/com.goome-ui.goome/Runtime/Plugins/</code>, every unity <code>.meta</code> file and the <code>.asmdef</code>. You cant review a binary in a PR, so those only come from my CI runs<br>
• Any <code>.dll</code>, <code>.so</code>, <code>.dylib</code> or <code>.exe</code> in <code>mods/</code>. The mods copy the libs from the unity Plugins folder when they build. <code>mods/nuget.config</code> and the <code>mods/Directory.Build.*</code> files count as project files too<br>
• <code>LICENSE</code> and the pictures (<code>docs/logo*</code>, <code>docs/demo.gif</code>, <code>docs/screenshot.png</code>)<br>
• Over 3000 changed files<br> <br>
Renaming or moving any of those counts too. Need a new package or a project change? <a href="https://github.com/goome-ui/csharp-goome/issues">Open an issue</a> and Ill do it.
</p>
</details>

<details>
<summary align="center"><b>📄 native/goome.h 📄</b></summary>

- `native/goome.h` is c-goome's single `goome.h`, copied as is
- `native/VERSION` is the c-goome commit it came from
- updating means `tools/update_native.sh ../c-goome` (or a path to any committed `goome.h`, like one in a c-goome worktree), then fixing whatever changed in `src/GoomeSharp/`
- if a struct moved the layout test catches it
- then `tools/sync_unity.sh` copies the binding into the unity package, CI fails if you forget
- the unity Plugins libs come from the `unity-plugins` artifact of a green CI run, copy the three files over and commit them
</details>

<details>
<summary align="center"><b>🤖 Using AI 🤖</b></summary>

<p align="center">
Using AI to help write your PR is allowed, just say in the PR that it helped.<br> <br>
You still have to know what your code does. If I ask about a line you should be able to answer without going back to the AI.<br> <br>
And test it yourself, untested AI dumps get closed.
</p>
</details>

<details>
<summary align="center"><b>🧪 Testing 🧪</b></summary>

<p align="center">
Build the native lib, then run the tests. Everything runs on goome's test backend, so nothing shows up on your screen:
</p>

```sh
./build.sh
dotnet run --project tests/GoomeSharp.Tests

# only some of them
dotnet run --project tests/GoomeSharp.Tests -- shapes ui

# an example on the test backend, with a screenshot of every frame in shots/
GOOME_BACKEND=test GOOME_TEST_SHOTS=shots/ GOOME_TEST_SCRIPT="screen 1280x720 1; end 10" dotnet run --project examples/UiDemo

# the app example in window mode, closing it at frame 6
GOOME_BACKEND=test GOOME_TEST_SHOTS=shots/ GOOME_TEST_SCRIPT="screen 1x1 1; 6 close; end 10" dotnet run --project examples/App

# the unity code against fake UnityEngine types, both input paths
tools/sync_unity.sh
dotnet build tests/UnityStubs
dotnet build tests/UnityStubs -p:InputSystem=true

# the mod helper against fake UnityEngine types with real harmony patches, then the mod templates
dotnet run --project tests/ModTests
dotnet build mods/templates/BepInEx5
dotnet build mods/templates/BepInEx6
dotnet build mods/templates/MelonLoader
```

<p align="center">
On windows run <code>./build.sh</code> from git bash with mingw gcc on PATH. <code>./build.sh win</code> cross builds goome.dll from linux.<br> <br>
The pixel hashes in the tests are the same ones c-goome checks, they have to come out the same on every OS. If one moves, something is passing the wrong values to goome, dont just update the number.<br> <br>
The layout test compares every struct in <code>src/GoomeSharp/Types.cs</code> against what the C compiler says (<code>tests/layout.c</code>). If you touch a struct, that test has to stay green on all four CI machines.<br> <br>
The frame test runs the same offscreen ui session in C (<code>tests/frame.c</code>) and in C#, and the pixels have to hash the same.<br> <br>
Theres no Unity in CI, so <code>tests/UnityStubs</code> builds the package code against tiny fake versions of the Unity types. It catches typos and name clashes (Unity has its own <code>Event</code>, <code>Canvas</code>, <code>RawImage</code>...), not behaviour. If you change anything in <code>unity/</code>, try it in a real Unity and say which version in the PR.<br> <br>
<code>tests/ModTests</code> does the same for <code>mods/GoomeMod/</code>, but it actually runs: it patches fake <code>Input</code> methods with Harmony and checks the game gets blocked and unblocked at the right times (hover, drag, typing, focus loss, scene change, unload) and that frames get drawn. The templates in <code>mods/templates/</code> only get compiled. If you touch <code>mods/</code>, try it in a real game too and say which game, loader and Mono or IL2CPP.<br> <br>
In the PR write a few lines on what you ran and on which OS. Changes you can see (examples, anything drawn) need a before and after screenshot.<br> <br>
CI runs on linux, windows plus both macs when you open the PR. It has to be green before I merge.
</p>
</details>

<details>
<summary align="center"><b>✏️ Code style ✏️</b></summary>

<p align="center">
• C# 9 with nullable on, the library builds for netstandard2.0 (older Unity and mod loaders), netstandard2.1 (Unity) and net8.0. No newer language stuff, Unitys compiler stops at C# 9<br>
• No new NuGet packages. The library has zero dependencies and that stays, apart from System.Memory on netstandard2.0 since thats where Span lives there<br>
• <code>mods/GoomeMod/</code> only gets HarmonyX and the Unity reference dlls, both compile only. Every loader ships its own Harmony<br>
• P/Invoke goes in <code>Native.cs</code> as <code>DllImport</code> with only numbers and pointers in it, so nothing needs marshalling on .NET, Mono or IL2CPP. Strings go in as <code>byte*</code> through <code>U8</code> and come back as <code>byte*</code><br>
• Never P/Invoke a C varargs function (it crashes on arm64 macs), wrap it in <code>native/goome_lib.c</code> instead<br>
• C structs keep their exact field order, and a C <code>long</code> is <code>CLong</code> on net8.0 (look at <code>Pacer</code> for the netstandard2.1 side)<br>
• Native memory gets freed in <code>Dispose</code>, and using something after that throws instead of crashing<br>
• Nothing allocates per frame that doesnt have to. Events, canvases, pixels: all structs or spans<br>
• Match the code around it, same naming and formatting. Comments only where something isnt obvious<br>
• Dont reformat whole files, it makes the diff impossible to read
</p>
</details>

<details>
<summary align="center"><b>💬 Discussions and first issues 💬</b></summary>
<p align="center">
Questions and stuff you made with goome go in <a href="https://github.com/goome-ui/c-goome/discussions">Discussions</a>.<br>
If you want something to work on, the <a href="https://github.com/goome-ui/csharp-goome/labels/good%20first%20issue">good first issue</a> ones are small. Comment on one before you start.
</p>
</details>

<details>
<summary align="center"><b>🛡️ Security 🛡️</b></summary>

<p align="center">
Found a security hole? Dont open a public issue or PR for it, read <a href="https://github.com/goome-ui/.github/blob/main/SECURITY.md">SECURITY.md</a> and tell me privately first.
</p>
</details>

<p align="center">
By sending a PR you agree your code goes out under the same <a href="LICENSE">GPL-3.0 license</a> as the rest of csharp-goome.
</p>
