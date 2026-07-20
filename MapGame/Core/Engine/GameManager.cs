using MapGame.Core.Geographic;
using MapGame.Core.Utils.JSON;
using MapGame.Core.Utils.Map;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

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
    public class GameManager(IJsonReader jsonReader)
    {
        private const string _mapAssetsFolderPath = "Assets/Map/img/";
        private const string _databaseAssetsFolderPath = "Assets/Databases/";
        private const string _languageAssetsFolderPath = _databaseAssetsFolderPath + "Languages/";
        private string _scenarioFolderPath = "";
        private string _languageFolderPath = "";

        private readonly AreaInitService _areaInitService = new(jsonReader);
        private readonly RegionInitService _regionInitService = new(jsonReader);
        private readonly CountryInitService _countryInitService = new(jsonReader);
        private readonly TagInitService _tagInitService = new(jsonReader);

        public void Init(Scenario scenario, Language language)
        {
            LoadMaps();

            _scenarioFolderPath = GetScenarioFolderPath(scenario);
            _languageFolderPath = GetLanguageFolderPath(language);
            
            LoadScenarioData(_databaseAssetsFolderPath + _scenarioFolderPath);
            LoadLanguageData(_languageAssetsFolderPath + _languageFolderPath);
            InitUIEvents();
        }

        private void LoadMaps()
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

            _areaInitService.InitAreaDefinitions(_databaseAssetsFolderPath + "areaDefinition.json");

            var (HistoricalRegions, HistoricalRegionIdentifiers) = _regionInitService.InitHistoricalRegions(_databaseAssetsFolderPath + "historicalRegions.json");
            MapContext.HistoricalRegions = HistoricalRegions;
            MapContext.HistoricalRegionIdentifiers = HistoricalRegionIdentifiers;

            GraphicContext.CountryColorTags = _tagInitService.GetColorTagData<CountryColorTagJSONData>(_databaseAssetsFolderPath + "countryColorTags.json", data => data.CountryColorTags.ToDictionary(x => x.ColorTag, x => x.GetColor()));

        }

        private void LoadScenarioData(string scenarioFolderPath)
        {
            _areaInitService.InitAreaPopulations(scenarioFolderPath + "areaPopulationData.json");

            var (Regions, RegionIds, RegionIdentifiers) = _regionInitService.InitRegions(scenarioFolderPath + "regionData.json");
            MapLogicContext.Regions = Regions;
            MapLogicContext.RegionIds = RegionIds;
            MapLogicContext.RegionIdentifiers = RegionIdentifiers;

            var (Countries, CountryIdentifiers) = _countryInitService.InitCountries(scenarioFolderPath + "countryData.json");
            MapLogicContext.Countries = Countries;
            MapLogicContext.CountryIdentifiers = CountryIdentifiers;

            MapLogicContext.GlobalRegionMap = MapUtils.GetRegionMap(MapContext.Width, MapContext.Height);
            MapLogicContext.GlobalCountryMap = MapUtils.GetCountryMap(MapContext.Width, MapContext.Height);
        }

        private void LoadLanguageData(string languageFolderPath)
        {
            LanguageContext.AreaNames = _tagInitService.GetNameTagData<AreaNamesJSONData>(languageFolderPath + "areas.json", data => data.AreaNames.ToDictionary(x => x.Identifier, x => x.Name));
            LanguageContext.RegionNameTags = _tagInitService.GetNameTagData<RegionNameTagsJSONData>(languageFolderPath + "regionNameTags.json", data => data.RegionNameTags.ToDictionary(x => x.NameTag, x => x.Name));
            LanguageContext.CountryNameTags = _tagInitService.GetNameTagData<CountryNameTagsJSONData>(languageFolderPath + "countryNameTags.json", data => data.CountryNameTags.ToDictionary(x => x.NameTag, x => x.Name));
        }

        private static void InitUIEvents()
        {
            EngineCommands.OnAreaParentChanged += MapDisplay.ChangeAreaParent;
            EngineCommands.OnMultipleAreasParentChanged += MapDisplay.ChangeAreasParent;
            EngineCommands.OnRegionOwnerChanged += MapDisplay.ChangeRegionOwner;
            EngineCommands.OnMultipleRegionsOwnerChanged += MapDisplay.AnnexRegions;
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
