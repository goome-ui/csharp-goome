<h1 align="center">🛠 Contributing to csharp-goome 🛠</h1>

<p align="center">
Hey, so you want to help out with this? Cool. Small PRs that fix one thing are way easier for me to check, so those get merged a lot faster.<br>
This repo is only the C# side. If the bug is in goome itself (drawing, a backend, the ui kit) it goes to <a href="https://github.com/goome-ui/c-goome">c-goome</a>.
</p>

<details>
<summary align="center"><b>📋 The short version 📋</b></summary>

<p align="center">
• One thing per PR. One fix or one feature, if it does two things it gets closed<br>
• Open your PR against the <b>dev</b> branch, not main<br>
• Change the C# in <code>src/</code>, <code>examples/</code> and <code>tests/</code>, never <code>native/goome.h</code><br>
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
Same for new public types or methods, always an issue first. Once people start using them they can't really change anymore, so I'd rather get the name and shape right before it's in.
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
• PRs that arent against <b>dev</b>. main and beta only get stuff that already sat on dev for a while<br>
• Anything in <code>.github/</code>, since thats CI and these rules and a PR could just turn them off for itself<br>
• <code>native/goome.h</code> or <code>native/VERSION</code>. Both come from c-goome through <code>tools/update_native.sh</code>, so fix goome over there<br>
• <code>build.sh</code>, <code>tools/</code>, <code>runtimes/</code>, any <code>.csproj</code>, <code>.props</code>, <code>.targets</code>, <code>.sln</code>, <code>nuget.config</code> or <code>global.json</code>. Those run on peoples computers the second they hit build, and a bad one in there could do pretty much anything to someone's machine before they even look at the code, so I keep all of that myself<br>
• <code>LICENSE</code> and the pictures (<code>docs/logo*</code>, <code>docs/demo.gif</code>, <code>docs/screenshot.png</code>)<br>
• Over 3000 changed files, I'm not reading that<br> <br>
Renaming or moving any of those counts too. Need a new package or a project change? <a href="https://github.com/goome-ui/csharp-goome/issues">Open an issue</a> and Ill do it.
</p>
</details>

<details>
<summary align="center"><b>📄 native/goome.h 📄</b></summary>

- `native/goome.h` is c-goome's single `goome.h`, copied as is
- `native/VERSION` is the c-goome commit it came from
- updating means `tools/update_native.sh ../c-goome`, then fixing whatever changed in `src/GoomeSharp/`
- if a struct moved the layout test catches it
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
```

<p align="center">
On windows run <code>./build.sh</code> from git bash with mingw gcc on PATH. <code>./build.sh win</code> cross builds goome.dll from linux.<br> <br>
The pixel hashes in the tests are the same ones c-goome checks, they have to come out the same on every OS. If one moves, something is passing the wrong values to goome, dont just update the number.<br> <br>
The layout test compares every struct in <code>src/GoomeSharp/Types.cs</code> against what the C compiler says (<code>tests/layout.c</code>). If you touch a struct, that test has to stay green on all four CI machines.<br> <br>
In the PR write a few lines on what you ran and on which OS. Changes you can see (examples, anything drawn) need a before and after screenshot.<br> <br>
CI runs on linux, windows and both macs when you open the PR. It has to be green before I merge.
</p>
</details>

<details>
<summary align="center"><b>✏️ Code style ✏️</b></summary>

<p align="center">
• C# with nullable on, target stays net8.0<br>
• No new NuGet packages. The library has zero dependencies and that stays<br>
• P/Invoke goes in <code>Native.cs</code> with <code>LibraryImport</code>, never <code>DllImport</code>. Strings that goome hands back come back as <code>byte*</code>, a <code>string</code> return would get freed by the marshaller<br>
• Never P/Invoke a C varargs function (it crashes on arm64 macs), wrap it in <code>native/goome_lib.c</code> instead<br>
• C structs keep their exact field order, and a C <code>long</code> is <code>CLong</code>, never <code>long</code> or <code>int</code><br>
• Native memory gets freed in <code>Dispose</code>, and using something after that throws instead of crashing<br>
• Nothing allocates per frame that doesnt have to. Events, canvases and pixels are structs or spans<br>
• Match the code around it, same naming and formatting. Comments only where something isnt obvious<br>
• Dont reformat whole files, it makes the diff impossible to read
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
