using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace FieldOfView
{
    [Serializable]
    public struct FovMeshInfos
    {
        // Quads
        public int BorderQuadCount, ArcQuadCount, FrontLineQuadCount, QuadCount;
        
        public int BorderVertexCount, ArcVertexCount, FrontLineVertexCount, VerticesCount;

        // Triangles
        public int BorderTrianglesCount => BorderQuadCount * 2;
        public int ArcTrianglesCount => ArcQuadCount * 2;
        public int FrontTrianglesCount => FrontLineQuadCount * 2;
        public int TrianglesCount => QuadCount * 2;
        
        // Triangles Indices
        public int BorderTrianglesIndicesCount => BorderTrianglesCount * 3;
        public int ArcTrianglesIndicesCount => ArcTrianglesCount * 3;
        public int FrontTrianglesIndicesCount => FrontTrianglesCount * 3;
        public int TriangleIndicesCount => TrianglesCount * 3;
        
        public IndexFormat IndexFormat => VerticesCount * 2 < 65536 ? IndexFormat.UInt16 : IndexFormat.UInt32;
	
        public FovMeshInfos(float range, float sideAngleRadian, float widthLength, int resolution = 1)
        {
            resolution = math.max(1, resolution);
            BorderQuadCount    = resolution * (int)math.round(range);
            ArcQuadCount       = resolution * (int)math.max(math.round((math.PIHALF - sideAngleRadian) * range), 0);
            FrontLineQuadCount = resolution * (int)math.max(1, math.round(widthLength));
            QuadCount = 2 * (BorderQuadCount + ArcQuadCount) + FrontLineQuadCount;
            
            BorderVertexCount    = (BorderQuadCount + 1) * 2;
            // -2: remove vertices at borders to avoid duplicate (shared vertices with border and front)
            ArcVertexCount       = math.max(0, (ArcQuadCount + 1) - 2) * 2; 
            FrontLineVertexCount = (FrontLineQuadCount + 1) * 2;
            VerticesCount = 2 * (BorderVertexCount + ArcVertexCount) + FrontLineVertexCount;
        }

        public override string ToString()
        {
            return $"BorderQuadCount = {BorderQuadCount} | ArcQuadCount = {ArcQuadCount} | FrontLineQuadCount = {FrontLineQuadCount} | QuadCount = {QuadCount}" +
                   $"BorderVertexCount = {BorderVertexCount} | ArcVertexCount = {ArcVertexCount} | FrontLineVertexCount = {FrontLineVertexCount} | VerticesCount = {VerticesCount}";
        }
    }
}