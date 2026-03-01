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
using LibVLCSharp.Shared;
using Maritime.Core.Config;
using Maritime.Infrastructure.Services;
using Maritime.App.Services;
using Newtonsoft.Json.Linq;

namespace Maritime.App.Pages
{
    public partial class MainPage : Page
    {
        private readonly AppConfig _config;
        private LibVLC _homeLibVlc;
        private MediaPlayer _homeMediaPlayer;
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        private const int DirectLiveCacheMs = 150;
        private const int AlgorithmLiveCacheMs = 800;
        private bool _envChecked;

        public MainPage()
        {
            InitializeComponent();
            _config = AppConfig.Load();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateHomeUrls();
            EnsureAlgorithmEnvironment();
            InitializeOneKeyStatus();
            UpdateDirectLiveStatus();
            ShowAutoIpHintIfNeeded();
            AutoSetupService.EnsureVCRedist();
            AutoSetupService.EnsureFirewallRules(_config);
            AlgorithmProcessService.Instance.StatusChanged += OnAlgorithmStatusChanged;

            var started = false;
            if (_config?.RelayAutoStart == true)
            {
                started = StartHomeRelay();
                if (started)
                {
                    SetChainStatus(OneKeyRelayStatus, "已启动", System.Windows.Media.Brushes.DarkGreen);
                }
            }

            if (!started)
            {
                StartHomeLive();
            }

            var algConfig = AppConfig.Load();
            if (algConfig.AlgorithmAutoStart && !AlgorithmProcessService.Instance.IsRunning)
            {
                StartAlgorithmWithConfig(algConfig, keepAlive: true, showError: true);
            }
        }

        private void ShowAutoIpHintIfNeeded()
        {
            try
            {
                if (_config == null || !_config.AutoIpAdjusted) return;
                var rtmp = (_config.DroneStreamUrl ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(rtmp))
                {
                    Clipboard.SetText(rtmp);
                }

                var message = $"已自动将推流地址调整为本机 IP：\n{rtmp}\n\n请在 DJI Pilot 2 中把 RTMP 推流地址改为上面的地址（已复制到剪贴板）。";
                MessageBox.Show(message, "推流地址已更新", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                // ignore
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopHomeLive();
            AlgorithmProcessService.Instance.StatusChanged -= OnAlgorithmStatusChanged;
        }

        #region Live
        private void StartHomeLive()
        {
            try
            {
                StopHomeLive();
                LibVLCSharp.Shared.Core.Initialize();
                var latest = AppConfig.Load();
                var url = latest?.RtspUrl ?? _config?.RtspUrl;
                if (string.IsNullOrWhiteSpace(url))
                {
                    HomeLiveStatus.Text = "未配置 RtspUrl";
                    HomeLiveOverlay.Text = "未配置拉流地址";
                    return;
                }

                var cacheMs = ResolveLiveCacheMs(url);
                _homeLibVlc = new LibVLC($"--network-caching={cacheMs}");
                _homeMediaPlayer = new MediaPlayer(_homeLibVlc);
                AttachHomePlayerEvents();
                HomeLiveVideoView.MediaPlayer = _homeMediaPlayer;

                HomeLiveStatus.Text = "拉流中";
                HomeLiveOverlay.Text = url;
                SetChainStatus(OneKeyPlayStatus, "拉流中", System.Windows.Media.Brushes.Goldenrod);
                using (var media = new Media(_homeLibVlc, new Uri(url)))
                {
                    _homeMediaPlayer.Play(media);
                }
            }
            catch (Exception ex)
            {
                HomeLiveStatus.Text = $"拉流失败: {ex.Message}";
                HomeLiveOverlay.Text = string.Empty;
                SetChainStatus(OneKeyPlayStatus, "播放失败", System.Windows.Media.Brushes.Red);
            }
        }

        private static int ResolveLiveCacheMs(string url)
        {
            if (!string.IsNullOrWhiteSpace(url) &&
                url.EndsWith("/live/m3t", StringComparison.OrdinalIgnoreCase))
            {
                return AlgorithmLiveCacheMs;
            }
            return DirectLiveCacheMs;
        }

        private void StopHomeLive()
        {
            try
            {
                _homeMediaPlayer?.Stop();
                _homeMediaPlayer?.Dispose();
                _homeMediaPlayer = null;
                _homeLibVlc?.Dispose();
                _homeLibVlc = null;
                HomeLiveVideoView.MediaPlayer = null;
                SetChainStatus(OneKeyPlayStatus, "未播放", System.Windows.Media.Brushes.Gray);
            }
            catch
            {
                // ignore
            }
        }
        #endregion

        #region Relay
        private bool StartHomeRelay()
        {
            try
            {
                if (RelayProcessService.Instance.IsRunning)
                {
                    UpdateHomeUrls();
                    return true;
                }

                var exePath = ResolveHomeRelayPath();
                if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                {
                    return false;
                }

                if (!RelayProcessService.Instance.Start(exePath, out _))
                {
                    return false;
                }

                _config.RelayExePath = exePath;
                _config.Save();

                UpdateHomeUrls();
                StartHomeLive();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void StopHomeRelay()
        {
            try
            {
                RelayProcessService.Instance.Stop();
            }
            catch
            {
                // ignore
            }
        }

        private void UpdateHomeUrls()
        {
            var ip = GetPreferredIPv4();
            var rtmp = $"rtmp://{ip}:{_config.RelayRtmpPort}/{_config.RelayAppName}";
            var rtsp = $"rtsp://{ip}:{_config.RelayRtspPort}/{_config.RelayAppName}";
            HomeLiveOverlay.Text = _config.RtspUrl;

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

        #region Algorithm
        private void StartAlgorithmWithConfig(AppConfig cfg, bool keepAlive = false, bool showError = true)
        {
            TryStartAlgorithm(cfg, showError, keepAlive);
        }

        private bool TryStartAlgorithm(AppConfig cfg, bool showError, bool keepAlive)
        {
            if (cfg != null && !string.Equals(cfg.AlgorithmScene, "离线回放", StringComparison.Ordinal))
            {
                var droneUrl = (cfg.DroneStreamUrl ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(droneUrl))
                {
                    droneUrl = (cfg.AlgorithmInputUrl ?? string.Empty).Trim();
                }

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
                return true;
            }

            if (error == "算法已在运行")
            {
                return true;
            }

            if (showError)
            {
                MessageBox.Show(error, "算法启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }

        private void OnAlgorithmStatusChanged(object sender, AlgorithmStatusEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var cfg = AppConfig.Load();
                if (string.Equals(cfg.AlgorithmScene, "离线回放", StringComparison.Ordinal))
                {
                    SetChainStatus(OneKeyAlgoStatus, "离线回放", System.Windows.Media.Brushes.Goldenrod);
                    return;
                }
                SetChainStatus(OneKeyAlgoStatus, e.IsRunning ? "运行中" : "未启动", e.IsRunning ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.Gray);
            });
        }
        #endregion

        #region Direct Live
        private void DirectLiveButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleDirectLiveMode();
        }

        private void ToggleDirectLiveMode()
        {
            var cfg = AppConfig.Load();
            var current = (cfg?.RtspUrl ?? string.Empty).Trim();
            var isDirect = current.EndsWith("/live/raw", StringComparison.OrdinalIgnoreCase);
            var changed = false;
            if (isDirect)
            {
                changed = ApplyAlgorithmLiveMode(cfg, current);
            }
            else
            {
                changed = ApplyDirectLiveMode(cfg);
            }

            if (!changed)
            {
                return;
            }

            UpdateHomeUrls();
            UpdateDirectLiveStatus();
            StopHomeLive();
            StartHomeLive();
        }

        private bool ApplyDirectLiveMode(AppConfig cfg)
        {
            var input = ResolveBestInputUrl((cfg?.DroneStreamUrl ?? string.Empty).Trim(),
                                            (cfg?.AlgorithmInputUrl ?? string.Empty).Trim(),
                                            string.Empty);
            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("未找到可用推流地址，请先确认 DJI 推流。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var host = ResolveRelayHostFromUrl(PreferLoopbackForLocalHost(input));
            var rtsp = $"rtsp://{host}:{_config.RelayRtspPort}/live/raw";
            SaveRtspUrl(cfg, rtsp);
            return true;
        }

        private bool ApplyAlgorithmLiveMode(AppConfig cfg, string currentRtsp)
        {
            var host = ResolveRelayHostFromUrl(PreferLoopbackForLocalHost(currentRtsp));
            var rtsp = $"rtsp://{host}:{_config.RelayRtspPort}/live/m3t";
            SaveRtspUrl(cfg, rtsp);
            return true;
        }

        private string ResolveRelayHostFromUrl(string url)
        {
            if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
            {
                return uri.Host;
            }
            var host = GetPreferredIPv4();
            return string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host;
        }

        private void SaveRtspUrl(AppConfig cfg, string rtsp)
        {
            _config.RtspUrl = rtsp;
            _config.Save();
            cfg.RtspUrl = rtsp;
            cfg.Save();
        }

        private void UpdateDirectLiveStatus()
        {
            if (DirectLiveStatus == null) return;
            var cfg = AppConfig.Load();
            var rtsp = (cfg.RtspUrl ?? string.Empty).Trim();
            if (rtsp.EndsWith("/live/raw", StringComparison.OrdinalIgnoreCase))
            {
                DirectLiveStatus.Text = "当前为直通模式";
                DirectLiveStatus.Foreground = System.Windows.Media.Brushes.DeepSkyBlue;
            }
            else
            {
                DirectLiveStatus.Text = "当前为算法模式";
                DirectLiveStatus.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }
        #endregion

        #region RC Pro
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
        private async Task<MtxPathInfo> RefreshMtxPathAsync(bool showHint)
        {
            var info = await FetchMtxPathAsync();
            if (!info.ApiOk)
            {
                var push = (_config?.DroneStreamUrl ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(push) && !IsLoopbackUrl(push))
                {
                    ApplyInputUrl(push, "API不可用，已使用推流地址");
                    if (showHint)
                    {
                        SetChainStatus(OneKeyPushStatus, "API不可用，已使用推流地址", System.Windows.Media.Brushes.Goldenrod);
                    }
                }
                else
                {
                    if (showHint)
                    {
                        SetChainStatus(OneKeyPushStatus, "未收到推流（API不可用）", System.Windows.Media.Brushes.Red);
                    }
                }
                return info;
            }

            if (string.IsNullOrWhiteSpace(info.Name))
            {
                var push = (_config?.DroneStreamUrl ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(push) && !IsLoopbackUrl(push))
                {
                    ApplyInputUrl(push, "转发器无路径，已使用推流地址");
                    if (showHint)
                    {
                        SetChainStatus(OneKeyPushStatus, "转发器无路径，已使用推流地址", System.Windows.Media.Brushes.Goldenrod);
                    }
                }
                else
                {
                    if (showHint)
                    {
                        SetChainStatus(OneKeyPushStatus, "未收到推流（转发器无路径）", System.Windows.Media.Brushes.Red);
                    }
                }
                if (showHint)
                {
                    // handled above
                }
                return info;
            }

            var pathName = info.Name.Trim().TrimStart('/');
            var rtmp = BuildRtmpUrl(pathName);
            if (!string.IsNullOrWhiteSpace(rtmp))
            {
                ApplyInputUrl(rtmp, "已同步转发器路径");
            }

            if (showHint)
            {
                SetChainStatus(OneKeyPushStatus, "转发器已收到推流", System.Windows.Media.Brushes.DarkGreen);
            }
            return info;
        }

        private void ApplyInputUrl(string url, string detail)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            var algorithmUrl = PreferLoopbackForLocalHost(url);
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
        private static string GetLocalIPv4()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                var addr = ni.GetIPProperties().UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                if (addr != null) return addr.Address.ToString();
            }
            return "127.0.0.1";
        }

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
                "vpn", "tap", "tun", "virtual", "hyper-v", "vEthernet".ToLowerInvariant(),
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

        private static string QuoteArg(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "\"\"";
            return value.Contains(" ") ? $"\"{value}\"" : value;
        }

        #region OneKey
        private void InitializeOneKeyStatus()
        {
            SetChainStatus(OneKeyRelayStatus, "未启动", System.Windows.Media.Brushes.Gray);
            SetChainStatus(OneKeyPushStatus, "未检测", System.Windows.Media.Brushes.Gray);
            SetChainStatus(OneKeyAlgoStatus, AlgorithmProcessService.Instance.IsRunning ? "运行中" : "未启动",
                AlgorithmProcessService.Instance.IsRunning ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.Gray);
            SetChainStatus(OneKeyPlayStatus, "未播放", System.Windows.Media.Brushes.Gray);
        }

        private async void OneKeyStartButton_Click(object sender, RoutedEventArgs e)
        {
            OneKeyStartButton.IsEnabled = false;
            try
            {
                SetChainStatus(OneKeyRelayStatus, "启动中...", System.Windows.Media.Brushes.Goldenrod);
                var relayOk = StartHomeRelay();
                SetChainStatus(OneKeyRelayStatus, relayOk ? "已启动" : "启动失败", relayOk ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.Red);
                if (!relayOk)
                {
                    return;
                }

                await RefreshMtxPathAsync(true);
                var input = (_config?.DroneStreamUrl ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    input = (_config?.AlgorithmInputUrl ?? string.Empty).Trim();
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    SetChainStatus(OneKeyPushStatus, "输入源为空", System.Windows.Media.Brushes.Red);
                    return;
                }

                // always sync input to config (prefer loopback for local host)
                ApplyInputUrl(input, "已同步输入源");
                input = (_config?.AlgorithmInputUrl ?? input).Trim();

                SetChainStatus(OneKeyPushStatus, "检测中...", System.Windows.Media.Brushes.Goldenrod);
                var detail = string.Empty;
                var pushOk = await Task.Run(() => DetectStreamSource(input, out detail, 20000));
                if (pushOk)
                {
                    SetChainStatus(OneKeyPushStatus, "检测成功", System.Windows.Media.Brushes.DarkGreen);
                }
                else
                {
                    var allowContinue = !string.IsNullOrWhiteSpace(detail) &&
                                        (detail.Contains("超时") || detail.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0);
                    SetChainStatus(OneKeyPushStatus, allowContinue ? $"启动中：{detail}" : $"检测失败：{detail}",
                        allowContinue ? System.Windows.Media.Brushes.Goldenrod : System.Windows.Media.Brushes.Red);
                    if (!allowContinue)
                    {
                        return;
                    }
                }

                var cfg = AppConfig.Load();
                if (AlgorithmProcessService.Instance.IsRunning)
                {
                    SetChainStatus(OneKeyAlgoStatus, "已预热", System.Windows.Media.Brushes.DarkGreen);
                }
                else
                {
                    SetChainStatus(OneKeyAlgoStatus, "启动中...", System.Windows.Media.Brushes.Goldenrod);
                    var algOk = TryStartAlgorithm(cfg, showError: true, keepAlive: true);
                    SetChainStatus(OneKeyAlgoStatus, algOk ? "运行中" : "启动失败", algOk ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.Red);
                    if (!algOk)
                    {
                        return;
                    }
                }

                SetChainStatus(OneKeyPlayStatus, "启动中...", System.Windows.Media.Brushes.Goldenrod);
                _ = StartHomeLiveWhenReadyAsync();
            }
            finally
            {
                OneKeyStartButton.IsEnabled = true;
            }
        }

        private static bool DetectStreamSource(string source, out string detail, int timeoutMs = 5000)
        {
            detail = string.Empty;
            if (string.IsNullOrWhiteSpace(source))
            {
                detail = "输入源为空";
                return false;
            }

            if (File.Exists(source))
            {
                detail = "本地文件存在";
                return true;
            }

            if (int.TryParse(source, out _))
            {
                detail = "本地摄像头";
                return true;
            }

            var ffmpeg = ResolveFfmpegPath();
            if (!string.IsNullOrWhiteSpace(ffmpeg) && (ffmpeg == "ffmpeg" || File.Exists(ffmpeg)))
            {
                var isRtmp = source.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase) ||
                             source.StartsWith("rtmps://", StringComparison.OrdinalIgnoreCase);
                var isRtsp = source.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) ||
                             source.StartsWith("rtsps://", StringComparison.OrdinalIgnoreCase);
                var duration = isRtmp || isRtsp ? 2 : 1;
                var waitMs = Math.Max(timeoutMs, isRtmp ? 15000 : 8000);
                return ProbeWithFfmpeg(ffmpeg, source, out detail, waitMs, duration);
            }

            if (source.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase))
            {
                return ProbeRtmp(source, out detail);
            }

            detail = "未配置 ffmpeg，无法验证流";
            return true;
        }

        private static string ResolveFfmpegPath()
        {
            try
            {
                var cfg = AppConfig.Load();
                var ffmpeg = (cfg.AlgorithmFfmpegPath ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(ffmpeg))
                {
                    return "ffmpeg";
                }
                if (Path.IsPathRooted(ffmpeg))
                {
                    return ffmpeg;
                }
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var candidate = Path.Combine(baseDir, ffmpeg);
                return File.Exists(candidate) ? candidate : ffmpeg;
            }
            catch
            {
                return "ffmpeg";
            }
        }

        private static bool ProbeWithFfmpeg(string ffmpegPath, string url, out string detail, int timeoutMs, int durationSec)
        {
            detail = string.Empty;
            try
            {
                var extra = string.Empty;
                if (!string.IsNullOrWhiteSpace(url) &&
                    (url.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase) ||
                     url.StartsWith("rtmps://", StringComparison.OrdinalIgnoreCase)))
                {
                    extra = "-rtmp_live live ";
                }
                if (!string.IsNullOrWhiteSpace(url) &&
                    (url.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) ||
                     url.StartsWith("rtsps://", StringComparison.OrdinalIgnoreCase)))
                {
                    extra += "-rtsp_transport tcp ";
                }

                var psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-hide_banner -loglevel error {extra}-i {QuoteCmd(url)} -t {durationSec} -f null -",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var p = Process.Start(psi))
                {
                    if (p == null)
                    {
                        detail = "ffmpeg 启动失败";
                        return false;
                    }

                    if (!p.WaitForExit(timeoutMs))
                    {
                        try { p.Kill(); } catch { }
                        detail = "检测超时";
                        return false;
                    }

                    var err = p.StandardError.ReadToEnd();
                    if (p.ExitCode == 0)
                    {
                        detail = "推流可用";
                        return true;
                    }

                    detail = string.IsNullOrWhiteSpace(err) ? $"ffmpeg 退出码 {p.ExitCode}" : FirstLine(err);
                    return false;
                }
            }
            catch (Exception ex)
            {
                detail = $"检测异常: {ex.Message}";
                return false;
            }
        }

        private async Task StartHomeLiveWhenReadyAsync()
        {
            try
            {
                var latest = AppConfig.Load();
                var url = latest?.RtspUrl ?? _config?.RtspUrl;
                if (string.IsNullOrWhiteSpace(url))
                {
                    SetChainStatus(OneKeyPlayStatus, "未配置RtspUrl", System.Windows.Media.Brushes.Red);
                    return;
                }

                HomeLiveStatus.Text = "启动中";
                HomeLiveOverlay.Text = url;
                SetChainStatus(OneKeyPlayStatus, "启动中...", System.Windows.Media.Brushes.Goldenrod);

                var ffmpeg = ResolveFfmpegPath();
                if (string.IsNullOrWhiteSpace(ffmpeg) || (ffmpeg != "ffmpeg" && !File.Exists(ffmpeg)))
                {
                    StartHomeLive();
                    return;
                }

                var sw = Stopwatch.StartNew();
                var detail = string.Empty;
                var ready = false;
                while (sw.ElapsedMilliseconds < 60000)
                {
                    ready = await Task.Run(() => ProbeWithFfmpeg(ffmpeg, url, out detail, 8000, 2));
                    if (ready)
                    {
                        break;
                    }
                    await Task.Delay(2000);
                }

                if (!ready)
                {
                    SetChainStatus(OneKeyPlayStatus, "等待输出中", System.Windows.Media.Brushes.Goldenrod);
                    HomeLiveStatus.Text = "等待输出中";
                    // still attempt to play; VLC will retry when stream appears
                    StartHomeLive();
                    return;
                }

                StartHomeLive();
            }
            catch (Exception ex)
            {
                SetChainStatus(OneKeyPlayStatus, $"播放失败：{ex.Message}", System.Windows.Media.Brushes.Red);
            }
        }

        private static string FirstLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 0 ? lines[0] : text.Trim();
        }

        private static string QuoteCmd(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "\"\"";
            if (value.Contains(" ")) return $"\"{value}\"";
            return value;
        }

        private void AttachHomePlayerEvents()
        {
            if (_homeMediaPlayer == null) return;
            _homeMediaPlayer.Playing -= HomePlayer_Playing;
            _homeMediaPlayer.EncounteredError -= HomePlayer_EncounteredError;
            _homeMediaPlayer.EndReached -= HomePlayer_EndReached;

            _homeMediaPlayer.Playing += HomePlayer_Playing;
            _homeMediaPlayer.EncounteredError += HomePlayer_EncounteredError;
            _homeMediaPlayer.EndReached += HomePlayer_EndReached;
        }

        private void HomePlayer_Playing(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => SetChainStatus(OneKeyPlayStatus, "播放中", System.Windows.Media.Brushes.DarkGreen));
        }

        private void HomePlayer_EncounteredError(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => SetChainStatus(OneKeyPlayStatus, "播放失败", System.Windows.Media.Brushes.Red));
        }

        private void HomePlayer_EndReached(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => SetChainStatus(OneKeyPlayStatus, "已结束", System.Windows.Media.Brushes.Gray));
        }

        private static void SetChainStatus(TextBlock target, string text, System.Windows.Media.Brush brush)
        {
            if (target == null) return;
            target.Text = text ?? string.Empty;
            target.Foreground = brush ?? System.Windows.Media.Brushes.Gray;
        }
        #endregion
    }
}


