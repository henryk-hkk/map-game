using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Constants;
using MapGame.Core.Utils.Geographic;
using System;
using System.Windows;
using System.Windows.Media;

namespace MapGame.Core.Utils.Graphic
{
    public static class SelectionTexturesGenerator
    {
        private const int SdfScale = 2;

        public static void InitializeSelectionRendering()
        {
            var (width, height, _) = MapUtils.GetBitmapParams();
            int scaledWidth = width * SdfScale;
            int scaledHeight = height * SdfScale;
            int scaledStride = scaledWidth * 4;

            Map.SelectionPixelData = new byte[scaledHeight * scaledStride];
        }

        public static void SelectRegionByAreaColor(Color areaColor)
        {
            if (!Map.Areas.TryGetValue(areaColor, out PixelArea clickedArea)) return;

            int targetRegionId = (int)clickedArea.ParentRegionId;
            if (targetRegionId == Map.CurrentlySelectedRegionId) return;

            var (width, height, _) = MapUtils.GetBitmapParams();
            int scaledStride = (width * SdfScale) * 4;

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            bool anyChanges = false;

            if (Map.CurrentlySelectedRegionId != -1)
            {
                foreach (var area in Map.Areas.Values)
                {
                    if (area.ParentRegionId == Map.CurrentlySelectedRegionId)
                    {
                        foreach (var pixel in area.Pixels)
                        {
                            if (pixel.X < minX) minX = pixel.X;
                            if (pixel.X > maxX) maxX = pixel.X;
                            if (pixel.Y < minY) minY = pixel.Y;
                            if (pixel.Y > maxY) maxY = pixel.Y;
                        }
                    }
                }

                Array.Clear(Map.SelectionPixelData, 0, Map.SelectionPixelData.Length);
                anyChanges = true;
            }

            Map.CurrentlySelectedRegionId = targetRegionId;

            foreach (var area in Map.Areas.Values)
            {
                if (area.ParentRegionId == targetRegionId)
                {
                    foreach (var pixel in area.Pixels)
                    {
                        int baseX = pixel.X * SdfScale;
                        int baseY = pixel.Y * SdfScale;

                        for (int dy = 0; dy < SdfScale; dy++)
                        {
                            for (int dx = 0; dx < SdfScale; dx++)
                            {
                                int idx = ((baseY + dy) * scaledStride) + ((baseX + dx) * 4);

                                Map.SelectionPixelData[idx] = 255;     // B
                                Map.SelectionPixelData[idx + 1] = 255; // G
                                Map.SelectionPixelData[idx + 2] = 255; // R
                                Map.SelectionPixelData[idx + 3] = 50;  // A
                            }
                        }

                        if (pixel.X < minX) minX = pixel.X;
                        if (pixel.X > maxX) maxX = pixel.X;
                        if (pixel.Y < minY) minY = pixel.Y;
                        if (pixel.Y > maxY) maxY = pixel.Y;
                        anyChanges = true;
                    }
                }
            }

            if (anyChanges)
            {
                minX = Math.Max(0, minX - 1);
                minY = Math.Max(0, minY - 1);
                maxX = Math.Min(width - 1, maxX + 1);
                maxY = Math.Min(height - 1, maxY + 1);

                Int32Rect updateRect = new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
                OverlayCompositor.ComposeAndApply(updateRect);
            }
        }

        public static void ClearSelection()
        {
            if (Map.CurrentlySelectedRegionId == -1) return;
            var (width, height, _) = MapUtils.GetBitmapParams();

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            bool anyChanges = false;

            foreach (var area in Map.Areas.Values)
            {
                if (area.ParentRegionId == Map.CurrentlySelectedRegionId)
                {
                    foreach (var pixel in area.Pixels)
                    {
                        if (pixel.X < minX) minX = pixel.X;
                        if (pixel.X > maxX) maxX = pixel.X;
                        if (pixel.Y < minY) minY = pixel.Y;
                        if (pixel.Y > maxY) maxY = pixel.Y;
                        anyChanges = true;
                    }
                }
            }

            Array.Clear(Map.SelectionPixelData, 0, Map.SelectionPixelData.Length);
            Map.CurrentlySelectedRegionId = -1;

            if (anyChanges)
            {
                minX = Math.Max(0, minX - 1);
                minY = Math.Max(0, minY - 1);
                maxX = Math.Min(width - 1, maxX + 1);
                maxY = Math.Min(height - 1, maxY + 1);

                Int32Rect updateRect = new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
                OverlayCompositor.ComposeAndApply(updateRect);
            }
        }
    }
}