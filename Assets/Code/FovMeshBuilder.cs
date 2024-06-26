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

namespace FieldOfView
{
    public class FovMeshBuilder : MonoBehaviour
    {
        public const float Thickness = 0.5f;

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
            
            Gizmos.color = Color.yellow;
            float2 normalLeftCw = new float2(LeftDirection.y, -LeftDirection.x) * Thickness;
            float2 leftInnerEnd = LeftPosition + LeftDirection * (Range - Thickness);
            Gizmos.DrawSphere(leftInnerEnd.x0y(), 0.05f);
            Gizmos.DrawSphere((leftInnerEnd+normalLeftCw).x0y(), 0.05f);
            
            Gizmos.color = Color.magenta;
            float3 offset = up() + right() * 0.075f;
            for (int i = 0; i < DebugVertices.Length; i++)
            {
                Gizmos.DrawWireSphere(DebugVertices[i], 0.05f);
                Handles.Label(DebugVertices[i] + offset, $"{i}");
            }
        }

        public void Initialize(float range, float sideAngleRadian, float widthLength, int resolution = 1)
        {
            (Range, SideAngleRadian, WidthLength) = (range, sideAngleRadian, widthLength);
            
            LeftPosition  = new float2(-widthLength / 2, 0); //-right().xz * (widthLength / 2);
            RightPosition = new float2(widthLength / 2, 0); //right().xz * (widthLength / 2);
		
            LeftDirection  = new float2(cos(PI - sideAngleRadian), sin(PI - sideAngleRadian));
            RightDirection = new float2(cos(sideAngleRadian), sin(sideAngleRadian));
            
            MeshInfos = new FovMeshInfos(range, sideAngleRadian, widthLength, resolution);
            
            float2 normalLeftCw = new float2(LeftDirection.y, -LeftDirection.x) * Thickness;
            
            float2 leftInnerEnd = LeftPosition + LeftDirection * (range - Thickness) + normalLeftCw;
            
            float distanceInner = distance(LeftPosition + float2(Thickness,0), leftInnerEnd);
            
            InitializeBorder(leftInnerEnd);
            InitializeArc(leftInnerEnd);
            
            print($"dst outer = {range}, distance inner = {distanceInner}");
            CreateMesh();
        }
        
        private void InitializeBorder(float2 leftInnerEnd)
        {
            float distanceInner = distance(LeftPosition + float2(LeftDirection.y, -LeftDirection.x) * Thickness, leftInnerEnd);
            
            BorderOuterStep = Range / MeshInfos.BorderQuadCount;
            BorderInnerStep = distanceInner / MeshInfos.BorderQuadCount;
            
            print($"BorderOuterStep = {BorderOuterStep}, BorderInnerStep = {BorderInnerStep}");
        }

        private void InitializeArc(float2 leftInnerEnd)
        {
            //int outerVerticesCount = MeshInfos.ArcVertexCount / 2;
            float2 leftStartToInnerEnd = normalize(leftInnerEnd - LeftPosition);
            InnerSideAngleRadian = PI - atan2(leftStartToInnerEnd.y, leftStartToInnerEnd.x);
            print($"InnerSideAngleRadian = {degrees(InnerSideAngleRadian)}, SideAngleRadian = {degrees(SideAngleRadian)}");
            ArcOuterStep = (PI - SideAngleRadian - PIHALF) / MeshInfos.ArcQuadCount;
            ArcInnerStep = (PI - InnerSideAngleRadian - PIHALF) / MeshInfos.ArcQuadCount;
            print($"SideAngleRadian = {SideAngleRadian}, InnerSideAngleRadian = {InnerSideAngleRadian}");
            print($"ArcOuterStep = {ArcOuterStep}, ArcInnerStep = {ArcInnerStep}");
        }

        public void CreateMesh()
        {
            int verticesCount = (MeshInfos.BorderVertexCount * 2) + (MeshInfos.ArcVertexCount * 2) + MeshInfos.FrontLineVertexCount;
            int triangleIndicesCount = (MeshInfos.BorderTrianglesIndicesCount * 2) + (MeshInfos.ArcTrianglesIndicesCount * 2) + MeshInfos.FrontTrianglesIndicesCount;
            //int verticesCount = (MeshInfos.BorderVertexCount * 2);
            //int triangleIndicesCount = (MeshInfos.BorderTrianglesIndicesCount * 2);
            print($"verticesCount = {verticesCount} | triangleIndicesCount = {triangleIndicesCount}");
            
            Mesh fovMesh = MeshFilter.mesh;
            fovMesh.Clear();
            
            NativeArray<float3> vertices = new (verticesCount, Temp, UninitializedMemory);
            BorderVertices(vertices);
            SetArcVertices(vertices);
            SetFrontVertices(vertices);
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
            
            //fovMesh.Optimize();
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

            //float2 startInner = LeftPosition + innerLeftDir;
            //float2 endInner = (LeftPosition + LeftDirection * Range) - (LeftDirection * Thickness) + innerLeftDir;
            //print($"BorderVertices: check dst = {distance((LeftPosition + LeftDirection * Range) - (LeftDirection * Thickness), endInner)}");
            //print($"BorderVertices: startInner = {startInner} | endInner = {endInner} | dst = {distance(startInner, endInner)}");
            //BorderInnerStep = distance(startInner, endInner) / MeshInfos.BorderQuadCount;
            
            int halfVertices = MeshInfos.BorderVertexCount / 2;
            for (int i = 0; i < halfVertices; i++)
            {
                // LEFT
                float2 baseLeftOffset = i * LeftDirection;
                //Left Outer
                float2 left = LeftPosition + baseLeftOffset * BorderOuterStep;
                vertices[i * 2] = left.x0y();

                //Left Inner
                float2 leftStartInner = LeftPosition + innerLeftDir;
                float2 leftInner = leftStartInner + baseLeftOffset * BorderInnerStep;
                vertices[i * 2 + 1] = leftInner.x0y();
                // ==================================================================
                // RIGHT
                int innerRightIndex = vertices.Length - (1 + (i * 2));
                int outerRightIndex = vertices.Length - (1 + (i * 2 + 1));
                float2 baseRightOffset = i * RightDirection;
                //Right Outer
                float2 right = RightPosition + baseRightOffset * BorderOuterStep;
                vertices[outerRightIndex] = right.x0y();
                
                //Right Inner
                float2 rightStartInner = RightPosition + innerRightDir;
                float2 rightInner = rightStartInner + baseRightOffset * BorderInnerStep;
                vertices[innerRightIndex] = rightInner.x0y();
            }
        }

        private void SetFrontVertices(NativeArray<float3> vertices)
        {
            int startIndex = MeshInfos.BorderVertexCount + MeshInfos.ArcVertexCount;
            float frontSteps = WidthLength / MeshInfos.FrontLineQuadCount;
            //print($"SetFrontVertices: startIndex = {startIndex} | frontSteps = {frontSteps} | FrontLineVertexCount = {MeshInfos.FrontLineVertexCount}");
            float2 start = LeftPosition + forward().xz * Range;
            float2 startInner = start + back().xz * Thickness;
            
            int halfVertices = MeshInfos.FrontLineVertexCount / 2;
            for (int i = 0; i < halfVertices; i++)
            {
                float2 offset = i * right().xz * frontSteps;
                int indexOuter = startIndex + i * 2;
                int indexInner = startIndex + i * 2 + 1;
                //print($"at {i} : indexOuter = {indexOuter} | indexInner = {indexInner}");
                //Outer
                vertices[indexOuter] = (start + offset).x0y();
                //Inner
                vertices[indexInner] = (startInner + offset).x0y();
            }
        }


        private void SetArcVertices(NativeArray<float3> vertices)
        {
            int startIndex = MeshInfos.BorderVertexCount;
            // ARC Vertices
            float outerAngleStep = (PI - SideAngleRadian - PIHALF) / MeshInfos.ArcQuadCount;
            float innerAngleStep = (PI - InnerSideAngleRadian - PIHALF) / MeshInfos.ArcQuadCount;
            print($"SetArcVertices: outerAngleStep = {outerAngleStep}({degrees(outerAngleStep)}) | innerAngleStep = {innerAngleStep}({degrees(innerAngleStep)})");
            
            // start at 180(PI) because of indices order
            float leftStartAngle = PI - SideAngleRadian;
            float leftStartAngleInner = PI - InnerSideAngleRadian;
            print($"SetArcVertices: leftStartAngle = {leftStartAngle}({degrees(leftStartAngle)}) | leftStartAngleInner = {leftStartAngleInner}({degrees(leftStartAngleInner)})");
            int halfVertices = MeshInfos.ArcVertexCount / 2;
            for (int i = 0; i < halfVertices; i++)
            {
                //Left Outer
                float currentAngleInRadian = leftStartAngle - (i + 1) * outerAngleStep;
                float2 direction = float2(cos(currentAngleInRadian), sin(currentAngleInRadian));
                float3 outerLeft = (LeftPosition + direction * Range).x0y();
                vertices[startIndex + i * 2] = outerLeft;
                
                //Left Inner
                float innerAngleInRadian = leftStartAngleInner - (i + 1) * innerAngleStep;
                float2 innerDirection = float2(cos(innerAngleInRadian), sin(innerAngleInRadian));
                float3 innerLeft = (LeftPosition + innerDirection * (Range - Thickness)).x0y();
                vertices[startIndex + i * 2 + 1] = innerLeft;
                print($"at {i}: currentAngleInRadian = {currentAngleInRadian}({degrees(currentAngleInRadian)}) | innerAngleInRadian = {innerAngleInRadian}({degrees(innerAngleInRadian)})");
                //Right Outer
                int innerRightIndex = vertices.Length - (1 + MeshInfos.BorderVertexCount + (i * 2));
                vertices[innerRightIndex] = (innerLeft.xz - 2 * project(innerLeft.xz, right().xz)).x0y();
                
                //Right Inner
                int outerRightIndex = vertices.Length - (1 + MeshInfos.BorderVertexCount + (i * 2 + 1));
                vertices[outerRightIndex] = (outerLeft.xz - 2 * project(outerLeft.xz, right().xz)).x0y();
                //print($"at {i}: direction = {direction} | innerDirection = {innerDirection}");
                //print($"at {i}: currentAngleInRadian = {currentAngleInRadian} | innerAngleInRadian = {innerAngleInRadian}");
            }
        }
        
    }
}
