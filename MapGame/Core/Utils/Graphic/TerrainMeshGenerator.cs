using HelixToolkit.Geometry;
using HelixToolkit.SharpDX;
using MapGame.Core.Constants;
using System;
using System.Numerics;

namespace MapGame.Core.Utils.Graphic
{
    public static class TerrainMeshGenerator
    {
        private const int _step = 8;
        private const int _seaLevelPixelHeight = 0;
        private const double _heightScale = 0.3;
        private const byte _seaLevel = 143;
        private const int _landPixelHeightOffset = 0;

        public static HelixToolkit.SharpDX.MeshGeometry3D Generate3DMapModel(byte[] heightmap, int width, int height)
        {
            var builder = new MeshBuilder(generateNormals: true, generateTexCoords: true);
            builder.TextureCoordinates = [];
            LoadPixelHeights(builder, heightmap, width, height);
            GenerateTriangularTerrain(builder, width, height);

            builder.ComputeNormalsAndTangents(MeshFaces.Default);

            var mesh = builder.ToMeshGeometry3D();

            mesh.UpdateOctree();

            return mesh;
        }

        public static double GetTerrainHeight(byte[] heightmap, int x, int y, int width, int height)
        {
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

            return finalPixelHeight;
        }

        private static void LoadPixelHeights(MeshBuilder builder, byte[] heightmap, int width, int height)
        {
            int cols = width / _step;
            int rows = height / _step;

            for (int r = 0; r <= rows; r++)
            {
                for (int c = 0; c <= cols; c++)
                {
                    int x = Math.Min(c * _step, width - 1);
                    int y = Math.Min(r * _step, height - 1);

                    float pixelHeight = (float)GetTerrainHeight(heightmap, x, y, width, height);

                    builder.Positions.Add(new Vector3((float)x, pixelHeight, (float)y));
                    float u = (float)x / (width - 1);
                    float v = (float)y / (height - 1);

                    builder.TextureCoordinates.Add(new System.Numerics.Vector2(u, v));
                    builder.Normals.Add(new System.Numerics.Vector3(0, 1, 0));
                }
            }
        }

        private static void GenerateTriangularTerrain(MeshBuilder builder, int width, int height)
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

                    builder.TriangleIndices.Add(topLeft);
                    builder.TriangleIndices.Add(bottomLeft);
                    builder.TriangleIndices.Add(topRight);

                    builder.TriangleIndices.Add(topRight);
                    builder.TriangleIndices.Add(bottomLeft);
                    builder.TriangleIndices.Add(bottomRight);
                }
            }
        }
    }
}