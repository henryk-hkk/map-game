using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.Utils
{
    public abstract class Area
    {
        public abstract bool Includes(Position pos);
    }

    public class AreaList : List<Area> 
    {
        public bool Includes(Position pos)
        {
            foreach (Area area in this)
            {
                if(area.Includes(pos)) return true;
            }
            return false;
        }
    }

    public class CircularArea : Area 
    {
        public CircularArea(Position center, double radius) 
        {
            _center = center;
            _radius = radius;
        }
        public CircularArea(double centerX, double centerY, double radius)
        {
            _center = new Position(centerX, centerY);
            _radius = radius;
        }
        private Position _center;
        private double _radius;
        public override bool Includes(Position pos) { 
            double distance = MathUtils.Distance(_center, pos);
            return distance <= _radius;
        }
    }

    class PolygonArea : Area
    {
        private List<Position> _vertices;

        public PolygonArea(List<Position> vertices)
        {
            if (vertices.Count <= 2) throw new Exception("Tried to initialize a polygon area with less than three vertices.");
            _vertices = vertices;
        }

        public override bool Includes(Position pos) //Ray Casting Algorithm - even intersection points = is outside the polygon.
        {
            bool IsInside = false;
            int previousVertex = _vertices.Count - 1;
            for (int nextVertex = 0; nextVertex < _vertices.Count; nextVertex++) //We iterate over pairs of neighbouring vertices, starting from Last-First vertices pair
            { 
                bool isYBetween = //Sharp inequalities, to prevent vertices[1].Y = vertices[2].Y.
                                  //Also makes the oblique sides only contain one of the vertices, in case the ray intersect directly with the vertex.
                                  //If the sides both create a sharp angle, the vertex counts as belonging to both :)
                    (pos.Y <= _vertices[nextVertex].Y && pos.Y > _vertices[previousVertex].Y)
                    || (pos.Y > _vertices[nextVertex].Y && pos.Y <= _vertices[previousVertex].Y);

                if (isYBetween)
                {
                    //height proportion at which the ray intersects with the polygon side - e.g. 0.5 means that in cuts it in half.
                    double intersectionProportion = (pos.Y - _vertices[nextVertex].Y) / (_vertices[previousVertex].Y - _vertices[nextVertex].Y);
                    //The same proportion but shifted to width (X axis)
                    double intersectionXProportion = intersectionProportion * (_vertices[previousVertex].X - _vertices[nextVertex].X);
                    // To get the final intersection X coordinate, we add the previous vertex' X coordinate
                    double intersectionX = _vertices[nextVertex].X + intersectionXProportion;

                    if (intersectionX > pos.X) //If the intersection point is to the right (where the ray was cast)
                    {
                        IsInside = !IsInside; //Negation
                    }
                }

                previousVertex = nextVertex;
            }
            return IsInside;
        }
    }

}