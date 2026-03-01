using System;
using System.Text.RegularExpressions;

namespace Maritime.Core.Config
{
    public class ConfigValidator
    {
        public static bool Validate(AppConfig config, out string errorMessage)
        {
            errorMessage = string.Empty;

            // 验证ServerIP格式
            if (!IsValidIpAddress(config.ServerIP))
            {
                errorMessage = "服务器IP地址格式无效";
                return false;
            }

            // 验证ServerPort范围
            if (config.ServerPort < 1 || config.ServerPort > 65535)
            {
                errorMessage = "服务器端口必须在1-65535之间";
                return false;
            }

            // 验证Tx2UsbCameraIp格式
            if (!IsValidIpAddress(config.Tx2UsbCameraIp))
            {
                errorMessage = "TX2 USB相机IP地址格式无效";
                return false;
            }

            // 验证Tx2UsbCameraPort范围
            if (config.Tx2UsbCameraPort < 1 || config.Tx2UsbCameraPort > 65535)
            {
                errorMessage = "TX2 USB相机端口必须在1-65535之间";
                return false;
            }

            // 验证Tx2ThermalCameraIp格式
            if (!IsValidIpAddress(config.Tx2ThermalCameraIp))
            {
                errorMessage = "TX2热成像相机IP地址格式无效";
                return false;
            }

            // 验证Tx2ThermalCameraPort范围
            if (config.Tx2ThermalCameraPort < 1 || config.Tx2ThermalCameraPort > 65535)
            {
                errorMessage = "TX2热成像相机端口必须在1-65535之间";
                return false;
            }

            // 验证KeepDBDays范围
            if (config.KeepDBDays < 1 || config.KeepDBDays > 5)
            {
                errorMessage = "数据库保留天数必须在1-5之间";
                return false;
            }

            // 验证KeepLogDays范围
            if (config.KeepLogDays < 1 || config.KeepLogDays > 30)
            {
                errorMessage = "日志保留天数必须在1-30之间";
                return false;
            }

            // 验证热成像相机参数
            if (config.ThermalCameraWidth <= 0 || config.ThermalCameraHeight <= 0)
            {
                errorMessage = "热成像相机分辨率必须大于0";
                return false;
            }

            if (config.ThermalCameraFps <= 0 || config.ThermalCameraFps > 60)
            {
                errorMessage = "热成像相机帧率必须在1-60之间";
                return false;
            }

            return true;
        }

        private static bool IsValidIpAddress(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return false;

            string pattern = @"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            return Regex.IsMatch(ipAddress, pattern);
        }
    }
}
