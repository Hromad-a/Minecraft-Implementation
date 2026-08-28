using System;

[Serializable]
public struct BlockData
{
    public bool IsPresent;
    public int TypeId;

    public BlockData(bool isPresent, int typeId)
    {
        IsPresent = isPresent;
        this.TypeId = typeId;
    }
}
