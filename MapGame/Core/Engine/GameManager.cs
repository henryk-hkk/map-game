using MapGame.Core.Constants;
using MapGame.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace MapGame.Core.Engine
{
    public class GameManager
    {
        public void Init()
        {
            loadMaps();
        }

        private void loadMaps()
        {
            Map.HeightMap = MapDataLoader.LoadGrayscaleMap("Assets/Map/img/Heightmap.png");
            //Map.HeightMap = MapDataLoader.LoadGrayscaleMap("Assets/Map/Heightmap_Fullres.png");
            Map.LandMask = MapDataLoader.LoadMask("Assets/Map/img/LandMask.png");
            Map.RiverMask = MapDataLoader.LoadMask("Assets/Map/img/RiverMask.png");
            Map.TextureMap = MapDataLoader.LoadTexture("Assets/Map/img/Colored.png");
            Map.WaterTexture = MapDataLoader.LoadTexture("Assets/Map/img/water.png");
            var AreasMapRead = MapDataLoader.LoadAreasFromColorMap("Assets/Map/img/Areas.bmp");
            Map.AreaPixels = AreasMapRead.Pixels;
            Map.Areas = AreasMapRead.Areas;
            var JSONMapRead = MapDataLoader.ReadJSONMapData();
            Map.Regions = JSONMapRead.Regions;
            Map.RegionNames = JSONMapRead.RegionDict;
        }
    }
}
