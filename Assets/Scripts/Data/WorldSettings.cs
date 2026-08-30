using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "World Settings", fileName = "WorldSettings")]
public class WorldSettings : ScriptableObject
{
    [SerializeField] private int ySize = 64;
    [SerializeField] private int xSize = 64;
    [SerializeField] private int zSize = 64;
    [SerializeField] private int baseHeightOffset = 0;
    [SerializeField] private int chunkSize = 8;
    [SerializeField, Tooltip("No value = Random")] private string seed;
    [SerializeField] private Vector3 perlinValue;
    [SerializeField] private int octave = 3;
    [SerializeField] private List<BlockDefinitionBase> blocks;

    public string Seed { get { return seed; } }
    public int YSize { get { return ySize; } }
    public int XSize { get { return xSize; } }
    public int ZSize { get { return zSize; } }
    public int BaseHeightOffset => baseHeightOffset;

    public int ChunkSize {get{return chunkSize;}}
    public int Octave { get { return octave; } }
    public List<BlockDefinitionBase> Blocks {get {return blocks;}}

    public void RegenerateWorld()
    {
        if(!Application.isPlaying) return;
        var world = FindAnyObjectByType<World>();
        world.RegenerateWorld();
        return;
        var seed = WorldGenerator.ResolveSeed(Seed);
        //WorldGenerator.
        Debug.Log("Seed offseted x: " + (perlinValue.x + WorldGenerator.GetSubSeed(seed, "xOffset")));
        Debug.Log("Seed offseted z: " + (perlinValue.z + WorldGenerator.GetSubSeed(seed, "zOffset")));
        Debug.Log(Perlin.Noise(perlinValue.x + WorldGenerator.GetSubSeed(seed, "xOffset"), perlinValue.y, perlinValue.z + WorldGenerator.GetSubSeed(seed, "zOffset")));
        Debug.Log("Fbm: " + Perlin.Fbm(perlinValue.x, perlinValue.y, perlinValue.z, octave));


    }

    public bool TryGetBlockById(int id, out BlockDefinitionBase block)
    {
        block = null;
        foreach (var b in blocks)
        {
            if (b.Id == id)
            {
                block = b;
                return true;
            }
        }
        return false;
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