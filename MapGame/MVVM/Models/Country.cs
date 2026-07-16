using System;
using System.Collections.Generic;
using Windows;
using System.Text;
using System.Windows.Media;
using MapGame.Core;
using MapGame.Core.Geographic;



namespace MapGame.MVVM.Models
{

    public class Country
    {
        private static int DefaultCountryIdentifier = 0;

        public Country(string? identifier, string? nameTag = null, string? colorTag = null)
        {
            if (identifier != null) Identifier = identifier;
            else Identifier = $"Default{DefaultCountryIdentifier++}";
            if (nameTag != null) NameTag = nameTag;
            else NameTag = Identifier;
            if(colorTag != null) ColorTag = colorTag;
            else ColorTag = Identifier;

        }
        public List<Region> OwnedRegions { get; set; } = [];
        public string Identifier { get; private set; } = "Default";
        public string NameTag { get; set; }
        public string ColorTag { get; set; }
        public string DisplayName { get => GetDisplayName(); }
        public Color DisplayColor { get => GetDisplayColor(); }

        private string GetDisplayName()
        {
            return LanguageContext.CountryNameTags.TryGetValue(NameTag, out string name) ? name : NameTag;
        }

        private Color GetDisplayColor()
        {
            return GraphicContext.CountryColorTags.TryGetValue(ColorTag, out Color color) ? color : Color.FromArgb(255, 0, 0, 0);
        }
    }
}
