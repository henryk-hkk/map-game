using MapGame.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Utils.Geographic
{
    public class Region(int id, string identifier, string? name = null) : List<Area>
    {
        public int Id { get; private set; } = id;
        public string Identifier { get; private set; } = identifier;
        public string? Name { get; private set; } = name;
        public Country? Owner { get; set; }
        public bool Includes(Position pos)
        {
            foreach (Area area in this)
            {
                if (area.Includes(pos)) return true;
            }
            return false;
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

        public int Population
        {
            get => GetPopulation();
            set => SetPopulation(value);
        }

    }
}
