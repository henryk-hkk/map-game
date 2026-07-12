using HelixToolkit.Geometry;
using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Core;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MapGame.Core.Utils.Graphic
{
    public static class LakeMeshGenerator
    {
        public static HelixToolkit.SharpDX.MeshGeometry3D GenerateLakeMesh(bool[] lakeMask, byte[] heightmap, bool flatWater = true)
        {
            int width = MapContext.Width;
            int height = MapContext.Height;

            var builder = new MeshBuilder(generateNormals: true, generateTexCoords: true);

            int smoothIterations = 15;
            Vector2[,] smoothedVerts = ComputeSmoothedVertices(lakeMask, width, height, smoothIterations);

            if (flatWater)
            {
                List<List<int>> lakes = FindLakes(lakeMask, width, height);

                foreach (var lake in lakes)
                {
                    float waterLevel = CalculateWaterLevel(lake, heightmap, width);

                    foreach (int idx in lake)
                    {
                        int x = idx % width;
                        int y = idx / width;

                        if (x < width - 1 && y < height - 1)
                        {
                            Vector2 v1 = smoothedVerts[x, y];
                            Vector2 v2 = smoothedVerts[x + 1, y];
                            Vector2 v3 = smoothedVerts[x + 1, y + 1];
                            Vector2 v4 = smoothedVerts[x, y + 1];

                            AddQuad(builder, v1, v2, v3, v4, waterLevel, waterLevel, waterLevel, waterLevel);
                        }
                    }
                }
            }
            else
            {
                for (int y = 0; y < height - 1; y++)
                {
                    for (int x = 0; x < width - 1; x++)
                    {
                        int idx = y * width + x;
                        if (lakeMask[idx])
                        {
                            Vector2 v1 = smoothedVerts[x, y];
                            Vector2 v2 = smoothedVerts[x + 1, y];
                            Vector2 v3 = smoothedVerts[x + 1, y + 1];
                            Vector2 v4 = smoothedVerts[x, y + 1];

                            float zOffset = 0.2f;
                            float h1 = (float)TerrainMeshGenerator.GetTerrainHeight(heightmap, x, y, width) + zOffset;
                            float h2 = (float)TerrainMeshGenerator.GetTerrainHeight(heightmap, x + 1, y, width) + zOffset;
                            float h3 = (float)TerrainMeshGenerator.GetTerrainHeight(heightmap, x + 1, y + 1, width) + zOffset;
                            float h4 = (float)TerrainMeshGenerator.GetTerrainHeight(heightmap, x, y + 1, width) + zOffset;

                            AddQuad(builder, v1, v2, v3, v4, h1, h2, h3, h4);
                        }
                    }
                }
            }

            builder.ComputeNormalsAndTangents(MeshFaces.Default);
            return builder.ToMeshGeometry3D();
        }

        private static Vector2[,] ComputeSmoothedVertices(bool[] lakeMask, int width, int height, int iterations)
        {
            Vector2[,] verts = new Vector2[width + 1, height + 1];

            for (int y = 0; y <= height; y++)
            {
                for (int x = 0; x <= width; x++)
                {
                    verts[x, y] = new Vector2(x, y);
                }
            }

            // Active Contours
            for (int it = 0; it < iterations; it++)
            {
                Vector2[,] nextVerts = (Vector2[,])verts.Clone();

                for (int y = 0; y <= height; y++)
                {
                    for (int x = 0; x <= width; x++)
                    {
                        bool wTopLeft = (x > 0 && y > 0) && lakeMask[(y - 1) * width + (x - 1)];
                        bool wTopRight = (x < width && y > 0) && lakeMask[(y - 1) * width + x];
                        bool wBotLeft = (x > 0 && y < height) && lakeMask[y * width + (x - 1)];
                        bool wBotRight = (x < width && y < height) && lakeMask[y * width + x];

                        int waterCount = (wTopLeft ? 1 : 0) + (wTopRight ? 1 : 0) + (wBotLeft ? 1 : 0) + (wBotRight ? 1 : 0);

                        if (waterCount == 0) continue;

                        if (waterCount > 0 && waterCount < 4)
                        {
                            Vector2 sum = Vector2.Zero;
                            int bCount = 0;

                            if (x > 0 && (wTopLeft != wBotLeft)) { sum += verts[x - 1, y]; bCount++; }
                            if (x < width && (wTopRight != wBotRight)) { sum += verts[x + 1, y]; bCount++; }
                            if (y > 0 && (wTopLeft != wTopRight)) { sum += verts[x, y - 1]; bCount++; }
                            if (y < height && (wBotLeft != wBotRight)) { sum += verts[x, y + 1]; bCount++; }

                            if (bCount > 0)
                            {
                                Vector2 target = sum / bCount;
                                Vector2 newPos = Vector2.Lerp(verts[x, y], target, 0.6f);

                                Vector2 origin = new Vector2(x, y);
                                Vector2 diff = newPos - origin;
                                if (diff.LengthSquared() > 0.8f * 0.8f)
                                {
                                    newPos = origin + Vector2.Normalize(diff) * 0.8f;
                                }

                                nextVerts[x, y] = newPos;
                            }
                        }
                        else if (waterCount == 4)
                        {
                            Vector2 sum = Vector2.Zero;
                            int iCount = 0;
                            if (x > 0) { sum += verts[x - 1, y]; iCount++; }
                            if (x < width) { sum += verts[x + 1, y]; iCount++; }
                            if (y > 0) { sum += verts[x, y - 1]; iCount++; }
                            if (y < height) { sum += verts[x, y + 1]; iCount++; }

                            if (iCount > 0)
                            {
                                Vector2 target = sum / iCount;
                                nextVerts[x, y] = Vector2.Lerp(verts[x, y], target, 0.6f);
                            }
                        }
                    }
                }
                verts = nextVerts;
            }

            return verts;
        }

        private static List<List<int>> FindLakes(bool[] lakeMask, int width, int height)
        {
            List<List<int>> lakes = new List<List<int>>();
            bool[] visited = new bool[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;

                    if (lakeMask[idx] && !visited[idx])
                    {
                        List<int> currentLake = new List<int>();
                        Queue<int> queue = new Queue<int>();

                        queue.Enqueue(idx);
                        visited[idx] = true;

                        while (queue.Count > 0)
                        {
                            int currentIdx = queue.Dequeue();
                            currentLake.Add(currentIdx);

                            int cx = currentIdx % width;
                            int cy = currentIdx / width;

                            int[] dx = { 1, -1, 0, 0 };
                            int[] dy = { 0, 0, 1, -1 };

                            for (int i = 0; i < 4; i++)
                            {
                                int nx = cx + dx[i];
                                int ny = cy + dy[i];

                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    int nIdx = ny * width + nx;
                                    if (lakeMask[nIdx] && !visited[nIdx])
                                    {
                                        visited[nIdx] = true;
                                        queue.Enqueue(nIdx);
                                    }
                                }
                            }
                        }
                        lakes.Add(currentLake);
                    }
                }
            }

            return lakes;
        }

        private static float CalculateWaterLevel(List<int> lake, byte[] heightmap, int width)
        {
            double maxH = double.MinValue;

            foreach (int idx in lake)
            {
                int x = idx % width;
                int y = idx / width;

                double h = TerrainMeshGenerator.GetTerrainHeight(heightmap, x, y, width);
                if (h > maxH) maxH = h;
            }

            return (float)maxH + 0.2f;
        }

        private static void AddQuad(MeshBuilder builder, Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4, float h1, float h2, float h3, float h4)
        {
            Vector3 p1 = new Vector3(v1.X, h1, v1.Y);
            Vector3 p2 = new Vector3(v2.X, h2, v2.Y);
            Vector3 p3 = new Vector3(v3.X, h3, v3.Y);
            Vector3 p4 = new Vector3(v4.X, h4, v4.Y);

            int currentIndex = builder.Positions.Count;

            builder.Positions.Add(p1);
            builder.Positions.Add(p2);
            builder.Positions.Add(p3);
            builder.Positions.Add(p4);

            Vector3 upNormal = new Vector3(0, 1, 0);
            builder.Normals.Add(upNormal);
            builder.Normals.Add(upNormal);
            builder.Normals.Add(upNormal);
            builder.Normals.Add(upNormal);

            float uvScale = 0.1f;
            builder.TextureCoordinates.Add(new Vector2(v1.X * uvScale, v1.Y * uvScale));
            builder.TextureCoordinates.Add(new Vector2(v2.X * uvScale, v2.Y * uvScale));
            builder.TextureCoordinates.Add(new Vector2(v3.X * uvScale, v3.Y * uvScale));
            builder.TextureCoordinates.Add(new Vector2(v4.X * uvScale, v4.Y * uvScale));

            builder.TriangleIndices.Add(currentIndex);
            builder.TriangleIndices.Add(currentIndex + 2);
            builder.TriangleIndices.Add(currentIndex + 1);

            builder.TriangleIndices.Add(currentIndex);
            builder.TriangleIndices.Add(currentIndex + 3);
            builder.TriangleIndices.Add(currentIndex + 2);
        }
    }
}