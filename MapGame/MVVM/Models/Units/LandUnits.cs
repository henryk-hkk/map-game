using System;
using System.Collections.Generic;
using System.Text;
using MapGame.Core.Utils.Geographic;

namespace MapGame.MVVM.Models.Units
{
    public class LandUnit : Unit
    {
        public LandUnit(Country owner, Position pos, StructureTemplate template)
        {
            Owner = owner;
            Pos = pos;
        }
        public Position Pos;
        public StructureTemplate Template;
    }
}
