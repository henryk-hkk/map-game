using MapGame.Core.Constants;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Utils.Graphic
{
    public static class MeshGenerator
    {

        private const int _step = 8;
        private const int _seaLevelPixelHeight = 0;
        private const double _heightScale = 0.3;
        private const byte _seaLevel = 143;
        private const int _landPixelHeightOffset = 0;

        public static GeometryModel3D Generate3DMapModel(byte[] heightmap, int width, int height)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            LoadPixelHeights(ref mesh, heightmap, width, height);
            GenerateTriangularTerrain(ref mesh, width, height);

            mesh.Freeze();

            GeometryModel3D model = new GeometryModel3D();
            model.Geometry = mesh;

            return model;
        }
        private static void LoadPixelHeights(ref MeshGeometry3D mesh, byte[] heightmap, int width, int height)
        {

            int cols = width / _step;
            int rows = height / _step;

            for (int r = 0; r <= rows; r++)
            {
                for (int c = 0; c <= cols; c++)
                {
                    int x = Math.Min(c * _step, width - 1);
                    int y = Math.Min(r * _step, height - 1);

                    int index = y * width + x;
                    byte z = heightmap[index];
                    bool isLand = Map.LandMask[index];

                    double finalPixelHeight;

                    if (isLand)
                    {
                        finalPixelHeight = (z - _seaLevel) * _heightScale + _landPixelHeightOffset;
                        if (finalPixelHeight < _seaLevelPixelHeight) finalPixelHeight = _seaLevelPixelHeight;
                    }
                    else
                    {
                        finalPixelHeight = _seaLevelPixelHeight;
                    }

                    mesh.Positions.Add(new Point3D(x, finalPixelHeight, y));

                    mesh.TextureCoordinates.Add(new Point((double)x / width, (double)y / height));
                }
            }
        }

        private static void GenerateTriangularTerrain(ref MeshGeometry3D mesh, int width, int height)
        {
            int cols = width / _step;
            int rows = height / _step;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int topLeft = r * (cols + 1) + c;
                    int topRight = topLeft + 1;
                    int bottomLeft = (r + 1) * (cols + 1) + c;
                    int bottomRight = bottomLeft + 1;

                    mesh.TriangleIndices.Add(topLeft);
                    mesh.TriangleIndices.Add(bottomLeft);
                    mesh.TriangleIndices.Add(topRight);

                    mesh.TriangleIndices.Add(topRight);
                    mesh.TriangleIndices.Add(bottomLeft);
                    mesh.TriangleIndices.Add(bottomRight);
                }
            }
        }
    }
}
