using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace FieldOfView
{

    public class FieldOfViewController : MonoBehaviour
    {
        [SerializeField] private FieldOfViewComponent FieldOfViewPrefab;
        
        [SerializeField] private FieldOfViewComponent FieldOfView;
        
        [SerializeField] private float Range;
        [SerializeField] private float SideAngleDegrees;
        
        // Formation Data
        [Min(1)] public int FormationWidth = 6;
        [Min(0)] public float SpaceBetweenUnits = 0.2f;
        [Min(1)] public Vector2 UnitSize = Vector2.one;
        private Vector2 DistanceUnitToUnit => UnitSize + new Vector2(SpaceBetweenUnits, SpaceBetweenUnits);
        private float WidthLength => (FormationWidth - 1) * (UnitSize.x + SpaceBetweenUnits);
        
        private void Awake()
        {
            FieldOfView = Instantiate(FieldOfViewPrefab, transform.position, transform.rotation, transform);
        }

        private void Start()
        {
            FieldOfView.Initialize(Range, SideAngleDegrees * math.TORADIANS, WidthLength);
        }

        public void OnDrawGizmos()
        {
            if (!Application.isPlaying || FormationWidth <= 1) return;
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
}
