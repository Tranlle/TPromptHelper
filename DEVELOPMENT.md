# TPromptHelper 开发与打包文档

## 环境说明

| 工具 | 版本 | 用途 |
|------|------|------|
| .NET SDK | 10.0.201 | 编译、测试、发布 |
| .NET Runtime 9.0 | 9.0.14 | vpk CLI 依赖 |
| ASP.NET Core 9.0 | 9.0.14 | vpk CLI 依赖 |
| Velopack CLI (`vpk`) | 0.0.1298 | 跨平台打包工具 |
| appimagetool | continuous | Linux AppImage 备用工具 |

所有工具安装在用户目录，无需 root 权限。

---

## 一、环境配置（首次）

### 1.1 安装 .NET 10 SDK

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
```

### 1.2 安装 vpk 所需的 .NET 9 Runtime

```bash
/tmp/dotnet-install.sh --runtime dotnet    --channel 9.0 --install-dir "$HOME/.dotnet"
/tmp/dotnet-install.sh --runtime aspnetcore --channel 9.0 --install-dir "$HOME/.dotnet"
```

### 1.3 安装 Velopack CLI

```bash
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
dotnet tool install -g vpk
```

### 1.4 安装 appimagetool（Linux 备用）

```bash
mkdir -p "$HOME/.local/bin"
wget -q https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage \
     -O "$HOME/.local/bin/appimagetool"
chmod +x "$HOME/.local/bin/appimagetool"
```

### 1.5 写入 ~/.bashrc（永久生效）

```bash
# 追加到 ~/.bashrc
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"
```

生效：`source ~/.bashrc`

### 1.6 验证

```bash
dotnet --version      # 10.0.201
vpk --help            # Velopack CLI ...
appimagetool --version
```

---

## 二、日常开发

```bash
cd /home/tja/develop/TPromptHelper

# 还原依赖
dotnet restore

# 调试运行
dotnet run --project src/TPromptHelper.Desktop

# 运行测试
dotnet test

# 查看测试详情
dotnet test --logger "console;verbosity=normal"
```

---

## 三、打包为单文件安装包

### 3.1 Linux — AppImage

```bash
# Step 1: 发布自包含二进制
dotnet publish src/TPromptHelper.Desktop \
  -c Release \
  -r linux-x64 \
  --self-contained \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -o publish/linux-x64

# Step 2: 打包为 AppImage（含自动更新支持）
mkdir -p dist/linux
vpk pack \
  --packId      TPromptHelper \
  --packVersion 1.0.0 \
  --packDir     publish/linux-x64 \
  --mainExe     TPromptHelper.Desktop \
  --outputDir   dist/linux
```

产出：`dist/linux/TPromptHelper.AppImage`（约 43MB，双击即用）

> 注意：AppImage 在运行时需要 FUSE。
> 若系统无 FUSE，可用 `./TPromptHelper.AppImage --appimage-extract-and-run` 运行。

---

### 3.2 Windows — Setup.exe

在 Windows 机器或 CI 上执行：

```powershell
# Step 1: 发布
dotnet publish src/TPromptHelper.Desktop `
  -c Release `
  -r win-x64 `
  --self-contained `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish/win-x64

# Step 2: 打包（需在 Windows 上运行）
mkdir dist/win
vpk pack `
  --packId      TPromptHelper `
  --packVersion 1.0.0 `
  --packDir     publish/win-x64 `
  --mainExe     TPromptHelper.Desktop.exe `
  --outputDir   dist/win
```

产出：`dist/win/TPromptHelper-win-Setup.exe`

---

### 3.3 macOS — .dmg

在 macOS 机器或 CI 上执行：

```bash
# Apple Silicon
dotnet publish src/TPromptHelper.Desktop \
  -c Release -r osx-arm64 --self-contained \
  -p:PublishSingleFile=true -o publish/osx-arm64

# Intel（可选，用于 Universal Binary）
dotnet publish src/TPromptHelper.Desktop \
  -c Release -r osx-x64 --self-contained \
  -p:PublishSingleFile=true -o publish/osx-x64

# 打包（Apple Silicon）
mkdir -p dist/osx
vpk pack \
  --packId      TPromptHelper \
  --packVersion 1.0.0 \
  --packDir     publish/osx-arm64 \
  --mainExe     TPromptHelper.Desktop \
  --outputDir   dist/osx
```

产出：`dist/osx/TPromptHelper.dmg`

> 正式分发需执行代码签名：
> ```bash
> codesign --deep --force --sign "Developer ID Application: ..." dist/osx/TPromptHelper.app
> xcrun notarytool submit TPromptHelper.dmg --apple-id "..." --wait
> ```

---

## 四、GitHub Actions 自动化（推 tag 触发）

`.github/workflows/release.yml` 已包含三平台并行构建。触发方式：

```bash
git tag v1.0.0
git push origin v1.0.0
```

CI 将自动：
1. 在 ubuntu/windows/macos Runner 上分别构建
2. 生成对应平台的安装包
3. 上传为 Release Artifacts

---

## 五、版本更新流程

```bash
# 修改版本号后打包（--packVersion 与历史版本不同即可）
vpk pack \
  --packId      TPromptHelper \
  --packVersion 1.1.0 \
  --packDir     publish/linux-x64 \
  --mainExe     TPromptHelper.Desktop \
  --outputDir   dist/linux

# dist/linux/ 目录下会新增：
#   TPromptHelper-1.1.0-linux-full.nupkg   （完整包）
#   TPromptHelper-1.0.0-1.1.0-delta.nupkg  （差量包，已安装用户自动增量更新）
#   TPromptHelper.AppImage                  （最新版本）
```

---

## 六、目录结构说明

```
TPromptHelper/
├── src/
│   ├── TPromptHelper.Core/          # 领域模型 + 接口（零依赖）
│   ├── TPromptHelper.Services/      # 加密、LLM调用、提示词优化引擎
│   ├── TPromptHelper.Infrastructure/# SQLite 持久化（Dapper）
│   ├── TPromptHelper.Desktop/       # Avalonia 11 UI 主程序
│   └── TPromptHelper.Tests/         # xUnit 单元测试
├── publish/                           # dotnet publish 输出（gitignore）
├── dist/                              # 打包产物（gitignore）
└── assets/                            # 图标等静态资源
```

---

## 七、常见问题

**Q: AppImage 运行报 `fuse: device not found`**
```bash
# 方式一：安装 FUSE
sudo apt install fuse libfuse2

# 方式二：绕过 FUSE 直接运行
./TPromptHelper.AppImage --appimage-extract-and-run
```

**Q: `vpk` 命令找不到**
```bash
export PATH="$HOME/.dotnet/tools:$PATH"
# 或重新执行：source ~/.bashrc
```

**Q: vpk 报 "must install .NET"**
```bash
# 确保已安装 .NET 9 runtime 和 ASP.NET Core 9
/tmp/dotnet-install.sh --runtime dotnet     --channel 9.0 --install-dir "$HOME/.dotnet"
/tmp/dotnet-install.sh --runtime aspnetcore --channel 9.0 --install-dir "$HOME/.dotnet"
```

**Q: 首次运行提示找不到模型配置**

在「模型管理」中新建一个模型配置，填入 Provider、Model Name 和 API Key 即可。
数据库路径：`~/.config/TPromptHelper/app.db`（Linux）
