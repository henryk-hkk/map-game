using MapGame.Core.Utils;
using MapGame.Core.Utils.Map;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.Geographic
{
    public abstract class Area
    {
        public abstract bool Includes(Position pos);
        public string Identifier { get; set; } = string.Empty;
        public string? Name {  get => GetDisplayName(); }
        public int? ParentRegionId { get; set; }
        public int Population { get => GetOverallPopulation(); set => SetOverallPopulation(value); }
        private Dictionary<string, int> _languagePopulations = [];
        private Dictionary<string, int> _languagePercentages = [];
        public Dictionary<string, int> LanguagePopulations { get => _languagePopulations; set => SetLanguagePopulations(value); }
        public Dictionary<string, int> LanguagePercentages { get => _languagePercentages; set => SetLanguagePercentages(value); }
        public void InitPopulationData(int totalPopulation, Dictionary<string, int> percentages)
        {
            _languagePercentages = percentages ?? [];
            _languagePopulations.Clear();

            foreach (var language in _languagePercentages)
            {
                _languagePopulations[language.Key] = (int)(language.Value / 100.0 * totalPopulation);
            }

            AdjustLanguagePopulation(totalPopulation);
        }
        protected string GetDisplayName()
        {
            return (!string.IsNullOrEmpty(Identifier) && LanguageContext.AreaNames.TryGetValue(Identifier, out string name)) ? name : "";
        }

        protected int GetOverallPopulation()
        {
            if (_languagePopulations == null || _languagePopulations.Count == 0) return 0;
            return _languagePopulations.Values.Sum();
        }

        protected void SetOverallPopulation(int population)
        {
            if (population <= 0)
            {
                _languagePopulations.Clear();
                _languagePercentages.Clear();
                return;
            }

            int currentPopulation = GetOverallPopulation();
            if (currentPopulation == 0)
            {
                _languagePopulations.Clear();
                _languagePopulations.Add("Polish", population);
                UpdateLanguagePercentages();
                return;
            }

            int populationDif = population - currentPopulation;

            var keys = _languagePopulations.Keys.ToList();
            foreach (var key in keys)
            {
                if (_languagePercentages.TryGetValue(key, out int percentage))
                {
                    _languagePopulations[key] += (int)(percentage / 100.0 * populationDif);
                }
            }

            AdjustLanguagePopulation(population);
            UpdateLanguagePercentages();
        }

        protected void SetLanguagePopulations(Dictionary<string, int>? newPopulations)
        {
            _languagePopulations = newPopulations ?? [];
            UpdateLanguagePercentages();
        }

        protected void SetLanguagePercentages(Dictionary<string, int>? newPercentages)
        {
            _languagePercentages = newPercentages ?? [];
            UpdateLanguagePopulations();
        }

        protected void UpdateLanguagePopulations()
        {
            int wholePop = GetOverallPopulation();
            if (wholePop == 0) return;

            _languagePopulations.Clear();
            foreach (var language in _languagePercentages)
            {
                _languagePopulations[language.Key] = (int)(language.Value / 100.0 * wholePop);
            }

            AdjustLanguagePopulation(wholePop);
        }

        protected void AdjustLanguagePopulation(int wantedPopulation)
        {
            if (_languagePopulations.Count == 0) return;
            int currentPop = GetOverallPopulation();
            if (currentPop == wantedPopulation) return;

            int populationDif = wantedPopulation - currentPop;

            var maxLanguage = _languagePopulations.OrderByDescending(x => x.Value).First().Key;
            _languagePopulations[maxLanguage] += populationDif;
        }

        protected void UpdateLanguagePercentages()
        {
            int wholePop = GetOverallPopulation();
            if (wholePop == 0)
            {
                _languagePercentages.Clear();
                return;
            }

            _languagePercentages.Clear();
            foreach (var language in _languagePopulations)
            {
                _languagePercentages[language.Key] = (int)((double)language.Value / wholePop * 100.0);
            }

            MathUtils.AdjustPercentage(_languagePercentages);
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
            if (vertices.Length <= 2) throw new Exception("Tried to initialize a polygon area with less than three vertices.");
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
        public List<Pixel> Pixels = [];
        public void AddPixel(int x, int y)
        {
            Pixel pixel = new(x, y);
            Pixels.Add(pixel);
        }
        public List<BorderPixelSegment> BorderPixelSegments { get; set; } = [];
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