# Spec: DialogServiceCommand

> **版本**：v0.8.1
> **关联 Design Doc**：[design/DialogServiceCommand.md](../design/DialogServiceCommand.md)
> **关联 ADR**：[ADR-004](../adr/ADR-004-prism8-prism9-api-resolution.md)

## API 面

| 特性 | 说明 |
|------|------|
| `[ShowDialogCommand(Name = "...")]` | `IDialogService.ShowDialog` + 可选 `On{Name}DialogClosed` partial |

## 诊断 ID

| ID | 说明 |
|----|------|
| PSG7101 | 缺少 `IDialogService` |
| PSG7102 | 对话框 `Name` 必填 |

## 不变量

1. 生成 `DelegateCommand`，非 async 包装除非 execute 方法本身异步。
2. Prism 8 / 9 服务接口命名空间探测。

## 不在范围内

- 对话框 View 注册（`RegisterDialog` 在 View code-behind）
