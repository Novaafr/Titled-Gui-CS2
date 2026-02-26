using System.Diagnostics;
using System.Runtime.InteropServices;
using static Titled_Gui.Data.Game.VRF.Types;
using Vector3 = System.Numerics.Vector3;

namespace Titled_Gui.Data.Game.MapParser
{
    public class MapLoader // https://github.com/AtomicBool/cs2-map-parser  THIS TOOK 40 MINS TO CONVERT FROM CPP TO C#
    {
        private readonly string cs2BaseFolder = @"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\csgo\";
        private readonly string _mapsFolder = "maps";
        private readonly string _pathToTris = Path.Combine(AppContext.BaseDirectory, "Data", "Game", "MapParser", "PreExtractedMapData" , "tri");
        private readonly string _pathToVphys = Path.Combine(AppContext.BaseDirectory, "Data", "Game", "MapParser", "PreExtractedMapData", "vphys");
        public string PreviousMapName = "";

        #region Misc Helpers
        public bool RayIntersectsKDTree(KDNode? node, Vector3 origin, Vector3 end)
        {
            if (node == null) return false;

            if (!node.bbox.Intersect(origin, end))
                return false;

            if (node.Triangles.Length > 0)
            {
                foreach (Triangle tri in node.Triangles)
                    if (tri.Intercect(origin, end))
                        return true;
            }

            bool hitLeft = RayIntersectsKDTree(node?.Left, origin, end);
            bool hitRight = RayIntersectsKDTree(node?.Right, origin, end);

            return hitLeft || hitRight;
        }
        private static BoundingBox CalculateBoundingBox(List<Triangle> triangles)
        {
            BoundingBox box;

            box.Min = box.Max = triangles[0].Point1;

            foreach (var tri in triangles)
            {
                foreach (var p in new[] { tri.Point1, tri.Point2, tri.Point3 })
                {
                    box.Min.X = Math.Min(box.Min.X, p.X);
                    box.Min.Y = Math.Min(box.Min.Y, p.Y);
                    box.Min.Z = Math.Min(box.Min.Z, p.Z);
                    box.Max.X = Math.Max(box.Max.X, p.X);
                    box.Max.Y = Math.Max(box.Max.Y, p.Y);
                    box.Max.Z = Math.Max(box.Max.Z, p.Z);
                }
            }
            return box;
        }
        KDNode? BuildKDTree(List<Triangle> triangles, int depth = 0)
        {
            if (triangles.Count <= 0) return null;

            KDNode node = new();

            node.bbox = CalculateBoundingBox(triangles);
            node.Axis = depth % 3;

            if (triangles.Count <= 3)
            {
                node.Triangles = [.. triangles];
                return node;
            }
            int axis = node.Axis;

            int Comparator(Triangle a, Triangle b)
            {
                float aCenter = 0f;
                float bCenter = 0f;

                switch (axis)
                {
                    case 0:
                        aCenter = (a.Point1.X + a.Point2.X + a.Point3.X) / 3f;
                        bCenter = (b.Point1.X + b.Point2.X + b.Point3.X) / 3f;
                        break;

                    case 1:
                        aCenter = (a.Point1.Y + a.Point2.Y + a.Point3.Y) / 3f;
                        bCenter = (b.Point1.Y + b.Point2.Y + b.Point3.Y) / 3f;
                        break;

                    case 2:
                        aCenter = (a.Point1.Z + a.Point2.Z + a.Point3.Z) / 3f;
                        bCenter = (b.Point1.Z + b.Point2.Z + b.Point3.Z) / 3f;
                        break;
                }

                return aCenter.CompareTo(bCenter);
            }

            int mid = triangles.Count / 2;
            NthElement(triangles, mid, Comparator);

            List<Triangle> leftTriangles = triangles.GetRange(0, mid);
            List<Triangle> rightTriangles = triangles.GetRange(mid, triangles.Count - mid);

            node.Left = BuildKDTree(leftTriangles, depth + 1);
            node.Right = BuildKDTree(rightTriangles, depth + 1);
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
        public List<Triangle> Triangles = new List<Triangle>();

        public bool LoadMap(string mapName)
        {
            string filePath = Path.Combine(cs2BaseFolder, _mapsFolder, mapName + ".vpk");
            string triFilePath = Path.Combine(_pathToTris, mapName + ".tri");
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
            PreviousMapName = mapName;
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

                Triangles = new List<Triangle>(numElements);

                unsafe
                {
                    fixed (byte* p = buffer)
                    {
                        Triangle* pTri = (Triangle*)p;

                        for (int i = 0; i < numElements; i++)
                            Triangles.Add(pTri[i]);
                    }
                }

                KDTreeRoot = BuildKDTree(Triangles);

                Triangles.Clear();
                Triangles.TrimExcess();

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
