using MapGame.Core.Utils.Geographic;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.Utils
{
    public static class MathUtils
    {
        public static double Distance(Position pos1, Position pos2)
        {
            if (pos1 == null || pos2 == null) return 0;
            if (pos1 == pos2) return 0;
            else
            {
                double xDifferenceSq = pos2.X - pos1.X;
                double yDifferenceSq = pos2.Y - pos1.Y;
                // Done some research, Math.Pow() is much slower. When calculating distances every frame it will add up.
                return Math.Sqrt(xDifferenceSq * xDifferenceSq + yDifferenceSq * yDifferenceSq); 
                
            }
        }
    }
}
