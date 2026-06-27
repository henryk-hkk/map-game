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
            Map.HeightMap = MapDataLoader.LoadGrayscaleMap("Assets/Map/Heightmap.png");
            //Map.HeightMap = MapDataLoader.LoadGrayscaleMap("Assets/Map/Heightmap_Fullres.png");
            Map.LandMask = MapDataLoader.LoadLandMask("Assets/Map/Contour.png");
            Map.TextureMap = MapDataLoader.LoadTextureMap("Assets/Map/Colored.png");
            var AreasMapRead = MapDataLoader.LoadAreasFromColorMap("Assets/Map/Areas.bmp");
            Map.AreaPixels = AreasMapRead.Pixels;
            Map.Areas = AreasMapRead.Areas;
            MapDataLoader.ReadJSONMapData();
        }
    }
}
