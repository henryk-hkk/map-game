using MapGame.Core.Geographic;
using MapGame.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace MapGame.Core.Engine
{
    public readonly record struct Unit(); // unit = nic, zamiast wpisywania void w pole typu outputu (bo sie nie da)
    public readonly record struct CommandOutput<T>(
        [property: MemberNotNullWhen(true, "Output")] bool Status,
        T? Output = default,
        string? Message = null
    );

    public class EngineCommands
    {
        // ----------------------------- Events
        public static event Action<Area, Region>? OnAreaParentChanged;
        public static event Action<IEnumerable<Area>, Region>? OnMultipleAreasParentChanged;
        public static event Action<Region, Country>? OnRegionOwnerChanged;
        public static event Action<IEnumerable<Region>, Country>? OnMultipleRegionsOwnerChanged;

        // ----------------------------- Area/Region/Country getters
        public static CommandOutput<PixelArea> GetAreaByColor(System.Windows.Media.Color areaColor)
        {
            if (!GraphicContext.AreaColors.TryGetValue(areaColor, out PixelArea? area))
            {
                string message = $"Invalid area color {areaColor}";
                return new(false, null, message);
            }
            return new(true, area);
        }
        public static CommandOutput<Area> GetAreaByIdentifier(string areaIdentifier)
        {
            Area? area = MapContext.Areas.Find(a => a.Identifier == areaIdentifier);
            if(area == null)
            {
                return new (false, null, $"Invalid area identifier {areaIdentifier}");
            }
            return new(true, area);
        }

        public static CommandOutput<Region> GetRegionById(int regionId)
        {
            if (!MapLogicContext.RegionIds.TryGetValue(regionId, out Region? region))
            {
                return new(false, null, $"Invalid region ID {regionId}");
            }
            return new(true, region);
        }
        public static CommandOutput<Region> GetRegionByIdentifier(string regionIdentifier)
        {
            if(!MapLogicContext.RegionIdentifiers.TryGetValue(regionIdentifier, out Region? region))
            {
                return new(false, null, $"Invalid region identifier {regionIdentifier}");
            }
            return new(true, region);
        }
        public static CommandOutput<HistoricalRegion> GetHistoricalRegionByIdentifier(string hRegionIdentifier)
        {
            if (!MapContext.HistoricalRegionIdentifiers.TryGetValue(hRegionIdentifier, out HistoricalRegion? hRegion))
            {
                return new(false, null, $"Invalid region identifier {hRegionIdentifier}");
            }
            return new(true, hRegion);
        }
        public static CommandOutput<Country> GetCountryByIdentifier(string countryIdentifier)
        {
            if(!MapLogicContext.CountryIdentifiers.TryGetValue(countryIdentifier, out Country? country))
            {
                return new(false, null, $"Invalid country identifier {countryIdentifier}");
            }
            return new(true, country);
        }
        // ----------------------------- Area/Region owner setters
        public static CommandOutput<Unit> SetAreaParent(AreaReference areaRef, RegionReference newParentRef, bool invokeEvent = true)
        {
            var areaOutput = areaRef.Resolve();
            if (!areaOutput.Status) return new(false, new(), areaOutput.Message);
            var area = areaOutput.Output;

            var regionOutput = newParentRef.Resolve();
            if (!regionOutput.Status) return new(false, new(), regionOutput.Message);
            var newParent = regionOutput.Output;

            if (area.ParentRegionId is int oldParentId)
            {
                if (area.ParentRegionId == newParent.Id)
                {
                    return new(false, new(), $"The area is already in the region {newParent.Id}");
                }
                if (MapLogicContext.RegionIds.TryGetValue(oldParentId, out Region? oldParent))
                {
                    oldParent?.Remove(area);
                }
            }

            area.ParentRegionId = newParent.Id;
            newParent.Add(area);

            if(area is PixelArea pixelArea && invokeEvent) OnAreaParentChanged?.Invoke(pixelArea, newParent);
            return new(true, new());
        }
        public static CommandOutput<Unit> SetMultipleAreasParent(List<string> areaIdentifiers, RegionReference regionRef)
        {
            var regionOutput = regionRef.Resolve();
            if (!regionOutput.Status) return new(false, new(), regionOutput.Message);
            var region = regionOutput.Output;

            List<Area> areas = [];

            foreach (string areaIdentifier in areaIdentifiers)
            {
                var areaOutput = GetAreaByIdentifier(areaIdentifier);
                if (!areaOutput.Status) continue;
                SetAreaParent(areaOutput.Output, region, false);
                areas.Add(areaOutput.Output);
            }
            OnMultipleAreasParentChanged?.Invoke(areas, region);
            return new(true);
        }
        public static CommandOutput<Unit> SetMultipleAreasParent(List<Area> areas, RegionReference regionRef)
        {
            var regionOutput = regionRef.Resolve();
            if (!regionOutput.Status) return new(false, new(), regionOutput.Message);
            var region = regionOutput.Output;

            foreach (Area area in areas)
            {
                SetAreaParent(area, region, false);
            }

            OnMultipleAreasParentChanged?.Invoke(areas, region);

            return new(true);
        }
        public static CommandOutput<Unit> SetRegionOwner(RegionReference regionRef, CountryReference ownerRef, bool invokeEvent = true)
        {
            var regionOutput = regionRef.Resolve();
            if (!regionOutput.Status) return new(false, new(), regionOutput.Message);
            var region = regionOutput.Output;

            var countryOutput = ownerRef.Resolve();
            if (!countryOutput.Status) return new(false, new(), countryOutput.Message);
            var owner = countryOutput.Output;

            Country? oldRegionOwner = region.Owner;
            oldRegionOwner?.OwnedRegions.Remove(region);

            owner.OwnedRegions.Add(region);
            region.Owner = owner;

            if(invokeEvent) OnRegionOwnerChanged?.Invoke(region, owner);

            return new(true, new());
        }

        public static CommandOutput<Region> CreateRegion(List<string> areaIdentifiers, int regionId, string regionIdentifier, string? regionNameTag, bool bypassSafetyChecks = false)
        {
            if (!bypassSafetyChecks)
            {
                if (areaIdentifiers.Count == 0)
                {
                    return new(false, null, $"List of area identifiers provided to create region {regionIdentifier} is empty");
                }
                if (GetRegionById(regionId).Status)
                {
                    return new(false, null, $"Region with ID {regionId} already exists");

                }
                if (GetRegionByIdentifier(regionIdentifier).Status)
                {
                    return new(false, null, $"Region with identifier {regionIdentifier} already exists");
                }
            }

            Region region = new(regionId, regionIdentifier, regionNameTag);

            SetMultipleAreasParent(areaIdentifiers, region);
            return new(true, region);
        }
        public static CommandOutput<Region> CreateRegion(List<Area> areas, int regionId, string regionIdentifier, string? regionNameTag, bool bypassSafetyChecks = false)
        {
            if (!bypassSafetyChecks)
            {
                if (areas.Count == 0)
                {
                    return new(false, null, $"List of areas provided to create region {regionIdentifier} is empty");
                }
                if (GetRegionById(regionId).Status)
                {
                    return new(false, null, $"Region with ID {regionId} already exists");
                    
                }
                if (GetRegionByIdentifier(regionIdentifier).Status)
                {
                    return new(false, null, $"Region with identifier {regionIdentifier} already exists");
                }
            }
            
            Region region = new(regionId, regionIdentifier, regionNameTag);

            SetMultipleAreasParent(areas, region);

            return new(true, region);
        }
        public static CommandOutput<Unit> AddRegionToContext(Region? region)
        {
            if (region == null) return new(false, new(), "Provided region is null");
            MapLogicContext.Regions.Add(region);
            MapLogicContext.RegionIds.Add(region.Id, region);
            MapLogicContext.RegionIdentifiers.Add(region.Identifier, region);
            return new(true, new());
        }
        public static CommandOutput<Country> CreateCountry(List<string> ownedRegionsIdentifiers, string countryIdentifier, string? nameTag, string? colorTag)
        {
            if(ownedRegionsIdentifiers.Count == 0)
            {
                return new(false, null, $"List of areas provided to create country {countryIdentifier} is empty");
            }
            if (GetCountryByIdentifier(countryIdentifier).Status)
            {
                return new(false, null, $"Country with identifier {countryIdentifier} already exists");
            }

            Country country = new(countryIdentifier, nameTag, colorTag);
            foreach (var regionIdentifier in ownedRegionsIdentifiers) 
            {
                var regionOutput = GetRegionByIdentifier(regionIdentifier);
                if (regionOutput.Status) SetRegionOwner(regionOutput.Output, country);
            }
            return new(true, country);
        }

        public static CommandOutput<Unit> SetRegionNameTag(RegionReference regionRef, string nameTag)
        {
            var regionOutput = regionRef.Resolve();
            if(!regionOutput.Status) return new(false, new(), regionOutput.Message);
            Region region = regionOutput.Output;

            region.NameTag = nameTag;
            return new(true, new());
        }
        public static CommandOutput<Unit> SetCountryNameTag(CountryReference countryRef, string nameTag)
        {
            var countryOutput = countryRef.Resolve();
            if (!countryOutput.Status) return new(false, new(), countryOutput.Message);
            Country country = countryOutput.Output;

            country.NameTag = nameTag;
            return new(true, new());
        }

        public static CommandOutput<Unit> SetCountryColorTag(CountryReference countryRef, string colorTag)
        {
            var countryOutput = countryRef.Resolve();
            if (!countryOutput.Status) return new(false, new(), countryOutput.Message);
            Country country = countryOutput.Output;

            country.ColorTag = colorTag;
            return new(true, new());
        }

        public static CommandOutput<Unit> AnnexCountry(CountryReference countryRef, CountryReference targetRef)
        {
            var countryOutput = countryRef.Resolve();
            if (!countryOutput.Status) return new(false, new(), countryOutput.Message);
            Country country = countryOutput.Output;

            var targetOutput = targetRef.Resolve();
            if (!targetOutput.Status) return new(false, new(), targetOutput.Message);
            Country target = targetOutput.Output;

            var regions = target.OwnedRegions.ToList();

            foreach(Region region in regions)
            {
                SetRegionOwner(region, country, false);
            }

            OnMultipleRegionsOwnerChanged?.Invoke(regions, country);

            return new(true, new());
        }
    }
}
