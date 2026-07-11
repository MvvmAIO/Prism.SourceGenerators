# 产品与技术路线图

功能与工程 backlog。生成器 API、诊断与实现契约见对应 [Design Doc](design/README.md)；开发规范见 [`../AGENTS.md`](../AGENTS.md)。

当前稳定版：**0.8.1**。诊断 ID 前缀 **PSG**；已发布 ID 不复用。

---

## 主线：导航与对话框（0.6 – 0.8，已完成）

| 项 | 说明 | 版本 | 状态 |
|----|------|------|------|
| `[NavigationAware]` / `[DialogAware]` | 生命周期 partial 钩子 + Prism 8/9 命名空间 | 0.6.0 | [x] |
| `[NavigateCommand]` / `[NavigateOnChanged]` | `IRegionManager.RequestNavigate` | 0.7.0 | [x] |
| `[ShowDialogCommand]` | `IDialogService.ShowDialog` | 0.7.0 | [x] |
| `[FromNavigationParameter]` / `[FromDialogParameter]` | 类型化参数绑定 | 0.8.0 | [x] |
| CS0579 修复（partial property 模式） | 参数特性不再重复转发 | 0.8.1 | [x] |
| Samples 参数绑定演示 | Dashboard / ConfirmDialog | Samples main | [x] |

---

## F1 — 发版与文档同步（近期）

| 项 | 说明 | 状态 |
|----|------|------|
| GitHub Release v0.8.1 | tag 已有，Release 待创建 | [ ] |
| Docs 站 getting-started 版本号 | 仍为 0.6.0，需升至 0.8.1 | [ ] |
| Docs 站历史页面状态更新 | P4 已实现，页面仍标 deferred | [ ] |
| 本仓库 `docs/` 文档体系精简 | Design Doc 合并契约，移除重复流程文档 | [x] |

---

## F2 — 导航/对话框增强（下一版本候选）

源自已完成的导航与对话框设计，以及 Docs 站历史页面。

| 项 | 说明 | 优先级 | 状态 |
|----|------|--------|------|
| `[DialogAware]` CloseDialog 辅助 | Prism 9 `DialogCloseListener` 简化 | 高 | [ ] |
| `IRegionMemberLifetime`（KeepAlive） | 页面缓存生成 | 中 | [ ] |
| Region 名称常量生成 | 减少字符串拼写错误 | 低 | [ ] |
| `IConfirmNavigationRequest` | 未保存更改提示 | 暂缓 | [ ] |
| `IJournalAware` | 返回栈集成 | 暂缓 | [ ] |

每项落地时：更新 Design Doc；有破坏性架构决策时新增 ADR；按需更新 Samples 与 Docs 站。

---

## F3 — MVVM 核心增强（候选）

| 项 | 说明 | 状态 |
|----|------|------|
| 更多 CodeFix 覆盖 | 除 MakePartial 外的可机械修复诊断 | [ ] |
| PSG6001 批量转换 | field → partial property 工作流打磨 | [ ] |
| 生成器性能剖析 | 大型解决方案编译期开销 | [ ] |

---

## 工程与维护

| 项 | 说明 | 状态 |
|----|------|------|
| Polyfill / 测试依赖 Dependabot | 已合并 #92 等 | [x] |
| Avalonia 12.0.5（Samples Prism8） | Dependabot #14–16 | [x] |
| `wiki/` 与 `docs/` 分工说明 | wiki 保留导读，深度迁入 docs/ | [~] |

---

## 已完成（归档）

| 项 | 版本 |
|----|------|
| ObservableProperty / DelegateCommand / BindableBase | 0.1.x – 0.3.x |
| `[Register]` 容器注册 | 0.3.0 |
| BindableValidator / `[NotifyDataErrorInfo]` | 0.4.0 |
| `Task<TResult>` / ValueTask 命令 | 0.5.0 |
| Bcl.Commands 独立包 | 0.2.0 |
| 语法树发射重构 | 0.3.0 |
| AnalyzerReleases 跟踪 | 0.5.1 |

---

## 明确不做

- IL weaving、运行时容器、XAML 改写
- 完整导航图 / 状态机生成
- 跨 MAUI 与桌面统一的 `INavigationService` 抽象（与 Region-first 桌面模型差异过大）
