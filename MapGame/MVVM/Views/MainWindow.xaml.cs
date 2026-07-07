using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Constants;
using MapGame.Core.Utils.Geographic;
using MapGame.MVVM.ViewModels;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Linq;

namespace MapGame.MVVM.Views
{
    public partial class MainWindow : Window
    {
        private Color? _currentPanelRegionColor = null;
        private DateTime _lastMouseMoveTime;
        private readonly TimeSpan _mouseMoveThrottle = TimeSpan.FromMilliseconds(33);

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

            Point mousePos = e.GetPosition(MainViewport);

            var hits = MainViewport.FindHits(mousePos);

            if (hits != null && hits.Count > 0)
            {
                var firstHit = hits[0];
                if (firstHit.ModelHit is not MeshGeometryModel3D)
                {
                    return;
                }

                CursorTransform.X = mousePos.X + 15;
                CursorTransform.Y = mousePos.Y + 15;

                var hitPoint = firstHit.PointHit;
                int mapX = (int)hitPoint.X;
                int mapY = (int)hitPoint.Z;

                Position mousePosition = new(hitPoint.X, hitPoint.Z);

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
                    CursorCoordsText.Text = $"X: {mapX} | Y: {mapY} | Region: {Map.RegionNames[hoveredRegion.Id]} ({hoveredRegion.Id})";
                }
                else
                {
                    CursorCoordsText.Text = $"X: {mapX} | Y: {mapY} | Region: brak";
                }

                //int index1D = (mapY * Map.Width) + mapX;


                //if (index1D >= 0 && Map.GlobalRegionMap != null && index1D < Map.GlobalRegionMap.Length)
                //{
                //    int hoveredRegionId = Map.GlobalRegionMap[index1D];
                //    if (hoveredRegionId == -1)
                //    {
                //        CursorCoordsText.Text = string.Empty;
                //        return;
                //    }

                //    if (hoveredRegionId >= 0 && hoveredRegionId < Map.RegionNames.Count)
                //    {
                //        string? regionName = Map.RegionNames[hoveredRegionId];
                //        if (regionName != null)
                //        {
                //            CursorCoordsText.Text = $"X: {mapX} | Y: {mapY} | Region: {regionName} ({hoveredRegionId})";
                //        }
                //    }
                //}
            }
            else
            {
                CursorCoordsText.Text = string.Empty;
            }
        }

        private void OnTerrainMouseDown(object sender, RoutedEventArgs e)
        {
            //if ((DateTime.Now - _lastMouseMoveTime).TotalMilliseconds < 33) return;
            //_lastMouseMoveTime = DateTime.Now;

            if (e is Mouse3DEventArgs mouseArgs && mouseArgs.HitTestResult != null)
            {
                if (mouseArgs.OriginalInputEventArgs is MouseButtonEventArgs btnArgs && btnArgs.LeftButton == MouseButtonState.Pressed)
                {
                    var hit = mouseArgs.HitTestResult;

                    var mesh = hit.Geometry as HelixToolkit.SharpDX.MeshGeometry3D;

                    if (mesh != null && mesh.TextureCoordinates != null && hit.TriangleIndices != null)
                    {
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

                        if (Math.Abs(denom) > 0.000001f)
                        {
                            float v = (d11 * d20 - d01 * d21) / denom;
                            float w = (d00 * d21 - d01 * d20) / denom;
                            float u = 1.0f - v - w;

                            Vector2 uv1 = mesh.TextureCoordinates[i1];
                            Vector2 uv2 = mesh.TextureCoordinates[i2];
                            Vector2 uv3 = mesh.TextureCoordinates[i3];

                            float hitU = (uv1.X * u) + (uv2.X * v) + (uv3.X * w);
                            float hitV = (uv1.Y * u) + (uv2.Y * v) + (uv3.Y * w);

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
                                    System.Windows.Media.Color clickedColor = System.Windows.Media.Color.FromRgb(r, g, b);

                                    if (this.DataContext is MapViewModel viewModel)
                                    {
                                        

                                        _currentPanelRegionColor = clickedColor;

                                        if (Map.AreaColors.TryGetValue(clickedColor, out PixelArea clickedArea) &&
                                            clickedArea.ParentRegionId.HasValue)
                                        {
                                            Region? clickedRegion = Map.Regions
                                            .FirstOrDefault(region => region.Id == clickedArea.ParentRegionId.Value);

                                            string regionName = clickedRegion?.Name ?? "Nieznany region";
                                            viewModel.SelectRegion(clickedColor, regionName);

                                            RegionNameText.Text = regionName;
                                        }   
                                        else
                                        {
                                            RegionNameText.Text = "Nieznany region";
                                        }

                                        ShowRegionPanel();
                                    }
                                }
                            }
                        }
                    }
                }
                else if (mouseArgs.OriginalInputEventArgs is MouseButtonEventArgs rightBtnArgs && rightBtnArgs.RightButton == MouseButtonState.Pressed)
                {
                    if (this.DataContext is MapViewModel viewModel)
                    {
                        viewModel.DeselectRegion();
                        _currentPanelRegionColor = null;
                        HideRegionPanel();
                    }
                }
            }
        }

        private void OnViewportMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (this.DataContext is MapGame.MVVM.ViewModels.MapViewModel viewModel)
            {
                viewModel.CameraController.Zoom(e.Delta);

                e.Handled = true;
            }
        }
    }
}