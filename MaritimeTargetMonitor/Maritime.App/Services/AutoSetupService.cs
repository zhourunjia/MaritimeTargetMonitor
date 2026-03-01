using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Maritime.Core.Config;

namespace Maritime.App.Services
{
    public static class AutoSetupService
    {
        private static bool _firewallChecked;
        private static bool _vcredistChecked;

        public static void EnsureFirewallRules(AppConfig config)
        {
            if (_firewallChecked)
            {
                return;
            }
            _firewallChecked = true;

            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var marker = Path.Combine(baseDir, "firewall.ok");
                if (File.Exists(marker))
                {
                    return;
                }

                var script = FindFirewallScriptPath(baseDir);
                if (string.IsNullOrWhiteSpace(script) || !File.Exists(script))
                {
                    return;
                }

                var ports = CollectPorts(config);
                if (ports.Count == 0)
                {
                    return;
                }

                var portArg = string.Join(",", ports.Distinct());
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -Ports \"{portArg}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
            }
            catch
            {
                // ignore
            }
        }

        public static void EnsureVCRedist()
        {
            if (_vcredistChecked)
            {
                return;
            }
            _vcredistChecked = true;

            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var marker = Path.Combine(baseDir, "vcredist.ok");
                if (File.Exists(marker))
                {
                    return;
                }

                var script = FindVCRedistScriptPath(baseDir);
                if (string.IsNullOrWhiteSpace(script) || !File.Exists(script))
                {
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
            }
            catch
            {
                // ignore
            }
        }

        private static string FindVCRedistScriptPath(string baseDir)
        {
            var current = baseDir;
            for (int i = 0; i < 8; i++)
            {
                var ps1 = Path.Combine(current, "tools", "vcredist_setup.ps1");
                if (File.Exists(ps1)) return ps1;

                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            return string.Empty;
        }

        private static string FindFirewallScriptPath(string baseDir)
        {
            var current = baseDir;
            for (int i = 0; i < 8; i++)
            {
                var ps1 = Path.Combine(current, "tools", "firewall_setup.ps1");
                if (File.Exists(ps1)) return ps1;

                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            return string.Empty;
        }

        private static List<int> CollectPorts(AppConfig config)
        {
            var ports = new List<int>();
            if (config != null)
            {
                if (config.RelayRtmpPort > 0) ports.Add(config.RelayRtmpPort);
                if (config.RelayRtspPort > 0) ports.Add(config.RelayRtspPort);
                if (config.ServerPort > 0) ports.Add(config.ServerPort);
                ports.AddRange(ExtractPorts(config.DroneStreamUrl));
                ports.AddRange(ExtractPorts(config.AlgorithmInputUrl));
                ports.AddRange(ExtractPorts(config.AlgorithmOutputUrl));
                ports.AddRange(ExtractPorts(config.RtspUrl));
            }

            return ports.Where(p => p > 0 && p < 65536).Distinct().ToList();
        }

        private static IEnumerable<int> ExtractPorts(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) yield break;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) yield break;

            if (uri.Port > 0)
            {
                yield return uri.Port;
                yield break;
            }

            var scheme = (uri.Scheme ?? string.Empty).ToLowerInvariant();
            if (scheme == "rtmp" || scheme == "rtmps")
            {
                yield return 1935;
            }
            else if (scheme == "rtsp" || scheme == "rtsps")
            {
                yield return 8554;
            }
        }
    }
}
