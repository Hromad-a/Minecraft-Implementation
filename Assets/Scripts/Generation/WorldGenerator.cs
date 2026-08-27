
using UnityEngine;

public static class WorldGenerator
{

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
                    float noiseScale = 40f;
                    float offsetedWorldX = localX + chunkCoordinate.x * size + posOffset;
                    float offsetedWorldZ = localZ + chunkCoordinate.z * size + posOffset;
                    var perlinValue = Perlin.Fbm(offsetedWorldX / noiseScale, offsetedWorldZ / noiseScale, settings.Octave);
                    float height = (perlinValue + 1f) * 0.5f * (settings.YSize * .9f);
                    blockData[BlockIndex(localX, localY, localZ, size)].IsPresent = worldY < height;
                }

            }

        }
        

        return blockData;
    }

    public static int BlockIndex(int localX, int localY, int localZ, int chunkSize) => localX + localZ * chunkSize + localY * chunkSize * chunkSize;

    public static int PickBlockTypeId(int worldX, int worldY, int worldZ, string seed)
    {
        
        return 0;
    }


}