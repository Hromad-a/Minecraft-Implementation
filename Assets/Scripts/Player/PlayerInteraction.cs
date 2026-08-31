using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private World world;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float reach = 5f;

    CharacterController controller;
    int selectedBlockIndex;
    Vector3Int miningTarget;
    float miningProgress; // seconds spent holding on the current target
    float miningDuration; // total seconds needed for the current target

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (Mouse.current == null)
            return;
        SelectBlockType();

        if (Mouse.current.leftButton.isPressed)
            Mine();
        else
            miningProgress = 0f;

        if (Mouse.current.rightButton.wasPressedThisFrame)
            Place();
    }

    // Scroll wheel cycles through the block types in WorldSettings.
    void SelectBlockType()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll == 0f)
            return;
        int count = world.WorldSettings.Blocks.Count;
        selectedBlockIndex = (selectedBlockIndex + (scroll > 0f ? 1 : -1) + count) % count;
    }

    // Raycast from the camera; the mined cell is just inside the hit face,
    // the place cell just outside it.
    bool TryGetTargets(out Vector3Int mineCell, out Vector3Int placeCell)
    {
        mineCell = placeCell = default;
        var ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (!Physics.Raycast(ray, out var hit, reach))
            return false;
        mineCell = Vector3Int.FloorToInt(hit.point - hit.normal * 0.5f);
        placeCell = Vector3Int.FloorToInt(hit.point + hit.normal * 0.5f);
        return true;
    }

    // Hold to mine: progress accumulates while aiming at the same block,
    // resets when the target changes or the button is released.
    void Mine()
    {
        if (!TryGetTargets(out var cell, out _))
        {
            miningProgress = 0f;
            return;
        }
        if (cell != miningTarget)
        {
            miningTarget = cell;
            miningProgress = 0f;
        }

        if (!world.TryGetBlock(cell, out var block) || !block.IsPresent)
            return;
        if (!world.WorldSettings.TryGetBlockById(block.TypeId, out var definition) || definition.IsUnbreakable)
            return;

        miningDuration = definition.MineDuration;
        miningProgress += Time.deltaTime;
        if (miningProgress < definition.MineDuration)
            return;
        world.SetBlock(cell, 0);
        miningProgress = 0f;
    }

    // Place the selected type into the empty cell on the hit face,
    // unless it is occupied or overlaps the player.
    void Place()
    {
        if (!TryGetTargets(out _, out var cell))
            return;
        if (world.TryGetBlock(cell, out var block) && block.IsPresent)
            return;
        if (OverlapsPlayer(cell))
            return;
        world.SetBlock(cell, world.WorldSettings.Blocks[selectedBlockIndex].Id);
    }

    bool OverlapsPlayer(Vector3Int cell)
    {
        var cellBounds = new Bounds((Vector3)cell + Vector3.one * 0.5f, Vector3.one);
        return controller != null && cellBounds.Intersects(controller.bounds);
    }

    // Minimal HUD: crosshair, selected block, mining progress.
    void OnGUI()
    {
        GUI.Label(new Rect(Screen.width / 2f - 4, Screen.height / 2f - 12, 20, 20), "+");
        GUI.Label(new Rect(10, 10, 300, 24), "Block: " + world.WorldSettings.Blocks[selectedBlockIndex].name);

        if (miningProgress > 0f && miningDuration > 0f)
        {
            float width = 120f;
            float left = Screen.width / 2f - width / 2f;
            float y = Screen.height / 2f + 24;

            // end ticks + progress line, drawn as plain white rects
            GUI.DrawTexture(new Rect(left, y - 4, 2, 8), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(left + width, y - 4, 2, 8), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(left, y - 1, width * Mathf.Clamp01(miningProgress / miningDuration), 2), Texture2D.whiteTexture);
        }
    }
}
