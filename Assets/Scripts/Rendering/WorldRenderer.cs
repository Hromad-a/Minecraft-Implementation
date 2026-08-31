using System.Collections.Generic;
using UnityEngine;

// Streams chunk meshes around the viewer: cubic chunks within the view radius
// are built nearest-first within a per-frame time budget; chunks left behind
// are destroyed. Chunks with no visible geometry (all air, or fully buried)
// are tracked but get no GameObject at all.
[RequireComponent(typeof(World))]
public class WorldRenderer : MonoBehaviour
{
    [SerializeField, Tooltip("Chunks stream around this transform; defaults to the main camera")] Transform viewer;
    [SerializeField, Min(1), Tooltip("How many chunks around the viewer are loaded")] int viewRadius = 8;
    [SerializeField, Min(0.5f), Tooltip("Milliseconds per frame spent generating/meshing chunks")] float streamingBudgetMs = 4f;

    World world;
    Material[] materials;
    // value is null for chunks that were built but produced no geometry
    readonly Dictionary<Vector3Int, MeshFilter> activeChunks = new();
    readonly List<Vector3Int> buildQueue = new();
    readonly List<Vector3Int> unloadBuffer = new();
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

        int chunkSize = world.WorldData.chunkSize;
        var current = new Vector2Int(
            WorldData.FloorDiv(Mathf.FloorToInt(viewer.position.x), chunkSize),
            WorldData.FloorDiv(Mathf.FloorToInt(viewer.position.z), chunkSize));
        if (current != viewerChunk || streamingDirty)
        {
            viewerChunk = current;
            streamingDirty = false;
            RefreshStreaming();
        }
        BuildQueuedChunks();
    }

    // Rebuilds one loaded chunk after a block edit (no-op for unloaded chunks).
    public void RebuildChunk(Vector3Int chunkCoord)
    {
        if (!activeChunks.TryGetValue(chunkCoord, out var filter))
            return;
        if (filter == null)
        {
            // was empty before the edit; may need an object now (e.g. block placed in the air)
            activeChunks.Remove(chunkCoord);
            BuildChunkAt(chunkCoord);
            return;
        }
        ChunkMeshBuilder.Build(world.WorldData, world.WorldSettings, chunkCoord, filter.sharedMesh);
        RefreshCollider(filter);
    }

    void OnWorldRegenerated()
    {
        foreach (var filter in activeChunks.Values)
        {
            if (filter == null)
                continue;
            Destroy(filter.sharedMesh);
            Destroy(filter.gameObject);
        }
        activeChunks.Clear();
        buildQueue.Clear();
        streamingDirty = true;
    }

    // Recomputed whenever the viewer crosses a chunk border: drop chunks that
    // fell out of range (radius + 1 = hysteresis against border thrashing),
    // queue missing ones nearest-first. Whole vertical stacks stream together.
    void RefreshStreaming()
    {
        int radius = viewRadius;
        int stackCount = world.WorldData.StackCount;

        unloadBuffer.Clear();
        foreach (var entry in activeChunks)
        {
            int distance = Mathf.Max(
                Mathf.Abs(entry.Key.x - viewerChunk.x), Mathf.Abs(entry.Key.z - viewerChunk.y));
            if (distance > radius + 1)
                unloadBuffer.Add(entry.Key);
        }
        foreach (var coord in unloadBuffer)
        {
            var filter = activeChunks[coord];
            if (filter != null)
            {
                Destroy(filter.sharedMesh);
                Destroy(filter.gameObject);
            }
            activeChunks.Remove(coord);
        }

        buildQueue.Clear();
        for (int dx = -radius; dx <= radius; dx++)
            for (int dz = -radius; dz <= radius; dz++)
                for (int cy = 0; cy < stackCount; cy++)
                {
                    var coord = new Vector3Int(viewerChunk.x + dx, cy, viewerChunk.y + dz);
                    if (!activeChunks.ContainsKey(coord))
                        buildQueue.Add(coord);
                }
        buildQueue.Sort((a, b) => HorizontalSqrDistance(a).CompareTo(HorizontalSqrDistance(b)));
    }

    int HorizontalSqrDistance(Vector3Int coord)
    {
        int dx = coord.x - viewerChunk.x;
        int dz = coord.z - viewerChunk.y;
        return dx * dx + dz * dz;
    }

    // Works through the queue one chunk per step until the frame's time budget
    // is spent. Meshing generates missing chunk data lazily; cubic chunks keep
    // each step small.
    void BuildQueuedChunks()
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (buildQueue.Count > 0 && timer.Elapsed.TotalMilliseconds < streamingBudgetMs)
        {
            var coord = buildQueue[0];
            buildQueue.RemoveAt(0);
            if (!activeChunks.ContainsKey(coord))
                BuildChunkAt(coord);
        }
    }

    // Builds one chunk's mesh; chunks without visible geometry are recorded
    // with a null filter and get no GameObject.
    void BuildChunkAt(Vector3Int coord)
    {
        // sky and deeply buried chunks are proven invisible from the cached
        // column heights alone — no chunk data is generated for them at all
        if (world.WorldData.IsChunkKnownInvisible(coord))
        {
            activeChunks.Add(coord, null);
            return;
        }

        var mesh = new Mesh { name = $"Chunk {coord}" };
        if (!ChunkMeshBuilder.Build(world.WorldData, world.WorldSettings, coord, mesh))
        {
            Destroy(mesh);
            activeChunks.Add(coord, null);
            return;
        }

        int chunkSize = world.WorldData.chunkSize;
        var chunkObject = new GameObject($"Chunk {coord}");
        chunkObject.transform.SetParent(transform, false);
        chunkObject.transform.localPosition = (Vector3)(coord * chunkSize);

        var filter = chunkObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        chunkObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
        chunkObject.AddComponent<MeshCollider>();
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
