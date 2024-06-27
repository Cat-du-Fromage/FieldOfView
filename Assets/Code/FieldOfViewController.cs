using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace FieldOfView
{

    public partial class FieldOfViewController : MonoBehaviour
    {
        [SerializeField] private GameObject FieldOfViewPrefab;
        
        [SerializeField] private float Range;
        [SerializeField] private float SideAngleDegrees;
        
        // Formation Data
        [SerializeField, Min(1)] private int FormationWidth = 6;
        [SerializeField, Min(0)] private float SpaceBetweenUnits = 0f;
        [SerializeField, Min(1)] private Vector2 UnitSize = Vector2.one;

        [field:SerializeField] public FieldOfViewComponent FieldOfView { get; private set; }

        private Vector2 DistanceUnitToUnit => UnitSize + new Vector2(SpaceBetweenUnits, SpaceBetweenUnits);
        private float WidthLength => (FormationWidth - 1) * (UnitSize.x + SpaceBetweenUnits);
        
        private void Awake()
        {
            FieldOfView = Instantiate(FieldOfViewPrefab, transform).GetComponent<FieldOfViewComponent>();
            FieldOfView.transform.localPosition += Vector3.down;
        }

        private void Start()
        {
            FieldOfView.Initialize(Range, SideAngleDegrees * math.TORADIANS, WidthLength, ResolutionMesh);
        }
    }

    
#if UNITY_EDITOR
    public partial class FieldOfViewController : MonoBehaviour
    {
        [SerializeField, Range(1,4)] private int ResolutionMesh = 1;
        
        public void OnDrawGizmos()
        {
            if (FormationWidth < 2) return;
            DrawGhostUnits(Color.green,Color.white);
        }
        
        private void DrawGhostUnits(Color sideUnits, Color betweenUnits)
        {
            Vector3 startPosition = transform.position - WidthLength / 2 * transform.right;
            for (int i = 0; i < FormationWidth; i++)
            {
                bool isBorderUnit = i == 0 || i == FormationWidth - 1;
                Gizmos.color = isBorderUnit ? sideUnits : betweenUnits;
                
                Vector3 unitPosition = startPosition + transform.right * i * DistanceUnitToUnit.x;
                Gizmos.DrawWireSphere(unitPosition, 0.5f);
            }
        }
    }
#endif
}
