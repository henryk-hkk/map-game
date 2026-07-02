using HelixToolkit.Geometry;
using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Constants;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MapGame.Core.Utils.Graphic
{
    public static class RiverMeshGenerator
    {
        public static HelixToolkit.SharpDX.MeshGeometry3D GenerateRiverMesh(bool[] riverMask, byte[] heightmap)
        {
            int width = Map.Width;
            int height = Map.Height;

            var thinnedMask = GetThinnedMask(riverMask, width, height);

            List<List<Vector2>> rawTracedRivers = RiverPathTracer.TraceAllRivers(thinnedMask, width, height);
            List<List<Vector2>> stitchedRivers = StitchBrokenPaths(rawTracedRivers, 4.0f);

            SnapTributaries(stitchedRivers, 4.0f);

            var builder = new MeshBuilder(generateNormals: true, generateTexCoords: true);
            float riverWidth = 3.0f;

            foreach (List<Vector2> rawRiverPath in stitchedRivers)
            {
                if (rawRiverPath.Count < 4) continue;

                Vector2 punktStartowy = rawRiverPath[0];
                Vector2 punktKoncowy = rawRiverPath[rawRiverPath.Count - 1];

                double wysokoscStartu = TerrainMeshGenerator.GetTerrainHeight(heightmap, (int)punktStartowy.X, (int)punktStartowy.Y, width, height);
                double wysokoscKonca = TerrainMeshGenerator.GetTerrainHeight(heightmap, (int)punktKoncowy.X, (int)punktKoncowy.Y, width, height);

                if (wysokoscStartu < wysokoscKonca)
                {
                    rawRiverPath.Reverse();
                }

                List<Vector2> riverPath = SmoothRiverPath(rawRiverPath, 5);

                for (int i = 0; i < riverPath.Count; i++)
                {
                    Vector2 pCurrent = riverPath[i];

                    int lookSpan = 3;
                    int indexAhead = Math.Min(i + lookSpan, riverPath.Count - 1);
                    int indexBehind = Math.Max(i - lookSpan, 0);

                    Vector2 direction = riverPath[indexAhead] - riverPath[indexBehind];

                    if (direction.LengthSquared() == 0 && i < riverPath.Count - 1) direction = riverPath[i + 1] - pCurrent;
                    if (direction.LengthSquared() == 0) direction = new Vector2(1, 0);

                    direction = Vector2.Normalize(direction);
                    Vector2 perpendicular = new Vector2(-direction.Y, direction.X) * (riverWidth / 2.0f);

                    Vector2 leftBank = pCurrent + perpendicular;
                    Vector2 rightBank = pCurrent - perpendicular;

                    int safeX = Math.Max(0, Math.Min((int)pCurrent.X, width - 1));
                    int safeY = Math.Max(0, Math.Min((int)pCurrent.Y, height - 1));

                    float h = (float)TerrainMeshGenerator.GetTerrainHeight(heightmap, safeX, safeY, width, height);

                    int currentIndex = builder.Positions.Count;

                    builder.Positions.Add(new Vector3(leftBank.X, h, leftBank.Y));
                    builder.Positions.Add(new Vector3(rightBank.X, h, rightBank.Y));

                    builder.TextureCoordinates.Add(new Vector2(0, (float)i / 10.0f));
                    builder.TextureCoordinates.Add(new Vector2(1, (float)i / 10.0f));

                    builder.Normals.Add(new System.Numerics.Vector3(0, 1, 0));
                    builder.Normals.Add(new System.Numerics.Vector3(0, 1, 0));

                    if (i > 0)
                    {
                        builder.TriangleIndices.Add(currentIndex - 2);
                        builder.TriangleIndices.Add(currentIndex - 1);
                        builder.TriangleIndices.Add(currentIndex);

                        builder.TriangleIndices.Add(currentIndex);
                        builder.TriangleIndices.Add(currentIndex - 1);
                        builder.TriangleIndices.Add(currentIndex + 1);
                    }
                }
            }

            builder.ComputeNormalsAndTangents(MeshFaces.Default);
            return builder.ToMeshGeometry3D();
        }

        private static List<Vector2> SmoothRiverPath(List<Vector2> path, int iterations)
        {
            if (path.Count < 3) return path;
            List<Vector2> smoothed = new List<Vector2>(path);

            for (int it = 0; it < iterations; it++)
            {
                List<Vector2> temp = new List<Vector2>();
                temp.Add(smoothed[0]);

                for (int i = 1; i < smoothed.Count - 1; i++)
                {
                    float nx = (smoothed[i - 1].X + smoothed[i].X + smoothed[i + 1].X) / 3.0f;
                    float ny = (smoothed[i - 1].Y + smoothed[i].Y + smoothed[i + 1].Y) / 3.0f;
                    temp.Add(new Vector2(nx, ny));
                }

                temp.Add(smoothed[smoothed.Count - 1]);
                smoothed = temp;
            }
            return smoothed;
        }

        private static List<List<Vector2>> StitchBrokenPaths(List<List<Vector2>> paths, float maxDistance)
        {
            List<List<Vector2>> currentPaths = new List<List<Vector2>>(paths);

            for (int i = 0; i < currentPaths.Count; i++)
            {
                var pathA = currentPaths[i];
                if (pathA.Count == 0) continue;

                bool mergeFound = true;
                while (mergeFound)
                {
                    mergeFound = false;
                    for (int j = 0; j < currentPaths.Count; j++)
                    {
                        if (i == j) continue;

                        var pathB = currentPaths[j];
                        if (pathB.Count == 0) continue;

                        Vector2 aStart = pathA[0];
                        Vector2 aEnd = pathA[pathA.Count - 1];
                        Vector2 bStart = pathB[0];
                        Vector2 bEnd = pathB[pathB.Count - 1];

                        bool connected = false;

                        if (Vector2.Distance(aEnd, bStart) <= maxDistance)
                        {
                            pathA.AddRange(pathB);
                            connected = true;
                        }
                        else if (Vector2.Distance(aEnd, bEnd) <= maxDistance)
                        {
                            pathB.Reverse();
                            pathA.AddRange(pathB);
                            connected = true;
                        }
                        else if (Vector2.Distance(aStart, bEnd) <= maxDistance)
                        {
                            pathB.AddRange(pathA);
                            pathA.Clear();
                            pathA.AddRange(pathB);
                            connected = true;
                        }
                        else if (Vector2.Distance(aStart, bStart) <= maxDistance)
                        {
                            pathA.Reverse();
                            pathA.AddRange(pathB);
                            connected = true;
                        }

                        if (connected)
                        {
                            currentPaths.RemoveAt(j);
                            if (j < i) i--;
                            mergeFound = true;
                            break;
                        }
                    }
                }
            }

            currentPaths.RemoveAll(p => p.Count == 0);
            return currentPaths;
        }

        private static void SnapTributaries(List<List<Vector2>> paths, float maxDistance)
        {
            foreach (var path in paths)
            {
                if (path.Count == 0) continue;

                Vector2 start = path[0];
                Vector2 end = path[path.Count - 1];

                Vector2? bestStartSnap = null;
                float bestStartDist = maxDistance;

                Vector2? bestEndSnap = null;
                float bestEndDist = maxDistance;

                foreach (var otherPath in paths)
                {
                    if (path == otherPath) continue;

                    foreach (var p in otherPath)
                    {
                        float dStart = Vector2.Distance(start, p);
                        if (dStart < bestStartDist)
                        {
                            bestStartDist = dStart;
                            bestStartSnap = p;
                        }

                        float dEnd = Vector2.Distance(end, p);
                        if (dEnd < bestEndDist)
                        {
                            bestEndDist = dEnd;
                            bestEndSnap = p;
                        }
                    }
                }

                if (bestStartSnap.HasValue && bestStartDist > 0.1f) path.Insert(0, bestStartSnap.Value);
                if (bestEndSnap.HasValue && bestEndDist > 0.1f) path.Add(bestEndSnap.Value);
            }
        }

        private static bool[] GetThinnedMask(bool[] mask, int width, int height)
        {// Zhang Suen Thinning
            bool[] grid = (bool[])mask.Clone();
            bool changed = true;

            while (changed)
            {
                changed = false;
                for (int step = 0; step < 2; step++)
                {
                    List<int> pixelsToRemove = new List<int>();

                    for (int y = 1; y < height - 1; y++)
                    {
                        for (int x = 1; x < width - 1; x++)
                        {
                            int idx = y * width + x;
                            if (!grid[idx]) continue;

                            int p2 = grid[idx - width] ? 1 : 0;
                            int p3 = grid[idx - width + 1] ? 1 : 0;
                            int p4 = grid[idx + 1] ? 1 : 0;
                            int p5 = grid[idx + width + 1] ? 1 : 0;
                            int p6 = grid[idx + width] ? 1 : 0;
                            int p7 = grid[idx + width - 1] ? 1 : 0;
                            int p8 = grid[idx - 1] ? 1 : 0;
                            int p9 = grid[idx - width - 1] ? 1 : 0;

                            int bp = p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9;
                            if (bp < 2 || bp > 6) continue;

                            int ap = 0;
                            if (p2 == 0 && p3 == 1) ap++;
                            if (p3 == 0 && p4 == 1) ap++;
                            if (p4 == 0 && p5 == 1) ap++;
                            if (p5 == 0 && p6 == 1) ap++;
                            if (p6 == 0 && p7 == 1) ap++;
                            if (p7 == 0 && p8 == 1) ap++;
                            if (p8 == 0 && p9 == 1) ap++;
                            if (p9 == 0 && p2 == 1) ap++;

                            if (ap != 1) continue;

                            if (step == 0)
                            {
                                if (p2 * p4 * p6 != 0) continue;
                                if (p4 * p6 * p8 != 0) continue;
                            }
                            else
                            {
                                if (p2 * p4 * p8 != 0) continue;
                                if (p2 * p6 * p8 != 0) continue;
                            }

                            pixelsToRemove.Add(idx);
                        }
                    }

                    foreach (int idx in pixelsToRemove)
                    {
                        grid[idx] = false;
                        changed = true;
                    }
                }
            }
            return grid;
        }
    }
}