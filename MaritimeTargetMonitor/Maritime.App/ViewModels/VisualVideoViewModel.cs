using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Maritime.Core.Config;
using Maritime.Core.Logging;

namespace Maritime.App.ViewModels
{
    public class VisualVideoViewModel : INotifyPropertyChanged
    {
        private readonly AppConfig _config;
        private readonly List<VideoRecord> _allRecords = new List<VideoRecord>();

        private string _selectedDate;

        public ObservableCollection<VideoRecord> VideoRecords { get; }
        public ObservableCollection<string> DateOptions { get; }

        public ICommand RefreshCommand { get; }
        public ICommand PlayCommand { get; }

        public event Action<VideoRecord> PlayRequested;

        public VisualVideoViewModel(AppConfig config)
        {
            _config = config ?? AppConfig.Load();
            VideoRecords = new ObservableCollection<VideoRecord>();
            DateOptions = new ObservableCollection<string>();

            RefreshCommand = new RelayCommand(Refresh);
            PlayCommand = new RelayCommand<VideoRecord>(record => PlayRequested?.Invoke(record));

            Refresh();
        }

        public string RecordDirDisplay => _config?.RecordDir ?? string.Empty;

        public string SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate == value) return;
                _selectedDate = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public void Refresh()
        {
            LoadLocalRecords();
        }

        private void LoadLocalRecords()
        {
            _allRecords.Clear();
            VideoRecords.Clear();
            DateOptions.Clear();

            var dir = _config?.RecordDir ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dir))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(dir);
                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".mp4", ".mkv", ".avi", ".mov", ".flv", ".ts"
                };

                var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                    .Where(f => allowed.Contains(Path.GetExtension(f)))
                    .OrderByDescending(f => f)
                    .ToList();

                var index = 1;
                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    var dateKey = info.Directory?.Name ?? "未知";
                    if (!DateTime.TryParseExact(dateKey, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
                    {
                        dateKey = "未知";
                    }

                    _allRecords.Add(new VideoRecord
                    {
                        Id = index++,
                        VideoName = info.Name,
                        Duration = 0,
                        AddTime = info.LastWriteTime,
                        FilePath = info.FullName,
                        DateKey = dateKey,
                        SizeBytes = info.Length
                    });
                }

                var dates = _allRecords.Select(r => r.DateKey)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToList();

                DateOptions.Add("全部");
                foreach (var d in dates)
                {
                    DateOptions.Add(d);
                }

                if (string.IsNullOrWhiteSpace(SelectedDate) || !DateOptions.Contains(SelectedDate))
                {
                    SelectedDate = "全部";
                }
                else
                {
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("加载本地录像失败", ex);
            }
        }

        private void ApplyFilter()
        {
            VideoRecords.Clear();
            IEnumerable<VideoRecord> filtered = _allRecords;
            if (!string.IsNullOrWhiteSpace(SelectedDate) && SelectedDate != "全部")
            {
                filtered = filtered.Where(r => r.DateKey == SelectedDate);
            }

            foreach (var record in filtered.OrderByDescending(r => r.AddTime))
            {
                VideoRecords.Add(record);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class VideoRecord
    {
        public long Id { get; set; }
        public string VideoName { get; set; }
        public int Duration { get; set; }
        public DateTime AddTime { get; set; }
        public string FilePath { get; set; }
        public string DateKey { get; set; }
        public long SizeBytes { get; set; }

        public string SizeText => FormatSize(SizeBytes);

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] units = { "B", "KB", "MB", "GB" };
            double size = bytes;
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return $"{size:0.##} {units[unit]}";
        }
    }

}
