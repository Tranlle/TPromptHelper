# TPromptHelper

<div align="center">

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-11.3-68217A?style=flat-square&logo=avalonia)](https://avaloniaui.net/)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](LICENSE)
[![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20Linux%20%7C%20macOS-blue?style=flat-square)](#)

**跨平台的提示词优化工具 —— 内置多种优化策略，支持主流 LLM 提供商**

</div>

---

## 功能特性

| 特性 | 描述 |
|------|------|
| **多策略优化** | 结构化、少样本、思维链、精简版、技术向，五种优化策略 |
| **多提供商支持** | OpenAI、Anthropic、阿里 Qwen、月之暗面 Kimi、Ollama 及自定义端点 |
| **流式输出** | 实时流式返回优化结果，支持中断取消 |
| **API 密钥安全** | AES-256-GCM 加密存储，Windows DPAPI 保护主密钥 |
| **用量统计** | Token 消耗追踪与费用估算 |
| **调用日志** | 完整的 API 请求/响应记录，便于调试 |
| **跨平台** | Windows、Linux、macOS 全平台支持 |

---

## 快速开始

### 前置要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10/11、macOS 10.14+、或 Linux（需图形环境）

### 从源码运行

```bash
# 克隆仓库
git clone https://github.com/yourusername/TPromptHelper.git
cd TPromptHelper

# 还原依赖
dotnet restore

# 运行
dotnet run --project src/TPromptHelper.Desktop
```

### 运行测试

```bash
dotnet test
```

---

## 使用指南

### 1. 添加模型配置

首次使用需配置至少一个 LLM 模型：

1. 点击 **设置** → **模型管理**
2. 点击 **新建配置**
3. 选择 **Provider**（如 OpenAI）
4. 填写 **模型名称**（如 `gpt-4o`）
5. 粘贴 **API Key**
6. 保存

> **安全提示**：API Key 使用 AES-256-GCM 加密存储，主密钥在 Windows 上由 DPAPI 保护。

### 2. 优化提示词

1. 在输入框输入原始提示词
2. 选择优化策略：
   - **结构化**：角色定义 + 任务描述 + 约束条件
   - **少样本**：自动补充输入输出示例
   - **思维链**：引导分步推理过程
   - **精简版**：去除冗余，保留核心
   - **技术向**：代码规范 + 输出格式要求
3. 点击 **优化**
4. 等待流式输出完成
5. 点击 **复制** 获取结果

### 3. 查看用量统计

- 点击 **Token 统计**：查看各模型的调用次数、Token 消耗和估算费用
- 点击 **API 日志**：查看详细的请求/响应记录

---

## 项目结构

```
TPromptHelper/
├── src/
│   ├── TPromptHelper.Core/          # 领域模型 + 接口定义
│   │   ├── Interfaces/               # 服务接口（ILlmService 等）
│   │   └── Models/                   # 实体模型（ModelProfile 等）
│   │
│   ├── TPromptHelper.Services/     # 服务实现
│   │   ├── LlmService.cs             # LLM API 调用
│   │   ├── EncryptionService.cs      # AES-256-GCM 加密
│   │   ├── PromptOptimizer.cs        # 提示词优化引擎
│   │   └── StrategyPrompts.cs        # 各策略的系统提示词
│   │
│   ├── TPromptHelper.Infrastructure/# 数据持久化
│   │   ├── Database/
│   │   │   └── AppDatabase.cs       # SQLite 连接管理
│   │   └── Repositories/             # 仓储实现
│   │
│   ├── TPromptHelper.Desktop/      # Avalonia UI
│   │   ├── ViewModels/               # MVVM 视图模型
│   │   └── Views/                   # XAML 视图
│   │
│   └── TPromptHelper.Tests/        # 单元测试 (xUnit + Moq)
│
├── assets/                           # 图标等资源
├── dist/                             # 打包输出
├── publish/                          # 发布输出
└── TPromptHelper.sln              # 解决方案文件
```

### 架构说明

```
┌─────────────────────────────────────────┐
│         TPromptHelper.Desktop          │
│         (Avalonia UI + MVVM)             │
└─────────────────┬───────────────────────┘
                  │
    ┌─────────────┼─────────────┐
    ▼             ▼             ▼
┌────────┐  ┌──────────┐  ┌───────────────┐
│ Core   │  │ Services  │  │ Infrastructure │
│(Models)│  │(LLM/Encrypt)│ │(SQLite/Repo)  │
└────────┘  └──────────┘  └───────────────┘
```

- **Core**：领域模型与服务接口，无外部依赖
- **Services**：业务逻辑实现，依赖 Core
- **Infrastructure**：数据持久化，依赖 Core
- **Desktop**：展示层，依赖上述三层

---

## 构建与打包

### Linux

```bash
# 发布
dotnet publish src/TPromptHelper.Desktop \
  -c Release -r linux-x64 --self-contained \
  -p:PublishSingleFile=true \
  -o publish/linux-x64

# 打包为 AppImage
mkdir -p dist/linux
vpk pack \
  --packId TPromptHelper \
  --packVersion 1.0.0 \
  --packDir publish/linux-x64 \
  --mainExe TPromptHelper.Desktop \
  --outputDir dist/linux
```

### Windows (PowerShell)

```powershell
dotnet publish src/TPromptHelper.Desktop `
  -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish/win-x64

vpk pack `
  --packId TPromptHelper `
  --packVersion 1.0.0 `
  --packDir publish/win-x64 `
  --mainExe TPromptHelper.Desktop.exe `
  --outputDir dist/win
```

### macOS

```bash
dotnet publish src/TPromptHelper.Desktop \
  -c Release -r osx-arm64 --self-contained \
  -p:PublishSingleFile=true \
  -o publish/osx-arm64

mkdir -p dist/osx
vpk pack \
  --packId TPromptHelper \
  --packVersion 1.0.0 \
  --packDir publish/osx-arm64 \
  --mainExe TPromptHelper.Desktop \
  --outputDir dist/osx
```

> 详细打包说明见 [DEVELOPMENT.md](DEVELOPMENT.md)

---

## 配置说明

### 数据库位置

| 操作系统 | 路径 |
|---------|------|
| Windows | `%APPDATA%\TPromptHelper\app.db` |
| Linux | `~/.config/TPromptHelper/app.db` |
| macOS | `~/Library/Application Support/TPromptHelper/app.db` |

### API 提供商默认端点

| Provider | 默认端点 |
|----------|---------|
| OpenAI | `https://api.openai.com/v1/chat/completions` |
| Anthropic | `https://api.anthropic.com/v1/messages` |
| Qwen | `https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions` |
| Kimi | `https://api.moonshot.cn/v1/chat/completions` |
| Ollama | `http://localhost:11434/v1/chat/completions` |

---

## 技术栈

| 组件 | 技术 |
|------|------|
| UI 框架 | [Avalonia 11.3](https://avaloniaui.net/) |
| UI 模式 | [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) |
| 数据库 | [SQLite](https://www.sqlite.org/) + [Dapper](https://github.com/DapperLib/Dapper) |
| HTTP 弹性 | [Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/en-us/dotnet/core/extensions/http-resilience) |
| 测试 | [xUnit](https://xunit.net/) + [Moq](https://github.com/moq/moq) |
| 打包 | [Velopack](https://velopack.app/) |

---

## 相关资源

- [DEVELOPMENT.md](DEVELOPMENT.md) - 开发与打包详细文档
- [LICENSE](LICENSE) - MIT 许可证

---

## 贡献

欢迎提交 Issue 和 Pull Request！

---

## 许可证

本项目基于 [MIT](LICENSE) 许可证开源。
