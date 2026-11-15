using System.IO;
using BepInEx;
using GoomeUI.ModKit;

namespace GoomeMenu
{
    [BepInPlugin("com.you.goomemenu", "Goome Menu", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        GoomeMod? mod;

        void Awake()
        {
            var hotkey = Config.Bind("Menu", "Hotkey", "F8", "Key that opens and closes the menu, any UnityEngine.KeyCode name");
            mod = new GoomeMod(Info.Metadata.GUID, hotkey.Value, Path.GetDirectoryName(Info.Location));
            mod.Log = s => Logger.LogWarning(s);
            new Menu(mod);
            Logger.LogInfo($"goome menu loaded, {hotkey.Value} opens it");
        }

        void Update() => mod?.Update();

        void OnGUI() => mod?.OnGUI();

        void OnApplicationFocus(bool focused) => mod?.FocusChanged(focused);

        void OnDisable() => mod?.Release();

        void OnDestroy()
        {
            mod?.Dispose();
            mod = null;
        }
    }
}
