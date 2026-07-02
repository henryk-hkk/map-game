using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Constants;
using MapGame.Core.Engine;
using MapGame.Core.Utils.Graphic;
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

        private HelixToolkit.Wpf.SharpDX.Material _countryMaterial;
        public HelixToolkit.Wpf.SharpDX.Material CountryMaterial
        {
            get => _countryMaterial;
            set { _countryMaterial = value; OnPropertyChanged(); }
        }

        private HelixToolkit.Wpf.SharpDX.Material _bordersMaterial;
        public HelixToolkit.Wpf.SharpDX.Material BordersMaterial
        {
            get => _bordersMaterial;
            set { _bordersMaterial = value; OnPropertyChanged(); }
        }

        private HelixToolkit.Wpf.SharpDX.Material _selectionMaterial;
        public HelixToolkit.Wpf.SharpDX.Material SelectionMaterial
        {
            get => _selectionMaterial;
            set { _selectionMaterial = value; OnPropertyChanged(); }
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

            Initialize3DMap();
            CompositionTarget.Rendering += OnGameUpdate;
        }

        private void Initialize3DMap()
        {
            var mapData = MapDisplay.GetMapDisplay(Map.HeightMap, Map.Width, Map.Height);

            TerrainGeometry = mapData.TerrainGeometry;
            RiverGeometry = mapData.RiverGeometry;

            TerrainBaseMaterial = mapData.BaseMaterial;
            CountryMaterial = mapData.CountryMaterial;
            SelectionMaterial = mapData.SelectionMaterial;
            BordersMaterial = mapData.BordersMaterial;
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
            // Ważne: w SharpDX aktualizacja tekstury w tle (np. zmapowanego bufora) 
            // często od razu odświeża widok bez konieczności robienia OnPropertyChanged całego modelu!
        }

        private void OnGameUpdate(object sender, EventArgs e)
        {
            _riverScrollY -= 0.002f;

            if (_riverScrollY < -1.0f) _riverScrollY += 1.0f;

            if (RiverMaterial is PhongMaterial phong)
            {
                phong.UVTransform = new HelixToolkit.SharpDX.UVTransform()
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
