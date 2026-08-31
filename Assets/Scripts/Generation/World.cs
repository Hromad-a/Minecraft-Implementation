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
        var chunkCoord = worldData.ChunkCoord(worldPos.x, worldPos.z);
        int localX = worldPos.x - chunkCoord.x * chunkSize;
        int localZ = worldPos.z - chunkCoord.y * chunkSize;

        worldRenderer?.RebuildChunk(chunkCoord);
        if (localX == 0) worldRenderer?.RebuildChunk(chunkCoord + Vector2Int.left);
        if (localX == chunkSize - 1) worldRenderer?.RebuildChunk(chunkCoord + Vector2Int.right);
        if (localZ == 0) worldRenderer?.RebuildChunk(chunkCoord + Vector2Int.down);
        if (localZ == chunkSize - 1) worldRenderer?.RebuildChunk(chunkCoord + Vector2Int.up);
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

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(World))]
public class WorldEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        UnityEditor.EditorGUILayout.Space();
        if (GUILayout.Button("Regenerate world") && Application.isPlaying)
            ((World)target).RegenerateWorld();
    }
}
#endif
