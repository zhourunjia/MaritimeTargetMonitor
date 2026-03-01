using Microsoft.VisualStudio.TestTools.UnitTesting;
using Maritime.Core.Config;
using System.IO;
using Newtonsoft.Json;

namespace Maritime.Core.Tests.Config
{
    [TestClass]
    public class AppConfigTests
    {
        private string _testConfigPath = "test_config.json";

        [TestCleanup]
        public void TestCleanup()
        {
            // 清理测试文件
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }

        [TestMethod]
        public void Load_ValidConfig_ReturnsConfig()
        {
            // 准备测试配置文件
            var testConfig = new AppConfig
            {
                ServerIP = "192.168.1.1",
                ServerPort = 8080,
                EnableServer = true,
                KeepDBDays = 3,
                KeepLogDays = 15,
                Tx2UsbCameraIp = "192.168.1.100",
                Tx2UsbCameraPort = 5000,
                Tx2ThermalCameraIp = "192.168.1.101",
                Tx2ThermalCameraPort = 5001
            };

            // 保存测试配置
            File.WriteAllText(_testConfigPath, JsonConvert.SerializeObject(testConfig));

            // 临时替换配置文件路径
            var originalPath = typeof(AppConfig).GetField("ConfigFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalValue = originalPath.GetValue(null);
            originalPath.SetValue(null, _testConfigPath);

            try
            {
                // 加载配置
                var config = AppConfig.Load();

                // 验证配置值
                Assert.AreEqual("192.168.1.1", config.ServerIP);
                Assert.AreEqual(8080, config.ServerPort);
                Assert.IsTrue(config.EnableServer);
                Assert.AreEqual(3, config.KeepDBDays);
                Assert.AreEqual(15, config.KeepLogDays);
                Assert.AreEqual("192.168.1.100", config.Tx2UsbCameraIp);
                Assert.AreEqual(5000, config.Tx2UsbCameraPort);
                Assert.AreEqual("192.168.1.101", config.Tx2ThermalCameraIp);
                Assert.AreEqual(5001, config.Tx2ThermalCameraPort);
            }
            finally
            {
                // 恢复原始路径
                originalPath.SetValue(null, originalValue);
            }
        }

        [TestMethod]
        public void Load_InvalidConfig_ReturnsDefault()
        {
            // 准备无效的配置文件
            File.WriteAllText(_testConfigPath, "invalid json");

            // 临时替换配置文件路径
            var originalPath = typeof(AppConfig).GetField("ConfigFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var originalValue = originalPath.GetValue(null);
            originalPath.SetValue(null, _testConfigPath);

            try
            {
                // 加载配置
                var config = AppConfig.Load();

                // 验证返回默认值
                Assert.IsNotNull(config);
            }
            finally
            {
                // 恢复原始路径
                originalPath.SetValue(null, originalValue);
            }
        }

        [TestMethod]
        public void ValidateAndTrim_KeepDBDays_ClipsToRange()
        {
            var config = new AppConfig();

            // 测试边界值
            config.KeepDBDays = 0; // 小于最小值
            config.ValidateAndTrim();
            Assert.AreEqual(1, config.KeepDBDays);

            config.KeepDBDays = 10; // 大于最大值
            config.ValidateAndTrim();
            Assert.AreEqual(5, config.KeepDBDays);

            config.KeepDBDays = 3; // 在范围内
            config.ValidateAndTrim();
            Assert.AreEqual(3, config.KeepDBDays);
        }

        [TestMethod]
        public void ValidateAndTrim_KeepLogDays_ClipsToRange()
        {
            var config = new AppConfig();

            // 测试边界值
            config.KeepLogDays = 0; // 小于最小值
            config.ValidateAndTrim();
            Assert.AreEqual(1, config.KeepLogDays);

            config.KeepLogDays = 40; // 大于最大值
            config.ValidateAndTrim();
            Assert.AreEqual(30, config.KeepLogDays);

            config.KeepLogDays = 15; // 在范围内
            config.ValidateAndTrim();
            Assert.AreEqual(15, config.KeepLogDays);
        }

        [TestMethod]
        public void ValidateAndTrim_Ports_ClipsToRange()
        {
            var config = new AppConfig();

            // 测试服务器端口
            config.ServerPort = 0; // 小于最小值
            config.ValidateAndTrim();
            Assert.AreEqual(1, config.ServerPort);

            config.ServerPort = 70000; // 大于最大值
            config.ValidateAndTrim();
            Assert.AreEqual(65535, config.ServerPort);

            // 测试USB相机端口
            config.Tx2UsbCameraPort = 0;
            config.ValidateAndTrim();
            Assert.AreEqual(1, config.Tx2UsbCameraPort);

            // 测试热成像相机端口
            config.Tx2ThermalCameraPort = 70000;
            config.ValidateAndTrim();
            Assert.AreEqual(65535, config.Tx2ThermalCameraPort);
        }
    }
}
