
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MapGame.Core.Utils;
using MapGame.Core.Utils.Geographic;

namespace MapGame.Core.Constants
{
    public static class Map
    {
        public const double MinX = 0.0;
        public static double MaxX = 6144.0;

        public const double MinY = 0.0;
        public static double MaxY = 3840.0;

        public static int Width = (int)MaxX;
        public static int Height = (int)MaxY;

        public static byte[]? HeightMap;
        public static bool[]? LandMask;
        public static bool[]? RiverMask;

        public static BitmapImage? TextureMap;
        public static BitmapImage? WaterTexture;

        public static Dictionary<Color, PixelArea> Areas;
        public static byte[] AreaPixels;

        public static List<Region> Regions = new List<Region>();
        public static BidirectionalMap<int, string> RegionNames;

        //public static Position Pos1 = new Position(3180, 966), Pos2 = new Position(3167, 1000), Pos3 = new Position(3186, 1002);
        //public static Position[] posList = {Pos1,Pos2,Pos3 };
        //public static PolygonArea Gdansk = new PolygonArea(posList);
        
    }
    

    //public static AreaList ImpassableAreas();

}

