using MapGame.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.Utils.Geographic
{
    public class Region : List<Area>
    {
        public Region(int id, string identifier, string? name = null)
        {
            Id = id;
            Identifier = identifier;
            Name = name;
        }
        public int Id { get; private set; }
        public string Identifier { get; private set; }
        public string? Name { get; private set; }
        public Country Owner { get; set; }
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
