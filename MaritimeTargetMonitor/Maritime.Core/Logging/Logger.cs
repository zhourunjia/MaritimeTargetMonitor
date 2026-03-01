using System;
using System.IO;
using System.Text;
using Maritime.Core.Config;

namespace Maritime.Core.Logging
{
    public class Logger
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly object _lock = new object();

        static Logger()
        {
            // 确保日志目录存在
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
            
            // 清理过期日志
            CleanupOldLogs();
        }

        public static void Info(string message)
        {
            WriteLog("INFO", message, GetInfoLogFile());
        }

        public static void Warn(string message, Exception ex = null)
        {
            if (ex != null)
            {
                message += $"\n寮傚父淇℃伅: {ex.Message}\n鍫嗘爤璺熻釜: {ex.StackTrace}";
            }
            WriteLog("WARNING", message, GetErrorLogFile());
        }

        public static void Warning(string message)
        {
            Warn(message);
        }

        public static void Error(string message, Exception ex = null)
        {
            if (ex != null)
            {
                message += $"\n异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}";
            }
            WriteLog("ERROR", message, GetErrorLogFile());
        }

        public static void Debug(string message)
        {
            WriteLog("DEBUG", message, GetInfoLogFile());
        }

        private static void WriteLog(string level, string message, string logFile)
        {
            lock (_lock)
            {
                try
                {
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                    
                    // 脱敏处理
                    logEntry = MaskSensitiveInfo(logEntry);
                    
                    File.AppendAllText(logFile, logEntry + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // 日志写入失败，忽略错误
                }
            }
        }

        private static string GetInfoLogFile()
        {
            return Path.Combine(LogDirectory, $"info_{DateTime.Now:yyyyMMdd}.txt");
        }

        private static string GetErrorLogFile()
        {
            return Path.Combine(LogDirectory, $"error_{DateTime.Now:yyyyMMdd}.txt");
        }

        private static void CleanupOldLogs()
        {
            try
            {
                // 加载配置获取保留天数
                AppConfig config = AppConfig.Load();
                int keepDays = config.KeepLogDays;
                
                if (keepDays <= 0)
                {
                    keepDays = 30; // 默认保留30天
                }

                DateTime cutoffDate = DateTime.Now.AddDays(-keepDays);
                string[] logFiles = Directory.GetFiles(LogDirectory, "*.txt");

                foreach (string file in logFiles)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // 清理失败，忽略错误
            }
        }

        private static string MaskSensitiveInfo(string message)
        {
            // 脱敏处理，替换敏感信息
            message = System.Text.RegularExpressions.Regex.Replace(message, @"(?i)password=([^&\s]+)", "password=***");
            message = System.Text.RegularExpressions.Regex.Replace(message, @"(?i)token=([^&\s]+)", "token=***");
            message = System.Text.RegularExpressions.Regex.Replace(message, @"(?i)Authorization: Bearer\s+([^\s]+)", "Authorization: Bearer ***");
            return message;
        }
    }
}
