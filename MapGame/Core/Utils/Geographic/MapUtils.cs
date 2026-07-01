using MapGame.Core.Constants;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Utils.Geographic
{
    public static class MapUtils
    {
        private const int _strideFactor = 4;
        public static (int width, int height, int stride) GetBitmapParams()
        {
            return (Map.Width, Map.Height, Map.Width * _strideFactor);
        }

        public static int[] GetRegionMap(int width, int height)
        {
            int totalPixels = width * height;

            int[] regionMap = new int[totalPixels];


            for (int i = 0; i < totalPixels; i++)
            {
                int byteIndex = i * 4;

                // Jeśli to czarna woda, oznaczamy ją jako StateID = -1
                if (Map.AreaPixels[byteIndex] == 0 && Map.AreaPixels[byteIndex + 1] == 0 && Map.AreaPixels[byteIndex + 2] == 0)
                {
                    regionMap[i] = -1;
                    continue;
                }

                Color c = Color.FromRgb(Map.AreaPixels[byteIndex + 2], Map.AreaPixels[byteIndex + 1], Map.AreaPixels[byteIndex]);
                if (Map.Areas.TryGetValue(c, out PixelArea area))
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
            var (width, height, stride) = MapUtils.GetBitmapParams();

            byte[] borderPixels = new byte[height * stride];
            List<int> borderPixelIndices = [];
            var borderGraph = new Dictionary<(Color, Color), BorderPixelSegment>();

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int index = (y * stride) + (x * 4);

                    if (Map.AreaPixels[index] == 0 && Map.AreaPixels[index + 1] == 0 && Map.AreaPixels[index + 2] == 0) // Ignore #000 (Water)
                        continue;

                    byte b = Map.AreaPixels[index];
                    byte g = Map.AreaPixels[index + 1];
                    byte r = Map.AreaPixels[index + 2];
                    Color color = Color.FromRgb(r, g, b);

                    int[] neighborIndices = {
                        index - stride, // Up
                        index + stride, // Down
                        index - 4,      // Left
                        index + 4       // Right
                    };

                    foreach (int nIndex in neighborIndices)
                    {
                        if (Map.AreaPixels[nIndex] == 0 && Map.AreaPixels[nIndex + 1] == 0 && Map.AreaPixels[nIndex + 2] == 0)
                            continue;

                        byte nB = Map.AreaPixels[nIndex];
                        byte nG = Map.AreaPixels[nIndex + 1];
                        byte nR = Map.AreaPixels[nIndex + 2];

                        if (nR != r || nG != g || nB != b)
                        {
                            Color neighborColor = Color.FromRgb(nR, nG, nB);

                            var segmentKey = CreateSortedKey(color, neighborColor);

                            if (!borderGraph.ContainsKey(segmentKey))
                            {
                                BorderPixelSegment newSegment = new BorderPixelSegment
                                {
                                    Area1 = segmentKey.Item1,
                                    Area2 = segmentKey.Item2
                                };

                                borderGraph[segmentKey] = newSegment;

                                Map.Areas[segmentKey.Item1].BorderPixelSegments.Add(newSegment);
                                Map.Areas[segmentKey.Item2].BorderPixelSegments.Add(newSegment);
                            }
                            borderGraph[segmentKey].PixelIndices.Add(index);
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
    }
}
