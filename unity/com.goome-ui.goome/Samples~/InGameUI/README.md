# In Game UI

1. Make a Canvas (Screen Space Overlay is fine) with a RawImage that fills it.
2. Put `GoomeView` on the RawImage and `InGameUI` on anything, then drag the RawImage into its `view` field.
3. Hit play. The window takes clicks, everything around it goes through to the game, and `view.PointerOverUi` tells your own code when not to shoot.

It uses `Input.GetMouseButtonDown`, so if your project is on the new Input System only, swap that line for `Mouse.current.leftButton.wasPressedThisFrame`.
