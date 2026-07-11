# 内部文档索引

本目录面向 **MvvmAIO/Prism.SourceGenerators** 维护者与贡献者（中文为主）。面向库使用者的多语言手册见 [Prism.SourceGenerators.Docs](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs)（站点：[mvvmaio.github.io/Prism.SourceGenerators.Docs](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/)）。

| 文档 | 说明 |
|------|------|
| [DOCUMENTATION.md](DOCUMENTATION.md) | 文档载体与维护规则 |
| [DEVELOPMENT.md](DEVELOPMENT.md) | 环境、构建、测试与仓库布局 |
| [CONTRIBUTING.md](../CONTRIBUTING.md) | 贡献流程与代码审查约定 |
| [PUBLISHING.md](PUBLISHING.md) | NuGet 打包与发布流程 |
| [ROADMAP.md](ROADMAP.md) | 功能与技术 backlog |
| [../AGENTS.md](../AGENTS.md) | 自动化与编码约束 |

## 设计与决策

| 载体 | 说明 |
|------|------|
| [adr/](adr/README.md) | 架构决策记录 |
| [design/](design/README.md) | 生成器 API、诊断、契约、实现与权衡 |

## 生成器索引

| 生成器 | Design Doc |
|--------|------------|
| ObservableProperty | [design/ObservableProperty.md](design/ObservableProperty.md) |
| DelegateCommand | [design/DelegateCommand.md](design/DelegateCommand.md) |
| BindableBase | [design/BindableBase.md](design/BindableBase.md) |
| BindableValidator | [design/BindableValidator.md](design/BindableValidator.md) |
| Register | [design/Register.md](design/Register.md) |
| NavigationAware | [design/NavigationAware.md](design/NavigationAware.md) |
| DialogAware | [design/DialogAware.md](design/DialogAware.md) |
| RegionNavigation | [design/RegionNavigation.md](design/RegionNavigation.md) |
| DialogServiceCommand | [design/DialogServiceCommand.md](design/DialogServiceCommand.md) |
| 架构与打包 | [design/Architecture.md](design/Architecture.md) |

修改诊断 ID、生成器产出或公共 API 时，同步更新 `DiagnosticDescriptors.cs`、`AnalyzerReleases.*.md`、对应 Design Doc、`CHANGELOG.md`，以及（用户可见时）Prism.SourceGenerators.Docs 诊断页。
