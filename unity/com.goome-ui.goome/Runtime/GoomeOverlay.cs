using System;
using GoomeSharp;
using UnityEngine;
using Event = GoomeSharp.Event;
using EventType = GoomeSharp.EventType;

namespace GoomeUI
{
    [DisallowMultipleComponent]
    public class GoomeOverlay : MonoBehaviour
    {
        public string title = "goome";
        [Tooltip("Open a normal app window instead of the see-through overlay")]
        public bool appWindow;
        public int width = 960;
        public int height = 640;
        public int monitor;
        public int fps = 60;
        [Tooltip("The editor is a desktop app too, but an overlay on top of it gets in the way. Off = only in standalone builds")]
        public bool runInEditor;
        public bool lightTheme;

        public event Action<Ui> Draw;
        public event Action<Event> OnEvent;
        public event Action Closed;

        public Goome Goome { get; private set; }
        public Ui Ui { get; private set; }

        float last;
        bool dirty = true;

        void OnEnable()
        {
            if (Application.isEditor && !runInEditor) return;
            try
            {
                Goome = appWindow ? Goome.OpenApp(title, width, height) : new Goome(new GoomeOptions { Title = title, NoCapture = true });
                Ui = new Ui(Goome);
            }
            catch (Exception e)
            {
                Debug.LogWarning("goome: no overlay here, " + e.Message);
                Goome?.Dispose();
                Goome = null;
                return;
            }
            if (lightTheme) Ui.Theme.Light();
            dirty = true;
        }

        void OnDisable() => Shut();

        void OnApplicationQuit() => Shut();

        void Shut()
        {
            Ui?.Dispose();
            Goome?.Dispose();
            Ui = null;
            Goome = null;
        }

        public void Redraw() => dirty = true;

        void Update()
        {
            if (Goome == null) return;
            while (Goome.Poll(out var ev, 0))
            {
                OnEvent?.Invoke(ev);
                if (ev.Type == EventType.Quit || ev.Type == EventType.Close)
                {
                    Shut();
                    Closed?.Invoke();
                    return;
                }
                Ui.Feed(ev);
                dirty = true;
            }
            float now = Time.unscaledTime;
            if (!dirty && !Ui.Animating && now - last < 1f / Math.Max(1, fps)) return;
            last = now;
            dirty = false;
            if (Ui.Begin(monitor) == null) return;
            try
            {
                if (appWindow) Ui.App(title);
                Draw?.Invoke(Ui);
                if (appWindow) Ui.AppEnd();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            Ui.End();
        }
    }
}
