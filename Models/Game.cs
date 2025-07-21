using System.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;

namespace EnhancedGameHub.Models
{
    public class Game : INotifyPropertyChanged
    {
        private string _title;
        private string _category;
        private string _coverArt;
        private string _executablePath;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        public string CoverArt
        {
            get => _coverArt;
            set { _coverArt = value; OnPropertyChanged(nameof(CoverArt)); }
        }

        public string ExecutablePath
        {
            get => _executablePath;
            set { _executablePath = value; OnPropertyChanged(nameof(ExecutablePath)); }
        }

        [JsonIgnore]
        public string ExecutableFileName => Path.GetFileName(ExecutablePath);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}