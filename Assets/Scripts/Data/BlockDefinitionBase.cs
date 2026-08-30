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
}
