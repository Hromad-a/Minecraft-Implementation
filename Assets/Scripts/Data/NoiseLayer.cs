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
    [SerializeField, Min(1)] private int octave = 2;
    [SerializeField, Range(-1f, 1f), Tooltip("Noise above this value is visible")] private float threshold = 0f;
    [SerializeField, Range(0f, 1f), Tooltip("Edge softness; 0 = hard 1-bit edges")] private float feather = 0.1f;
    [SerializeField] private bool invert;

    public bool Enabled => enabled;
    public float NoiseScale => noiseScale;
    public int Octave => octave;
    public float Threshold => threshold;
    public float Feather => feather;
    public bool Invert => invert;
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(NoiseLayer))]
public class NoiseLayerEditor : UnityEditor.Editor
{
    const int previewSize = 128;
    Texture2D noiseTexture;
    Texture2D maskTexture;
    Texture2D combinedTexture;
    int lastPreviewHash;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var layer = (NoiseLayer)target;
        int previewHash = System.HashCode.Combine(
            layer.NoiseScale, layer.Octave,
            layer.Mask.Enabled, layer.Mask.NoiseScale, layer.Mask.Octave,
            layer.Mask.Threshold, layer.Mask.Feather, layer.Mask.Invert);
        if (previewHash != lastPreviewHash || noiseTexture == null)
        {
            lastPreviewHash = previewHash;
            GeneratePreviews();
        }

        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        DrawPreview(noiseTexture, "Noise");
        DrawPreview(maskTexture, "Mask");
        DrawPreview(combinedTexture, "Combined");
        GUILayout.EndHorizontal();
    }

    void GeneratePreviews()
    {
        var layer = (NoiseLayer)target;
        if (noiseTexture == null) noiseTexture = NewPreviewTexture();
        if (maskTexture == null) maskTexture = NewPreviewTexture();
        if (combinedTexture == null) combinedTexture = NewPreviewTexture();
        for (int z = 0; z < previewSize; z++)
        {
            for (int x = 0; x < previewSize; x++)
            {
                float noise = Perlin.Fbm(x / layer.NoiseScale, z / layer.NoiseScale, layer.Octave);
                float noiseValue = Mathf.InverseLerp(-1f, 1f, noise);
                noiseTexture.SetPixel(x, z, new Color(noiseValue, noiseValue, noiseValue));
                float maskValue = WorldGenerator.EvaluateMask(layer.Mask, Vector2.zero, x, z);
                maskTexture.SetPixel(x, z, new Color(maskValue, maskValue, maskValue));
                float combinedValue = noiseValue * maskValue;
                combinedTexture.SetPixel(x, z, new Color(combinedValue, combinedValue, combinedValue));
            }
        }
        noiseTexture.Apply();
        maskTexture.Apply();
        combinedTexture.Apply();
    }

    static Texture2D NewPreviewTexture() => new Texture2D(previewSize, previewSize) { hideFlags = HideFlags.HideAndDontSave };

    static void DrawPreview(Texture2D texture, string label)
    {
        GUILayout.BeginVertical();
        GUILayout.Label(label);
        GUILayout.Label(texture, GUILayout.Width(previewSize), GUILayout.Height(previewSize));
        GUILayout.EndVertical();
    }

    void OnDisable()
    {
        DestroyImmediate(noiseTexture);
        DestroyImmediate(maskTexture);
        DestroyImmediate(combinedTexture);
    }
}
#endif
