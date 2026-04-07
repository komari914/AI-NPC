using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleFPSController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3.0f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 0.1f;
    public Transform playerCamera;

    private float               xRotation   = 0f;
    private CharacterController _cc;
    private float               _verticalVelocity = 0f;
    private const float         Gravity = -9.81f;

    void Start()
    {
        _cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        if (Mouse.current == null) return;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        float x = 0f;
        float z = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.wKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
        }

        // Gravity
        if (_cc != null && _cc.isGrounded)
            _verticalVelocity = -2f; // small downward force to keep grounded
        else
            _verticalVelocity += Gravity * Time.deltaTime;

        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        Vector3 velocity = move * moveSpeed + Vector3.up * _verticalVelocity;

        if (_cc != null)
            _cc.Move(velocity * Time.deltaTime);
        else
            transform.position += move * moveSpeed * Time.deltaTime;
    }
}