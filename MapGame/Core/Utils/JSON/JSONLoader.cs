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

        private static string GetJSONFileContent(string relativePath)
        {
            Uri fileUri = new Uri(relativePath, UriKind.Relative);
            string jsonPath = fileUri.ToString();
            if (!File.Exists(jsonPath))
            {
                System.Diagnostics.Debug.WriteLine($"BŁĄD: Nie znaleziono pliku konfiguracyjnego w {jsonPath}");
                return null;
            }
            string json = File.ReadAllText(jsonPath);
            return json;
        }
        public static (BidirectionalMap<int, string>? RegionDict, List<Region>? Regions) ReadJSONRegionData(string relativePath)
        {

            string jsonContent = GetJSONFileContent(relativePath);
            if (jsonContent == null) return (null, null);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            MapJSONData config = JsonSerializer.Deserialize<MapJSONData>(jsonContent, options);

            if (config?.Regions == null) return (null, null);

            BidirectionalMap<int, string> regionsDict = new();
            List<Region> regions = [];

            foreach (var region in config.Regions)
            {
                if (region.Areas == null) continue;

                Region mapRegion = new Region(region.RegionId, region.Identifier, region.Name);
                regionsDict.Add(region.RegionId, region.Name);
                regions.Add(mapRegion);

                foreach (var areaDef in region.Areas)
                {
                    Color targetColor = areaDef.GetColor();

                    if (Map.Areas.TryGetValue(targetColor, out PixelArea? actualArea))
                    {
                        actualArea.Identifier = areaDef.Identifier;
                        actualArea.Name = areaDef.Name;
                        actualArea.ParentRegionId = region.RegionId;
                        mapRegion.Add(actualArea);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"OSTRZEŻENIE: Kolor {targetColor} zdefiniowany w JSON nie istnieje na mapie rastrowej!");
                    }
                }
            }
            return (regionsDict, regions);
        }

        public static List<Country> ReadJSONCountryData(string relativePath)
        {
            string jsonContent = GetJSONFileContent(relativePath);
            if (jsonContent == null) return (null);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            CountryJSONData config = JsonSerializer.Deserialize<CountryJSONData>(jsonContent, options);

            if (config?.Countries == null) return null;
            List<Country> countries = [];
            foreach (var country in config.Countries)
            {
                Country mapCountry = new Country(country.Identifier);
                mapCountry.DisplayColor = country.GetColor();
                if (country.DisplayName != null) mapCountry.DisplayName = country.DisplayName;

                countries.Add(mapCountry);
                foreach(var ownedRegionId in country.OwnedRegionIds)
                {
                    Region region = Map.Regions.Find(r => r.Identifier == ownedRegionId);
                    if (region == null) continue;
                    region.Owner = mapCountry;
                    mapCountry.OwnedRegions.Add(region);
                }

            }

            return countries;
        }
    }
}
