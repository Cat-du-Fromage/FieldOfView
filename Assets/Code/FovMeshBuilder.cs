using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace FieldOfView
{
    [Serializable]
    public struct FovMeshInfos
    {
        public int BorderQuadCount, ArcQuadCount, FrontLineQuadCount;
        
        public int BorderVertexCount, ArcVertexCount, FrontLineVertexCount;

        public readonly int VerticesCount => 2 * (BorderVertexCount * 2 + ArcVertexCount * 2 + FrontLineVertexCount);
        //public readonly int TrianglesCount => ((BorderResolution + 1) * 2 + (ArcResolution + 1)) * 2;
        //public readonly int TriangleIndicesCount => TrianglesCount * 3;
	
        public FovMeshInfos(float range, float sideAngleRadian, float widthLength)
        {
            BorderQuadCount    = (int)round(range);
            ArcQuadCount       = (int)max(round((PIHALF - sideAngleRadian) * range), 0);
            FrontLineQuadCount = (int)max(1, round(widthLength));
            Debug.Log($"BorderQuadCount = {BorderQuadCount} | ArcQuadCount = {ArcQuadCount} | FrontLineQuadCount = {FrontLineQuadCount}");
            
            BorderVertexCount = BorderQuadCount + 1;
            ArcVertexCount = max(0, ArcQuadCount - 2);
            FrontLineVertexCount = FrontLineQuadCount + 1;
            Debug.Log($"BorderVertexCount = {BorderVertexCount} | ArcVertexCount = {ArcVertexCount} | FrontLineVertexCount = {FrontLineVertexCount}");
            //remove vertices at borders
            //Debug.Log($"round((PIHALF - sideAngleRadian) * range = { (int)(round((sideAngleRadian) * range)) }");
            
        }
    }
    
    public class FovMeshBuilder : MonoBehaviour
    {
        public const float Thickness = 0.2f;

        public MeshFilter MeshFilter;
	
        public FovMeshInfos MeshInfos;
	
        public float2 LeftPosition, RightPosition;
        public float2 LeftDirection, RightDirection;

        public float BorderOuterStep, BorderInnerStep;

        private void Awake()
        {
            MeshFilter = GetComponent<MeshFilter>();
        }

        public void Initialize(float range, float sideAngleRadian, float widthLength)
        {
            LeftPosition  = -right().xz * (widthLength / 2);
            RightPosition = right().xz * (widthLength / 2);
		
            LeftDirection  = float2(cos(PI - sideAngleRadian), sin(PI - sideAngleRadian));
            RightDirection = float2(cos(sideAngleRadian), sin(sideAngleRadian));
            
            MeshInfos = new FovMeshInfos(range, sideAngleRadian, widthLength);

            BorderOuterStep = range / MeshInfos.BorderVertexCount;
            
            float2 leftInnerEnd = LeftPosition + LeftDirection * (range - Thickness) + float2(LeftDirection.y, -LeftDirection.x) * Thickness;
            float distanceInner = distance(LeftPosition + right().xz * Thickness, leftInnerEnd);
            BorderInnerStep = distanceInner / MeshInfos.BorderVertexCount;
            print($"dst outer = {range}, distance inner = {distanceInner}");
            
            CreateMesh();
        }

        public void CreateMesh()
        {
            //int verticesCount = MeshInfos.VerticesCount;
            //int triangleIndicesCount =  MeshInfos.TriangleIndicesCount;
            Mesh fovMesh = MeshFilter.mesh;
            fovMesh.Clear();
            
            
        }

        public void BorderVertices(NativeArray<float3> vertices)
        {
            //Inner vertices Offsets
            float2 innerRightDir = float2(Thickness, 0);
            float2 innerLeftDir = float2(-Thickness, 0);
            
            for (int i = 0; i < MeshInfos.BorderVertexCount; i++)
            {
                bool isLeftInner = (i & 1) == 1;
                //Left
                float2 leftStart = LeftPosition + (isLeftInner ? innerRightDir : 0);
                float2 left = leftStart + i * LeftDirection * (isLeftInner ? BorderInnerStep : BorderOuterStep);
                vertices[i] = ToFloat3(left);
                
                //Right
                bool isRightInner = (i & 1) == 0;
                float2 rightStart = RightPosition + (isRightInner ? innerLeftDir : 0);
                float2 right = rightStart + i * RightDirection * (isRightInner ? BorderInnerStep : BorderOuterStep);
                vertices[^(1+i)] = ToFloat3(right);
            }

            return;
            float3 ToFloat3(float2 value) => new float3(value.x, 0, value.y);
        }
    }
}
