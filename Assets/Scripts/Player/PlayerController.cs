using UnityEngine;
using UnityEngine.InputSystem;

// Walking player with custom voxel collision — no Unity physics for the body.
// The body is an axis-aligned box (position = center of the feet) moved axis by
// axis against the world data: each axis' displacement is applied alone and
// clamped to the first solid cell it would enter, which gives wall sliding,
// ground detection and head bonks for free. A 0.6-wide box fits 1-block holes.
public class PlayerController : MonoBehaviour
{
    [SerializeField] private World world;
    [SerializeField] private Transform cameraTransform;

    [Header("Body")]
    [SerializeField, Min(0.1f)] private float width = 0.6f;
    [SerializeField, Min(0.1f)] private float height = 1.8f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField, Min(1f)] private float maxFallSpeed = 30f;
    [SerializeField] private float lookSensitivity = 0.1f;

    // Keeps clamped positions strictly outside walls despite float rounding.
    const float SkinEpsilon = 0.001f;

    Vector3 velocity;
    bool grounded;
    float cameraPitch;

    // The player's collision box in world space (used by PlayerInteraction).
    public Bounds WorldBounds =>
        new(transform.position + Vector3.up * (height * 0.5f), new Vector3(width, height, width));

    void Awake()
    {
        if (world == null)
            world = FindFirstObjectByType<World>();
        world.Regenerated += Respawn;
    }

    void OnDestroy()
    {
        world.Regenerated -= Respawn;
    }

    void Start()
    {
        Respawn();
    }

    // Drops the player at the middle of the map, 2 blocks above the ground.
    void Respawn()
    {
        int centerX = world.WorldSettings.XSize / 2;
        int centerZ = world.WorldSettings.ZSize / 2;
        float y = world.GetSurfaceHeight(centerX, centerZ) + 2f;
        transform.position = new Vector3(centerX + 0.5f, y, centerZ + 0.5f);
        velocity = Vector3.zero;
        grounded = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null)
            return;

        // -- cursor: click to lock, Esc to unlock; no input while unlocked --
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;
        if (keyboard.escapeKey.wasPressedThisFrame)
            Cursor.lockState = CursorLockMode.None;
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        Look(mouse);
        UpdateVelocity(keyboard);

        // -- resolve movement one axis at a time --
        MoveAxis(0, velocity.x * Time.deltaTime);
        grounded = false;
        MoveAxis(1, velocity.y * Time.deltaTime);
        MoveAxis(2, velocity.z * Time.deltaTime);

        // safety net: fell out of the world (e.g. off the edge)
        if (transform.position.y < -10f)
            Respawn();
    }

    // Mouse look: yaw rotates the body, pitch tilts only the camera.
    void Look(Mouse mouse)
    {
        var delta = mouse.delta.ReadValue() * lookSensitivity;
        transform.Rotate(0f, delta.x, 0f);
        cameraPitch = Mathf.Clamp(cameraPitch - delta.y, -89f, 89f);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    // WASD relative to facing, gravity, jump while grounded.
    void UpdateVelocity(Keyboard keyboard)
    {
        var input = Vector3.zero;
        if (keyboard.wKey.isPressed) input += transform.forward;
        if (keyboard.sKey.isPressed) input -= transform.forward;
        if (keyboard.dKey.isPressed) input += transform.right;
        if (keyboard.aKey.isPressed) input -= transform.right;
        input.y = 0f;
        input = input.normalized * moveSpeed;
        velocity.x = input.x;
        velocity.z = input.z;

        if (grounded && keyboard.spaceKey.isPressed)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y = Mathf.Max(velocity.y + gravity * Time.deltaTime, -maxFallSpeed);
    }

    // Applies one axis' displacement; on overlap, rests the box against the
    // cell boundary and zeroes that velocity axis.
    void MoveAxis(int axis, float delta)
    {
        if (delta == 0f)
            return;

        // one frame can never move more than a block per axis, so the clamp
        // below always catches the first wall even at terrible framerates
        delta = Mathf.Clamp(delta, -0.9f, 0.9f);

        var position = transform.position;
        position[axis] += delta;

        if (BoxOverlapsSolid(position))
        {
            if (delta > 0f)
            {
                // leading face entered the cell starting at floor(max); rest against it
                float boundary = Mathf.Floor(BoxMax(position)[axis]);
                position[axis] = boundary - (BoxMax(position)[axis] - position[axis]) - SkinEpsilon;
            }
            else
            {
                // leading face entered the cell ending at floor(min) + 1
                float boundary = Mathf.Floor(BoxMin(position)[axis]) + 1f;
                position[axis] = boundary - (BoxMin(position)[axis] - position[axis]) + SkinEpsilon;
                if (axis == 1)
                    grounded = true;
            }
            velocity[axis] = 0f;
        }

        transform.position = position;
    }

    Vector3 BoxMin(Vector3 position) =>
        position + new Vector3(-width * 0.5f, 0f, -width * 0.5f);

    Vector3 BoxMax(Vector3 position) =>
        position + new Vector3(width * 0.5f, height, width * 0.5f);

    // Does the body box at this position overlap any solid block?
    // Outside the world TryGetBlock fails, which counts as air.
    bool BoxOverlapsSolid(Vector3 position)
    {
        var min = Vector3Int.FloorToInt(BoxMin(position));
        var max = Vector3Int.FloorToInt(BoxMax(position) - Vector3.one * SkinEpsilon);
        for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
                for (int z = min.z; z <= max.z; z++)
                    if (world.TryGetBlock(new Vector3Int(x, y, z), out var block) && block.IsPresent)
                        return true;
        return false;
    }
}
