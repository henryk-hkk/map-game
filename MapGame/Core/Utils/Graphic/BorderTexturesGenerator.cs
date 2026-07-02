using MapGame.Core.Constants;
using MapGame.Core.Utils.Geographic;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HelixToolkit.Wpf.SharpDX;

namespace MapGame.Core.Utils.Graphic
{
    public static class BorderTexturesGenerator
    {
        private const int SdfScale = 2;

        public static void InitializeBorderRendering(Dictionary<(Color, Color), BorderPixelSegment> borderGraph)
        {
            var (width, height, _) = MapUtils.GetBitmapParams();

            int scaledWidth = width * SdfScale;
            int scaledHeight = height * SdfScale;
            int scaledStride = scaledWidth * 4;

            Map.RegionBorderPixelData = new byte[scaledHeight * scaledStride];
            Map.RegionBordersBitmap = new WriteableBitmap(scaledWidth, scaledHeight, 96, 96, PixelFormats.Bgra32, null);

            Int32Rect fullMapRect = new Int32Rect(0, 0, width, height);
            RefreshDirtyRectSDF(fullMapRect);

            Map.RegionBordersMaterial = new PhongMaterial()
            {
                DiffuseMap = Map.RegionBordersBitmap.ToDynamicTextureModel(),
                AmbientColor = new HelixToolkit.Maths.Color4(1,1,1,0)
            };
        }

        public static void UpdateBorders(IEnumerable<BorderPixelSegment> segmentsToUpdate)
        {
            var (width, height, _) = MapUtils.GetBitmapParams();

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            bool anyChanges = false;

            // Bounding box of changed segments
            foreach (var segment in segmentsToUpdate)
            {
                foreach (int index in segment.PixelIndices)
                {
                    int pX = (index / 4) % width;
                    int pY = (index / 4) / width;

                    if (pX < minX) minX = pX;
                    if (pX > maxX) maxX = pX;
                    if (pY < minY) minY = pY;
                    if (pY > maxY) maxY = pY;

                    anyChanges = true;
                }
            }

            if (!anyChanges) return;

            float maxSdfDistance = SDFAgent.BorderThickness + (SdfScale * SDFAgent.SmoothRadiusMultiplier);
            int margin = (int)Math.Ceiling(maxSdfDistance) + SdfScale + 2;

            minX = Math.Max(0, minX - margin);
            minY = Math.Max(0, minY - margin);
            maxX = Math.Min(width - 1, maxX + margin);
            maxY = Math.Min(height - 1, maxY + margin);

            Int32Rect dirtyRect = new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);

            RefreshDirtyRectSDF(dirtyRect);
        }

        private static void RefreshDirtyRectSDF(Int32Rect dirtyRect)
        {
            var (width, height, _) = MapUtils.GetBitmapParams();
            int scaledWidth = width * SdfScale;
            int scaledStride = scaledWidth * 4;

            int startX_scaled = dirtyRect.X * SdfScale;
            int startY_scaled = dirtyRect.Y * SdfScale;
            int endX_scaled = (dirtyRect.X + dirtyRect.Width) * SdfScale;
            int endY_scaled = (dirtyRect.Y + dirtyRect.Height) * SdfScale;

            // Erasing
            for (int y = startY_scaled; y < endY_scaled; y++)
            {
                for (int x = startX_scaled; x < endX_scaled; x++)
                {
                    int idx = (y * scaledStride) + (x * 4);
                    Map.RegionBorderPixelData[idx] = 0;
                    Map.RegionBorderPixelData[idx + 1] = 0;
                    Map.RegionBorderPixelData[idx + 2] = 0;
                    Map.RegionBorderPixelData[idx + 3] = 0; // Alpha na 0
                }
            }

            var sdfPixels = SDFAgent.ComputeLocalSDF(Map.GlobalRegionMap, width, height, SdfScale, dirtyRect);

            foreach (var pixel in sdfPixels)
            {
                int idx = pixel.Index;
                byte alpha = pixel.Alpha;

                if (idx >= 0 && idx + 3 < Map.RegionBorderPixelData.Length)
                {
                    Map.RegionBorderPixelData[idx] = 50;     // B
                    Map.RegionBorderPixelData[idx + 1] = 50; // G
                    Map.RegionBorderPixelData[idx + 2] = 50; // R
                    Map.RegionBorderPixelData[idx + 3] = (byte)(alpha * 0.8);
                }
            }

            Int32Rect scaledDirtyRect = new Int32Rect(
                startX_scaled,
                startY_scaled,
                endX_scaled - startX_scaled,
                endY_scaled - startY_scaled);

            int offset = (startY_scaled * scaledStride) + (startX_scaled * 4);

            Map.RegionBordersBitmap.WritePixels(scaledDirtyRect, Map.RegionBorderPixelData, scaledStride, offset);

            if (Map.RegionBordersMaterial is PhongMaterial phong)
            {
                phong.DiffuseMap = Map.RegionBordersBitmap.ToDynamicTextureModel();
            }
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

                    // Ignoring water
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

        private static Material GenerateAreaBorder(PolygonArea area, int scale = 4)
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

            // RenderTargetBitmap dziedziczy z BitmapSource, więc używamy klasycznego ToTextureModel z PNG w pamięci
            return new PhongMaterial()
            {
                DiffuseMap = rtb.ToTextureModel(),
                AmbientColor = HelixToolkit.Maths.Color4.White
            };
        }

        private static Material GenerateAreaBorders()
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

            return new PhongMaterial()
            {
                DiffuseMap = bitmap.ToDynamicTextureModel(),
                AmbientColor = HelixToolkit.Maths.Color4.White
            };
        }
    }
}