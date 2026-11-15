using GoomeSharp;
using GoomeUI;
using UnityEngine;

public class DesktopOverlay : MonoBehaviour
{
    public GoomeOverlay overlay;

    double x = 60, y = 60, w = 280, h = 0;
    bool open = true;
    int score;

    void OnEnable()
    {
        if (overlay == null) overlay = GetComponent<GoomeOverlay>();
        overlay.Draw += DrawUi;
        overlay.Closed += Application.Quit;
    }

    void OnDisable()
    {
        if (overlay == null) return;
        overlay.Draw -= DrawUi;
        overlay.Closed -= Application.Quit;
    }

    void Update()
    {
        if (Time.frameCount % 60 == 0) score++;
    }

    void DrawUi(Ui ui)
    {
        if (!open) return;
        if (ui.Window("From Unity", ref x, ref y, ref w, ref h, WindowFlags.Move | WindowFlags.Close, ref open))
        {
            ui.Label("this floats over the whole desktop");
            ui.Label($"score {score}");
            ui.Progress(score % 100 / 100.0);
            if (ui.Button("Quit")) Application.Quit();
        }
        ui.WindowEnd();
        if (!open) Application.Quit();
    }
}
