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
        public string Name { get; set; }
        public List<string> Areas { get; set; }
    }
}
