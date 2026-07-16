using MapGame.Core.Geographic;
using MapGame.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace MapGame.Core.Engine
{
    public readonly record struct Unit(); // unit = nic, zamiast wpisywania void w pole typu outputu (bo sie nie da)
    public readonly record struct CommandOutput<T>(
        bool Status,
        T? Output = default,
        string? Message = null
    );

    internal class Commands
    {
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

        public static CommandOutput<Country> GetCountryByIdentifier(string countryIdentifier)
        {
            if(!MapLogicContext.CountryIdentifiers.TryGetValue(countryIdentifier, out Country? country))
            {
                return new(false, null, $"Invalid country identifier {countryIdentifier}");
            }
            return new(true, country);
        }

        public static CommandOutput<Unit> SetAreaParent(System.Windows.Media.Color areaColor, int newParentId)
        {
            var areaOutput = GetAreaByColor(areaColor);
            if (!areaOutput.Status) return new(false, new(), areaOutput.Message);

            var regionOutput = GetRegionById(newParentId);
            if(!regionOutput.Status) return new(false, new(), regionOutput.Message);

            return SetAreaParent(areaOutput.Output, regionOutput.Output);
        }

        public static CommandOutput<Unit> SetAreaParent(System.Windows.Media.Color areaColor, string newParentIdentifier)
        {
            var areaOutput = GetAreaByColor(areaColor);
            if (!areaOutput.Status) return new(false, new(), areaOutput.Message);

            var regionOutput = GetRegionByIdentifier(newParentIdentifier);
            if (!regionOutput.Status) return new(false, new(), regionOutput.Message);

            return SetAreaParent(areaOutput.Output, regionOutput.Output);
        }

        public static CommandOutput<Unit> SetAreaParent(Area area, Region newParent)
        {
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

            if(area is PixelArea pixelArea) MapDisplay.ChangeAreaOwner(pixelArea, newParent);
            return new(true, new());
        }
        public static CommandOutput<Unit> SetRegionOwner(Region region, string ownerIdentifier)
        {
            var output = GetCountryByIdentifier(ownerIdentifier);
            if (!output.Status)
            {
                return new(false, new(), output.Message);
            }
            return SetRegionOwner(region, output.Output);
        }

        public static CommandOutput<Unit> SetRegionOwner(Region region, Country owner)
        {
            Country? oldRegionOwner = region.Owner;
            oldRegionOwner?.OwnedRegions.Remove(region);

            owner.OwnedRegions.Add(region);
            region.Owner = owner;
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
            }

            List<Area> areas = [];
            foreach (string areaIdentifier in areaIdentifiers)
            {
                var areaOutput = GetAreaByIdentifier(areaIdentifier);
                if (areaOutput.Status) areas.Add(areaOutput.Output);
            }
            return CreateRegion(areas, regionId, regionIdentifier, regionNameTag, bypassSafetyChecks);
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
            foreach (Area area in areas)
            {
                SetAreaParent(area, region);
            }

            return new(true, region);
        }
        public static CommandOutput<Unit> AddRegionToDatabase(Region? region)
        {
            if (region == null) return new(false, new(), "Provided region is null");
            MapLogicContext.Regions.Add(region);
            MapLogicContext.RegionIds.Add(region.Id, region);
            MapLogicContext.RegionIdentifiers.Add(region.Identifier, region);
            return new(true, new());
        }
    }
}
