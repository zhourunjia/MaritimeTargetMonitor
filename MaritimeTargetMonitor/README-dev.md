# 开发指南

## 项目结构

```
MaritimeTargetMonitor/
├── docs/                    # 文档目录
│   ├── contract.md          # 契约说明
│   ├── security-gap.md      # 安全风险与升级方案
│   └── ui-contract.md       # UI契约说明
├── Maritime.App/            # WPF应用项目
│   ├── App.xaml             # 应用程序入口
│   ├── MainWindow.xaml      # 主窗口
│   └── ...
├── Maritime.Core/           # 核心类库
│   ├── Interfaces/          # 核心接口
│   ├── State/               # 状态管理
│   └── ...
├── Maritime.Infrastructure/ # 基础设施类库
│   ├── Services/            # 服务实现
│   └── ...
├── Maritime.Tests/          # 测试项目
│   └── ...
├── MaritimeTargetMonitor.sln # 解决方案文件
└── README-dev.md            # 开发指南
```

## 构建说明

### 环境要求

- Visual Studio 2022 或更高版本
- .NET Framework 4.8 开发工具
- Windows 10 或更高版本

### 构建步骤

1. 打开 `MaritimeTargetMonitor.sln` 解决方案文件
2. 在解决方案资源管理器中，右键点击解决方案，选择 "属性"
3. 在 "配置属性" -> "配置" 中，确保所有项目的 "Active Solution Configuration" 为 "Release"
4. 在 "配置属性" -> "平台" 中，确保所有项目的 "Active Solution Platform" 为 "x64"
5. 点击 "生成" -> "生成解决方案" 或按 F6 构建解决方案

### 构建输出

构建成功后，输出文件将位于各项目的 `bin/x64/Release/` 目录中。

## 运行说明

### 直接运行

1. 构建解决方案后，导航到 `Maritime.App/bin/x64/Release/` 目录
2. 双击 `Maritime.App.exe` 运行应用程序

### 调试运行

1. 在 Visual Studio 中，将 `Maritime.App` 设置为启动项目
2. 点击 "调试" -> "开始调试" 或按 F5 运行应用程序

## 项目说明

### Maritime.App

- WPF 应用程序项目，负责用户界面和交互
- 采用 MVVM 模式，只包含 View 和 ViewModel
- 不直接访问设备或网络，通过 Core 层接口与 Infrastructure 层通信

### Maritime.Core

- 核心类库，定义系统的核心接口和状态管理
- 包含设备、网络等服务的抽象接口
- 实现系统状态机，管理系统的运行状态

### Maritime.Infrastructure

- 基础设施类库，负责具体的设备和网络实现
- 实现 Core 层定义的接口
- 包含相机控制、服务器通信、配置管理等服务

### Maritime.Tests

- 测试项目，包含单元测试和集成测试
- 确保系统各组件的功能正常

## 开发规范

1. **命名规范**：
   - 类名：PascalCase
   - 方法名：PascalCase
   - 属性名：PascalCase
   - 变量名：camelCase

2. **代码风格**：
   - 使用 4 空格缩进
   - 每行不超过 120 字符
   - 适当添加注释，尤其是复杂逻辑

3. **MVVM 规范**：
   - View 只负责 UI 展示，不包含业务逻辑
   - ViewModel 负责业务逻辑和数据绑定
   - Model 负责数据模型和状态管理

4. **依赖注入**：
   - 使用构造函数注入依赖
   - 避免直接实例化服务，通过接口获取

## 常见问题

### 构建失败

- 检查是否安装了 .NET Framework 4.8 开发工具
- 确保所有项目的目标平台都是 x64
- 检查项目引用是否正确

### 运行时错误

- 检查配置文件是否正确
- 确保所需的依赖库存在
- 查看日志文件了解详细错误信息

### 设备连接失败

- 检查设备是否正常运行
- 确保网络连接正常
- 检查设备 IP 和端口配置是否正确