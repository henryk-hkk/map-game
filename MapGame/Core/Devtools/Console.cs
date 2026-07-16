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

    public struct ConsoleOutput(bool status, string? message)
    {
        public required bool status = status;
        public readonly string Message = message ?? (status ? "Command executed successfully" : "Command execution failed.");
    }

    public class Console
    {
        public void PushCommand(ConsoleArgs args)
        {

        }

        public void LogError(string message) 
        {
            
        }
    }

    
}
