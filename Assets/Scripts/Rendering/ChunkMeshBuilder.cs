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
    static readonly List<List<int>> trianglesPerSubmesh = new();

    // Rebuilds `mesh` as the cubic chunk's geometry with only air-facing faces.
    // Submeshes are index-aligned with settings.Blocks (empty ones stay empty),
    // so every chunk can share one material array. Returns false when the chunk
    // has no visible geometry (all air, or fully buried).
    public static bool Build(WorldData world, WorldSettings settings, Vector3Int chunkCoord, Mesh mesh)
    {
        int size = world.chunkSize;
        var blocks = world.GetChunkCells(chunkCoord);

        // -- fetch the six neighbor chunks once (null = above/below the world = air) --
        var neighbors = new BlockData[6][];
        for (int face = 0; face < 6; face++)
        {
            var neighborCoord = chunkCoord + faceDirections[face];
            if (neighborCoord.y >= 0 && neighborCoord.y < world.StackCount)
                neighbors[face] = world.GetChunkCells(neighborCoord);
        }

        // -- reset the shared buffers --
        vertices.Clear();
        normals.Clear();
        uvs.Clear();
        while (trianglesPerSubmesh.Count < settings.Blocks.Count)
            trianglesPerSubmesh.Add(new List<int>());
        for (int s = 0; s < trianglesPerSubmesh.Count; s++)
            trianglesPerSubmesh[s].Clear();

        // -- emit a face for every solid block side that touches air --
        // y-z-x order walks the array sequentially (BlockIndex = x + z*size + y*size²)
        int index = 0;
        for (int y = 0; y < size; y++)
        for (int z = 0; z < size; z++)
        for (int x = 0; x < size; x++)
        {
            var block = blocks[index++];
            if (!block.IsPresent)
                continue;
            var localPos = new Vector3Int(x, y, z);

            for (int face = 0; face < 6; face++)
            {
                if (IsFaceCovered(blocks, neighbors[face], localPos + faceDirections[face], size))
                    continue;
                AddFace(localPos, face, SubmeshIndex(settings, block.TypeId));
            }
        }

        // -- write the geometry into the mesh --
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = settings.Blocks.Count;
        for (int s = 0; s < settings.Blocks.Count; s++)
            mesh.SetTriangles(trianglesPerSubmesh[s], s);
        return vertices.Count > 0;
    }

    // Which submesh (index into settings.Blocks) a block type renders with.
    static int SubmeshIndex(WorldSettings settings, int typeId)
    {
        var blocks = settings.Blocks;
        for (int i = 0; i < blocks.Count; i++)
            if (blocks[i].Id == typeId)
                return i;
        return 0;
    }

    // Appends one quad (4 vertices, 2 triangles) into the buffers.
    static void AddFace(Vector3Int localPos, int face, int submesh)
    {
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
        var triangles = trianglesPerSubmesh[submesh];
        triangles.Add(i);
        triangles.Add(i + 1);
        triangles.Add(i + 2);
        triangles.Add(i + 2);
        triangles.Add(i + 1);
        triangles.Add(i + 3);
    }

    // Is the block on the far side of a face solid? neighborPos is in this chunk's
    // local coords and may be one step outside it on exactly one axis.
    static bool IsFaceCovered(BlockData[] blocks, BlockData[] neighborChunk, Vector3Int neighborPos, int size)
    {
        // -- neighbor inside this same chunk: direct lookup --
        bool insideChunk = neighborPos.x >= 0 && neighborPos.x < size
                        && neighborPos.y >= 0 && neighborPos.y < size
                        && neighborPos.z >= 0 && neighborPos.z < size;
        if (insideChunk)
            return blocks[WorldGenerator.BlockIndex(neighborPos.x, neighborPos.y, neighborPos.z, size)].IsPresent;

        // -- no chunk on that side (above/below the world): air --
        if (neighborChunk == null)
            return false;

        // -- neighbor in the adjacent chunk: wrap the out-of-range coordinate --
        var p = new Vector3Int((neighborPos.x + size) % size, (neighborPos.y + size) % size, (neighborPos.z + size) % size);
        return neighborChunk[WorldGenerator.BlockIndex(p.x, p.y, p.z, size)].IsPresent;
    }
}
