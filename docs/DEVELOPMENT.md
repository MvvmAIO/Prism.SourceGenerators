# 开发文档

本文档是**操作手册**（环境、构建、测试、布局）。**项目级开发规范的权威源是 [`../AGENTS.md`](../AGENTS.md)**；功能 backlog 见 [`ROADMAP.md`](ROADMAP.md)；文档体系见 [`DOCUMENTATION.md`](DOCUMENTATION.md)。

## 环境要求

| 工具 | 版本 |
|------|------|
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0**（`global.json` 锁定）+ **8.0**（测试 `net8.0`） |
| IDE | Visual Studio **2022 17.13+**、Rider、VS Code + C# Dev Kit（`.slnx`） |
| Git | 2.x |

## 克隆与构建

```bash
git clone https://github.com/MvvmAIO/Prism.SourceGenerators.git
cd Prism.SourceGenerators
```

**推荐（与 CI 一致，Nuke）：**

```bash
dotnet run --project build/_build.csproj -- --target Ci --configuration Release
```

**传统 dotnet：**

```bash
dotnet restore Prism.SourceGenerators.slnx
dotnet build Prism.SourceGenerators.slnx --configuration Release
dotnet test Prism.SourceGenerators.slnx --configuration Release --no-build
```

> Nuke 须在仓库根目录（含 `.nuke` 标记）执行。

## 仓库布局

| 路径 | 职责 |
|------|------|
| `Prism.SourceGenerators/` | 共享生成器源码（`.shproj` / `.projitems`） |
| `Prism.SourceGenerators.Roslyn4001` … `Roslyn5000` | 按 Roslyn API 带编译的分析器 DLL |
| `Prism.SourceGenerators.Core` | **MvvmAIO.Prism.Core** 特性 |
| `Prism.SourceGenerators.Package` | **MvvmAIO.Prism.SourceGenerators** NuGet |
| `Prism.Bcl.Commands` | **MvvmAIO.Prism.Bcl.Commands**（Prism 8 异步命令） |
| `Prism.SourceGenerators.Tests` | xUnit v3 + Verify（Roslyn 4.12 宿主） |
| `Prism.SourceGenerators.Tests.Roslyn5000` | Roslyn 5.0 冒烟 |
| `Prism.SourceGenerators.Integration.Tests` | 打包与 PSG 集成场景 |
| `build/` | Nuke（`build.slnx`） |
| `docs/` | **维护者文档**（Spec / Design / ADR / RFC） |
| `wiki/` | GitHub Wiki 同步源（简短导读，非 Spec 合同） |

详见 [design/Architecture.md](design/Architecture.md)。

## 架构原则

### 源生成器

1. 使用 **`IIncrementalGenerator`**，优先 `ForAttributeWithMetadataName`。
2. 生成逻辑经 **语法树** 发射（非裸字符串拼接，自 0.3.0 起）。
3. 分析器：`IsRoslynComponent`、`EnforceExtendedAnalyzerRules`。
4. 诊断前缀 **`PSG`**；新 ID 登记于 `AnalyzerReleases.*.md` 与 `docs/spec/`。
5. **四套 Roslyn 变体** + MSBuild targets 按编译器版本选择 analyzer 路径。

### 与 Prism 的边界

- 不嵌入 Prism 运行时；仅生成胶水代码与特性。
- Prism 8 异步命令在独立包 **Bcl.Commands**（见 ADR-003）。
- Prism 8 / 9 API 差异通过引用程序集探测命名空间（见 ADR-004）。

## 测试

| 项目 | 覆盖 |
|------|------|
| `Prism.SourceGenerators.Tests` | 生成器 Verify 快照、矩阵、诊断、CodeFix |
| `Prism.SourceGenerators.Tests.Roslyn5000` | Roslyn 5.0 冒烟 |
| `Prism.SourceGenerators.Integration.Tests` | NuGet 布局、PSG3002、Prism 8 契约 |

```bash
dotnet test Prism.SourceGenerators.slnx -c Release
```

TRX 输出：`TestResults/test-results.trx`（CI 制品）。

## 临时与实验

- 一次性实验、本地 `dotnet new` 测试应用 → 仓库根 **`.Temp/`**（已 gitignore / cursorignore）。
- 禁止将 `.Temp/` 内容提交或纳入 CI。

## 相关仓库

| 仓库 | 说明 |
|------|------|
| [Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples) | Avalonia / WPF / MAUI / Uno 示例 |
| [Prism.SourceGenerators.Docs](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs) | 使用者文档站 |

贡献流程见 [CONTRIBUTING.md](../CONTRIBUTING.md)。发版见 [PUBLISHING.md](PUBLISHING.md)。
