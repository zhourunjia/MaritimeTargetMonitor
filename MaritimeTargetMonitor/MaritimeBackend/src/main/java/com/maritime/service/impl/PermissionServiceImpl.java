package com.maritime.service.impl;

import com.maritime.model.Menu;
import com.maritime.service.PermissionService;
import com.maritime.service.UserService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;

@Service
public class PermissionServiceImpl implements PermissionService {

    @Autowired
    private UserService userService;

    @Override
    public List<Menu> getMenuListByUser(String username) {
        // 获取用户信息
        com.maritime.model.User user = userService.getUserByUsername(username);
        String role = user != null ? user.getRole() : "user";

        // 根据角色返回不同的菜单树
        if ("admin".equals(role)) {
            // 管理员角色返回全菜单
            return getFullMenuList();
        } else {
            // 其他角色返回相应的菜单
            return getUserMenuList();
        }
    }

    /**
     * 获取全菜单列表（管理员角色）
     * @return 全菜单树
     */
    private List<Menu> getFullMenuList() {
        List<Menu> menuList = new ArrayList<>();

        // 首页
        Menu home = new Menu();
        home.setCode("MainPage");
        home.setTitle("首页");
        home.setPath("/main");
        home.setIcon("home");
        home.setOrder(1);
        menuList.add(home);

        // 算法选择
        Menu algSelect = new Menu();
        algSelect.setCode("AlgSelect");
        algSelect.setTitle("算法选择");
        algSelect.setPath("/alg-select");
        algSelect.setIcon("algorithm");
        algSelect.setOrder(2);
        menuList.add(algSelect);

        // 视频监控
        Menu video = new Menu();
        video.setCode("VideoMonitor");
        video.setTitle("视频监控");
        video.setPath("/video");
        video.setIcon("video");
        video.setOrder(3);

        // 视频监控子菜单
        List<Menu> videoChildren = new ArrayList<>();
        
        Menu visualVideo = new Menu();
        visualVideo.setCode("VisualVideo");
        visualVideo.setTitle("可视化视频");
        visualVideo.setPath("/video/visual");
        visualVideo.setIcon("eye");
        visualVideo.setOrder(1);
        videoChildren.add(visualVideo);

        Menu thermalVideo = new Menu();
        thermalVideo.setCode("ThermalVideo");
        thermalVideo.setTitle("热成像视频");
        thermalVideo.setPath("/video/thermal");
        thermalVideo.setIcon("temperature");
        thermalVideo.setOrder(2);
        videoChildren.add(thermalVideo);

        video.setChildren(videoChildren);
        menuList.add(video);

        // 历史记录
        Menu history = new Menu();
        history.setCode("History");
        history.setTitle("历史记录");
        history.setPath("/history");
        history.setIcon("history");
        history.setOrder(4);

        // 历史记录子菜单
        List<Menu> historyChildren = new ArrayList<>();

        Menu robotHistory = new Menu();
        robotHistory.setCode("RobotHistory");
        robotHistory.setTitle("机器人历史");
        robotHistory.setPath("/history/robot");
        robotHistory.setIcon("robot");
        robotHistory.setOrder(1);
        historyChildren.add(robotHistory);

        Menu alarmLog = new Menu();
        alarmLog.setCode("AlarmLog");
        alarmLog.setTitle("报警日志");
        alarmLog.setPath("/history/alarm");
        alarmLog.setIcon("bell");
        alarmLog.setOrder(2);
        historyChildren.add(alarmLog);

        Menu robotRunLog = new Menu();
        robotRunLog.setCode("RobotRunLog");
        robotRunLog.setTitle("机器人运行日志");
        robotRunLog.setPath("/history/robot-run");
        robotRunLog.setIcon("running");
        robotRunLog.setOrder(3);
        historyChildren.add(robotRunLog);

        Menu enviromentLog = new Menu();
        enviromentLog.setCode("EnviromentLog");
        enviromentLog.setTitle("环境日志");
        enviromentLog.setPath("/history/enviroment");
        enviromentLog.setIcon("environment");
        enviromentLog.setOrder(4);
        historyChildren.add(enviromentLog);

        Menu videoLog = new Menu();
        videoLog.setCode("VideoLog");
        videoLog.setTitle("视频日志");
        videoLog.setPath("/history/video");
        videoLog.setIcon("camera");
        videoLog.setOrder(5);
        historyChildren.add(videoLog);

        history.setChildren(historyChildren);
        menuList.add(history);

        // 系统设置
        Menu system = new Menu();
        system.setCode("System");
        system.setTitle("系统设置");
        system.setPath("/system");
        system.setIcon("settings");
        system.setOrder(5);

        // 系统设置子菜单
        List<Menu> systemChildren = new ArrayList<>();

        Menu systemRoute = new Menu();
        systemRoute.setCode("SystemRoute");
        systemRoute.setTitle("系统路由");
        systemRoute.setPath("/system/route");
        systemRoute.setIcon("route");
        systemRoute.setOrder(1);
        systemChildren.add(systemRoute);

        Menu systemManual = new Menu();
        systemManual.setCode("SystemManual");
        systemManual.setTitle("系统手册");
        systemManual.setPath("/system/manual");
        systemManual.setIcon("book");
        systemManual.setOrder(2);
        systemChildren.add(systemManual);

        system.setChildren(systemChildren);
        menuList.add(system);

        // 帮助
        Menu help = new Menu();
        help.setCode("Help");
        help.setTitle("帮助");
        help.setPath("/help");
        help.setIcon("help");
        help.setOrder(6);
        menuList.add(help);

        return menuList;
    }

    /**
     * 获取用户菜单列表（普通用户角色）
     * @return 用户菜单树
     */
    private List<Menu> getUserMenuList() {
        List<Menu> menuList = new ArrayList<>();

        // 首页
        Menu home = new Menu();
        home.setCode("MainPage");
        home.setTitle("首页");
        home.setPath("/main");
        home.setIcon("home");
        home.setOrder(1);
        menuList.add(home);

        // 视频监控
        Menu video = new Menu();
        video.setCode("VideoMonitor");
        video.setTitle("视频监控");
        video.setPath("/video");
        video.setIcon("video");
        video.setOrder(2);

        // 视频监控子菜单
        List<Menu> videoChildren = new ArrayList<>();
        
        Menu visualVideo = new Menu();
        visualVideo.setCode("VisualVideo");
        visualVideo.setTitle("可视化视频");
        visualVideo.setPath("/video/visual");
        visualVideo.setIcon("eye");
        visualVideo.setOrder(1);
        videoChildren.add(visualVideo);

        Menu thermalVideo = new Menu();
        thermalVideo.setCode("ThermalVideo");
        thermalVideo.setTitle("热成像视频");
        thermalVideo.setPath("/video/thermal");
        thermalVideo.setIcon("temperature");
        thermalVideo.setOrder(2);
        videoChildren.add(thermalVideo);

        video.setChildren(videoChildren);
        menuList.add(video);

        // 历史记录
        Menu history = new Menu();
        history.setCode("History");
        history.setTitle("历史记录");
        history.setPath("/history");
        history.setIcon("history");
        history.setOrder(3);

        // 历史记录子菜单
        List<Menu> historyChildren = new ArrayList<>();

        Menu robotHistory = new Menu();
        robotHistory.setCode("RobotHistory");
        robotHistory.setTitle("机器人历史");
        robotHistory.setPath("/history/robot");
        robotHistory.setIcon("robot");
        robotHistory.setOrder(1);
        historyChildren.add(robotHistory);

        Menu alarmLog = new Menu();
        alarmLog.setCode("AlarmLog");
        alarmLog.setTitle("报警日志");
        alarmLog.setPath("/history/alarm");
        alarmLog.setIcon("bell");
        alarmLog.setOrder(2);
        historyChildren.add(alarmLog);

        history.setChildren(historyChildren);
        menuList.add(history);

        // 帮助
        Menu help = new Menu();
        help.setCode("Help");
        help.setTitle("帮助");
        help.setPath("/help");
        help.setIcon("help");
        help.setOrder(4);
        menuList.add(help);

        return menuList;
    }

}
