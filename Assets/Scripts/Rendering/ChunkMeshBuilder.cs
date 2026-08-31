using System.Collections.Generic;
using UnityEngine;

public static class ChunkMeshBuilder
{
    // ---- face tables: direction of each face + its 4 corner vertices on a unit cube ----
    static readonly Vector3Int[] faceDirections =
    {
        new Vector3Int(0, 0, -1), // back
        new Vector3Int(0, 0, 1),  // front
        new Vector3Int(0, 1, 0),  // top
        new Vector3Int(0, -1, 0), // bottom
        new Vector3Int(-1, 0, 0), // left
        new Vector3Int(1, 0, 0),  // right
    };

    static readonly Vector3[][] faceVertices =
    {
        new[] { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,0,0), new Vector3(1,1,0) }, // back
        new[] { new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,0,1), new Vector3(0,1,1) }, // front
        new[] { new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(1,1,0), new Vector3(1,1,1) }, // top
        new[] { new Vector3(1,0,0), new Vector3(1,0,1), new Vector3(0,0,0), new Vector3(0,0,1) }, // bottom
        new[] { new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,0,0), new Vector3(0,1,0) }, // left
        new[] { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,0,1), new Vector3(1,1,1) }, // right
    };

    // ---- texture atlas: three tiles side by side; which tile each face uses ----
    const int TopTile = 0, SideTile = 1, BottomTile = 2;
    const float TileWidth = 1f / 3f;
    static readonly int[] faceTile = { SideTile, SideTile, TopTile, BottomTile, SideTile, SideTile };

    // ---- build buffers, reused across chunks to avoid garbage ----
    static readonly List<Vector3> vertices = new();
    static readonly List<Vector3> normals = new();
    static readonly List<Vector2> uvs = new();
    static readonly Dictionary<int, List<int>> trianglesByType = new();

    // Builds one chunk's mesh with only air-facing faces.
    // Returns null when nothing is visible. typeIds is index-aligned with the submeshes.
    public static Mesh Build(WorldData world, Vector3Int chunkCoord, int chunkSize, out List<int> typeIds)
    {
        var blocks = world.chunks[chunkCoord];

        // -- fetch the six neighboring chunks once (null = world edge on that side) --
        var neighbors = new BlockData[6][];
        for (int face = 0; face < 6; face++)
            world.chunks.TryGetValue(chunkCoord + faceDirections[face], out neighbors[face]);

        // -- reset the shared buffers --
        vertices.Clear();
        normals.Clear();
        uvs.Clear();
        trianglesByType.Clear();

        // -- emit a face for every solid block side that touches air --
        // y-z-x order walks the array sequentially (BlockIndex = x + z*size + y*size²)
        int index = 0;
        for (int y = 0; y < chunkSize; y++)
        for (int z = 0; z < chunkSize; z++)
        for (int x = 0; x < chunkSize; x++)
        {
            var block = blocks[index++];
            if (!block.IsPresent)
                continue;
            var localPos = new Vector3Int(x, y, z);

            for (int face = 0; face < 6; face++)
            {
                if (IsFaceCovered(blocks, neighbors[face], localPos + faceDirections[face], chunkSize))
                    continue;
                AddFace(localPos, face, block.TypeId);
            }
        }

        // -- assemble the mesh: one submesh per block type used --
        typeIds = new List<int>(trianglesByType.Keys);
        if (vertices.Count == 0)
            return null;

        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = typeIds.Count;
        for (int s = 0; s < typeIds.Count; s++)
            mesh.SetTriangles(trianglesByType[typeIds[s]], s);
        return mesh;
    }

    // Appends one quad (4 vertices, 2 triangles) into the buffers.
    static void AddFace(Vector3Int localPos, int face, int typeId)
    {
        if (!trianglesByType.TryGetValue(typeId, out var triangles))
            trianglesByType[typeId] = triangles = new List<int>();

        // 4 corners, all sharing the face's normal
        int i = vertices.Count;
        var corners = faceVertices[face];
        var normal = (Vector3)faceDirections[face];
        for (int c = 0; c < 4; c++)
        {
            vertices.Add(localPos + corners[c]);
            normals.Add(normal);
        }

        // UVs into the face's atlas tile; corner order is bl, tl, br, tr
        float u0 = faceTile[face] * TileWidth;
        float u1 = u0 + TileWidth;
        uvs.Add(new Vector2(u0, 0f));
        uvs.Add(new Vector2(u0, 1f));
        uvs.Add(new Vector2(u1, 0f));
        uvs.Add(new Vector2(u1, 1f));

        // two triangles over those corners
        triangles.Add(i);
        triangles.Add(i + 1);
        triangles.Add(i + 2);
        triangles.Add(i + 2);
        triangles.Add(i + 1);
        triangles.Add(i + 3);
    }

    // Is the block on the far side of a face solid? neighborPos is in this chunk's
    // local coords and may be one step outside it on exactly one axis.
    static bool IsFaceCovered(BlockData[] blocks, BlockData[] neighborChunk, Vector3Int neighborPos, int chunkSize)
    {
        // -- neighbor inside this same chunk: direct lookup --
        bool insideChunk = neighborPos.x >= 0 && neighborPos.x < chunkSize
                        && neighborPos.y >= 0 && neighborPos.y < chunkSize
                        && neighborPos.z >= 0 && neighborPos.z < chunkSize;
        if (insideChunk)
            return blocks[WorldGenerator.BlockIndex(neighborPos.x, neighborPos.y, neighborPos.z, chunkSize)].IsPresent;

        // -- neighbor beyond the world edge: treat as air --
        if (neighborChunk == null)
            return false;

        // -- neighbor in the adjacent chunk: wrap the out-of-range coordinate --
        // (-1 becomes chunkSize-1, chunkSize becomes 0; in-range values map to themselves)
        var p = new Vector3Int(
            (neighborPos.x + chunkSize) % chunkSize,
            (neighborPos.y + chunkSize) % chunkSize,
            (neighborPos.z + chunkSize) % chunkSize);
        return neighborChunk[WorldGenerator.BlockIndex(p.x, p.y, p.z, chunkSize)].IsPresent;
    }
}
