using MapGame.Core.Engine;
using MapGame.Core.Geographic;
using MapGame.MVVM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Media;
using static System.Windows.Forms.Design.AxImporter;

namespace MapGame.Core.Utils.JSON
{
    public class AreaInitService(IJsonReader jsonReader)
    {
        public void InitAreaDefinitions(string relativePath)
        {
            var config = jsonReader.Read<AreaJSONData>(relativePath);
            if (config?.AreaDefinitions == null) return;

            foreach (var areaDef in config.AreaDefinitions)
            {
                Color targetColor = areaDef.GetColor();

                var areaOutput = EngineCommands.GetAreaByColor(targetColor);

                if (areaOutput.Status)
                {
                    areaOutput.Output.Identifier = areaDef.Identifier;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"OSTRZEŻENIE: Kolor {targetColor} zdefiniowany w JSON jako area {areaDef.Identifier} nie istnieje na mapie rastrowej!");
                }
            }
        }

        public void InitAreaPopulations(string relativePath)
        {
            var config = jsonReader.Read<AreaPopulationJSONData>(relativePath);
            if (config?.AreaPopulations == null) return;

            foreach (var areaPopulation in config.AreaPopulations)
            {
                var mapAreaOutput = EngineCommands.GetAreaByIdentifier(areaPopulation.Identifier);
                if (!mapAreaOutput.Status) continue;
                Area mapArea = mapAreaOutput.Output;

                mapArea.InitPopulationData(areaPopulation.Population, areaPopulation.Languages);
            }
        }
    }
    public class RegionInitService(IJsonReader jsonReader)
    {

        public (List<Region> Regions, Dictionary<int, Region> RegionIds, Dictionary<string, Region> RegionIdentifiers) InitRegions(string relativePath)
        {
            var config = jsonReader.Read<MapJSONData>(relativePath);

            if (config?.Regions == null) return ([], [], []);

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
                    var areaOutput = EngineCommands.GetAreaByIdentifier(areaId);
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

        public (List<HistoricalRegion> HistoricalRegions, Dictionary<string, HistoricalRegion> HistoricalRegionIdentifiers) InitHistoricalRegions(string relativePath)
        {
            var config = jsonReader.Read<HistoricalRegionsJSONData>(relativePath);
            if (config?.HistoricalRegions == null) return ([], []);

            List<HistoricalRegion> hRegions = [];
            Dictionary<string, HistoricalRegion> hRegionIdentifiers = [];
            foreach (var region in config.HistoricalRegions)
            {
                HistoricalRegion hRegion = new(region.Identifier);
                hRegions.Add(hRegion);
                hRegionIdentifiers.Add(region.Identifier, hRegion);

                foreach (var areaId in region.Areas)
                {
                    var areaOutput = EngineCommands.GetAreaByIdentifier(areaId);
                    if (!areaOutput.Status)
                    {
                        System.Diagnostics.Debug.WriteLine($"OSTRZEŻENIE: Area o identyfikatorze {areaId} zdefiniowana w regionie {region.Identifier} nie istnieje w słowniku!");
                        continue;
                    }
                    hRegion.Add(areaOutput.Output);
                }
            }
            return (hRegions, hRegionIdentifiers);
        }
    }

    public class CountryInitService(IJsonReader jsonReader)
    {
        public (List<Country> Countries, Dictionary<string, Country> countryIdentifiers) InitCountries(string relativePath)
        {
            var config = jsonReader.Read<CountryJSONData>(relativePath);

            if (config?.Countries == null) return ([], []);
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
                    var regionOutput = EngineCommands.GetRegionByIdentifier(ownedRegionIdentifier);
                    if (!regionOutput.Status) continue;
                    regionOutput.Output.Owner = mapCountry;
                    mapCountry.OwnedRegions.Add(regionOutput.Output);
                }
            }
            return (countries, countryIdentifiers);
        }
    }
    public class TagInitService(IJsonReader jsonReader)
    {
        public Dictionary<string, Color> GetColorTagData<TData>(string path, Func<TData, Dictionary<string, Color>> extractDictionaryFunc)
        {
            var data = jsonReader.Read<TData>(path);
            if (data == null) return [];

            return extractDictionaryFunc(data);
        }
        public Dictionary<string, string> GetNameTagData<TData>(string path, Func<TData, Dictionary<string, string>> extractDictionaryFunc)
        {
            var data = jsonReader.Read<TData>(path);
            if (data == null) return [];

            return extractDictionaryFunc(data);
        }
    }
    public static class JSONServices
    {


        //public static Dictionary<string, Color>? ReadJSONCountryColorTagData(string relativePath)
        //{
        //    string? jsonContent = GetJSONFileContent(relativePath);
        //    if (jsonContent == null) return null;

        //    CountryColorTagJSONData? config = JsonSerializer.Deserialize<CountryColorTagJSONData>(jsonContent, _options);
        //    if (config?.CountryColorTags == null) return null;

        //    Dictionary<string, Color> countryColorTags = [];

        //    foreach(var colorTag in config.CountryColorTags)
        //    {
        //        countryColorTags.Add(colorTag.ColorTag, colorTag.GetColor());
        //    }
        //    return countryColorTags;
        //}

        //public static Dictionary<string,string>? ReadJSONAreaNameData(string relativePath)
        //{
        //    string? jsonContent = GetJSONFileContent(relativePath);
        //    if (jsonContent == null) return null;

        //    AreaNamesJSONData? config = JsonSerializer.Deserialize<AreaNamesJSONData>(jsonContent, _options);

        //    if(config?.AreaNames == null) return null;
        //    Dictionary<string, string> areaNames = [];
        //    foreach(var area in config.AreaNames)
        //    {
        //        areaNames.Add(area.Identifier, area.Name);
        //    }

        //    return areaNames;
        //}

        //public static Dictionary<string, string>? ReadJSONRegionNameTagData(string relativePath)
        //{
        //    string? jsonContent = GetJSONFileContent(relativePath);
        //    if (jsonContent == null) return null;

        //    RegionNameTagsJSONData? config = JsonSerializer.Deserialize<RegionNameTagsJSONData>(jsonContent, _options);

        //    if(config?.RegionNameTags == null) return null;
        //    Dictionary<string, string> regionNameTags = [];
        //    foreach(var regionNameTag in  config.RegionNameTags)
        //    {
        //        regionNameTags.Add(regionNameTag.NameTag, regionNameTag.Name);
        //    }
        //    return regionNameTags;
        //}

        //public static Dictionary<string, string>? ReadJSONCountryNameTagData(string relativePath)
        //{
        //    string? jsonContent = GetJSONFileContent(relativePath);
        //    if (jsonContent == null) return null;

        //    CountryNameTagsJSONData? config = JsonSerializer.Deserialize<CountryNameTagsJSONData>(jsonContent, _options);

        //    if (config?.CountryNameTags == null) return null;
        //    Dictionary<string, string> countryNameTags = [];
        //    foreach (var countryNameTag in config.CountryNameTags)
        //    {
        //        countryNameTags.Add(countryNameTag.NameTag, countryNameTag.Name);
        //    }
        //    return countryNameTags;
        //}
    }
}
