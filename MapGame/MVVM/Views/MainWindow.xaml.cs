using MapGame.MVVM.ViewModels;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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
    }
}