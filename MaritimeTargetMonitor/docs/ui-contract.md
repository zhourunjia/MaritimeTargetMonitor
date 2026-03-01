# UI 契约说明

## 1. 顶部状态栏

### 1.1 状态栏字段

| 字段 | 说明 |
|------|------|
| 日期时间 | 显示当前系统日期和时间 |
| 热成像相机 | 显示热成像相机的 IP:Port |
| 可视化相机 | 显示可视化相机的 IP:Port |
| 服务器 | 显示服务器的 IP:Port |
| PLC | 显示 PLC 的 IP:Port |
| 账号/角色 | 显示当前登录用户的账号和角色 |
| 修改密码 | 点击后打开修改密码对话框 |
| 退出按钮 | 点击后退出系统 |

## 2. 左侧菜单

### 2.1 离线默认菜单结构

| 菜单项 | 页面映射 | 说明 |
|--------|----------|------|
| MainPage | 监控首页 | 系统主页面，显示视频监控和控制面板 |
| AlgSelect | 算法选择 | 选择和配置目标检测算法 |
| VisualVideo | 可视化视频 | 显示可见光相机的视频流 |
| ThermalVideo | 热成像视频 | 显示热成像相机的视频流 |
| RobotHistory | 机器人历史 | 查看机器人的历史任务记录 |
| AlarmLog | 报警日志 | 查看系统报警记录 |
| RobotRunLog | 机器人运行日志 | 查看机器人的运行状态日志 |
| EnviromentLog | 环境日志 | 查看环境监测数据日志 |
| VideoLog | 视频日志 | 查看视频录制和事件日志 |
| SystemRoute | 系统轨迹 | 查看和管理系统轨迹 |
| Help | 帮助 | 显示系统帮助文档 |

### 2.2 在线模式菜单获取

- **当 enableServer=true 时**：通过 `/app/permission/list/self` 接口下发菜单，根据用户权限动态生成
- **当 enableServer=false 时**：返回离线默认菜单结构

## 3. 监控首页布局

### 3.1 布局结构

- **左侧**：视频区，显示主视频流（可切换可视化/热成像）
- **右侧上方**："无人机信息"面板，包含连接状态和连接按钮
- **右侧下方**："控制面板"，包含以下控件：
  - 开始巡检按钮
  - 设置按钮
  - 方向盘控件
  - 速度滑块
  - 前进/后退/左转/右转按钮
- **底部**：标签页，包含"实时轨迹"和"查看图片"两个标签

## 4. UI 组件树建议 (WPF)

### 4.1 主窗口组件树

```
MainWindow
├── TopStatusBar (DockPanel, Dock.Top)
│   ├── DateTimeDisplay (TextBlock)
│   ├── CameraStatusList (StackPanel, Orientation=Horizontal)
│   │   ├── ThermalCameraStatus (TextBlock)
│   │   ├── VisualCameraStatus (TextBlock)
│   │   ├── ServerStatus (TextBlock)
│   │   ├── PlcStatus (TextBlock)
│   ├── UserInfo (StackPanel, Orientation=Horizontal)
│   │   ├── UserRoleDisplay (TextBlock)
│   │   ├── ChangePasswordButton (Button)
│   │   ├── ExitButton (Button)
├── MainContent (Grid)
│   ├── LeftMenu (NavigationView, Grid.Column=0)
│   │   ├── NavigationViewItem (MainPage)
│   │   ├── NavigationViewItem (AlgSelect)
│   │   ├── NavigationViewItem (VisualVideo)
│   │   ├── NavigationViewItem (ThermalVideo)
│   │   ├── NavigationViewItem (RobotHistory)
│   │   ├── NavigationViewItem (AlarmLog)
│   │   ├── NavigationViewItem (RobotRunLog)
│   │   ├── NavigationViewItem (EnviromentLog)
│   │   ├── NavigationViewItem (VideoLog)
│   │   ├── NavigationViewItem (SystemRoute)
│   │   ├── NavigationViewItem (Help)
│   ├── ContentArea (Grid, Grid.Column=1)
│   │   ├── MainPageView (当选择MainPage时)
│   │   │   ├── VideoArea (Grid, Grid.Row=0)
│   │   │   │   ├── VideoPlayer (MediaElement)
│   │   │   ├── RightPanel (Grid, Grid.Row=0, Grid.Column=1)
│   │   │   │   ├── DroneInfoPanel (StackPanel, Grid.Row=0)
│   │   │   │   │   ├── ConnectionStatus (TextBlock)
│   │   │   │   │   ├── ConnectButton (Button)
│   │   │   │   ├── ControlPanel (StackPanel, Grid.Row=1)
│   │   │   │   │   ├── StartInspectionButton (Button)
│   │   │   │   │   ├── SettingsButton (Button)
│   │   │   │   │   ├── SteeringWheel (自定义控件)
│   │   │   │   │   ├── SpeedSlider (Slider)
│   │   │   │   │   ├── DirectionButtons (StackPanel, Orientation=Horizontal)
│   │   │   │   │   │   ├── ForwardButton (Button)
│   │   │   │   │   │   ├── BackwardButton (Button)
│   │   │   │   │   │   ├── LeftButton (Button)
│   │   │   │   │   │   ├── RightButton (Button)
│   │   │   ├── BottomTabControl (TabControl, Grid.Row=1)
│   │   │   │   ├── RealTimeTrackTab (TabItem)
│   │   │   │   │   ├── TrackDisplay (自定义控件)
│   │   │   │   ├── ViewImagesTab (TabItem)
│   │   │   │   │   ├── ImageList (ListBox)
│   │   │   │   │   ├── ImageViewer (Image)
│   │   ├── OtherPageViews (根据选择的菜单项显示)
├── StatusBar (DockPanel, Dock.Bottom)
```

### 4.2 MVVM 绑定点

#### 4.2.1 顶部状态栏绑定

| 控件 | 绑定属性 | 说明 |
|------|----------|------|
| DateTimeDisplay | `DateTime.Now` | 绑定到当前时间 |
| ThermalCameraStatus | `ViewModel.ThermalCameraStatus` | 绑定到热成像相机状态 |
| VisualCameraStatus | `ViewModel.VisualCameraStatus` | 绑定到可视化相机状态 |
| ServerStatus | `ViewModel.ServerStatus` | 绑定到服务器状态 |
| PlcStatus | `ViewModel.PlcStatus` | 绑定到 PLC 状态 |
| UserRoleDisplay | `ViewModel.CurrentUser` | 绑定到当前用户信息 |
| ChangePasswordButton | `Command={Binding ChangePasswordCommand}` | 绑定修改密码命令 |
| ExitButton | `Command={Binding ExitCommand}` | 绑定退出命令 |

#### 4.2.2 左侧菜单绑定

| 控件 | 绑定属性 | 说明 |
|------|----------|------|
| NavigationView | `ItemsSource={Binding MenuItems}` | 绑定菜单项列表 |
| NavigationView | `SelectedItem={Binding SelectedMenuItem}` | 绑定当前选中的菜单项 |

#### 4.2.3 监控首页绑定

| 控件 | 绑定属性 | 说明 |
|------|----------|------|
| VideoPlayer | `Source={Binding VideoSource}` | 绑定视频源 |
| ConnectionStatus | `Text={Binding DroneConnectionStatus}` | 绑定无人机连接状态 |
| ConnectButton | `Command={Binding ConnectDroneCommand}` | 绑定连接无人机命令 |
| ConnectButton | `IsEnabled={Binding CanConnectDrone}` | 绑定连接按钮启用状态 |
| StartInspectionButton | `Command={Binding StartInspectionCommand}` | 绑定开始巡检命令 |
| SpeedSlider | `Value={Binding RobotSpeed}` | 绑定机器人速度 |
| DirectionButtons | `Command={Binding MoveCommand}` | 绑定移动命令，参数为方向 |
| TrackDisplay | `ItemsSource={Binding CurrentTrack}` | 绑定实时轨迹数据 |
| ImageList | `ItemsSource={Binding CapturedImages}` | 绑定捕获的图片列表 |
| ImageViewer | `Source={Binding SelectedImage}` | 绑定选中的图片 |

### 4.3 数据模型

#### 4.3.1 菜单模型

```csharp
public class MenuItem
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Icon { get; set; }
    public Type PageType { get; set; }
}
```

#### 4.3.2 相机状态模型

```csharp
public class CameraStatus
{
    public string Ip { get; set; }
    public int Port { get; set; }
    public bool IsConnected { get; set; }
    public string DisplayText => $"{Ip}:{Port} {(IsConnected ? "✓" : "✗")}";
}
```

#### 4.3.3 无人机信息模型

```csharp
public class DroneInfo
{
    public string Status { get; set; }
    public double Battery { get; set; }
    public double Altitude { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
```

## 5. 交互流程

### 5.1 菜单切换流程

1. 用户点击左侧菜单项
2. ViewModel 更新 SelectedMenuItem 属性
3. 触发页面切换逻辑
4. 根据 SelectedMenuItem 加载对应的页面

### 5.2 在线模式菜单获取流程

1. 应用启动时检查 enableServer 配置
2. 如果 enableServer=true，调用 `/app/permission/list/self` 接口
3. 解析返回的权限数据，生成菜单列表
4. 如果 enableServer=false 或接口调用失败，使用离线默认菜单

### 5.3 监控操作流程

1. 用户在监控首页查看视频流
2. 点击"连接"按钮连接无人机
3. 调整速度滑块设置速度
4. 使用方向盘或方向按钮控制移动
5. 点击"开始巡检"启动自动巡检
6. 在底部标签页查看实时轨迹或捕获的图片