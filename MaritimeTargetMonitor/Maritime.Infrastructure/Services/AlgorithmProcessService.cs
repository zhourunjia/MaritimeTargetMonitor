using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using Maritime.Core.Config;
using Maritime.Core.Logging;

namespace Maritime.Infrastructure.Services
{
    public sealed class AlgorithmProcessService
    {
        private static readonly Lazy<AlgorithmProcessService> _instance =
            new Lazy<AlgorithmProcessService>(() => new AlgorithmProcessService());

        public static AlgorithmProcessService Instance => _instance.Value;

        private readonly object _sync = new object();
        private Process _process;
        private readonly RecordingService _recordingService = new RecordingService();

        public event EventHandler<AlgorithmStatusEventArgs> StatusChanged;

        public bool IsRunning => _process != null && !_process.HasExited;

        private AlgorithmProcessService()
        {
        }

        public bool Start(AppConfig config, out string error, bool keepAlive = false)
        {
            error = string.Empty;
            if (config == null)
            {
                error = "配置为空";
                return false;
            }

            TryAutoRepairPaths(config);

            lock (_sync)
            {
                if (IsRunning)
                {
                    error = "算法已在运行";
                    return false;
                }
            }

            var pythonExe = ResolveExecutable(config.AlgorithmPythonPath, "python");
            var scriptPath = ResolvePath(config.AlgorithmScriptPath);
            var weightsPath = ResolvePath(config.AlgorithmWeightsPath);
            var inputUrl = (config.AlgorithmInputUrl ?? string.Empty).Trim();
            var outputUrl = (config.AlgorithmOutputUrl ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
            {
                error = $"算法脚本不存在: {scriptPath}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(weightsPath) || !File.Exists(weightsPath))
            {
                error = $"权重文件不存在: {weightsPath}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(inputUrl))
            {
                error = "输入源为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(outputUrl))
            {
                error = "输出地址为空";
                return false;
            }

            if (RequiresFileCheck(pythonExe) && !File.Exists(pythonExe))
            {
                error = $"Python 未找到: {pythonExe}";
                return false;
            }

            var args = BuildArguments(config, scriptPath, weightsPath, inputUrl, outputUrl, keepAlive);
            var workingDir = Path.GetDirectoryName(scriptPath) ?? AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = args,
                    WorkingDirectory = workingDir,
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
                        Logger.Info($"[Algorithm] {e.Data}");
                    }
                };
                _process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Logger.Warn($"[Algorithm] {e.Data}");
                    }
                };
                _process.Exited += (s, e) =>
                {
                    try
                    {
                        _recordingService.Stop();
                    }
                    catch
                    {
                        // ignore
                    }
                    OnStatusChanged(false, "算法已退出");
                };

                if (!_process.Start())
                {
                    error = "算法启动失败";
                    return false;
                }

                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                OnStatusChanged(true, "算法已启动");

                if (config.RecordEnabled)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            if (!_recordingService.Start(config, out var recordError))
                            {
                                if (!string.IsNullOrWhiteSpace(recordError))
                                {
                                    Logger.Warn($"录制启动失败: {recordError}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn("录制启动异常", ex);
                        }
                    });
                }
                return true;
            }
            catch (Exception ex)
            {
                error = $"算法启动异常: {ex.Message}";
                Logger.Error("算法启动异常", ex);
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
                    _recordingService.Stop();
                }
                catch
                {
                    // ignore
                }

                try
                {
                    _process.Kill();
                    _process.WaitForExit(3000);
                }
                catch (Exception ex)
                {
                    Logger.Warn("停止算法进程失败", ex);
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

            OnStatusChanged(false, "算法已停止");
        }

        private static string BuildArguments(AppConfig config, string scriptPath, string weightsPath, string inputUrl, string outputUrl, bool keepAlive)
        {
            var args = new List<string>
            {
                Quote(scriptPath),
                "-w", Quote(weightsPath),
                "-s", Quote(inputUrl),
                "-o", Quote(outputUrl)
            };

            if (IsStreamSource(inputUrl))
            {
                args.Add("--retry");
                args.Add(keepAlive ? "-1" : "60");
                args.Add("--retry-interval");
                args.Add("1");
            }

            if (config.AlgorithmUseCpu)
            {
                args.Add("--cpu");
            }
            else if (config.AlgorithmUseTrt)
            {
                args.Add("--trt");
            }

            var size = ParseInputSize(config.AlgorithmInputSize);
            if (size != null)
            {
                args.Add("--input-size");
                args.Add(size.Item1.ToString());
                args.Add(size.Item2.ToString());
            }

            var outSize = ParseOutputSize(config.AlgorithmOutputSize);
            if (outSize != null)
            {
                args.Add("--out-size");
                args.Add(outSize.Item1.ToString());
                args.Add(outSize.Item2.ToString());
            }

            var targetFps = ParseFps(config.AlgorithmTargetFps);
            if (targetFps > 0)
            {
                var fpsText = targetFps.ToString("0.###", CultureInfo.InvariantCulture);
                args.Add("--cap-fps");
                args.Add(fpsText);
                args.Add("--fps");
                args.Add(fpsText);
            }

            var ffmpeg = (config.AlgorithmFfmpegPath ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(ffmpeg))
            {
                var resolved = ResolveExecutable(ffmpeg, ffmpeg);
                if (!RequiresFileCheck(resolved) || File.Exists(resolved))
                {
                    args.Add("--ffmpeg");
                    args.Add(Quote(resolved));
                }
            }

            return string.Join(" ", args);
        }

        private static Tuple<int, int> ParseInputSize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var parts = value.Split(new[] { ',', 'x', 'X', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;

            if (int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int w))
            {
                if (h > 0 && w > 0) return Tuple.Create(h, w);
            }
            return null;
        }

        private static Tuple<int, int> ParseOutputSize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var parts = value.Split(new[] { ',', 'x', 'X', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;

            if (int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
            {
                if (w > 0 && h > 0) return Tuple.Create(w, h);
            }
            return null;
        }

        private static double ParseFps(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            if (double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var fps))
            {
                if (fps > 0) return fps;
            }
            if (double.TryParse(value.Trim(), out fps))
            {
                if (fps > 0) return fps;
            }
            return 0;
        }

        private static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            var p = path.Trim();
            if (Path.IsPathRooted(p)) return p;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidate = Path.GetFullPath(Path.Combine(baseDir, p));
            if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate;

            var current = baseDir;
            for (int i = 0; i < 6; i++)
            {
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                var alt = Path.GetFullPath(Path.Combine(parent.FullName, p));
                if (File.Exists(alt) || Directory.Exists(alt)) return alt;
                current = parent.FullName;
            }

            return candidate;
        }

        private static void TryAutoRepairPaths(AppConfig config)
        {
            try
            {
                var root = FindEdgeYoloRoot(AppDomain.CurrentDomain.BaseDirectory);
                if (string.IsNullOrWhiteSpace(root))
                {
                    return;
                }

                var changed = false;

                var resolvedScript = ResolvePath(config.AlgorithmScriptPath);
                if (string.IsNullOrWhiteSpace(resolvedScript) || !File.Exists(resolvedScript))
                {
                    var preferred = Path.Combine(root, "stream_infer_local.py");
                    if (File.Exists(preferred))
                    {
                        config.AlgorithmScriptPath = preferred;
                        changed = true;
                    }
                    else
                    {
                        var candidate = Path.Combine(root, "stream_detect.py");
                        if (File.Exists(candidate))
                        {
                            config.AlgorithmScriptPath = candidate;
                            changed = true;
                        }
                    }
                }

                var resolvedWeights = ResolvePath(config.AlgorithmWeightsPath);
                if (string.IsNullOrWhiteSpace(resolvedWeights) || !File.Exists(resolvedWeights))
                {
                    var candidate = Path.Combine(root, "output", "train", "edgeyolo", "best.pth");
                    if (!File.Exists(candidate))
                    {
                        var alt = Path.Combine(root, "output", "ir_best.pth");
                        if (File.Exists(alt))
                        {
                            candidate = alt;
                        }
                    }
                    if (File.Exists(candidate))
                    {
                        config.AlgorithmWeightsPath = candidate;
                        changed = true;
                    }
                }

                var resolvedPython = ResolvePath(config.AlgorithmPythonPath);
                if (string.IsNullOrWhiteSpace(resolvedPython) || !File.Exists(resolvedPython))
                {
                    var runtimeCandidate = ResolvePath(Path.Combine("runtime", "python", "python.exe"));
                    if (File.Exists(runtimeCandidate))
                    {
                        config.AlgorithmPythonPath = runtimeCandidate;
                        changed = true;
                    }
                    else
                    {
                        var candidate = Path.Combine(root, ".venv", "Scripts", "python.exe");
                        if (File.Exists(candidate))
                        {
                            config.AlgorithmPythonPath = candidate;
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    config.Save();
                    Logger.Info("已自动修复算法路径配置");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("自动修复算法路径失败", ex);
            }
        }

        private static string FindEdgeYoloRoot(string startDir)
        {
            if (string.IsNullOrWhiteSpace(startDir)) return string.Empty;

            var current = startDir;
            for (int i = 0; i < 8; i++)
            {
                var candidate = Path.Combine(current, "edgeyolo-main", "edgeyolo-main");
                if (File.Exists(Path.Combine(candidate, "stream_infer_local.py")) ||
                    File.Exists(Path.Combine(candidate, "stream_detect.py")))
                {
                    return candidate;
                }

                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            return string.Empty;
        }

        private static string ResolveExecutable(string path, string defaultValue)
        {
            var candidate = string.IsNullOrWhiteSpace(path) ? defaultValue : path.Trim();
            if (RequiresFileCheck(candidate))
            {
                return ResolvePath(candidate);
            }
            return candidate;
        }

        private static bool RequiresFileCheck(string path)
        {
            return path.IndexOf(Path.DirectorySeparatorChar) >= 0 || path.IndexOf(Path.AltDirectorySeparatorChar) >= 0 || path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStreamSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) return false;
            var s = source.Trim();
            if (s.IndexOf("://", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            return int.TryParse(s, out _);
        }

        private static string Quote(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "\"\"";
            return value.Contains(" ") ? $"\"{value}\"" : value;
        }

        private void OnStatusChanged(bool running, string message)
        {
            StatusChanged?.Invoke(this, new AlgorithmStatusEventArgs(running, message));
        }
    }

    public class AlgorithmStatusEventArgs : EventArgs
    {
        public bool IsRunning { get; }
        public string Message { get; }

        public AlgorithmStatusEventArgs(bool isRunning, string message)
        {
            IsRunning = isRunning;
            Message = message ?? string.Empty;
        }
    }
}
