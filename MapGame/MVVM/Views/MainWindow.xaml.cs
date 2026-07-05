using MapGame.Core.Constants;
using MapGame.Core.Engine;
using MapGame.Core.Utils.Graphic;
using MapGame.MVVM.Models.Units;
using MapGame.MVVM.ViewModels;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MapGame.Core.Utils.Geographic;
using System.Windows.Media.Animation;
using System.Linq;

namespace MapGame.MVVM.Views
{
    public partial class MainWindow : Window
    {
        private Color? _currentPanelRegionColor = null;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ShowRegionPanel()
        {
            RegionInfoPanel.Visibility = Visibility.Visible;

            DoubleAnimation slideIn = new DoubleAnimation
            {
                From = -340,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(100)
            };

            RegionInfoPanelTransform.BeginAnimation(TranslateTransform.XProperty, slideIn);
        }

        private void HideRegionPanel()
        {
            DoubleAnimation slideOut = new DoubleAnimation
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

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DataContext is MapViewModel viewModel)
            {
                viewModel.ZoomCamera(e.Delta);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(MainViewport);

            CursorTransform.X = mousePos.X + 15;
            CursorTransform.Y = mousePos.Y + 15;

            HitTestResult result = VisualTreeHelper.HitTest(MainViewport, mousePos);

            if (result is RayMeshGeometry3DHitTestResult meshResult)
            {
                Point3D hitPoint = meshResult.PointHit;

                int mapX = (int)hitPoint.X;
                int mapY = (int)hitPoint.Z;

                Position mousePosition = new Position(hitPoint.X, hitPoint.Z);

                Region? hoveredRegion = null;

                foreach (Region region in Map.Regions)
                {
                    if (region.Includes(mousePosition))
                    {
                        hoveredRegion = region;
                        break;
                    }
                }

                if (hoveredRegion != null)
                {
                    CursorCoordsText.Text = $"X: {mapX} | Y: {mapY} | Region: {hoveredRegion.Id}";
                }
                else
                {
                    CursorCoordsText.Text = $"X: {mapX} | Y: {mapY} | Region: brak";
                }
            }
            else
            {
                CursorCoordsText.Text = string.Empty;
            }
        }
        private void OnViewportMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePosition = e.GetPosition(MainViewport);

            PointHitTestParameters hitParams = new PointHitTestParameters(mousePosition);

            VisualTreeHelper.HitTest(MainViewport, null, HitTestResultCallback, hitParams);
        }
        private HitTestResultBehavior HitTestResultCallback(HitTestResult result)
        {
            if (result is RayMeshGeometry3DHitTestResult hitResult)
            {
                MeshGeometry3D mesh = hitResult.MeshHit;
                if (mesh.TextureCoordinates == null || mesh.TextureCoordinates.Count == 0)
                    return HitTestResultBehavior.Stop;

                // Logika wag barycentrycznych
                int v1 = hitResult.VertexIndex1;
                int v2 = hitResult.VertexIndex2;
                int v3 = hitResult.VertexIndex3;
                double w1 = hitResult.VertexWeight1;
                double w2 = hitResult.VertexWeight2;
                double w3 = hitResult.VertexWeight3;

                Point uv1 = mesh.TextureCoordinates[v1];
                Point uv2 = mesh.TextureCoordinates[v2];
                Point uv3 = mesh.TextureCoordinates[v3];

                double hitU = (uv1.X * w1) + (uv2.X * w2) + (uv3.X * w3);
                double hitV = (uv1.Y * w1) + (uv2.Y * w2) + (uv3.Y * w3);

                int x = (int)(hitU * Map.Width);
                int y = (int)(hitV * Map.Height);

                if (x >= 0 && x < Map.Width && y >= 0 && y < Map.Height)
                {
                    int stride = Map.Width * 4;
                    int index = (y * stride) + (x * 4);

                    byte b = Map.AreaPixels[index];
                    byte g = Map.AreaPixels[index + 1];
                    byte r = Map.AreaPixels[index + 2];

                    if (!(r == 0 && g == 0 && b == 0))
                    {
                        Color clickedColor = Color.FromRgb(r, g, b);

                        if (_currentPanelRegionColor.HasValue &&
                            _currentPanelRegionColor.Value == clickedColor &&
                            RegionInfoPanel.Visibility == Visibility.Visible)
                        {
                            return HitTestResultBehavior.Stop;
                        }

                        if (this.DataContext is MapViewModel viewModel)
                        {
                            viewModel.SelectRegion(clickedColor);

                            _currentPanelRegionColor = clickedColor;

                            if (Map.Areas.TryGetValue(clickedColor, out PixelArea clickedArea) &&
                                clickedArea.ParentRegionId.HasValue)
                            {
                                Region? clickedRegion = Map.Regions
                                    .FirstOrDefault(region => region.Id == clickedArea.ParentRegionId.Value);

                                RegionNameText.Text = clickedRegion?.Name ?? "Nieznany region";
                            }
                            else
                            {
                                RegionNameText.Text = "Nieznany region";
                            }

                            ShowRegionPanel();
                        }
                    }
                }
                return HitTestResultBehavior.Stop;
            }
            return HitTestResultBehavior.Continue;
        }

        private void OnViewportRightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.DataContext is MapViewModel viewModel)
            {
                viewModel.DeselectRegion();
            }

            _currentPanelRegionColor = null;
            HideRegionPanel();
        }
    }
}