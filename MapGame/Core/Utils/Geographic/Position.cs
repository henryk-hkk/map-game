using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.Utils.Geographic
{
    public class Position(double x = 0, double y = 0, bool isLand = false)
    {
        public double X { get; private set; } = x;
        public double Y { get; private set; } = y;
        public bool IsLandPosition { get; private set; } = isLand;
        public bool IsOutOfMap
        {
            get => OutOfMap(X, Y);
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

        protected bool ValidatePosition(double x, double y) {
            if (OutOfMap(x, y)) { return false; }
            if (IsLandPosition && !IsLand(x,y)) { return false; }
            return true;
        }

        protected bool OutOfMap(double x, double y) {
            if (x < MapContext.MinX || x > MapContext.MaxX) { return true; }
            if (y < MapContext.MinY || y > MapContext.MaxY) { return true; }
            return false;
        }

        protected bool IsLand(double x, double y)
        {
            int pxY = (int)Math.Ceiling(y);
            int pxX = (int)Math.Ceiling(x);
            int index = pxY * MapContext.Width + pxX;

            return (MapContext.LandMask != null && MapContext.LandMask[index]);
        }

        //private bool Impassable(double x, double y)
        //{
        //    for()
        //}
    }
}
