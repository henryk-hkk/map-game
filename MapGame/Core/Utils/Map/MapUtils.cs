using MapGame.Core.Geographic;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Utils.Map
{
    public static class MapUtils
    {
        private const int _strideFactor = 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int width, int height, int stride) GetBitmapParams()
        {
            return (MapContext.Width, MapContext.Height, MapContext.Width * _strideFactor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int scaledWidth, int scaledHeight, int scaledStride) GetScaledBitmapParams()
        {
            return
                (MapContext.Width * GraphicContext.SdfScale,
                MapContext.Height * GraphicContext.SdfScale,
                MapContext.Width * _strideFactor * GraphicContext.SdfScale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color GetColorByPosition(Position pos)
        {
            Color def = Color.FromArgb(0, 0, 0, 0);
            if (pos == null) return def;
            if (pos.IsOutOfMap) return def;

            int stride = MapContext.Width * 4;
            int index = ((int)pos.Y * stride) + ((int)pos.X * 4);

            byte b = GraphicContext.AreaPixels[index];
            byte g = GraphicContext.AreaPixels[index + 1];
            byte r = GraphicContext.AreaPixels[index + 2];
            byte a = GraphicContext.AreaPixels[index + 3];

            return Color.FromArgb(a, r, g, b);
        }
        public static int[] GetRegionMap(int width, int height)
        {
            int totalPixels = width * height;

            int[] regionMap = new int[totalPixels];


            for (int i = 0; i < totalPixels; i++)
            {
                int byteIndex = i * 4;

                // Jeśli to woda, oznaczamy ją jako id = -1
                if (GraphicContext.AreaPixels[byteIndex] == 0 && GraphicContext.AreaPixels[byteIndex + 1] == 0 && GraphicContext.AreaPixels[byteIndex + 2] == 0)
                {
                    regionMap[i] = -1;
                    continue;
                }

                Color c = Color.FromRgb(GraphicContext.AreaPixels[byteIndex + 2], GraphicContext.AreaPixels[byteIndex + 1], GraphicContext.AreaPixels[byteIndex]);
                if (GraphicContext.AreaColors.TryGetValue(c, out PixelArea area))
                {
                    if (area.ParentRegionId.HasValue)
                    {
                        regionMap[i] = (int)area.ParentRegionId;
                    }
                    else
                    {
                        regionMap[i] = -Math.Abs(c.GetHashCode());
                    }
                }
                else
                {
                    regionMap[i] = -2;
                }
            }
            return regionMap;
        }

        public static Dictionary<(Color, Color), BorderPixelSegment> GetAreaBorderPixels()
        {
            var (width, height, stride) = GetBitmapParams();

            var borderGraph = new Dictionary<(Color, Color), BorderPixelSegment>();

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int index = (y * stride) + (x * 4);

                    if (GraphicContext.AreaPixels[index] == 0 && GraphicContext.AreaPixels[index + 1] == 0 && GraphicContext.AreaPixels[index + 2] == 0) // Ignore #000 (Water)
                        continue;

                    byte b = GraphicContext.AreaPixels[index];
                    byte g = GraphicContext.AreaPixels[index + 1];
                    byte r = GraphicContext.AreaPixels[index + 2];
                    Color color = Color.FromRgb(r, g, b);

                    int[] neighborIndices = [
                        index - stride, // Up
                        index + stride, // Down
                        index - 4,      // Left
                        index + 4       // Right
                    ];

                    foreach (int nIndex in neighborIndices)
                    {
                        if (GraphicContext.AreaPixels[nIndex] == 0 && GraphicContext.AreaPixels[nIndex + 1] == 0 && GraphicContext.AreaPixels[nIndex + 2] == 0)
                            continue;

                        byte nB = GraphicContext.AreaPixels[nIndex];
                        byte nG = GraphicContext.AreaPixels[nIndex + 1];
                        byte nR = GraphicContext.AreaPixels[nIndex + 2];

                        if (nR != r || nG != g || nB != b)
                        {
                            Color neighborColor = Color.FromRgb(nR, nG, nB);

                            var segmentKey = CreateSortedKey(color, neighborColor);

                            if (!borderGraph.TryGetValue(segmentKey, out BorderPixelSegment? value))
                            {
                                BorderPixelSegment newSegment = new()
                                {
                                    Area1 = segmentKey.Item1,
                                    Area2 = segmentKey.Item2
                                };
                                value = newSegment;
                                borderGraph[segmentKey] = value;

                                GraphicContext.AreaColors[segmentKey.Item1].BorderPixelSegments.Add(newSegment);
                                GraphicContext.AreaColors[segmentKey.Item2].BorderPixelSegments.Add(newSegment);
                            }

                            value.PixelIndices.Add(index);
                        }
                    }
                }
            }
            return borderGraph;
        }
        private static (Color, Color) CreateSortedKey(Color c1, Color c2)
        {
            int val1 = (c1.R << 16) | (c1.G << 8) | c1.B;
            int val2 = (c2.R << 16) | (c2.G << 8) | c2.B;

            return val1 < val2 ? (c1, c2) : (c2, c1);
        }

        public static int[] GetCountryMap(int width, int height)
        {
            int[] countryMap = new int[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = (y * width) + x;
                    int stride = width * 4;
                    int byteIdx = (y * stride) + (x * 4);

                    if (GraphicContext.AreaPixels[byteIdx] == 0 && GraphicContext.AreaPixels[byteIdx + 1] == 0 && GraphicContext.AreaPixels[byteIdx + 2] == 0)
                    {
                        countryMap[idx] = -1;
                        continue;
                    }

                    Color c = Color.FromRgb(GraphicContext.AreaPixels[byteIdx + 2], GraphicContext.AreaPixels[byteIdx + 1], GraphicContext.AreaPixels[byteIdx]);
                    if (!GraphicContext.AreaColors.TryGetValue(c, out PixelArea? area)) continue;
                    if (area == null || area.ParentRegionId == null) continue;

                    var region = MapLogicContext.RegionIds[(int)area.ParentRegionId];

                    if (region?.Owner != null)
                    {
                        countryMap[idx] = region.Owner.Identifier.GetHashCode();
                    }
                    else
                    {
                        countryMap[idx] = -2;
                    }
                }
            }
            return countryMap;
        }
    }
}
