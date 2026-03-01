using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Linq;
using Maritime.Core.Config;

namespace Maritime.Core.Logging
{
    public class DiagnosticExporter
    {
        private const int DefaultRecentMinutes = 30; // 默认导出最近30分钟的日志

        public static string ExportDiagnostics()
        {
            return ExportDiagnostics(DefaultRecentMinutes);
        }

        public static string ExportDiagnostics(int recentMinutes)
        {
            try
            {
                string exportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Diagnostics");
                if (!Directory.Exists(exportPath))
                {
                    Directory.CreateDirectory(exportPath);
                }

                string fileName = Path.Combine(exportPath, $"diagnostic_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                StringBuilder sb = new StringBuilder();

                // 系统信息
                sb.AppendLine("=== 系统信息 ===");
                sb.AppendLine($"操作系统: {Environment.OSVersion.VersionString}");
                sb.AppendLine($".NET版本: {Environment.Version}");
                sb.AppendLine($"应用路径: {AppDomain.CurrentDomain.BaseDirectory}");
                sb.AppendLine($"当前时间: {DateTime.Now}");
                sb.AppendLine();

                // 应用信息
                sb.AppendLine("=== 应用信息 ===");
                var assembly = Assembly.GetExecutingAssembly();
                sb.AppendLine($"应用版本: {assembly.GetName().Version}");
                sb.AppendLine($"程序集名称: {assembly.GetName().Name}");
                sb.AppendLine();

                // 自检结果
                sb.AppendLine("=== 自检结果 ===");
                try
                {
                    if (StartupValidator.Validate(out string errorMessage))
                    {
                        sb.AppendLine("✅ 所有自检项目通过");
                    }
                    else
                    {
                        sb.AppendLine($"❌ 自检失败: {errorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"自检过程错误: {ex.Message}");
                }
                sb.AppendLine();

                // 配置信息
                sb.AppendLine("=== 配置信息 ===");
                try
                {
                    var config = AppConfig.Load();
                    sb.AppendLine($"ServerIP: {MaskIpAddress(config.ServerIP)}");
                    sb.AppendLine($"ServerPort: {config.ServerPort}");
                    sb.AppendLine($"IsHttp: {config.IsHttp}");
                    sb.AppendLine($"KeepDBDays: {config.KeepDBDays}");
                    sb.AppendLine($"KeepLogDays: {config.KeepLogDays}");
                    sb.AppendLine($"EnableServer: {config.EnableServer}");
                    sb.AppendLine($"Tx2UsbCameraIp: {MaskIpAddress(config.Tx2UsbCameraIp)}");
                    sb.AppendLine($"Tx2UsbCameraPort: {config.Tx2UsbCameraPort}");
                    sb.AppendLine($"Tx2ThermalCameraIp: {MaskIpAddress(config.Tx2ThermalCameraIp)}");
                    sb.AppendLine($"Tx2ThermalCameraPort: {config.Tx2ThermalCameraPort}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"配置读取失败: {ex.Message}");
                }
                sb.AppendLine();

                // 目录信息
                sb.AppendLine("=== 目录信息 ===");
                string[] directories = { "Logs", "Files", "Images", "Videos", "VLC", "SDKs", "Diagnostics" };
                foreach (string dir in directories)
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
                    bool exists = Directory.Exists(path);
                    sb.AppendLine($"{dir}: {(exists ? "存在" : "不存在")}");
                    if (exists)
                    {
                        try
                        {
                            bool writable = IsDirectoryWritable(path);
                            sb.AppendLine($"  可写: {writable}");
                        }
                        catch
                        {
                            sb.AppendLine("  可写: 未知");
                        }
                    }
                }
                sb.AppendLine();

                // 文件信息
                sb.AppendLine("=== 文件信息 ===");
                string[] files = { "sqlite3.dll", "VLC/libvlc.dll", "SDKs/HCNetSDK.dll", "SDKs/PlayCtrl.dll", "SDKs/hlog.dll" };
                foreach (string file in files)
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
                    bool exists = File.Exists(path);
                    sb.AppendLine($"{file}: {(exists ? "存在" : "不存在")}");
                }
                sb.AppendLine();

                // 最近N分钟日志
                sb.AppendLine($"=== 最近{recentMinutes}分钟日志 ===");
                try
                {
                    string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                    if (Directory.Exists(logDir))
                    {
                        DateTime cutoffTime = DateTime.Now.AddMinutes(-recentMinutes);
                        var logFiles = Directory.GetFiles(logDir, "*.txt")
                            .Where(f => File.GetLastWriteTime(f) >= cutoffTime)
                            .OrderByDescending(f => File.GetLastWriteTime(f));

                        if (logFiles.Any())
                        {
                            foreach (var logFile in logFiles)
                            {
                                sb.AppendLine($"文件: {Path.GetFileName(logFile)}");
                                sb.AppendLine($"修改时间: {File.GetLastWriteTime(logFile)}");
                                try
                                {
                                    var lines = File.ReadAllLines(logFile, Encoding.UTF8);
                                    var recentLines = lines
                                        .Where(line => 
                                        {
                                            // 尝试解析日志时间
                                            if (line.Length > 20)
                                            {
                                                try
                                                {
                                                    string timeStr = line.Substring(1, 19);
                                                    if (DateTime.TryParse(timeStr, out DateTime logTime))
                                                    {
                                                        return logTime >= cutoffTime;
                                                    }
                                                }
                                                catch
                                                {
                                                    // 时间解析失败，包含该行
                                                }
                                            }
                                            return true;
                                        })
                                        .Take(50); // 最多取50行

                                    if (recentLines.Any())
                                    {
                                        foreach (var line in recentLines)
                                        {
                                            sb.AppendLine($"  {line}");
                                        }
                                    }
                                    else
                                    {
                                        sb.AppendLine("  无符合条件的日志行");
                                    }
                                }
                                catch
                                {
                                    sb.AppendLine("  无法读取日志内容");
                                }
                                sb.AppendLine();
                            }
                        }
                        else
                        {
                            sb.AppendLine("无符合条件的日志文件");
                        }
                    }
                    else
                    {
                        sb.AppendLine("日志目录不存在");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"日志读取失败: {ex.Message}");
                }

                // 保存诊断信息
                File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
                return fileName;
            }
            catch (Exception ex)
            {
                Logger.Error("诊断信息导出失败", ex);
                return null;
            }
        }

        private static string MaskIpAddress(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return ipAddress;

            // 脱敏处理IP地址，保留前两段
            var parts = ipAddress.Split('.');
            if (parts.Length == 4)
            {
                return $"{parts[0]}.{parts[1]}.***.***";
            }
            return ipAddress;
        }

        private static bool IsDirectoryWritable(string path)
        {
            try
            {
                string testFile = Path.Combine(path, "test_write.txt");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

