using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core;
using MapGame.Core.Geographic;
using MapGame.Core.Utils.Map;
using MapGame.MVVM.ViewModels;
using MapGame.MVVM.Views.Components;
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
        private DateTime _lastMouseMoveTime;
        private bool _isConsoleOpen = false;


        private readonly DoubleAnimation _slideInFromLeft = new()
        {
            From = -340,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(100)
        };

        public MainWindow()
        {
            InitializeComponent();

            var consoleViewModel = new DevConsoleViewModel();

            consoleViewModel.OnLogRequested += DevConsoleControl.AppendLog;
            consoleViewModel.OnClearRequested += DevConsoleControl.ClearConsole;

            DevConsoleControl.DataContext = consoleViewModel;

            //consoleViewModel.RequestLog("MapGame DevConsole", LogType.Normal);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Oem3) // tylda ~
            {
                e.Handled = true;
                ToggleConsole();
            }
        }

        private void ToggleConsole()
        {

            double consoleWidth = DevConsoleControl.ActualWidth > 0 ? DevConsoleControl.ActualWidth : 400;

            TranslateTransform transform = (TranslateTransform)DevConsoleControl.RenderTransform;

            if (!_isConsoleOpen)
            {
                _isConsoleOpen = true;
                DevConsoleControl.Visibility = Visibility.Visible;

                DoubleAnimation slideInFromRight = new()
                {
                    From = consoleWidth,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(100),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                transform.BeginAnimation(TranslateTransform.XProperty, slideInFromRight);

                // DevConsoleControl.FocusInput(); 
            }
            else
            {
                _isConsoleOpen = false;

                DoubleAnimation slideOut = new()
                {
                    From = 0,
                    To = consoleWidth,
                    Duration = TimeSpan.FromMilliseconds(100),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                slideOut.Completed += (s, e) => DevConsoleControl.Visibility = Visibility.Collapsed;

                transform.BeginAnimation(TranslateTransform.XProperty, slideOut);
            }
        }

        private void ShowRegionPanel()
        {
            RegionPanelControl.RegionInfoPanel.Visibility = Visibility.Visible;

            RegionPanelControl.RegionInfoPanelTransform.BeginAnimation(TranslateTransform.XProperty, _slideInFromLeft);
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
                RegionPanelControl.RegionInfoPanel.Visibility = Visibility.Collapsed;
            };

            RegionPanelControl.RegionInfoPanelTransform.BeginAnimation(TranslateTransform.XProperty, slideOut);
        }


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

            Region? region = MapViewModel.GetRegionByMousePosition(mousePosition);

            int mapX = (int)mousePosition.X;
            int mapY = (int)mousePosition.Y;

            if (region != null)
            {
                CursorCoordsText.Text = $"X: {mapX} | Y: {mapY} | Region: {region.DisplayName} ({region.Id})";
            }
            else
            {
                CursorCoordsText.Text = $"X: {mapX} | Y: {mapY} | Region: brak";
            }
        }

        private void OnTerrainMouseDown(object sender, RoutedEventArgs e)
        {
            if (e is not Mouse3DEventArgs mouseArgs) return;
            if (DataContext is not MapViewModel viewModel) return;


            if (mouseArgs.OriginalInputEventArgs is MouseButtonEventArgs btnArgs && btnArgs.LeftButton == MouseButtonState.Pressed)
            {
                var mousePosition = GetBaricentricMousePosition(mouseArgs);
                if (mousePosition == null) return;

                Region? clickedRegion = MapViewModel.GetRegionByMousePosition(mousePosition);

                viewModel.SelectRegion(clickedRegion);
                ShowRegionPanel();
            }
            else if (mouseArgs.OriginalInputEventArgs is MouseButtonEventArgs rightBtnArgs && rightBtnArgs.RightButton == MouseButtonState.Pressed)
            {
                viewModel.DeselectRegion();
                HideRegionPanel();
            }
        }

        private void OnViewportMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DataContext is not MapViewModel viewModel) return;
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