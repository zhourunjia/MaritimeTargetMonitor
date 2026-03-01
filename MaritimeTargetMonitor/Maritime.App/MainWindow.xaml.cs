using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Maritime.App.Pages;
using Maritime.App.Views;
using Maritime.Core.Config;
using Maritime.Core.Logging;
using Maritime.Core.Models;
using Maritime.Infrastructure.Services;

namespace Maritime.App
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer _healthCheckTimer;
        private AppConfig _config;
        private ApiClient _apiClient;
        private OnlineMenuService _onlineMenuService;
        private string _token;
        private string _username;
        private string _role;
        private Button _activeMenuButton;

        public MainWindow(string token = null, string username = "admin", string role = "管理员")
        {
            InitializeComponent();
            _token = token;
            _username = username;
            _role = role;
            InitializeConfig();
            InitializeApiClient();
            InitializeOnlineMenuService();
            InitializeHealthCheckTimer();
            InitializeMainFrame();
            LoadMenuAsync();
        }

        private void InitializeOnlineMenuService()
        {
            _onlineMenuService = new OnlineMenuService(_config, _apiClient);
        }

        private void InitializeHealthCheckTimer()
        {
            // 初始化健康检查定时器，每10秒执行一次
            _healthCheckTimer = new DispatcherTimer();
            _healthCheckTimer.Interval = TimeSpan.FromSeconds(10);
            _healthCheckTimer.Tick += async (sender, e) =>
            {
                await CheckServerHealthAsync();
            };
            _healthCheckTimer.Start();
        }

        private async Task CheckServerHealthAsync()
        {
            // 只在在线模式且有token时执行健康检查
            if (_config.EnableServer && !string.IsNullOrEmpty(_token))
            {
                try
                {
                    // 调用服务器健康检查接口
                    await _apiClient.GetAsync<object>("app/user/health");
                }
                catch (ApiException ex)
                {
                    Logger.Warn("服务器健康检查失败", ex);
                    
                    // 检查是否是token失效
                    if (ex.UserFriendlyMessage.Contains("未认证") || ex.UserFriendlyMessage.Contains("token"))
                    {
                        // token失效，提示重新登录
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (MessageBox.Show("登录已过期，请重新登录", "登录过期", MessageBoxButton.OK, MessageBoxImage.Information) == MessageBoxResult.OK)
                            {
                                // 关闭当前窗口，重新显示登录窗口
                                Close();
                                
                                // 显示登录窗口
                                LoginWindow loginWindow = new LoginWindow();
                                bool? loginResult = loginWindow.ShowDialog();
                                
                                if (loginResult == true && loginWindow.IsLoggedIn)
                                {
                                    // 登录成功，显示新的主窗口
                                    MainWindow mainWindow = new MainWindow(loginWindow.Token, loginWindow.Username, loginWindow.Role);
                                    mainWindow.Show();
                                }
                                else
                                {
                                    // 登录失败或取消，退出应用
                                    try
                                    {
                                        AlgorithmProcessService.Instance.Stop();
                                    }
                                    catch
                                    {
                                        // ignore
                                    }
                                    Application.Current.Shutdown();
                                }
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("服务器健康检查失败", ex);
                }
            }
        }

        private void InitializeConfig()
        {
            _config = AppConfig.Load();
        }

        private void InitializeApiClient()
        {
            _apiClient = new ApiClient(_config);
            if (!string.IsNullOrEmpty(_token))
            {
                _apiClient.SetToken(_token);
            }
        }

        private void InitializeMainFrame()
        {
            // 默认显示首页
            MainFrame.Navigate(new MainPage());
            if (btnMainPage != null)
            {
                SetActiveMenuButton(btnMainPage);
            }
        }

        private async Task LoadMenuAsync()
        {
            try
            {
                var menuItems = await _onlineMenuService.GetOnlineMenuAsync();
                if (menuItems != null && menuItems.Count > 0)
                {
                    BuildDynamicMenu(menuItems);
                }
                else
                {
                    ShowDefaultMenu();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("加载菜单失败", ex);
                ShowDefaultMenu();
            }
        }

        private void BuildDynamicMenu(List<MenuItemModel> menuItems)
        {
            try
            {
                var leftPanel = FindName("LeftPanel") as StackPanel;
                if (leftPanel == null)
                {
                    Logger.Warn("未找到LeftPanel控件");
                    return;
                }

                leftPanel.Children.Clear();

                foreach (var menuItem in menuItems)
                {
                    if (menuItem.Children != null && menuItem.Children.Count > 0)
                    {
                        var expander = new Expander
                        {
                            Header = menuItem.Title,
                            Margin = new Thickness(12, 10, 12, 0)
                        };
                        if (Application.Current.Resources.Contains("MenuExpanderStyle"))
                        {
                            expander.Style = (Style)Application.Current.Resources["MenuExpanderStyle"];
                        }

                        var stackPanel = new StackPanel();
                        foreach (var childMenuItem in menuItem.Children)
                        {
                            var button = CreateMenuButton(childMenuItem, true);
                            stackPanel.Children.Add(button);
                        }

                        expander.Content = stackPanel;
                        leftPanel.Children.Add(expander);
                    }
                    else
                    {
                        var button = CreateMenuButton(menuItem, false);
                        leftPanel.Children.Add(button);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("构建动态菜单失败", ex);
                ShowDefaultMenu();
            }
        }

        private Button CreateMenuButton(MenuItemModel menuItem, bool isChild)
        {
            var button = new Button
            {
                Content = menuItem.Title,
                Tag = menuItem
            };

            var styleKey = isChild ? "MenuSubButtonStyle" : "MenuButtonStyle";
            if (Application.Current.Resources.Contains(styleKey))
            {
                button.Style = (Style)Application.Current.Resources[styleKey];
            }

            if (menuItem.IsMapped)
            {
                button.Click += MenuButton_Click;
            }
            else
            {
                button.IsEnabled = false;
                button.Click += (sender, e) =>
                {
                    MessageBox.Show($"功能'{menuItem.Title}'尚未实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                };
            }

            return button;
        }

        private void ShowDefaultMenu()
        {
            // 保持现有的默认菜单
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                SetActiveMenuButton(button);
                var menuItem = button.Tag as MenuItemModel;
                if (menuItem != null && menuItem.IsMapped)
                {
                    NavigateToPage(menuItem.PageName);
                }
            }
        }

        private void SetActiveMenuButton(Button button)
        {
            if (_activeMenuButton != null)
            {
                MenuButtonHelper.SetIsActive(_activeMenuButton, false);
            }

            _activeMenuButton = button;
            if (_activeMenuButton != null)
            {
                MenuButtonHelper.SetIsActive(_activeMenuButton, true);
            }
        }

        private void NavigateToPage(string pageName)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainFrame.Navigate(new MainPage());
                    break;
                case "AlgSelect":
                    MainFrame.Navigate(new AlgSelect());
                    break;
                case "AlgConfig":
                    MainFrame.Navigate(new AlgorithmConfig());
                    break;
                case "VisualVideo":
                    MainFrame.Navigate(new VisualVideo());
                    break;
                case "ThermalVideo":
                    MainFrame.Navigate(new ThermalVideo());
                    break;
                case "RobotHistory":
                    MainFrame.Navigate(new RobotHistory());
                    break;
                case "AlarmLog":
                    MainFrame.Navigate(new AlarmLog());
                    break;
                case "RobotRunLog":
                    MainFrame.Navigate(new RobotRunLog());
                    break;
                case "EnviromentLog":
                    MainFrame.Navigate(new EnviromentLog());
                    break;
                case "VideoLog":
                    MainFrame.Navigate(new VideoLog());
                    break;
                case "SystemRoute":
                    MainFrame.Navigate(new SystemRoute());
                    break;
                case "SystemManual":
                    MainFrame.Navigate(new SystemManual());
                    break;
                case "Help":
                    MainFrame.Navigate(new Help());
                    break;
                default:
                    MainFrame.Navigate(new MainPage());
                    break;
            }
        }

        private class MenuItem
        {
            public string code { get; set; }
            public string title { get; set; }
            public List<MenuItem> children { get; set; }
        }
    }
}
