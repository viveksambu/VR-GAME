
using UnityEngine;
using UnityEngine.InputSystem;

public class PanoramaMouseLook : MonoBehaviour
{
    public float sensitivity = 2f;

    private float pitch;
    private float yaw;

    void Update()
    {
        // Desktop-only preview control. Quest uses the headset pose supplied by
        // the XR Origin, so it never needs Unity's legacy Input API.
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.isPressed)
            return;

        Vector2 mouseDelta = mouse.delta.ReadValue();
        yaw += mouseDelta.x * sensitivity * 0.02f;
        pitch -= mouseDelta.y * sensitivity * 0.02f;
        pitch = Mathf.Clamp(pitch, -85f, 85f);

        transform.localRotation =
            Quaternion.Euler(pitch, yaw, 0f);
    }
}
