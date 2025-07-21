using EnhancedGameHub.Helpers;
using EnhancedGameHub.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace EnhancedGameHub.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Fields
        private readonly string _gamesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "games.json");
        private ObservableCollection<Game> _allGames;
        private ObservableCollection<Game> _filteredGames;
        private string _searchText;
        private string _selectedCategory;
        private bool _isLoading;
        private bool _isAddGameFlyoutOpen;
        private Game _newGame;
        #endregion

        #region Properties
        public ObservableCollection<Game> FilteredGames
        {
            get => _filteredGames;
            set { _filteredGames = value; OnPropertyChanged(nameof(FilteredGames)); }
        }

        public List<string> Categories { get; } = new List<string> { "All", "Action", "Adventure", "RPG", "Strategy", "Sports", "Simulation", "Puzzle" };

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); FilterGames(); }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(nameof(SelectedCategory)); FilterGames(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        public bool IsAddGameFlyoutOpen
        {
            get => _isAddGameFlyoutOpen;
            set { _isAddGameFlyoutOpen = value; OnPropertyChanged(nameof(IsAddGameFlyoutOpen)); }
        }

        public Game NewGame
        {
            get => _newGame;
            set { _newGame = value; OnPropertyChanged(nameof(NewGame)); }
        }
        #endregion

        #region Commands
        public ICommand OpenAddGameFlyoutCommand { get; }
        public ICommand SelectNewGameExeCommand { get; }
        public ICommand SaveNewGameCommand { get; }
        public ICommand LaunchGameCommand { get; }
        public ICommand SelectExeCommand { get; }
        public ICommand RemoveGameCommand { get; }
        #endregion

        #region Constructor
        public MainViewModel()
        {
            _allGames = new ObservableCollection<Game>();
            FilteredGames = new ObservableCollection<Game>();
            SelectedCategory = "All";
            NewGame = new Game { Category = "Action" };
            OpenAddGameFlyoutCommand = new RelayCommand(OpenAddGameFlyout);
            SelectNewGameExeCommand = new RelayCommand(async () => await SelectNewGameExeAsync());
            SaveNewGameCommand = new RelayCommand(async () => await SaveNewGameAsync(), () => !string.IsNullOrEmpty(NewGame?.ExecutablePath));
            LaunchGameCommand = new RelayCommand<Game>(LaunchGame);
            SelectExeCommand = new RelayCommand<Game>(async (game) => await SelectGameExecutableAsync(game));
            RemoveGameCommand = new RelayCommand<Game>(async (game) => await RemoveGameAsync(game));
            Task.Run(LoadGamesAsync);
        }
        #endregion

        #region Data Persistence
        private async Task LoadGamesAsync()
        {
            Application.Current.Dispatcher.Invoke(() => IsLoading = true);

            if (!File.Exists(_gamesFilePath))
            {
                Application.Current.Dispatcher.Invoke(() => IsLoading = false);
                return;
            }

            try
            {
                using (var stream = File.OpenRead(_gamesFilePath))
                {
                    var games = await JsonSerializer.DeserializeAsync<List<Game>>(stream);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _allGames.Clear();
                        if (games != null)
                        {
                            foreach (var game in games)
                            {
                                _allGames.Add(game);
                            }
                        }
                        FilterGames();
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load games: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => IsLoading = false);
            }
        }

        private async Task SaveGamesAsync()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                using (var stream = File.Create(_gamesFilePath))
                {
                    await JsonSerializer.SerializeAsync(stream, _allGames, options);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save games: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Game Filtering
        private void FilterGames()
        {
            var filtered = _allGames.AsEnumerable();

            if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "All")
            {
                filtered = filtered.Where(g => g.Category == SelectedCategory);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(g => g.Title.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            FilteredGames = new ObservableCollection<Game>(filtered.OrderBy(g => g.Title));
        }
        #endregion

        #region Add Game Logic (Flyout)
        private void OpenAddGameFlyout()
        {
            NewGame = new Game { Category = "Action" };
            IsAddGameFlyoutOpen = true;
        }

        private async Task SelectNewGameExeAsync()
        {
            var exeDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Select Game Executable"
            };

            if (exeDialog.ShowDialog() == true)
            {
                NewGame.ExecutablePath = exeDialog.FileName;
                NewGame.Title = Path.GetFileNameWithoutExtension(exeDialog.FileName);
                NewGame.CoverArt = await ExtractExeIconAsync(exeDialog.FileName) ?? "pack://application:,,,/Resources/default_cover.png";
            }
        }

        private async Task SaveNewGameAsync()
        {
            if (string.IsNullOrEmpty(NewGame.Title) || string.IsNullOrEmpty(NewGame.ExecutablePath))
            {
                MessageBox.Show("Please select an executable file and ensure it has a title.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _allGames.Add(NewGame);
            FilterGames();
            await SaveGamesAsync();

            IsAddGameFlyoutOpen = false; // Close the flyout
        }
        #endregion

        #region Game Actions
        private void LaunchGame(Game game)
        {
            if (game == null || string.IsNullOrEmpty(game.ExecutablePath) || !File.Exists(game.ExecutablePath))
            {
                MessageBox.Show("The path to the game's executable is not valid.", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(game.ExecutablePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not start the game: {ex.Message}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SelectGameExecutableAsync(Game game)
        {
            if (game == null) return;

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Select New Executable"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                game.ExecutablePath = openFileDialog.FileName;
                game.Title = Path.GetFileNameWithoutExtension(game.ExecutablePath);
                game.CoverArt = await ExtractExeIconAsync(game.ExecutablePath) ?? game.CoverArt;
                await SaveGamesAsync();
                FilterGames();
            }
        }

        private async Task<string> ExtractExeIconAsync(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return null;

            // Run the extraction on a background thread.
            return await Task.Run(() =>
            {
                try
                {
                    // Use our new helper to get the highest quality icon as a BitmapSource.
                    BitmapSource bestIcon = IconHelper.GetHighestResolutionIcon(exePath);

                    if (bestIcon == null)
                    {
                        // Fallback to the old method if the new one fails.
                        using (Icon icon = Icon.ExtractAssociatedIcon(exePath))
                        {
                            if (icon == null) return null;
                            using (var bmp = icon.ToBitmap())
                            {
                                bestIcon = Imaging.CreateBitmapSourceFromHBitmap(
                                    bmp.GetHbitmap(),
                                    IntPtr.Zero,
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());
                            }
                        }
                    }

                    if (bestIcon != null)
                    {
                        // Ensure the Icons directory exists.
                        string iconDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons");
                        Directory.CreateDirectory(iconDirectory);

                        // Use a GUID for the filename to prevent conflicts.
                        string iconPath = Path.Combine(iconDirectory, $"{Guid.NewGuid()}.png");

                        // Encode the high-quality BitmapSource to a PNG file.
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bestIcon));

                        using (var stream = new FileStream(iconPath, FileMode.Create))
                        {
                            encoder.Save(stream);
                        }

                        return iconPath; // Return the path to our high-quality saved icon.
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"High-quality icon extraction failed: {ex.Message}");
                }

                return null; // Return null if everything fails.
            });
        }

        private async Task RemoveGameAsync(Game game)
        {
            if (game == null) return;

            var result = MessageBox.Show($"Are you sure you want to remove '{game.Title}' from your library?",
                                         "Confirm Deletion",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _allGames.Remove(game);
                FilterGames();
                await SaveGamesAsync();
            }
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}