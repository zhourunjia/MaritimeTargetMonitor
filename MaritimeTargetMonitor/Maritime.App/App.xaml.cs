﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Maritime.App.Views;
using Maritime.Core.Config;
using Maritime.Core.Logging;

namespace Maritime.App
{
    /// <summary>
    /// App.xaml 鐨勪氦浜掗€昏緫
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);            try
            {
                // 启动校验
                if (!StartupValidator.Validate(out string errorMessage))
                {
                    StartupValidator.ShowErrorAndExit(errorMessage);
                    return;
                }

                // 加载配置
                AppConfig config = AppConfig.Load();
                config.ValidateAndTrim();

                // 直接进入主界面（跳过登录）
                MainWindow mainWindow = new MainWindow(token: null, username: "offline", role: "user");
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Error("应用启动失败", ex);
                MessageBox.Show("应用启动失败，请查看日志。\n" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }}
    }
}


