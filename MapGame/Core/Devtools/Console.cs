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
            var output = Commands.SetAreaParent(args[0], args[1]);
            if (!output.Status) return new(false, output.Message);
            return new(true);
        }
    }
    public class Console
    {
        CommandInterpreter _interpreter = new();
        public void PushCommand(ConsoleArgs args)
        {
            var output = _interpreter.Execute(args.Command);
        }

        public void Print(string message) 
        {
            
        }

        public static void Clear() 
        {

        }
    }
}
