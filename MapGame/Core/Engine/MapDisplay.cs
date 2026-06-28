using MapGame.Core.Constants;
using MapGame.Core.Utils;
using MapGame.Core.Utils.Graphic;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Engine
{
    public static class MapDisplay
    {
        private const int _scale = 2;

        public static GeometryModel3D GetMapDisplay(byte[] heightmap, int width, int height)
        {
            var model = MeshGenerator.Generate3DMapModel(heightmap, width, height);

            MaterialGroup materialGroup = new MaterialGroup();

            BitmapImage baseTexture = Map.TextureMap; //Map.TextureMap is not null, this function is called after the game engine does its thing and loads the maps.
            materialGroup.Children.Add(new DiffuseMaterial(new ImageBrush(baseTexture)));

            DiffuseMaterial bordersMaterial = GenerateRegionBorders();
            materialGroup.Children.Add(bordersMaterial);

            model.Material = materialGroup;

            return model;
        }

        private static DiffuseMaterial GenerateRegionBorders()
        {
            var (width, height, stride) = MapUtils.GetBitmapParams();
            var regionMap = MapUtils.GetRegionMap(width, height);

            int scaledWidth = width * _scale;
            int scaledHeight = height * _scale;
            int scaledStride = scaledWidth * 4;

            var borderPixels = SDFAgent.GetSmoothSDFBorders(regionMap, width, height, _scale);

            WriteableBitmap bitmap = new WriteableBitmap(scaledWidth, scaledHeight, 96, 96, PixelFormats.Bgra32, null);
            bitmap.WritePixels(new Int32Rect(0, 0, scaledWidth, scaledHeight), borderPixels, scaledStride, 0);
            bitmap.Freeze();

            ImageBrush brush = new ImageBrush(bitmap);
            RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.HighQuality);
            brush.Freeze();

            return new DiffuseMaterial(brush);
        }
    }
}
