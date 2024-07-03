using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FieldOfView
{
    public class FieldOfViewComponent : MonoBehaviour
    {
        [SerializeField] private FieldOfViewMeshBuilder meshBuilder;
        
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
            meshBuilder     = GetComponent<FieldOfViewMeshBuilder>().Initialize();
        }

        public bool IsInsideFieldOfView() { return default; }
        public void OnWidthChanged(float newWidth) { /* Width Change */ }
    }
}
