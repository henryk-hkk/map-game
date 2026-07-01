using MapGame.Core.Constants;
using MapGame.Core.Utils.Geographic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Utils.Graphic
{
    public static class SelectionTexturesGenerator
    {
        public static void InitializeSelectionRendering()
        {
            var (width, height, stride) = MapUtils.GetBitmapParams();
            Map.SelectionPixelData = new byte[height * stride];
            Map.SelectionBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            ImageBrush brush = new ImageBrush(Map.SelectionBitmap);
            RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.NearestNeighbor);
            //brush.Freeze();
            Map.SelectionMaterial = new DiffuseMaterial(brush);
        }

        public static void SelectRegionByAreaColor(Color areaColor)
        {
            if (!Map.Areas.TryGetValue(areaColor, out PixelArea clickedArea)) return;

            int targetRegionId = (int)clickedArea.ParentRegionId;

            if (targetRegionId == Map.CurrentlySelectedRegionId) return;

            var (width, height, stride) = MapUtils.GetBitmapParams();

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
                            int idx = (pixel.Y * stride) + (pixel.X * 4);
                            Map.SelectionPixelData[idx] = 0;
                            Map.SelectionPixelData[idx + 1] = 0;
                            Map.SelectionPixelData[idx + 2] = 0;
                            Map.SelectionPixelData[idx + 3] = 0;

                            if (pixel.X < minX) minX = pixel.X;
                            if (pixel.X > maxX) maxX = pixel.X;
                            if (pixel.Y < minY) minY = pixel.Y;
                            if (pixel.Y > maxY) maxY = pixel.Y;
                            anyChanges = true;
                        }
                    }
                }
            }

            Map.CurrentlySelectedRegionId = targetRegionId;

            foreach (var area in Map.Areas.Values)
            {
                if (area.ParentRegionId == targetRegionId)
                {
                    foreach (var pixel in area.Pixels)
                    {
                        int idx = (pixel.Y * stride) + (pixel.X * 4);

                        Map.SelectionPixelData[idx] = 255;
                        Map.SelectionPixelData[idx + 1] = 255;
                        Map.SelectionPixelData[idx + 2] = 255;
                        Map.SelectionPixelData[idx + 3] = 50;

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
                int offset = (minY * stride) + (minX * 4);

                Map.SelectionBitmap.WritePixels(updateRect, Map.SelectionPixelData, stride, offset);
            }
        }

        public static void ClearSelection()
        {
            if (Map.CurrentlySelectedRegionId == -1) return;

            var (width, height, stride) = MapUtils.GetBitmapParams();

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            bool anyChanges = false;

            foreach (var area in Map.Areas.Values)
            {
                if (area.ParentRegionId == Map.CurrentlySelectedRegionId)
                {
                    foreach (var pixel in area.Pixels)
                    {
                        int idx = (pixel.Y * stride) + (pixel.X * 4);

                        Map.SelectionPixelData[idx] = 0;
                        Map.SelectionPixelData[idx + 1] = 0;
                        Map.SelectionPixelData[idx + 2] = 0;
                        Map.SelectionPixelData[idx + 3] = 0;

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
                int offset = (minY * stride) + (minX * 4);

                Map.SelectionBitmap.WritePixels(updateRect, Map.SelectionPixelData, stride, offset);
            }

            Map.CurrentlySelectedRegionId = -1;
        }
    }
}
