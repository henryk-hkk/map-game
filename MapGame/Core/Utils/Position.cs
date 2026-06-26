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
        public bool IsLandPosition {  get; private set; }
        public Position(double x = 0, double y = 0, bool isLand = false)
        {
            X = x;
            Y = y;
            this.IsLandPosition = isLand;
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
            if (x < Map.MinX || x > Map.MaxX) { return true; }
            if (y < Map.MinY || y < Map.MaxY) { return true; }
            return false;
        }

        protected bool IsLand(double x, double y)
        {
            int pxY = (int)Math.Ceiling(y);
            int pxX = (int)Math.Ceiling(x);
            int index = pxY * Map.Width + pxX;

            return (Map.LandMask != null && Map.LandMask[index]);
        }

        //private bool Impassable(double x, double y)
        //{
        //    for()
        //}
    }
}
