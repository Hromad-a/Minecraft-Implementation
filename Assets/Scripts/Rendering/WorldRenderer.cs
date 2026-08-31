using System.Collections.Generic;
using UnityEngine;

// Streams chunk meshes around the viewer: chunks within the view radius get
// one GameObject, mesh and collider each, built nearest-first within a
// per-frame time budget; chunks left behind are destroyed.
[RequireComponent(typeof(World))]
public class WorldRenderer : MonoBehaviour
{
    [SerializeField, Tooltip("Chunks stream around this transform; defaults to the main camera")] Transform viewer;
    [SerializeField, Min(1), Tooltip("How many chunks around the viewer are loaded")] int viewRadius = 8;
    [SerializeField, Min(0.5f), Tooltip("Milliseconds per frame spent generating/meshing chunks")] float streamingBudgetMs = 4f;

    World world;
    Material[] materials;
    readonly Dictionary<Vector2Int, MeshFilter> activeChunks = new();
    readonly List<Vector2Int> buildQueue = new();
    readonly List<Vector2Int> unloadBuffer = new();
    Vector2Int viewerChunk;
    bool streamingDirty = true;

    void Awake()
    {
        world = GetComponent<World>();
        world.Regenerated += OnWorldRegenerated;
    }

    void OnDestroy()
    {
        world.Regenerated -= OnWorldRegenerated;
    }

    void Start()
    {
        if (viewer == null && Camera.main != null)
            viewer = Camera.main.transform;
        EnsureMaterials();
    }

    void Update()
    {
        if (world.WorldData == null || viewer == null)
            return;

        var current = world.WorldData.ChunkCoord(
            Mathf.FloorToInt(viewer.position.x), Mathf.FloorToInt(viewer.position.z));
        if (current != viewerChunk || streamingDirty)
        {
            viewerChunk = current;
            streamingDirty = false;
            RefreshStreaming();
        }
        BuildQueuedChunks();
    }

    // Rebuilds one loaded chunk after a block edit (no-op for unloaded chunks).
    public void RebuildChunk(Vector2Int chunkCoord)
    {
        if (!activeChunks.TryGetValue(chunkCoord, out var filter))
            return;
        ChunkMeshBuilder.Build(world.WorldData, world.WorldSettings, chunkCoord, filter.sharedMesh);
        RefreshCollider(filter);
    }

    void OnWorldRegenerated()
    {
        foreach (var filter in activeChunks.Values)
        {
            Destroy(filter.sharedMesh);
            Destroy(filter.gameObject);
        }
        activeChunks.Clear();
        buildQueue.Clear();
        streamingDirty = true;
    }

    // Recomputed whenever the viewer crosses a chunk border: drop chunks that
    // fell out of range (radius + 1 = hysteresis against border thrashing),
    // queue missing ones nearest-first.
    void RefreshStreaming()
    {
        int radius = viewRadius;

        unloadBuffer.Clear();
        foreach (var entry in activeChunks)
        {
            var offset = entry.Key - viewerChunk;
            if (Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y)) > radius + 1)
                unloadBuffer.Add(entry.Key);
        }
        foreach (var coord in unloadBuffer)
        {
            Destroy(activeChunks[coord].sharedMesh);
            Destroy(activeChunks[coord].gameObject);
            activeChunks.Remove(coord);
        }

        buildQueue.Clear();
        for (int dx = -radius; dx <= radius; dx++)
            for (int dz = -radius; dz <= radius; dz++)
            {
                var coord = new Vector2Int(viewerChunk.x + dx, viewerChunk.y + dz);
                if (!activeChunks.ContainsKey(coord))
                    buildQueue.Add(coord);
            }
        buildQueue.Sort((a, b) =>
            (a - viewerChunk).sqrMagnitude.CompareTo((b - viewerChunk).sqrMagnitude));
    }

    // Works through the queue in small units — one chunk's data generation or
    // one mesh build per step — until the frame's time budget is spent.
    void BuildQueuedChunks()
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (buildQueue.Count > 0 && timer.Elapsed.TotalMilliseconds < streamingBudgetMs)
        {
            var coord = buildQueue[0];
            if (activeChunks.ContainsKey(coord))
            {
                buildQueue.RemoveAt(0);
                continue;
            }

            if (GenerateOneMissingDataChunk(coord))
                continue;

            CreateChunk(coord);
            buildQueue.RemoveAt(0);
        }
    }

    // Meshing a chunk reads its own data plus all four neighbors (border face
    // checks). Generating one missing piece per step keeps each unit small.
    bool GenerateOneMissingDataChunk(Vector2Int coord)
    {
        Vector2Int[] needed =
        {
            coord, new(coord.x + 1, coord.y), new(coord.x - 1, coord.y),
            new(coord.x, coord.y + 1), new(coord.x, coord.y - 1),
        };
        foreach (var c in needed)
        {
            if (!world.WorldData.HasChunk(c))
            {
                world.WorldData.GetChunkCells(c);
                return true;
            }
        }
        return false;
    }

    void CreateChunk(Vector2Int coord)
    {
        int chunkSize = world.WorldData.chunkSize;
        var chunkObject = new GameObject($"Chunk {coord}");
        chunkObject.transform.SetParent(transform, false);
        chunkObject.transform.localPosition = new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);

        var filter = chunkObject.AddComponent<MeshFilter>();
        filter.sharedMesh = new Mesh { name = chunkObject.name };
        chunkObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
        chunkObject.AddComponent<MeshCollider>();

        ChunkMeshBuilder.Build(world.WorldData, world.WorldSettings, coord, filter.sharedMesh);
        RefreshCollider(filter);
        activeChunks.Add(coord, filter);
    }

    // Reassigning the mesh forces PhysX to re-cook the collider.
    static void RefreshCollider(MeshFilter filter)
    {
        var collider = filter.GetComponent<MeshCollider>();
        collider.sharedMesh = null;
        collider.sharedMesh = filter.sharedMesh;
    }

    // One material per block type, shared by every chunk (submeshes are
    // index-aligned with the settings' block list).
    void EnsureMaterials()
    {
        var blocks = world.WorldSettings.Blocks;
        materials = new Material[blocks.Count];
        for (int i = 0; i < blocks.Count; i++)
            materials[i] = blocks[i].Material;
    }
}
