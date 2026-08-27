
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
        //Vygenerovat chunky v rozsahu/chunk size
        
        //Proloopovat vsechny chunky po x, y a z a v kazdem vygenerovat vsechny bloky

        for(int chunkCountX = xSize / settings.ChunkSize; chunkCountX > 0; chunkCountX--)
        {
            for(int chunkCountZ = zSize / settings.ChunkSize; chunkCountZ > 0; chunkCountZ--)
            {
                for(int chunkCountY = ySize / settings.ChunkSize; chunkCountY > 0; chunkCountY--)
                {
                    Vector3Int chunkCoordinate = new Vector3Int(chunkCountX, chunkCountY, chunkCountZ);
                    var newChunk = GenerateChunk(settings, chunkCoordinate);
                    world.chunks.Add(chunkCoordinate, newChunk);
                }
            }
        }
        return world;
    }


    public static BlockData[] GenerateChunk(WorldSettings settings, Vector3Int chunkCoordinate)
    {
        var size = settings.ChunkSize;
        var blockData = new BlockData[size * size * size];
        for(int localX = 0; localX < size; localX++)
        {
            for(int localY = 0; localY < size; localY++)
            {
                for(int localZ = 0; localZ < size; localZ++)
                {
                    float offsetedWorldX = localX * size + GetSubSeed(settings.Seed, "posOffset");
                    float offsetedWorldY = localY * size + GetSubSeed(settings.Seed, "yOffset");
                    float offsetedWorldZ = localZ * size + GetSubSeed(settings.Seed, "zOffset");
                    var perlinValue = Perlin.Fbm(offsetedWorldX, offsetedWorldY, offsetedWorldZ, settings.Octave);
                    blockData[BlockIndex(localX, localY, localZ, size)].IsPresent = perlinValue > 0f;
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