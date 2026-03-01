using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Maritime.Core.Config;
using Maritime.Core.Logging;
using Maritime.Infrastructure.Services;

namespace Maritime.App.Views
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly AppConfig _config;
        private readonly ApiClient _apiClient;

        public bool IsLoggedIn { get; private set; }
        public string Token { get; private set; }
        public string Username { get; private set; }
        public string Role { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            
            // 加载配置
            _config = AppConfig.Load();
            toggleServer.IsChecked = _config.EnableServer;
            UpdateServerStatus();
            
            // 初始化API客户端
            _apiClient = new ApiClient(_config);
            
            // 默认用户名
            txtUsername.Text = "admin";
            
            // 根据配置更新登录入口状态
            UpdateLoginEntryStatus();
        }

        private void UpdateServerStatus()
        {
            bool enableServer = toggleServer.IsChecked ?? false;
            lblServerStatus.Text = enableServer ? "在线模式" : "离线模式";
            _config.EnableServer = enableServer;
            
            // 更新登录入口状态
            UpdateLoginEntryStatus();
        }

        private void UpdateLoginEntryStatus()
        {
            bool enableServer = toggleServer.IsChecked ?? false;
            
            // 离线模式下禁用登录相关控件
            if (!enableServer)
            {
                txtUsername.IsEnabled = false;
                txtPassword.IsEnabled = false;
                btnLogin.Content = "进入系统";
                lblError.Text = "离线模式：直接进入系统";
            }
            else
            {
                txtUsername.IsEnabled = true;
                txtPassword.IsEnabled = true;
                btnLogin.Content = "登录";
                lblError.Text = "";
            }
        }

        private void toggleServer_Checked(object sender, RoutedEventArgs e)
        {
            UpdateServerStatus();
        }

        private void toggleServer_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateServerStatus();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            // 离线模式直接登录
            if (!_config.EnableServer)
            {
                IsLoggedIn = true;
                Username = "offline";
                Role = "user";
                DialogResult = true;
                Close();
                return;
            }

            // 在线模式需要验证用户名和密码
            if (string.IsNullOrEmpty(username))
            {
                lblError.Text = "请输入用户名";
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                lblError.Text = "请输入密码";
                return;
            }

            try
            {
                lblError.Text = "正在登录...";
                btnLogin.IsEnabled = false;

                // MD5加密密码
                string md5Password = GetMD5Hash(password);

                // 登录请求
                var loginRequest = new
                {
                    username = username,
                    password = md5Password
                };

                var response = await _apiClient.PostAsync<LoginResponse>("app/user/login", loginRequest);

                if (response != null && !string.IsNullOrEmpty(response.token))
                {
                    Token = response.token;
                    Username = response.user?.username ?? username;
                    Role = response.user?.role ?? "user";
                    _apiClient.SetToken(Token);
                    IsLoggedIn = true;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    lblError.Text = "登录失败：无效的响应";
                }
            }
            catch (ApiException ex)
            {
                Logger.Error("登录失败", ex);
                lblError.Text = $"登录失败：{ex.UserFriendlyMessage}";
            }
            catch (Exception ex)
            {
                Logger.Error("登录失败", ex);
                lblError.Text = $"登录失败：{ex.Message}";
            }
            finally
            {
                btnLogin.IsEnabled = true;
            }
        }

        private string GetMD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private class LoginResponse
        {
            public string token { get; set; }
            public UserInfo user { get; set; }
        }

        private class UserInfo
        {
            public string username { get; set; }
            public string role { get; set; }
        }
    }
}
