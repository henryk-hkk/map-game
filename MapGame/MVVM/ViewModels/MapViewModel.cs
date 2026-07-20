using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core;
using MapGame.Core.Engine;
using MapGame.Core.Geographic;
using MapGame.Core.Utils.Graphic;
using MapGame.Core.Utils.Map;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace MapGame.MVVM.ViewModels
{
    public class MapViewModel : INotifyPropertyChanged
    {
        private float _riverScrollY = 0f;
        private float _lakeScrollX = 0f;
        private float _lakeScrollY = 0f;
        private TimeSpan _lastRenderTime = TimeSpan.Zero;
        private readonly Region _defaultRegion = new (0, "", "Brak wyboru");

        public IEffectsManager EffectsManager { get; }

        public PerspectiveCamera MainCamera { get; }
        public MapCameraController CameraController { get; }

        private HelixToolkit.SharpDX.MeshGeometry3D _terrainGeometry;
        public HelixToolkit.SharpDX.MeshGeometry3D TerrainGeometry
        {
            get => _terrainGeometry;
            set { _terrainGeometry = value; OnPropertyChanged(); }
        }

        private HelixToolkit.SharpDX.MeshGeometry3D _riverGeometry;
        public HelixToolkit.SharpDX.MeshGeometry3D RiverGeometry
        {
            get => _riverGeometry;
            set { _riverGeometry = value; OnPropertyChanged(); }
        }
        private HelixToolkit.SharpDX.MeshGeometry3D _lakeGeometry;
        public HelixToolkit.SharpDX.MeshGeometry3D LakeGeometry
        {
            get => _lakeGeometry;
            set { _lakeGeometry = value; OnPropertyChanged(); }
        }

        private HelixToolkit.Wpf.SharpDX.Material _terrainBaseMaterial;
        public HelixToolkit.Wpf.SharpDX.Material TerrainBaseMaterial
        {
            get => _terrainBaseMaterial;
            set { _terrainBaseMaterial = value; OnPropertyChanged(); }
        }

        private HelixToolkit.Wpf.SharpDX.Material _overlayMaterial;
        public HelixToolkit.Wpf.SharpDX.Material OverlayMaterial
        {
            get => _overlayMaterial;
            set { _overlayMaterial = value; OnPropertyChanged(); }
        }

        private HelixToolkit.Wpf.SharpDX.Material _riverMaterial;
        public HelixToolkit.Wpf.SharpDX.Material RiverMaterial
        {
            get => _riverMaterial;
            set { _riverMaterial = value; OnPropertyChanged(); }
        }

        private HelixToolkit.Wpf.SharpDX.Material _lakeMaterial;
        public HelixToolkit.Wpf.SharpDX.Material LakeMaterial
        {
            get => _lakeMaterial;
            set { _lakeMaterial = value; OnPropertyChanged(); }
        }

        private Region _selectedRegion;
        public Region SelectedRegion
        {
            get => _selectedRegion;
            set { _selectedRegion = value; OnPropertyChanged(); OnPropertyChanged(nameof(RegionLanguagePercentages)); }
        }

        public Dictionary<string, int> RegionLanguagePercentages =>
            SelectedRegion?.LanguagePercentages ?? [];

        public MapViewModel()
        {
            EffectsManager = new DefaultEffectsManager();

            MainCamera = new PerspectiveCamera()
            {
                Position = new System.Windows.Media.Media3D.Point3D(3072, 2500, 2000),
                LookDirection = new System.Windows.Media.Media3D.Vector3D(0, -1, -0.01),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                FarPlaneDistance = 15000
            };

            CameraController = new MapCameraController(MainCamera);

            Initialize3DMap();
            CompositionTarget.Rendering += OnGameUpdate;
        }

        private void Initialize3DMap()
        {
            var mapData = MapDisplay.GetMapDisplay(MapContext.HeightMap, MapContext.Width, MapContext.Height);

            TerrainGeometry = mapData.TerrainGeometry;
            RiverGeometry = mapData.RiverGeometry;
            LakeGeometry = mapData.LakeGeometry;

            TerrainGeometry.UpdateOctree();
            RiverGeometry?.UpdateOctree();
            LakeGeometry?.UpdateOctree();

            TerrainBaseMaterial = mapData.BaseMaterial;
            OverlayMaterial = mapData.OverlayMaterial;
            RiverMaterial = mapData.RiverMaterial;
            LakeMaterial = mapData.LakeMaterial;
        }

        public static Region? GetRegionByMousePosition(Position mousePosition)
        {
            Color clickedColor = MapUtils.GetColorByPosition(mousePosition);

            if (clickedColor.R == 0 && clickedColor.G == 0 && clickedColor.B == 0) return null;

            if (GraphicContext.AreaColors.TryGetValue(clickedColor, out PixelArea? clickedArea) &&
                clickedArea != null &&
                clickedArea.ParentRegionId.HasValue)
            {
                Region? clickedRegion = MapLogicContext.RegionIds[clickedArea.ParentRegionId.Value];

                return clickedRegion;
            }
            return null;
        }

        private void OnGameUpdate(object sender, EventArgs e)
        {
            if (e is RenderingEventArgs args)
            {
                if (_lastRenderTime == TimeSpan.Zero) _lastRenderTime = args.RenderingTime;
                double deltaTime = (args.RenderingTime - _lastRenderTime).TotalSeconds;
                _lastRenderTime = args.RenderingTime;

                CameraController.Update(deltaTime);
            }

            AnimateRivers();

            AnimateLakes();
        }

        private void AnimateRivers()
        {
            _riverScrollY -= 0.002f;
            if (_riverScrollY < -1.0f) _riverScrollY += 1.0f;

            if (RiverMaterial is PhongMaterial wpfMaterial &&
                wpfMaterial.Core is HelixToolkit.SharpDX.Model.PhongMaterialCore coreMaterial)
            {
                coreMaterial.UVTransform = new UVTransform()
                {
                    Rotation = 0f,
                    Scaling = new Vector2(1f, 1f),
                    Translation = new Vector2(0f, _riverScrollY)
                };
            }
        }

        private void AnimateLakes()
        {
            _lakeScrollX += 0.0005f;
            _lakeScrollY += 0.0008f;

            if (_lakeScrollX > 1.0f) _lakeScrollX -= 1.0f;
            if (_lakeScrollY > 1.0f) _lakeScrollY -= 1.0f;

            if (LakeMaterial is PhongMaterial wpfLakeMat &&
                wpfLakeMat.Core is HelixToolkit.SharpDX.Model.PhongMaterialCore coreLakeMat)
            {
                coreLakeMat.UVTransform = new UVTransform()
                {
                    Rotation = 0f,
                    Scaling = new Vector2(1f, 1f),
                    Translation = new Vector2(_lakeScrollX, _lakeScrollY)
                };
            }
        }
        public void SelectRegion(Region? region)
        {
            if (region == null)
            {
                DeselectRegion();
                return;
            }
            MapDisplay.SelectRegion(region.Id);
            SelectedRegion = region;
        }
        public void DeselectRegion()
        {
            SelectedRegion = _defaultRegion;
            MapDisplay.ClearSelection();
        }

        public static void AnnexSelectedArea(Color areaColor, int newRegionId)
        {
            EngineCommands.SetAreaParent(areaColor, newRegionId);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}