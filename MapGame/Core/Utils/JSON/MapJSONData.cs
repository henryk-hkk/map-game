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
        public List<AreaDefinition> Areas { get; set; }
    }

    public class AreaDefinition
    {
        public string Identifier { get; set; }
        public string Name { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public Color GetColor() => Color.FromRgb(R, G, B);
    }
}
