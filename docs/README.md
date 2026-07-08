# 内部文档索引

本目录面向 **MvvmAIO/Prism.SourceGenerators 维护者与贡献者**（中文为主）。面向库使用者的多语言手册见 **[Prism.SourceGenerators.Docs](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs)**（站点：[mvvmaio.github.io/Prism.SourceGenerators.Docs](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/)）。

> **文档体系标准**：[DOCUMENTATION.md](DOCUMENTATION.md) — 定义所有文档的类型、结构、生命周期与归档规则。人类开发者和编码助手均须遵守。

## 入门

| 文档 | 说明 |
|------|------|
| [DOCUMENTATION.md](DOCUMENTATION.md) | **文档体系标准**（类型、生命周期、模板、归档、工作流） |
| [DEVELOPMENT.md](DEVELOPMENT.md) | 环境、构建、测试、仓库布局 |
| [CONTRIBUTING.md](../CONTRIBUTING.md) | 贡献流程、测试与 Roslyn 组件结构 |
| [PUBLISHING.md](PUBLISHING.md) | NuGet 打包与发布流程 |
| [ROADMAP.md](ROADMAP.md) | 功能与技术 backlog |
| [../AGENTS.md](../AGENTS.md) | 编码助手与自动化约束（权威源） |

## 设计提案与决策

| 目录 | 说明 |
|------|------|
| [rfc/](rfc/README.md) | RFC — 设计提案与讨论记录 |
| [adr/](adr/README.md) | ADR — 架构决策记录 |

## 计划与评审

| 目录 | 说明 |
|------|------|
| [plans/](plans/README.md) | Plan — 大型任务计划（跨多 PR） |
| [review/](review/README.md) | Review — 评审记录（设计/实现/发版/回顾） |

## 规范与设计文档

| 目录 | 说明 |
|------|------|
| [spec/](spec/README.md) | Spec — 生成器稳定契约（特性、诊断 ID、不变量） |
| [design/](design/README.md) | Design Doc — 实现细节、设计权衡、已知局限 |

## 生成器索引

| 生成器 | Spec | Design Doc |
|--------|------|------------|
| ObservableProperty | [spec/ObservableProperty.md](spec/ObservableProperty.md) | [design/ObservableProperty.md](design/ObservableProperty.md) |
| DelegateCommand | [spec/DelegateCommand.md](spec/DelegateCommand.md) | [design/DelegateCommand.md](design/DelegateCommand.md) |
| BindableBase | [spec/BindableBase.md](spec/BindableBase.md) | [design/BindableBase.md](design/BindableBase.md) |
| BindableValidator | [spec/BindableValidator.md](spec/BindableValidator.md) | [design/BindableValidator.md](design/BindableValidator.md) |
| Register | [spec/Register.md](spec/Register.md) | [design/Register.md](design/Register.md) |
| NavigationAware | [spec/NavigationAware.md](spec/NavigationAware.md) | [design/NavigationAware.md](design/NavigationAware.md) |
| DialogAware | [spec/DialogAware.md](spec/DialogAware.md) | [design/DialogAware.md](design/DialogAware.md) |
| RegionNavigation | [spec/RegionNavigation.md](spec/RegionNavigation.md) | [design/RegionNavigation.md](design/RegionNavigation.md) |
| DialogServiceCommand | [spec/DialogServiceCommand.md](spec/DialogServiceCommand.md) | [design/DialogServiceCommand.md](design/DialogServiceCommand.md) |
| 架构与打包 | — | [design/Architecture.md](design/Architecture.md) |

## 与其他文档面的分工

| 受众 | 位置 | 语言 | 角色 |
|------|------|------|------|
| 库使用者 | **Prism.SourceGenerators.Docs** | 英 / 中 / 日 | **对外权威手册**（安装、示例、PSG 参考） |
| 维护者 | 本目录 `docs/` | 中文为主 | **内部契约与实现**（Spec / Design / ADR） |
| 快速导读 | `README` / `wiki/` | 中 / 英 | 仓库首屏与 Wiki 条目化笔记，**非** Spec 合同 |
| 自动化约束 | `AGENTS.md` | 中 / 英摘要 | 构建、CI、工作流 |

修改诊断 ID、生成器产出或公共 API 时，同步更新：`DiagnosticDescriptors.cs`、`AnalyzerReleases.*.md`、**Spec**、**CHANGELOG**、以及（用户可见时）**Prism.SourceGenerators.Docs** 诊断页。
