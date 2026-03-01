﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Maritime.Core.Config;
using Maritime.Core.Logging;

namespace Maritime.Infrastructure.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly AppConfig _config;
        private string _token;

        // 错误码映射
        private readonly Dictionary<int, string> _errorCodeMap = new Dictionary<int, string>
        {
            { 10001, "用户未认证" },
            { 10002, "用户名或密码错误" },
            { 10003, "账号已被锁定" },
            { 10004, "账号已过期" },
            { 20001, "权限不足" },
            { 20002, "资源不存在" },
            { 30001, "系统内部错误" },
            { 30002, "数据库操作失败" },
            { 40001, "请求参数错误" },
            { 40002, "请求频率过高" }
        };

        public ApiClient(AppConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            // 鍒濆鍖栧熀纭€鍦板潃
            UpdateBaseAddress();
        }

        // 鏇存柊鍩虹鍦板潃
        public void UpdateBaseAddress()
        {
            string scheme = _config.IsHttp ? "http" : "https";
            _httpClient.BaseAddress = new Uri($"{scheme}://{_config.ServerIP}:{_config.ServerPort}/");
        }

        public void SetToken(string token)
        {
            _token = token;
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync<T>(HttpMethod.Get, endpoint, null, cancellationToken);
        }

        public async Task<T> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync<T>(HttpMethod.Post, endpoint, data, cancellationToken);
        }

        private async Task<T> SendRequestAsync<T>(HttpMethod method, string endpoint, object data, CancellationToken cancellationToken)
        {
            // 妫€鏌ユ槸鍚﹀惎鐢ㄦ湇鍔″櫒杩炴帴
            if (!_config.EnableServer)
            {
                throw new ApiException("离线模式", "服务器连接已禁用");
            }

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(method, endpoint);
                
                // 添加请求体
                if (data != null)
                {
                    string json = JsonConvert.SerializeObject(data);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                // 发送请求
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                
                // 璇诲彇鍝嶅簲
                string responseContent = await response.Content.ReadAsStringAsync();
                
                // 瑙ｆ瀽SResult
                var sResult = JsonConvert.DeserializeObject<SResult<T>>(responseContent);
                
                if (sResult.Code == 10000)
                {
                    return sResult.Data;
                }
                else
                {
                    string errorMessage = GetUserFriendlyErrorMessage(sResult.Code, sResult.Message);
                    throw new ApiException($"API璇锋眰澶辫触: {errorMessage}", sResult.Message);
                }
            }
            catch (TaskCanceledException ex)
            {
                Logger.Error($"请求超时: {endpoint}", ex);
                throw new ApiException("请求超时", "服务器响应超时，请稍后再试");
            }
            catch (OperationCanceledException)
            {
                throw new ApiException("请求已取消", "用户取消了请求");
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"网络请求失败: {endpoint}", ex);
                throw new ApiException("网络连接失败", "无法连接到服务器，请检查网络设置");
            }
            catch (Exception ex)
            {
                Logger.Error($"API璇锋眰澶辫触: {endpoint}", ex);
                // 妫€鏌ユ槸鍚﹀凡缁忔槸ApiException
                if (ex is ApiException)
                {
                    throw;
                }
                throw new ApiException("未知错误", "发生未知错误，请稍后再试");
            }
        }

        // 获取用户友好的错误信息
        private string GetUserFriendlyErrorMessage(int code, string originalMessage)
        {
            if (_errorCodeMap.TryGetValue(code, out string errorMessage))
            {
                return errorMessage;
            }
            return originalMessage;
        }

        // SResult鍝嶅簲鏍煎紡
        private class SResult<T>
        {
            public int Code { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }
        }
    }

    // API异常类
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
