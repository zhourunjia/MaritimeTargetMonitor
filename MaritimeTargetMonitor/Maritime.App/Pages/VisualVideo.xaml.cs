using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LibVLCSharp.Shared;
using Microsoft.Win32;
using Maritime.App.ViewModels;
using Maritime.Core.Config;

namespace Maritime.App.Pages
{
    /// <summary>
    /// 可视化视频页：本地录像库与播放
    /// </summary>
    public partial class VisualVideo : Page
    {
        public VisualVideoViewModel ViewModel { get; set; }
        private readonly AppConfig _config;

        private LibVLC _localLibVlc;
        private MediaPlayer _localMediaPlayer;

        public VisualVideo()
        {
            InitializeComponent();
            _config = AppConfig.Load();
            ViewModel = new VisualVideoViewModel(_config);
            ViewModel.PlayRequested += OnPlayRequested;
            DataContext = ViewModel;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LocalVideoStatus.Text = "请选择本地录像播放";
            LocalVideoStatus.Foreground = System.Windows.Media.Brushes.LightGray;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopLocalVideo();
        }

        private void OnPlayRequested(VideoRecord record)
        {
            if (record == null) return;
            StartLocalVideo(record.FilePath);
        }

        #region 本地视频播放
        private void SelectLocalVideoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "视频文件|*.mp4;*.mkv;*.avi;*.mov;*.flv;*.ts|所有文件|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                StartLocalVideo(dialog.FileName);
            }
        }

        private void StopLocalVideoButton_Click(object sender, RoutedEventArgs e)
        {
            StopLocalVideo();
        }

        private void StartLocalVideo(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    LocalVideoStatus.Text = "文件不存在";
                    LocalVideoStatus.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                StopLocalVideo();

                LibVLCSharp.Shared.Core.Initialize();
                _localLibVlc = new LibVLC();
                _localMediaPlayer = new MediaPlayer(_localLibVlc);
                LocalVideoView.MediaPlayer = _localMediaPlayer;

                using (var media = new Media(_localLibVlc, new Uri(filePath)))
                {
                    _localMediaPlayer.Play(media);
                }

                LocalVideoStatus.Text = $"播放：{Path.GetFileName(filePath)}";
                LocalVideoStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                LocalVideoStatus.Text = $"播放失败: {ex.Message}";
                LocalVideoStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void StopLocalVideo()
        {
            try
            {
                _localMediaPlayer?.Stop();
                _localMediaPlayer?.Dispose();
                _localMediaPlayer = null;
                _localLibVlc?.Dispose();
                _localLibVlc = null;
                LocalVideoView.MediaPlayer = null;
                LocalVideoStatus.Text = "已停止";
                LocalVideoStatus.Foreground = System.Windows.Media.Brushes.Gray;
            }
            catch
            {
                // ignore
            }
        }
        #endregion
    }

    /// <summary>
    /// bool -> Visibility
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// bool 取反转换
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
