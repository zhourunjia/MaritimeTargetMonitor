using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Maritime.Core.Config;

namespace Maritime.App.Pages
{
    /// <summary>
    /// AlgorithmConfigPanel.xaml 的交互逻辑
    /// </summary>
    public partial class AlgorithmConfigPanel : UserControl
    {
        private AppConfig _config;

        public event EventHandler ConfigSaved;

        public AlgorithmConfigPanel()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            _config = AppConfig.Load();

            SetSceneSelection(_config.AlgorithmScene);
            PythonPathBox.Text = _config.AlgorithmPythonPath ?? string.Empty;
            ScriptPathBox.Text = _config.AlgorithmScriptPath ?? string.Empty;
            WeightsPathBox.Text = _config.AlgorithmWeightsPath ?? string.Empty;
            InputUrlBox.Text = _config.AlgorithmInputUrl ?? string.Empty;
            OutputUrlBox.Text = _config.AlgorithmOutputUrl ?? string.Empty;
            FfmpegPathBox.Text = _config.AlgorithmFfmpegPath ?? string.Empty;
            InputSizeBox.Text = _config.AlgorithmInputSize ?? string.Empty;
            OutputSizeBox.Text = _config.AlgorithmOutputSize ?? string.Empty;
            TargetFpsBox.Text = _config.AlgorithmTargetFps ?? string.Empty;
            UseCpuCheck.IsChecked = _config.AlgorithmUseCpu;
            UseTrtCheck.IsChecked = _config.AlgorithmUseTrt;
            AutoStartCheck.IsChecked = _config.AlgorithmAutoStart;

            UpdateRtspPreview();
            UpdateSceneHint();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _config.AlgorithmScene = GetSelectedScene();
            _config.AlgorithmPythonPath = (PythonPathBox.Text ?? string.Empty).Trim();
            _config.AlgorithmScriptPath = (ScriptPathBox.Text ?? string.Empty).Trim();
            _config.AlgorithmWeightsPath = (WeightsPathBox.Text ?? string.Empty).Trim();
            _config.AlgorithmInputUrl = (InputUrlBox.Text ?? string.Empty).Trim();
            _config.AlgorithmOutputUrl = (OutputUrlBox.Text ?? string.Empty).Trim();
            _config.AlgorithmFfmpegPath = (FfmpegPathBox.Text ?? string.Empty).Trim();
            _config.AlgorithmInputSize = (InputSizeBox.Text ?? string.Empty).Trim();
            _config.AlgorithmOutputSize = (OutputSizeBox.Text ?? string.Empty).Trim();
            _config.AlgorithmTargetFps = (TargetFpsBox.Text ?? string.Empty).Trim();
            _config.AlgorithmUseCpu = UseCpuCheck.IsChecked == true;
            _config.AlgorithmUseTrt = UseTrtCheck.IsChecked == true;
            _config.AlgorithmAutoStart = AutoStartCheck.IsChecked == true;

            _config.Save();
            MessageBox.Show("已保存算法配置", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateRtspPreview();
            UpdateSceneHint();
            ConfigSaved?.Invoke(this, EventArgs.Empty);
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadConfig();
        }

        private void OutputUrlBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRtspPreview();
        }

        private void ApplyPresetButton_Click(object sender, RoutedEventArgs e)
        {
            var scene = GetSelectedScene();
            if (string.IsNullOrWhiteSpace(scene))
            {
                scene = "实时无人机";
            }

            _config.AlgorithmScene = scene;

            var inputUrl = (_config.DroneStreamUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(inputUrl))
            {
                inputUrl = (_config.AlgorithmInputUrl ?? string.Empty).Trim();
            }

            if (string.IsNullOrWhiteSpace(inputUrl))
            {
                inputUrl = "rtmp://127.0.0.1:1935/live/raw";
            }

            var host = GetHostFromUrl(inputUrl);
            var outputUrl = $"rtmp://{host}:1935/live/m3t";

            _config.AlgorithmInputUrl = inputUrl;
            _config.AlgorithmOutputUrl = outputUrl;
            _config.AlgorithmTargetFps = "10";
            _config.AlgorithmOutputSize = "1280x720";

            if (scene == "本地演示")
            {
                _config.RecordEnabled = true;
            }
            else
            {
                _config.RecordEnabled = false;
            }

            if (scene == "离线回放")
            {
                _config.AlgorithmAutoStart = false;
            }

            _config.Save();

            InputUrlBox.Text = _config.AlgorithmInputUrl ?? string.Empty;
            OutputUrlBox.Text = _config.AlgorithmOutputUrl ?? string.Empty;
            OutputSizeBox.Text = _config.AlgorithmOutputSize ?? string.Empty;
            TargetFpsBox.Text = _config.AlgorithmTargetFps ?? string.Empty;
            AutoStartCheck.IsChecked = _config.AlgorithmAutoStart;

            UpdateRtspPreview();
            UpdateSceneHint();
            ConfigSaved?.Invoke(this, EventArgs.Empty);
        }

        private void BrowsePython_Click(object sender, RoutedEventArgs e)
        {
            SelectFile(PythonPathBox, "python.exe|python.exe|All Files|*.*");
        }

        private void BrowseScript_Click(object sender, RoutedEventArgs e)
        {
            SelectFile(ScriptPathBox, "Python Files|*.py|All Files|*.*");
        }

        private void BrowseWeights_Click(object sender, RoutedEventArgs e)
        {
            SelectFile(WeightsPathBox, "Weight Files|*.pth;*.pt|All Files|*.*");
        }

        private void BrowseFfmpeg_Click(object sender, RoutedEventArgs e)
        {
            SelectFile(FfmpegPathBox, "ffmpeg.exe|ffmpeg.exe|All Files|*.*");
        }

        private void SelectFile(TextBox target, string filter)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                CheckFileExists = true
            };
            if (dialog.ShowDialog() == true)
            {
                target.Text = dialog.FileName;
            }
        }

        private void UpdateRtspPreview()
        {
            var output = (OutputUrlBox.Text ?? string.Empty).Trim();
            var preview = BuildRtspPreview(output);
            RtspPreviewText.Text = string.IsNullOrWhiteSpace(preview) ? "未设置" : preview;
        }

        private void UpdateSceneHint()
        {
            var scene = GetSelectedScene();
            if (scene == "实时无人机")
            {
                SceneHintText.Text = "输入=无人机推流，输出=m3t，录制关闭";
            }
            else if (scene == "本地演示")
            {
                SceneHintText.Text = "输入=无人机推流，输出=m3t，录制开启";
            }
            else if (scene == "离线回放")
            {
                SceneHintText.Text = "仅回放本地录像，算法不启动";
            }
            else
            {
                SceneHintText.Text = string.Empty;
            }
        }

        private string GetSelectedScene()
        {
            if (SceneComboBox.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString() ?? string.Empty;
            }
            return SceneComboBox.Text ?? string.Empty;
        }

        private void SetSceneSelection(string scene)
        {
            if (SceneComboBox.Items.Count == 0)
            {
                return;
            }

            var target = string.IsNullOrWhiteSpace(scene) ? "实时无人机" : scene;
            foreach (var obj in SceneComboBox.Items)
            {
                if (obj is ComboBoxItem item)
                {
                    if (string.Equals(item.Content?.ToString(), target, StringComparison.OrdinalIgnoreCase))
                    {
                        SceneComboBox.SelectedItem = item;
                        return;
                    }
                }
            }

            SceneComboBox.SelectedIndex = 0;
        }

        private string GetHostFromUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (!string.IsNullOrWhiteSpace(uri.Host))
                {
                    return uri.Host;
                }
            }
            return "127.0.0.1";
        }

        private string BuildRtspPreview(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return string.Empty;

            if (output.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) ||
                output.StartsWith("rtsps://", StringComparison.OrdinalIgnoreCase))
            {
                return output;
            }

            if (output.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase) ||
                output.StartsWith("rtmps://", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(output, UriKind.Absolute, out var uri))
                {
                    var host = uri.Host;
                    var path = uri.AbsolutePath.TrimStart('/');
                    var port = _config?.RelayRtspPort ?? 8554;
                    return $"rtsp://{host}:{port}/{path}";
                }
            }

            return string.Empty;
        }
    }
}
