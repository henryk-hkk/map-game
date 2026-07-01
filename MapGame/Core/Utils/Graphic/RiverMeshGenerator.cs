using MapGame.Core.Utils.Geographic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace MapGame.Core.Utils.Graphic
{
    public static class RiverMeshGenerator
    {
        public static GeometryModel3D GenerateAnimatedRivers(bool[] riverMask, BitmapImage waterTexture, byte[] heightmap)
        {
            var (width, height, stride) = MapUtils.GetBitmapParams();
            var thinnedMask = GetThinnedMask(riverMask, width, height);

            List<List<Point>> tracedRivers = RiverPathTracer.TraceAllRivers(thinnedMask, width, height);

            MeshGeometry3D finalMesh = new MeshGeometry3D();
            Point3DCollection positions = new Point3DCollection();
            PointCollection textureCoordinates = new PointCollection();
            Int32Collection triangleIndices = new Int32Collection();

            double riverWidth = 3.0;
            double yOffset = 0.5;

            foreach (List<Point> rawRiverPath in tracedRivers)
            {
                if (rawRiverPath.Count < 15) continue;

                Point punktStartowy = rawRiverPath[0];
                Point punktKoncowy = rawRiverPath[rawRiverPath.Count - 1];

                double wysokoscStartu = TerrainMeshGenerator.GetTerrainHeight(heightmap, (int)punktStartowy.X, (int)punktStartowy.Y, width, height);
                double wysokoscKonca = TerrainMeshGenerator.GetTerrainHeight(heightmap, (int)punktKoncowy.X, (int)punktKoncowy.Y, width, height);
                if (wysokoscStartu < wysokoscKonca)
                {
                    rawRiverPath.Reverse();
                }

                List<Point> riverPath = SmoothRiverPath(rawRiverPath, 5);

                for (int i = 0; i < riverPath.Count; i++)
                {
                    Point pCurrent = riverPath[i];

                    int lookSpan = 3;
                    int indexAhead = Math.Min(i + lookSpan, riverPath.Count - 1);
                    int indexBehind = Math.Max(i - lookSpan, 0);

                    Vector direction = riverPath[indexAhead] - riverPath[indexBehind];

                    if (direction.Length == 0 && i < riverPath.Count - 1)
                        direction = riverPath[i + 1] - pCurrent;
                    if (direction.Length == 0)
                        direction = new Vector(1, 0);

                    direction.Normalize();

                    if (i == 0)
                    {
                        pCurrent = pCurrent - direction * 4.0;
                    }
                    else if (i == riverPath.Count - 1)
                    {
                        pCurrent = pCurrent + direction * 4.0;
                    }

                    Vector perpendicular = new Vector(-direction.Y, direction.X) * (riverWidth / 2.0);

                    Point lewyBrzeg = pCurrent + perpendicular;
                    Point prawyBrzeg = pCurrent - perpendicular;

                    int safeX = Math.Max(0, Math.Min((int)pCurrent.X, width - 1));
                    int safeY = Math.Max(0, Math.Min((int)pCurrent.Y, height - 1));

                    double h = TerrainMeshGenerator.GetTerrainHeight(heightmap, safeX, safeY, width, height) + yOffset;

                    if (i == 0 || i == riverPath.Count - 1)
                    {
                        h -= 0.05;
                    }

                    int currentIndex = positions.Count;

                    positions.Add(new Point3D(lewyBrzeg.X, h, lewyBrzeg.Y));
                    positions.Add(new Point3D(prawyBrzeg.X, h, prawyBrzeg.Y));

                    textureCoordinates.Add(new Point(0, (double)i / 10.0));
                    textureCoordinates.Add(new Point(1, (double)i / 10.0));

                    if (i > 0)
                    {
                        triangleIndices.Add(currentIndex - 2);
                        triangleIndices.Add(currentIndex - 1);
                        triangleIndices.Add(currentIndex);

                        triangleIndices.Add(currentIndex);
                        triangleIndices.Add(currentIndex - 1);
                        triangleIndices.Add(currentIndex + 1);
                    }
                }
            }

            finalMesh.Positions = positions;
            finalMesh.TextureCoordinates = textureCoordinates;
            finalMesh.TriangleIndices = triangleIndices;
            finalMesh.Freeze();

            var riverMaterial = GetAnimatedRiverMaterial(waterTexture, animationSpeed: 5);
            GeometryModel3D riverModel = new GeometryModel3D(finalMesh, riverMaterial);
            riverModel.BackMaterial = riverMaterial;

            return riverModel;
        }

        private static DiffuseMaterial GetAnimatedRiverMaterial(BitmapImage waterTex, int animationSpeed)
        {
            ImageBrush waterBrush = new ImageBrush(waterTex);
            waterBrush.TileMode = TileMode.Tile;
            waterBrush.ViewportUnits = BrushMappingMode.Absolute;
            waterBrush.Viewport = new Rect(0, 0, 1, 1);

            RenderOptions.SetBitmapScalingMode(waterBrush, BitmapScalingMode.HighQuality);

            TranslateTransform waterScroll = new TranslateTransform();
            waterBrush.Transform = waterScroll;

            DoubleAnimation flowAnim = new DoubleAnimation(0, 1.0, TimeSpan.FromSeconds(animationSpeed));
            flowAnim.RepeatBehavior = RepeatBehavior.Forever;

            waterScroll.BeginAnimation(TranslateTransform.YProperty, flowAnim);

            return new DiffuseMaterial(waterBrush);
        }

        private static List<Point> SmoothRiverPath(List<Point> path, int iterations)
        {
            if (path.Count < 3) return path;

            List<Point> smoothed = new List<Point>(path);

            for (int it = 0; it < iterations; it++)
            {
                List<Point> temp = new List<Point>();

                temp.Add(smoothed[0]);

                for (int i = 1; i < smoothed.Count - 1; i++)
                {
                    double nx = (smoothed[i - 1].X + smoothed[i].X + smoothed[i + 1].X) / 3.0;
                    double ny = (smoothed[i - 1].Y + smoothed[i].Y + smoothed[i + 1].Y) / 3.0;
                    temp.Add(new Point(nx, ny));
                }

                temp.Add(smoothed[smoothed.Count - 1]);

                smoothed = temp;
            }

            return smoothed;
        }

        private static MeshGeometry3D BuildRiverMesh(bool[] mask, byte[] heightmap, int width, int height, double yOffset, double textureSize)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            Point3DCollection positions = new Point3DCollection();
            PointCollection textureCoordinates = new PointCollection();
            Int32Collection triangleIndices = new Int32Collection();

            int vertexIndex = 0;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    if (mask[y * width + x])
                    {
                        double h0 = TerrainMeshGenerator.GetTerrainHeight(heightmap, x, y, width, height) + yOffset;
                        double h1 = TerrainMeshGenerator.GetTerrainHeight(heightmap, x + 1, y, width, height) + yOffset;
                        double h2 = TerrainMeshGenerator.GetTerrainHeight(heightmap, x, y + 1, width, height) + yOffset;
                        double h3 = TerrainMeshGenerator.GetTerrainHeight(heightmap, x + 1, y + 1, width, height) + yOffset;

                        positions.Add(new Point3D(x, h0, y));
                        positions.Add(new Point3D(x + 1, h1, y));
                        positions.Add(new Point3D(x, h2, y + 1));
                        positions.Add(new Point3D(x + 1, h3, y + 1));

                        textureCoordinates.Add(new Point(x / textureSize, y / textureSize));
                        textureCoordinates.Add(new Point((x + 1) / textureSize, y / textureSize));
                        textureCoordinates.Add(new Point(x / textureSize, (y + 1) / textureSize));
                        textureCoordinates.Add(new Point((x + 1) / textureSize, (y + 1) / textureSize));

                        triangleIndices.Add(vertexIndex);
                        triangleIndices.Add(vertexIndex + 2);
                        triangleIndices.Add(vertexIndex + 1);

                        triangleIndices.Add(vertexIndex + 1);
                        triangleIndices.Add(vertexIndex + 2);
                        triangleIndices.Add(vertexIndex + 3);

                        vertexIndex += 4;
                    }
                }
            }

            mesh.Positions = positions;
            mesh.TextureCoordinates = textureCoordinates;
            mesh.TriangleIndices = triangleIndices;
            mesh.Freeze();

            return mesh;
        }

        private static bool[] GetErodedMask(bool[] mask, int erosionPasses, int width, int height)
        {
            bool[] isRiver = (bool[])mask.Clone();
            for (int pass = 0; pass < erosionPasses; pass++)
            {
                bool[] nextIsRiver = (bool[])isRiver.Clone();
                
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int idx = y * width + x;
                    
                        if (isRiver[idx])
                        {
                            bool touchesLand = !isRiver[idx - width] || !isRiver[idx + width] || !isRiver[idx - 1] || !isRiver[idx + 1];
                            if (touchesLand)
                            {
                                nextIsRiver[idx] = false;
                            }
                        }
                    }
                }
                isRiver = nextIsRiver;
            }
            return isRiver;
        }

        private static bool[] GetThinnedMask(bool[] mask, int width, int height)
        { //Zhang Suen Thinning
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

                        if (ap == 1)
                        {
                            grid[idx] = false;
                            changed = true;
                        }
                    }
                }
            }
            return grid;
        }
    }
}
