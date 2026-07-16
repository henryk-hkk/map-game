using MapGame.Core.Engine;
using MapGame.Core.Geographic;
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
        private static string? GetJSONFileContent(string relativePath)
        {
            Uri fileUri = new(relativePath, UriKind.Relative);
            string jsonPath = fileUri.ToString();
            if (!File.Exists(jsonPath))
            {
                System.Diagnostics.Debug.WriteLine($"BŁĄD: Nie znaleziono pliku konfiguracyjnego w {jsonPath}");
                return null;
            }
            string json = File.ReadAllText(jsonPath);
            return json;
        }
        public static void ReadJSONAreaDefinitionData(string relativePath)
        {
            string? jsonContent = GetJSONFileContent(relativePath);
            if (jsonContent == null) return;

            AreaJSONData? config = JsonSerializer.Deserialize<AreaJSONData>(jsonContent, _options);
            if (config?.AreaDefinitions == null) return;

            foreach(var areaDef in config.AreaDefinitions)
            {
                Color targetColor = areaDef.GetColor();

                var areaOutput = Commands.GetAreaByColor(targetColor);

                if (areaOutput.Status)
                {
                    areaOutput.Output.Identifier = areaDef.Identifier;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"OSTRZEŻENIE: Kolor {targetColor} zdefiniowany w JSON nie istnieje na mapie rastrowej!");
                }
            }
        }
        public static (List<Region>? Regions, Dictionary<int, Region>? RegionIds, Dictionary<string, Region>? RegionIdentifiers) ReadJSONRegionData(string relativePath)
        {
            string? jsonContent = GetJSONFileContent(relativePath);
            if (jsonContent == null) return (null, null, null);

            MapJSONData? config = JsonSerializer.Deserialize<MapJSONData>(jsonContent, _options);

            if (config?.Regions == null) return (null, null, null);

            Dictionary<int, Region> regionIds = [];
            Dictionary<string, Region> regionIdentifiers = [];
            List<Region> regions = [];

            foreach (var region in config.Regions)
            {
                if (region.Areas == null) continue;

                Region mapRegion = new(region.RegionId, region.Identifier, region.NameTag);
                regions.Add(mapRegion);
                regionIds.Add(region.RegionId, mapRegion);
                regionIdentifiers.Add(region.Identifier, mapRegion);

                foreach (var areaId in region.Areas)
                {
                    var areaOutput = Commands.GetAreaByIdentifier(areaId);
                    if (!areaOutput.Status)
                    {
                        System.Diagnostics.Debug.WriteLine($"OSTRZEŻENIE: Area o identyfikatorze {areaId} zdefiniowana w regionie {region.Identifier} nie istnieje w słowniku!");
                        continue;
                    }
                    areaOutput.Output.ParentRegionId = region.RegionId;
                    mapRegion.Add(areaOutput.Output);
                }
            }
            return (regions, regionIds, regionIdentifiers);
        }

        public static (List<Country>?, Dictionary<string, Country>? countryIdentifiers) ReadJSONCountryData(string relativePath)
        {
            string? jsonContent = GetJSONFileContent(relativePath);
            if (jsonContent == null) return (null, null);

            CountryJSONData? config = JsonSerializer.Deserialize<CountryJSONData>(jsonContent, _options);

            if (config?.Countries == null) return (null, null);
            List<Country> countries = [];
            Dictionary<string, Country> countryIdentifiers = [];
            foreach (var country in config.Countries)
            {
                Country mapCountry = new(country.Identifier, country.NameTag, country.ColorTag);

                countries.Add(mapCountry);
                countryIdentifiers.Add(mapCountry.Identifier, mapCountry);

                if (country.OwnedRegionIds == null) continue;

                foreach (var ownedRegionIdentifier in country.OwnedRegionIds)
                {
                    var regionOutput = Commands.GetRegionByIdentifier(ownedRegionIdentifier);
                    if (!regionOutput.Status) continue;
                    regionOutput.Output.Owner = mapCountry;
                    mapCountry.OwnedRegions.Add(regionOutput.Output);
                }
            }
            return (countries, countryIdentifiers);
        }

        public static Dictionary<string, Color>? ReadJSONCountryColorTagData(string relativePath)
        {
            string? jsonContent = GetJSONFileContent(relativePath);
            if (jsonContent == null) return null;

            CountryColorTagJSONData? config = JsonSerializer.Deserialize<CountryColorTagJSONData>(jsonContent, _options);
            if (config?.CountryColorTags == null) return null;

            Dictionary<string, Color> countryColorTags = [];

            foreach(var colorTag in config.CountryColorTags)
            {
                countryColorTags.Add(colorTag.ColorTag, colorTag.GetColor());
            }
            return countryColorTags;
        }

        public static Dictionary<string,string>? ReadJSONAreaNameData(string relativePath)
        {
            string? jsonContent = GetJSONFileContent(relativePath);
            if (jsonContent == null) return null;

            AreaNamesJSONData? config = JsonSerializer.Deserialize<AreaNamesJSONData>(jsonContent, _options);

            if(config?.AreaNames == null) return null;
            Dictionary<string, string> areaNames = [];
            foreach(var area in config.AreaNames)
            {
                areaNames.Add(area.Identifier, area.Name);
            }

            return areaNames;
        }

        public static Dictionary<string, string>? ReadJSONRegionNameTagData(string relativePath)
        {
            string? jsonContent = GetJSONFileContent(relativePath);
            if (jsonContent == null) return null;

            RegionNameTagsJSONData? config = JsonSerializer.Deserialize<RegionNameTagsJSONData>(jsonContent, _options);

            if(config?.RegionNameTags == null) return null;
            Dictionary<string, string> regionNameTags = [];
            foreach(var regionNameTag in  config.RegionNameTags)
            {
                regionNameTags.Add(regionNameTag.NameTag, regionNameTag.Name);
            }
            return regionNameTags;
        }

        public static Dictionary<string, string>? ReadJSONCountryNameTagData(string relativePath)
        {
            string? jsonContent = GetJSONFileContent(relativePath);
            if (jsonContent == null) return null;

            CountryNameTagsJSONData? config = JsonSerializer.Deserialize<CountryNameTagsJSONData>(jsonContent, _options);

            if (config?.CountryNameTags == null) return null;
            Dictionary<string, string> countryNameTags = [];
            foreach (var countryNameTag in config.CountryNameTags)
            {
                countryNameTags.Add(countryNameTag.NameTag, countryNameTag.Name);
            }
            return countryNameTags;
        }
    }
}
