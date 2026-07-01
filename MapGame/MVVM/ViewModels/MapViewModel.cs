using MapGame.Core.Constants;
using MapGame.Core.Engine;
using MapGame.Core.Utils.Graphic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;


namespace MapGame.MVVM.ViewModels
{
    public class MapViewModel : INotifyPropertyChanged
    {
        private Core.Engine.Camera _camera;
        private DateTime _lastFrameTime = DateTime.Now;
        private Model3DGroup _terrainModel;
        private string _selectedRegionName = "Brak wyboru";

        public Point3D CameraPosition => _camera.Position;
        public Vector3D CameraLookDirection => _camera.LookDirection;
        public Vector3D CameraUpDirection => _camera.UpDirection;
        public Model3DGroup TerrainModel
        {
            get => _terrainModel;
            private set
            {
                _terrainModel = value;
                OnPropertyChanged();
            }
        }
        public string SelectedRegionName
        {
            get => _selectedRegionName;
            set
            {
                _selectedRegionName = value;
                OnPropertyChanged();
            }
        }

        public MapViewModel() 
        {
            InitializeCamera();
            Initialize3DMap();

            CompositionTarget.Rendering += OnGameUpdate;
        }
        private void InitializeCamera()
        {
            _camera = new Core.Engine.Camera(
                new Point3D(3072, 2000, 2500),
                new Vector3D(0, -1, -0.01),
                new Vector3D(0, 1, 0)
            );
        }

        private void Initialize3DMap()
        {
            TerrainModel = MapDisplay.GetMapDisplay(Map.HeightMap, Map.Width, Map.Height);
        }

        public void SelectRegion(Color areaColor)
        {
            SelectionTexturesGenerator.SelectRegionByAreaColor(areaColor);

            if (Map.Areas.TryGetValue(areaColor, out var area))
            {
                var region = Map.Regions.Find(r => r.Id == area.parentRegionId);
                SelectedRegionName = region != null ? region.Name : "Nieznany Region";
            }

            OnPropertyChanged(nameof(TerrainModel));
        }

        public void DeselectRegion()
        {
            SelectedRegionName = "Brak wyboru";

            SelectionTexturesGenerator.ClearSelection();
        }

        public void AnnexSelectedArea(Color areaColor, int newRegionId)
        {
            MapDisplay.ChangeAreaOwner(areaColor, newRegionId);

            OnPropertyChanged(nameof(TerrainModel));
        }

        public void ZoomCamera(double delta)
        {
            _camera.Zoom(delta);
            OnPropertyChanged(nameof(CameraPosition));
        }

        private void OnGameUpdate(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double deltaTime = (now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;

            _camera.WASD(deltaTime);
            _camera.Update();
            OnPropertyChanged(nameof(CameraPosition));
            OnPropertyChanged(nameof(CameraLookDirection));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
