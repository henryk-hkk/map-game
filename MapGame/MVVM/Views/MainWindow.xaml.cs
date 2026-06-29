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

namespace MapGame.MVVM.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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

                CursorCoordsText.Text = $"X: {mapX} | Y: {mapY}";
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

                // Logika wag barycentrycznych (zostaje bez zmian)
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

                        if (this.DataContext is MapViewModel viewModel)
                        {
                            viewModel.SelectRegion(clickedColor);
                        }
                    }
                }
                return HitTestResultBehavior.Stop;
            }
            return HitTestResultBehavior.Continue;
        }

        private void OnViewportRightMouseDown(object sender, MouseButtonEventArgs e)
        {
            // ARCHITEKTURA MVVM: Przekazujemy zdarzenie odznaczenia do ViewModelu
            if (this.DataContext is MapViewModel viewModel)
            {
                viewModel.DeselectRegion();
            }
        }
    }
}