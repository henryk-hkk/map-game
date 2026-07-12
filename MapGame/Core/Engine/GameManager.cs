using MapGame.Core.Utils.JSON;
using MapGame.Core.Utils.Geographic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace MapGame.Core.Engine
{
    public class GameManager
    {
        private const string _mapAssetsFolderPath = "Assets/Map/img/";
        private const string _databaseAssetsFolderPath = "Assets/Databases/";
        private string _scenarioFolderPath = "";
        public void Init(Scenario scenario)
        {
            LoadMaps();
            _scenarioFolderPath = GetScenarioFolderPath(scenario);
            LoadRegionData(_databaseAssetsFolderPath + _scenarioFolderPath);
        }

        private static void LoadMaps()
        {
            MapContext.HeightMap = MapDataLoader.LoadGrayscaleMap(_mapAssetsFolderPath + "Heightmap.png");
            MapContext.LandMask = MapDataLoader.LoadMask(_mapAssetsFolderPath + "LandMask.png");
            MapContext.RiverMask = MapDataLoader.LoadMask(_mapAssetsFolderPath + "RiverMask.png");
            MapContext.LakeMask = MapDataLoader.LoadMask(_mapAssetsFolderPath + "LakeMask.png");

            GraphicContext.TextureMap = MapDataLoader.LoadTexture(_mapAssetsFolderPath + "TextureMap.png");
            GraphicContext.RiverTexture = MapDataLoader.LoadTexture(_mapAssetsFolderPath + "RiverTexture.png");
            GraphicContext.LakeTexture = MapDataLoader.LoadTexture(_mapAssetsFolderPath + "LakeTexture.png");

            var (AreaColors, Areas, Pixels) = MapDataLoader.LoadAreasFromColorMap(_mapAssetsFolderPath + "Areas.bmp");
            GraphicContext.AreaColors = AreaColors;
            MapContext.Areas = Areas;
            GraphicContext.AreaPixels = Pixels;

            JSONLoader.ReadJSONAreaDefinitionData("Assets/Databases/areaDefinition.json");
        }

        private static void LoadRegionData(string databaseFolderPath)
        {
            var (RegionDict, RegionIds, Regions) = JSONLoader.ReadJSONRegionData(databaseFolderPath + "regionData.json");
            MapContext.RegionNames = RegionDict;
            MapContext.RegionIds = RegionIds;
            MapContext.Regions = Regions;
            MapContext.Countries = JSONLoader.ReadJSONCountryData(databaseFolderPath + "countryData.json");

            MapContext.GlobalRegionMap = MapUtils.GetRegionMap(MapContext.Width, MapContext.Height);
            MapContext.GlobalCountryMap = MapUtils.GetCountryMap(MapContext.Width, MapContext.Height);
        }

        private static string GetScenarioFolderPath(Scenario scenario)
        {
            return scenario switch
            {
                Scenario.His_PreWar1910 => "1910/",
                Scenario.His_War1914 => "1914/",
                Scenario.His_PreWar1933 => "1933/",
                Scenario.His_Asia1937 => "1937/",
                Scenario.His_War1939 => "1939/",
                Scenario.Alt_KR1936 => "KR1936/",
                _ => "1933/"
            };
        }
    }
}
