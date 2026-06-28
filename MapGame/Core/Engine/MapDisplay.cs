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
        

        public static GeometryModel3D GetMapDisplay(byte[] heightmap, int width, int height)
        {
            var model = MeshGenerator.Generate3DMapModel(heightmap, width, height);

            MaterialGroup materialGroup = new MaterialGroup();

            BitmapImage baseTexture = Map.TextureMap; //Map.TextureMap is not null, this function is called after the game engine does its thing and loads the maps.
            materialGroup.Children.Add(new DiffuseMaterial(new ImageBrush(baseTexture)));

            DiffuseMaterial riversMaterial = RiverTexturesGenerator.GenerateAnimatedRivers(Map.RiverMask, Map.WaterTexture);
            materialGroup.Children.Add(riversMaterial);

            DiffuseMaterial bordersMaterial = BorderTexturesGenerator.GenerateRegionBorders();
            materialGroup.Children.Add(bordersMaterial);
            
            model.Material = materialGroup;

            return model;
        }
    }
}
