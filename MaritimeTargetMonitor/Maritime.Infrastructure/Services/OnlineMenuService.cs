using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Maritime.Core.Config;
using Maritime.Core.Logging;
using Maritime.Core.Models;

namespace Maritime.Infrastructure.Services
{
    public class OnlineMenuService
    {
        private readonly ApiClient _apiClient;
        private readonly AppConfig _config;

        private readonly Dictionary<string, string> _pageMapping = new Dictionary<string, string>
        {
            { "MainPage", "MainPage" },
            { "AlgSelect", "AlgSelect" },
            { "AlgConfig", "AlgConfig" },
            { "VisualVideo", "VisualVideo" },
            { "ThermalVideo", "ThermalVideo" },
            { "RobotHistory", "RobotHistory" },
            { "AlarmLog", "AlarmLog" },
            { "RobotRunLog", "RobotRunLog" },
            { "EnviromentLog", "EnviromentLog" },
            { "VideoLog", "VideoLog" },
            { "SystemRoute", "SystemRoute" },
            { "SystemManual", "SystemManual" },
            { "Help", "Help" }
        };

        public OnlineMenuService(AppConfig config, ApiClient apiClient)
        {
            _config = config;
            _apiClient = apiClient;
        }

        public async Task<List<MenuItemModel>> GetOnlineMenuAsync()
        {
            try
            {
                if (!_config.EnableServer)
                {
                    Logger.Info("离线模式，使用默认菜单");
                    return GetDefaultMenu();
                }

                var onlineMenus = await _apiClient.GetAsync<List<OnlineMenu>>("app/permission/list/self");
                if (onlineMenus == null || onlineMenus.Count == 0)
                {
                    Logger.Warn("在线菜单为空，使用默认菜单");
                    return GetDefaultMenu();
                }

                var menuItems = ConvertToMenuItemModels(onlineMenus);
                MapMenuItemsToPages(menuItems);
                
                Logger.Info($"成功加载在线菜单，共{menuItems.Count}个菜单项");
                return menuItems;
            }
            catch (ApiException ex)
            {
                Logger.Error("加载在线菜单失败，降级为默认菜单", ex);
                return GetDefaultMenu();
            }
            catch (Exception ex)
            {
                Logger.Error("加载在线菜单失败，降级为默认菜单", ex);
                return GetDefaultMenu();
            }
        }

        private List<MenuItemModel> ConvertToMenuItemModels(List<OnlineMenu> onlineMenus)
        {
            var menuItems = new List<MenuItemModel>();
            
            foreach (var onlineMenu in onlineMenus)
            {
                var menuItem = new MenuItemModel
                {
                    Code = onlineMenu.code,
                    Title = onlineMenu.title,
                    Path = onlineMenu.path,
                    Icon = onlineMenu.icon,
                    Order = onlineMenu.order ?? 0
                };

                if (onlineMenu.children != null && onlineMenu.children.Count > 0)
                {
                    menuItem.Children = ConvertToMenuItemModels(onlineMenu.children);
                }

                menuItems.Add(menuItem);
            }

            return menuItems.OrderBy(m => m.Order).ToList();
        }

        private void MapMenuItemsToPages(List<MenuItemModel> menuItems)
        {
            foreach (var menuItem in menuItems)
            {
                if (_pageMapping.ContainsKey(menuItem.Code))
                {
                    menuItem.IsMapped = true;
                    menuItem.PageName = _pageMapping[menuItem.Code];
                }
                else
                {
                    menuItem.IsMapped = false;
                    menuItem.PageName = null;
                }

                if (menuItem.Children != null && menuItem.Children.Count > 0)
                {
                    MapMenuItemsToPages(menuItem.Children);
                }
            }
        }

        private List<MenuItemModel> GetDefaultMenu()
        {
            return new List<MenuItemModel>
            {
                new MenuItemModel
                {
                    Code = "MainPage",
                    Title = "首页",
                    Path = "/main",
                    Icon = "home",
                    Order = 1,
                    IsMapped = true,
                    PageName = "MainPage"
                },
                new MenuItemModel
                {
                    Code = "AlgSelect",
                    Title = "算法选择",
                    Path = "/alg-select",
                    Icon = "algorithm",
                    Order = 2,
                    IsMapped = true,
                    PageName = "AlgSelect"
                },
                new MenuItemModel
                {
                    Code = "AlgConfig",
                    Title = "算法参数配置",
                    Path = "/alg-config",
                    Icon = "algorithm-config",
                    Order = 3,
                    IsMapped = true,
                    PageName = "AlgConfig"
                },
                new MenuItemModel
                {
                    Code = "VisualVideo",
                    Title = "可视化视频",
                    Path = "/visual-video",
                    Icon = "video",
                    Order = 3,
                    IsMapped = true,
                    PageName = "VisualVideo"
                },
                new MenuItemModel
                {
                    Code = "ThermalVideo",
                    Title = "热成像视频",
                    Path = "/thermal-video",
                    Icon = "thermal",
                    Order = 4,
                    IsMapped = true,
                    PageName = "ThermalVideo"
                },
                new MenuItemModel
                {
                    Code = "RobotHistory",
                    Title = "无人机历史",
                    Path = "/robot-history",
                    Icon = "history",
                    Order = 5,
                    IsMapped = true,
                    PageName = "RobotHistory"
                },
                new MenuItemModel
                {
                    Code = "AlarmLog",
                    Title = "报警日志",
                    Path = "/alarm-log",
                    Icon = "alarm",
                    Order = 6,
                    IsMapped = true,
                    PageName = "AlarmLog"
                },
                new MenuItemModel
                {
                    Code = "RobotRunLog",
                    Title = "无人机运行日志",
                    Path = "/robot-run-log",
                    Icon = "log",
                    Order = 7,
                    IsMapped = true,
                    PageName = "RobotRunLog"
                },
                new MenuItemModel
                {
                    Code = "EnviromentLog",
                    Title = "环境日志",
                    Path = "/enviroment-log",
                    Icon = "env",
                    Order = 8,
                    IsMapped = true,
                    PageName = "EnviromentLog"
                },
                new MenuItemModel
                {
                    Code = "VideoLog",
                    Title = "视频日志",
                    Path = "/video-log",
                    Icon = "video-log",
                    Order = 9,
                    IsMapped = true,
                    PageName = "VideoLog"
                },
                new MenuItemModel
                {
                    Code = "SystemRoute",
                    Title = "系统轨迹库",
                    Path = "/system-route",
                    Icon = "route",
                    Order = 10,
                    IsMapped = true,
                    PageName = "SystemRoute"
                },
                new MenuItemModel
                {
                    Code = "SystemManual",
                    Title = "系统手册",
                    Path = "/system-manual",
                    Icon = "manual",
                    Order = 11,
                    IsMapped = true,
                    PageName = "SystemManual"
                },
                new MenuItemModel
                {
                    Code = "Help",
                    Title = "说明",
                    Path = "/help",
                    Icon = "help",
                    Order = 12,
                    IsMapped = true,
                    PageName = "Help"
                }
            };
        }

        private class OnlineMenu
        {
            public string code { get; set; }
            public string title { get; set; }
            public string path { get; set; }
            public string icon { get; set; }
            public int? order { get; set; }
            public List<OnlineMenu> children { get; set; }
        }
    }
}
