using System;

[Serializable]
public struct BlockData
{
    public int TypeId; // 0 = air
    public bool IsPresent => TypeId != 0;
}
