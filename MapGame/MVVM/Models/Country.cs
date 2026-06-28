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
        Country(string identifier)
        {
            Identifier = identifier;
        }
        public List<Region> OwnedRegions { get; set; } = [];
        public string Identifier { get; private set; } = "Default"; 
        string? DisplayName;
        Color? DisplayColor = Color.FromArgb(255,0,0,0);
    }
}
