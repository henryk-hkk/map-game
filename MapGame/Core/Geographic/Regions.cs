using MapGame.Core.Utils;
using MapGame.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Geographic
{
    public class Region(int id, string identifier, string? nameTag = null) : List<Area>
    {
        public readonly int Id = id;
        public readonly string Identifier = identifier;
        public string NameTag { get; set; } = nameTag ?? identifier;
        public string? DisplayName { get => GetDisplayName(); }
        public Country? Owner { get; set; }
        public int Population
        {
            get => GetPopulation();
            set => SetPopulation(value);
        }

        public Dictionary<string, int> LanguagePercentages
        {
            get => GetTotalLanguagePercentages();
        }

        private Dictionary<string, int> GetTotalLanguagePopulations()
        {
            return this.SelectMany(area => area.LanguagePopulations)
                       .GroupBy(kvp => kvp.Key) 
                       .ToDictionary(
                           group => group.Key,
                           group => group.Sum(kvp => kvp.Value)
                       );
        }

        private Dictionary<string, int> GetTotalLanguagePercentages()
        {
            var totalPopulations = GetTotalLanguagePopulations();

            double totalRegionPopulation = totalPopulations.Values.Sum();

            if (totalRegionPopulation == 0)
            {
                return [];
            }

            var dict = totalPopulations.ToDictionary(
                kvp => kvp.Key,
                kvp => (int)Math.Round((kvp.Value / totalRegionPopulation) * 100)
            );

            MathUtils.AdjustPercentage(dict);
            return dict;
        }

        public bool Includes(Position pos)
        {
            foreach (Area area in this)
            {
                if (area.Includes(pos)) return true;
            }
            return false;
        }

        protected string GetDisplayName()
        {
            return LanguageContext.RegionNameTags.TryGetValue(NameTag, out string name) ? name : "";
        }

        protected int GetPopulation()
        {
            return this.Sum(area => (int)area.Population);
        }

        protected void SetPopulation(int newPopulation)
        {
            int dPopulation = newPopulation - GetPopulation();
            for(int i = 0; i < this.Count; i++)
            {
                int localPopulationChange = (int)((double)dPopulation / (this.Count - i)); // we calculate the area.population change
                Area area = this[i];

                if (localPopulationChange < 0 && area.Population < Math.Abs(localPopulationChange)) // edge case - if we subtract more people than the area has
                {
                    dPopulation -= (int)area.Population;
                    area.Population = 0;
                    continue;
                }

                area.Population += localPopulationChange;
                dPopulation -= localPopulationChange;
            }
        }

    }
    public class HistoricalRegion(string? identifier = null) : List<Area>
    {
        public readonly string Identifier = identifier ?? "Default";

        protected string GetDisplayName()
        {
            return LanguageContext.RegionNameTags.TryGetValue(Identifier, out string name) ? name : "";
        }
    }
}
