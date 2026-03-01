using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Maritime.Core.Config;
using Maritime.Core.Logging;

namespace Maritime.Infrastructure.Services
{
    public sealed class RecordingService
    {
        private readonly object _sync = new object();
        private Process _process;

        public bool IsRunning => _process != null && !_process.HasExited;

        public bool Start(AppConfig config, out string error)
        {
            error = string.Empty;
            if (config == null)
            {
                error = "配置为空";
                return false;
            }

            if (!config.RecordEnabled)
            {
                return false;
            }

            lock (_sync)
            {
                if (IsRunning)
                {
                    error = "录制已在运行";
                    return false;
                }
            }

            var outputUrl = (config.AlgorithmOutputUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(outputUrl))
            {
                error = "算法输出地址为空";
                return false;
            }

            var ffmpeg = (config.AlgorithmFfmpegPath ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(ffmpeg))
            {
                ffmpeg = "ffmpeg";
            }
            else if (!File.Exists(ffmpeg))
            {
                error = $"FFmpeg 未找到: {ffmpeg}";
                return false;
            }

            var recordDir = (config.RecordDir ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(recordDir))
            {
                error = "录制目录为空";
                return false;
            }

            try
            {
                Directory.CreateDirectory(recordDir);
                CleanupOldFolders(recordDir, config.RecordKeepDays);

                var dateFolder = Path.Combine(recordDir, DateTime.Now.ToString("yyyy-MM-dd"));
                Directory.CreateDirectory(dateFolder);

                var format = string.IsNullOrWhiteSpace(config.RecordFormat) ? "mp4" : config.RecordFormat.Trim().TrimStart('.');
                var segmentSeconds = Math.Max(60, config.RecordSegmentMinutes * 60);
                var outputPattern = Path.Combine(dateFolder, $"%H%M%S.{format}");

                var args = BuildFfmpegArgs(outputUrl, outputPattern, segmentSeconds, format);
                var psi = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _process = new Process
                {
                    StartInfo = psi,
                    EnableRaisingEvents = true
                };

                _process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Logger.Info($"[Recorder] {e.Data}");
                    }
                };
                _process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Logger.Warn($"[Recorder] {e.Data}");
                    }
                };
                _process.Exited += (s, e) =>
                {
                    Logger.Warn("录制进程已退出");
                };

                if (!_process.Start())
                {
                    error = "录制启动失败";
                    return false;
                }

                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                Logger.Info("录制已启动");
                return true;
            }
            catch (Exception ex)
            {
                error = $"录制启动异常: {ex.Message}";
                Logger.Warn("录制启动异常", ex);
                return false;
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                if (!IsRunning) return;

                try
                {
                    _process.Kill();
                    _process.WaitForExit(3000);
                }
                catch (Exception ex)
                {
                    Logger.Warn("停止录制失败", ex);
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

            Logger.Info("录制已停止");
        }

        private static string BuildFfmpegArgs(string inputUrl, string outputPattern, int segmentSeconds, string format)
        {
            var args = "-hide_banner -loglevel error ";
            if (inputUrl.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) ||
                inputUrl.StartsWith("rtsps://", StringComparison.OrdinalIgnoreCase))
            {
                args += "-rtsp_transport tcp ";
            }

            args += $"-i \"{inputUrl}\" ";
            args += "-an -c:v libx264 -preset veryfast -tune zerolatency -pix_fmt yuv420p ";
            args += $"-f segment -segment_time {segmentSeconds.ToString(CultureInfo.InvariantCulture)} ";
            args += $"-segment_format {format} -reset_timestamps 1 -strftime 1 ";
            args += $"\"{outputPattern}\"";
            return args;
        }

        private static void CleanupOldFolders(string recordDir, int keepDays)
        {
            if (keepDays <= 0) return;

            try
            {
                var cutoff = DateTime.Now.Date.AddDays(-keepDays);
                foreach (var dir in Directory.GetDirectories(recordDir))
                {
                    var name = Path.GetFileName(dir);
                    if (!DateTime.TryParseExact(name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    {
                        continue;
                    }

                    if (date < cutoff)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
