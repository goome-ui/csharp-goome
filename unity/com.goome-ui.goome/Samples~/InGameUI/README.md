# In Game UI

1. Make a Canvas (Screen Space Overlay is fine) with a RawImage that fills it.
2. Put `GoomeView` on the RawImage and `InGameUI` on anything, then drag the RawImage into its `view` field.
3. Put `MouseLook` on the camera and drag the RawImage into its `view` field too.
4. Hit play. The window takes clicks, everything around it goes through to the game, and `view.PointerOverUi` tells your own code when not to shoot. Hold right click to look around. Drag the Speed slider and keep going past the window, the camera doesnt move until you let go, because `MouseLook` checks `view.WantsMouse` first.

It uses `Input.GetMouseButtonDown` and `Input.GetAxis`, so if your project is on the new Input System only, swap those for `Mouse.current.leftButton.wasPressedThisFrame` and `Mouse.current.delta.ReadValue()`. If your look is an InputAction you dont even need the check, `blockGameInput` on the view turns it off while goome has the mouse.
