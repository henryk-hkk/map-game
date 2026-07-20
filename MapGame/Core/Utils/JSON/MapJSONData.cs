using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace MapGame.Core.Utils.JSON
{
    public class MapJSONData
    {
        public List<RegionData> Regions { get; set; }
    }

    public class RegionData
    {
        public int RegionId { get; set; }
        public string Identifier { get; set; }
        public string NameTag { get; set; }
        public List<string> Areas { get; set; }
    }

    public class HistoricalRegionsJSONData
    {
        public List<HistoricalRegionData> HistoricalRegions { get; set; }
    }

    public class HistoricalRegionData
    {
        public string Identifier { get; set; }
        public List<string> Areas { get; set; }
    }
}
