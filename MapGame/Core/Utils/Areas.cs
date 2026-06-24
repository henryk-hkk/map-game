using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.Utils
{
    public abstract class Area
    {
        public abstract bool Includes(Position pos);
    }

    public class AreaList : List<Area> { }

    public class CircularArea : Area 
    {
        private Position center;
        private double radius;
        public override bool Includes(Position pos) { return true; }
    }

    class PolygonArea : Area 
    {
        public override bool Includes(Position pos)
        {
            return true;
        }
    }

}