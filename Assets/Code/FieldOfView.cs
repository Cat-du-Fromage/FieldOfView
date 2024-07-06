using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine;

namespace FieldOfView
{
    public class FieldOfView : MonoBehaviour
    {
        public const float THICKNESS = 0.2f;
        
        private MeshFilter meshFilter;
        private MeshInfos meshInfos;
        
        public float Resolution { get; private set; }
        public float Range { get; private set; }
        public float SideAngleRadian { get; private set; }
        public float WidthLength { get; private set; }
        
        public void Initialize(float range, float sideAngleRadian, float widthLength, int resolution = 1)
        {
            Range           = range;
            SideAngleRadian = sideAngleRadian;
            WidthLength     = widthLength;
            Resolution      = resolution;

            meshInfos = new MeshInfos(range, sideAngleRadian, widthLength, THICKNESS);
            meshFilter = GetComponent<MeshFilter>();
            
        }
        
        private void CreateMesh(int verticesCount, int trianglesIndicesCount)
        {
            Mesh fovMesh = new Mesh { name = "FovMesh" };
            VertexAttributeDescriptor[] vertexAttributeDescriptors = 
            {
                new (VertexAttribute.Position, VertexAttributeFormat.Float32, dimension: 3, stream: 0),
                new (VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension: 3, stream: 1),
                new (VertexAttribute.Tangent, VertexAttributeFormat.Float16, dimension: 4, stream: 2),
                new (VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, dimension: 2, stream: 3),
            };
            fovMesh.SetVertexBufferParams(verticesCount, vertexAttributeDescriptors);
            fovMesh.SetIndexBufferParams(trianglesIndicesCount, IndexFormat.UInt16);
            
            NativeArray<float3> vertices = new(verticesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            SetBorderVertices(vertices, meshInfos.BorderVertexCount / 4);
            
            NativeArray<ushort> triangleIndices = new(trianglesIndicesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            
            meshFilter.sharedMesh = fovMesh;
        }
        
        public void SetBorderVertices(NativeArray<float3> vertices, int borderVerticesCount)
        {
            float2 borderDirection = new (math.PI - math.cos(SideAngleRadian),math.PI - math.sin(SideAngleRadian));
            float2 outerBorderStart = new float2(-WidthLength / 2, 0);
            float2 innerBorderStart = outerBorderStart + new float2(borderDirection.y, -borderDirection.x) * THICKNESS;
            
            for (int i = 0; i < borderVerticesCount; i++)
            {
                float2 baseLeftOffset = i * borderDirection;
                // ------------------------------------------ LEFT SIDE ------------------------------------------------
                // Left Outer
                int outerLeftIndex = i * 2;
                float2 leftOuter = (outerBorderStart + baseLeftOffset * meshInfos.BorderOuterStep);
                vertices[outerLeftIndex] = leftOuter.xyy * new float3(1,0,1);
                // Left Inner
                int innerLeftIndex = outerLeftIndex + 1;
                float2 leftInner = (innerBorderStart + baseLeftOffset * meshInfos.BorderInnerStep);
                vertices[innerLeftIndex] = leftInner.xyy * new float3(1,0,1);
                
                // ----------------------------------------- RIGHT SIDE ------------------------------------------------
                // Right Inner
                int innerRightIndex = vertices.Length - (1 + i * 2);
                float2 rightInner = leftInner - 2 * math.project(leftInner, math.right().xz);
                vertices[innerRightIndex] = rightInner.xyy * new float3(1,0,1);
                // Right Outer
                int outerRightIndex = innerRightIndex - 1; // vertices.Length - (1 + i * 2) - 1;
                float2 rightOuter = leftOuter - 2 * math.project(leftOuter, math.right().xz);
                vertices[outerRightIndex] = rightOuter.xyy * new float3(1,0,1);
            }
        }
    }
}
