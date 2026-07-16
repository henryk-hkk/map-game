using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;
using MapGame.Core.JSON;
using MapGame.Core.Utils.Map;

namespace MapGame.Core.Engine
{
    public enum Scenario
    {
        His_PreWar1910,
        His_War1914,
        His_PreWar1933,
        His_Asia1937,
        His_War1939,
        Alt_KR1936
    }

    public enum Language
    {
        English,
        Polish
    }
    public class GameManager
    {
        private const string _mapAssetsFolderPath = "Assets/Map/img/";
        private const string _databaseAssetsFolderPath = "Assets/Databases/";
        private const string _languageAssetsFolderPath = _databaseAssetsFolderPath + "Languages/";
        private string _scenarioFolderPath = "";
        private string _languageFolderPath = "";
        public void Init(Scenario scenario, Language language)
        {
            LoadMaps();

            _scenarioFolderPath = GetScenarioFolderPath(scenario);
            _languageFolderPath = GetLanguageFolderPath(language);
            
            LoadRegionData(_databaseAssetsFolderPath + _scenarioFolderPath);
            LoadLanguageData(_languageAssetsFolderPath + _languageFolderPath);
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
            MapContext.Areas = Areas;

            GraphicContext.AreaColors = AreaColors;
            GraphicContext.AreaPixels = Pixels;

            JSONLoader.ReadJSONAreaDefinitionData(_databaseAssetsFolderPath + "areaDefinition.json");
            GraphicContext.CountryColorTags = JSONLoader.ReadJSONCountryColorTagData(_databaseAssetsFolderPath + "countryColorTags.json");
        }

        private static void LoadRegionData(string scenarioFolderPath)
        {
            var (Regions, RegionIds, RegionIdentifiers) = JSONLoader.ReadJSONRegionData(scenarioFolderPath + "regionData.json");
            MapLogicContext.Regions = Regions;
            MapLogicContext.RegionIds = RegionIds;
            MapLogicContext.RegionIdentifiers = RegionIdentifiers;

            var (Countries, CountryIdentifiers) = JSONLoader.ReadJSONCountryData(scenarioFolderPath + "countryData.json");
            MapLogicContext.Countries = Countries;
            MapLogicContext.CountryIdentifiers = CountryIdentifiers;

            MapLogicContext.GlobalRegionMap = MapUtils.GetRegionMap(MapContext.Width, MapContext.Height);
            MapLogicContext.GlobalCountryMap = MapUtils.GetCountryMap(MapContext.Width, MapContext.Height);
        }

        private static void LoadLanguageData(string languageFolderPath)
        {
            LanguageContext.AreaNames = JSONLoader.ReadJSONAreaNameData(languageFolderPath + "areas.json");
            LanguageContext.RegionNameTags = JSONLoader.ReadJSONRegionNameTagData(languageFolderPath + "regionNameTags.json");
            LanguageContext.CountryNameTags = JSONLoader.ReadJSONCountryNameTagData(languageFolderPath + "countryNameTags.json");
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

        private static string GetLanguageFolderPath(Language language)
        {
            return language switch
            {
                Language.English => "English/",
                Language.Polish => "Polish/",
                _ => "English/"
            };
        }
    }
}
