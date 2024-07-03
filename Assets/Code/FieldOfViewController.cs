using UnityEngine;
using Unity.Mathematics;

namespace FieldOfView
{
    public partial class FieldOfViewController : MonoBehaviour
    {
        [SerializeField] private GameObject FieldOfViewPrefab;
        
        [SerializeField, Range(1,4)] private int ResolutionMesh = 1;  
        
        [SerializeField] private float Range;
        [SerializeField] private float SideAngleDegrees;
        // Row Size
        [SerializeField] private int FormationWidth = 6;
        [SerializeField] private float SpaceBetweenUnits = 0f;
        [SerializeField] private float UnitSize = 1f;
        private float DistanceUnitToUnit => UnitSize + SpaceBetweenUnits;
        private float WidthLength => (FormationWidth - 1) * DistanceUnitToUnit;
#region Initialization
        [field:SerializeField] public FieldOfViewComponent2 FieldOfView { get; private set; }
        
        private void Awake()
        {
            FieldOfView = Instantiate(FieldOfViewPrefab, transform).GetComponent<FieldOfViewComponent2>();
            FieldOfView.transform.localPosition += Vector3.down;
        }

        private void Start()
        {
            FieldOfView.Initialize(Range, math.radians(SideAngleDegrees), WidthLength, ResolutionMesh);
        }
#endregion
    }
    
#if UNITY_EDITOR
    public partial class FieldOfViewController : MonoBehaviour
    {
        private void DrawGhostUnits(Color baseColor, Color borderColor)
        {
            const float radius = 0.5f;

            float midWidth        = WidthLength / 2;
            Vector3 startPosition = transform.position - midWidth * transform.right;
            Vector3 baseOffset    = transform.right * DistanceUnitToUnit;
            
            for (int i = 0; i < FormationWidth; i++)
            {
                bool isBorderUnit = i == 0 || i == FormationWidth - 1;
                Gizmos.color = isBorderUnit ? borderColor : baseColor;
                
                Vector3 position = startPosition + baseOffset * i;
                Gizmos.DrawWireSphere(position, radius);
            }
        }
        
        public void OnDrawGizmos()
        {
            if (FormationWidth < 2) return;
            DrawGhostUnits(Color.white, Color.green);
        }
    }
#endif
}
