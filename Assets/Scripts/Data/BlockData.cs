using System;

[Serializable]
public struct BlockData
{
    public bool IsPresent;
    public int typeId;

    public BlockData(bool isPresent, int typeId)
    {
        IsPresent = isPresent;
        this.typeId = typeId;
    }
}
