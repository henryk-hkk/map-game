using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Utils.Map;
using System;
using System.Windows;
using System.Windows.Media;

namespace MapGame.Core.Utils.Graphic
{
    public class SelectionTexturesGenerator : ITexturesGenerator
    {
        private const int SdfScale = ITexturesGenerator.SdfScale;
        public static Int32Rect LastSelectionRect { get; set; } = Int32Rect.Empty;
        public static void Initialize()
        {
            var (width, height, _) = MapUtils.GetBitmapParams();
            var (_, scaledHeight, scaledStride) = MapUtils.GetScaledBitmapParams();

            GraphicContext.SelectionPixelData = new byte[scaledHeight * scaledStride];
            MapLogicContext.GlobalSelectionMask = new int[width * height];
        }

        public static Int32Rect GetSelectionUpdateDirtyRect (int targetRegionId)
        {
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
                        MapLogicContext.GlobalSelectionMask[(pixel.Y * width) + pixel.X] = 1;

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

            if (!anyChanges) return Int32Rect.Empty;

            int margin = 8;

            return GraphicUtils.GetDirtyRect(minX, maxX, minY, maxY, margin);
        }

        public static void RefreshDirtyRect(Int32Rect dirtyRect)
        {
            LastSelectionRect = dirtyRect;
            var (width, height, _) = MapUtils.GetBitmapParams();

            float origThickness = SDFAgent.BorderThickness;
            float origRadius = SDFAgent.SmoothRadiusMultiplier;

            SDFAgent.BorderThickness = 0.2f;
            SDFAgent.SmoothRadiusMultiplier = 1.5f;

            var sdfPixels = SDFAgent.ComputeLocalSDF(MapLogicContext.GlobalSelectionMask, width, height, dirtyRect);

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
        }

        public static void ClearSelection()
        {
            if (MapLogicContext.CurrentlySelectedRegionId == -1) return;

            Array.Clear(MapLogicContext.GlobalSelectionMask, 0, MapLogicContext.GlobalSelectionMask.Length);
            Array.Clear(GraphicContext.SelectionPixelData, 0, GraphicContext.SelectionPixelData.Length);

            MapLogicContext.CurrentlySelectedRegionId = -1;
        }

        public static Int32Rect GetClearedSelectionDirtyRect()
        {
            if (LastSelectionRect == Int32Rect.Empty) return Int32Rect.Empty;

            var dirtyRect = LastSelectionRect;
            LastSelectionRect = Int32Rect.Empty;
            return dirtyRect;
        }
    }
}