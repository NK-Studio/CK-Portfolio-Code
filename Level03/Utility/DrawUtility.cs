using System;
using UnityEngine;

namespace Utility
{
    using LineDrawer = Action<Vector3, Vector3>;
    public static class DrawUtility
    {

        public static Vector4 ToVector4(this in Vector3 v, float w) => new Vector4(v.x, v.y, v.z, w);
        
        public static void DrawMesh(this Mesh mesh, LineDrawer drawer, Transform transform)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            // var maxMeshRotator = Quaternion.Euler(-90f, 0f, 0f);
            var maxMeshRotator = Quaternion.identity;
            var matrix = transform.localToWorldMatrix;
            var count = mesh.triangles.Length / 3;
            for (int i = 0; i < count; i++)
            {
                var v0 = (Vector3)(matrix * (maxMeshRotator * vertices[triangles[i * 3 + 0]]).ToVector4(1f));
                var v1 = (Vector3)(matrix * (maxMeshRotator * vertices[triangles[i * 3 + 1]]).ToVector4(1f));
                var v2 = (Vector3)(matrix * (maxMeshRotator * vertices[triangles[i * 3 + 2]]).ToVector4(1f));
                drawer.Invoke(v0, v1);
                drawer.Invoke(v1, v2);
                drawer.Invoke(v2, v0);
            }
        }
        
        public static void DrawBox(this in Bounds b, LineDrawer drawer)
        {
            // bottom
            var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
            var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
            var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
            var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

            drawer(p1, p2);
            drawer(p2, p3);
            drawer(p3, p4);
            drawer(p4, p1);

            // top
            var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
            var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
            var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
            var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

            drawer(p5, p6);
            drawer(p6, p7);
            drawer(p7, p8);
            drawer(p8, p5);

            // sides
            drawer(p1, p5);
            drawer(p2, p6);
            drawer(p3, p7);
            drawer(p4, p8);
        }

        public static LineDrawer DebugXDrawer(Color color, float duration = 0f, bool depthTest = true) 
            => (a, b) => DebugX.DrawLine(a, b, color, duration, depthTest);
        public static LineDrawer DebugDrawer(Color color, float duration = 0f, bool depthTest = true) 
            => (a, b) => Debug.DrawLine(a, b, color, duration, depthTest);
        public static LineDrawer GizmosDrawer() 
            => Gizmos.DrawLine;
        public static void DrawCircle(in Vector3 center, float radius, Vector3 normal, int divide, LineDrawer drawer)
        {
            normal.Normalize();

            var rotator = Quaternion.AngleAxis(360f / divide, normal);

            var drawPoint = Vector3.Cross(normal, Vector3.up);

            if (drawPoint.sqrMagnitude <= Vector3.kEpsilon)
            {
                drawPoint = Vector3.right;
            }
            else
            {
                drawPoint.Normalize();
            }

            drawPoint *= radius;

            for (int i = 0; i < divide; i++)
            {
                var start = drawPoint;
                var end = rotator * drawPoint;
                drawer(center + start, center + end);
                drawPoint = end;
            }
        }

        public static void DrawWireSphere(in Vector3 center, float radius, int divide, LineDrawer drawer)
        {
            DrawCircle(center, radius, Vector3.up, divide, drawer);
            DrawCircle(center, radius, Vector3.forward, divide, drawer);
            DrawCircle(center, radius, Vector3.right, divide, drawer);
        }
        
    
        public struct OBB
        {
            public Vector3 Center;
            public Vector3[] Basis;
            public Vector3 HalfExtents;
        }

        public static void DrawOBB(in OBB obb, LineDrawer drawer)
        {
            Vector3[] corners = GetCorners(obb);

            // X-axis lines
            drawer(corners[0], corners[1]);
            drawer(corners[2], corners[3]);
            drawer(corners[4], corners[5]);
            drawer(corners[6], corners[7]);

            // Y-axis lines
            drawer(corners[0], corners[2]);
            drawer(corners[1], corners[3]);
            drawer(corners[4], corners[6]);
            drawer(corners[5], corners[7]);

            // Z-axis lines
            drawer(corners[0], corners[4]);
            drawer(corners[1], corners[5]);
            drawer(corners[2], corners[6]);
            drawer(corners[3], corners[7]);
        }
        
        public static Vector3[] GetCorners(in OBB obb)
        {
            Vector3[] corners = new Vector3[8];

            for (int i = 0; i < 8; i++)
            {
                corners[i] = obb.Center;

                corners[i] += obb.Basis[0] * (i % 2 == 0 ? obb.HalfExtents.x : -obb.HalfExtents.x);
                corners[i] += obb.Basis[1] * ((i / 2) % 2 == 0 ? obb.HalfExtents.y : -obb.HalfExtents.y);
                corners[i] += obb.Basis[2] * (i / 4 == 0 ? obb.HalfExtents.z : -obb.HalfExtents.z);
            }

            return corners;
        }
    }
}