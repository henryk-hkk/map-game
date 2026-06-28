using MapGame.Core.Constants;
using MapGame.Core.Utils.Geographic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Utils
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
                    if (area.parentRegionId.HasValue)
                    {
                        regionMap[i] = (int)area.parentRegionId;
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


    }
}
