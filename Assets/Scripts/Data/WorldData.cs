using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldData
{
    public Dictionary<Vector3Int, ChunkData> chunks;
    public string seed;
    public int sizeY;

    public WorldData(string seed, int sizeY)
    {
        this.seed = WorldGenerator.ResolveSeed(seed);
        this.sizeY = sizeY;
    }
}
