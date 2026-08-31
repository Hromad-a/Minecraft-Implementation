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

    public void RegenerateWorld()
    {
        worldData = WorldGenerator.GenerateWorld(worldSettings, worldSettings.XSize, worldSettings.YSize, worldSettings.ZSize, worldSettings.Seed);
        worldRenderer?.RenderWorld(worldData, worldSettings);
        Regenerated?.Invoke();
    }

    // Y of the first air cell above the highest solid block in the column.
    public int GetSurfaceHeight(int x, int z)
    {
        for (int y = worldSettings.YSize - 1; y >= 0; y--)
            if (TryGetBlock(new Vector3Int(x, y, z), out var block) && block.IsPresent)
                return y + 1;
        return 0;
    }

    // Reads the block at a world position; false when outside the world.
    public bool TryGetBlock(Vector3Int worldPos, out BlockData block)
    {
        block = default;
        int chunkSize = worldSettings.ChunkSize;
        var chunkCoord = FloorDiv(worldPos, chunkSize);
        if (!worldData.chunks.TryGetValue(chunkCoord, out var blocks))
            return false;
        var local = worldPos - chunkCoord * chunkSize;
        block = blocks[WorldGenerator.BlockIndex(local.x, local.y, local.z, chunkSize)];
        return true;
    }

    // Writes a block (0 = air) and re-renders the affected chunk,
    // plus neighbor chunks when the block sits on a chunk border.
    public void SetBlock(Vector3Int worldPos, int typeId)
    {
        int chunkSize = worldSettings.ChunkSize;
        var chunkCoord = FloorDiv(worldPos, chunkSize);
        if (!worldData.chunks.TryGetValue(chunkCoord, out var blocks))
            return; // outside the world
        var local = worldPos - chunkCoord * chunkSize;
        blocks[WorldGenerator.BlockIndex(local.x, local.y, local.z, chunkSize)].TypeId = typeId;

        RenderChunkIfExists(chunkCoord);
        if (local.x == 0) RenderChunkIfExists(chunkCoord + Vector3Int.left);
        if (local.x == chunkSize - 1) RenderChunkIfExists(chunkCoord + Vector3Int.right);
        if (local.y == 0) RenderChunkIfExists(chunkCoord + Vector3Int.down);
        if (local.y == chunkSize - 1) RenderChunkIfExists(chunkCoord + Vector3Int.up);
        if (local.z == 0) RenderChunkIfExists(chunkCoord + Vector3Int.back);
        if (local.z == chunkSize - 1) RenderChunkIfExists(chunkCoord + Vector3Int.forward);
    }

    void RenderChunkIfExists(Vector3Int chunkCoord)
    {
        if (worldData.chunks.ContainsKey(chunkCoord))
            worldRenderer?.RenderChunk(worldData, worldSettings, chunkCoord);
    }

    static Vector3Int FloorDiv(Vector3Int v, int size) => new(
        Mathf.FloorToInt((float)v.x / size),
        Mathf.FloorToInt((float)v.y / size),
        Mathf.FloorToInt((float)v.z / size));
}
