using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Utils.Map;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace MapGame.Core.Utils.Graphic
{
    public class BorderTexturesGenerator : ITexturesGenerator
    {
        private const int SdfScale = ITexturesGenerator.SdfScale;
        public static void Initialize()
        {
            var (width, height, _) = MapUtils.GetBitmapParams();

            int scaledWidth = width * SdfScale;
            int scaledHeight = height * SdfScale;
            int scaledStride = scaledWidth * 4;

            GraphicContext.RegionBorderPixelData = new byte[scaledHeight * scaledStride];

            Int32Rect fullMapRect = new(0, 0, width, height);
            RefreshDirtyRect(fullMapRect);
        }

        public static void UpdateBorders(IEnumerable<BorderPixelSegment> segmentsToUpdate)
        {
            var (width, _, _) = MapUtils.GetBitmapParams();

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            bool anyChanges = false;

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

            Int32Rect dirtyRect = GraphicUtils.GetDirtyRect(minX, maxX, minY, maxY, margin);

            RefreshDirtyRect(dirtyRect);
        }

        public static void RefreshDirtyRect(Int32Rect dirtyRect)
        {
            var (width, height, _) = MapUtils.GetBitmapParams();
            var (_, _, scaledStride) = MapUtils.GetScaledBitmapParams();


            int startX_scaled = dirtyRect.X * SdfScale;
            int startY_scaled = dirtyRect.Y * SdfScale;
            int endX_scaled = (dirtyRect.X + dirtyRect.Width) * SdfScale;
            int endY_scaled = (dirtyRect.Y + dirtyRect.Height) * SdfScale;

            int rowByteLength = (endX_scaled - startX_scaled) * 4;
            for (int y = startY_scaled; y < endY_scaled; y++)
            {
                int startByteIdx = (y * scaledStride) + (startX_scaled * 4);
                Array.Clear(GraphicContext.RegionBorderPixelData, startByteIdx, rowByteLength);
            }

            var sdfPixels = SDFAgent.ComputeLocalSDF(MapLogicContext.GlobalRegionMap, width, height, dirtyRect);

            foreach (var pixel in sdfPixels)
            {
                int byteIdx = pixel.Index;
                byte alpha = pixel.Alpha;

                if (byteIdx >= 0 && byteIdx + 3 < GraphicContext.RegionBorderPixelData.Length)
                {
                    GraphicContext.RegionBorderPixelData[byteIdx] = 50;     // B
                    GraphicContext.RegionBorderPixelData[byteIdx + 1] = 50; // G
                    GraphicContext.RegionBorderPixelData[byteIdx + 2] = 50; // R
                    GraphicContext.RegionBorderPixelData[byteIdx + 3] = (byte)(alpha * 0.8f); // Alpha
                }
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
            var (width, height, _) = MapUtils.GetBitmapParams();
            var (scaledWidth, scaledHeight, scaledStride) = MapUtils.GetScaledBitmapParams();


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
    }
}