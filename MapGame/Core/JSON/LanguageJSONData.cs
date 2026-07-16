using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.JSON
{
    public class AreaNamesJSONData
    {
        public List<AreaNameData> AreaNames { get; set; }
    }

    public class AreaNameData
    {
        public string Identifier { get; set; }
        public string Name { get; set; }
    }
    public class NameTagData
    {
        public string NameTag { get; set; }
        public string Name {  get; set; }
    }

    public class RegionNameTagsJSONData
    {
        public List<NameTagData> RegionNameTags { get; set; }
    }

    public class CountryNameTagsJSONData
    {
        public List<NameTagData> CountryNameTags { get; set; }
    }

}
