using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FieldOfView
{
    public class FieldOfViewController : MonoBehaviour
    {
        [SerializeField] private GameObject FieldOfViewPrefab;
        
        [SerializeField] private float Range;
        [SerializeField] private float SideAngleDegrees;
        
        //Formation's row size
        [SerializeField, Min(1)] private int FormationWidth = 6;
        [SerializeField, Min(0)] private float SpaceBetweenUnits = 0f;
        [SerializeField, Min(1)] private Vector2 UnitSize = Vector2.one;
    }
}
