using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Maritime.Core.Config;
using Maritime.Infrastructure.Services;

namespace Maritime.Tests
{
    [TestClass]
    public class ApiClientTests
    {
        private AppConfig _config;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private HttpClient _httpClient;

        [TestInitialize]
        public void TestInitialize()
        {
            // 创建测试配置
            _config = new AppConfig
            {
                ServerIP = "127.0.0.1",
                ServerPort = 60800,
                IsHttp = true,
                EnableServer = true
            };

            // 创建模拟的HttpMessageHandler
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnData_WhenSResultIsSuccess()
        {
            // 准备测试数据
            var testData = new { Name = "Test", Value = 123 };
            var sResult = new { Code = 10000, Message = "成功", Data = testData };
            var jsonResponse = JsonConvert.SerializeObject(sResult);

            // 设置模拟的HttpResponse
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // 创建ApiClient实例并替换HttpClient
            var apiClient = new ApiClient(_config);
            // 使用反射替换私有字段_httpClient
            var httpClientField = typeof(ApiClient).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpClientField.SetValue(apiClient, _httpClient);

            // 执行测试
            var result = await apiClient.GetAsync<object>("/test");

            // 验证结果
            Assert.IsNotNull(result);
            var resultJson = JsonConvert.SerializeObject(result);
            var testDataJson = JsonConvert.SerializeObject(testData);
            Assert.AreEqual(testDataJson, resultJson);
        }

        [TestMethod]
        [ExpectedException(typeof(ApiException))]
        public async Task GetAsync_ShouldThrowApiException_WhenSResultIsFailure()
        {
            // 准备测试数据
            var sResult = new { Code = 10001, Message = "用户未认证", Data = (object)null };
            var jsonResponse = JsonConvert.SerializeObject(sResult);

            // 设置模拟的HttpResponse
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // 创建ApiClient实例并替换HttpClient
            var apiClient = new ApiClient(_config);
            var httpClientField = typeof(ApiClient).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpClientField.SetValue(apiClient, _httpClient);

            // 执行测试
            await apiClient.GetAsync<object>("/test");
        }

        [TestMethod]
        public void SetToken_ShouldAddAuthorizationHeader_WhenTokenIsProvided()
        {
            // 创建ApiClient实例
            var apiClient = new ApiClient(_config);

            // 设置token
            var testToken = "test-token-123";
            apiClient.SetToken(testToken);

            // 获取HttpClient的Authorization header
            var httpClientField = typeof(ApiClient).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var httpClient = (HttpClient)httpClientField.GetValue(apiClient);
            var authorizationHeader = httpClient.DefaultRequestHeaders.Authorization;

            // 验证结果
            Assert.IsNotNull(authorizationHeader);
            Assert.AreEqual("Bearer", authorizationHeader.Scheme);
            Assert.AreEqual(testToken, authorizationHeader.Parameter);
        }

        [TestMethod]
        public void SetToken_ShouldRemoveAuthorizationHeader_WhenTokenIsNullOrEmpty()
        {
            // 创建ApiClient实例
            var apiClient = new ApiClient(_config);

            // 先设置一个token
            var testToken = "test-token-123";
            apiClient.SetToken(testToken);

            // 然后设置空token
            apiClient.SetToken(null);

            // 获取HttpClient的Authorization header
            var httpClientField = typeof(ApiClient).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var httpClient = (HttpClient)httpClientField.GetValue(apiClient);
            var authorizationHeader = httpClient.DefaultRequestHeaders.Authorization;

            // 验证结果
            Assert.IsNull(authorizationHeader);
        }

        [TestMethod]
        [ExpectedException(typeof(ApiException))]
        public async Task SendRequestAsync_ShouldThrowApiException_WhenEnableServerIsFalse()
        {
            // 修改配置，禁用服务器连接
            _config.EnableServer = false;

            // 创建ApiClient实例
            var apiClient = new ApiClient(_config);

            // 执行测试
            await apiClient.GetAsync<object>("/test");
        }

        [TestMethod]
        [ExpectedException(typeof(ApiException))]
        public async Task SendRequestAsync_ShouldThrowApiException_WhenRequestTimesOut()
        {
            // 设置模拟的HttpResponse，模拟超时
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(async (HttpRequestMessage request, CancellationToken cancellationToken) =>
                {
                    // 模拟长时间延迟
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("{\"Code\": 10000, \"Message\": \"成功\", \"Data\": null}")
                    };
                });

            // 创建ApiClient实例并替换HttpClient
            var apiClient = new ApiClient(_config);
            var httpClientField = typeof(ApiClient).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                Timeout = TimeSpan.FromMilliseconds(100) // 设置很短的超时时间
            };
            httpClientField.SetValue(apiClient, httpClient);

            // 执行测试
            await apiClient.GetAsync<object>("/test");
        }

        [TestMethod]
        public void UpdateBaseAddress_ShouldSetCorrectBaseAddress()
        {
            // 创建ApiClient实例
            var apiClient = new ApiClient(_config);

            // 修改配置
            _config.ServerIP = "192.168.1.1";
            _config.ServerPort = 8080;
            _config.IsHttp = false;

            // 调用UpdateBaseAddress
            apiClient.UpdateBaseAddress();

            // 获取HttpClient的BaseAddress
            var httpClientField = typeof(ApiClient).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var httpClient = (HttpClient)httpClientField.GetValue(apiClient);
            var baseAddress = httpClient.BaseAddress;

            // 验证结果
            Assert.IsNotNull(baseAddress);
            Assert.AreEqual("https", baseAddress.Scheme);
            Assert.AreEqual("192.168.1.1", baseAddress.Host);
            Assert.AreEqual(8080, baseAddress.Port);
        }
    }

    // 用于测试的API异常类
    public class ApiException : Exception
    {
        public string UserFriendlyMessage { get; }

        public ApiException(string userFriendlyMessage, string message) : base(message)
        {
            UserFriendlyMessage = userFriendlyMessage;
        }

        public ApiException(string userFriendlyMessage, string message, Exception innerException) : base(message, innerException)
        {
            UserFriendlyMessage = userFriendlyMessage;
        }
    }
}