using System;
using System.Collections.Generic;
using System.Text;
using MapGame.Core.Constants;

namespace MapGame.Core.Utils
{
    public class Position
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public Position(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }

        public void MoveTo(double newX, double newY)
        {
            X = newX;
            Y = newY;
        }
        public void Translate(double deltaX, double deltaY)
        {
            X += deltaX;
            Y += deltaY;
        }

        private bool ValidatePosition(double x, double y) {
            if (OutOfMap(x, y)) { return false; }
            return true;
        }

        private bool OutOfMap(double x, double y) {
            if (x < MapConstraints.MinX || x > MapConstraints.MaxX) { return true; }
            if( y < MapConstraints.MinY || y < MapConstraints.MaxY) { return true; }
            return false;
        }

        //private bool Impassable(double x, double y)
        //{
        //    for()
        //}
    }
}
