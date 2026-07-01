using System;
using System.Collections.Generic;
using Windows;
using System.Text;
using System.Windows.Media;
using MapGame.Core.Utils.Geographic;



namespace MapGame.MVVM.Models
{

    public class Country
    {
        private static int DefaultCountryIdentifier = 0;

        public Country(string? identifier)
        {
            if (identifier != null) Identifier = identifier;
            else Identifier = $"Default{DefaultCountryIdentifier++}";
        }
        public List<Region> OwnedRegions { get; set; } = [];
        public string Identifier { get; private set; } = "Default"; 
        public string? DisplayName;
        public Color? DisplayColor = Color.FromArgb(255,0,0,0);
    }
}
