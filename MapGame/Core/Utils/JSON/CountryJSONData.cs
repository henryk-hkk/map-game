using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Text;

namespace MapGame.Core.Utils.JSON
{
    public class CountryJSONData
    {
        public List<CountryData>? Countries { get; set; }
    }

    public class CountryData
    {
        public string? Identifier { get; set; }
        public string? DisplayName { get; set; }

        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public List<string>? OwnedRegionIds { get; set; }

        public Color GetColor() => Color.FromRgb(R, G, B);
    }
}
