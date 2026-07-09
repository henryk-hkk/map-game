using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Utils.Geographic;
using MapGame.Core.Engine; // Dodano dla SDFAgent
using System;
using System.Windows;
using System.Windows.Media;

namespace MapGame.Core.Utils.Graphic
{
    public static class SelectionTexturesGenerator
    {
        private const int SdfScale = GraphicContext.SdfScale;
        public static Int32Rect LastSelectionRect { get; set; } = Int32Rect.Empty;
        public static void InitializeSelectionRendering()
        {
            var (width, height, _) = MapUtils.GetBitmapParams();
            var (_, scaledHeight, scaledStride) = MapUtils.GetScaledBitmapParams();

            GraphicContext.SelectionPixelData = new byte[scaledHeight * scaledStride];
            MapContext.GlobalSelectionMask = new int[width * height];
        }

        public static void SelectRegionByAreaColor(Color areaColor)
        {
            if (!GraphicContext.AreaColors.TryGetValue(areaColor, out PixelArea clickedArea)) return;
            if (clickedArea.ParentRegionId == null) return;

            int targetRegionId = (int)clickedArea.ParentRegionId;
            if (targetRegionId == MapContext.CurrentlySelectedRegionId) return;

            ClearSelection();

            MapContext.CurrentlySelectedRegionId = targetRegionId;

            var (width, height, _) = MapUtils.GetBitmapParams();
            var (_, _, scaledStride) = MapUtils.GetScaledBitmapParams();

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            bool anyChanges = false;


            foreach (var area in GraphicContext.AreaColors.Values)
            {
                if (area.ParentRegionId == targetRegionId)
                {
                    foreach (var pixel in area.Pixels)
                    {
                        MapContext.GlobalSelectionMask[(pixel.Y * width) + pixel.X] = 1;

                        int baseX = pixel.X * SdfScale;
                        int baseY = pixel.Y * SdfScale;

                        for (int dy = 0; dy < SdfScale; dy++)
                        {
                            for (int dx = 0; dx < SdfScale; dx++)
                            {
                                int idx = ((baseY + dy) * scaledStride) + ((baseX + dx) * 4);

                                GraphicContext.SelectionPixelData[idx] = 255;     // B
                                GraphicContext.SelectionPixelData[idx + 1] = 255; // G
                                GraphicContext.SelectionPixelData[idx + 2] = 255; // R
                                GraphicContext.SelectionPixelData[idx + 3] = 50;  // A
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

                Int32Rect updateRect = new(minX, minY, maxX - minX + 1, maxY - minY + 1);
                LastSelectionRect = updateRect;

                float origThickness = SDFAgent.BorderThickness;
                float origRadius = SDFAgent.SmoothRadiusMultiplier;

                SDFAgent.BorderThickness = 0.2f;
                SDFAgent.SmoothRadiusMultiplier = 1.5f;

                var sdfPixels = SDFAgent.ComputeLocalSDF(MapContext.GlobalSelectionMask, width, height, updateRect);

                SDFAgent.BorderThickness = origThickness;
                SDFAgent.SmoothRadiusMultiplier = origRadius;
                foreach (var p in sdfPixels)
                {
                    int idx = p.Index;
                    if (idx >= 0 && idx + 3 < GraphicContext.SelectionPixelData.Length)
                    {
                        GraphicContext.SelectionPixelData[idx] = 255;
                        GraphicContext.SelectionPixelData[idx + 1] = 255;
                        GraphicContext.SelectionPixelData[idx + 2] = 255;

                        byte glowAlpha = (byte)(p.Alpha * 0.7f);
                        byte currentAlpha = GraphicContext.SelectionPixelData[idx + 3];

                        GraphicContext.SelectionPixelData[idx + 3] = Math.Max(currentAlpha, glowAlpha);
                    }
                }

                OverlayCompositor.ComposeAndApply(updateRect);
            }
        }

        public static void ClearSelection()
        {
            if (MapContext.CurrentlySelectedRegionId == -1) return;

            Array.Clear(MapContext.GlobalSelectionMask, 0, MapContext.GlobalSelectionMask.Length);
            Array.Clear(GraphicContext.SelectionPixelData, 0, GraphicContext.SelectionPixelData.Length);

            MapContext.CurrentlySelectedRegionId = -1;

            if (LastSelectionRect != Int32Rect.Empty)
            {
                OverlayCompositor.ComposeAndApply(LastSelectionRect);
                LastSelectionRect = Int32Rect.Empty;
            }
        }
    }
}