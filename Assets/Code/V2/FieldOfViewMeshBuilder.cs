using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace FieldOfView
{
    public struct MeshInfos
    {
        //BORDER Infos
        public int BorderQuadCount;
        public int BorderVertexCount => (BorderQuadCount + 1) * 2;
        public int BorderTrianglesCount => BorderQuadCount * 2;
        public int BorderTrianglesIndicesCount => BorderTrianglesCount * 3;
        
        //Steps
        public float BorderOuterStep;
        public float BorderInnerStep;
        
        public MeshInfos(float range, float sideAngleRadian, float widthLength, float thickness)
        {
            BorderQuadCount = (int)math.round(range);

            BorderOuterStep = range / BorderQuadCount;
            float2 borderDirection  = new (math.cos(math.PI - sideAngleRadian), math.sin(math.PI - sideAngleRadian));
            float2 outerBorderStart = new (-widthLength / 2, 0); 
            float2 innerBorderStart = outerBorderStart + new float2(borderDirection.y, -borderDirection.x) * thickness;
            float2 innerBorderEnd = innerBorderStart + borderDirection * (range - thickness);
            BorderInnerStep = math.distance(innerBorderStart, innerBorderEnd) / BorderQuadCount;
        }
    }
    
    [RequireComponent(typeof(FieldOfViewComponent), typeof(MeshFilter))]
    public class FieldOfViewMeshBuilder : MonoBehaviour
    {
        public const float THICKNESS = 0.2f;
        
        private MeshFilter meshFilter;
        private FieldOfViewComponent fieldOfViewComponent;
        
        public float Range                => fieldOfViewComponent.Range;
        public float WidthLength          => fieldOfViewComponent.WidthLength;
        public float OuterSideAngleRadian => fieldOfViewComponent.SideAngleRadian;
        
        public FieldOfViewMeshBuilder Initialize()
        {
            fieldOfViewComponent = GetComponent<FieldOfViewComponent>();
            meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = new Mesh { name = "FovMesh" };
            return this;
        }

        private void CreateMesh()
        {
            Mesh fovMesh = meshFilter.sharedMesh;
        }
        /*
        public void SetBorderVertices(NativeArray<float3> vertices, int borderVerticesCount)
        {
            int borderOuterStep = 0;
            int borderInnerStep = 0;
            
            int halfVertices = borderVerticesCount / 2;
            for (int i = 0; i < halfVertices; i++)
            {
                float2 baseLeftOffset = i * BorderDirection;
                //Left Outer
                float3 leftOuter = (OuterBorderStart + baseLeftOffset * borderOuterStep).x0y();
                vertices[i * 2] = leftOuter;
                //Left Inner
                float3 leftInner = (InnerBorderStart + baseLeftOffset * borderInnerStep).x0y();
                vertices[i * 2 + 1] = leftInner;
                
                //Right Outer
                int innerRightIndex = vertices.Length - (1 + i * 2);
                vertices[innerRightIndex] = leftInner - 2 * math.project(leftInner, math.right());
                //Right Inner
                int outerRightIndex = innerRightIndex - 1; // vertices.Length - (1 + i * 2) - 1;
                vertices[outerRightIndex] = leftOuter - 2 * math.project(leftOuter, math.right());
            }
        }
        */
    }
}
