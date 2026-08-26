using UnityEngine;

public abstract class BlockDefinitionBase : ScriptableObject
{
    [SerializeField, Min(0)] private int id;
    [SerializeField] private Material material;
    [SerializeField] private float mineDuration = 1f;
    [SerializeField, Min(0)] private int minHeight;
    [SerializeField, Min(0)] private int maxHeight;

    public float MineDuration => mineDuration;
    public bool IsUnbreakable => mineDuration < 0f;

}
