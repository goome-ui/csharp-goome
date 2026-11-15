using GoomeUI.ModKit;
using MelonLoader;
using MelonLoader.Utils;

[assembly: MelonInfo(typeof(GoomeMenu.Mod), "Goome Menu", "0.1.0", "you")]
[assembly: MelonGame]

namespace GoomeMenu
{
    public class Mod : MelonMod
    {
        GoomeMod? mod;

        public override void OnInitializeMelon()
        {
            var cat = MelonPreferences.CreateCategory("GoomeMenu");
            var hotkey = cat.CreateEntry("Hotkey", "F8", description: "Key that opens and closes the menu, any UnityEngine.KeyCode name");
            mod = new GoomeMod("GoomeMenu", hotkey.Value, MelonEnvironment.UserLibsDirectory);
            mod.Log = s => LoggerInstance.Warning(s);
            new Menu(mod);
            LoggerInstance.Msg("goome menu loaded, " + hotkey.Value + " opens it");
        }

        public override void OnUpdate() => mod?.Update();

        public override void OnGUI() => mod?.OnGUI();

        public override void OnSceneWasLoaded(int buildIndex, string sceneName) => mod?.SceneChanged();

        public override void OnApplicationQuit() => mod?.Release();

        public override void OnDeinitializeMelon()
        {
            mod?.Dispose();
            mod = null;
        }
    }
}
