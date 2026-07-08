# RFC: 导航与对话框高级契约

> **状态**：Implemented（P4 已交付；F2 余项见 ROADMAP）
> **类型**：Feature
> **创建**：2026-06-17
> **更新**：2026-07-08
> **作者**：MvvmAIO 维护者
> **关联 Roadmap**：F2（未完成项）
> **关联 Issue**：
> **衍生 ADR**：[ADR-004](../adr/ADR-004-prism8-prism9-api-resolution.md)

## 摘要

在 0.7.0 导航/对话框调用层生成器基础上，评估类型化参数绑定与进阶 Prism 契约；**0.8.0** 交付 `[FromNavigationParameter]` / `[FromDialogParameter]`，**0.8.1** 修复 partial property 模式 CS0579。

## 动机

- 手写 `TryGetValue` 易错且重复
- Samples 需要端到端演示以验证 PSG7xxx 诊断带

## 非目标

- IL weaving、XAML 改写、完整导航图生成
- 跨 MAUI / 桌面的统一导航抽象

## 设计方案（已交付部分）

| 特性 | 行为 | 版本 |
|------|------|------|
| `[FromNavigationParameter]` | `OnNavigatedTo` 前从 `NavigationContext.Parameters` 绑定 | 0.8.0 |
| `[FromDialogParameter]` | `OnDialogOpened` 前从 `IDialogParameters` 绑定 | 0.8.0 |
| PSG7006–7008 / PSG7103–7105 | 参数特性校验 | 0.8.0 |
| 抑制参数特性转发（partial property） | 避免 CS0579 | 0.8.1 |

## 尚未实现（见 ROADMAP F2）

| 想法 | 建议 |
|------|------|
| `CloseDialog` helper | 高优先级跟进 |
| `IRegionMemberLifetime` | 中 |
| Region 名称常量 | 低 |
| `IConfirmNavigationRequest` / `IJournalAware` | 暂缓 |

## 验收（P4，已满足）

1. [x] Samples 演示参数绑定（Dashboard / ConfirmDialog）
2. [x] PSG7xxx 诊断一致
3. [x] Prism 8/9 集成测试

## 决策记录

- **Accepted** P4 参数绑定；其余 F2 项滚动至 ROADMAP，不阻塞 0.8.x 发版。

## 参考

- [Spec: NavigationAware](../spec/NavigationAware.md)
- [Spec: DialogAware](../spec/DialogAware.md)
- [CHANGELOG 0.7.0 – 0.8.1](../../CHANGELOG.md)
