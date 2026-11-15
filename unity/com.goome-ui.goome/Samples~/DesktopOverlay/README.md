# Desktop Overlay

1. Put `GoomeOverlay` and `DesktopOverlay` on an empty GameObject.
2. Make a standalone build (Linux, Windows or Mac) and run it. The goome window floats over everything, even other apps, and only the window takes clicks.

In the editor it stays off unless you tick `runInEditor` on the GoomeOverlay. Tick `appWindow` instead if you want a normal second window rather than an overlay.
