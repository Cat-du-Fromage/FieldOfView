using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;
using static Unity.Collections.Allocator;
using static Unity.Collections.NativeArrayOptions;
using float2 = Unity.Mathematics.float2;
using quaternion = Unity.Mathematics.quaternion;

namespace FieldOfView
{
    public partial class FovMeshBuilder : MonoBehaviour
    {
        public const float Thickness = 0.2f;

        [SerializeField] private float Range;
        [SerializeField] private float WidthLength;
        
        [SerializeField] private float SideAngleRadian;
        [SerializeField] private float InnerSideAngleRadian;
        
        public MeshFilter MeshFilter;
        private FovMeshInfos meshInfos;
        
        private float3[] meshVertices = Array.Empty<float3>();

    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Accessors ◈◈◈◈◈◈                                                                                        ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
    
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ BORDER ◇◇◇◇◇◇                                                                                      │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        // Directions (Left border)
        public float2 BorderDirection  => new (math.cos(PI - SideAngleRadian), math.sin(PI - SideAngleRadian));
        
        // Start Positions
        public float2 OuterBorderStart => new (-WidthLength / 2, 0); 
        public float2 InnerBorderStart => OuterBorderStart + new float2(BorderDirection.y, -BorderDirection.x) * Thickness;
        
        //Steps
        public float BorderOuterStep => Range / meshInfos.BorderQuadCount;
        public float BorderInnerStep => math.distance(InnerBorderStart, InnerBorderStart + BorderDirection * (Range - Thickness)) / meshInfos.BorderQuadCount;
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ ARC ◇◇◇◇◇◇                                                                                         │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        // Start Angles (Radian)
        public float OuterArcStart => PI - SideAngleRadian;
        public float InnerArcStart => PI - InnerSideAngleRadian;
        
        // Steps
        public float OuterArcAngleStep => (PIHALF - SideAngleRadian) / meshInfos.ArcQuadCount;
        public float InnerArcAngleStep => (PIHALF - InnerSideAngleRadian) / meshInfos.ArcQuadCount;
        
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ FRONT ◇◇◇◇◇◇                                                                                       │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        // Start Positions
        public float2 OuterFrontStart => OuterBorderStart + new float2(0,Range);
        public float2 InnerFrontStart => OuterFrontStart + new float2(0,-Thickness);
        
        // Steps
        public float2 FrontStep => new float2(WidthLength / meshInfos.FrontLineQuadCount, 0);
        
        private void Awake()
        {
            MeshFilter = GetComponent<MeshFilter>();
        }

        public void Initialize(float range, float sideAngleRadian, float widthLength, int resolution = 1)
        {
            this.Range = range;
            this.SideAngleRadian = sideAngleRadian;
            this.WidthLength = widthLength;
            
            //float2 leftInnerEnd = InnerBorderStart + LeftDirection * (range - Thickness);
            //float2 leftStartToInnerEnd = math.normalize(leftInnerEnd - OuterBorderStart);
            float2 leftStartToInnerEnd = normalize(mad(BorderDirection, range - Thickness, InnerBorderStart) - OuterBorderStart);
            InnerSideAngleRadian = PI - math.atan2(leftStartToInnerEnd.y, leftStartToInnerEnd.x);
            
            meshInfos = new FovMeshInfos(range, sideAngleRadian, widthLength, resolution);
            
            CreateMesh();
        }
        
        private void CreateMesh()
        {
            Mesh fovMesh = MeshFilter.mesh;
            fovMesh.Clear();

            meshVertices = new float3[meshInfos.VerticesCount];
            //NativeArray<float3> vertices = new (meshInfos.VerticesCount, Temp, UninitializedMemory);
            BuildVertices(meshVertices);
            
            NativeArray<ushort> triangleIndices = new (meshInfos.TriangleIndicesCount, Temp, UninitializedMemory);
            BuildTriangleIndices(triangleIndices.AsSpan());
            
            // Set Parameters Data
            fovMesh.SetVertexBufferParams(meshInfos.VerticesCount, Utils.VertexAttributeDescriptors);
            fovMesh.SetIndexBufferParams(meshInfos.TriangleIndicesCount, meshInfos.IndexFormat);
            // Set Buffer Data
            fovMesh.SetVertexBufferData(meshVertices, 0,0, meshInfos.VerticesCount, 0);
            fovMesh.SetIndexBufferData(triangleIndices, 0,0, meshInfos.TriangleIndicesCount);
            
            SubMeshDescriptor descriptor = new(0, triangleIndices.Length) { vertexCount = meshVertices.Length };
            fovMesh.SetSubMesh(0, descriptor);
            
            fovMesh.Optimize();
            fovMesh.RecalculateNormals();
            fovMesh.RecalculateTangents();
            fovMesh.RecalculateBounds();
        }
        
        private void BuildTriangleIndices(Span<ushort> triangleIndices)
        {
            for (int i = 0; i < meshInfos.TrianglesCount; i++)
            {
                int triangleIndicesStartIndex = i * 3;
                //int2 indices = int2(i + 2 - (i & 1), i + 1 + (i & 1));
                bool isEven = (i & 1) == 0;// even => (+2,+1); odd => (+1,+2)
                int2 indices = isEven ? int2(i + 2, i + 1) : int2(i + 1, i + 2);
                triangleIndices[triangleIndicesStartIndex]     = (ushort)i;
                triangleIndices[triangleIndicesStartIndex + 1] = (ushort)indices[0];
                triangleIndices[triangleIndicesStartIndex + 2] = (ushort)indices[1];
            }
        }
        
        public void BuildVertices(Span<float3> vertices)
        {
            SetBorderVertices(vertices);
            SetArcVertices(vertices);
            SetFrontVertices(vertices);

            SetVerticesHeight(vertices);
        }

        private void SetVerticesHeight(Span<float3> vertices)
        {
            const int terrainLayerIndex = 8;
            const float groundOffset = 0.5f;
            const float heightOffset = 8;
            const float maxDistance = 16;
            
            /*
            NativeArray<float3> tmpVertices = new (vertices.ToArray(), Temp);
            transform.TransformPoints(tmpVertices.Reinterpret<Vector3>().AsSpan());
            float3 upOffset = up() * heightOffset;
            for (int i = 0; i < tmpVertices.Length; i++)
            {
                Ray ray = new (tmpVertices[i] + upOffset, Vector3.down);
                if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, 1 << terrainLayerIndex)) continue;
                vertices[i].y = hit.point.y + groundOffset;
            }
            */
            float3 transformForward = transform.forward;
            float3 transformPosition = transform.position;
            for (int i = 0; i < vertices.Length; i++)
            {
                float2 origin2D = vertices[i].xz + transformPosition.xz;
                //float angle       = acos(transformForward.z);
                //float signValue   = sign(-transformForward.x);
                //acos(dot(forward().xz, transformForward.xz)) * sign(forward().x * transformForward.y - forward().y * transformForward.x);;
                float signedAngle = acos(transformForward.z) * sign(-transformForward.x);
                math.sincos(signedAngle, out float sinA, out float cosA);
                
                //origin2D = float2(cosA * origin2D.x - sinA * origin2D.y, sinA * origin2D.x + cosA * origin2D.y);
                float2x2 rotationMatrix = new float2x2(cosA, -sinA, sinA,  cosA);
                origin2D = mul(rotationMatrix, origin2D);

                Ray ray = new (new Vector3(origin2D.x, heightOffset, origin2D.y), Vector3.down);
                if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, 1 << terrainLayerIndex)) continue;
                vertices[i].y = hit.point.y + groundOffset;
            }
        }
        
        public void SetBorderVertices(Span<float3> vertices)
        {
            int halfVertices = meshInfos.BorderVertexCount / 2;
            for (int i = 0; i < halfVertices; i++)
            {
                float2 baseLeftOffset = i * BorderDirection;
                //Left Outer
                float3 leftOuter = (OuterBorderStart + baseLeftOffset * BorderOuterStep).x0y();
                vertices[i * 2] = leftOuter;
                //Left Inner
                float3 leftInner = (InnerBorderStart + baseLeftOffset * BorderInnerStep).x0y();
                vertices[i * 2 + 1] = leftInner;
                
                //Right Outer
                int innerRightIndex = vertices.Length - (1 + i * 2);
                vertices[innerRightIndex] = leftInner - 2 * math.project(leftInner, math.right());
                //Right Inner
                int outerRightIndex = innerRightIndex - 1; // vertices.Length - (1 + i * 2) - 1;
                vertices[outerRightIndex] = leftOuter - 2 * math.project(leftOuter, math.right());
            }
        }

        private void SetFrontVertices(Span<float3> vertices)
        {
            int startIndex = meshInfos.BorderVertexCount + meshInfos.ArcVertexCount;
            int halfVertices = meshInfos.FrontLineVertexCount / 2;
            for (int i = 0; i < halfVertices; i++)
            {
                int index = startIndex + i * 2;
                float2 offset = i * FrontStep;
                vertices[index] = (OuterFrontStart + offset).x0y();//Outer
                vertices[index + 1] = (InnerFrontStart + offset).x0y();//Inner
            }
        }
        
        private void SetArcVertices(Span<float3> vertices)
        {
            int startIndex = meshInfos.BorderVertexCount;
            int halfVertices = meshInfos.ArcVertexCount / 2;
            for (int i = 0; i < halfVertices; i++)
            {
                //Left Outer
                float currentAngleInRadian = OuterArcStart - (i + 1) * OuterArcAngleStep;
                float2 outerDirection = new(math.cos(currentAngleInRadian), math.sin(currentAngleInRadian));
                float3 outerLeft = (OuterBorderStart + outerDirection * Range).x0y();
                vertices[startIndex + i * 2] = outerLeft;
                
                //Left Inner
                float innerAngleInRadian = InnerArcStart - (i + 1) * InnerArcAngleStep;
                float2 innerDirection = new(math.cos(innerAngleInRadian), math.sin(innerAngleInRadian));
                float3 innerLeft = (OuterBorderStart + innerDirection * (Range - Thickness)).x0y();
                vertices[startIndex + i * 2 + 1] = innerLeft;
                
                //Right Outer
                int innerRightIndex = vertices.Length - (1 + i * 2) - meshInfos.BorderVertexCount;
                vertices[innerRightIndex] = innerLeft - 2 * math.project(innerLeft, math.right());
                
                //Right Inner
                int outerRightIndex = innerRightIndex - 1; // vertices.Length - (1 + i * 2) - MeshInfos.BorderVertexCount - 1;
                vertices[outerRightIndex] = outerLeft - 2 * math.project(outerLeft, math.right());
            }
        }
    }
    
#if UNITY_EDITOR
    public partial class FovMeshBuilder : MonoBehaviour
    {
        public bool DebugIndicesNumber = false;
        
        
        
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || meshVertices == null || meshVertices.Length == 0) return;
            
            //Gizmos.color = Color.yellow;
            //float2 normalLeftCw = new float2(LeftDirection.y, -LeftDirection.x) * Thickness;
            //float2 leftInnerEnd = OuterBorderStart + LeftDirection * (Range - Thickness);
            //Gizmos.DrawSphere(leftInnerEnd.x0y(), 0.05f);
            //Gizmos.DrawSphere((leftInnerEnd+normalLeftCw).x0y(), 0.05f);

            NativeArray<float3> debugVertices = new (meshVertices, Temp);
            transform.TransformPoints(debugVertices.Reinterpret<Vector3>().AsSpan());
            
            Gizmos.color = Color.magenta;
            float3 offset = up() + right() * 0.075f;
            for (int i = 0; i < meshVertices.Length; i++)
            {
                Gizmos.DrawWireSphere(debugVertices[i], 0.05f);
                if (!DebugIndicesNumber) continue;
                Handles.Label(debugVertices[i] + offset, $"{i}");
            }
        }
    }
}
#endif