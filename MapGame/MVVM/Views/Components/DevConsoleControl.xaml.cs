using MapGame.MVVM.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MapGame.MVVM.Views.Components
{
    public partial class DevConsoleControl : UserControl
    {
        private readonly Brush _commandColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#89b4fa"));
        private readonly Brush _textColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cdd6f4"));
        private readonly Brush _errorColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f38ba8"));

        public DevConsoleControl()
        {
            InitializeComponent();
        }

        public void AppendLog(string text, LogType logType)
        {
            TextBlock logEntry = new()
            {
                Text = text,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 4),
                TextWrapping = TextWrapping.Wrap
            };

            switch (logType)
            {
                case LogType.Error:
                    logEntry.Foreground = _errorColor;
                    break;
                case LogType.Command:
                    logEntry.Foreground = _commandColor;
                    break;
                default:
                    logEntry.Foreground = _textColor;
                    break;
            }

            LogPanel.Children.Add(logEntry);
            LogScrollViewer.ScrollToBottom();
        }

        public void FocusInput()
        {
            InputTextBox.Focus();
        }

        public void ClearConsole()
        {
            LogPanel.Children.Clear();
        }
    }
}