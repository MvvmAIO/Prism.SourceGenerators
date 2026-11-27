# 发布流程

NuGet 包发布与版本管理。工程约束见 [`../AGENTS.md`](../AGENTS.md)；变更记录见 [`../CHANGELOG.md`](../CHANGELOG.md)。

## 包清单

| PackageId | 项目 | 说明 |
|-----------|------|------|
| **MvvmAIO.Prism.SourceGenerators** | `Prism.SourceGenerators.Package` | 分析器 + 捆绑 **MvvmAIO.Prism.Core** |
| **MvvmAIO.Prism.Bcl.Commands** | `Prism.Bcl.Commands` | Prism 8 `AsyncDelegateCommand` |

## 本地打包

```bash
dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version <VER>
```

产物位于 `Prism.SourceGenerators.Package/bin/Release/` 与 `Prism.Bcl.Commands/bin/Release/`。

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

`.github/workflows/release.yml` 使用 GitHub Actions OIDC 与 `NuGet/login`；只有 `publish` job 具有 `id-token: write`。NuGet.org Trusted Publishing 策略面向仓库 `MvvmAIO/Prism.SourceGenerators`、工作流 `release.yml`，未设置 Environment；包名 glob 为 `MvvmAIO.Prism*`，允许向两个包推送新版本。

在 GitHub Actions 仓库变量中设置 `NUGET_USER` 为创建该策略、或被该策略信任的 NuGet.org 帐户的 **profile 名称**（不要假定它与 GitHub 用户名或组织包所有者相同，也不要使用邮箱）。两个包都不需要持久 API Key。

- 白名单维护者推送 `vX.Y.Z` tag 会触发发布。
- 恢复已存在标签（例如 `v0.8.2`）时，在 `master` 上运行：

  ```bash
  gh workflow run release.yml --ref master -f version=0.8.2
  ```

  发布会从既有 tag 构建，而不是当前 `master`；不要移动或重建 tag，也不要重跑旧的 API Key 工作流。
- `--skip-duplicate` 支持部分重试。
- 旧 API Key secret 只有在迁移验证成功后才能退役。

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
