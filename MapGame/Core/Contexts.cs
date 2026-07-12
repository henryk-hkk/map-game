
using HelixToolkit.SharpDX.Utilities;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Utils;
using MapGame.Core.Utils.Geographic;
using MapGame.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapGame.Core
{
    public static class MapContext
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
        public static bool[]? LakeMask;

        public static List<PixelArea> Areas;
        public static List<Region> Regions;
        public static Dictionary<int, Region> RegionIds = [];
        public static BidirectionalMap<int, string> RegionNames;
        public static List<Country> Countries;

        public static int[] GlobalRegionMap;
        public static int[] GlobalCountryMap;
        public static int[] GlobalSelectionMask;

        public static int CurrentlySelectedRegionId = -1;

        //public static Position Pos1 = new Position(3180, 966), Pos2 = new Position(3167, 1000), Pos3 = new Position(3186, 1002);
        //public static Position[] posList = {Pos1,Pos2,Pos3 };
        //public static PolygonArea Gdansk = new PolygonArea(posList);

    }

    public static class GraphicContext
    {
        public const int SdfScale = 2;
        public static BitmapImage? TextureMap;
        public static BitmapImage? RiverTexture;
        public static BitmapImage? LakeTexture;

        public static Dictionary<Color, PixelArea> AreaColors;
        public static byte[] AreaPixels;
        public static Dictionary<(Color, Color), BorderPixelSegment> BorderGraph;

        public static byte[] RegionBorderPixelData;

        public static byte[] CountryPixelData;
        
        public static byte[] SelectionPixelData;

        public static HelixToolkit.Maths.Color4[] RegionBorderColorData;
        public static WriteableBitmap RegionBordersBitmap;
        public static HelixToolkit.Wpf.SharpDX.Material RegionBordersMaterial;

        public static WriteableBitmap CountryBitmap;
        public static HelixToolkit.Wpf.SharpDX.Material CountryMaterial;

        public static WriteableBitmap SelectionBitmap;
        public static HelixToolkit.Wpf.SharpDX.Material SelectionMaterial;

        public static byte[] MasterOverlayPixelData;
        public static PhongMaterial OverlayMaterial;

    }

    public enum Scenario
    {
        His_PreWar1910,
        His_War1914,
        His_PreWar1933,
        His_Asia1937,
        His_War1939,
        Alt_KR1936
    }

    //public static AreaList ImpassableAreas();

}

