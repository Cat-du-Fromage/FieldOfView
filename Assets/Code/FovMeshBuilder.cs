using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;
using static Unity.Collections.Allocator;
using static Unity.Collections.NativeArrayOptions;

namespace FieldOfView
{
    public class FovMeshBuilder : MonoBehaviour
    {
        public const float Thickness = 0.2f;

        [SerializeField] private float Range, SideAngleRadian, WidthLength;

        public float InnerSideAngleRadian;
        
        public MeshFilter MeshFilter;
	
        public FovMeshInfos MeshInfos;
	
        public float2 LeftPosition, RightPosition;
        public float2 LeftDirection, RightDirection;

        public float BorderOuterStep, BorderInnerStep;
        public float ArcOuterStep, ArcInnerStep;
        private float3[] DebugVertices = Array.Empty<float3>();

        private void Awake()
        {
            MeshFilter = GetComponent<MeshFilter>();
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = Color.magenta;
            for (int i = 0; i < DebugVertices.Length; i++)
            {
                Gizmos.DrawSphere(DebugVertices[i], 0.05f);
            }
        }

        public void Initialize(float range, float sideAngleRadian, float widthLength)
        {
            (Range, SideAngleRadian, WidthLength) = (range, sideAngleRadian, widthLength);
            
            LeftPosition  = new float2(-widthLength / 2, 0); //-right().xz * (widthLength / 2);
            RightPosition = new float2(widthLength / 2, 0); //right().xz * (widthLength / 2);
		
            LeftDirection  = new float2(cos(PI - sideAngleRadian), sin(PI - sideAngleRadian));
            RightDirection = new float2(cos(sideAngleRadian), sin(sideAngleRadian));
            
            MeshInfos = new FovMeshInfos(range, sideAngleRadian, widthLength);
            
            float2 normalLeftCw = new(LeftDirection.y, -LeftDirection.x);
            float2 leftInnerEnd = LeftPosition + LeftDirection * (range - Thickness) + normalLeftCw * Thickness;
            float distanceInner = distance(LeftPosition + float2(Thickness,0), leftInnerEnd);
            
            InitializeBorder(leftInnerEnd);
            InitializeArc(leftInnerEnd);
            
            print($"dst outer = {range}, distance inner = {distanceInner}");
            CreateMesh();
        }
        
        private void InitializeBorder(float2 leftInnerEnd)
        {
            int outerVerticesCount = MeshInfos.BorderVertexCount / 2 + 3;
            float distanceInner = distance(LeftPosition + float2(Thickness,0), leftInnerEnd);
            
            BorderOuterStep = Range / MeshInfos.BorderQuadCount;
            BorderInnerStep = distanceInner / MeshInfos.BorderQuadCount;
            print($"BorderOuterStep = {BorderOuterStep}, BorderInnerStep = {BorderInnerStep}");
        }

        private void InitializeArc(float2 leftInnerEnd)
        {
            int outerVerticesCount = MeshInfos.ArcVertexCount / 2;
            
            float2 leftStartToInnerEnd = normalize(leftInnerEnd - LeftPosition);
            InnerSideAngleRadian = PI - atan2(leftStartToInnerEnd.y, leftStartToInnerEnd.x);
            
            ArcOuterStep = (PIHALF - SideAngleRadian) * Range / outerVerticesCount;
            ArcInnerStep = (PIHALF - InnerSideAngleRadian) * Range / outerVerticesCount;
            
            print($"SideAngleRadian = {SideAngleRadian}, InnerSideAngleRadian = {InnerSideAngleRadian}");
            print($"ArcOuterStep = {ArcOuterStep}, ArcInnerStep = {ArcInnerStep}");
        }

        public void CreateMesh()
        {
            int verticesCount = MeshInfos.BorderVertexCount * 2;
            int triangleIndicesCount = (MeshInfos.BorderTrianglesCount * 2 + 2) * 3;
            print($"verticesCount = {verticesCount} | triangleIndicesCount = {triangleIndicesCount}");
            
            Mesh fovMesh = MeshFilter.mesh;
            fovMesh.Clear();
            
            NativeArray<float3> vertices = new (verticesCount, Temp, UninitializedMemory);
            BorderVertices(vertices);
            NativeArray<ushort> triangleIndices = new (triangleIndicesCount, Temp, UninitializedMemory);
            BuildTriangleIndices(triangleIndices);
            
            // Set Parameters Data
            fovMesh.SetVertexBufferParams(verticesCount, Utils.VertexAttributeDescriptors);
            fovMesh.SetIndexBufferParams(triangleIndicesCount, MeshInfos.IndexFormat);
            // Set Buffer Data
            fovMesh.SetVertexBufferData(vertices, 0,0, verticesCount, 0);
            fovMesh.SetIndexBufferData(triangleIndices, 0,0, triangleIndicesCount);

            SubMeshDescriptor descriptor = new(0, triangleIndices.Length) { vertexCount = vertices.Length };
            fovMesh.SetSubMesh(0, descriptor);
            //fovMesh.SetSubMesh(triangleIndices.Length, vertices.Length);
            
            fovMesh.Optimize();
            fovMesh.RecalculateNormals();
            fovMesh.RecalculateTangents();
            fovMesh.RecalculateBounds();
            //fovMesh.MarkDynamic();
            
            DebugVertices = vertices.ToArray();
        }
        
        private void BuildTriangleIndices(NativeArray<ushort> triangleIndices)
        {
            int borderOnlyTriangles = triangleIndices.Length / 3;
            for (int i = 0; i < borderOnlyTriangles; i++)
            {
                int triangleIndicesStartIndex = i * 3;
                bool isEven = (i & 1) == 0;// even => (+2,+1); odd => (+1,+2)
                int2 indices = isEven ? new(i + 2, i + 1) : new(i + 1, i + 2);
                triangleIndices[triangleIndicesStartIndex]     = (ushort)i;
                triangleIndices[triangleIndicesStartIndex + 1] = (ushort)indices[0];
                triangleIndices[triangleIndicesStartIndex + 2] = (ushort)indices[1];
            }
        }

        public void BorderVertices(NativeArray<float3> vertices)
        {
            //Inner vertices Offsets
            float2 innerLeftDir = new float2(LeftDirection.y, -LeftDirection.x) * Thickness;
            float2 innerRightDir = new float2(-RightDirection.y, RightDirection.x) * Thickness;
            
            for (int i = 0; i < MeshInfos.BorderVertexCount; i++)
            {
                //Left
                bool isLeftInner = (i & 1) == 1;
                
                float2 leftStart = LeftPosition + (isLeftInner ? innerLeftDir : 0);
                float offsetLeft = isLeftInner ? BorderInnerStep : BorderOuterStep;
                int indexLeftMultiplier = (i - (i & 1)) / 2;
                float2 left = leftStart + indexLeftMultiplier * LeftDirection * offsetLeft;
                vertices[i] = left.x0y();

                //int offset = (i & 1) == 0 ? 1 : 0;
                //vertices[^(1 + i + offset)] = (left - 2 * project(left, right().xz)).x0y();
                
                //Right
                int rightIndex = vertices.Length - (1 + i);
                bool isRightInner = (rightIndex & 1) == 1;
                
                float2 rightStart = RightPosition + (isRightInner ? innerRightDir : 0);
                float offsetRight = isRightInner ? BorderInnerStep : BorderOuterStep;
                int indexRightMultiplier = (i - 1 + (rightIndex & 1)) / 2;
                float2 right = rightStart + indexRightMultiplier * RightDirection * offsetRight;
                vertices[rightIndex] = right.x0y();
            }
        }

        private void SetFrontVertices(NativeArray<float3> vertices)
        {
            int startIndex = MeshInfos.BorderVertexCount + MeshInfos.ArcVertexCount;
            float frontSteps = WidthLength / MeshInfos.FrontLineQuadCount;
            for (int i = 0; i < MeshInfos.FrontLineVertexCount; i++)
            {
                
            }
        }


        private void SetArcVertices(NativeArray<float3> vertices)
        {
            int startIndex = MeshInfos.BorderVertexCount;
            
            // ARC Vertices
            float angleStep = SideAngleRadian / MeshInfos.ArcQuadCount;
            
            // start at 180(PI) because of indices order
            float leftStartAngle = PI - SideAngleRadian;
            float leftStartAngleInner = PI - InnerSideAngleRadian;
            
            
            for (int i = 0; i < MeshInfos.ArcVertexCount; i++)
            {
                float currentAngleInRadian = leftStartAngle - (i + 1) * angleStep;
                float2 direction = float2(cos(currentAngleInRadian), sin(currentAngleInRadian));
                
                vertices[startIndex++] = (LeftPosition + direction * Range).x0y();
                
                //vertices[startLeftIndex++] = (FovData.TriangleTip + direction * (Range - Thickness)).x0y();
            }
        }
        
    }
}
