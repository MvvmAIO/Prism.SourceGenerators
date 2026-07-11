# 发布流程

NuGet 包发布与版本管理。工程约束见 [`../AGENTS.md`](../AGENTS.md)；变更记录见 [`../CHANGELOG.md`](../CHANGELOG.md)。

## 包清单

| PackageId | 项目 | 说明 |
|-----------|------|------|
| **MvvmAIO.Prism.SourceGenerators** | `Prism.SourceGenerators.Package` | 分析器 + 捆绑 **MvvmAIO.Prism.Core** |
| **MvvmAIO.Prism.Bcl.Commands** | `Prism.Bcl.Commands` | Prism 8 `AsyncDelegateCommand`（**独立 API Key**） |

## 本地打包

```bash
dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version <VER>
```

产物位于 `artifacts/`（以 Nuke 配置为准）。

## 发布前检查

发版前核对：

- [ ] `CHANGELOG.md` `[Unreleased]` 已整理
- [ ] `Prism.SourceGenerators.Package` 与 `Prism.Bcl.Commands` 的 `<Version>` 一致（若同期发版）
- [ ] README 安装示例版本号
- [ ] **Prism.SourceGenerators.Docs** getting-started 版本号
- [ ] `AnalyzerReleases.Shipped.md` 已合并 Unshipped 条目
- [ ] Nuke **Ci** 全绿（183+ 测试）
- [ ] GitHub Release 草稿（非仅 tag）

## CI 发布

- 工作流：`.github/workflows/`（`dotnet.yml`、Publish NuGet）
- 主包与 Bcl.Commands 可使用 **不同** `NUGET_API_KEY` / `NUGET_API_KEY_BCL`
- Tag：`v<VER>` 触发发布（维护者权限）

```bash
dotnet run --project build/_build.csproj -- --target Publish --configuration Release --version <VER> --nuget-api-key <KEY>
```

## 版本策略

- 遵循 [SemVer](https://semver.org/) 与 [Keep a Changelog](https://keepachangelog.com/)
- **破坏性**生成器产出或特性语义变更 → **主版本**
- 新增生成器 / 诊断 / 非破坏性特性 → **次版本**
- Bug fix、文档、依赖补丁 → **修订版本**

## 发布后

1. 创建 **GitHub Release**（附 CHANGELOG 摘要）
2. 更新 **Prism.SourceGenerators.Samples** 包引用（如需要）
3. Roadmap 已完成项移入归档章节

## 示例与消费者验证

- [Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples) CI 使用 NuGet 包（无同级源码时）
- 本地开发可用同级 `../Prism.SourceGenerators` 项目引用（Samples 的 `Directory.Build.props`）
