using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core;
using MapGame.Core.Geographic;
using MapGame.Core.Utils.Map;
using MapGame.MVVM.ViewModels;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MapGame.MVVM.Views
{
    public partial class MainWindow : Window
    {
        private Color? _currentPanelRegionColor = null;
        private DateTime _lastMouseMoveTime;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ShowRegionPanel()
        {
            RegionInfoPanel.Visibility = Visibility.Visible;

            DoubleAnimation slideIn = new()
            {
                From = -340,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(100)
            };

            RegionInfoPanelTransform.BeginAnimation(TranslateTransform.XProperty, slideIn);
        }

        private void HideRegionPanel()
        {
            DoubleAnimation slideOut = new()
            {
                From = 0,
                To = -340,
                Duration = TimeSpan.FromMilliseconds(100)
            };

            slideOut.Completed += (sender, e) =>
            {
                RegionInfoPanel.Visibility = Visibility.Collapsed;
            };

            RegionInfoPanelTransform.BeginAnimation(TranslateTransform.XProperty, slideOut);
        }
        //private int _lastHoveredRegionId = -2;

        private void OnViewportMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if ((DateTime.Now - _lastMouseMoveTime).TotalMilliseconds < 33) return;
            _lastMouseMoveTime = DateTime.Now;

            Position? mousePosition = GetSimpleMousePosition(e);

            if(mousePosition ==  null)
            {
                CursorCoordsText.Text = string.Empty;
                return;
            }

            Region? hoveredRegion = null;

            foreach (Region region in MapLogicContext.Regions)
            {
                if (region.Includes(mousePosition))
                {
                    hoveredRegion = region;
                    break;
                }
            }

            int mapX = (int)mousePosition.X;
            int mapY = (int)mousePosition.Y;

            if (hoveredRegion != null)
            {
                CursorCoordsText.Text = $"X: {mapX} | Y: {mapY} | Region: {LanguageContext.RegionNameTags[hoveredRegion.NameTag]} ({hoveredRegion.Id})";
            }
            else
            {
                CursorCoordsText.Text = $"X: {mapX} | Y: {mapY} | Region: brak";
            }
        }

        private void OnTerrainMouseDown(object sender, RoutedEventArgs e)
        {
            if (e is not Mouse3DEventArgs mouseArgs) return;
            if (this.DataContext is not MapViewModel viewModel) return;


            if (mouseArgs.OriginalInputEventArgs is MouseButtonEventArgs btnArgs && btnArgs.LeftButton == MouseButtonState.Pressed)
            {
                var mousePosition = GetBaricentricMousePosition(mouseArgs);
                if (mousePosition == null) return;

                Color clickedColor = MapUtils.GetColorByPosition(mousePosition);

                if (clickedColor.R == 0 && clickedColor.G == 0 && clickedColor.B == 0) return;
                _currentPanelRegionColor = clickedColor;

                string oldRegionName = RegionNameText.Text;

                if (GraphicContext.AreaColors.TryGetValue(clickedColor, out PixelArea? clickedArea) &&
                    clickedArea != null &&
                    clickedArea.ParentRegionId.HasValue)
                {
                    Region? clickedRegion = MapLogicContext.RegionIds[clickedArea.ParentRegionId.Value];

                    string regionName = clickedRegion?.DisplayName ?? "Nieznany region";
                    viewModel.SelectRegion(clickedColor, regionName);

                    RegionNameText.Text = regionName;
                }
                else
                {
                    RegionNameText.Text = "Nieznany region";
                }
                
                if (oldRegionName == RegionNameText.Text && RegionInfoPanel.Visibility == Visibility.Visible) return; 
                
                ShowRegionPanel();
            }
            else if (mouseArgs.OriginalInputEventArgs is MouseButtonEventArgs rightBtnArgs && rightBtnArgs.RightButton == MouseButtonState.Pressed)
            {
                viewModel.DeselectRegion();
                _currentPanelRegionColor = null;
                HideRegionPanel();
            }
        }

        private void OnViewportMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this.DataContext is not MapGame.MVVM.ViewModels.MapViewModel viewModel) return;
            viewModel.CameraController.Zoom(e.Delta);

            e.Handled = true;
        }

        private Position? GetSimpleMousePosition(MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(MainViewport);

            var hits = MainViewport.FindHits(mousePos);
            if (hits == null || hits.Count <= 0) return null;

            var firstHit = hits[0];
            if (firstHit.ModelHit is not MeshGeometryModel3D) return null;

            CursorTransform.X = mousePos.X + 15;
            CursorTransform.Y = mousePos.Y + 15;

            var hitPoint = firstHit.PointHit;

            return new(hitPoint.X, hitPoint.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Position? GetBaricentricMousePosition(Mouse3DEventArgs mouseArgs)
        {
            if (mouseArgs.HitTestResult == null) return null;

            var hit = mouseArgs.HitTestResult;
            if (hit.TriangleIndices == null) return null;
            if (hit.Geometry is not HelixToolkit.SharpDX.MeshGeometry3D mesh) return null;
            if (mesh.Positions == null || mesh.TextureCoordinates == null) return null;

            int i1 = hit.TriangleIndices.Item1;
            int i2 = hit.TriangleIndices.Item2;
            int i3 = hit.TriangleIndices.Item3;

            Vector3 p1 = mesh.Positions[i1];
            Vector3 p2 = mesh.Positions[i2];
            Vector3 p3 = mesh.Positions[i3];

            Vector3 p = hit.PointHit;

            Vector3 v0 = p2 - p1;
            Vector3 v1 = p3 - p1;
            Vector3 v2 = p - p1;

            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);

            float denom = d00 * d11 - d01 * d01;

            if (Math.Abs(denom) <= 0.000001f) return null;

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            Vector2 uv1 = mesh.TextureCoordinates[i1];
            Vector2 uv2 = mesh.TextureCoordinates[i2];
            Vector2 uv3 = mesh.TextureCoordinates[i3];

            float hitU = (uv1.X * u) + (uv2.X * v) + (uv3.X * w);
            float hitV = (uv1.Y * u) + (uv2.Y * v) + (uv3.Y * w);

            float x = hitU * MapContext.Width, y = hitV * MapContext.Height;

            return new(x, y);
        }
    }
}