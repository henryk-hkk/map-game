using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Geographic;
using MapGame.Core.Utils.Graphic;
using MapGame.MVVM.Models;
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
        public MeshGeometry3D LakeGeometry { get; set; }
        public Material BaseMaterial { get; set; }
        public Material OverlayMaterial { get; set; }
        public Material RiverMaterial { get; set; }
        public Material LakeMaterial { get; set; }
    }

    public static class MapDisplay
    {
        public static void ChangeAreaParent(Area area, Region newRegion)
        {
            if (area is not PixelArea pArea) return;
            int newCountryId = newRegion?.Owner != null ? newRegion.Owner.Identifier.GetHashCode() : -2;

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var pixel in pArea.Pixels)
            {
                int index1D = (pixel.Y * MapContext.Width) + pixel.X;
                MapLogicContext.GlobalRegionMap[index1D] = newRegion.Id;
                MapLogicContext.GlobalCountryMap[index1D] = newCountryId;

                if (pixel.X < minX) minX = pixel.X;
                if (pixel.X > maxX) maxX = pixel.X;
                if (pixel.Y < minY) minY = pixel.Y;
                if (pixel.Y > maxY) maxY = pixel.Y;
            }

            int margin = 6;

            Int32Rect dirtyRect = GraphicUtils.GetDirtyRect(minX, maxX, minY, maxY, margin);

            BorderTexturesGenerator.UpdateBorders(pArea.BorderPixelSegments);
            BorderTexturesGenerator.RefreshDirtyRect(dirtyRect);
            CountryTexturesGenerator.RefreshDirtyRect(dirtyRect);

            OverlayCompositor.ComposeAndApply(dirtyRect);
        }
        public static void ChangeAreasParent(IEnumerable<Area> areas, Region newRegion)
        {
            int newCountryId = newRegion?.Owner != null ? newRegion.Owner.Identifier.GetHashCode() : -2;
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (Area area in areas)
            {
                if (area is not PixelArea pArea) continue;
                foreach (var pixel in pArea.Pixels)
                {
                    int index1D = (pixel.Y * MapContext.Width) + pixel.X;
                    MapLogicContext.GlobalCountryMap[index1D] = newCountryId;

                    if (pixel.X < minX) minX = pixel.X;
                    if (pixel.X > maxX) maxX = pixel.X;
                    if (pixel.Y < minY) minY = pixel.Y;
                    if (pixel.Y > maxY) maxY = pixel.Y;
                }

                BorderTexturesGenerator.UpdateBorders(pArea.BorderPixelSegments);
            }

            int margin = 6;

            Int32Rect dirtyRect = GraphicUtils.GetDirtyRect(minX, maxX, minY, maxY, margin);

            BorderTexturesGenerator.RefreshDirtyRect(dirtyRect);
            CountryTexturesGenerator.RefreshDirtyRect(dirtyRect);
            OverlayCompositor.ComposeAndApply(dirtyRect);
        }

        public static void ChangeRegionOwner(Region region, Country newOwner)
        {
            int newCountryId = newOwner.Identifier.GetHashCode();

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (Area area in region)
            {
                if (area is not PixelArea pArea) continue;
                foreach (var pixel in pArea.Pixels)
                {
                    int index1D = (pixel.Y * MapContext.Width) + pixel.X;
                    MapLogicContext.GlobalCountryMap[index1D] = newCountryId;

                    if (pixel.X < minX) minX = pixel.X;
                    if (pixel.X > maxX) maxX = pixel.X;
                    if (pixel.Y < minY) minY = pixel.Y;
                    if (pixel.Y > maxY) maxY = pixel.Y;
                }
            }

            int margin = 6;

            Int32Rect dirtyRect = GraphicUtils.GetDirtyRect(minX, maxX, minY, maxY, margin);
            CountryTexturesGenerator.RefreshDirtyRect(dirtyRect);

            OverlayCompositor.ComposeAndApply(dirtyRect);
        }

        public static void AnnexRegions(IEnumerable<Region> regions, Country country)
        {
            int newCountryId = country.Identifier.GetHashCode();

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach(Region region in regions)
            {
                foreach (Area area in region)
                {
                    if (area is not PixelArea pArea) continue;
                    foreach (var pixel in pArea.Pixels)
                    {
                        int index1D = (pixel.Y * MapContext.Width) + pixel.X;
                        MapLogicContext.GlobalCountryMap[index1D] = newCountryId;

                        if (pixel.X < minX) minX = pixel.X;
                        if (pixel.X > maxX) maxX = pixel.X;
                        if (pixel.Y < minY) minY = pixel.Y;
                        if (pixel.Y > maxY) maxY = pixel.Y;
                    }
                }
            }

            int margin = 6;

            Int32Rect dirtyRect = GraphicUtils.GetDirtyRect(minX, maxX, minY, maxY, margin);
            CountryTexturesGenerator.RefreshDirtyRect(dirtyRect);

            OverlayCompositor.ComposeAndApply(dirtyRect);
        }

        public static void SelectRegionByAreaColor(System.Windows.Media.Color areaColor)
        {
            if (!GraphicContext.AreaColors.TryGetValue(areaColor, out PixelArea clickedArea)) return;
            if (clickedArea.ParentRegionId == null) return;

            int targetRegionId = (int)clickedArea.ParentRegionId;
            SelectRegion(targetRegionId);
        }

        public static void SelectRegion(int regionId)
        {
            if (!EngineCommands.GetRegionById(regionId).Status) return;
            if (regionId == MapLogicContext.CurrentlySelectedRegionId) return;

            ClearSelection();

            MapLogicContext.CurrentlySelectedRegionId = regionId;

            var updateRect = SelectionTexturesGenerator.GetSelectionUpdateDirtyRect(regionId);
            if (updateRect.IsEmpty) return;

            SelectionTexturesGenerator.RefreshDirtyRect(updateRect);
            OverlayCompositor.ComposeAndApply(updateRect);
        }

        public static void ClearSelection()
        {
            if (MapLogicContext.CurrentlySelectedRegionId == -1) return;

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
                LakeGeometry = LakeMeshGenerator.GenerateLakeMesh(MapContext.LakeMask, heightmap, false),

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
                DiffuseMap = GraphicContext.RiverTexture.ToTextureModel(),
                DiffuseColor = HelixToolkit.Maths.Color4.White,
                AmbientColor = HelixToolkit.Maths.Color4.White,

                UVTransform = new HelixToolkit.SharpDX.UVTransform()
                {
                    Rotation = 0f,
                    Scaling = new System.Numerics.Vector2(1f, 1f),
                    Translation = new System.Numerics.Vector2(0f, 0f)
                }
            };

            data.LakeMaterial = new PhongMaterial()
            {
                DiffuseMap = GraphicContext.LakeTexture.ToTextureModel(),
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