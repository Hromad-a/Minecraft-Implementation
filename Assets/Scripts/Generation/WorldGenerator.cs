using System.Collections.Generic;
using UnityEngine;

public struct HeightBand
{
    public int Id;
    public int MinHeight;
    public int MaxHeight;
}

public struct LayerOffsets
{
    public Vector2 Noise;
    public Vector2 Mask;
}

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
        seed = ResolveSeed(seed);
        var world = new WorldData(seed, ySize);
        if (settings.NoiseLayers == null || settings.NoiseLayers.Count == 0)
        {
            Debug.LogWarning("World generation skipped: WorldSettings has no noise layers.");
            return world;
        }
        var heightBands = BuildHeightBands(settings);
        var layerOffsets = BuildLayerOffsets(settings, seed);
        for(int chunkCountX = xSize / settings.ChunkSize; chunkCountX > 0; chunkCountX--)
        {
            for(int chunkCountZ = zSize / settings.ChunkSize; chunkCountZ > 0; chunkCountZ--)
            {
                for(int chunkCountY = ySize / settings.ChunkSize; chunkCountY > 0; chunkCountY--)
                {
                    Vector3Int chunkCoordinate = new Vector3Int(chunkCountX, chunkCountY, chunkCountZ);
                    var newChunk = GenerateChunk(settings, chunkCoordinate, heightBands, layerOffsets);
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

    public static LayerOffsets[] BuildLayerOffsets(WorldSettings settings, string seed)
    {
        var offsets = new LayerOffsets[settings.NoiseLayers.Count];
        for (int i = 0; i < offsets.Length; i++)
        {
            offsets[i] = new LayerOffsets
            {
                Noise = new Vector2(GetSubSeed(seed, i + "x"), GetSubSeed(seed, i + "z")),
                Mask = new Vector2(GetSubSeed(seed, i + "maskx"), GetSubSeed(seed, i + "maskz")),
            };
        }
        return offsets;
    }

    public static BlockData[] GenerateChunk(WorldSettings settings, Vector3Int chunkCoordinate, HeightBand[] heightBands, LayerOffsets[] layerOffsets)
    {
        var size = settings.ChunkSize;
        var blockData = new BlockData[size * size * size];
        for(int localX = 0; localX < size; localX++)
        {
            for(int localZ = 0; localZ < size; localZ++)
            {
                int worldX = localX + chunkCoordinate.x * size;
                int worldZ = localZ + chunkCoordinate.z * size;
                int terrainHeight = GetTerrainHeight(settings, layerOffsets, worldX, worldZ);
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

    static int GetTerrainHeight(WorldSettings settings, LayerOffsets[] layerOffsets, int worldX, int worldZ)
    {
        float height = 0f;
        for (int i = 0; i < settings.NoiseLayers.Count; i++)
        {
            var layer = settings.NoiseLayers[i];
            var offsets = layerOffsets[i];
            float mask = EvaluateMask(layer.Mask, offsets.Mask, worldX, worldZ);
            if (mask <= 0f)
                continue;
            float x = (worldX + offsets.Noise.x) / layer.NoiseScale;
            float z = (worldZ + offsets.Noise.y) / layer.NoiseScale;
            float noise = Perlin.Fbm(x, z, layer.Octave); // -1..1
            height += (noise * layer.Amplitude + layer.HeightOffset) * mask;
        }
        return Mathf.Clamp(Mathf.CeilToInt(height), 0, settings.YSize);
    }

    // 0..1: like Photoshop levels applied to plain perlin noise
    public static float EvaluateMask(NoiseMask mask, Vector2 offset, int worldX, int worldZ)
    {
        if (!mask.Enabled)
            return 1f;
        float noise = Perlin.Fbm((worldX + offset.x) / mask.NoiseScale, (worldZ + offset.y) / mask.NoiseScale, mask.Octave);
        float value = mask.Feather <= 0f
            ? (noise >= mask.Threshold ? 1f : 0f)
            : Mathf.InverseLerp(mask.Threshold - mask.Feather, mask.Threshold + mask.Feather, noise);
        return mask.Invert ? 1f - value : value;
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