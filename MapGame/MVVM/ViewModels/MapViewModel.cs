using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Constants;
using MapGame.Core.Engine;
using MapGame.Core.Utils.Graphic;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace MapGame.MVVM.ViewModels
{
    public class MapViewModel : INotifyPropertyChanged
    {
        private string _selectedRegionName = "Brak wyboru";
        private float _riverScrollY = 0f;

        public IEffectsManager EffectsManager { get; }

        public PerspectiveCamera MainCamera { get; }
        public MapCameraController CameraController { get; }
        private TimeSpan _lastRenderTime = TimeSpan.Zero;

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

        private HelixToolkit.Wpf.SharpDX.Material _terrainBaseMaterial;
        public HelixToolkit.Wpf.SharpDX.Material TerrainBaseMaterial
        {
            get => _terrainBaseMaterial;
            set { _terrainBaseMaterial = value; OnPropertyChanged(); }
        }

        // Zastępuje CountryMaterial, BordersMaterial i SelectionMaterial
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

        public string SelectedRegionName
        {
            get => _selectedRegionName;
            set { _selectedRegionName = value; OnPropertyChanged(); }
        }

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
            var mapData = MapDisplay.GetMapDisplay(Map.HeightMap, Map.Width, Map.Height);

            TerrainGeometry = mapData.TerrainGeometry;
            RiverGeometry = mapData.RiverGeometry;

            TerrainGeometry.UpdateOctree();
            if (RiverGeometry != null) RiverGeometry.UpdateOctree();

            TerrainBaseMaterial = mapData.BaseMaterial;
            OverlayMaterial = mapData.OverlayMaterial; // Nowy materiał
            RiverMaterial = mapData.RiverMaterial;
        }

        public void SelectRegion(Color areaColor)
        {
            SelectionTexturesGenerator.SelectRegionByAreaColor(areaColor);

            if (Map.Areas.TryGetValue(areaColor, out var area))
            {
                var region = Map.Regions.Find(r => r.Id == area.ParentRegionId);
                SelectedRegionName = region != null ? region.Name : "Nieznany Region";
            }
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

            _riverScrollY -= 0.002f;
            if (_riverScrollY < -1.0f) _riverScrollY += 1.0f;

            if (RiverMaterial is HelixToolkit.Wpf.SharpDX.PhongMaterial wpfMaterial &&
                wpfMaterial.Core is HelixToolkit.SharpDX.Model.PhongMaterialCore coreMaterial)
            {
                coreMaterial.UVTransform = new HelixToolkit.SharpDX.UVTransform()
                {
                    Rotation = 0f,
                    Scaling = new Vector2(1f, 1f),
                    Translation = new Vector2(0f, _riverScrollY)
                };
            }
        }

        public void DeselectRegion()
        {
            SelectedRegionName = "Brak wyboru";
            SelectionTexturesGenerator.ClearSelection();
        }

        public void AnnexSelectedArea(Color areaColor, int newRegionId)
        {
            MapDisplay.ChangeAreaOwner(areaColor, newRegionId);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}