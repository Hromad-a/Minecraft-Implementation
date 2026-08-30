using NaughtyAttributes;
using UnityEngine;

public abstract class BlockDefinitionBase : ScriptableObject
{
    [SerializeField, Min(0)] private int id;
    [SerializeField] private Material material;
    [SerializeField] private float mineDuration = 1f;
    [SerializeField, MinMaxSlider(0f, 1f)] private Vector2 heightRange;

    public int Id => id;
    public Material Material => material;
    public float MineDuration => mineDuration;
    public Vector2 HeightRange => heightRange;
    public bool IsUnbreakable => mineDuration < 0f;
    public bool ContainsHeight(int height, int sizeY)
    {
        int minHeight = Mathf.RoundToInt(sizeY + heightRange.x);
        int maxHeight = Mathf.RoundToInt(sizeY + heightRange.y);
        return height > minHeight && height < maxHeight;
    }

    public bool ContainsHeight(int height, int sizeY, out float influence)
    {
        influence = 0f;
        int minHeight = Mathf.RoundToInt(sizeY * heightRange.x);
        int maxHeight = Mathf.RoundToInt(sizeY * heightRange.y);
        if(height < minHeight || height > maxHeight) return false;
        int midPoint = ((maxHeight - minHeight)/2) + minHeight;
        if (height >= midPoint)
            influence = Mathf.Clamp01(Mathf.InverseLerp(maxHeight, midPoint, height));
        else
            influence = Mathf.Clamp01(Mathf.InverseLerp(minHeight, midPoint, height));
        return true;
    }

}
