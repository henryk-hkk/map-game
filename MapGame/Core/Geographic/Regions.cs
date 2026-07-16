using MapGame.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Geographic
{
    public class Region(int id, string identifier, string nameTag) : List<Area>
    {
        public readonly int Id = id;
        public readonly string Identifier = identifier;
        public string NameTag { get; set; } = nameTag;
        public string? DisplayName { get => GetDisplayName(); }
        public Country? Owner { get; set; }
        public int Population
        {
            get => GetPopulation();
            set => SetPopulation(value);
        }
        public bool Includes(Position pos)
        {
            foreach (Area area in this)
            {
                if (area.Includes(pos)) return true;
            }
            return false;
        }

        private string GetDisplayName()
        {
            return LanguageContext.RegionNameTags.TryGetValue(NameTag, out string name) ? name : "";
        }

        private int GetPopulation()
        {
            int population = 0;
            foreach (Area area in this)
            {
                if (area.Population.HasValue) population += (int)area.Population;
            }
            return population;
        }

        private void SetPopulation(int newPopulation)
        {
            int dPopulation = newPopulation - GetPopulation();
            int ignoredElements = 0;
            for(int i = 0; i < this.Count; i++)
            {
                int localPopulationChange = (int)(dPopulation / (this.Count - i + ignoredElements)); // we calculate the area.population change
                Area area = this[i];

                if (!area.Population.HasValue) { //if the area does not have population attribute
                    ignoredElements++;
                    continue;
                }

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
}
