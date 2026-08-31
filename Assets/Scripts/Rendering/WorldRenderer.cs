using System.Collections.Generic;
using UnityEngine;

public class WorldRenderer : MonoBehaviour
{
    readonly Dictionary<Vector3Int, GameObject> chunkObjects = new();

    // Full rebuild: throws away all chunk objects and creates them fresh.
    public void RenderWorld(WorldData world, WorldSettings settings)
    {
        ClearChunks();
        foreach (var chunkCoord in world.chunks.Keys)
            RenderChunk(world, settings, chunkCoord);
    }

    // Rebuilds one chunk. After a block edit, call this for the edited chunk —
    // and for the adjacent chunk too when the block sat on a chunk border.
    public void RenderChunk(WorldData world, WorldSettings settings, Vector3Int chunkCoord)
    {
        // -- drop the old version of this chunk, if any --
        if (chunkObjects.TryGetValue(chunkCoord, out var oldObject))
        {
            DestroyChunkObject(oldObject);
            chunkObjects.Remove(chunkCoord);
        }

        // -- build the mesh; empty/buried chunks produce no object at all --
        var mesh = ChunkMeshBuilder.Build(world, chunkCoord, settings.ChunkSize, out var typeIds);
        if (mesh == null)
            return;

        // -- create the chunk object at its world position --
        var chunkObject = new GameObject($"Chunk {chunkCoord}");
        chunkObject.transform.SetParent(transform, false);
        chunkObject.transform.position = (Vector3)(chunkCoord * settings.ChunkSize);
        chunkObject.AddComponent<MeshFilter>().mesh = mesh;

        // -- one material per submesh, matched to the block types used --
        var materials = new Material[typeIds.Count];
        for (int i = 0; i < typeIds.Count; i++)
            if (settings.TryGetBlockById(typeIds[i], out var block))
                materials[i] = block.Material;
        chunkObject.AddComponent<MeshRenderer>().materials = materials;

        chunkObjects[chunkCoord] = chunkObject;
    }

    void ClearChunks()
    {
        foreach (var chunkObject in chunkObjects.Values)
            DestroyChunkObject(chunkObject);
        chunkObjects.Clear();
    }

    // Destroying the GameObject does not free its Mesh — destroy it explicitly
    // or repeated regeneration leaks meshes.
    static void DestroyChunkObject(GameObject chunkObject)
    {
        Destroy(chunkObject.GetComponent<MeshFilter>().sharedMesh);
        Destroy(chunkObject);
    }
}
