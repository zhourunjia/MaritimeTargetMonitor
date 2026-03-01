using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Maritime.Core.Config
{
    public class AppConfig
    {
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private bool _dirty;

        // Scene presets & recording
        public string AlgorithmScene { get; set; } = "实时无人机";
        public string DroneStreamUrl { get; set; } = "rtmp://127.0.0.1:1935/live/raw";
        public bool RecordEnabled { get; set; } = true;
        public string RecordDir { get; set; } = "Recordings";
        public int RecordSegmentMinutes { get; set; } = 30;
        public int RecordKeepDays { get; set; } = 30;
        public string RecordFormat { get; set; } = "mp4";
        public string DroneInfoSourceType { get; set; } = "None";
        public string DroneInfoSourceUrl { get; set; } = string.Empty;

        public string ServerIP { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 60800;
        public bool IsHttp { get; set; } = true;
        public int KeepDBDays { get; set; } = 2;
        public int KeepLogDays { get; set; } = 10;
        public int UploadInterval { get; set; } = 0;
        public string Tx2UsbCameraIp { get; set; } = "192.168.1.106";
        public int Tx2UsbCameraPort { get; set; } = 5000;
        public bool EnableTx2UsbCamera { get; set; } = true;
        public string Tx2ThermalCameraIp { get; set; } = "192.168.1.106";
        public int Tx2ThermalCameraPort { get; set; } = 5001;
        public bool EnableTx2ThermalCamera { get; set; } = true;
        public int ThermalCameraWidth { get; set; } = 640;
        public int ThermalCameraHeight { get; set; } = 480;
        public int ThermalCameraFps { get; set; } = 30;
        public string ThermalCameraFormat { get; set; } = "YUYV";
        public bool EnableServer { get; set; } = false;
        public string RtspUrl { get; set; } = "rtsp://127.0.0.1:8554/live/m3t";
        public string RelayExePath { get; set; } = "mediamtx.exe";
        public int RelayRtmpPort { get; set; } = 1935;
        public int RelayRtspPort { get; set; } = 8554;
        public string RelayAppName { get; set; } = "live/m3t";
        public bool RelayAutoStart { get; set; } = true;

        // Algorithm streaming settings
        public string AlgorithmPythonPath { get; set; } = "runtime\\python\\python.exe";
        public string AlgorithmScriptPath { get; set; } = "edgeyolo-main\\edgeyolo-main\\stream_infer_local.py";
        public string AlgorithmWeightsPath { get; set; } = "edgeyolo-main\\edgeyolo-main\\output\\train\\edgeyolo\\best.pth";
        public string AlgorithmInputUrl { get; set; } = "rtmp://127.0.0.1:1935/live/raw";
        public string AlgorithmOutputUrl { get; set; } = "rtmp://127.0.0.1:1935/live/m3t";
        public string AlgorithmFfmpegPath { get; set; } = "ffmpeg-full\\bin\\ffmpeg.exe";
        public string AlgorithmInputSize { get; set; } = "320,320";
        public string AlgorithmOutputSize { get; set; } = "1280x720";
        public string AlgorithmTargetFps { get; set; } = "10";
        public bool AlgorithmUseCpu { get; set; } = true;
        public bool AlgorithmUseTrt { get; set; } = false;
        public bool AlgorithmAutoStart { get; set; } = true;

        [JsonIgnore]
        public bool AutoIpAdjusted { get; private set; }

        public static AppConfig Load()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    json = json.TrimStart('\uFEFF');
                    var config = JsonConvert.DeserializeObject<AppConfig>(json);
                    config.ValidateAndTrim();
                    if (config._dirty)
                    {
                        config.Save();
                    }
                    return config;
                }
                catch
                {
                    return new AppConfig();
                }
            }
            return new AppConfig();
        }

        public void Save()
        {
            ValidateAndTrim();
            try
            {
                JObject root;
                if (File.Exists(ConfigFilePath))
                {
                    try
                    {
                        var original = File.ReadAllText(ConfigFilePath);
                        original = original.TrimStart('\uFEFF');
                        root = JObject.Parse(original);
                    }
                    catch
                    {
                        root = new JObject();
                    }
                }
                else
                {
                    root = new JObject();
                }

                foreach (var prop in GetType().GetProperties())
                {
                    var value = prop.GetValue(this, null);
                    root[prop.Name] = value == null ? JValue.CreateNull() : JToken.FromObject(value);
                }

                File.WriteAllText(ConfigFilePath, root.ToString(Formatting.Indented));
            }
            catch
            {
                // ignore
            }
            finally
            {
                _dirty = false;
            }
        }

        public void ValidateAndTrim()
        {
            KeepDBDays = Math.Max(1, Math.Min(5, KeepDBDays));
            KeepLogDays = Math.Max(1, Math.Min(30, KeepLogDays));
            RecordSegmentMinutes = Math.Max(1, Math.Min(1440, RecordSegmentMinutes));
            RecordKeepDays = Math.Max(1, Math.Min(3650, RecordKeepDays));
            if (string.IsNullOrWhiteSpace(RecordFormat))
            {
                RecordFormat = "mp4";
            }

            if (!IsValidScene(AlgorithmScene))
            {
                AlgorithmScene = "实时无人机";
            }

            ServerPort = Math.Max(1, Math.Min(65535, ServerPort));
            Tx2UsbCameraPort = Math.Max(1, Math.Min(65535, Tx2UsbCameraPort));
            Tx2ThermalCameraPort = Math.Max(1, Math.Min(65535, Tx2ThermalCameraPort));

            RecordDir = ResolveRecordDirectory(RecordDir);
            EnsureRecordDirectory(RecordDir);
            NormalizePaths();
            NormalizeStreamUrls();

            if (!AlgorithmUseCpu && !AlgorithmUseTrt)
            {
                AlgorithmUseCpu = true;
                _dirty = true;
            }
        }

        private static bool IsValidScene(string scene)
        {
            if (string.IsNullOrWhiteSpace(scene)) return false;
            return scene == "实时无人机" || scene == "本地演示" || scene == "离线回放";
        }

        private static string ResolveRecordDirectory(string recordDir)
        {
            var dir = (recordDir ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(dir))
            {
                dir = "Recordings";
            }

            if (Path.IsPathRooted(dir))
            {
                return dir;
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var appRoot = FindMaritimeAppRoot(baseDir);
            var root = string.IsNullOrWhiteSpace(appRoot) ? baseDir : appRoot;
            return Path.GetFullPath(Path.Combine(root, dir));
        }

        private void NormalizePaths()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var appRoot = FindMaritimeAppRoot(baseDir);
            if (string.IsNullOrWhiteSpace(appRoot))
            {
                appRoot = baseDir;
            }
            var edgeRoot = FindEdgeYoloRoot(baseDir);

            AlgorithmScriptPath = NormalizeFilePath(
                AlgorithmScriptPath,
                baseDir,
                edgeRoot,
                "stream_infer_local.py",
                fallbackRelative: "stream_detect.py");

            AlgorithmWeightsPath = NormalizeFilePath(
                AlgorithmWeightsPath,
                baseDir,
                edgeRoot,
                Path.Combine("output", "train", "edgeyolo", "best.pth"),
                fallbackRelative: Path.Combine("output", "ir_best.pth"));

            AlgorithmPythonPath = NormalizePythonPath(AlgorithmPythonPath, baseDir, edgeRoot);

            AlgorithmFfmpegPath = NormalizeFilePath(
                AlgorithmFfmpegPath,
                baseDir,
                baseDir,
                Path.Combine("ffmpeg-full", "bin", "ffmpeg.exe"),
                fallbackRelative: "ffmpeg.exe");

            RelayExePath = NormalizeFilePath(
                RelayExePath,
                baseDir,
                baseDir,
                "mediamtx.exe",
                fallbackRelative: "rtsp-simple-server.exe");

            RecordDir = NormalizeDirectoryPath(
                RecordDir,
                baseDir,
                appRoot,
                "Recordings");
        }

        private void NormalizeStreamUrls()
        {
            var localIp = GetPreferredIPv4();
            if (string.IsNullOrWhiteSpace(localIp))
            {
                return;
            }

            var localSet = GetLocalIPv4Set();

            var anyChanged = false;

            var updated = NormalizeStreamUrl(DroneStreamUrl, localIp, localSet);
            if (!string.Equals(updated, DroneStreamUrl, StringComparison.OrdinalIgnoreCase))
            {
                DroneStreamUrl = updated;
                _dirty = true;
                anyChanged = true;
            }

            updated = NormalizeStreamUrl(AlgorithmInputUrl, localIp, localSet);
            if (!string.Equals(updated, AlgorithmInputUrl, StringComparison.OrdinalIgnoreCase))
            {
                AlgorithmInputUrl = updated;
                _dirty = true;
                anyChanged = true;
            }

            updated = NormalizeStreamUrl(AlgorithmOutputUrl, localIp, localSet);
            if (!string.Equals(updated, AlgorithmOutputUrl, StringComparison.OrdinalIgnoreCase))
            {
                AlgorithmOutputUrl = updated;
                _dirty = true;
                anyChanged = true;
            }

            updated = NormalizeStreamUrl(RtspUrl, localIp, localSet);
            if (!string.Equals(updated, RtspUrl, StringComparison.OrdinalIgnoreCase))
            {
                RtspUrl = updated;
                _dirty = true;
                anyChanged = true;
            }

            AutoIpAdjusted = anyChanged;
        }

        private static string NormalizeStreamUrl(string url, string localIp, HashSet<string> localSet)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return url;

            var scheme = (uri.Scheme ?? string.Empty).ToLowerInvariant();
            if (scheme != "rtmp" && scheme != "rtmps" && scheme != "rtsp" && scheme != "rtsps")
            {
                return url;
            }

            var host = uri.Host ?? string.Empty;
            if (string.IsNullOrWhiteSpace(host)) return url;

            if (IsLocalHost(host, localSet))
            {
                return url;
            }

            if (IPAddress.TryParse(host, out var ip))
            {
                if (!IsPrivateIPv4(ip))
                {
                    return url;
                }

                var builder = new UriBuilder(uri)
                {
                    Host = localIp
                };
                return builder.Uri.ToString().TrimEnd('/');
            }

            return url;
        }

        private static HashSet<string> GetLocalIPv4Set()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "127.0.0.1",
                "localhost"
            };

            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        set.Add(ua.Address.ToString());
                    }
                }
            }
            catch
            {
                // ignore
            }

            return set;
        }

        private static string GetPreferredIPv4()
        {
            IPAddress fallback = null;
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Ppp) continue;

                    var isVirtual = IsLikelyVirtualOrVpn(ni);
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        var ip = ua.Address;
                        if (IsApipa(ip)) continue;

                        if (IsPrivateIPv4(ip) && !isVirtual)
                        {
                            return ip.ToString();
                        }

                        if (fallback == null)
                        {
                            fallback = ip;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            return fallback?.ToString() ?? "127.0.0.1";
        }

        private static bool IsLocalHost(string host, HashSet<string> localSet)
        {
            if (string.IsNullOrWhiteSpace(host)) return false;
            if (localSet == null || localSet.Count == 0) return false;
            return localSet.Contains(host);
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
            var name = (ni.Name ?? string.Empty).ToLowerInvariant();
            var desc = (ni.Description ?? string.Empty).ToLowerInvariant();
            var text = name + " " + desc;
            string[] keywords =
            {
                "vpn", "tap", "tun", "virtual", "hyper-v", "vethernet", "vmware",
                "virtualbox", "loopback", "wsl", "cisco", "fortinet", "wireguard",
                "zerotier", "tailscale", "hamachi", "openvpn"
            };
            foreach (var k in keywords)
            {
                if (text.Contains(k)) return true;
            }
            return false;
        }

        private string NormalizeFilePath(string value, string baseDir, string preferredRoot, string preferredRelative, string fallbackRelative = "")
        {
            var updated = NormalizePathInternal(value, baseDir, preferredRoot, preferredRelative, fallbackRelative, false);
            if (!string.Equals(updated, value, StringComparison.OrdinalIgnoreCase))
            {
                _dirty = true;
            }
            return updated;
        }

        private string NormalizeDirectoryPath(string value, string baseDir, string preferredRoot, string preferredRelative)
        {
            var updated = NormalizePathInternal(value, baseDir, preferredRoot, preferredRelative, string.Empty, true);
            if (!string.Equals(updated, value, StringComparison.OrdinalIgnoreCase))
            {
                _dirty = true;
            }
            return updated;
        }

        private string NormalizePythonPath(string value, string baseDir, string edgeRoot)
        {
            var trimmed = (value ?? string.Empty).Trim();
            var resolved = ResolveConfigPath(trimmed, baseDir, false);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                var updated = IsUnderRoot(resolved, baseDir) ? ToRelativePath(resolved, baseDir) : trimmed;
                if (!string.Equals(updated, value, StringComparison.OrdinalIgnoreCase))
                {
                    _dirty = true;
                }
                return updated;
            }

            var runtimeCandidate = ResolveConfigPath(Path.Combine("runtime", "python", "python.exe"), baseDir, false);
            if (!string.IsNullOrWhiteSpace(runtimeCandidate))
            {
                var updated = ToRelativePath(runtimeCandidate, baseDir);
                if (!string.Equals(updated, value, StringComparison.OrdinalIgnoreCase))
                {
                    _dirty = true;
                }
                return updated;
            }

            if (!string.IsNullOrWhiteSpace(edgeRoot))
            {
                var venvCandidate = Path.Combine(edgeRoot, ".venv", "Scripts", "python.exe");
                if (File.Exists(venvCandidate))
                {
                    var updated = ToRelativePath(venvCandidate, baseDir);
                    if (!string.Equals(updated, value, StringComparison.OrdinalIgnoreCase))
                    {
                        _dirty = true;
                    }
                    return updated;
                }
            }

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                var updated = Path.Combine("runtime", "python", "python.exe");
                if (!string.Equals(updated, value, StringComparison.OrdinalIgnoreCase))
                {
                    _dirty = true;
                }
                return updated;
            }

            return trimmed;
        }

        private static string NormalizePathInternal(string value, string baseDir, string preferredRoot, string preferredRelative, string fallbackRelative, bool isDirectory)
        {
            var trimmed = (value ?? string.Empty).Trim();
            var resolved = ResolveConfigPath(trimmed, baseDir, isDirectory);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                if (IsUnderRoot(resolved, baseDir) || (!string.IsNullOrWhiteSpace(preferredRoot) && IsUnderRoot(resolved, preferredRoot)))
                {
                    return ToRelativePath(resolved, baseDir);
                }
                return trimmed;
            }

            if (!string.IsNullOrWhiteSpace(preferredRoot))
            {
                var candidate = Path.Combine(preferredRoot, preferredRelative ?? string.Empty);
                if (isDirectory || File.Exists(candidate) || Directory.Exists(candidate))
                {
                    return ToRelativePath(candidate, baseDir);
                }
            }

            if (!string.IsNullOrWhiteSpace(fallbackRelative))
            {
                var fallback = Path.Combine(preferredRoot ?? baseDir, fallbackRelative);
                if (isDirectory || File.Exists(fallback) || Directory.Exists(fallback))
                {
                    return ToRelativePath(fallback, baseDir);
                }
            }

            if (string.IsNullOrWhiteSpace(trimmed) && !string.IsNullOrWhiteSpace(preferredRelative))
            {
                var preferred = Path.Combine(preferredRoot ?? baseDir, preferredRelative);
                return ToRelativePath(preferred, baseDir);
            }

            return trimmed;
        }

        private static string ResolveConfigPath(string value, string baseDir, bool isDirectory)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var p = value.Trim();
            if (Path.IsPathRooted(p))
            {
                return PathExists(p, isDirectory) ? p : string.Empty;
            }

            var candidate = Path.GetFullPath(Path.Combine(baseDir, p));
            if (PathExists(candidate, isDirectory)) return candidate;

            var current = baseDir;
            for (int i = 0; i < 8; i++)
            {
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                var alt = Path.GetFullPath(Path.Combine(parent.FullName, p));
                if (PathExists(alt, isDirectory)) return alt;
                current = parent.FullName;
            }

            return string.Empty;
        }

        private static bool PathExists(string path, bool isDirectory)
        {
            return isDirectory ? Directory.Exists(path) : File.Exists(path);
        }

        private static bool IsUnderRoot(string path, string root)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(root)) return false;
            try
            {
                var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string ToRelativePath(string absolutePath, string baseDir)
        {
            if (string.IsNullOrWhiteSpace(absolutePath)) return absolutePath;
            try
            {
                var baseRoot = Path.GetPathRoot(baseDir);
                var absRoot = Path.GetPathRoot(absolutePath);
                if (!string.Equals(baseRoot, absRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return absolutePath;
                }
                var basePath = AppendDirectorySeparator(Path.GetFullPath(baseDir));
                var targetPath = Path.GetFullPath(absolutePath);
                var baseUri = new Uri(basePath, UriKind.Absolute);
                var targetUri = new Uri(targetPath, UriKind.Absolute);
                var relUri = baseUri.MakeRelativeUri(targetUri);
                var rel = Uri.UnescapeDataString(relUri.ToString());
                return rel.Replace('/', '\\');
            }
            catch
            {
                return absolutePath;
            }
        }

        private static string AppendDirectorySeparator(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            var sep = Path.DirectorySeparatorChar.ToString();
            var alt = Path.AltDirectorySeparatorChar.ToString();
            if (path.EndsWith(sep) || path.EndsWith(alt)) return path;
            return path + Path.DirectorySeparatorChar;
        }

        private static string FindEdgeYoloRoot(string startDir)
        {
            if (string.IsNullOrWhiteSpace(startDir)) return string.Empty;

            var current = startDir;
            for (int i = 0; i < 8; i++)
            {
                var candidate = Path.Combine(current, "edgeyolo-main", "edgeyolo-main");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            return string.Empty;
        }

        private static string FindMaritimeAppRoot(string startDir)
        {
            if (string.IsNullOrWhiteSpace(startDir)) return string.Empty;

            var current = startDir;
            for (int i = 0; i < 8; i++)
            {
                var currentCsproj = Path.Combine(current, "Maritime.App.csproj");
                if (File.Exists(currentCsproj))
                {
                    return current;
                }

                var candidate = Path.Combine(current, "Maritime.App");
                var candidateCsproj = Path.Combine(candidate, "Maritime.App.csproj");
                if (File.Exists(candidateCsproj))
                {
                    return candidate;
                }

                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            return string.Empty;
        }

        private static void EnsureRecordDirectory(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return;
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch
            {
                // ignore
            }
        }
    }
}
