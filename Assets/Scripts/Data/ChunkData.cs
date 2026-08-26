using System;

[Serializable]
public class ChunkData
{
    public int size;
    public BlockData[] cells;
    public ChunkData(int size)
    {
        this.size = size;
        cells = new BlockData[size * size * size];
    }
    public BlockData this[int x, int y, int z]
    {
        get => cells[Index(x, y, z)];
        set => cells[Index(x, y, z)] = value;
    }
    private int Index(int x, int y, int z) => x + z * size + y * size * size;
}
