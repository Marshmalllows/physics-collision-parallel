using UnityEngine;
using UnityEngine.InputSystem;

public class FreeCameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float lookSensitivity = 0.1f;
    public float fastMultiplier = 3f;
    public float roomBound = 9.5f;

    float yaw;
    float pitch;

    void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse == null || keyboard == null)
            return;

        if (mouse.rightButton.isPressed)
        {
            var delta = mouse.delta.ReadValue();
            yaw += delta.x * lookSensitivity;
            pitch -= delta.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -90f, 90f);
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        var speed = moveSpeed * (keyboard.leftShiftKey.isPressed ? fastMultiplier : 1f) * Time.deltaTime;
        var move = Vector3.zero;

        if (keyboard.wKey.isPressed) move += transform.forward;
        if (keyboard.sKey.isPressed) move -= transform.forward;
        if (keyboard.aKey.isPressed) move -= transform.right;
        if (keyboard.dKey.isPressed) move += transform.right;
        if (keyboard.eKey.isPressed) move += Vector3.up;
        if (keyboard.qKey.isPressed) move -= Vector3.up;

        var pos = transform.position + move * speed;
        pos.x = Mathf.Clamp(pos.x, -roomBound, roomBound);
        pos.y = Mathf.Clamp(pos.y, -roomBound, roomBound);
        pos.z = Mathf.Clamp(pos.z, -roomBound, roomBound);
        transform.position = pos;
    }
}
