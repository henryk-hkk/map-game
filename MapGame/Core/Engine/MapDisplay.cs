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

        private static void ColorPixel(ref byte[] pixels, int pixelIndex, Color color)
        {
            byte blue = color.B, green = color.G, red = color.R, alpha = color.A;
            ColorPixel(ref pixels, pixelIndex, blue, green, red, alpha);
        }

        private static void ColorPixel(ref byte[] pixels, int pixelIndex, byte blue, byte green, byte red, byte alpha)
        {
            pixels[pixelIndex] = blue;
            pixels[pixelIndex + 1] = green;
            pixels[pixelIndex + 2] = red;
            pixels[pixelIndex + 3] = alpha;
        }

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

            DiffuseMaterial bordersMaterial = GenerateRegionBorders();
            materialGroup.Children.Add(bordersMaterial);

            model.Material = materialGroup;

            return model;
        }

        private static DiffuseMaterial GenerateAreaBorder(PolygonArea area)
        {
            var (width, height, stride) = MapUtils.GetBitmapParams();
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

            RenderTargetBitmap rtb = new RenderTargetBitmap(width * _scale, height * _scale, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();

            ImageBrush bordersBrush = new ImageBrush(rtb);

            RenderOptions.SetBitmapScalingMode(bordersBrush, BitmapScalingMode.NearestNeighbor);
            bordersBrush.Freeze();
            return new DiffuseMaterial(new ImageBrush(rtb));
        }

        private static DiffuseMaterial GenerateAreaBorders()
        {
            var (width, height, stride) = MapUtils.GetBitmapParams();

            byte[] borderPixels = new byte[height * stride];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int index = (y * stride) + (x * 4);

                    if (Map.AreaPixels[index] == 0 && Map.AreaPixels[index + 1] == 0 && Map.AreaPixels[index + 2] == 0) // Ignore #000 (Water)
                        continue;

                    byte b = Map.AreaPixels[index];
                    byte g = Map.AreaPixels[index + 1];
                    byte r = Map.AreaPixels[index + 2];

                    int indexUp = index - stride;
                    int indexDown = index + stride;
                    int indexLeft = index - 4;
                    int indexRight = index + 4;

                    // Is a border if any of the surrounding pixels has a different color.
                    bool isBorder =
                        (Map.AreaPixels[indexUp] != b || Map.AreaPixels[indexUp + 1] != g || Map.AreaPixels[indexUp + 2] != r) ||
                        (Map.AreaPixels[indexDown] != b || Map.AreaPixels[indexDown + 1] != g || Map.AreaPixels[indexDown + 2] != r) ||
                        (Map.AreaPixels[indexLeft] != b || Map.AreaPixels[indexLeft + 1] != g || Map.AreaPixels[indexLeft + 2] != r) ||
                        (Map.AreaPixels[indexRight] != b || Map.AreaPixels[indexRight + 1] != g || Map.AreaPixels[indexRight + 2] != r);

                    if (isBorder)
                    {
                        ColorPixel(ref borderPixels, index, 0, 0, 255, 255);
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
        private static DiffuseMaterial GenerateRegionBorders()
        {
            var (width, height, stride) = MapUtils.GetBitmapParams();
            int totalPixels = width * height;

            var regionMap = MapUtils.GetRegionMap(width, height);
            var borderPixels = GetRegionBorderPixels(regionMap, width, height, stride);

            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), borderPixels, stride, 0);
            bitmap.Freeze();

            ImageBrush brush = new ImageBrush(bitmap);
            RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.NearestNeighbor);
            brush.Freeze();

            return new DiffuseMaterial(brush);
        }

        private static byte[] GetRegionBorderPixels(int[] regionMap, int width, int height, int stride)
        {
            byte[] borderPixels = new byte[height * stride];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int index1D = (y * width) + x;
                    int currentRegion = regionMap[index1D];

                    // Ignorujemy wodę przy sprawdzaniu bycia krawędzią od środka
                    if (currentRegion == -1) continue;

                    int regionUp = regionMap[index1D - width];
                    int regionDown = regionMap[index1D + width];
                    int regionLeft = regionMap[index1D - 1];
                    int regionRight = regionMap[index1D + 1];

                    bool isBorder =
                        (regionUp != currentRegion)
                        || (regionDown != currentRegion)
                        || (regionLeft != currentRegion)
                        || (regionRight != currentRegion);

                    if (isBorder)
                    {
                        int byteIndex = index1D * 4;
                        ColorPixel(ref borderPixels, byteIndex, 0, 0, 0, 255);
                    }
                }
            }

            return borderPixels;
        }
    }
}
