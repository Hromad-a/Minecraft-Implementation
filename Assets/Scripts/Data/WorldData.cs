using System.Collections.Generic;
using UnityEngine;

// World state as a grid of lazily generated cubic chunks (chunkSize³), keyed by
// 3D chunk coordinate — unbounded horizontally, y bounded to [0, sizeY).
// Reading any cell generates its chunk on demand; generation is deterministic
// per position. Terrain heights are cached per chunk column so a stack of cubic
// chunks computes its heightmap only once.
public class WorldData
{
    public readonly string seed;
    public readonly int chunkSize;
    public readonly int sizeY;

    readonly WorldSettings settings;
    readonly HeightBand[] heightBands;
    readonly LayerOffsets[] layerOffsets;
    readonly Vector2 typeJitterOffset;
    readonly Dictionary<Vector3Int, BlockData[]> chunks = new();
    readonly Dictionary<Vector2Int, ChunkColumn> columns = new();
    readonly HashSet<Vector3Int> modifiedChunks = new();

    public WorldData(WorldSettings settings, string configuredSeed)
    {
        this.settings = settings;
        seed = WorldGenerator.ResolveSeed(configuredSeed);
        chunkSize = settings.ChunkSize;
        sizeY = settings.YSize;

        // generation context, computed once and reused by every chunk
        heightBands = WorldGenerator.BuildHeightBands(settings);
        layerOffsets = WorldGenerator.BuildLayerOffsets(settings, seed);
        typeJitterOffset = new Vector2(
            WorldGenerator.GetSubSeed(seed, "typeJitterx"),
            WorldGenerator.GetSubSeed(seed, "typeJitterz"));
    }

    // How many cubic chunks are stacked vertically.
    public int StackCount => (sizeY + chunkSize - 1) / chunkSize;

    public bool HasChunk(Vector3Int chunkCoord) => chunks.ContainsKey(chunkCoord);

    // The chunk's raw cell array, generated on demand.
    public BlockData[] GetChunkCells(Vector3Int chunkCoord)
    {
        if (!chunks.TryGetValue(chunkCoord, out var cells))
        {
            cells = WorldGenerator.GenerateChunk(settings, heightBands, GetColumn(chunkCoord.x, chunkCoord.z), chunkCoord.y);
            chunks.Add(chunkCoord, cells);
        }
        return cells;
    }

    ChunkColumn GetColumn(int chunkX, int chunkZ)
    {
        var coord = new Vector2Int(chunkX, chunkZ);
        if (!columns.TryGetValue(coord, out var column))
        {
            column = WorldGenerator.BuildColumn(settings, layerOffsets, typeJitterOffset, chunkX, chunkZ);
            columns.Add(coord, column);
        }
        return column;
    }

    public bool TryGetBlock(Vector3Int worldPos, out BlockData block)
    {
        block = default;
        if (worldPos.y < 0 || worldPos.y >= sizeY)
            return false;
        block = GetChunkCells(ChunkCoord(worldPos))[CellIndex(worldPos)];
        return true;
    }

    public void SetBlock(Vector3Int worldPos, int typeId)
    {
        if (worldPos.y < 0 || worldPos.y >= sizeY)
            return;
        var chunkCoord = ChunkCoord(worldPos);
        GetChunkCells(chunkCoord)[CellIndex(worldPos)].TypeId = typeId;
        modifiedChunks.Add(chunkCoord); // edits invalidate the heightmap-based visibility shortcut
    }

    // True when the chunk provably has no visible faces, decided from the cached
    // column heights alone — without generating any chunk data. Only valid for
    // chunks whose neighborhood is untouched by player edits; anything modified
    // falls back to a real mesh pass.
    public bool IsChunkKnownInvisible(Vector3Int chunkCoord)
    {
        if (modifiedChunks.Contains(chunkCoord)
            || modifiedChunks.Contains(chunkCoord + Vector3Int.up)
            || modifiedChunks.Contains(chunkCoord + Vector3Int.down))
            return false;

        int bottom = chunkCoord.y * chunkSize;
        int top = bottom + chunkSize;
        var column = GetColumn(chunkCoord.x, chunkCoord.z);

        // entirely above the terrain: all air, nothing to render
        if (bottom >= column.MaxHeight)
            return true;

        // contains surface, or its top face row is exposed: must be meshed
        if (column.MinHeight <= top)
            return false;

        // fully solid below the terrain; the world floor is still rendered
        if (chunkCoord.y == 0)
            return false;

        // buried: invisible unless a horizontal neighbor's boundary row dips
        // below our top (which would expose side faces) or was edited
        for (int face = 0; face < 4; face++)
        {
            var direction = horizontalDirections[face];
            if (modifiedChunks.Contains(chunkCoord + new Vector3Int(direction.x, 0, direction.y)))
                return false;
            var neighbor = GetColumn(chunkCoord.x + direction.x, chunkCoord.z + direction.y);
            if (!BoundaryRowCovers(neighbor.Heights, direction, top))
                return false;
        }
        return true;
    }

    static readonly Vector2Int[] horizontalDirections = { Vector2Int.left, Vector2Int.right, new(0, -1), new(0, 1) };

    // Does the neighbor column's row facing us (opposite side to `direction`)
    // stay solid up to `top`?
    bool BoundaryRowCovers(int[] neighborHeights, Vector2Int direction, int top)
    {
        for (int i = 0; i < chunkSize; i++)
        {
            int localX = direction.x == 1 ? 0 : direction.x == -1 ? chunkSize - 1 : i;
            int localZ = direction.y == 1 ? 0 : direction.y == -1 ? chunkSize - 1 : i;
            if (neighborHeights[localX + localZ * chunkSize] < top)
                return false;
        }
        return true;
    }

    public Vector3Int ChunkCoord(Vector3Int worldPos) => new(
        FloorDiv(worldPos.x, chunkSize),
        FloorDiv(worldPos.y, chunkSize),
        FloorDiv(worldPos.z, chunkSize));

    // Floor division, so negative coordinates map to chunks correctly.
    public static int FloorDiv(int value, int size) =>
        value >= 0 ? value / size : (value + 1) / size - 1;

    int CellIndex(Vector3Int worldPos)
    {
        int localX = worldPos.x - FloorDiv(worldPos.x, chunkSize) * chunkSize;
        int localY = worldPos.y - FloorDiv(worldPos.y, chunkSize) * chunkSize;
        int localZ = worldPos.z - FloorDiv(worldPos.z, chunkSize) * chunkSize;
        return WorldGenerator.BlockIndex(localX, localY, localZ, chunkSize);
    }
}
