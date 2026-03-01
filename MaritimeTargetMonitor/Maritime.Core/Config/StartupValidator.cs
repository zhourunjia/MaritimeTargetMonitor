using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Maritime.Core.Logging;
using Newtonsoft.Json;

namespace Maritime.Core.Config
{
    public class StartupValidator
    {
        public static bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine("===== 启动验证开始 =====");

            try
            {
                bool isDevelopment = IsDevelopmentEnvironment();
                if (isDevelopment)
                {
                    logBuilder.AppendLine("开发环境：跳过依赖文件检查，只验证配置文件");
                    if (!CheckConfigJson(out string configError))
                    {
                        errorMessage = configError;
                        logBuilder.AppendLine($"配置文件检查失败：{errorMessage}");
                        LogValidationResult(logBuilder.ToString());
                        return false;
                    }
                    logBuilder.AppendLine("配置文件检查通过");
                }
                else
                {
                    // 1) config.json
                    if (!CheckConfigJson(out string configError))
                    {
                        errorMessage = configError;
                        logBuilder.AppendLine($"配置文件检查失败：{errorMessage}");
                        LogValidationResult(logBuilder.ToString());
                        return false;
                    }
                    logBuilder.AppendLine("配置文件检查通过");

                    // 2) sqlite3.dll（缺失仅警告）
                    if (!CheckFileExists("sqlite3.dll", "SQLite3数据库", out string sqliteError))
                    {
                        logBuilder.AppendLine($"SQLite3未找到（可忽略）：{sqliteError}");
                    }
                    else
                    {
                        logBuilder.AppendLine("SQLite3检查通过");
                    }

                    // 3) VLC
                    if (!CheckVlcDirectory(out string vlcError))
                    {
                        errorMessage = vlcError;
                        logBuilder.AppendLine($"VLC检查失败：{errorMessage}");
                        LogValidationResult(logBuilder.ToString());
                        return false;
                    }
                    logBuilder.AppendLine("VLC检查通过");

                    // 4) SDKs（缺失仅警告）
                    if (!CheckSdksDirectory(out string sdksError))
                    {
                        logBuilder.AppendLine($"SDKs缺失（可忽略）：{sdksError}");
                    }
                    else
                    {
                        logBuilder.AppendLine("SDKs检查通过");
                    }
                }

                // 5) 目录写权限
                if (!CheckDirectoriesWritable(out string dirError))
                {
                    errorMessage = dirError;
                    logBuilder.AppendLine($"目录权限检查失败：{errorMessage}");
                    LogValidationResult(logBuilder.ToString());
                    return false;
                }
                logBuilder.AppendLine("目录权限检查通过");

                logBuilder.AppendLine("===== 启动验证完成：全部通过 =====");
                LogValidationResult(logBuilder.ToString());
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"启动验证异常：{ex.Message}";
                logBuilder.AppendLine($"验证异常：{errorMessage}");
                LogValidationResult(logBuilder.ToString());
                return false;
            }
        }

        private static bool IsDevelopmentEnvironment()
        {
            try
            {
                string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                bool isInBinFolder = assemblyLocation.Contains("\\bin\\");
                bool hasProjectFiles = Directory.Exists(Path.Combine(baseDirectory, "..", "..")) &&
                                       (Directory.GetFiles(Path.Combine(baseDirectory, "..", ".."), "*.csproj").Length > 0 ||
                                        Directory.GetFiles(Path.Combine(baseDirectory, "..", ".."), "*.sln").Length > 0);
                return isInBinFolder || hasProjectFiles;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckConfigJson(out string errorMessage)
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (!File.Exists(configPath))
            {
                errorMessage = "配置文件不存在：config.json\n请确认 config.json 在程序根目录";
                return false;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                JsonConvert.DeserializeObject<AppConfig>(json);
                errorMessage = string.Empty;
                return true;
            }
            catch (JsonException ex)
            {
                errorMessage = $"配置文件格式错误：{ex.Message}\n请检查 config.json 的 JSON 格式";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"配置文件读取错误：{ex.Message}\n请确认 config.json 可访问";
                return false;
            }
        }

        private static bool CheckFileExists(string relativePath, string description, out string errorMessage)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            if (File.Exists(fullPath))
            {
                errorMessage = string.Empty;
                return true;
            }

            errorMessage = $"{description}文件不存在：{relativePath}\n请确认文件位置正确";
            return false;
        }

        private static bool CheckVlcDirectory(out string errorMessage)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "VLC"),
                Path.Combine(baseDir, "libvlc"),
                Path.Combine(baseDir, "libvlc", "win-x64"),
                Path.Combine(baseDir, "libvlc", "win-x86")
            };

            foreach (string candidate in candidates)
            {
                if (!Directory.Exists(candidate)) continue;
                string libvlcPath = Path.Combine(candidate, "libvlc.dll");
                string pluginsPath = Path.Combine(candidate, "plugins");
                if (File.Exists(libvlcPath) && Directory.Exists(pluginsPath))
                {
                    errorMessage = string.Empty;
                    return true;
                }
            }

            errorMessage = "VLC目录未找到或结构不完整。\n需要以下任一结构：\n- VLC\\libvlc.dll + VLC\\plugins\n- libvlc\\win-x64\\libvlc.dll + libvlc\\win-x64\\plugins";
            return false;
        }

        private static bool CheckSdksDirectory(out string errorMessage)
        {
            string sdksPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SDKs");
            if (!Directory.Exists(sdksPath))
            {
                errorMessage = "SDKs目录不存在：SDKs";
                return false;
            }

            string[] requiredFiles = { "HCNetSDK.dll", "PlayCtrl.dll", "hlog.dll" };
            foreach (string file in requiredFiles)
            {
                string filePath = Path.Combine(sdksPath, file);
                if (!File.Exists(filePath))
                {
                    errorMessage = $"SDKs缺少文件：SDKs/{file}";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        private static bool CheckDirectoriesWritable(out string errorMessage)
        {
            string[] dirs =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"),
                Path.GetTempPath()
            };

            string[] dirNames = { "日志", "数据", "临时" };

            for (int i = 0; i < dirs.Length; i++)
            {
                if (!CheckDirectoryWritable(dirs[i], dirNames[i], out errorMessage))
                {
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        private static bool CheckDirectoryWritable(string path, string directoryName, out string errorMessage)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string testFile = Path.Combine(path, "test_write.txt");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                errorMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"{directoryName}目录无写权限：{path}\n错误：{ex.Message}\n请确认程序对该目录有写权限";
                return false;
            }
        }

        private static void LogValidationResult(string logContent)
        {
            Logger.Info(logContent);
        }

        public static void ShowErrorAndExit(string errorMessage)
        {
            string fullMessage = $"启动失败，无法继续运行：\n\n{errorMessage}\n\n请根据提示修复后再启动。";
            MessageBox.Show(fullMessage, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }
    }
}
