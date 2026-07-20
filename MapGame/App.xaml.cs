using System.Configuration;
using System.Data;
using System.Windows;
using MapGame.Core;
using MapGame.Core.Engine;
using MapGame.Core.Utils.JSON;

namespace MapGame
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            IJsonReader jsonReader = new JsonReader();
            GameManager gm = new(jsonReader);
            gm.Init(Scenario.His_PreWar1933, Language.Polish);
        }
        
        
    }

}
