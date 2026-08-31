using System.Collections.Generic;
using UnityEngine;

// World state as a grid of lazily generated full-height column chunks,
// unbounded horizontally (only y is bounded). Reading any cell generates its
// chunk on demand; generation is deterministic per position, so a chunk's
// content never depends on when it was first touched.
public class WorldData
{
    public readonly string seed;
    public readonly int chunkSize;
    public readonly int sizeY;

    readonly WorldSettings settings;
    readonly HeightBand[] heightBands;
    readonly LayerOffsets[] layerOffsets;
    readonly Vector2 typeJitterOffset;
    readonly Dictionary<Vector2Int, BlockData[]> chunks = new();

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

    public bool HasChunk(Vector2Int chunkCoord) => chunks.ContainsKey(chunkCoord);

    // The chunk's raw cell array, generated on demand.
    public BlockData[] GetChunkCells(Vector2Int chunkCoord)
    {
        if (!chunks.TryGetValue(chunkCoord, out var cells))
        {
            cells = WorldGenerator.GenerateChunk(settings, heightBands, layerOffsets, typeJitterOffset, chunkCoord.x, chunkCoord.y);
            chunks.Add(chunkCoord, cells);
        }
        return cells;
    }

    public bool TryGetBlock(Vector3Int worldPos, out BlockData block)
    {
        block = default;
        if (worldPos.y < 0 || worldPos.y >= sizeY)
            return false;
        block = GetChunkCells(ChunkCoord(worldPos.x, worldPos.z))[CellIndex(worldPos)];
        return true;
    }

    public void SetBlock(Vector3Int worldPos, int typeId)
    {
        if (worldPos.y < 0 || worldPos.y >= sizeY)
            return;
        GetChunkCells(ChunkCoord(worldPos.x, worldPos.z))[CellIndex(worldPos)].TypeId = typeId;
    }

    public Vector2Int ChunkCoord(int x, int z) => new(FloorDiv(x, chunkSize), FloorDiv(z, chunkSize));

    // Floor division, so negative coordinates map to chunks correctly.
    public static int FloorDiv(int value, int size) =>
        value >= 0 ? value / size : (value + 1) / size - 1;

    int CellIndex(Vector3Int worldPos)
    {
        int localX = worldPos.x - FloorDiv(worldPos.x, chunkSize) * chunkSize;
        int localZ = worldPos.z - FloorDiv(worldPos.z, chunkSize) * chunkSize;
        return WorldGenerator.BlockIndex(localX, worldPos.y, localZ, chunkSize);
    }
}
