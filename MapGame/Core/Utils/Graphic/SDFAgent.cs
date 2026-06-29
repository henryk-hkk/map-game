using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Utils.Graphic
{
    public static class SDFAgent
    {

        public const float BorderThickness = 0.1f;
        public const float SmoothRadiusMultiplier = 1f;

        public static List<(int Index, byte Alpha)> ComputeLocalSDF(
            int[] regionMap,
            int mapWidth,
            int mapHeight,
            int scale,
            Int32Rect updateRect) // Obszar oryginalnej (nieskalowanej) mapy!
        {
            float maxDistance = BorderThickness + (scale * SmoothRadiusMultiplier);
            int margin = (int)Math.Ceiling(maxDistance) + scale; // Zapas na rozmycie

            // Obliczamy "okno robocze" w oryginalnej rozdzielczości z marginesem bezpieczeństwa
            int startX = Math.Max(0, updateRect.X - margin);
            int startY = Math.Max(0, updateRect.Y - margin);
            int endX = Math.Min(mapWidth - 1, updateRect.X + updateRect.Width + margin);
            int endY = Math.Min(mapHeight - 1, updateRect.Y + updateRect.Height + margin);

            int localWidth = endX - startX;
            int localHeight = endY - startY;

            // Przechodzimy na rozdzielczość "skalowaną" (jak u Ciebie w starym kodzie)
            int scaledLocalWidth = localWidth * scale;
            int scaledLocalHeight = localHeight * scale;
            int scaledTotal = scaledLocalWidth * scaledLocalHeight;

            // Alokujemy znacznie mniejsze tablice robocze (np. 300x300 zamiast 12000x8000!)
            float[] localDistances = new float[scaledTotal];
            Array.Fill(localDistances, float.MaxValue);

            // KROK 1: Lokalizacja krawędzi (tylko w oknie)
            MarkLocalBorderPixels(regionMap, localDistances, mapWidth, startX, startY, endX, endY, scale, scaledLocalWidth);

            // KROK 2: Chamfer Distance Transform (na małym fragmencie)
            ChamferDistanceTransform(localDistances, scaledLocalWidth, scaledLocalHeight);

            // KROK 3: Rozmycie lokalne
            ApplyBlur(localDistances, scaledTotal, scaledLocalWidth, scaledLocalHeight, scale);

            // KROK 4: Smoothstep i wyciągnięcie wyników wprost do postaci listy dla segmentów
            return ExtractSdfPixels(localDistances, startX, startY, mapWidth, scale, scaledLocalWidth, scaledLocalHeight);
        }


        private static void MarkLocalBorderPixels(int[] regionMap, float[] distances, int globalWidth, int startX, int startY, int endX, int endY, int scale, int scaledLocalWidth)
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

                    // Tłumaczenie współrzędnych globalnych na nasze okno lokalne
                    int localX = x - startX;
                    int localY = y - startY;

                    if (currentRegion != rightRegion)
                    {
                        int sx = (localX + 1) * scale;
                        for (int dy = 0; dy < scale; dy++)
                            distances[(localY * scale + dy) * scaledLocalWidth + sx] = 0f;
                    }

                    if (currentRegion != bottomRegion)
                    {
                        int sy = (localY + 1) * scale;
                        for (int dx = 0; dx < scale; dx++)
                            distances[sy * scaledLocalWidth + (localX * scale + dx)] = 0f;
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

        private static void ApplyBlur(float[] distances, int totalScaledPixels, int scaledWidth, int scaledHeight, int scale)
        {

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
        private static List<(int, byte)> ExtractSdfPixels(float[] distances, int startX, int startY, int globalWidth, int scale, int scaledLocalWidth, int scaledLocalHeight)
        {
            List<(int, byte)> sdfResults = new List<(int, byte)>();

            float smoothRadius = scale * SmoothRadiusMultiplier;
            float maxDistance = BorderThickness + smoothRadius;

            // Używamy standardowej pętli, gdyż ConcurrentBag (dla Parallel) powoduje alokacje i nie zachowuje kolejności
            for (int y = 0; y < scaledLocalHeight; y++)
            {
                for (int x = 0; x < scaledLocalWidth; x++)
                {
                    int i = y * scaledLocalWidth + x;
                    float dist = distances[i];

                    if (dist >= maxDistance) continue;

                    byte alpha = 0;

                    if (dist <= BorderThickness)
                    {
                        alpha = 255;
                    }
                    else
                    {
                        float t = (dist - BorderThickness) / smoothRadius;
                        float smoothT = t * t * (3f - 2f * t);
                        alpha = (byte)(255f * (1f - smoothT));
                    }

                    if (alpha > 0)
                    {
                        // Przeliczenie z okna lokalnego na PRAWDZIWY globalny indeks 1D na płótnie!
                        int globalY = (startY * scale) + y;
                        int globalX = (startX * scale) + x;

                        int globalScaledWidth = globalWidth * scale; // Zakładam, że Twoja docelowa tekstura borderPixels jest przeskalowana
                        int globalByteIndex = ((globalY * globalScaledWidth) + globalX) * 4;

                        sdfResults.Add((globalByteIndex, alpha));
                    }
                }
            }
            return sdfResults;
        }
    }
}
