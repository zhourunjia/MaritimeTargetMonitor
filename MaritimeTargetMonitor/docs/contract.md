# 契约说明

## 1. config.json 全字段说明

### 1.1 字段清单、类型、默认值与校验规则

| 字段 | 类型 | 默认值 | 校验规则 | 说明 |
|------|------|--------|----------|------|
| ServerIP | string | "127.0.0.1" | 必须为合法IP地址格式 | 服务器IP地址 |
| ServerPort | int | 60800 | 必须在1-65535之间 | 服务器端口 |
| IsHttp | bool | true | 无特殊校验 | 是否使用HTTP协议 |
| KeepDBDays | int | 2 | 无特殊校验 | 数据库保留天数 |
| KeepLogDays | int | 10 | 无特殊校验 | 日志保留天数 |
| UploadInterval | int | 0 | 无特殊校验 | 上传间隔 |
| Tx2UsbCameraIp | string | "192.168.1.106" | 必须为合法IP地址格式 | TX2 USB相机IP |
| Tx2UsbCameraPort | int | 5000 | 必须为合法端口号 | TX2 USB相机端口 |
| EnableTx2UsbCamera | bool | true | 无特殊校验 | 是否启用TX2 USB相机 |
| Tx2ThermalCameraIp | string | "192.168.1.106" | 必须为合法IP地址格式 | TX2热成像相机IP |
| Tx2ThermalCameraPort | int | 5001 | 必须为合法端口号 | TX2热成像相机端口 |
| EnableTx2ThermalCamera | bool | true | 无特殊校验 | 是否启用TX2热成像相机 |
| ThermalCameraWidth | int | 640 | 无特殊校验 | 热成像相机宽度 |
| ThermalCameraHeight | int | 480 | 无特殊校验 | 热成像相机高度 |
| ThermalCameraFps | int | 30 | 无特殊校验 | 热成像相机帧率 |
| ThermalCameraFormat | string | "YUYV" | 无特殊校验 | 热成像相机格式 |

### 1.2 结构定义

```json
{
  "ServerIP": "127.0.0.1",
  "ServerPort": 60800,
  "IsHttp": true,
  "KeepDBDays": 2,
  "KeepLogDays": 10,
  "UploadInterval": 0,
  "Tx2UsbCameraIp": "192.168.1.106",
  "Tx2UsbCameraPort": 5000,
  "EnableTx2UsbCamera": true,
  "Tx2ThermalCameraIp": "192.168.1.106",
  "Tx2ThermalCameraPort": 5001,
  "EnableTx2ThermalCamera": true,
  "ThermalCameraWidth": 640,
  "ThermalCameraHeight": 480,
  "ThermalCameraFps": 30,
  "ThermalCameraFormat": "YUYV"
}
```

## 2. 桌面端启动自检策略

### 2.1 外部依赖清单

| 依赖名称 | 版本 | 路径 | 用途 |
|----------|------|------|------|
| VLC | 未指定 | VLC/libvlc.dll | 视频播放 |
| HCNetSDK | 未指定 | SDKs/HCNetSDK.dll | 海康相机SDK |
| PlayCtrl | 未指定 | SDKs/PlayCtrl.dll | 视频播放控制 |
| hlog | 未指定 | SDKs/hlog.dll | 日志组件 |
| sqlite3.dll | 未指定 | sqlite3.dll | 数据库 |

### 2.2 启动自检策略

启动时，应用会执行以下自检：

**配置文件校验**：
- 检查config.json是否存在且为合法JSON
- 验证ServerIP格式是否合法
- 验证ServerPort是否在有效范围内

**外部依赖检查**：
- 检查VLC/libvlc.dll是否存在
- 检查SDKs/HCNetSDK.dll是否存在
- 检查SDKs/PlayCtrl.dll是否存在
- 检查SDKs/hlog.dll是否存在
- 检查sqlite3.dll是否存在

**目录可写性检查**：
- 检查临时目录是否可写
- 检查文件目录是否可写
- 检查图片目录是否可写
- 检查视频目录是否可写
- 检查日志目录是否可写

**异常处理**：
- 若检查失败，抛出MSException异常，包含具体错误信息
- 应用启动前必须通过所有检查

## 3. 后端接口清单

### 3.1 认证相关

| 接口URL | 方法 | Headers | Body | 成功返回码 | 说明 |
|---------|------|---------|------|------------|------|
| /app/user/login | POST | Authorization(可选) | {"username": "xxx", "password": "md5加密后的密码"} | 10000 | 用户登录 |
| /app/user/info | GET | Authorization | N/A | 10000 | 获取用户信息 |
| /app/user/pwd | POST | Authorization | {"oldPassword": "xxx", "newPassword": "xxx"} | 10000 | 修改密码 |

### 3.2 设备与巡检相关

| 接口URL | 方法 | Headers | Body | 成功返回码 | 说明 |
|---------|------|---------|------|------------|------|
| /app/equipment/list/own | GET | Authorization | N/A | 10000 | 获取设备列表 |
| /app/history/inspection/save/{equipmentId} | POST | Authorization | N/A (参数通过URL传递) | 10000 | 保存巡检记录 |
| /app/history/inspection/page | POST | Authorization | 包含设备ID、时间范围、分页参数 | 10000 | 查询巡检记录 |
| /app/history/inspection/{type}/{inspectionId}/save | POST | Authorization | 文件上传 | 10000 | 上传巡检数据 |
| /app/history/inspection/{type}/{inspectionId}/list | POST | Authorization | N/A | 10000 | 获取巡检数据 |

### 3.3 文件相关

| 接口URL | 方法 | Headers | Body | 成功返回码 | 说明 |
|---------|------|---------|------|------------|------|
| /app/file/upload/ | POST | Authorization | 文件上传 | 10000 | 上传文件 |
| /app/file/download/{fileId}/public | GET | N/A | N/A | 10000 | 下载文件 |
| /app/file/upload/exist/{md5} | GET | Authorization | N/A | 10000 | 检查文件是否存在 |
| /app/file/download/info/{fileId}/public | GET | Authorization | N/A | 10000 | 获取文件信息 |
| /app/file/upload/slice/genParent/{md5} | POST | Authorization | N/A (参数通过URL传递) | 10000 | 创建分片上传父文件 |
| /app/file/upload/slice | POST | Authorization | 文件分片上传 | 10000 | 上传文件分片 |
| /app/file/upload/slice/merge/{md5} | POST | Authorization | N/A | 10000 | 合并文件分片 |

### 3.4 视频相关

| 接口URL | 方法 | Headers | Body | 成功返回码 | 说明 |
|---------|------|---------|------|------------|------|
| /app/history/video/{type}/save | POST | Authorization | 包含设备ID、文件ID、开始时间等 | 10000 | 保存历史视频 |
| /app/history/video/{type}/page | POST | Authorization | 包含设备ID、时间范围、分页参数 | 10000 | 查询历史视频 |
| /app/history/video/{id} | DELETE | Authorization | N/A | 10000 | 删除视频 |

### 3.5 日志相关

| 接口URL | 方法 | Headers | Body | 成功返回码 | 说明 |
|---------|------|---------|------|------------|------|
| /app/log/alarm/save | POST | Authorization | 包含报警类型、内容、位置等 | 10000 | 上传报警日志 |
| /app/log/sysUser/save | POST | Authorization | 包含操作内容、操作类型等 | 10000 | 上传用户日志 |
| /app/log/equipmentRun/save | POST | Authorization | 包含设备状态、电池信息、环境数据等 | 10000 | 上传运行日志 |
| /app/log/environmental/save | POST | Authorization | 包含温度、湿度、气体浓度等 | 10000 | 上传环境日志 |
| /app/log/alarm/page | POST | Authorization | 包含设备ID、时间范围、分页参数 | 10000 | 查询报警日志 |
| /app/log/sysUser/page | POST | Authorization | 包含时间范围、分页参数 | 10000 | 查询用户日志 |
| /app/log/environmental/page | POST | Authorization | 包含设备ID、时间范围、分页参数 | 10000 | 查询环境日志 |
| /app/log/equipmentRun/page | POST | Authorization | 包含设备ID、时间范围、分页参数 | 10000 | 查询运行日志 |
| /app/log/video/page | POST | Authorization | 包含设备ID、时间范围、分页参数 | 10000 | 查询视频日志 |

### 3.6 轨迹相关

| 接口URL | 方法 | Headers | Body | 成功返回码 | 说明 |
|---------|------|---------|------|------------|------|
| /app/inspectionTrack/page | POST | Authorization | 包含名称、时间范围、分页参数 | 10000 | 查询系统轨迹 |
| /app/inspectionTrack/add | POST | Authorization | 包含轨迹名称、坐标数据、RFID数据等 | 10000 | 新增轨迹 |
| /app/inspectionTrack/save | POST | Authorization | 包含轨迹ID、名称、坐标数据、RFID数据等 | 10000 | 修改轨迹 |
| /app/inspectionTrack/id/{id} | DELETE | Authorization | N/A | 10000 | 删除轨迹 |
| /app/inspectionTrack/{id}/enabled/{enabled} | POST | Authorization | N/A | 10000 | 启用/禁用轨迹 |

### 3.7 其他

| 接口URL | 方法 | Headers | Body | 成功返回码 | 说明 |
|---------|------|---------|------|------------|------|
| /time/public | GET | N/A | N/A | 10000 | 获取服务器时间 |
| /app/permission/list/self | GET | Authorization | N/A | 10000 | 获取菜单权限 |
| /app/user/health | POST | Authorization | N/A | 10000 | 心跳检测 |

## 4. TX2 可见光/热成像协议

### 4.1 协议概述

TX2 相机通信协议包括以下核心功能：

- **SERV_INFO**：服务信息交换，用于设备发现和能力协商
- **JSON**：配置和状态数据的传输格式
- **PING**：心跳机制，保持连接活跃
- **帧格式**：视频数据的编码和传输格式
- **重连条件**：连接断开时的自动重连策略

### 4.2 心跳机制

- 客户端定期发送PING请求
- 服务器响应PONG
- 若超过指定时间未收到响应，触发重连

### 4.3 帧格式

- 热成像相机：使用YUYV格式
- 分辨率：640x480
- 帧率：30fps

### 4.4 重连条件

- 网络连接断开
- 心跳超时
- 服务器响应错误
- 手动触发重连