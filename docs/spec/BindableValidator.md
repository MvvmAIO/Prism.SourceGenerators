# Spec: BindableValidator

> **版本**：v0.8.1
> **关联 Design Doc**：[design/BindableValidator.md](../design/BindableValidator.md)

## API 面

| 特性 | 说明 |
|------|------|
| `[BindableValidator]` | 生成 `BindableValidator` 基类实现（`INotifyDataErrorInfo`） |
| `[NotifyDataErrorInfo]` | 与 `[ObservableProperty]` 联用或类级；setter 内 `ValidateProperty` |

类型须继承 **`BindableValidator`**（原 ObservableValidator，0.4.0 更名）。

## 诊断 ID

| ID | 说明 |
|----|------|
| PSG0005 | `[BindableValidator]` 类非 partial |
| PSG0006 | `[BindableValidator]` 仅用于 class |
| PSG5001 | `[NotifyDataErrorInfo]` 但基类非 BindableValidator |

## 不变量

- `ValidationAttribute` 在 partial property 模式保留于用户声明，不重复到生成 partial。
