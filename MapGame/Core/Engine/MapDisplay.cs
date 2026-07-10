using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Utils.Geographic;
using MapGame.Core.Utils.Graphic;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MapGame.Core.Engine
{
    public class MapRenderData
    {
        public MeshGeometry3D TerrainGeometry { get; set; }
        public MeshGeometry3D RiverGeometry { get; set; }
        public Material BaseMaterial { get; set; }
        public Material OverlayMaterial { get; set; }
        public Material RiverMaterial { get; set; }
    }

    public static class MapDisplay
    {
        public static void ChangeAreaOwner(System.Windows.Media.Color areaColor, int newRegionId)
        {
            PixelArea targetArea = GraphicContext.AreaColors[areaColor];

            if (targetArea.ParentRegionId.HasValue) 
            {
                int oldRegionId = (int)targetArea.ParentRegionId;
                Region oldRegion = MapContext.RegionIds[oldRegionId];
                oldRegion?.Remove(targetArea);
            }

            if (targetArea.ParentRegionId == newRegionId) return;

            targetArea.ParentRegionId = newRegionId;
            Region newRegion = MapContext.RegionIds[newRegionId];
            newRegion?.Add(targetArea);

            int newCountryId = newRegion?.Owner != null ? newRegion.Owner.Identifier.GetHashCode() : -2;

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var pixel in targetArea.Pixels)
            {
                int index1D = (pixel.Y * MapContext.Width) + pixel.X;
                MapContext.GlobalRegionMap[index1D] = newRegionId;
                MapContext.GlobalCountryMap[index1D] = newCountryId;

                if (pixel.X < minX) minX = pixel.X;
                if (pixel.X > maxX) maxX = pixel.X;
                if (pixel.Y < minY) minY = pixel.Y;
                if (pixel.Y > maxY) maxY = pixel.Y;
            }

            int margin = 6;

            Int32Rect dirtyRect = GraphicUtils.GetDirtyRect(minX, maxX, minY, maxY, margin);

            BorderTexturesGenerator.UpdateBorders(targetArea.BorderPixelSegments);
            CountryTexturesGenerator.RefreshDirtyRect(dirtyRect);

            OverlayCompositor.ComposeAndApply(dirtyRect);
        }

        public static void SelectRegionByAreaColor(System.Windows.Media.Color areaColor)
        {
            if (!GraphicContext.AreaColors.TryGetValue(areaColor, out PixelArea clickedArea)) return;
            if (clickedArea.ParentRegionId == null) return;

            int targetRegionId = (int)clickedArea.ParentRegionId;
            if (targetRegionId == MapContext.CurrentlySelectedRegionId) return;

            ClearSelection();

            MapContext.CurrentlySelectedRegionId = targetRegionId;

            var updateRect = SelectionTexturesGenerator.GetSelectionUpdateDirtyRect(targetRegionId);
            if (updateRect.IsEmpty) return;

            SelectionTexturesGenerator.RefreshDirtyRect(updateRect);
            OverlayCompositor.ComposeAndApply(updateRect);
        }

        public static void ClearSelection()
        {
            if (MapContext.CurrentlySelectedRegionId == -1) return;

            SelectionTexturesGenerator.ClearSelection();
            var selectionRect = SelectionTexturesGenerator.GetClearedSelectionDirtyRect();
            if (!selectionRect.IsEmpty) OverlayCompositor.ComposeAndApply(selectionRect);
        }


        public static MapRenderData GetMapDisplay(byte[] heightmap, int width, int height)
        {
            var data = new MapRenderData
            {
                TerrainGeometry = TerrainMeshGenerator.Generate3DMapModel(heightmap, width, height),
                RiverGeometry = RiverMeshGenerator.GenerateRiverMesh(MapContext.RiverMask, heightmap),

                BaseMaterial = new PhongMaterial()
                {
                    DiffuseMap = GraphicContext.TextureMap.ToTextureModel(),
                    DiffuseColor = HelixToolkit.Maths.Color4.White,
                    AmbientColor = HelixToolkit.Maths.Color4.White
                }
            };

            if (GraphicContext.TextureMap == null)
                throw new Exception("Tekstura bazowa nie została załadowana!");

            if (GraphicContext.TextureMap.PixelWidth == 0)
                throw new Exception("Tekstura ma wymiar 0!");

            if (GraphicContext.OverlayMaterial == null)
            {
                CountryTexturesGenerator.Initialize();
                BorderTexturesGenerator.Initialize();
                SelectionTexturesGenerator.Initialize();

                OverlayCompositor.InitializeCompositor();

                OverlayCompositor.ComposeAndApply(new Int32Rect(0, 0, width, height));
            }

            data.OverlayMaterial = GraphicContext.OverlayMaterial;

            data.RiverMaterial = new PhongMaterial()
            {
                DiffuseMap = GraphicContext.WaterTexture.ToTextureModel(),
                DiffuseColor = HelixToolkit.Maths.Color4.White,
                AmbientColor = HelixToolkit.Maths.Color4.White,

                UVTransform = new HelixToolkit.SharpDX.UVTransform()
                {
                    Rotation = 0f,
                    Scaling = new System.Numerics.Vector2(1f, 1f),
                    Translation = new System.Numerics.Vector2(0f, 0f)
                }
            };

            return data;
        }
    }
}