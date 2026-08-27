
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

    public static WorldData GenerateWorld(int chunkSize, int xSize, int ySize, int zSize, string seed)
    {
        var world = new WorldData(seed, ySize);
        //Vygenerovat chunky v rozsahu/chunk size
        var chunkCountX = xSize / chunkSize;
        var chunkCountZ = zSize / chunkSize;
        var chunkCountY = ySize / chunkSize;
        

        return null;
    }


    public static BlockData[] GenerateChunk(int xSize, int ySize, int zSize)
    {
        var blockData = new BlockData[xSize * ySize * zSize];

        return null;
    }

}
