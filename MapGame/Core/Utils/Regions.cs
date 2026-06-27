using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.Utils
{
    public class Region : List<Area>
    {
        public Region(int id, string? name = null)
        {
            Id = id;
            Name = name;
        }
        public int Id { get; private set; }
        public string? Name { get; private set; }
        public bool Includes(Position pos)
        {
            foreach (Area area in this)
            {
                if (area.Includes(pos)) return true;
            }
            return false;
        }
    }
}
