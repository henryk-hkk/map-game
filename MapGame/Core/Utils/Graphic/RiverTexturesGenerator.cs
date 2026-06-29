using MapGame.Core.Utils.Geographic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace MapGame.Core.Utils.Graphic
{
    public static class RiverTexturesGenerator
    {
        public static DiffuseMaterial GenerateAnimatedRivers(bool[] riverMask, BitmapImage waterTexture)
        {
            var (width, height, stride) = MapUtils.GetBitmapParams();
            var thinnedMask = GetThinnedMask(riverMask, width, height);
            var maskBitmap = GetOpacityMaskBrush(thinnedMask, width, height, stride);
            var waterBrush = GetAnimatedRiverTextureBrush(waterTexture, 20);
            return GetAnimatedRiversMaterial(maskBitmap, waterBrush, width, height);

        }
        private static DiffuseMaterial GetAnimatedRiversMaterial(ImageBrush opacityMaskBrush, ImageBrush animatedWaterTextureBrush, int width, int height)
        {
            Rectangle riverLayer = new Rectangle();
            riverLayer.Width = width;
            riverLayer.Height = height;

            // Wypełniamy prostokąt animowaną wodą
            riverLayer.Fill = animatedWaterTextureBrush;
            // Wycinamy kształt lądów zostawiając tylko rzeki
            riverLayer.OpacityMask = opacityMaskBrush;

            // Tworzymy pędzel z naszego prostokąta, by móc go użyć w 3D
            VisualBrush finalRiverBrush = new VisualBrush(riverLayer);
            RenderOptions.SetBitmapScalingMode(finalRiverBrush, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetCachingHint(finalRiverBrush, CachingHint.Cache);


            return new DiffuseMaterial(finalRiverBrush);
        }
        private static ImageBrush GetAnimatedRiverTextureBrush(BitmapImage waterTex, int animationSpeed)
        {
            ImageBrush waterBrush = new ImageBrush(waterTex);

            waterBrush.TileMode = TileMode.Tile;
            waterBrush.ViewportUnits = BrushMappingMode.Absolute;
            waterBrush.Viewport = new Rect(0, 0, 256, 256);
            RenderOptions.SetBitmapScalingMode(waterBrush, BitmapScalingMode.NearestNeighbor);

            TranslateTransform waterScroll = new TranslateTransform();
            waterBrush.Transform = waterScroll;

            DoubleAnimation flowAnim = new DoubleAnimation(0, 256, TimeSpan.FromSeconds(animationSpeed));
            flowAnim.RepeatBehavior = RepeatBehavior.Forever;

            waterScroll.BeginAnimation(TranslateTransform.XProperty, flowAnim);
            waterScroll.BeginAnimation(TranslateTransform.YProperty, flowAnim);

            return waterBrush;
        }

        private static ImageBrush GetOpacityMaskBrush(bool[] mask, int width, int height, int stride)
        {
            int totalPixels = width * height;

            byte[] alphaMaskPixels = new byte[totalPixels * 4];
            for (int i = 0; i < totalPixels; i++)
            {
                int idx = i * 4;
                if (mask[i])
                {
                    alphaMaskPixels[idx] = 255;     // B
                    alphaMaskPixels[idx + 1] = 255; // G
                    alphaMaskPixels[idx + 2] = 255; // R
                    alphaMaskPixels[idx + 3] = 255; // A
                }
                else
                {
                    alphaMaskPixels[idx + 3] = 0;
                }
            }
            WriteableBitmap maskBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            maskBitmap.WritePixels(new Int32Rect(0, 0, width, height), alphaMaskPixels, stride, 0);
            maskBitmap.Freeze();

            ImageBrush opacityMaskBrush = new ImageBrush(maskBitmap);
            opacityMaskBrush.Freeze();
            return opacityMaskBrush;
        }
        private static bool[] GetErodedMask(bool[] mask, int erosionPasses, int width, int height)
        {
            bool[] isRiver = (bool[])mask.Clone();
            for (int pass = 0; pass < erosionPasses; pass++)
            {
                bool[] nextIsRiver = (bool[])isRiver.Clone();

                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int idx = y * width + x;

                        if (isRiver[idx])
                        {
                            bool touchesLand = !isRiver[idx - width] || !isRiver[idx + width] || !isRiver[idx - 1] || !isRiver[idx + 1];
                            if (touchesLand)
                            {
                                nextIsRiver[idx] = false;
                            }
                        }
                    }
                }
                isRiver = nextIsRiver;
            }
            return isRiver;
        }

        private static bool[] GetThinnedMask(bool[] mask, int width, int height)
        { //Zhang Suen Thinning
            bool[] grid = (bool[])mask.Clone();
            bool changed = true;

            List<(int x, int y)> pixelsToRemove = new List<(int x, int y)>();

            while (changed)
            {
                changed = false;
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int idx = y * width + x;
                        if (!grid[idx]) continue;

                        // Pobieramy 8 sąsiadów zgodnie z konwencją algorytmu (P2 - P9)
                        int p2 = grid[idx - width] ? 1 : 0;         // Góra
                        int p3 = grid[idx - width + 1] ? 1 : 0;     // Prawa-Góra
                        int p4 = grid[idx + 1] ? 1 : 0;             // Prawo
                        int p5 = grid[idx + width + 1] ? 1 : 0;     // Prawa-Dół
                        int p6 = grid[idx + width] ? 1 : 0;         // Dół
                        int p7 = grid[idx + width - 1] ? 1 : 0;     // Lewa-Dół
                        int p8 = grid[idx - 1] ? 1 : 0;             // Lewo
                        int p9 = grid[idx - width - 1] ? 1 : 0;     // Lewa-Góra

                        // Liczba niezerowych sąsiadów
                        int bp = p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9;
                        if (bp < 2 || bp > 6) continue;

                        // Liczba przejść 0 -> 1 w sekwencji kołowej (P2->P3->...->P9->P2)
                        int ap = 0;
                        if (p2 == 0 && p3 == 1) ap++;
                        if (p3 == 0 && p4 == 1) ap++;
                        if (p4 == 0 && p5 == 1) ap++;
                        if (p5 == 0 && p6 == 1) ap++;
                        if (p6 == 0 && p7 == 1) ap++;
                        if (p7 == 0 && p8 == 1) ap++;
                        if (p8 == 0 && p9 == 1) ap++;
                        if (p9 == 0 && p2 == 1) ap++;

                        if (ap != 1) continue;

                        // Warunki specyficzne dla Podkroku 1
                        if (p2 * p4 * p6 != 0) continue;
                        if (p4 * p6 * p8 != 0) continue;

                        pixelsToRemove.Add((x, y));
                    }
                }

                // Usuwamy zakwalifikowane piksele
                if (pixelsToRemove.Count > 0)
                {
                    foreach (var p in pixelsToRemove) grid[p.y * width + p.x] = false;
                    pixelsToRemove.Clear();
                    changed = true;
                }

                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int idx = y * width + x;
                        if (!grid[idx]) continue;

                        int p2 = grid[idx - width] ? 1 : 0;
                        int p3 = grid[idx - width + 1] ? 1 : 0;
                        int p4 = grid[idx + 1] ? 1 : 0;
                        int p5 = grid[idx + width + 1] ? 1 : 0;
                        int p6 = grid[idx + width] ? 1 : 0;
                        int p7 = grid[idx + width - 1] ? 1 : 0;
                        int p8 = grid[idx - 1] ? 1 : 0;
                        int p9 = grid[idx - width - 1] ? 1 : 0;

                        int bp = p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9;
                        if (bp < 2 || bp > 6) continue;

                        int ap = 0;
                        if (p2 == 0 && p3 == 1) ap++;
                        if (p3 == 0 && p4 == 1) ap++;
                        if (p4 == 0 && p5 == 1) ap++;
                        if (p5 == 0 && p6 == 1) ap++;
                        if (p6 == 0 && p7 == 1) ap++;
                        if (p7 == 0 && p8 == 1) ap++;
                        if (p8 == 0 && p9 == 1) ap++;
                        if (p9 == 0 && p2 == 1) ap++;

                        if (ap != 1) continue;

                        // Warunki specyficzne (inna kombinacja sąsiadów)
                        if (p2 * p4 * p8 != 0) continue;
                        if (p2 * p6 * p8 != 0) continue;

                        pixelsToRemove.Add((x, y));
                    }
                }

                // Usuwamy zakwalifikowane piksele 
                if (pixelsToRemove.Count > 0)
                {
                    foreach (var p in pixelsToRemove) grid[p.y * width + p.x] = false;
                    pixelsToRemove.Clear();
                    changed = true;
                }
            }

            return grid;
        }
    }
}
