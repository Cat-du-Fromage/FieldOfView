using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace FieldOfView
{
    public class FieldOfViewComponent : MonoBehaviour
    {
        [SerializeField] private FieldOfViewController FovController;
        [SerializeField] private FovMeshBuilder MeshBuilder;
        
        private void Awake()
        {
            
        }

        public void Initialize(float range, float sideAngleRadian, float widthLength)
        {
            MeshBuilder = TryGetComponent(out FovMeshBuilder meshBuilder) ? meshBuilder : gameObject.AddComponent<FovMeshBuilder>();
            MeshBuilder.Initialize(range, sideAngleRadian, widthLength);
        }
    }
}
