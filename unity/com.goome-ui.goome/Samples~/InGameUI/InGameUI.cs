using GoomeSharp;
using GoomeUI;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public GoomeView view;
    public Light sun;

    double x = 24, y = 24, w = 300, h = 0, speed = 1;
    bool spin = true, open = true;
    uint tint = 0xFF6FDC5F;
    string playerName = "player";
    int clicks;

    void OnEnable()
    {
        if (view == null) view = GetComponent<GoomeView>();
        view.Draw += DrawUi;
    }

    void OnDisable()
    {
        if (view != null) view.Draw -= DrawUi;
    }

    void Update()
    {
        if (spin) transform.Rotate(0, (float)speed * 60 * Time.deltaTime, 0);
        if (Input.GetMouseButtonDown(0) && !view.PointerOverUi) clicks++;
    }

    void DrawUi(Ui ui)
    {
        if (!open) return;
        if (ui.Window("Game", ref x, ref y, ref w, ref h, WindowFlags.Move | WindowFlags.Collapse | WindowFlags.Close, ref open))
        {
            ui.Label($"fps {1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f):0}");
            ui.Label($"clicks on the game: {clicks}");
            ui.Toggle("Spin", ref spin);
            ui.Slider("Speed", ref speed, 0, 5, "%.1f");
            ui.Input("Name", ref playerName, 32);
            if (ui.Color("Sun", ref tint) && sun != null)
                sun.color = new Color((tint >> 16 & 255) / 255f, (tint >> 8 & 255) / 255f, (tint & 255) / 255f);
            if (ui.Button("Say hi")) ui.Toast($"hi {playerName}");
        }
        ui.WindowEnd();
    }
}
