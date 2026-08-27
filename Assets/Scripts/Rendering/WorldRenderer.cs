using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WorldRenderer : MonoBehaviour
{
    List<GameObject> cubes = new();

    public void RenderWorld(WorldData world, int chunkSize)
    {
        ClearCubes();
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
                    cubes.Add(cube);
                }
            }
        }
    }

    void ClearCubes()
    {
        if(cubes.Count == 0) return;
        for (int i = cubes.Count - 1; i >= 0; i--)
        {
            GameObject c = cubes[i];
            Destroy(c);
        }
    }
}