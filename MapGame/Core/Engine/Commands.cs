using MapGame.Core.Utils.Geographic;
using MapGame.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MapGame.Core.Engine
{
    internal class Commands
    {
        public static PixelArea? GetAreaByColor(System.Windows.Media.Color areaColor)
        {
            if (!GraphicContext.AreaColors.TryGetValue(areaColor, out PixelArea? area))
            {
                Debug.WriteLine($"Invalid area color {areaColor}");
                return null;
            }
            return area;
        }
        public static Area? GetAreaByIdentifier(string areaIdentifier)
        {
            Area? area = MapContext.Areas.Find(a => a.Identifier == areaIdentifier);
            if(area == null)
            {
                Debug.WriteLine($"Invalid area identifier {areaIdentifier}");
            }
            return area;
        }

        public static Region? GetRegionById(int regionId)
        {
            if (!MapContext.RegionIds.TryGetValue(regionId, out Region? region))
            {
                Debug.WriteLine($"Invalid region ID {regionId}");
            }
            return region;
        }

        public static Region? GetRegionByIdentifier(string regionIdentifier)
        {
            Region? region = MapContext.Regions.Find(r => r.Identifier == regionIdentifier);
            if(region == null)
            {
                Debug.WriteLine($"Invalid region identifier {regionIdentifier}");
            }
            return region;
        }

        public static void SetRegionOwner(Region region, Country newRegionOwner)
        {
            Country? oldRegionOwner = region.Owner;
            oldRegionOwner?.OwnedRegions.Remove(region);

            newRegionOwner.OwnedRegions.Add(region);
            region.Owner = newRegionOwner;
        }

        public static void SetAreaParent(System.Windows.Media.Color areaColor, int newParentId)
        {
            PixelArea? area = GetAreaByColor(areaColor);
            if (area == null) return;

            Region? newParent = GetRegionById(newParentId);
            if(newParent == null) return;

            SetAreaParent(area, newParent);
        }
        public static void SetAreaParent(System.Windows.Media.Color areaColor, string newParentIdentifier)
        {
            PixelArea? area = GetAreaByColor(areaColor);
            if (area == null) return;

            Region? newParent = GetRegionByIdentifier(newParentIdentifier);
            if (newParent == null) return;

            SetAreaParent(area, newParent);
        }

        public static void SetAreaParent(Area area, Region newParent)
        {
            if (area.ParentRegionId is int oldParentId)
            {
                if (area.ParentRegionId == newParent.Id)
                {
                    Debug.WriteLine($"The area is already in the region {newParent.Id}");
                    return;
                }
                if (MapContext.RegionIds.TryGetValue(oldParentId, out Region? oldParent))
                {
                    oldParent?.Remove(area);
                }
            }

            area.ParentRegionId = newParent.Id;
            newParent.Add(area);

            if(area is PixelArea pixelArea) MapDisplay.ChangeAreaOwner(pixelArea, newParent);
        }
        public static Region? CreateRegion(List<Area> areas, int regionId, string regionIdentifier, string? regionName)
        {
            if (areas.Count == 0)
            {
                Debug.WriteLine($"List of areas provided to create region {regionIdentifier} is empty");
                return null;
            }
            if (GetRegionById(regionId) != null)
            {
                Debug.WriteLine($"Region with ID {regionId} already exists");
                return null;
            }
            if (GetRegionByIdentifier(regionIdentifier) != null)
            {
                Debug.WriteLine($"Region with identifier {regionIdentifier} already exists");
                return null;
            }
            
            Region region = new(regionId, regionIdentifier, regionName);
            foreach (Area area in areas)
            {
                SetAreaParent(area, region);
            }
            MapContext.Regions.Add(region);
            MapContext.RegionIds.Add(regionId, region);
            MapContext.RegionNames.Add(regionId, regionName ?? "");

            return region;
        }
    }
}
