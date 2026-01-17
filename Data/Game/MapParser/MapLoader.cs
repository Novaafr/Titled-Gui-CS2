using PhysExtractor.src;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Vector3 = System.Numerics.Vector3;

namespace Titled_Gui.Data.Game.MapParser
{
    public class MapLoader // https://github.com/AtomicBool/cs2-map-parser  THIS TOOK 40 MINS TO CONVERT FROM CPP TO C#
    {
        private string cs2BaseFolder = @"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\csgo\";
        private string mapsFolder = "maps";
        private string pathToTris = "Data\\Game\\MapParser\\PreExtractedMapData\\tri";
        private string pathToVphys = "Data\\Game\\MapParser\\PreExtractedMapData\\vphys";
        public string previousMapName = "";
        private const float EPSILON = 0.0000001f;

        #region STRUCTS
        public struct BoundingBox
        {
            public Vector3 min, max;

            public bool Intersect(Vector3 origin, Vector3 end)
            {
                Vector3 dir = end - origin;

                float invX = 1f / dir.X, invY = 1f / dir.Y, invZ = 1f / dir.Z;

                float t1 = (min.X - origin.X) * invX;
                float t2 = (max.X - origin.X) * invX;
                float t3 = (min.Y - origin.Y) * invY;
                float t4 = (max.Y - origin.Y) * invY;
                float t5 = (min.Z - origin.Z) * invZ;
                float t6 = (max.Z - origin.Z) * invZ;

                float tmin = Math.Max(Math.Max(Math.Min(t1, t2), Math.Min(t3, t4)), Math.Min(t5, t6));
                float tmax = Math.Min(Math.Min(Math.Max(t1, t2), Math.Max(t3, t4)), Math.Max(t5, t6));

                return tmax >= 0 && tmin <= tmax;
            }
        };
        public struct Triangle
        {
            public Vector3 p1, p2, p3;

            public bool Intercect(Vector3 origin, Vector3 end)
            {
                Vector3 edge1, edge2, h, s, q;
                float a, f, u, v, t;
                edge1 = p2 - p1;
                edge2 = p3 - p1;
                h = CrossProduct(end - origin, edge2);
                a = Dot(edge1, h);
                if (a > -EPSILON && a < EPSILON)
                    return false;

                f = 1f / a;
                s = origin - p1;
                u = f * Dot(s, h);

                if (u < 0f || u > 1f)
                    return false;

                q = CrossProduct(s, edge1);
                v = f * Dot((end - origin), q);

                if (v < 0f || u + v > 1f)
                    return false;

                t = f * Dot(edge2, q);

                if (t > EPSILON && t < 1f)
                    return true;

                return false;
            }
        }
        public class KDNode
        {
            public BoundingBox bbox;
            public Triangle[] triangles = Array.Empty<Triangle>();
            public KDNode? left = null;
            public KDNode? right = null;
            public int axis;

            static void DeleteKDTree(KDNode? node)
            {
                if (node == null) return;

                DeleteKDTree(node?.left);
                DeleteKDTree(node?.right);

                node?.left = null;
                node?.right = null;
            }
        }
        #endregion STRUCTS
        #region Misc Helpers
        public bool RayIntersectsKDTree(KDNode? node, Vector3 origin, Vector3 end)
        {
            if (node == null) return false;

            if (!node.bbox.Intersect(origin, end))
                return false;

            if (node.triangles.Length > 0)
            {
                foreach (Triangle tri in node.triangles)
                    if (tri.Intercect(origin, end))
                        return true;
            }

            bool hit_left = RayIntersectsKDTree(node?.left, origin, end);
            bool hit_right = RayIntersectsKDTree(node?.right, origin, end);

            return hit_left || hit_right;
        }
        static BoundingBox CalculateBoundingBox(List<Triangle> triangles)
        {
            BoundingBox box;

            box.min = box.max = triangles[0].p1;

            foreach (var tri in triangles)
            {
                foreach (var p in new[] { tri.p1, tri.p2, tri.p3 })
                {
                    box.min.X = Math.Min(box.min.X, p.X);
                    box.min.Y = Math.Min(box.min.Y, p.Y);
                    box.min.Z = Math.Min(box.min.Z, p.Z);
                    box.max.X = Math.Max(box.max.X, p.X);
                    box.max.Y = Math.Max(box.max.Y, p.Y);
                    box.max.Z = Math.Max(box.max.Z, p.Z);
                }
            }
            return box;
        }
        KDNode? BuildKDTree(List<Triangle> triangles, int depth = 0)
        {
            if (triangles.Count <= 0) return null;

            KDNode node = new();

            node.bbox = CalculateBoundingBox(triangles);
            node.axis = depth % 3;

            if (triangles.Count <= 3)
            {
                node.triangles = [.. triangles];
                return node;
            }
            int axis = node.axis;

            int comparator(Triangle a, Triangle b)
            {
                float a_center = 0f;
                float b_center = 0f;

                switch (axis)
                {
                    case 0:
                        a_center = (a.p1.X + a.p2.X + a.p3.X) / 3f;
                        b_center = (b.p1.X + b.p2.X + b.p3.X) / 3f;
                        break;

                    case 1:
                        a_center = (a.p1.Y + a.p2.Y + a.p3.Y) / 3f;
                        b_center = (b.p1.Y + b.p2.Y + b.p3.Y) / 3f;
                        break;

                    case 2:
                        a_center = (a.p1.Z + a.p2.Z + a.p3.Z) / 3f;
                        b_center = (b.p1.Z + b.p2.Z + b.p3.Z) / 3f;
                        break;
                }

                return a_center.CompareTo(b_center);
            }

            int mid = triangles.Count / 2;
            NthElement(triangles, mid, comparator);

            List<Triangle> leftTriangles = triangles.GetRange(0, mid);
            List<Triangle> rightTriangles = triangles.GetRange(mid, triangles.Count - mid);

            node.left = BuildKDTree(leftTriangles, depth + 1);
            node.right = BuildKDTree(rightTriangles, depth + 1);
            return node;
        }
        #endregion
        #region STD
        public static void NthElement<T>(List<T> list, int n, Comparison<T> comp)
        {
            int left = 0;
            int right = list.Count - 1;

            while (true)
            {
                if (left == right) return;

                int pivotIndex = Partition(list, left, right, (left + right) / 2, comp);

                if (n == pivotIndex)
                    return;
                else if (n < pivotIndex)
                    right = pivotIndex - 1;
                else
                    left = pivotIndex + 1;
            }
        }

        private static int Partition<T>(List<T> list, int left, int right, int pivotIndex, Comparison<T> comp)
        {
            T pivotValue = list[pivotIndex];
            Swap(list, pivotIndex, right);

            int storeIndex = left;

            for (int i = left; i < right; i++)
            {
                if (comp(list[i], pivotValue) < 0)
                {
                    Swap(list, storeIndex, i);
                    storeIndex++;
                }
            }

            Swap(list, right, storeIndex);
            return storeIndex;
        }

        private static void Swap<T>(List<T> list, int i, int j)
        {
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }


        #endregion STD
        #region Vector Helpers
        public static float Dot(Vector3 inVec, Vector3 vOther) {
            return (inVec.X * vOther.X) + (inVec.Y * vOther.Y) + (inVec.Z * vOther.Z);
        }
        public static Vector3 CrossProduct(Vector3 a, Vector3 b)
        {
            return new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        }
        #endregion
        public List<Triangle> triangles = new List<Triangle>();

        public bool LoadMap(string mapName)
        {

            string filePath = Path.Combine(cs2BaseFolder, mapsFolder, mapName + ".vpk");
            string triFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pathToTris, mapName + ".tri");
            if (!File.Exists(filePath) || !File.Exists(triFilePath))
            {
                Console.WriteLine("Failed To Find Current Map And Or The Tri File. filePath: " + filePath + " " + "triFilePath: " + triFilePath);
                return false;
            }

            if (!LoadTri(triFilePath))
            {
                Console.WriteLine("Failed to load .tri: " + triFilePath);
                return false;
            }
            previousMapName = mapName;
            return true;
        }
        private static KDNode? KDTreeRoot;
        public bool LoadTri(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                var sw = Stopwatch.StartNew();

                byte[] buffer = File.ReadAllBytes(filePath);

                int triSize = Marshal.SizeOf<Triangle>();
                int numElements = buffer.Length / triSize;

                triangles = new List<Triangle>(numElements);

                unsafe
                {
                    fixed (byte* p = buffer)
                    {
                        Triangle* pTri = (Triangle*)p;

                        for (int i = 0; i < numElements; i++)
                            triangles.Add(pTri[i]);
                    }
                }

                KDTreeRoot = BuildKDTree(triangles);

                triangles.Clear();
                triangles.TrimExcess();

                sw.Stop();
                Console.WriteLine($"Loaded {filePath} in {sw.ElapsedMilliseconds}ms");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadTri exception: " + ex);
                return false;
            }
        }
        public bool IsVisible(Vector3 origin, Vector3 end)
        {
            return !RayIntersectsKDTree(KDTreeRoot, origin, end);
        }
    }
}
