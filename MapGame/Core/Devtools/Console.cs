using MapGame.Core.Engine;
using MapGame.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MapGame.Core.Devtools
{
    public struct ConsoleArgs(string command, Country? sender = null)
    {
        public string Command = command;
        public readonly Country? Sender = sender;
    }

    public struct ConsoleOutput(bool status, string? message = null)
    {
        public bool status = status;
        public readonly string Message = message ?? (status ? "Command executed successfully" : "Command execution failed");
    }
    internal class CommandInterpreter
    {
        public delegate ConsoleOutput CommandAction(string[] args);

        private readonly Dictionary<string, CommandAction> _commands = new(StringComparer.OrdinalIgnoreCase);

        public CommandInterpreter()
        {
            _commands.Add("echo", ConsoleCommands.Echo);
            _commands.Add("clear", ConsoleCommands.Clear);
            _commands.Add("setparent", ConsoleCommands.SetAreaParent);
            _commands.Add("setparent_hr", ConsoleCommands.SetHistoricalRegionParent);
            _commands.Add("setowner", ConsoleCommands.SetRegionOwner);
            _commands.Add("annex", ConsoleCommands.AnnexCountry);

        }

        public ConsoleOutput Execute(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new ConsoleOutput(false, "Empty console input.");

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var commandName = parts[0];
            var args = parts.Skip(1).ToArray();

            if (_commands.TryGetValue(commandName, out var commandFunc))
            {
                try
                {
                    return commandFunc(args);
                }
                catch (Exception ex)
                {
                    return new ConsoleOutput(false, $"Error: {ex.Message}");
                }
            }

            return new ConsoleOutput(false, $"Nieznana komenda: '{commandName}'.");
        }
    }

    internal static class ConsoleCommands
    {
        public static ConsoleOutput Echo(string[] args)
        {
            if (args.Length == 0)
                return new(false, "Brak argumentów.");

            return new(true, string.Join(" ", args));
        }

        public static ConsoleOutput Clear(string[] args)
        {
            Console.Clear();
            return new(true);
        }

        public static ConsoleOutput SetAreaParent(string[] args)
        {
            var output = EngineCommands.SetAreaParent(args[0], args[1]);
            if (!output.Status) return new(false, output.Message);
            return new(true);
        }

        public static ConsoleOutput SetRegionOwner(string[] args)
        {
            var output = EngineCommands.SetRegionOwner(args[0], args[1]);
            if (!output.Status) return new(false, output.Message);
            return new(true);
        }

        public static ConsoleOutput AnnexCountry(string[] args)
        {
            var output = EngineCommands.AnnexCountry(args[0], args[1]);
            if (!output.Status) return new(false, output.Message);
            return new(true);
        }

        public static ConsoleOutput SetHistoricalRegionParent(string[] args)
        {
            var output = EngineCommands.GetHistoricalRegionByIdentifier(args[0]);
            if(!output.Status) return new(false, output.Message);
            var output2 = EngineCommands.SetMultipleAreasParent(output.Output, args[1]);
            if(!output2.Status) return new(false, output2.Message);
            return new(true);
        }
    }
    public class Console
    {
        CommandInterpreter _interpreter = new();
        public ConsoleOutput PushCommand(ConsoleArgs args)
        {
            return _interpreter.Execute(args.Command);
        }

        public ConsoleOutput Print(string message) 
        {
            return new(true, message);
        }

        public static void Clear() 
        {

        }
    }
}
