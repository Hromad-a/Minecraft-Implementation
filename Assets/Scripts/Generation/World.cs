using UnityEngine;

public class World : MonoBehaviour
{
    [SerializeField] WorldSettings worldSettings;
    WorldData worldData;
    WorldRenderer worldRenderer;
    public WorldData WorldData {get {return worldData;}}
    public WorldSettings WorldSettings { get{return worldSettings;}}
    public event System.Action Regenerated;

    void Awake()
    {
        worldRenderer = GetComponent<WorldRenderer>();
        RegenerateWorld();
    }

    // Creates a fresh lazy world; chunks generate on demand as they are needed.
    [ContextMenu("Regenerate")]
    public void RegenerateWorld()
    {
        if (worldSettings.NoiseLayers == null || worldSettings.NoiseLayers.Count == 0)
        {
            Debug.LogWarning("World generation skipped: WorldSettings has no noise layers.");
            worldData = null;
        }
        else
        {
            worldData = new WorldData(worldSettings, worldSettings.Seed);
        }
        Regenerated?.Invoke();
    }

    // Reads the block at a world position; false above/below the world.
    public bool TryGetBlock(Vector3Int worldPos, out BlockData block)
    {
        block = default;
        return worldData != null && worldData.TryGetBlock(worldPos, out block);
    }

    // Writes a block (0 = air) and rebuilds the affected chunk's mesh,
    // plus neighbor chunks when the block sits on a chunk border.
    public void SetBlock(Vector3Int worldPos, int typeId)
    {
        if (worldData == null || worldPos.y < 0 || worldPos.y >= worldData.sizeY)
            return;
        worldData.SetBlock(worldPos, typeId);

        int chunkSize = worldData.chunkSize;
        var chunkCoord = worldData.ChunkCoord(worldPos);
        var local = worldPos - chunkCoord * chunkSize;

        worldRenderer?.RebuildChunk(chunkCoord);
        if (local.x == 0) worldRenderer?.RebuildChunk(chunkCoord + Vector3Int.left);
        if (local.x == chunkSize - 1) worldRenderer?.RebuildChunk(chunkCoord + Vector3Int.right);
        if (local.y == 0) worldRenderer?.RebuildChunk(chunkCoord + Vector3Int.down);
        if (local.y == chunkSize - 1) worldRenderer?.RebuildChunk(chunkCoord + Vector3Int.up);
        if (local.z == 0) worldRenderer?.RebuildChunk(chunkCoord + Vector3Int.back);
        if (local.z == chunkSize - 1) worldRenderer?.RebuildChunk(chunkCoord + Vector3Int.forward);
    }

    // Y of the first air cell above the highest solid block in the column.
    public int GetSurfaceHeight(int x, int z)
    {
        for (int y = worldSettings.YSize - 1; y >= 0; y--)
            if (TryGetBlock(new Vector3Int(x, y, z), out var block) && block.IsPresent)
                return y + 1;
        return 0;
    }
}
