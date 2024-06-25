using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

using static UnityEngine.Mesh;

using static UnityEngine.Rendering.VertexAttribute;
using static UnityEngine.Rendering.VertexAttributeFormat;
using static Unity.Collections.Allocator;
using static Unity.Collections.NativeArrayOptions;

namespace FieldOfView
{
    public static class Utils
    {
        public static readonly VertexAttributeDescriptor[] VertexAttributeDescriptors = new[]
        {
            new VertexAttributeDescriptor(Position, Float32, dimension: 3, stream: 0),
            new VertexAttributeDescriptor(Normal, Float32, dimension: 3, stream: 1),
            new VertexAttributeDescriptor(Tangent, Float16, dimension: 4, stream: 2),
            new VertexAttributeDescriptor(TexCoord0, Float16, dimension: 2, stream: 3),
        };
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetSubMesh(this Mesh mesh, int indicesCount, int verticesCount, MeshUpdateFlags flags = MeshUpdateFlags.Default)
        {
            SubMeshDescriptor descriptor = new(0, indicesCount) { vertexCount = verticesCount };
            mesh.SetSubMesh(0, descriptor, flags);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 x0y(this float2 value)
        {
            return new float3(value.x, 0, value.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SubMeshDescriptor WithVertices(this SubMeshDescriptor descriptor, int vertexCount)
        {
            descriptor.vertexCount = vertexCount;
            return descriptor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleRadian(float2 direction)
        {
            return math.atan2(direction.y, direction.x);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 NormalCw(this float2 direction)
        {
            return new float2(direction.y, -direction.x);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 NormalCCw(this float2 direction)
        {
            return new float2(-direction.y, direction.x);
        }
    }
}
