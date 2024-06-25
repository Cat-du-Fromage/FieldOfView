using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace FieldOfView
{
    [Serializable]
    public struct FovMeshInfos
    {
        public int BorderQuadCount, ArcQuadCount, FrontLineQuadCount, QuadCount;
        
        public int BorderVertexCount, ArcVertexCount, FrontLineVertexCount, VerticesCount;

        public int BorderTrianglesCount => BorderQuadCount * 2;
        
        public IndexFormat IndexFormat => VerticesCount * 2 < 65536 ? IndexFormat.UInt16 : IndexFormat.UInt32;
	
        public FovMeshInfos(float range, float sideAngleRadian, float widthLength)
        {
            BorderQuadCount    = (int)math.round(range);
            ArcQuadCount       = (int)math.max(math.round((math.PIHALF - sideAngleRadian) * range), 0);
            FrontLineQuadCount = (int)math.max(1, math.round(widthLength));
            QuadCount = FrontLineQuadCount + (BorderQuadCount + ArcQuadCount) * 2;
            
            BorderVertexCount = (BorderQuadCount + 1) * 2;
            ArcVertexCount = math.max(0, (ArcQuadCount + 1) - 2) * 2;
            FrontLineVertexCount = (FrontLineQuadCount + 1) * 2;
            VerticesCount = FrontLineVertexCount + (BorderVertexCount + ArcVertexCount) * 2;
            //remove vertices at borders
            //Debug.Log($"round((PIHALF - sideAngleRadian) * range = { (int)(round((sideAngleRadian) * range)) }");
            DebugInfos();
        }

        public void DebugInfos()
        {
            Debug.Log($"BorderQuadCount = {BorderQuadCount} | ArcQuadCount = {ArcQuadCount} | FrontLineQuadCount = {FrontLineQuadCount} | QuadCount = {QuadCount}");
            Debug.Log($"BorderVertexCount = {BorderVertexCount} | ArcVertexCount = {ArcVertexCount} | FrontLineVertexCount = {FrontLineVertexCount} | VerticesCount = {VerticesCount}");
        }
    }
}