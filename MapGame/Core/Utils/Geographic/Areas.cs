using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.Utils.Geographic
{
    public abstract class Area
    {
        public abstract bool Includes(Position pos);
        public string? Name {  get; set; }
        public int? ParentRegionId { get; set; }
        public int? Population { get; set; }

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

    public class PolygonArea : Area
    {
        public List<Position> Vertices { get; private set; }

        public PolygonArea(List<Position> vertices)
        {
            if (vertices.Count <= 2) throw new Exception("Tried to initialize a polygon area with less than three vertices.");
            Vertices = vertices;
        }

        public PolygonArea(Position[] vertices)
        {
            if (vertices.Count() <= 2) throw new Exception("Tried to initialize a polygon area with less than three vertices.");
            Vertices = [.. vertices];
        }

        public override bool Includes(Position pos) //Ray Casting Algorithm - even intersection points = is outside the polygon.
        {
            bool IsInside = false;
            int previousVertex = Vertices.Count - 1;
            for (int nextVertex = 0; nextVertex < Vertices.Count; nextVertex++) //We iterate over pairs of neighbouring vertices, starting from Last-First vertices pair
            { 
                bool isYBetween = //Sharp inequalities, to prevent vertices[1].Y = vertices[2].Y.
                                  //Also makes the oblique sides only contain one of the vertices, in case the ray intersect directly with the vertex.
                                  //If the sides both create a sharp angle, the vertex counts as belonging to both :)
                    (pos.Y <= Vertices[nextVertex].Y && pos.Y > Vertices[previousVertex].Y)
                    || (pos.Y > Vertices[nextVertex].Y && pos.Y <= Vertices[previousVertex].Y);

                if (isYBetween)
                {
                    //height proportion at which the ray intersects with the polygon side - e.g. 0.5 means that in cuts it in half.
                    double intersectionProportion = (pos.Y - Vertices[nextVertex].Y) / (Vertices[previousVertex].Y - Vertices[nextVertex].Y);
                    //The same proportion but shifted to width (X axis)
                    double intersectionXProportion = intersectionProportion * (Vertices[previousVertex].X - Vertices[nextVertex].X);
                    // To get the final intersection X coordinate, we add the previous vertex' X coordinate
                    double intersectionX = Vertices[nextVertex].X + intersectionXProportion;

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

    public class PixelArea : Area
    {
        public List<Pixel> Pixels = new List<Pixel>();
        public void AddPixel(int x, int y)
        {
            Pixel pixel = new Pixel(x, y);
            Pixels.Add(pixel);
        }
        public List<BorderPixelSegment> BorderPixelSegments { get; set; } = new List<BorderPixelSegment>();
        public override bool Includes(Position pos)
        {
            int pxX = (int)Math.Ceiling(pos.X);
            int pxY = (int)Math.Ceiling(pos.Y);

            foreach (Pixel pixel in Pixels)
            {
                if (pixel.X == pxX && pixel.Y == pxY)
                {
                    return true;
                }
            }

            return false;
        }

    }

}