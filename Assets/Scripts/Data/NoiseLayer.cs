using UnityEngine;

[CreateAssetMenu(menuName = "Noise Layer", fileName = "NoiseLayer")]
public class NoiseLayer : ScriptableObject
{
    [SerializeField, Min(0.01f), Tooltip("Horizontal size of features in blocks")] private float noiseScale = 40f;
    [SerializeField, Min(1)] private int octave = 3;
    [SerializeField, Tooltip("Vertical strength: layer contributes ±amplitude blocks")] private float amplitude = 32f;
    [SerializeField] private int heightOffset = 0;
    [SerializeField] private NoiseMask mask;

    public float NoiseScale => noiseScale;
    public int Octave => octave;
    public float Amplitude => amplitude;
    public int HeightOffset => heightOffset;
    public NoiseMask Mask => mask;
}

[System.Serializable]
public class NoiseMask
{
    [SerializeField] private bool enabled = true;
    [SerializeField, Min(0.01f), Tooltip("Horizontal size of the mask patches in blocks")] private float noiseScale = 60f;
    [SerializeField, Range(-1f, 1f), Tooltip("Noise above this value is visible")] private float threshold = 0f;
    [SerializeField, Range(0f, 1f), Tooltip("Edge softness; 0 = hard 1-bit edges")] private float feather = 0.1f;

    public bool Enabled => enabled;
    public float NoiseScale => noiseScale;
    public float Threshold => threshold;
    public float Feather => feather;
}
