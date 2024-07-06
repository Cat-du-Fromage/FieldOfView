using Unity.Mathematics;
using UnityEngine.Rendering;

namespace FieldOfView
{
    public struct MeshInfos
    {
        // Borders Infos
        public int BorderQuadCount;
        public int BorderVertexCount => (BorderQuadCount + 1) * 4;
        public int BorderTrianglesCount => BorderQuadCount * 2;
        public int BorderTrianglesIndicesCount => BorderTrianglesCount * 3;
        
        //Steps
        public float BorderOuterStep;
        public float BorderInnerStep;
        
        // General Infos
        public int VerticesCount => BorderVertexCount;// + ArcVerticesCount + FrontVerticesCount;
        public int TriangleIndicesCount => BorderTrianglesIndicesCount;// + ArcTrianglesIndicesCount + FrontTrianglesIndicesCount;
        
        public MeshInfos(float range, float sideAngleRadian, float widthLength, float thickness)
        {
            BorderQuadCount = (int)math.round(range);
            BorderOuterStep = range / BorderQuadCount;
            BorderInnerStep = (range - thickness) / BorderQuadCount;
        }
    }
}