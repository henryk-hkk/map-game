using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace MapGame.Core.Utils.JSON
{
    public class AreaJSONData
    {
        public List<AreaDefinition> AreaDefinitions { get; set; }
    }
    public class AreaDefinition
    {
        public string Identifier { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public Color GetColor() => Color.FromRgb(R, G, B);
    }

    public class AreaPopulationJSONData
    {
        public List<AreaPopulatioData> AreaPopulations { get; set; }
    }

    public class AreaPopulatioData
    {
        public string Identifier { get; set; }
        public int Population { get; set; }

        [JsonPropertyName("Languages")]
        public List<LanguageInfo> LanguagesDto { get; set; }


        [JsonIgnore]
        public Dictionary<string, int> Languages =>
                LanguagesDto?.ToDictionary(
                k => k.Language,
                v => int.Parse(v.Population.TrimEnd('%'))
            );
    }

    public class LanguageInfo
    {
        public string Language { get; set; }
        public string Population { get; set; }
    }
}
