using Microsoft.VisualStudio.TestTools.UnitTesting;
using Maritime.Core.Config;

namespace Maritime.Core.Tests.Config
{
    [TestClass]
    public class ConfigValidatorTests
    {
        [TestMethod]
        public void Validate_ValidConfig_ReturnsTrue()
        {
            var config = new AppConfig
            {
                ServerIP = "192.168.1.1",
                ServerPort = 8080,
                EnableServer = true,
                KeepDBDays = 3,
                KeepLogDays = 15,
                Tx2UsbCameraIp = "192.168.1.100",
                Tx2UsbCameraPort = 5000,
                Tx2ThermalCameraIp = "192.168.1.101",
                Tx2ThermalCameraPort = 5001,
                ThermalCameraWidth = 640,
                ThermalCameraHeight = 480,
                ThermalCameraFps = 30
            };

            bool result = ConfigValidator.Validate(config, out string errorMessage);

            Assert.IsTrue(result);
            Assert.IsEmpty(errorMessage);
        }

        [TestMethod]
        public void Validate_InvalidServerIp_ReturnsFalse()
        {
            var config = new AppConfig
            {
                ServerIP = "invalid-ip", // 无效IP
                ServerPort = 8080,
                KeepDBDays = 3,
                KeepLogDays = 15,
                Tx2UsbCameraIp = "192.168.1.100",
                Tx2UsbCameraPort = 5000,
                Tx2ThermalCameraIp = "192.168.1.101",
                Tx2ThermalCameraPort = 5001,
                ThermalCameraWidth = 640,
                ThermalCameraHeight = 480,
                ThermalCameraFps = 30
            };

            bool result = ConfigValidator.Validate(config, out string errorMessage);

            Assert.IsFalse(result);
            Assert.IsTrue(errorMessage.Contains("服务器IP地址格式无效"));
        }

        [TestMethod]
        public void Validate_InvalidServerPort_ReturnsFalse()
        {
            var config = new AppConfig
            {
                ServerIP = "192.168.1.1",
                ServerPort = 70000, // 无效端口
                KeepDBDays = 3,
                KeepLogDays = 15,
                Tx2UsbCameraIp = "192.168.1.100",
                Tx2UsbCameraPort = 5000,
                Tx2ThermalCameraIp = "192.168.1.101",
                Tx2ThermalCameraPort = 5001,
                ThermalCameraWidth = 640,
                ThermalCameraHeight = 480,
                ThermalCameraFps = 30
            };

            bool result = ConfigValidator.Validate(config, out string errorMessage);

            Assert.IsFalse(result);
            Assert.IsTrue(errorMessage.Contains("服务器端口必须在1-65535之间"));
        }

        [TestMethod]
        public void Validate_InvalidKeepDBDays_ReturnsFalse()
        {
            var config = new AppConfig
            {
                ServerIP = "192.168.1.1",
                ServerPort = 8080,
                KeepDBDays = 10, // 超出范围
                KeepLogDays = 15,
                Tx2UsbCameraIp = "192.168.1.100",
                Tx2UsbCameraPort = 5000,
                Tx2ThermalCameraIp = "192.168.1.101",
                Tx2ThermalCameraPort = 5001,
                ThermalCameraWidth = 640,
                ThermalCameraHeight = 480,
                ThermalCameraFps = 30
            };

            bool result = ConfigValidator.Validate(config, out string errorMessage);

            Assert.IsFalse(result);
            Assert.IsTrue(errorMessage.Contains("数据库保留天数必须在1-5之间"));
        }

        [TestMethod]
        public void Validate_InvalidKeepLogDays_ReturnsFalse()
        {
            var config = new AppConfig
            {
                ServerIP = "192.168.1.1",
                ServerPort = 8080,
                KeepDBDays = 3,
                KeepLogDays = 40, // 超出范围
                Tx2UsbCameraIp = "192.168.1.100",
                Tx2UsbCameraPort = 5000,
                Tx2ThermalCameraIp = "192.168.1.101",
                Tx2ThermalCameraPort = 5001,
                ThermalCameraWidth = 640,
                ThermalCameraHeight = 480,
                ThermalCameraFps = 30
            };

            bool result = ConfigValidator.Validate(config, out string errorMessage);

            Assert.IsFalse(result);
            Assert.IsTrue(errorMessage.Contains("日志保留天数必须在1-30之间"));
        }

        [TestMethod]
        public void Validate_InvalidThermalCameraParams_ReturnsFalse()
        {
            var config = new AppConfig
            {
                ServerIP = "192.168.1.1",
                ServerPort = 8080,
                KeepDBDays = 3,
                KeepLogDays = 15,
                Tx2UsbCameraIp = "192.168.1.100",
                Tx2UsbCameraPort = 5000,
                Tx2ThermalCameraIp = "192.168.1.101",
                Tx2ThermalCameraPort = 5001,
                ThermalCameraWidth = 0, // 无效宽度
                ThermalCameraHeight = 480,
                ThermalCameraFps = 30
            };

            bool result = ConfigValidator.Validate(config, out string errorMessage);

            Assert.IsFalse(result);
            Assert.IsTrue(errorMessage.Contains("热成像相机分辨率必须大于0"));
        }
    }
}
