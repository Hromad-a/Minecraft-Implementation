using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World Settings", fileName = "WorldSettings")]
public class WorldSettings : ScriptableObject
{
    [SerializeField] private int ySize = 64;
    [SerializeField] private int chunkSize = 8;
    [SerializeField, Range(0f, 1f), Tooltip("Default terrain height as fraction of world height")] private float groundLevel = 0.5f;
    [SerializeField, Tooltip("No value = Random")] private string seed;
    [SerializeField] private List<NoiseLayer> noiseLayers;
    [SerializeField] private List<BlockDefinitionBase> blocks;
    [Header("Block type boundary jitter")]
    [SerializeField, Min(0f), Tooltip("How many blocks the type boundaries shift up/down; 0 = off")] private float typeJitterAmplitude = 4f;
    [SerializeField, Min(0.01f), Tooltip("Horizontal size of the boundary waves in blocks")] private float typeJitterScale = 30f;
    [SerializeField, Min(1)] private int typeJitterOctave = 2;

    public string Seed { get { return seed; } }
    public int YSize { get { return ySize; } }
    public int ChunkSize {get{return chunkSize;}}
    public float GroundLevel { get { return groundLevel; } }
    public List<NoiseLayer> NoiseLayers { get { return noiseLayers; } }
    public List<BlockDefinitionBase> Blocks {get {return blocks;}}
    public float TypeJitterAmplitude => typeJitterAmplitude;
    public float TypeJitterScale => typeJitterScale;
    public int TypeJitterOctave => typeJitterOctave;

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
