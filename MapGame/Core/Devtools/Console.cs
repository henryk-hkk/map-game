using MapGame.Core.Utils.Geographic;
using MapGame.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.Devtools
{
    public struct ConsoleArgs(string command, Country? sender)
    {
        public required string Command = command;
        public readonly Country? Sender = sender;

    }

    public class Console
    {
        public void Invoke(ConsoleArgs args)
        {

        }

        public void LogError(string message) 
        {
            
        }
    }

    
}
