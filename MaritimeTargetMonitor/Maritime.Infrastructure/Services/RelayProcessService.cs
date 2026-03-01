using System;
using System.Diagnostics;
using System.IO;

namespace Maritime.Infrastructure.Services
{
    public sealed class RelayProcessService
    {
        private static readonly Lazy<RelayProcessService> _instance =
            new Lazy<RelayProcessService>(() => new RelayProcessService());

        public static RelayProcessService Instance => _instance.Value;

        private readonly object _sync = new object();
        private Process _process;

        public bool IsRunning => _process != null && !_process.HasExited;

        private RelayProcessService()
        {
        }

        public bool Start(string exePath, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(exePath))
            {
                error = "转发器路径为空";
                return false;
            }

            lock (_sync)
            {
                if (IsRunning)
                {
                    return true;
                }

                if (!File.Exists(exePath))
                {
                    error = $"转发器不存在: {exePath}";
                    return false;
                }

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = Path.GetDirectoryName(exePath),
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    var exeName = Path.GetFileName(exePath) ?? string.Empty;
                    if (exeName.Equals("mediamtx.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        var conf = Path.Combine(Path.GetDirectoryName(exePath) ?? string.Empty, "mediamtx.yml");
                        if (File.Exists(conf))
                        {
                            psi.Arguments = QuoteArg(conf);
                        }
                    }

                    _process = Process.Start(psi);
                    if (_process == null)
                    {
                        error = "转发器启动失败";
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    error = $"转发器启动异常: {ex.Message}";
                    return false;
                }
            }

            return true;
        }

        public void Stop()
        {
            lock (_sync)
            {
                if (!IsRunning) return;

                try
                {
                    _process.Kill();
                    _process.WaitForExit(2000);
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    try
                    {
                        _process.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }
                    _process = null;
                }
            }
        }

        private static string QuoteArg(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "\"\"";
            return value.Contains(" ") ? $"\"{value}\"" : value;
        }
    }
}
