using MapGame.Core.Constants;
using MapGame.Core.Utils.Geographic;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace MapGame.Core.Utils.Graphic
{
    public static class BorderTexturesGenerator
    {
        public static DiffuseMaterial GenerateRegionBorders(int scale = 4)
        {
            var (width, height, stride) = MapUtils.GetBitmapParams();
            var regionMap = MapUtils.GetRegionMap(width, height);

            int scaledWidth = width * scale;
            int scaledHeight = height * scale;
            int scaledStride = scaledWidth * 4;

            var borderPixels = SDFAgent.GetSmoothSDFBorders(regionMap, width, height, scale);

            WriteableBitmap bitmap = new WriteableBitmap(scaledWidth, scaledHeight, 96, 96, PixelFormats.Bgra32, null);
            bitmap.WritePixels(new Int32Rect(0, 0, scaledWidth, scaledHeight), borderPixels, scaledStride, 0);
            bitmap.Freeze();

            ImageBrush brush = new ImageBrush(bitmap);
            RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.HighQuality);
            brush.Freeze();

            return new DiffuseMaterial(brush);
        }

        public static byte[] GetRegionBorderPixels(int[] regionMap)
        {
            var (width, height, stride) = MapUtils.GetBitmapParams();

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
                        GraphicUtils.ColorPixel(borderPixels, byteIndex, 0, 0, 0, 255);
                    }
                }
            }

            return borderPixels;
        }

        public static byte[] GetScaledRegionBorderPixels(int[] regionMap, int scale)
        {
            var (width, height, stride) = MapUtils.GetBitmapParams();
            int scaledWidth = width * scale;
            int scaledHeight = height * scale;
            int scaledStride = scaledWidth * 4;

            byte[] borderPixels = new byte[scaledHeight * scaledStride];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index1D = (y * width) + x;
                    int currentRegion = regionMap[index1D];

                    if (currentRegion == -1) continue;

                    if (x < width - 1)
                    {
                        int rightRegion = regionMap[index1D + 1];
                        if (currentRegion != rightRegion)
                        {
                            int hx = (x + 1) * scale - 1;
                            for (int dy = 0; dy < scale; dy++)
                            {
                                int hy = y * scale + dy;
                                int byteIndex = (hy * scaledWidth + hx) * 4;
                                GraphicUtils.ColorPixel(borderPixels, byteIndex, 0, 0, 0, 220);
                            }
                        }
                    }

                    if (y < height - 1)
                    {
                        int bottomRegion = regionMap[index1D + width];
                        if (currentRegion != bottomRegion)
                        {
                            int hy = (y + 1) * scale - 1;
                            for (int dx = 0; dx < scale; dx++)
                            {
                                int hx = x * scale + dx;
                                int byteIndex = (hy * scaledWidth + hx) * 4;
                                GraphicUtils.ColorPixel(borderPixels, byteIndex, 0, 0, 0, 220);
                            }
                        }
                    }
                }
            }

            return borderPixels;
        }

        private static DiffuseMaterial GenerateAreaBorder(PolygonArea area, int scale = 4)
        {
            var (width, height, stride) = MapUtils.GetBitmapParams();
            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext dc = visual.RenderOpen())
            {
                Pen borderPen = new Pen(Brushes.Red, 1);

                StreamGeometry geometry = new StreamGeometry();
                using (StreamGeometryContext ctx = geometry.Open())
                {
                    Point startPoint = new Point(area.Vertices[0].X * scale, area.Vertices[0].Y * scale);
                    ctx.BeginFigure(startPoint, isFilled: false, isClosed: true);

                    for (int i = 1; i < area.Vertices.Count; i++)
                    {
                        ctx.LineTo(new Point(area.Vertices[i].X * scale, area.Vertices[i].Y * scale), isStroked: true, isSmoothJoin: true);
                    }
                }

                dc.DrawGeometry(null, borderPen, geometry);
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(width * scale, height * scale, 96, 96, PixelFormats.Pbgra32);
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
                        GraphicUtils.ColorPixel(borderPixels, index, 0, 0, 255, 255);
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
