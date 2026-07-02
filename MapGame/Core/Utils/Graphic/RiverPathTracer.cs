using System;
using System.Collections.Generic;
using System.Numerics;

namespace MapGame.Core.Utils.Graphic
{
    public static class RiverPathTracer
    {
        private static readonly int[] Dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private static readonly int[] Dy = { -1, -1, 0, 1, 1, 1, 0, -1 };

        public static List<List<Vector2>> TraceAllRivers(bool[] thinnedMask, int width, int height)
        {
            List<List<Vector2>> allRivers = new List<List<Vector2>>();

            bool[] visited = new bool[thinnedMask.Length];
            Array.Copy(thinnedMask, visited, thinnedMask.Length);

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int idx = y * width + x;
                    if (!visited[idx]) continue;

                    int neighborCount = CountNeighbors(visited, x, y, width);

                    if (neighborCount == 1)
                    {
                        List<Vector2> currentRiverPath = TraceSingleRiver(visited, thinnedMask, x, y, width, height);
                        if (currentRiverPath.Count > 1)
                        {
                            allRivers.Add(currentRiverPath);
                        }
                    }
                }
            }

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int idx = y * width + x;
                    if (visited[idx])
                    {
                        List<Vector2> currentRiverPath = TraceSingleRiver(visited, thinnedMask, x, y, width, height);
                        if (currentRiverPath.Count > 1) allRivers.Add(currentRiverPath);
                    }
                }
            }

            return allRivers;
        }

        private static List<Vector2> TraceSingleRiver(bool[] visited, bool[] originalMask, int startX, int startY, int width, int height)
        {
            List<Vector2> path = new List<Vector2>();
            int cx = startX;
            int cy = startY;
            bool pathContinues = true;

            while (pathContinues)
            {
                path.Add(new Vector2(cx, cy));
                visited[cy * width + cx] = false;

                int nextX = -1;
                int nextY = -1;
                pathContinues = false;

                for (int i = 0; i < 8; i++)
                {
                    int nx = cx + Dx[i];
                    int ny = cy + Dy[i];

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (visited[ny * width + nx])
                        {
                            nextX = nx;
                            nextY = ny;
                            pathContinues = true;
                            break;
                        }
                    }
                }

                if (pathContinues)
                {
                    cx = nextX;
                    cy = nextY;
                }
                else
                {
                    bool snapped = false;
                    for (int r = 1; r <= 2; r++)
                    {
                        for (int dy = -r; dy <= r; dy++)
                        {
                            for (int dx = -r; dx <= r; dx++)
                            {
                                if (Math.Abs(dx) < r && Math.Abs(dy) < r) continue;

                                int nx = cx + dx;
                                int ny = cy + dy;

                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    if (originalMask[ny * width + nx] && !visited[ny * width + nx])
                                    {
                                        bool isRecentPixel = false;
                                        for (int p = 1; p <= Math.Min(3, path.Count); p++)
                                        {
                                            Vector2 prev = path[path.Count - p];
                                            if (prev.X == nx && prev.Y == ny) { isRecentPixel = true; break; }
                                        }

                                        if (!isRecentPixel)
                                        {
                                            path.Add(new Vector2(nx, ny));
                                            snapped = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (snapped) break;
                        }
                        if (snapped) break;
                    }
                }
            }

            return path;
        }

        private static int CountNeighbors(bool[] mask, int x, int y, int width)
        {
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                if (mask[(y + Dy[i]) * width + (x + Dx[i])]) count++;
            }
            return count;
        }
    }
}