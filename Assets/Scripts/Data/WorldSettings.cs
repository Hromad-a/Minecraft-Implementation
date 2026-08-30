using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "World Settings", fileName = "WorldSettings")]
public class WorldSettings : ScriptableObject
{
    [SerializeField] private int ySize = 64;
    [SerializeField] private int xSize = 64;
    [SerializeField] private int zSize = 64;
    [SerializeField] private int chunkSize = 8;
    [SerializeField, Range(0f, 1f), Tooltip("Default terrain height as fraction of world height")] private float groundLevel = 0.5f;
    [SerializeField, Tooltip("No value = Random")] private string seed;
    [SerializeField] private List<NoiseLayer> noiseLayers;
    [SerializeField] private List<BlockDefinitionBase> blocks;

    public string Seed { get { return seed; } }
    public int YSize { get { return ySize; } }
    public int XSize { get { return xSize; } }
    public int ZSize { get { return zSize; } }
    public int ChunkSize {get{return chunkSize;}}
    public float GroundLevel { get { return groundLevel; } }
    public List<NoiseLayer> NoiseLayers { get { return noiseLayers; } }
    public List<BlockDefinitionBase> Blocks {get {return blocks;}}

    public void RegenerateWorld()
    {
        if(!Application.isPlaying) return;
        var world = FindAnyObjectByType<World>();
        world.RegenerateWorld();
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