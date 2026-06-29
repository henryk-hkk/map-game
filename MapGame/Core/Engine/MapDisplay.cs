using MapGame.Core.Constants;
using MapGame.Core.Utils;
using MapGame.Core.Utils.Geographic;
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

        public static void ChangeAreaOwner(Color areaColor, int newRegionId)
        {
            PixelArea targetArea = Map.Areas[areaColor];

            int oldRegionId = (int)targetArea.parentRegionId;
            Region oldRegion = Map.Regions.Find(r => r.Id == oldRegionId);
            oldRegion?.Remove(targetArea);

            targetArea.parentRegionId = newRegionId;
            Region newRegion = Map.Regions.Find(r => r.Id == newRegionId);
            newRegion?.Add(targetArea);

            foreach (var pixel in targetArea.Pixels)
            {
                int index1D = (pixel.Y * Map.Width) + pixel.X;
                Map.GlobalRegionMap[index1D] = newRegionId;
            }

            BorderTexturesGenerator.UpdateBorders(targetArea.BorderPixelSegments);
        }

        public static GeometryModel3D GetMapDisplay(byte[] heightmap, int width, int height)
        {
            var model = MeshGenerator.Generate3DMapModel(heightmap, width, height);

            MaterialGroup materialGroup = new MaterialGroup();

            BitmapImage baseTexture = Map.TextureMap; //Map.TextureMap is not null, this function is called after the game engine does its thing and loads the maps.
            materialGroup.Children.Add(new DiffuseMaterial(new ImageBrush(baseTexture)));

            DiffuseMaterial riversMaterial = RiverTexturesGenerator.GenerateAnimatedRivers(Map.RiverMask, Map.WaterTexture);
            materialGroup.Children.Add(riversMaterial);

            if (Map.SelectionMaterial == null)
            {
                SelectionTexturesGenerator.InitializeSelectionRendering();
            }
            materialGroup.Children.Add(Map.SelectionMaterial);


            if (Map.BordersMaterial == null)
            {
                BorderTexturesGenerator.InitializeBorderRendering(Map.BorderGraph);
            }

            materialGroup.Children.Add(Map.BordersMaterial);

            model.Material = materialGroup;

            return model;
        }
    }
}
