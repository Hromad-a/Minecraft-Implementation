using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private Transform cameraTransform;

    CharacterController controller;
    float verticalVelocity;
    float cameraPitch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null)
            return;
        Look();
        Move();
    }

    // Mouse look: yaw rotates the body, pitch tilts only the camera.
    void Look()
    {
        var delta = Mouse.current.delta.ReadValue() * lookSensitivity;
        transform.Rotate(0f, delta.x, 0f);
        cameraPitch = Mathf.Clamp(cameraPitch - delta.y, -89f, 89f);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    // WASD relative to facing, plus gravity and jump.
    void Move()
    {
        var keyboard = Keyboard.current;

        // -- horizontal input --
        var input = Vector2.zero;
        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        // -- vertical: stick to ground, jump, fall --
        if (controller.isGrounded)
        {
            verticalVelocity = -1f;
            if (keyboard.spaceKey.isPressed)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        verticalVelocity += gravity * Time.deltaTime;

        var motion = (transform.right * input.x + transform.forward * input.y) * moveSpeed;
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }
}
