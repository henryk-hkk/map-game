using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MapGame.Core.Devtools;

namespace MapGame.MVVM.ViewModels
{
    public enum LogType
    {
        Normal,
        Command,
        Error
    }

    public class DevConsoleViewModel : INotifyPropertyChanged
    {
        private string _currentInput;
        private readonly Core.Devtools.Console _console = new();
        public string CurrentInput
        {
            get { return _currentInput; }
            set
            {
                _currentInput = value;
                OnPropertyChanged();
            }
        }

        public ICommand ExecuteConsoleCommand { get; }

        public event Action<string, LogType> OnLogRequested;
        public event Action OnClearRequested;

        public DevConsoleViewModel()
        {
            ExecuteConsoleCommand = new RelayCommand(ExecuteCommand);
        }

        private void ExecuteCommand()
        {
            string? command = CurrentInput?.Trim();

            if (string.IsNullOrEmpty(command)) return;

            RequestLog($"> {command}", LogType.Command);

            ProcessCommandString(command);

            CurrentInput = string.Empty;
        }

        private void ProcessCommandString(string command)
        {
            var output = _console.PushCommand(new(command));
            var logType = output.status ? LogType.Normal : LogType.Error;
            RequestLog(output.Message, logType);
        }

        public void RequestLog(string message, LogType type = LogType.Normal)
        {
            OnLogRequested?.Invoke(message, type);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}