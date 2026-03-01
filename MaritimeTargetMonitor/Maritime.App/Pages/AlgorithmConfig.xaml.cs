using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Maritime.Core.Config;
using Maritime.Infrastructure.Services;
using Newtonsoft.Json.Linq;

namespace Maritime.App.Pages
{
    /// <summary>
    /// AlgorithmConfig.xaml 的交互逻辑
    /// </summary>
    public partial class AlgorithmConfig : Page
    {
        private readonly AppConfig _config;
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        private bool _envChecked;

        public AlgorithmConfig()
        {
            InitializeComponent();
            _config = AppConfig.Load();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            HomeRelayPathBox.Text = ResolveHomeRelayPath();
            UpdateHomeUrls();
            InitializeRcProModule();
            EnsureInputFromPushAddress(false);
            _ = RefreshMtxPathAsync(false);
            UpdateAlgorithmInfo();
            EnsureAlgorithmEnvironment();
            AlgorithmProcessService.Instance.StatusChanged += OnAlgorithmStatusChanged;
            AlgorithmConfigPanel.ConfigSaved += OnAlgorithmConfigSaved;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            AlgorithmProcessService.Instance.StatusChanged -= OnAlgorithmStatusChanged;
            AlgorithmConfigPanel.ConfigSaved -= OnAlgorithmConfigSaved;
        }

        #region Algorithm
        private void AlgorithmStartButton_Click(object sender, RoutedEventArgs e)
        {
            var cfg = AppConfig.Load();
            if (string.Equals(cfg.AlgorithmScene, "离线回放", StringComparison.Ordinal))
            {
                MessageBox.Show("当前为离线回放模式，算法不启动。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            StartAlgorithmWithConfig(cfg, keepAlive: false, showError: true);
        }

        private void AlgorithmStopButton_Click(object sender, RoutedEventArgs e)
        {
            AlgorithmProcessService.Instance.Stop();
            UpdateAlgorithmInfo();
        }

        private void AlgorithmPrewarmButton_Click(object sender, RoutedEventArgs e)
        {
            var cfg = AppConfig.Load();
            if (string.Equals(cfg.AlgorithmScene, "离线回放", StringComparison.Ordinal))
            {
                MessageBox.Show("当前为离线回放模式，算法不预热。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            StartAlgorithmWithConfig(cfg, keepAlive: true, showError: true);
        }

        private void StartAlgorithmWithConfig(AppConfig cfg, bool keepAlive = false, bool showError = true)
        {
            TryStartAlgorithm(cfg, showError, keepAlive);
        }

        private bool TryStartAlgorithm(AppConfig cfg, bool showError, bool keepAlive)
        {
            if (cfg != null && !string.Equals(cfg.AlgorithmScene, "离线回放", StringComparison.Ordinal))
            {
                var pushUrl = (RcProAddressBox?.Text ?? string.Empty).Trim();
                var uiDroneUrl = (RcProInputBox?.Text ?? string.Empty).Trim();
                var droneUrl = ResolveBestInputUrl(pushUrl, uiDroneUrl, cfg.DroneStreamUrl);

                if (!string.IsNullOrWhiteSpace(droneUrl))
                {
                    var algoInput = PreferLoopbackForLocalHost(droneUrl);
                    if (!string.Equals(cfg.DroneStreamUrl, droneUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        cfg.DroneStreamUrl = droneUrl;
                    }
                    if (!string.Equals(cfg.AlgorithmInputUrl, algoInput, StringComparison.OrdinalIgnoreCase))
                    {
                        cfg.AlgorithmInputUrl = algoInput;
                    }
                    cfg.Save();
                }
                else if (showError)
                {
                    MessageBox.Show("输入源为空，请先确认推流地址。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            if (AlgorithmProcessService.Instance.Start(cfg, out string error, keepAlive))
            {
                UpdateAlgorithmInfo();
                return true;
            }

            if (error == "算法已在运行")
            {
                UpdateAlgorithmInfo();
                return true;
            }

            AlgorithmStatusText.Text = error;
            AlgorithmStatusText.Foreground = System.Windows.Media.Brushes.Red;
            if (showError)
            {
                MessageBox.Show(error, "算法启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }
        private void UpdateAlgorithmInfo()
        {
            var cfg = AppConfig.Load();
            AlgorithmInputText.Text = cfg.AlgorithmInputUrl ?? string.Empty;
            AlgorithmOutputText.Text = cfg.AlgorithmOutputUrl ?? string.Empty;

            var isOffline = string.Equals(cfg.AlgorithmScene, "离线回放", StringComparison.Ordinal);
            if (isOffline)
            {
                if (AlgorithmProcessService.Instance.IsRunning)
                {
                    AlgorithmProcessService.Instance.Stop();
                }
                AlgorithmStatusText.Text = "离线回放模式";
                AlgorithmStatusText.Foreground = System.Windows.Media.Brushes.Goldenrod;
                AlgorithmStartButton.IsEnabled = false;
                AlgorithmStopButton.IsEnabled = false;
                if (AlgorithmPrewarmButton != null)
                {
                    AlgorithmPrewarmButton.IsEnabled = false;
                }
                EnterPlaybackButton.Visibility = Visibility.Visible;
                return;
            }

            var running = AlgorithmProcessService.Instance.IsRunning;
            AlgorithmStartButton.IsEnabled = !running;
            AlgorithmStopButton.IsEnabled = running;
            if (AlgorithmPrewarmButton != null)
            {
                AlgorithmPrewarmButton.IsEnabled = !running;
            }
            EnterPlaybackButton.Visibility = Visibility.Collapsed;

            if (running)
            {
                AlgorithmStatusText.Text = "算法运行中";
                AlgorithmStatusText.Foreground = System.Windows.Media.Brushes.DarkGreen;
            }
            else
            {
                AlgorithmStatusText.Text = "算法未启动";
                AlgorithmStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void OnAlgorithmStatusChanged(object sender, AlgorithmStatusEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var cfg = AppConfig.Load();
                if (string.Equals(cfg.AlgorithmScene, "离线回放", StringComparison.Ordinal))
                {
                    AlgorithmStatusText.Text = "离线回放模式";
                    AlgorithmStatusText.Foreground = System.Windows.Media.Brushes.Goldenrod;
                    return;
                }
                AlgorithmStatusText.Text = e.Message;
                AlgorithmStatusText.Foreground = e.IsRunning ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.Gray;
            });
        }

        private void EnterPlaybackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new VisualVideo());
        }

        private void OnAlgorithmConfigSaved(object sender, EventArgs e)
        {
            UpdateAlgorithmInfo();
        }
        #endregion

        #region Relay
        private void HomeStartRelayButton_Click(object sender, RoutedEventArgs e)
        {
            StartHomeRelay();
        }

        private void HomeStopRelayButton_Click(object sender, RoutedEventArgs e)
        {
            StopHomeRelay();
        }

        private bool StartHomeRelay()
        {
            try
            {
                if (RelayProcessService.Instance.IsRunning)
                {
                    HomeRelayStatus.Text = "转发器已在运行";
                    HomeRelayStatus.Foreground = System.Windows.Media.Brushes.DarkGreen;
                    UpdateHomeUrls();
                    return true;
                }

                var exePath = ResolveHomeRelayPath();
                HomeRelayPathBox.Text = exePath;
                if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                {
                    HomeRelayStatus.Text = "未找到转发器（rtsp-simple-server.exe / mediamtx.exe）";
                    HomeRelayStatus.Foreground = System.Windows.Media.Brushes.Red;
                    return false;
                }

                if (!RelayProcessService.Instance.Start(exePath, out var error))
                {
                    HomeRelayStatus.Text = string.IsNullOrWhiteSpace(error) ? "转发器启动失败" : error;
                    HomeRelayStatus.Foreground = System.Windows.Media.Brushes.Red;
                    return false;
                }

                _config.RelayExePath = exePath;
                _config.Save();

                HomeRelayStatus.Text = "转发器已启动";
                HomeRelayStatus.Foreground = System.Windows.Media.Brushes.DarkGreen;
                UpdateHomeUrls();
                _ = RefreshMtxPathAsync(false);

                return true;
            }
            catch (Exception ex)
            {
                HomeRelayStatus.Text = $"启动失败: {ex.Message}";
                HomeRelayStatus.Foreground = System.Windows.Media.Brushes.Red;
                return false;
            }
        }

        private void StopHomeRelay()
        {
            try
            {
                RelayProcessService.Instance.Stop();
                HomeRelayStatus.Text = "转发器已停止";
                HomeRelayStatus.Foreground = System.Windows.Media.Brushes.Gray;
            }
            catch (Exception ex)
            {
                HomeRelayStatus.Text = $"停止失败: {ex.Message}";
                HomeRelayStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void UpdateHomeUrls()
        {
            var ip = GetPreferredIPv4();
            var rtmp = $"rtmp://{ip}:{_config.RelayRtmpPort}/{_config.RelayAppName}";
            var rtsp = $"rtsp://{ip}:{_config.RelayRtspPort}/{_config.RelayAppName}";
            HomeRtmpUrlText.Text = rtmp;
            HomeRtspUrlText.Text = rtsp;

            if (!string.IsNullOrWhiteSpace(ip))
            {
                if (string.IsNullOrWhiteSpace(_config.RtspUrl))
                {
                    _config.RtspUrl = rtsp;
                    _config.Save();
                }
            }
        }

        private string ResolveHomeRelayPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // 1) 用户输入或配置的路径优先
            var fromInput = ResolveCandidateToExecutable(HomeRelayPathBox?.Text, baseDir);
            if (!string.IsNullOrWhiteSpace(fromInput))
            {
                return PersistRelayPathIfNeeded(fromInput);
            }

            var fromConfig = ResolveCandidateToExecutable(_config?.RelayExePath, baseDir);
            if (!string.IsNullOrWhiteSpace(fromConfig))
            {
                return PersistRelayPathIfNeeded(fromConfig);
            }

            // 2) 常用位置自动探测（首次运行自动写入配置）
            var candidates = new[]
            {
                Path.Combine(baseDir, "mediamtx.exe"),
                Path.Combine(baseDir, "rtsp-simple-server.exe"),
                Path.Combine(baseDir, "tools"),
                Path.Combine(baseDir, "tools", "mediamtx"),
                Path.Combine(baseDir, "tools", "rtsp-simple-server")
            };

            foreach (var c in candidates)
            {
                var found = ResolveCandidateToExecutable(c, baseDir);
                if (!string.IsNullOrWhiteSpace(found))
                {
                    return PersistRelayPathIfNeeded(found);
                }
            }

            var upward = SearchUpwardsForRelayExecutable(baseDir, 8);
            if (!string.IsNullOrWhiteSpace(upward))
            {
                return PersistRelayPathIfNeeded(upward);
            }

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!string.IsNullOrWhiteSpace(desktop))
            {
                var desktopCandidates = new[]
                {
                    Path.Combine(desktop, "mediamtx.exe"),
                    Path.Combine(desktop, "rtsp-simple-server.exe"),
                    Path.Combine(desktop, "MediaMTX"),
                    Path.Combine(desktop, "mediamtx"),
                    Path.Combine(desktop, "rtsp-simple-server")
                };
                foreach (var c in desktopCandidates)
                {
                    var found = ResolveCandidateToExecutable(c, baseDir);
                    if (!string.IsNullOrWhiteSpace(found))
                    {
                        return PersistRelayPathIfNeeded(found);
                    }
                }
            }

            return Path.Combine(baseDir, "mediamtx.exe");
        }

        private static string FindRelayExecutable(string directory)
        {
            try
            {
                var rtsp = Path.Combine(directory, "rtsp-simple-server.exe");
                if (File.Exists(rtsp)) return rtsp;
                var mediamtx = Path.Combine(directory, "mediamtx.exe");
                if (File.Exists(mediamtx)) return mediamtx;
            }
            catch
            {
                // ignore
            }
            return string.Empty;
        }

        private static string SearchUpwardsForRelayExecutable(string startDir, int maxDepth)
        {
            try
            {
                var current = startDir;
                for (int i = 0; i < maxDepth; i++)
                {
                    var found = FindRelayExecutable(current);
                    if (!string.IsNullOrWhiteSpace(found)) return found;

                    var parent = Directory.GetParent(current);
                    if (parent == null) break;
                    current = parent.FullName;
                }
            }
            catch
            {
                // ignore
            }
            return string.Empty;
        }

        private static string ResolveCandidateToExecutable(string candidate, string baseDir)
        {
            if (string.IsNullOrWhiteSpace(candidate)) return string.Empty;
            var p = candidate.Trim();

            if (Directory.Exists(p))
            {
                var found = FindRelayExecutable(p);
                if (!string.IsNullOrWhiteSpace(found)) return found;
            }

            if (!Path.IsPathRooted(p))
            {
                p = Path.Combine(baseDir, p);
            }

            if (Directory.Exists(p))
            {
                var found = FindRelayExecutable(p);
                if (!string.IsNullOrWhiteSpace(found)) return found;
            }

            if (File.Exists(p)) return Path.GetFullPath(p);
            return string.Empty;
        }

        private string PersistRelayPathIfNeeded(string resolvedPath)
        {
            if (_config == null) return resolvedPath;

            var current = _config.RelayExePath ?? string.Empty;
            var currentResolved = ResolveCandidateToExecutable(current, AppDomain.CurrentDomain.BaseDirectory);
            if (string.IsNullOrWhiteSpace(currentResolved) || !File.Exists(currentResolved))
            {
                _config.RelayExePath = resolvedPath;
                _config.Save();
            }

            return resolvedPath;
        }
        #endregion

        #region RC Pro
        private void InitializeRcProModule()
        {
            var ip = GetPreferredIPv4();
            var rtmp = $"rtmp://{ip}:{_config.RelayRtmpPort}/live/raw";
            RcProAddressBox.Text = rtmp;

            var saved = (_config.DroneStreamUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(saved))
            {
                saved = rtmp;
            }
            RcProInputBox.Text = saved;

            UpdateRcProStatusByInput();
        }

        private void RcProCopyButton_Click(object sender, RoutedEventArgs e)
        {
            var text = RcProAddressBox.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text)) return;
            Clipboard.SetText(text);
            SetRcProStatus("已复制推流地址", System.Windows.Media.Brushes.LightGreen, string.Empty);
        }

        private void RcProApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var url = (RcProInputBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                SetRcProStatus("未连接", System.Windows.Media.Brushes.Gray, "请输入 RTMP 地址");
                return;
            }

            _config.DroneStreamUrl = url;
            _config.AlgorithmInputUrl = url;
            _config.Save();

            UpdateAlgorithmInfo();
            SetRcProStatus("已配置", System.Windows.Media.Brushes.DeepSkyBlue, "已写入算法输入源");
        }

        private async void RcProDetectButton_Click(object sender, RoutedEventArgs e)
        {
            var url = (RcProInputBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                SetRcProStatus("未连接", System.Windows.Media.Brushes.Gray, "请输入 RTMP 地址");
                return;
            }

            SetRcProStatus("检测中...", System.Windows.Media.Brushes.Goldenrod, string.Empty);
            string detail = string.Empty;
            var result = await Task.Run(() => ProbeRtmp(url, out detail));
            if (result)
            {
                SetRcProStatus("推流检测成功", System.Windows.Media.Brushes.LawnGreen, detail);
            }
            else
            {
                SetRcProStatus("推流检测失败", System.Windows.Media.Brushes.Red, detail);
            }
        }

        private void RcProInputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRcProStatusByInput();
        }

        private void UpdateRcProStatusByInput()
        {
            var url = (RcProInputBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                SetRcProStatus("未连接", System.Windows.Media.Brushes.Gray, string.Empty);
            }
            else
            {
                SetRcProStatus("已配置", System.Windows.Media.Brushes.DeepSkyBlue, "待检测");
            }
        }

        private void EnsureInputFromPushAddress(bool showHint)
        {
            var push = (RcProAddressBox?.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(push) || IsLoopbackUrl(push))
            {
                return;
            }
            var input = (RcProInputBox?.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(input) || IsLoopbackUrl(input))
            {
                ApplyInputUrl(push, showHint ? "已自动纠正输入源" : string.Empty);
                if (showHint)
                {
                    SetRcProStatus("已配置", System.Windows.Media.Brushes.DeepSkyBlue, "输入源已纠正为推流地址");
                }
            }
        }

        private void SetRcProStatus(string status, System.Windows.Media.Brush brush, string detail)
        {
            RcProStatusText.Text = status ?? string.Empty;
            RcProStatusText.Foreground = brush;
            RcProDetailText.Text = detail ?? string.Empty;
        }

        private static bool ProbeRtmp(string url, out string detail)
        {
            detail = string.Empty;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                detail = "地址格式错误";
                return false;
            }

            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 1935;

            try
            {
                using (var client = new TcpClient())
                {
                    var task = client.ConnectAsync(host, port);
                    if (!task.Wait(TimeSpan.FromSeconds(2)))
                    {
                        detail = "连接超时";
                        return false;
                    }
                    if (!client.Connected)
                    {
                        detail = "无法连接到转发器端口";
                        return false;
                    }
                }
            }
            catch (SocketException ex)
            {
                detail = $"连接失败: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                detail = $"检测异常: {ex.Message}";
                return false;
            }

            detail = "端口可达（请确认 DJI Pilot 2 正在推流且地址一致）";
            return true;
        }
        #endregion

        #region Mediamtx Path
        private async void MtxRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshMtxPathAsync(true);
        }

        private async Task<MtxPathInfo> RefreshMtxPathAsync(bool showHint)
        {
            var info = await FetchMtxPathAsync();
            if (!info.ApiOk)
            {
                var push = (RcProAddressBox?.Text ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(push) && !IsLoopbackUrl(push))
                {
                    ApplyInputUrl(push, "API不可用，已使用推流地址");
                    if (showHint)
                    {
                        SetRcProStatus("已配置", System.Windows.Media.Brushes.Goldenrod, "API不可用，已使用推流地址");
                    }
                }
                else
                {
                    SetMtxPathStatus("API不可用", info.Error ?? "无法访问转发器 API");
                }
                return info;
            }

            if (string.IsNullOrWhiteSpace(info.Name))
            {
                var push = (RcProAddressBox?.Text ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(push) && !IsLoopbackUrl(push))
                {
                    ApplyInputUrl(push, "转发器无路径，已使用推流地址");
                    if (showHint)
                    {
                        SetRcProStatus("已配置", System.Windows.Media.Brushes.Goldenrod, "转发器无路径，已使用推流地址");
                    }
                }
                else
                {
                    SetMtxPathStatus("未收到推流", "转发器未接收到任何推流路径");
                }
                return info;
            }

            var pathName = info.Name.Trim().TrimStart('/');
            var rtmp = BuildRtmpUrl(pathName);
            SetMtxPathStatus(pathName, $"RTMP: {rtmp}");
            if (!string.IsNullOrWhiteSpace(rtmp))
            {
                ApplyInputUrl(rtmp, "已同步转发器路径");
            }

            return info;
        }

        private void ApplyInputUrl(string url, string detail)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            var algorithmUrl = PreferLoopbackForLocalHost(url);
            if (RcProInputBox != null && !string.Equals(RcProInputBox.Text?.Trim(), url, StringComparison.OrdinalIgnoreCase))
            {
                RcProInputBox.Text = algorithmUrl;
            }
            if (_config != null)
            {
                if (!string.Equals(_config.DroneStreamUrl, url, StringComparison.OrdinalIgnoreCase))
                {
                    _config.DroneStreamUrl = url;
                }
                if (!string.Equals(_config.AlgorithmInputUrl, algorithmUrl, StringComparison.OrdinalIgnoreCase))
                {
                    _config.AlgorithmInputUrl = algorithmUrl;
                }
                _config.Save();
            }
            UpdateAlgorithmInfo();
            SetMtxPathStatus("输入源已更新", detail ?? string.Empty);
        }

        private void SetMtxPathStatus(string title, string detail)
        {
            if (MtxPathText != null)
            {
                MtxPathText.Text = title ?? string.Empty;
            }
            if (MtxPathDetailText != null)
            {
                MtxPathDetailText.Text = detail ?? string.Empty;
            }
        }

        private async Task<MtxPathInfo> FetchMtxPathAsync()
        {
            var info = new MtxPathInfo();
            var endpoints = new[]
            {
                "http://127.0.0.1:9997/v2/paths/list",
                "http://127.0.0.1:9997/v3/paths/list",
                "http://127.0.0.1:9997/v2/paths",
                "http://127.0.0.1:9997/v3/paths"
            };

            foreach (var url in endpoints)
            {
                try
                {
                    var resp = await _http.GetAsync(url);
                    if (!resp.IsSuccessStatusCode)
                    {
                        info.Error = $"API返回 {((int)resp.StatusCode)}";
                        continue;
                    }

                    var json = await resp.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        info.Error = "API返回空";
                        continue;
                    }

                    var root = JObject.Parse(json);
                    var items = ExtractPathItems(root);
                    if (items == null || items.Count == 0)
                    {
                        info.ApiOk = true;
                        return info;
                    }

                    JObject pick = null;
                    foreach (var item in items)
                    {
                        var ready = item.Value<bool?>("ready") ?? false;
                        var bytes = item.Value<long?>("bytesReceived") ?? item.Value<long?>("bytes") ?? 0;
                        if (ready || bytes > 0)
                        {
                            pick = item;
                            break;
                        }
                    }
                    if (pick == null)
                    {
                        pick = items[0];
                    }

                    info.ApiOk = true;
                    info.Name = pick.Value<string>("name")
                                ?? pick.Value<string>("path")
                                ?? pick.Value<string>("id")
                                ?? string.Empty;
                    info.Ready = pick.Value<bool?>("ready") ?? false;
                    info.BytesReceived = pick.Value<long?>("bytesReceived") ?? pick.Value<long?>("bytes") ?? 0;
                    return info;
                }
                catch (Exception ex)
                {
                    info.Error = ex.Message;
                }
            }

            info.ApiOk = false;
            return info;
        }

        private static System.Collections.Generic.List<JObject> ExtractPathItems(JObject root)
        {
            var list = new System.Collections.Generic.List<JObject>();
            if (root == null) return list;

            foreach (var key in new[] { "items", "paths", "data", "results" })
            {
                var token = root[key];
                if (token is JArray arr)
                {
                    foreach (var item in arr.OfType<JObject>())
                    {
                        list.Add(item);
                    }
                    if (list.Count > 0) return list;
                }
                else if (token is JObject obj)
                {
                    var inner = obj["items"] as JArray ?? obj["paths"] as JArray;
                    if (inner != null)
                    {
                        foreach (var item in inner.OfType<JObject>())
                        {
                            list.Add(item);
                        }
                        if (list.Count > 0) return list;
                    }
                }
            }

            return list;
        }

        private string BuildRtmpUrl(string pathName)
        {
            if (string.IsNullOrWhiteSpace(pathName)) return string.Empty;
            var ip = GetPreferredIPv4();
            if (string.IsNullOrWhiteSpace(ip))
            {
                ip = "127.0.0.1";
            }
            return $"rtmp://{ip}:{_config.RelayRtmpPort}/{pathName.TrimStart('/')}";
        }

        private static string ResolveBestInputUrl(string pushUrl, string uiUrl, string configUrl)
        {
            var push = (pushUrl ?? string.Empty).Trim();
            var ui = (uiUrl ?? string.Empty).Trim();
            var cfg = (configUrl ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(push) && !IsLoopbackUrl(push))
            {
                return push;
            }

            if (!string.IsNullOrWhiteSpace(ui) && !IsLoopbackUrl(ui))
            {
                return ui;
            }

            if (!string.IsNullOrWhiteSpace(cfg) && !IsLoopbackUrl(cfg))
            {
                return cfg;
            }

            if (!string.IsNullOrWhiteSpace(ui)) return ui;
            if (!string.IsNullOrWhiteSpace(cfg)) return cfg;
            return push;
        }

        private class MtxPathInfo
        {
            public bool ApiOk { get; set; }
            public string Error { get; set; }
            public string Name { get; set; }
            public bool Ready { get; set; }
            public long BytesReceived { get; set; }
        }
        #endregion

        #region Helpers
        private static string GetPreferredIPv4()
        {
            string fallback = null;
            string best = null;
            var bestScore = int.MinValue;

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                if (IsLikelyVirtualOrVpn(ni)) continue;

                var props = ni.GetIPProperties();
                var gateways = props.GatewayAddresses
                    .Select(g => g.Address)
                    .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                    .ToList();

                foreach (var addr in props.UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork))
                {
                    var ip = addr.Address;
                    if (IsApipa(ip)) continue;

                    if (fallback == null) fallback = ip.ToString();

                    var score = 0;
                    if (IsPrivateIPv4(ip)) score += 100;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) score += 30;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) score += 30;
                    if (gateways.Count > 0) score += 20;
                    if (ip.ToString().StartsWith("192.168.", StringComparison.Ordinal)) score += 5;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = ip.ToString();
                    }
                }
            }

            return best ?? fallback ?? "127.0.0.1";
        }

        private void EnsureAlgorithmEnvironment()
        {
            if (_envChecked)
            {
                return;
            }
            _envChecked = true;

            try
            {
                var cfg = AppConfig.Load();
                var configured = (cfg?.AlgorithmPythonPath ?? string.Empty).Trim();
                var expectsLocal = string.IsNullOrWhiteSpace(configured) ||
                                   configured.IndexOf(".venv", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   configured.IndexOf("runtime\\python", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!expectsLocal)
                {
                    return;
                }

                var python = ResolveConfigPath(configured);
                if (!string.IsNullOrWhiteSpace(python) && File.Exists(python))
                {
                    return;
                }

                var script = FindSetupScriptPath();
                if (string.IsNullOrWhiteSpace(script) || !File.Exists(script))
                {
                    MessageBox.Show("未检测到算法运行环境。请运行“一键部署.bat”进行一键部署。", "算法环境",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show("检测到算法运行环境不存在（本地 Python 未就绪）。是否现在一键下载并安装依赖？",
                    "算法环境", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    StartSetupScript(script);
                }
            }
            catch
            {
                // ignore
            }
        }

        private static string ResolveConfigPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var p = value.Trim();
            if (Path.IsPathRooted(p)) return p;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidate = Path.GetFullPath(Path.Combine(baseDir, p));
            if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate;

            var current = baseDir;
            for (int i = 0; i < 8; i++)
            {
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                var alt = Path.GetFullPath(Path.Combine(parent.FullName, p));
                if (File.Exists(alt) || Directory.Exists(alt)) return alt;
                current = parent.FullName;
            }

            return candidate;
        }

        private static string FindSetupScriptPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var current = baseDir;
            for (int i = 0; i < 8; i++)
            {
                var bat = Path.Combine(current, "tools", "one_click_setup.bat");
                if (File.Exists(bat)) return bat;
                var ps1 = Path.Combine(current, "tools", "one_click_setup.ps1");
                if (File.Exists(ps1)) return ps1;
                var rootBat = Path.Combine(current, "一键部署.bat");
                if (File.Exists(rootBat)) return rootBat;

                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            return string.Empty;
        }

        private static void StartSetupScript(string scriptPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
                {
                    MessageBox.Show("未找到一键部署脚本。请确认目录中存在 tools\\one_click_setup.bat 或 一键部署.bat",
                        "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var ext = Path.GetExtension(scriptPath)?.ToLowerInvariant();
                var psi = new ProcessStartInfo();
                if (ext == ".ps1")
                {
                    psi.FileName = "powershell";
                    psi.Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";
                }
                else
                {
                    psi.FileName = "cmd.exe";
                    psi.Arguments = $"/c start \"\" \"{scriptPath}\"";
                }
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动一键部署失败：{ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string PreferLoopbackForLocalHost(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return url;
            if (IsLocalHost(uri.Host))
            {
                var builder = new UriBuilder(uri)
                {
                    Host = "127.0.0.1"
                };
                var rebuilt = builder.Uri.ToString();
                return rebuilt.TrimEnd('/');
            }
            return url;
        }

        private static bool IsLocalHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host)) return false;
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)) return true;
            if (!IPAddress.TryParse(host, out var ip)) return false;
            if (IPAddress.IsLoopback(ip)) return true;
            foreach (var addr in GetLocalIPv4Addresses())
            {
                if (addr.Equals(ip)) return true;
            }
            return false;
        }

        private static IPAddress[] GetLocalIPv4Addresses()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.Address)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<IPAddress>();
            }
        }

        private static bool IsPrivateIPv4(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            if (bytes.Length != 4) return false;
            if (bytes[0] == 10) return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            return false;
        }

        private static bool IsApipa(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            return bytes.Length == 4 && bytes[0] == 169 && bytes[1] == 254;
        }

        private static bool IsLikelyVirtualOrVpn(NetworkInterface ni)
        {
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) return true;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Ppp) return true;

            var name = (ni.Name ?? string.Empty).ToLowerInvariant();
            var desc = (ni.Description ?? string.Empty).ToLowerInvariant();
            var text = name + " " + desc;

            string[] keywords =
            {
                "vpn", "tap", "tun", "virtual", "hyper-v", "vethernet",
                "vmware", "virtualbox", "loopback", "wsl", "cisco", "fortinet",
                "wireguard", "zerotier", "tailscale", "hamachi", "openvpn"
            };

            foreach (var k in keywords)
            {
                if (text.Contains(k)) return true;
            }

            return false;
        }
        #endregion

        private static bool IsLoopbackUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            if (uri.IsLoopback) return true;
            var host = uri.Host ?? string.Empty;
            return host == "127.0.0.1" || host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        }
    }
}
