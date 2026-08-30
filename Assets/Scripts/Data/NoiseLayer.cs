using UnityEngine;

[CreateAssetMenu(menuName = "Noise Layer", fileName = "NoiseLayer")]
public class NoiseLayer : ScriptableObject
{
    [SerializeField, Min(0.01f), Tooltip("Horizontal size of features in blocks")] private float noiseScale = 40f;
    [SerializeField, Min(1)] private int octave = 3;
    [SerializeField, Tooltip("Vertical strength: layer contributes ±amplitude blocks")] private float amplitude = 32f;
    [SerializeField] private int heightOffset = 0;

    public float NoiseScale => noiseScale;
    public int Octave => octave;
    public float Amplitude => amplitude;
    public int HeightOffset => heightOffset;
}
