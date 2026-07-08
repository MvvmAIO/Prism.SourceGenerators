# Spec: BindableBase

> **版本**：v0.8.1
> **关联 Design Doc**：[design/BindableBase.md](../design/BindableBase.md)

## API 面

`[BindableBase]` 于未继承 `Prism.Mvvm.BindableBase` 的 **partial class**：生成 `INotifyPropertyChanged` + `SetProperty` / `RaisePropertyChanged` / `OnPropertyChanged`。

若已继承 `BindableBase` 或层级中已有 `INotifyPropertyChanged` → **不生成**。

## 诊断 ID

| ID | 说明 |
|----|------|
| PSG0004 | 类非 partial |

## 不变量

- 始终考虑 `INotifyPropertyChanging`（可通过 `FeatureSwitches` 关闭运行时行为，见 Design Doc）。
