using System.IO;
using BepInEx;
using GoomeUI.ModKit;
#if IL2CPP
using System;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
#else
using BepInEx.Unity.Mono;
#endif

namespace GoomeMenu
{
#if IL2CPP
    [BepInPlugin("com.you.goomemenu", "Goome Menu", "0.1.0")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            var hotkey = Config.Bind("Menu", "Hotkey", "F8", "Key that opens and closes the menu, any UnityEngine.KeyCode name");
            var mod = new GoomeMod("com.you.goomemenu", hotkey.Value, Path.GetDirectoryName(typeof(Plugin).Assembly.Location));
            mod.Log = s => Log.LogWarning(s);
            new Menu(mod);
            Driver.Mod = mod;
            AddComponent<Driver>();
            Log.LogInfo("goome menu loaded, " + hotkey.Value + " opens it");
        }

        public override bool Unload()
        {
            Driver.Mod?.Dispose();
            Driver.Mod = null;
            return true;
        }
    }

    public class Driver : MonoBehaviour
    {
        internal static GoomeMod? Mod;

        public Driver(IntPtr ptr) : base(ptr)
        {
        }

        void Update() => Mod?.Update();

        void OnGUI() => Mod?.OnGUI();

        void OnApplicationFocus(bool focused) => Mod?.FocusChanged(focused);

        void OnDisable() => Mod?.Release();

        void OnDestroy() => Mod?.Release();
    }
#else
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
            Logger.LogInfo("goome menu loaded, " + hotkey.Value + " opens it");
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
#endif
}
