using MapGame.Core.Constants;
using MapGame.Core.Utils.Geographic;
using MapGame.MVVM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Media;

namespace MapGame.Core.Utils.JSON
{
    public static class JSONLoader
    {

        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static string GetJSONFileContent(string relativePath)
        {
            Uri fileUri = new(relativePath, UriKind.Relative);
            string jsonPath = fileUri.ToString();
            if (!File.Exists(jsonPath))
            {
                System.Diagnostics.Debug.WriteLine($"BŁĄD: Nie znaleziono pliku konfiguracyjnego w {jsonPath}");
                return "";
            }
            string json = File.ReadAllText(jsonPath);
            return json;
        }
        public static void ReadJSONAreaDefinitionData(string relativePath)
        {
            string jsonContent = GetJSONFileContent(relativePath);
            if (jsonContent == null) return;

            AreaJSONData config = JsonSerializer.Deserialize<AreaJSONData>(jsonContent, _options);
            if (config?.AreaDefinitions == null) return;

            foreach(var areaDef in config.AreaDefinitions)
            {
                Color targetColor = areaDef.GetColor();

                if (Map.AreaColors.TryGetValue(targetColor, out PixelArea? actualArea))
                {
                    actualArea.Identifier = areaDef.Identifier;
                    actualArea.Name = areaDef.Name;
                    System.Diagnostics.Debug.WriteLine($"Dodano Area o identyfikatorze {areaDef.Identifier}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"OSTRZEŻENIE: Kolor {targetColor} zdefiniowany w JSON nie istnieje na mapie rastrowej!");
                }
            }
        }
        public static (BidirectionalMap<int, string>? RegionDict, List<Region>? Regions) ReadJSONRegionData(string relativePath)
        {

            string jsonContent = GetJSONFileContent(relativePath);
            if (jsonContent == null) return (null, null);

            MapJSONData config = JsonSerializer.Deserialize<MapJSONData>(jsonContent, _options);

            if (config?.Regions == null) return (null, null);

            BidirectionalMap<int, string> regionsDict = new();
            List<Region> regions = [];

            foreach (var region in config.Regions)
            {
                if (region.Areas == null) continue;

                Region mapRegion = new(region.RegionId, region.Identifier, region.Name);
                regionsDict.Add(region.RegionId, region.Name);
                regions.Add(mapRegion);

                foreach (var areaId in region.Areas)
                {
                    Area area = Map.Areas.Find(a => a.Identifier == areaId);
                    if (area == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"OSTRZEŻENIE: Area o identyfikatorze {areaId} zdefiniowana w regionie {region.Identifier} nie istnieje w słowniku!");
                        continue;
                    }
                    area.ParentRegionId = region.RegionId;
                    mapRegion.Add(area);
                }
            }
            return (regionsDict, regions);
        }

        public static List<Country> ReadJSONCountryData(string relativePath)
        {
            string jsonContent = GetJSONFileContent(relativePath);
            if (jsonContent == null) return [];

            CountryJSONData config = JsonSerializer.Deserialize<CountryJSONData>(jsonContent, _options);

            if (config?.Countries == null) return [];
            List<Country> countries = [];
            foreach (var country in config.Countries)
            {
                Country mapCountry = new(country.Identifier)
                {
                    DisplayColor = country.GetColor()
                };
                if (country.DisplayName != null) mapCountry.DisplayName = country.DisplayName;

                countries.Add(mapCountry);
                foreach(var ownedRegionId in country.OwnedRegionIds)
                {
                    var region = Map.Regions.Find(r => r.Identifier == ownedRegionId);
                    if (region == null) continue;
                    region.Owner = mapCountry;
                    mapCountry.OwnedRegions.Add(region);
                }

            }

            return countries;
        }
    }
}
