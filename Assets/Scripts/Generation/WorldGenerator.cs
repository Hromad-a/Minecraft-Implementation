using System.Collections.Generic;
using UnityEngine;

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
        var posOffset = GetSubSeed(settings.Seed, "posOffset");
        for(int chunkCountX = xSize / settings.ChunkSize; chunkCountX > 0; chunkCountX--)
        {
            for(int chunkCountZ = zSize / settings.ChunkSize; chunkCountZ > 0; chunkCountZ--)
            {
                for(int chunkCountY = ySize / settings.ChunkSize; chunkCountY > 0; chunkCountY--)
                {
                    Vector3Int chunkCoordinate = new Vector3Int(chunkCountX, chunkCountY, chunkCountZ);
                    var newChunk = GenerateChunk(settings, chunkCoordinate, posOffset);
                    world.chunks.Add(chunkCoordinate, newChunk);
                }
            }
        }
        return world;
    }


    public static BlockData[] GenerateChunk(WorldSettings settings, Vector3Int chunkCoordinate, float posOffset)
    {
        var size = settings.ChunkSize;
        var blockData = new BlockData[size * size * size];
        for(int localX = 0; localX < size; localX++)
        {
            for(int localY = 0; localY < size; localY++)
            {
                for(int localZ = 0; localZ < size; localZ++)
                {
                    int worldY = localY + chunkCoordinate.y * size;
                    int worldX = localX + chunkCoordinate.x * size;
                    int worldZ = localZ + chunkCoordinate.z * size;
                    float offsetedWorldX = localX + chunkCoordinate.x * size + posOffset;
                    float offsetedWorldZ = localZ + chunkCoordinate.z * size + posOffset;
                    var perlinValue = Perlin.Fbm(worldX / noiseScale, worldZ / noiseScale, settings.Octave);
                    float height = (perlinValue + 1f) * 0.5f * (settings.YSize * .9f);
                    int i = BlockIndex(localX, localY, localZ, size);
                    blockData[i].IsPresent = worldY < height;
                    blockData[i].TypeId = PickBlockTypeId(worldX, worldY, worldZ, settings);
                }

            }

        }
        

        return blockData;
    }

    public static int BlockIndex(int localX, int localY, int localZ, int chunkSize) => localX + localZ * chunkSize + localY * chunkSize * chunkSize;

    public static int PickBlockTypeId(int worldX, int worldY, int worldZ, WorldSettings settings)
    {
        int lowestCubeHeight = settings.YSize+1;
        int lowestCubeId = 0;
        int highestCubeHeight = -1;
        int highestCubeId = 0;
        KeyValuePair<int, float> mostSuitableBlockType = new(0,-1f);
        foreach (var b in settings.Blocks)
        {
            int minH = Mathf.RoundToInt(settings.YSize * b.HeightRange.x);
            int maxH = Mathf.RoundToInt(settings.YSize * b.HeightRange.y);
            if (minH < lowestCubeHeight)
            {
                lowestCubeHeight = minH;
                lowestCubeId = b.Id;
            }
            if (maxH > highestCubeHeight)
            {
                highestCubeHeight = maxH;
                highestCubeId = b.Id;
            }
            if (b.ContainsHeight(worldY, settings.YSize, out var influence) && mostSuitableBlockType.Value < influence)
                mostSuitableBlockType = new(b.Id, influence);
        }
        if (worldY > highestCubeHeight) return highestCubeId;
        if (worldY < lowestCubeHeight) return lowestCubeId;

        return mostSuitableBlockType.Key;
    }


}