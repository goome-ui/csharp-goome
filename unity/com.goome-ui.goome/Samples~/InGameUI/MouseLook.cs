using GoomeUI;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public GoomeView view;
    public float speed = 2;
    public bool holdRightButton = true;

    float yaw, pitch;

    void Update()
    {
        if (view != null && view.WantsMouse) return;
        if (holdRightButton && !Input.GetMouseButton(1)) return;
        yaw += Input.GetAxis("Mouse X") * speed;
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * speed, -85, 85);
        transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
    }
}
