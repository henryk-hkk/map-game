using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Utils.Graphic
{
    public static class SDFAgent
    {
        public static float BorderThickness = 0.1f;
        public static float SmoothRadiusMultiplier = 1f;
        private const int _scale = GraphicContext.SdfScale;

        private const int ParallelThreshold = 10000;

        public static List<(int Index, byte Alpha)> ComputeLocalSDF(
            int[] regionMap,
            int mapWidth,
            int mapHeight,
            Int32Rect updateRect)
        {
            float maxDistance = BorderThickness + (_scale * SmoothRadiusMultiplier);
            int margin = (int)Math.Ceiling(maxDistance) + _scale;

            int startX = Math.Max(0, updateRect.X - margin);
            int startY = Math.Max(0, updateRect.Y - margin);
            int endX = Math.Min(mapWidth - 1, updateRect.X + updateRect.Width + margin);
            int endY = Math.Min(mapHeight - 1, updateRect.Y + updateRect.Height + margin);

            int localWidth = endX - startX;
            int localHeight = endY - startY;

            int scaledLocalWidth = localWidth * _scale;
            int scaledLocalHeight = localHeight * _scale;
            int scaledTotal = scaledLocalWidth * scaledLocalHeight;

            float[] localDistances = ArrayPool<float>.Shared.Rent(scaledTotal);

            try
            {
                Array.Fill(localDistances, float.MaxValue, 0, scaledTotal);

                MarkLocalBorderPixels(regionMap, localDistances, mapWidth, startX, startY, endX, endY, scaledLocalWidth);
                ChamferDistanceTransform(localDistances, scaledLocalWidth, scaledLocalHeight);
                ApplyBlur(localDistances, scaledTotal, scaledLocalWidth, scaledLocalHeight);

                return ExtractSDFPixels(localDistances, startX, startY, mapWidth, scaledLocalWidth, scaledLocalHeight);
            }
            finally
            {
                ArrayPool<float>.Shared.Return(localDistances);
            }
        }


        private static void MarkLocalBorderPixels(int[] regionMap, float[] distances, int globalWidth, int startX, int startY, int endX, int endY, int scaledLocalWidth)
        { //Marks the border pixels as distance = 0
            for (int y = startY; y < endY - 1; y++)
            {
                for (int x = startX; x < endX - 1; x++)
                {
                    int index1D = (y * globalWidth) + x;
                    int currentRegion = regionMap[index1D];

                    if (currentRegion == -1) continue; // Woda

                    int rightRegion = regionMap[index1D + 1];
                    int bottomRegion = regionMap[index1D + globalWidth];

                    int localX = x - startX;
                    int localY = y - startY;

                    if (currentRegion != rightRegion)
                    {
                        int sx = (localX + 1) * _scale;
                        for (int dy = 0; dy < _scale; dy++)
                            distances[(localY * _scale + dy) * scaledLocalWidth + sx] = 0f;
                    }

                    if (currentRegion != bottomRegion)
                    {
                        int sy = (localY + 1) * _scale;
                        for (int dx = 0; dx < _scale; dx++)
                            distances[sy * scaledLocalWidth + (localX * _scale + dx)] = 0f;
                    }
                }
            }
        }

        private static void ChamferDistanceTransform(float[] distances, int scaledWidth, int scaledHeight)
        {

            // Top to bottom, left to right
            for (int y = 0; y < scaledHeight; y++)
            {
                for (int x = 0; x < scaledWidth; x++)
                {
                    int i = y * scaledWidth + x;
                   
                    float min = distances[i];
                    if (min == 0f) continue;

                    bool xGt0 = x > 0;
                    bool yGt0 = y > 0;

                    if (xGt0) min = Math.Min(min, distances[i - 1] + 1f);
                    if (yGt0) min = Math.Min(min, distances[i - scaledWidth] + 1f);

                    // Diagonals
                    if (xGt0 && yGt0)
                        min = Math.Min(min, distances[i - scaledWidth - 1] + 1.414f);
                    if (x < scaledWidth - 1 && yGt0)
                        min = Math.Min(min, distances[i - scaledWidth + 1] + 1.414f);

                    distances[i] = min;
                }
            }

            // Bottom to top, right to left
            for (int y = scaledHeight - 1; y >= 0; y--)
            {
                for (int x = scaledWidth - 1; x >= 0; x--)
                {
                    int i = y * scaledWidth + x;

                    float min = distances[i];

                    bool xLtMax = x < scaledWidth - 1;
                    bool yLtMax = y < scaledHeight - 1;

                    if (xLtMax) min = Math.Min(min, distances[i + 1] + 1f);
                    if (yLtMax) min = Math.Min(min, distances[i + scaledWidth] + 1f);

                    // Diagonals, again
                    if (xLtMax && yLtMax)
                        min = Math.Min(min, distances[i + scaledWidth + 1] + 1.414f);
                    if (x > 0 && yLtMax)
                        min = Math.Min(min, distances[i + scaledWidth - 1] + 1.414f);

                    distances[i] = min;
                }
            }
        }

        private static void ApplyBlur(float[] distances, int totalScaledPixels, int scaledWidth, int scaledHeight)
        {
            int blurRadius = _scale;
            float[] blurredDistances = ArrayPool<float>.Shared.Rent(totalScaledPixels);

            try
            {
                if (totalScaledPixels > ParallelThreshold)
                {
                    Parallel.For(0, scaledHeight, y => BlurHorizontal(y, distances, blurredDistances, scaledWidth, blurRadius));
                    Parallel.For(0, scaledWidth, x => BlurVertical(x, distances, blurredDistances, scaledWidth, scaledHeight, blurRadius));
                }
                else
                {
                    for (int y = 0; y < scaledHeight; y++) BlurHorizontal(y, distances, blurredDistances, scaledWidth, blurRadius);
                    for (int x = 0; x < scaledWidth; x++) BlurVertical(x, distances, blurredDistances, scaledWidth, scaledHeight, blurRadius);
                }
            }
            finally
            {
                ArrayPool<float>.Shared.Return(blurredDistances);
            }
        }

        private static void BlurHorizontal(int y, float[] src, float[] dst, int width, int radius)
        {
            int rowStart = y * width;
            float sum = 0;
            int count = 0;
            // Initial window on the edge
            for (int dx = 0; dx <= radius && dx < width; dx++)
            {
                sum += src[rowStart + dx];
                count++;
            }
            // Moving window through the row

            for (int x = 0; x < width; x++)
            {
                dst[rowStart + x] = sum / count;

                // subtracting pixel that left the window (from the left)
                int leftIndex = x - radius;
                if (leftIndex >= 0) { sum -= src[rowStart + leftIndex]; count--; }

                // adding pixel that entered the window
                int rightIndex = x + radius + 1;
                if (rightIndex < width) { sum += src[rowStart + rightIndex]; count++; }
            }
        }

        private static void BlurVertical(int x, float[] dst, float[] src, int width, int height, int radius)
        {
            float sum = 0;
            int count = 0;
            // Window initialization on the edge
            for (int dy = 0; dy <= radius && dy < height; dy++)
            {
                sum += src[dy * width + x];
                count++;
            }

            // Moving the window through the column
            for (int y = 0; y < height; y++)
            {
                dst[y * width + x] = sum / count;

                int topIndex = y - radius;
                if (topIndex >= 0) { sum -= src[topIndex * width + x]; count--; }

                int bottomIndex = y + radius + 1;
                if (bottomIndex < height) { sum += src[bottomIndex * width + x]; count++; }
            }
        }

        private static List<(int, byte)> ExtractSDFPixels(float[] distances, int startX, int startY, int globalWidth, int scaledLocalWidth, int scaledLocalHeight)
        {
            int estimatedCount = (scaledLocalWidth * scaledLocalHeight) / 10;
            List<(int, byte)> sdfResults = new(estimatedCount);

            float smoothRadius = _scale * SmoothRadiusMultiplier;
            float maxDistance = BorderThickness + smoothRadius;

            float invSmoothRadius = 1f / smoothRadius;
            int globalScaledWidth = globalWidth * _scale;
            int startYGlobalOffset = startY * _scale;
            int startXGlobalOffset = startX * _scale;

            for (int y = 0; y < scaledLocalHeight; y++)
            {
                // local window index -> global index
                int rowOffset = y * scaledLocalWidth;
                int globalYOffset = (startYGlobalOffset + y) * globalScaledWidth;

                for (int x = 0; x < scaledLocalWidth; x++)
                {
                    float dist = distances[rowOffset + x];

                    if (dist >= maxDistance) continue;

                    byte alpha;
                    if (dist <= BorderThickness)
                    {
                        alpha = 255;
                    }
                    else
                    {
                        float t = (dist - BorderThickness) * invSmoothRadius;
                        float smoothT = t * t * (3f - 2f * t);
                        alpha = (byte)(255f - (255f * smoothT));
                    }
                    if (alpha > 0)
                    {
                        int globalByteIndex = (globalYOffset + startXGlobalOffset + x) * 4;
                        sdfResults.Add((globalByteIndex, alpha));
                    }
                }
            }
            return sdfResults;
        }
    }
}
