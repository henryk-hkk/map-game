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

            int oldRegionId = (int)targetArea.ParentRegionId;
            Region oldRegion = Map.Regions.Find(r => r.Id == oldRegionId);
            oldRegion?.Remove(targetArea);

            targetArea.ParentRegionId = newRegionId;
            Region newRegion = Map.Regions.Find(r => r.Id == newRegionId);
            newRegion?.Add(targetArea);

            int newCountryId = newRegion?.Owner != null ? newRegion.Owner.Identifier.GetHashCode() : -2;

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var pixel in targetArea.Pixels)
            {
                int index1D = (pixel.Y * Map.Width) + pixel.X;
                Map.GlobalRegionMap[index1D] = newRegionId;
                Map.GlobalCountryMap[index1D] = newCountryId;

                if (pixel.X < minX) minX = pixel.X;
                if (pixel.X > maxX) maxX = pixel.X;
                if (pixel.Y < minY) minY = pixel.Y;
                if (pixel.Y > maxY) maxY = pixel.Y;
            }

            int margin = 6;
            minX = Math.Max(0, minX - margin);
            minY = Math.Max(0, minY - margin);
            maxX = Math.Min(Map.Width - 1, maxX + margin);
            maxY = Math.Min(Map.Height - 1, maxY + margin);
            Int32Rect dirtyRect = new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);

            BorderTexturesGenerator.UpdateBorders(targetArea.BorderPixelSegments);

            CountryTexturesGenerator.RefreshCountryDirtyRect(dirtyRect);
        }

        public static Model3DGroup GetMapDisplay(byte[] heightmap, int width, int height)
        {
            Model3DGroup mapGroup = new Model3DGroup();

            var landModel = TerrainMeshGenerator.Generate3DMapModel(heightmap, width, height);
            MaterialGroup landMaterials = new MaterialGroup();

            BitmapImage baseTexture = Map.TextureMap;
            landMaterials.Children.Add(new DiffuseMaterial(new ImageBrush(baseTexture)));

            if (Map.CountryMaterial == null)
            {
                CountryTexturesGenerator.InitializeCountryRendering();
            }
            landMaterials.Children.Add(Map.CountryMaterial);

            if (Map.SelectionMaterial == null)
            {
                SelectionTexturesGenerator.InitializeSelectionRendering();
            }
            landMaterials.Children.Add(Map.SelectionMaterial);

            if (Map.RegionBordersMaterial == null)
            {
                BorderTexturesGenerator.InitializeBorderRendering(Map.BorderGraph);
            }
            landMaterials.Children.Add(Map.RegionBordersMaterial);

            landModel.Material = landMaterials;

            mapGroup.Children.Add(landModel);

            GeometryModel3D riversModel = RiverMeshGenerator.GenerateAnimatedRivers(Map.RiverMask, Map.WaterTexture, heightmap);

            mapGroup.Children.Add(riversModel);

            return mapGroup;
        }
    }
}
