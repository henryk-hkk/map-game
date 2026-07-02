using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Constants;
using MapGame.Core.Utils.Geographic;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace MapGame.Core.Utils.Graphic
{
    public static class OverlayCompositor
    {
        private const int SdfScale = 2;

        public static void InitializeCompositor()
        {
            var (width, height, _) = MapUtils.GetBitmapParams();
            int scaledWidth = width * SdfScale;
            int scaledHeight = height * SdfScale;
            int scaledStride = scaledWidth * 4;

            Map.MasterOverlayPixelData = new byte[scaledHeight * scaledStride];

            Map.OverlayMaterial = new PhongMaterial()
            {
                AmbientColor = new HelixToolkit.Maths.Color4(1, 1, 1, 0)
            };
        }

        public static void ComposeAndApply(Int32Rect dirtyRect)
        {
            var (width, height, _) = MapUtils.GetBitmapParams();
            int scaledWidth = width * SdfScale;
            int scaledHeight = height * SdfScale;
            int scaledStride = scaledWidth * 4;

            int startX = dirtyRect.X * SdfScale;
            int startY = dirtyRect.Y * SdfScale;
            int endX = (dirtyRect.X + dirtyRect.Width) * SdfScale;
            int endY = (dirtyRect.Y + dirtyRect.Height) * SdfScale;

            startX = Math.Max(0, startX);
            startY = Math.Max(0, startY);
            endX = Math.Min(scaledWidth, endX);
            endY = Math.Min(scaledHeight, endY);

            byte[] dest = Map.MasterOverlayPixelData;
            byte[] countries = Map.CountryPixelData;
            byte[] borders = Map.RegionBorderPixelData;
            byte[] selections = Map.SelectionPixelData;

            Parallel.For(startY, endY, y =>
            {
                int rowOffset = y * scaledStride;
                int startIdx = rowOffset + (startX * 4);
                int endIdx = rowOffset + (endX * 4);

                for (int i = startIdx; i < endIdx; i += 4)
                {
                    float rC = countries[i + 2], gC = countries[i + 1], bC = countries[i], aC = countries[i + 3] / 255f;

                    float aB = borders[i + 3] / 255f;
                    float r1 = (borders[i + 2] * aB) + (rC * (1 - aB));
                    float g1 = (borders[i + 1] * aB) + (gC * (1 - aB));
                    float b1 = (borders[i] * aB) + (bC * (1 - aB));
                    float alpha1 = aC + aB - (aC * aB);

                    float aS = selections[i + 3] / 255f;
                    float rFinal = (selections[i + 2] * aS) + (r1 * (1 - aS));
                    float gFinal = (selections[i + 1] * aS) + (g1 * (1 - aS));
                    float bFinal = (selections[i] * aS) + (b1 * (1 - aS));
                    float alphaFinal = alpha1 + aS - (alpha1 * aS);

                    dest[i] = (byte)bFinal;
                    dest[i + 1] = (byte)gFinal;
                    dest[i + 2] = (byte)rFinal;
                    dest[i + 3] = (byte)(alphaFinal * 255f);
                }
            });

            if (Map.OverlayMaterial is PhongMaterial phong)
            {
                phong.DiffuseMap = dest.ToFastDynamicTextureModel(scaledWidth, scaledHeight);
            }
        }
    }
}