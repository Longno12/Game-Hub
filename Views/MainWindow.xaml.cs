using ControlzEx.Theming;
using MahApps.Metro;
using MahApps.Metro.Controls;
using System.Windows;

namespace EnhancedGameHub.Views
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                ThemeManager.Current.ChangeTheme(Application.Current, toggleSwitch.IsOn ? "Dark.Blue" : "Light.Blue");
            }
        }
    }
}