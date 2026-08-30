using System.Collections.Generic;
using UnityEngine;

public struct HeightBand
{
    public int Id;
    public int MinHeight;
    public int MaxHeight;
}

public static class WorldGenerator
{
    public const float noiseScale = 40f;
    public static string ResolveSeed(string configuredSeed) => string.IsNullOrEmpty(configuredSeed) ? System.Guid.NewGuid().ToString() : configuredSeed;
    public static float GetSubSeed(string seed, string noiseName = "")
    {
        int hash = 0;
        foreach (char c in seed + "#" + noiseName)
            hash = hash * 31 + c;

        return ((uint)hash % 256_000u) / 1000f;
    }

    public static WorldData GenerateWorld(WorldSettings settings, int xSize, int ySize, int zSize, string seed)
    {
        var world = new WorldData(seed, ySize);
        var heightBands = BuildHeightBands(settings);
        for(int chunkCountX = xSize / settings.ChunkSize; chunkCountX > 0; chunkCountX--)
        {
            for(int chunkCountZ = zSize / settings.ChunkSize; chunkCountZ > 0; chunkCountZ--)
            {
                for(int chunkCountY = ySize / settings.ChunkSize; chunkCountY > 0; chunkCountY--)
                {
                    Vector3Int chunkCoordinate = new Vector3Int(chunkCountX, chunkCountY, chunkCountZ);
                    var newChunk = GenerateChunk(settings, chunkCoordinate, heightBands);
                    world.chunks.Add(chunkCoordinate, newChunk);
                }
            }
        }
        return world;
    }

    public static HeightBand[] BuildHeightBands(WorldSettings settings)
    {
        var bands = new HeightBand[settings.Blocks.Count];
        for (int i = 0; i < bands.Length; i++)
        {
            var block = settings.Blocks[i];
            bands[i] = new HeightBand
            {
                Id = block.Id,
                MinHeight = Mathf.RoundToInt(settings.YSize * block.HeightRange.x),
                MaxHeight = Mathf.RoundToInt(settings.YSize * block.HeightRange.y),
            };
        }
        return bands;
    }

    public static BlockData[] GenerateChunk(WorldSettings settings, Vector3Int chunkCoordinate, HeightBand[] heightBands)
    {
        var size = settings.ChunkSize;
        var blockData = new BlockData[size * size * size];
        for(int localX = 0; localX < size; localX++)
        {
            for(int localZ = 0; localZ < size; localZ++)
            {
                int worldX = localX + chunkCoordinate.x * size;
                int worldZ = localZ + chunkCoordinate.z * size;
                int terrainHeight = GetTerrainHeight(settings, worldX, worldZ);
                for(int localY = 0; localY < size; localY++)
                {
                    int worldY = localY + chunkCoordinate.y * size;
                    int i = BlockIndex(localX, localY, localZ, size);
                    blockData[i].IsPresent = worldY < terrainHeight;
                    blockData[i].TypeId = PickBlockTypeId(worldX, worldY, worldZ, heightBands);
                }
            }
        }
        return blockData;
    }

    static int GetTerrainHeight(WorldSettings settings, int worldX, int worldZ)
    {
        float noise = Perlin.Fbm(worldX / noiseScale, worldZ / noiseScale, settings.Octave); // -1..1
        float normalizedNoise = Mathf.InverseLerp(-1f, 1f, noise);                           //  0..1
        int height = Mathf.CeilToInt(normalizedNoise * settings.YSize) + settings.BaseHeightOffset;
        return Mathf.Clamp(height, 0, settings.YSize);
    }

    public static int BlockIndex(int localX, int localY, int localZ, int chunkSize) => localX + localZ * chunkSize + localY * chunkSize * chunkSize;

    public static int PickBlockTypeId(int worldX, int worldY, int worldZ, HeightBand[] heightBands)
    {
        int bestId = 0;
        float bestInfluence = -1f;
        int nearestId = 0;
        int nearestDistance = int.MaxValue;
        foreach (var band in heightBands)
        {
            if (worldY < band.MinHeight || worldY > band.MaxHeight)
            {
                int distance = worldY < band.MinHeight ? band.MinHeight - worldY : worldY - band.MaxHeight;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestId = band.Id;
                }
                continue;
            }
            // influence: 1 at the band's middle, falling to 0 at its edges
            int midHeight = (band.MinHeight + band.MaxHeight) / 2;
            float influence = worldY >= midHeight
                ? Mathf.InverseLerp(band.MaxHeight, midHeight, worldY)
                : Mathf.InverseLerp(band.MinHeight, midHeight, worldY);
            if (bestInfluence < influence)
            {
                bestId = band.Id;
                bestInfluence = influence;
            }
        }
        return bestInfluence >= 0f ? bestId : nearestId;
    }


}