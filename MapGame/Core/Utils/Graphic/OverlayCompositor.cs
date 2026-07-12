using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Utils.Geographic;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace MapGame.Core.Utils.Graphic
{
    public static class OverlayCompositor
    {
        private const int SdfScale = GraphicContext.SdfScale;
        private const float inv255 = 1f / 255f;

        public static void InitializeCompositor()
        {
            var (_, scaledHeight, scaledStride) = MapUtils.GetScaledBitmapParams();

            GraphicContext.MasterOverlayPixelData = new byte[scaledHeight * scaledStride];

            GraphicContext.OverlayMaterial = new PhongMaterial()
            { 
                AmbientColor = new HelixToolkit.Maths.Color4(1, 1, 1, 0)
            };
        }

        public static void ComposeAndApply(Int32Rect dirtyRect)
        {
            var (scaledWidth, scaledHeight, scaledStride) = MapUtils.GetScaledBitmapParams();

            int startX = dirtyRect.X * SdfScale;
            int startY = dirtyRect.Y * SdfScale;
            int endX = (dirtyRect.X + dirtyRect.Width) * SdfScale;
            int endY = (dirtyRect.Y + dirtyRect.Height) * SdfScale;

            startX = Math.Max(0, startX);
            startY = Math.Max(0, startY);
            endX = Math.Min(scaledWidth, endX);
            endY = Math.Min(scaledHeight, endY);

            byte[] dest = GraphicContext.MasterOverlayPixelData;
            byte[] countries = GraphicContext.CountryPixelData;
            byte[] borders = GraphicContext.RegionBorderPixelData;
            byte[] selections = GraphicContext.SelectionPixelData;

            Parallel.For(startY, endY, y =>
            {
                int rowOffset = y * scaledStride;
                int startIdx = rowOffset + (startX * 4);
                int endIdx = rowOffset + (endX * 4);

                for (int i = startIdx; i < endIdx; i += 4)
                {
                    float bC = countries[i], gC = countries[i + 1], rC = countries[i + 2], aC = countries[i + 3] * inv255;

                    float aB = borders[i + 3] * inv255;
                    float alpha1 = aB + aC * (1 - aB);

                    float r1 = 0, g1 = 0, b1 = 0;
                    if (alpha1 > 0)
                    {
                        float invAlpha1 = 1f / alpha1;

                        r1 = (borders[i + 2] * aB + rC * aC * (1 - aB)) * invAlpha1;
                        g1 = (borders[i + 1] * aB + gC * aC * (1 - aB)) * invAlpha1;
                        b1 = (borders[i] * aB + bC * aC * (1 - aB)) * invAlpha1;
                    }

                    float aS = selections[i + 3] * inv255;
                    float alphaFinal = aS + alpha1 * (1 - aS);

                    float rFinal = 0, gFinal = 0, bFinal = 0;
                    if (alphaFinal > 0)
                    {
                        float invAlphaFinal = 1f / alphaFinal;
                        rFinal = (selections[i + 2] * aS + r1 * alpha1 * (1 - aS)) * invAlphaFinal;
                        gFinal = (selections[i + 1] * aS + g1 * alpha1 * (1 - aS)) * invAlphaFinal;
                        bFinal = (selections[i] * aS + b1 * alpha1 * (1 - aS)) * invAlphaFinal;
                    }

                    dest[i] = (byte)bFinal;
                    dest[i + 1] = (byte)gFinal;
                    dest[i + 2] = (byte)rFinal;
                    dest[i + 3] = (byte)(alphaFinal * 255f);
                }
            });

            if (GraphicContext.OverlayMaterial is PhongMaterial phong)
            {
                phong.DiffuseMap = dest.ToFastDynamicTextureModel(scaledWidth, scaledHeight);
            }
        }
    }
}