using UnityEngine;

public class WorldRenderer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var world = GetComponent<World>();
        if(world)
            RenderWorld(world.WorldData, world.WorldSettings.ChunkSize);
    }

    public void RenderWorld(WorldData world, int chunkSize)
    {
        foreach(var ch in world.chunks)
        {
            Vector3 chunkPos = new Vector3(ch.Key.x * chunkSize, ch.Key.y * chunkSize, ch.Key.z * chunkSize);
            RenderChunk(chunkPos, chunkSize, ch.Value);
        }
    }

    void RenderChunk(Vector3 center, int chunkSize, BlockData[] blocks)
    {
        for(int x = 0; x < chunkSize; x++)
        {
            for(int y = 0; y < chunkSize; y++)
            {
                for(int z = 0; z < chunkSize; z++)
                {
                    var isPresent = blocks[WorldGenerator.BlockIndex(x, y, z, chunkSize)].IsPresent;
                    if(!isPresent)
                        continue;
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.SetParent(transform, false);
                    cube.transform.position = new Vector3(center.x + x, center.y + y, center.z + z);
                    cube.transform.localScale = Vector3.one * .15f;
                }
            }
        }
    }
}