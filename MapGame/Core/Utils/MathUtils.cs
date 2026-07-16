using System;
using System.Collections.Generic;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Text;
using MapGame.Core.Geographic;

namespace MapGame.Core.Utils
{
    public static class MathUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(Point p1, Point p2)
        {
            if (p1 == p2) return 0;
            else
            {
                double xDifferenceSq = p2.X - p1.X;
                double yDifferenceSq = p2.Y - p1.Y;
                return Math.Sqrt(xDifferenceSq * xDifferenceSq + yDifferenceSq * yDifferenceSq);
            }
        }
    }
}
