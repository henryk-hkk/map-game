using System.Configuration;
using System.Data;
using System.Windows;
using MapGame.Core.Engine;

namespace MapGame
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            GameManager gm = new GameManager();
            gm.Init();
        }
        
        
    }

}
