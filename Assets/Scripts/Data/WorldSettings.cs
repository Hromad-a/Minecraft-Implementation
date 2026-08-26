using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "World Settings", fileName = "WorldSettings")]
public class WorldSettings : ScriptableObject
{
    [SerializeField] private int ySize = 50;
    [SerializeField] private int chunkSize = 8;
    [SerializeField, Tooltip("No value = Random")] private string seed;
    [SerializeField] private Vector3 perlinValue;
    [SerializeField] private int octave = 3;

    public string Seed { get { return seed; } }
    public int YSize { get { return ySize; } }

    public void RegenerateWorld()
    {
        var seed = WorldGenerator.ResolveSeed(Seed);
        //WorldGenerator.
        Debug.Log("Seed offseted x: " + (perlinValue.x + WorldGenerator.GetSubSeed(seed, "xOffset")));
        Debug.Log("Seed offseted z: " + (perlinValue.z + WorldGenerator.GetSubSeed(seed, "zOffset")));
        Debug.Log(Perlin.Noise(perlinValue.x + WorldGenerator.GetSubSeed(seed, "xOffset"), perlinValue.y, perlinValue.z + WorldGenerator.GetSubSeed(seed, "zOffset")));
        Debug.Log("Fbm: " + Perlin.Fbm(perlinValue.x, perlinValue.y, perlinValue.z, octave));

    }
}

[CustomEditor(typeof(WorldSettings))]
public class WorldSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var settings = (WorldSettings)target;
        EditorGUILayout.Space();
        if (GUILayout.Button("Regenerate world"))
            settings.RegenerateWorld();
    }
}