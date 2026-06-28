using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Utils.Graphic
{
    public static class SDFAgent
    {
        public static byte[] GetSmoothSDFBorders(int[] regionMap, int width, int height, int scale)
        {
            int scaledWidth = width * scale;
            int scaledHeight = height * scale;
            int totalScaledPixels = scaledWidth * scaledHeight;

            float[] distances = new float[totalScaledPixels];
            Array.Fill(distances, float.MaxValue); // At first the distance to border is maximum everywhere.

            MarkBorderPixels(regionMap, distances, width, height, scale);

            ChamferDistanceTransform(distances, width, height, scale);

            ApplyBlur(distances, totalScaledPixels, width, height, scale);
            byte[] borderPixels = new byte[totalScaledPixels * 4];

            ApplySmoothstep(borderPixels, distances, totalScaledPixels, scale);

            return borderPixels;
        }

            
        private static void MarkBorderPixels(int[] regionMap, float[] distances, int width, int height, int scale)
        { //Marks the border pixels as distance = 0
            int scaledWidth = width * scale;
            int scaledHeight = height * scale;
        
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int index1D = (y * width) + x;
                    int currentRegion = regionMap[index1D];

                    if (currentRegion == -1) continue; // Ignoring water

                    int rightRegion = regionMap[index1D + 1];
                    int bottomRegion = regionMap[index1D + width];

                    if (currentRegion != rightRegion)
                    {
                        int sx = (x + 1) * scale;
                        for (int dy = 0; dy < scale; dy++)
                        {
                            distances[(y * scale + dy) * scaledWidth + sx] = 0f;
                        }
                    }

                    if (currentRegion != bottomRegion)
                    {
                        int sy = (y + 1) * scale;
                        for (int dx = 0; dx < scale; dx++)
                        {
                            distances[sy * scaledWidth + (x * scale + dx)] = 0f;
                        }
                    }
                }
            }
        }

        private static void ChamferDistanceTransform(float[] distances, int width, int height, int scale)
        {
            int scaledWidth = width * scale;
            int scaledHeight = height * scale;

            // Top to bottom, left to right
            for (int y = 0; y < scaledHeight; y++)
            {
                for (int x = 0; x < scaledWidth; x++)
                {
                    int i = y * scaledWidth + x;
                    if (distances[i] == 0f) continue;

                    float min = distances[i];

                    if (x > 0) min = Math.Min(min, distances[i - 1] + 1f);
                    if (y > 0) min = Math.Min(min, distances[i - scaledWidth] + 1f);

                    // Diagonals
                    if (x > 0 && y > 0)
                        min = Math.Min(min, distances[i - scaledWidth - 1] + 1.414f);
                    if (x < scaledWidth - 1 && y > 0)
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

                    if (x < scaledWidth - 1) min = Math.Min(min, distances[i + 1] + 1f);
                    if (y < scaledHeight - 1) min = Math.Min(min, distances[i + scaledWidth] + 1f);

                    // Diagonals, again
                    if (x < scaledWidth - 1 && y < scaledHeight - 1)
                        min = Math.Min(min, distances[i + scaledWidth + 1] + 1.414f);
                    if (x > 0 && y < scaledHeight - 1)
                        min = Math.Min(min, distances[i + scaledWidth - 1] + 1.414f);

                    distances[i] = min;
                }
            }
        }

        private static void ApplyBlur(float[] distances, int totalScaledPixels, int width, int height, int scale)
        {
            int scaledWidth = width * scale;
            int scaledHeight = height * scale;

            int blurRadius = scale;
            float[] blurredDistances = new float[totalScaledPixels];

            // Horizontal pass
            Parallel.For(0, scaledHeight, y =>
            {
                int rowStart = y * scaledWidth;
                float sum = 0;
                int count = 0;

                // Initial window on the edge
                for (int dx = 0; dx <= blurRadius && dx < scaledWidth; dx++)
                {
                    sum += distances[rowStart + dx];
                    count++;
                }

                // Moving window through the row
                for (int x = 0; x < scaledWidth; x++)
                {
                    blurredDistances[rowStart + x] = sum / count;

                    // subtracting pixel that left the window (from the left)
                    int leftIndex = x - blurRadius;
                    if (leftIndex >= 0)
                    {
                        sum -= distances[rowStart + leftIndex];
                        count--;
                    }

                    // adding pixel that entered the window
                    int rightIndex = x + blurRadius + 1;
                    if (rightIndex < scaledWidth)
                    {
                        sum += distances[rowStart + rightIndex];
                        count++;
                    }
                }
            });

            Parallel.For(0, scaledWidth, x =>
            {
                float sum = 0;
                int count = 0;

                // Window initialization on the edge
                for (int dy = 0; dy <= blurRadius && dy < scaledHeight; dy++)
                {
                    sum += blurredDistances[dy * scaledWidth + x];
                    count++;
                }

                // Moving the window through the column
                for (int y = 0; y < scaledHeight; y++)
                {
                    distances[y * scaledWidth + x] = sum / count;

                    int topIndex = y - blurRadius;
                    if (topIndex >= 0)
                    {
                        sum -= blurredDistances[topIndex * scaledWidth + x];
                        count--;
                    }

                    int bottomIndex = y + blurRadius + 1;
                    if (bottomIndex < scaledHeight)
                    {
                        sum += blurredDistances[bottomIndex * scaledWidth + x];
                        count++;
                    }
                }
            });
        }
        private static void ApplySmoothstep(byte[] borderPixels, float[] distances, int totalScaledPixels, int scale)
        {
            float borderThickness = 0.1f;
            float smoothRadius = scale * 1f;

            float maxDistance = borderThickness + smoothRadius;

            Parallel.For(0, totalScaledPixels, i =>
            {
                float dist = distances[i];

                if (dist >= maxDistance) return;

                byte alpha = 0;

                if (dist <= borderThickness)
                {
                    alpha = 255;
                }
                else
                {
                    // Smoothstep
                    float t = (dist - borderThickness) / smoothRadius;
                    float smoothT = t * t * (3f - 2f * t);
                    alpha = (byte)(255f * (1f - smoothT));
                }

                if (alpha > 0)
                {
                    int byteIndex = i * 4;
                    GraphicUtils.ColorPixel(borderPixels, byteIndex, 0, 0, 0, alpha);
                }
            });
        }
    }
}
