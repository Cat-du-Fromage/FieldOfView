using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace FieldOfView
{
    [RequireComponent(typeof(FovMeshBuilder2))]
    public class FieldOfViewComponent2 : MonoBehaviour
    {
        [FormerlySerializedAs("MeshBuilder")] [SerializeField] private FovMeshBuilder2 meshBuilder2;
        public float Range { get; private set; }
        public float SideAngleRadian { get; private set; }
        public float WidthLength { get; private set; }
        
        public void Initialize(float range, float sideAngleRadian, float widthLength, int resolution = 1)
        {
            Range = range;
            SideAngleRadian = sideAngleRadian;
            WidthLength = widthLength;
            
            meshBuilder2 = GetComponent<FovMeshBuilder2>();
            meshBuilder2.Initialize(range, sideAngleRadian, widthLength, resolution);
        }

        public bool IsInsideFieldOfView() { return default; }
        public void OnWidthChanged(float newWidth) { /* Width Change */ }
    }
}
