# Cosmos

> 一个用于控制整台计算机的子系统。

Cosmos 是一个 Windows 桌面自动化平台，基于 .NET 10 和 WebView2 构建。它包含自定义脚本语言（cm-script）、任务调度器、密码管理器、应用启动器、铃声播放器和日志查看器——全部通过一个标签页界面统一管理。

---

## 目录

- [功能特性](#功能特性)
- [环境要求](#环境要求)
- [安装方式](#安装方式)
- [界面指南](#界面指南)
  - [开始页面](#开始页面)
  - [日志查看器](#日志查看器)
  - [调度器](#调度器)
  - [脚本终端](#脚本终端)
  - [密码管理器](#密码管理器)
  - [铃声播放器](#铃声播放器)
  - [应用启动器](#应用启动器)
  - [设置](#设置)
- [cm-script 语言参考](#cm-script-语言参考)
  - [语句](#语句)
  - [注释](#注释)
  - [参数类型](#参数类型)
  - [内置函数](#内置函数)
  - [外部可执行文件](#外部可执行文件)
  - [Python 脚本](#python-脚本)
  - [IPC 协议](#ipc-协议)
  - [完整示例](#完整示例)
- [Python SDK](#python-sdk)
- [数据文件](#数据文件)
- [错误代码](#错误代码)
- [许可证](#许可证)

---

## 功能特性

- **自定义脚本语言** — cm-script，语法简洁，适合自动化任务
- **任务调度器** — 支持一次性、每天或每周定时执行脚本
- **密码管理器** — AES-256-CBC 加密存储，主密码保护
- **应用启动器** — 注册并启动应用程序，支持拖拽排序
- **铃声播放器** — 循环播放音频文件作为通知
- **日志查看器** — 可过滤的系统日志，支持复制到剪贴板
- **Python 集成** — 通过 `cosmos-client` SDK 实现双向 IPC 通信
- **WebView2 界面** — 无边框窗口，原生 DWM 动画效果

---

## 环境要求

- **Windows 10/11**
- **.NET 10.0 SDK** — [下载地址](https://dotnet.microsoft.com/download)
- **WebView2 运行时** — 现代 Windows 已预装；如缺失请[下载](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)
- **Visual Studio 2022**（推荐）或 `dotnet` 命令行工具

---

## 安装方式

### 方式一：从 Release 下载（推荐）

从 [Releases](https://github.com/Fluffvoya/cosmos/releases) 页面下载最新的构建包，解压后直接运行 `app.exe`，无需构建。

### 方式二：从源码构建

```bash
# 克隆仓库
git clone https://github.com/Fluffvoya/cosmos.git
cd cosmos

# 构建
dotnet build cosmos.sln

# 运行
dotnet run --project modules/app/app.csproj

# 构建发布版本
dotnet build cosmos.sln -c Release

# 运行测试
dotnet test modules/tests/tests.csproj
```

输出文件位于 `modules/app/bin/{Configuration}/net10.0-windows/app.exe`。

---

## 界面指南

Cosmos 使用单个无边框窗口，标签页布局。标签栏可配置在**顶部**、**左侧**或**右侧**显示（在设置中调整）。

### 开始页面

着陆页显示内容：

- **时钟** — 实时显示，支持 12 小时制或 24 小时制（点击切换）
- **问候语** — 使用您配置的用户名显示个性化问候
- **快捷操作** — 通往所有功能的卡片：日志、调度器、脚本、密码管理器、铃声、应用启动器和设置

### 日志查看器

可过滤的系统日志表格。

| 列名 | 说明 |
|------|------|
| 时间 | 日志条目的时间戳 |
| 级别 | `Info`（信息）、`Warning`（警告）或 `Error`（错误） |
| 发送者 | 产生日志的模块 |
| 消息 | 日志内容 |

- **过滤按钮** — 显示全部、仅错误、仅警告或仅信息
- **复制** — 点击任意行的复制图标，将内容复制到剪贴板
- **清除** — 删除所有日志条目

### 调度器

管理定时执行的 cm-script 任务。

**添加任务：**

1. 点击 **添加任务**
2. 设置**时间**（HH:MM 格式）
3. 选择**重复方式**：一次、每天或每周
4. 每周模式：选择星期几
5. 一次性模式：选择具体日期
6. 浏览并选择 `.cms` 脚本文件
7. 点击 **保存**

**管理任务：**

- **立即运行** — 立即执行该任务
- **删除** — 移除任务
- **最后状态** — 显示最近一次执行的结果

任务持久化存储在 `~/.cosmos/tasks.json` 中，后台调度器每 15 秒轮询一次。

### 脚本终端

cm-script 的交互式 REPL。

- **输入** — 在 `>` 提示符处输入 cm-script 命令，按回车执行
- **历史记录** — 使用上/下方向键浏览命令历史
- **输出** — 颜色编码：白色为信息，绿色为成功，红色为错误，黄色为警告，灰色为回显的命令
- **清除** — 重置终端输出

输出持久化存储在 `~/.cosmos/script-output.json` 中（最多 500 行），切换标签页后内容保留。

### 密码管理器

受主密码保护的加密凭据存储。

**首次设置：**

1. 输入并确认主密码
2. 主密码哈希（SHA-256 + 盐值）存储在 `~/.cosmos/password_hash.dat` 中

**日常使用：**

1. 输入主密码解锁
2. 在左侧边栏浏览平台
3. 点击平台查看其账户
4. 每个账户显示：用户名、密码（默认隐藏）和复制按钮

**管理凭据：**

- **添加平台** — 创建新的平台条目（如 "GitHub"、"AWS"）
- **添加账户** — 在平台下添加用户名/密码对
- **编辑/删除** — 修改或移除平台和账户
- **密码生成器** — 生成随机密码，可配置长度和是否包含特殊字符
- **更改主密码** — 使用新密码重新加密所有数据

所有数据均使用 AES-256-CBC 加密，存储在 `~/.cosmos/password_data.enc` 中。

### 铃声播放器

显示当前正在播放的音频文件。

- 每个正在播放的铃声显示为一个水平条，显示文件名
- 点击 **关闭** 按钮停止播放
- 从 cm-script 执行 `PlayRingtone` 命令时，该标签页会自动打开

### 应用启动器

基于卡片的应用启动器。

- **卡片网格** — 每个注册的应用显示其图标（从可执行文件中提取）和名称
- **搜索** — 按名称过滤应用
- **添加应用** — 注册新应用，填写名称、可执行文件路径和可选参数
- **删除** — 移除已注册的应用
- **拖拽排序** — 拖拽卡片重新排列
- **启动** — 点击卡片启动应用

注册的应用存储在 `~/.cosmos/launch-apps.json` 中。

### 设置

通过菜单栏访问。更改立即保存。

| 分类 | 设置项 | 说明 |
|------|--------|------|
| **常规** | 用户名 | 开始页面问候语中显示的名称 |
| | 标签位置 | 顶部、左侧或右侧 |
| | 标签栏宽度 | 像素单位（用于左侧/右侧位置） |
| **脚本** | Python 路径 | Python 解释器路径（支持 `%VAR%` 环境变量展开） |
| **启动** | 启动脚本 | Cosmos 启动时运行的 `.cms` 文件路径 |

---

## cm-script 语言参考

cm-script 是一种面向行的脚本语言。每行一条语句，空行会被忽略。文件扩展名：`.cms`。

### 语句

| 关键字 | 别名 | 说明 |
|--------|------|------|
| `COSMOS` | `$` | 通过内部路由器调用已注册的 Cosmos 函数 |
| `EXE` | `#` | 启动外部可执行文件，通过 stdin/stdout JSON IPC 通信 |
| `PYTHON` | — | 启动 Python 脚本，通过 stdin/stdout JSON IPC 通信 |

### 注释

注释以 `!` 开头，延伸到行尾。支持行内注释：

```
! 这是整行注释
COSMOS Log "你好"  ! 这是行内注释
```

### 参数类型

参数以空格分隔，支持三种类型：

| 类型 | 格式 | 示例 |
|------|------|------|
| 整数 | 可选符号 + 数字 | `42`、`-100`、`0` |
| 浮点数 | 带小数点的数字 | `3.14`、`-2.5`、`.5` |
| 字符串 | 双引号或单引号文本 | `"你好世界"`、`'你好世界'` |

### 内置函数

这些函数注册在 Cosmos 路由器中，可通过 `COSMOS` 或 `$` 调用：

#### `Log` — 记录信息消息

```
COSMOS Log "应用程序启动成功"
$ Log "准备就绪"
```

#### `Warning` — 记录警告消息

```
COSMOS Warning "磁盘空间不足"
$ Warning "请检查配置"
```

#### `Error` — 记录错误消息

```
COSMOS Error "无法连接到数据库"
```

#### `MessageBox` — 显示原生对话框

```
COSMOS MessageBox "提示" "操作已完成"
$ MessageBox "确认" "确定要继续吗？"
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `name` | 字符串 | 对话框标题 |
| `message` | 字符串 | 对话框内容 |

#### `MessageBar` — 显示通知提示

```
COSMOS MessageBar "构建完成" "info"
$ MessageBar "内存不足" "warning"
COSMOS MessageBar "连接断开" "error"
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `message` | 字符串 | 通知文本 |
| `level` | 字符串 | `"info"`（信息）、`"warning"`（警告）或 `"error"`（错误） |

#### `GetUserName` — 获取已配置的用户名

```
COSMOS GetUserName
```

返回设置中配置的用户名。无参数。

#### `PlayRingtone` — 播放音频文件

```
COSMOS PlayRingtone "C:/Music/alert.mp3"
$ PlayRingtone "notification.wav"
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `audioPath` | 字符串 | 音频文件路径 |

音频会循环播放，直到用户在铃声标签页中关闭它。

#### `OpenRegisteredApp` — 启动已注册的应用

```
COSMOS OpenRegisteredApp "Visual Studio Code"
$ OpenRegisteredApp "终端"
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `appName` | 字符串 | 已注册应用的名称 |

### 外部可执行文件

使用 `EXE` 或 `#` 启动外部程序。程序通过 stdin/stdout JSON IPC 与 Cosmos 通信。

```
EXE mytool.exe --verbose --output result.txt
# helper.cmd /silent
$  ! 这是无效的——$ 后面需要跟函数名
```

可执行文件路径后面可以跟可选的字符串参数。

### Python 脚本

使用 `PYTHON` 启动 Python 脚本。脚本通过 stdin/stdout JSON IPC 与 Cosmos 通信。

```
PYTHON cleanup.py --path /tmp
PYTHON analyze.py "input.csv"
```

Python 脚本路径后面可以跟可选的字符串参数。

**要求：**
- 必须安装 Python，并在 设置 > 脚本 > Python 路径 中配置路径
- 使用 `cosmos-client` SDK 简化 IPC 通信（参见 [Python SDK](#python-sdk)）

### IPC 协议

外部进程（EXE 和 PYTHON）通过 stdin/stdout 使用 JSON 消息与 Cosmos 通信。

**请求（Cosmos → 进程）：**

```json
{"request": "Log", "args": ["来自外部进程的消息"]}
```

**响应（进程 → Cosmos）：**

```json
{"request": "Log", "message": "已收到"}
```

可用的请求名称与内置函数名称一致：`Log`、`Warning`、`Error`、`MessageBox`、`MessageBar` 等。

### 完整示例

```
! ===================================
! Cosmos 启动脚本
! ===================================

! 记录系统状态
COSMOS Log "=== 系统启动 ==="
$ Log "正在初始化组件..."

! 显示欢迎对话框
COSMOS MessageBox "欢迎" "Cosmos 已准备就绪！"

! 检查配置
COSMOS Warning "请在设置中确认 Python 路径"

! 播放启动音效
COSMOS PlayRingtone "C:/Sounds/startup.mp3"

! 启动外部工具
EXE notepad.exe
# cmd.exe /c echo "来自 CMD 的问候"

! 运行 Python 维护脚本
PYTHON maintenance.py --check-all

! 通知完成
$ MessageBar "所有系统正常运行" "info"
$ Log "=== 启动完成 ==="
```

---

## Python SDK

`cosmos-client` 包提供了与 Cosmos 进行 IPC 通信的 Python 接口。

### 安装

```bash
pip install cosmos-client
```

### 使用方法

```python
from cosmos_client import Client

# 记录消息
response = Client.Send(Client.Log("来自 Python 的问候"))

# 显示警告
response = Client.Send(Client.Warning("请检查此项"))

# 显示消息对话框
response = Client.Send(Client.MessageBox("标题", "消息内容"))

# 显示通知提示
response = Client.Send(Client.MessageBar("任务完成", "info"))

# 播放铃声
response = Client.Send(Client.PlayRingtone("/path/to/audio.mp3"))

# 打开已注册的应用
response = Client.Send(Client.OpenRegisteredApp("VS Code"))

# 获取用户名
response = Client.Send(Client.GetUserName())
```

### 自定义请求

```python
from cosmos_client import Client

# 发送自定义请求
response = Client.Send({"request": "Log", "args": ["自定义消息"]})
```

---

## 数据文件

所有 Cosmos 数据存储在 `~/.cosmos/` 目录中（通常是 `C:\Users\<用户名>\.cosmos\`）：

| 文件 | 说明 |
|------|------|
| `settings.json` | 应用设置（用户名、标签位置、Python 路径等） |
| `start-config.json` | 开始页面配置（时间格式） |
| `tasks.json` | 调度任务列表 |
| `script-output.json` | 持久化的脚本终端输出（最多 500 行） |
| `launch-apps.json` | 应用启动器中注册的应用 |
| `password_hash.dat` | 主密码盐值和 SHA-256 哈希 |
| `password_data.enc` | AES-256-CBC 加密的凭据数据 |

---

## 错误代码

| 范围 | 模块 | 示例 |
|------|------|------|
| 1xxx | 参数 | `ArgumentNull`、`ArgumentFormatInvalid`、`ArgumentTypeMismatch`、`ArgumentOverflow` |
| 2xxx | 函数路由器 | `FunctionNotFound`、`ArgumentCountMismatch`、`ArgumentTypeCheckFailed` |
| 3xxx | 数据模型 | `JsonDeserializeFailed`、`JsonSerializeFailed`、`EmptyRequestName` |
| 4xxx | cm-script | `SyntaxError`、`MissingFunctionName` |
| 5xxx | 脚本函数 | `EmptyArgumentValue`、`InvalidArgumentValue` |
| 6xxx | 进程 | `PythonNotFound`、`ScriptNotFound`、`PythonRuntimeError`、`ProcessCommunicationError` |
| 7xxx | 启动器 | `AppNotFound`、`AppPathInvalid`、`DuplicateAppName`、`AppRegistryLoadFailed` |

---

## 许可证

MIT 许可证 — Copyright (c) 2026 Hao Meng
