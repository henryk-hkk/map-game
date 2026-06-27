using MapGame.Core.Constants;
using MapGame.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Engine
{
    public static class MapDisplay
    {
        private const int _step = 8;
        private const int _scale = 2;
        private const int _seaLevelPixelHeight = 0;
        private const double _heightScale = 0.3;
        private const byte _seaLevel = 143;
        private const int _landPixelHeightOffset = 0;
        public static GeometryModel3D GenerateTerrainMesh(byte[] heightmap, int width, int height)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            int cols = width / _step;
            int rows = height / _step;

            for (int r = 0; r <= rows; r++)
            {
                for (int c = 0; c <= cols; c++)
                {
                    int x = Math.Min(c * _step, width - 1);
                    int y = Math.Min(r * _step, height - 1);

                    int index = y * width + x;
                    byte z = heightmap[index];
                    bool isLand = Map.LandMask[index];

                    double finalPixelHeight;

                    if (isLand)
                    {
                        finalPixelHeight = (z - _seaLevel) * _heightScale + _landPixelHeightOffset;
                        if (finalPixelHeight < _seaLevelPixelHeight) finalPixelHeight = _seaLevelPixelHeight;
                    }
                    else
                    {
                        finalPixelHeight = _seaLevelPixelHeight;
                    }

                    mesh.Positions.Add(new Point3D(x, finalPixelHeight, y));

                    mesh.TextureCoordinates.Add(new Point((double)x / width, (double)y / height));
                }
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int topLeft = r * (cols + 1) + c;
                    int topRight = topLeft + 1;
                    int bottomLeft = (r + 1) * (cols + 1) + c;
                    int bottomRight = bottomLeft + 1;

                    mesh.TriangleIndices.Add(topLeft);
                    mesh.TriangleIndices.Add(bottomLeft);
                    mesh.TriangleIndices.Add(topRight);

                    mesh.TriangleIndices.Add(topRight);
                    mesh.TriangleIndices.Add(bottomLeft);
                    mesh.TriangleIndices.Add(bottomRight);
                }
            }
            mesh.Freeze();
            GeometryModel3D model = new GeometryModel3D();
            model.Geometry = mesh;

            MaterialGroup materialGroup = new MaterialGroup();

            BitmapImage baseTexture = Map.TextureMap; //Map.TextureMap is not null, this function is called after the game engine does its thing and loads the maps.
            materialGroup.Children.Add(new DiffuseMaterial(new ImageBrush(baseTexture)));

            //DiffuseMaterial bordersMaterial = GenerateBordersMaterial(Map.Gdansk, width, height);
            //materialGroup.Children.Add(bordersMaterial);

            DiffuseMaterial bordersMaterial = GenerateBordersFromColorMap(Map.AreaPixels);
            materialGroup.Children.Add(bordersMaterial);

            model.Material = materialGroup;

            return model;
        }

        private static DiffuseMaterial GenerateBordersMaterial(PolygonArea area, int mapWidth, int mapHeight)
        {
            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext dc = visual.RenderOpen())
            {
                Pen borderPen = new Pen(Brushes.Red, 1);

                StreamGeometry geometry = new StreamGeometry();
                using (StreamGeometryContext ctx = geometry.Open())
                {
                    Point startPoint = new Point(area.Vertices[0].X * _scale, area.Vertices[0].Y * _scale);
                    ctx.BeginFigure(startPoint, isFilled: false, isClosed: true);

                    for (int i = 1; i < area.Vertices.Count; i++)
                    {
                        ctx.LineTo(new Point(area.Vertices[i].X * _scale, area.Vertices[i].Y * _scale), isStroked: true, isSmoothJoin: true);
                    }
                }

                dc.DrawGeometry(null, borderPen, geometry);
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(mapWidth * _scale, mapHeight * _scale, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();

            ImageBrush bordersBrush = new ImageBrush(rtb);

            RenderOptions.SetBitmapScalingMode(bordersBrush, BitmapScalingMode.NearestNeighbor);
            bordersBrush.Freeze();
            return new DiffuseMaterial(new ImageBrush(rtb));
        }

        public static DiffuseMaterial GenerateBordersFromColorMap(byte[] colorPixels)
        {
            int stride = Map.Width * 4;

            byte[] borderPixels = new byte[Map.Height * stride];

            for (int y = 1; y < Map.Height - 1; y++)
            {
                for (int x = 1; x < Map.Width - 1; x++)
                {
                    int index = (y * stride) + (x * 4);

                    if (colorPixels[index] == 0 && colorPixels[index + 1] == 0 && colorPixels[index + 2] == 0) // Ignore #000 (Water)
                        continue;

                    byte b = colorPixels[index];
                    byte g = colorPixels[index + 1];
                    byte r = colorPixels[index + 2];

                    int indexUp = index - stride;
                    int indexDown = index + stride;
                    int indexLeft = index - 4;
                    int indexRight = index + 4;

                    // Is a border if any of the surrounding pixels has a different color.
                    bool isBorder =
                        (colorPixels[indexUp] != b || colorPixels[indexUp + 1] != g || colorPixels[indexUp + 2] != r) ||
                        (colorPixels[indexDown] != b || colorPixels[indexDown + 1] != g || colorPixels[indexDown + 2] != r) ||
                        (colorPixels[indexLeft] != b || colorPixels[indexLeft + 1] != g || colorPixels[indexLeft + 2] != r) ||
                        (colorPixels[indexRight] != b || colorPixels[indexRight + 1] != g || colorPixels[indexRight + 2] != r);

                    if (isBorder)
                    {
                        borderPixels[index] = 0;       // Blue
                        borderPixels[index + 1] = 0;   // Green
                        borderPixels[index + 2] = 255; // Red
                        borderPixels[index + 3] = 255; // Alpha
                    }
                }
            }

            WriteableBitmap bitmap = new WriteableBitmap(Map.Width, Map.Height, 96, 96, PixelFormats.Bgra32, null);

            bitmap.WritePixels(new Int32Rect(0, 0, Map.Width, Map.Height), borderPixels, stride, 0);
            bitmap.Freeze();

            ImageBrush brush = new ImageBrush(bitmap);
            RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.NearestNeighbor);
            brush.Freeze();

            return new DiffuseMaterial(brush);
        }
    }
}
