using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

using static UnityEngine.Screen;

namespace Kaizerwald.RTTCamera
{
    public static class UGuiUtils
    {
        private static readonly Color DefaultBackGround    = new Color(0.5f, 1f, 0.4f, 0.2f);
        private static readonly Color DefaultBorder        = new Color(0.5f, 1f, 0.4f);
        
        private static readonly Color DefaultUiColor       = new Color(0.8f,0.8f,0.95f,0.25f);
        private static readonly Color DefaultUiBorderColor = new Color(0.8f, 0.8f, 0.95f);
        
        private static Texture2D whiteTexture;
        
        public static Texture2D WhiteTexture
        {
            get
            {
                if (whiteTexture == null)
                {
                    whiteTexture = new Texture2D(1, 1);
                    whiteTexture.SetPixel(0, 0, Color.white);
                    whiteTexture.Apply();
                }
                return whiteTexture;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Texture2D GetWhiteTexture()
        {
            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
            return whiteTexture;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawFullScreenRect(Rect rect, float thickness, Color? baseColor = null, Color? border = null)
        {
            DrawScreenRect(rect, baseColor ?? DefaultUiColor);
            DrawScreenRectBorder(rect, thickness, border ?? DefaultUiBorderColor);
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawScreenRect(Rect rect, Color color = default)
        {
            GUI.color = color == default ? DefaultBackGround : color;
            GUI.DrawTexture(rect, WhiteTexture);
            GUI.color = Color.white;
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawScreenRectBorder(Rect rect, float thickness, Color color = default)
        {
            color = color == default ? DefaultBorder : color;
            // Top
            DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            // Left
            DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            // Right
            DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
            // Bottom
            DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect GetScreenRect(Vector2 startPoint, Vector2 endPoint)
        {
            // Careful, 0,0 is at BOTTOM-LEFT (not top left as usual..)
            // Move origin from bottom left to top left
            startPoint.y = height - startPoint.y;
            endPoint.y = height - endPoint.y;
            // Calculate corners
            Vector2 topLeftCorner = Vector2.Min(startPoint, endPoint);
            Vector2 bottomRightCorner = Vector2.Max(startPoint, endPoint);
            return Rect.MinMaxRect(topLeftCorner.x, topLeftCorner.y, bottomRightCorner.x, bottomRightCorner.y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bounds GetViewportBounds(this Camera camera, Vector3 startPoint, Vector3 endPoint)
        {
            Vector3 start = camera.ScreenToViewportPoint(startPoint);
            Vector3 end = camera.ScreenToViewportPoint(endPoint);
            Vector3 min = Vector3.Min(start, end);
            Vector3 max = Vector3.Max(start, end);
            min.z = camera.nearClipPlane;
            max.z = camera.farClipPlane;
            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetViewportBounds(this ref Bounds bounds, Camera camera, Vector3 screenPosition1, Vector3 screenPosition2)
        {
            Vector3 v1 = camera.ScreenToViewportPoint(screenPosition1);
            Vector3 v2 = camera.ScreenToViewportPoint(screenPosition2);
            Vector3 min = Vector3.Min(v1, v2);
            Vector3 max = Vector3.Max(v1, v2);
            min.z = camera.nearClipPlane;
            max.z = camera.farClipPlane;
            bounds.SetMinMax(min, max);
        }
    }
}
