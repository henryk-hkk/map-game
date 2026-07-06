using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Constants;
using MapGame.Core.Utils.Geographic;
using MapGame.Core.Engine; // Dodano dla SDFAgent
using System;
using System.Windows;
using System.Windows.Media;

namespace MapGame.Core.Utils.Graphic
{
    public static class SelectionTexturesGenerator
    {
        private const int SdfScale = Constants.Graphic.SdfScale;

        public static void InitializeSelectionRendering()
        {
            var (width, height, _) = MapUtils.GetBitmapParams();
            int scaledWidth = width * SdfScale;
            int scaledHeight = height * SdfScale;
            int scaledStride = scaledWidth * 4;

            Map.SelectionPixelData = new byte[scaledHeight * scaledStride];
            Map.GlobalSelectionMask = new int[width * height];
        }

        public static void SelectRegionByAreaColor(Color areaColor)
        {
            if (!Map.Areas.TryGetValue(areaColor, out PixelArea clickedArea)) return;
            if (clickedArea.ParentRegionId == null) return;

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
                            Map.GlobalSelectionMask[(pixel.Y * width) + pixel.X] = 0;

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
                        Map.GlobalSelectionMask[(pixel.Y * width) + pixel.X] = 1;

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
                                Map.SelectionPixelData[idx + 3] = 40;  // A
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
                int margin = 8;
                minX = Math.Max(0, minX - margin);
                minY = Math.Max(0, minY - margin);
                maxX = Math.Min(width - 1, maxX + margin);
                maxY = Math.Min(height - 1, maxY + margin);

                Int32Rect updateRect = new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);

                float origThickness = SDFAgent.BorderThickness;
                float origRadius = SDFAgent.SmoothRadiusMultiplier;

                SDFAgent.BorderThickness = 0.2f;
                SDFAgent.SmoothRadiusMultiplier = 1.5f;

                var sdfPixels = SDFAgent.ComputeLocalSDF(Map.GlobalSelectionMask, width, height, SdfScale, updateRect);

                SDFAgent.BorderThickness = origThickness;
                SDFAgent.SmoothRadiusMultiplier = origRadius;
                foreach (var p in sdfPixels)
                {
                    int idx = p.Index;
                    if (idx >= 0 && idx + 3 < Map.SelectionPixelData.Length)
                    {
                        Map.SelectionPixelData[idx] = 255;
                        Map.SelectionPixelData[idx + 1] = 255;
                        Map.SelectionPixelData[idx + 2] = 255;

                        byte glowAlpha = (byte)(p.Alpha * 0.7f);
                        byte currentAlpha = Map.SelectionPixelData[idx + 3];

                        Map.SelectionPixelData[idx + 3] = Math.Max(currentAlpha, glowAlpha);
                    }
                }

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
                        Map.GlobalSelectionMask[(pixel.Y * width) + pixel.X] = 0;

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
                int margin = 8;
                minX = Math.Max(0, minX - margin);
                minY = Math.Max(0, minY - margin);
                maxX = Math.Min(width - 1, maxX + margin);
                maxY = Math.Min(height - 1, maxY + margin);

                Int32Rect updateRect = new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
                OverlayCompositor.ComposeAndApply(updateRect);
            }
        }
    }
}