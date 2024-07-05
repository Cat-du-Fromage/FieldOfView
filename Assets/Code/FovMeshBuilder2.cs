using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

using static Unity.Mathematics.math;
using static Unity.Collections.Allocator;
using static Unity.Collections.NativeArrayOptions;

using float2 = Unity.Mathematics.float2;
using quaternion = Unity.Mathematics.quaternion;

namespace FieldOfView
{
    [RequireComponent(typeof(MeshFilter))]
    public partial class FovMeshBuilder2 : MonoBehaviour
    {
#region Raycast Constant
        private const int ORIGIN_HEIGHT   = 32;
        private const int RAY_DISTANCE    = 64;
        private const int TERRAIN_LAYER   = 1 << 8;
        private const float GROUND_OFFSET = 0.5f;
#endregion Raycast Constant
        private const float THICKNESS = 0.2f;

        public float Range;
        public float WidthLength;
        
        public float OuterSideAngleRadian;
        public float InnerSideAngleRadian;
        
        public MeshFilter MeshFilter;
        private FovMeshInfos meshInfos;
        
        private NativeArray<float3> meshVertices;

    //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
    //║ ◈◈◈◈◈◈ Accessors ◈◈◈◈◈◈                                                                                        ║
    //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        public float3 Position => transform.position;
        public float3 Forward => transform.forward;

#region BORDER Accessors
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ BORDER ◇◇◇◇◇◇                                                                                      │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        // Directions (Left border)
        public float2 BorderDirection  => new (math.cos(PI - OuterSideAngleRadian), math.sin(PI - OuterSideAngleRadian));
        
        // Start Positions
        public float2 OuterBorderStart => new (-WidthLength / 2, 0); 
        public float2 InnerBorderStart => OuterBorderStart + new float2(BorderDirection.y, -BorderDirection.x) * THICKNESS;
        
        // Steps
        public float BorderOuterStep => Range / meshInfos.BorderQuadCount;
        //public float BorderInnerStep => math.distance(InnerBorderStart, InnerBorderStart + BorderDirection * (Range - THICKNESS)) / meshInfos.BorderQuadCount;
        
        public float BorderInnerStep
        {
            get
            {
                float2 borderDirection = float2(cos(OuterSideAngleRadian), sin(OuterSideAngleRadian));
                float2 innerStart = float2(WidthLength / 2, 0) + float2(-borderDirection.y, borderDirection.x) * THICKNESS;
                return distance(innerStart, innerStart + borderDirection * (Range - THICKNESS)) / meshInfos.BorderQuadCount;
            }
        }

        #endregion BORDER Accessors
        
#region ARC Accessors
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ ARC ◇◇◇◇◇◇                                                                                         │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        // Start Angles (Radian)
        public float OuterArcStart => math.PI - OuterSideAngleRadian;
        public float InnerArcStart => math.PI - InnerSideAngleRadian;
        
        // Steps
        public float OuterArcAngleStep => (math.PIHALF - OuterSideAngleRadian) / meshInfos.ArcQuadCount;
        public float InnerArcAngleStep => (math.PIHALF - InnerSideAngleRadian) / meshInfos.ArcQuadCount;
#endregion ARC Accessors
        
#region Front Accessors
        //┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
        //│  ◇◇◇◇◇◇ FRONT ◇◇◇◇◇◇                                                                                       │
        //└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        // Start Positions
        public float2 OuterFrontStart => OuterBorderStart + new float2(0,Range);
        public float2 InnerFrontStart => OuterFrontStart + new float2(0,-THICKNESS);
        
        // Steps
        public float2 FrontStep => new float2(WidthLength / meshInfos.FrontLineQuadCount, 0);
#endregion Front Accessors

        private void Awake()
        {
            MeshFilter = GetComponent<MeshFilter>();
        }

        private void OnDestroy()
        {
            if (meshVertices.IsCreated) meshVertices.Dispose();
        }

        public void Initialize(float range, float sideAngleRadian, float widthLength, int resolution = 1)
        {
            this.Range = range;
            this.OuterSideAngleRadian = sideAngleRadian;
            this.WidthLength = widthLength;
            
            float2 borderInnerEnd = InnerBorderStart + BorderDirection * (range - THICKNESS);
            float2 borderOuterStartToInnerEnd = math.normalize(borderInnerEnd - OuterBorderStart);
            InnerSideAngleRadian = math.PI - math.atan2(borderOuterStartToInnerEnd.y, borderOuterStartToInnerEnd.x);
            
            meshInfos = new FovMeshInfos(range, sideAngleRadian, widthLength, resolution);
            
            CreateMesh();
        }
        
        private void CreateMesh()
        {
            Mesh fovMesh = MeshFilter.mesh;
            fovMesh.Clear();

            meshVertices = new NativeArray<float3>(meshInfos.VerticesCount, Persistent, UninitializedMemory);
            BuildVertices(meshVertices);
            
            NativeArray<ushort> triangleIndices = new (meshInfos.TriangleIndicesCount, Temp, UninitializedMemory);
            BuildTriangleIndices(triangleIndices);
            
            // Set Parameters Data
            fovMesh.SetVertexBufferParams(meshInfos.VerticesCount, Utils.VertexAttributeDescriptors);
            fovMesh.SetIndexBufferParams(meshInfos.TriangleIndicesCount, meshInfos.IndexFormat);
            // Set Buffer Data
            fovMesh.SetVertexBufferData(meshVertices, 0,0, meshInfos.VerticesCount, 0);
            fovMesh.SetIndexBufferData(triangleIndices, 0,0, meshInfos.TriangleIndicesCount);
            
            SubMeshDescriptor descriptor = new(0, triangleIndices.Length) { vertexCount = meshVertices.Length };
            fovMesh.SetSubMesh(0, descriptor);
            
            fovMesh.Optimize();
            fovMesh.RecalculateNormals();
            fovMesh.RecalculateTangents();
            fovMesh.RecalculateBounds();
        }
        
        private void BuildTriangleIndices(NativeArray<ushort> triangleIndices)
        {
            for (int i = 0; i < meshInfos.TrianglesCount; i++)
            {
                int triangleIndicesStartIndex = i * 3;
                //int2 indices = int2(i + 2 - (i & 1), i + 1 + (i & 1));
                bool isEven = (i & 1) == 0;// even => (+2,+1); odd => (+1,+2)
                int2 indices = isEven ? int2(i + 2, i + 1) : int2(i + 1, i + 2);
                triangleIndices[triangleIndicesStartIndex]     = (ushort)i;
                triangleIndices[triangleIndicesStartIndex + 1] = (ushort)indices[0];
                triangleIndices[triangleIndicesStartIndex + 2] = (ushort)indices[1];
            }
        }
        
        public void BuildVertices(NativeArray<float3> vertices)
        {
            SetBorderVertices(vertices);
            SetArcVertices(vertices);
            SetFrontVertices(vertices);

            SetVerticesHeight(vertices);
        }

        private void AdvancedSetVerticesHeight(NativeArray<float3> vertices)
        {
            QueryParameters queryParams = new QueryParameters(TERRAIN_LAYER);
            
            using NativeArray<RaycastHit> results = new (vertices.Length, TempJob, UninitializedMemory);
            RaycastHitsJob(results, vertices,queryParams).Complete();

            NativeSlice<float> pointsSlice = results.Slice(0).SliceWithStride<float3>(0).SliceWithStride<float>(4);
            NativeSlice<float> heights = vertices.Slice(0).SliceWithStride<float>(4);
            
            heights.CopyFrom(pointsSlice);
            for (int i = 0; i < heights.Length; i++)
            {
                heights[i] += GROUND_OFFSET;
            }
        }

        private void SetVerticesHeight(NativeArray<float3> vertices)
        {
            NativeSlice<float> heights = vertices.Slice(0).SliceWithStride<float>(4);
            
            Span<Vector3> tmpVertices = stackalloc Vector3[vertices.Length];
            transform.TransformPoints(vertices.Reinterpret<Vector3>().AsReadOnlySpan(), tmpVertices);
            
            Vector3 upOffset = Vector3.up * ORIGIN_HEIGHT;
            for (int i = 0; i < tmpVertices.Length; i++)
            {
                Ray ray = new (tmpVertices[i] + upOffset, Vector3.down);
                if (!Physics.Raycast(ray, out RaycastHit hit, RAY_DISTANCE, TERRAIN_LAYER)) continue;
                heights[i] = hit.point.y + GROUND_OFFSET;
                //vertices[i] = new float3(vertices[i].x, hit.point.y + groundOffset, vertices[i].z);
            }
        }
        
        public void SetBorderVertices(NativeArray<float3> vertices)
        {
            int halfVertices = meshInfos.BorderVertexCount / 2;
            for (int i = 0; i < halfVertices; i++)
            {
                float2 baseLeftOffset = i * BorderDirection;
                //Left Outer
                float3 leftOuter = (OuterBorderStart + baseLeftOffset * BorderOuterStep).x0y();
                vertices[i * 2] = leftOuter;
                //Left Inner
                float3 leftInner = (InnerBorderStart + baseLeftOffset * BorderInnerStep).x0y();
                vertices[i * 2 + 1] = leftInner;
                
                //Right Outer
                int innerRightIndex = vertices.Length - (1 + i * 2);
                vertices[innerRightIndex] = leftInner - 2 * math.project(leftInner, math.right());
                //Right Inner
                int outerRightIndex = innerRightIndex - 1; // vertices.Length - (1 + i * 2) - 1;
                vertices[outerRightIndex] = leftOuter - 2 * math.project(leftOuter, math.right());
            }
        }

        private void SetFrontVertices(NativeArray<float3> vertices)
        {
            int startIndex = meshInfos.BorderVertexCount + meshInfos.ArcVertexCount;
            int halfVertices = meshInfos.FrontLineVertexCount / 2;
            for (int i = 0; i < halfVertices; i++)
            {
                int index = startIndex + i * 2;
                float2 offset = i * FrontStep;
                vertices[index] = (OuterFrontStart + offset).x0y();//Outer
                vertices[index + 1] = (InnerFrontStart + offset).x0y();//Inner
            }
        }
        
        private void SetArcVertices(NativeArray<float3> vertices)
        {
            int startIndex = meshInfos.BorderVertexCount;
            int halfVertices = meshInfos.ArcVertexCount / 2;
            float innerRange = Range - THICKNESS;
            for (int i = 0; i < halfVertices; i++)
            {
                //Left Outer
                float currentAngleInRadian = OuterArcStart - (i + 1) * OuterArcAngleStep;
                float2 outerDirection = new(math.cos(currentAngleInRadian), math.sin(currentAngleInRadian));
                float3 outerLeft = (OuterBorderStart + outerDirection * Range).x0y();
                vertices[startIndex + i * 2] = outerLeft;
                
                //Left Inner
                float innerAngleInRadian = InnerArcStart - (i + 1) * InnerArcAngleStep;
                float2 innerDirection = new(math.cos(innerAngleInRadian), math.sin(innerAngleInRadian));
                float3 innerLeft = (OuterBorderStart + innerDirection * innerRange).x0y();
                vertices[startIndex + i * 2 + 1] = innerLeft;
                
                //Right Outer
                int innerRightIndex = vertices.Length - (1 + i * 2) - meshInfos.BorderVertexCount;
                vertices[innerRightIndex] = innerLeft - 2 * math.project(innerLeft, math.right());
                
                //Right Inner
                int outerRightIndex = innerRightIndex - 1; // vertices.Length - (1 + i * 2) - MeshInfos.BorderVertexCount - 1;
                vertices[outerRightIndex] = outerLeft - 2 * math.project(outerLeft, math.right());
            }
        }

#region Raycast JobSystem
        public JobHandle RaycastHitsJob(NativeArray<RaycastHit> results, NativeArray<float3> positions, QueryParameters queryParams, JobHandle dependency = default)
        {
            NativeArray<RaycastCommand> commands = new (positions.Length, TempJob, UninitializedMemory);
            JRaycastsCommands job = new JRaycastsCommands
            {
                OriginHeight = ORIGIN_HEIGHT,
                RayDistance = RAY_DISTANCE,
                QueryParams = queryParams,
                Position = Position,
                Forward = Forward,
                Vertices = positions,
                Commands = commands
            };
            JobHandle rayCastCommandJh = job.ScheduleParallel(commands.Length, JobsUtility.JobWorkerCount - 1, dependency);
            JobHandle rayCastHitJh = RaycastCommand.ScheduleBatch(commands, results, 1, 1, rayCastCommandJh);
            commands.Dispose(rayCastHitJh);
            return rayCastHitJh;
        }
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                                ◆◆◆◆◆◆ JOBS ◆◆◆◆◆◆                                                  ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
        public struct JRaycastsCommands : IJobFor
        {
            [ReadOnly] public int OriginHeight;
            [ReadOnly] public int RayDistance;
            [ReadOnly] public QueryParameters QueryParams;
            
            [ReadOnly] public float3 Position;
            [ReadOnly] public float3 Forward;
            
            [ReadOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction] 
            public NativeArray<float3> Vertices;
            [WriteOnly, NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            public NativeArray<RaycastCommand> Commands;
            
            public void Execute(int index)
            {
                float2 origin2D = Vertices[index].xz + Position.xz;
                float angle     = math.acos(Forward.z);// acos(dot(forward().xz, transformForward.xz)); ( forward.xy = (0,1) )
                float signValue = math.sign(-Forward.x);// sign(forward().x * transformForward.y - forward().y * transformForward.x);
                float signedAngle = angle * signValue;
                math.sincos(signedAngle, out float sinA, out float cosA);
                
                //origin2D = float2(cosA * origin2D.x - sinA * origin2D.y, sinA * origin2D.x + cosA * origin2D.y);
                float2x2 rotationMatrix = new float2x2(cosA, -sinA, sinA,  cosA);
                origin2D = mul(rotationMatrix, origin2D);

                Vector3 origin3D = new Vector3(origin2D.x, OriginHeight, origin2D.y);
                Commands[index] = new RaycastCommand(origin3D, Vector3.down, QueryParams, RayDistance);
            }
        }
#endregion Raycast JobSystem
    }
    
    /*
    float3 transformForward = transform.forward;
    float3 transformPosition = transform.position;
    for (int i = 0; i < vertices.Length; i++)
    {
        float2 origin2D = vertices[i].xz + transformPosition.xz;
        float angle     = acos(transformForward.z);//acos(dot(forward().xz, transformForward.xz)); forward.xy = (0,1)
        float signValue = sign(-transformForward.x);//sign(forward().x * transformForward.y - forward().y * transformForward.x);
        float signedAngle = angle * signValue;
        math.sincos(signedAngle, out float sinA, out float cosA);

        //origin2D = float2(cosA * origin2D.x - sinA * origin2D.y, sinA * origin2D.x + cosA * origin2D.y);
        float2x2 rotationMatrix = new float2x2(cosA, -sinA, sinA,  cosA);
        origin2D = mul(rotationMatrix, origin2D);

        Ray ray = new (new Vector3(origin2D.x, heightOffset, origin2D.y), Vector3.down);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, 1 << terrainLayerIndex)) continue;
        vertices[i] = new float3(vertices[i].x, hit.point.y + groundOffset, vertices[i].z);
        //vertices[i].y = hit.point.y + groundOffset;
    }
    */
#if UNITY_EDITOR
    public partial class FovMeshBuilder2 : MonoBehaviour
    {
        public bool DebugIndicesNumber = false;
        
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !meshVertices.IsCreated || meshVertices.Length == 0) return;

            Span<Vector3> debugVertices = stackalloc Vector3[meshVertices.Length];
            transform.TransformPoints(meshVertices.Reinterpret<Vector3>().AsReadOnlySpan(), debugVertices);
            
            Gizmos.color = Color.magenta;
            Vector3 offset = Vector3.up + Vector3.right * 0.075f;
            for (int i = 0; i < meshVertices.Length; i++)
            {
                Gizmos.DrawWireSphere(debugVertices[i], 0.05f);
                if (!DebugIndicesNumber) continue;
                Handles.Label(debugVertices[i] + offset, $"{i}");
            }
        }
    }
}
#endif