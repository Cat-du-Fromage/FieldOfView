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
        public int BorderVertexCount;
        public int ArcVertexCount;
        public int FrontLineVertexCount;
	
        public FovMeshInfos(float range, float sideAngleRadian, float widthLength)
        {
            BorderVertexCount = (int)round(range + 1);
            FrontLineVertexCount = (int)max(1, round(widthLength + 1));
            //remove vertices at borders
            //Debug.Log($"round((PIHALF - sideAngleRadian) * range = { (int)(round((sideAngleRadian) * range)) }");
            ArcVertexCount = (int)max(round((PIHALF - sideAngleRadian) * range) - 2, 0);
        }
    }
    
    public class FovMeshBuilder : MonoBehaviour
    {
        public const float Thickness = 0.2f;
	
        public FovMeshInfos MeshInfos;
	
        public float2 LeftPosition, RightPosition;
        public float2 LeftDirection, RightDirection;
	
        public void Initialize(float range, float sideAngleRadian, float widthLength)
        {
            LeftPosition  = -right().xz * (widthLength / 2);
            RightPosition = right().xz * (widthLength / 2);
		
            LeftDirection  = float2(cos(PI - sideAngleRadian), sin(PI - sideAngleRadian));
            RightDirection = float2(cos(sideAngleRadian), sin(sideAngleRadian));

            MeshInfos = new FovMeshInfos(range, sideAngleRadian, widthLength);
        }

        public void CreateMesh()
        {
            
        }

        public void BorderVertices(NativeArray<float3> vertices)
        {
            for (int i = 0; i < MeshInfos.BorderVertexCount; i++)
            {
                
            }
        }
    }
}
