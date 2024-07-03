using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace FieldOfView
{
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
    }
}
