# Spec: ObservableProperty

> **版本**：v0.8.1
> **关联 Design Doc**：[design/ObservableProperty.md](../design/ObservableProperty.md)
> **关联 ADR**：[ADR-005](../adr/ADR-005-syntax-tree-emission.md)、[ADR-006](../adr/ADR-006-incremental-generators.md)

## API 面

### 特性

| 特性 | 目标 | 说明 |
|------|------|------|
| `[ObservableProperty]` | field 或 partial property | 生成 `SetProperty` 属性；field 模式可用 `PropertyAccess` |
| `[NotifyPropertyChangedFor]` | 同成员 | 额外 `RaisePropertyChanged` |
| `[NotifyCanExecuteChangedFor]` | 同成员 | 命令 `RaiseCanExecuteChanged` |
| `[NotifyDataErrorInfo]` | 成员或类 | 见 [BindableValidator](BindableValidator.md) |

命名空间：`Prism.SourceGenerators`（**MvvmAIO.Prism.Core**）。

### 生成器产出

- 属性 getter/setter（field 或 `field` 关键字实现）
- `On{Name}Changing` / `On{Name}Changed` 四个 partial 重载
- 可选 `*.ObservablePropertyChanging.g.cs`（未继承 BindableBase 时）
- 用户特性转发（`ValidationAttribute` 在 partial property 模式不重复）

## 诊断 ID

| ID | 级别 | 触发条件 |
|----|------|----------|
| PSG0001 | Error | 含 `[ObservableProperty]` 的类非 partial |
| PSG0003 | Error | partial property 目标未声明 partial |
| PSG2005 | Warning | `[NotifyCanExecuteChangedFor]` 命令未解析 |
| PSG6001 | Info | 建议 field → partial property（C# 13+） |

## 不变量

1. Setter 经 `BindableBase.SetProperty`（或生成 BindableBase 路径）更新存储。
2. 相等性快速路径使用 `EqualityComparer<T>.Default`。
3. `OnChanging` 在写入前；`OnChanged` 在 `SetProperty` 回调内。
4. 生成器自有特性不参与转发；`FromNavigationParameter` / `FromDialogParameter` 在 partial property 模式亦不转发。

## 兼容基线

- C# 12−：field 后备
- C# 13+：partial property + `field`
- C# 14+：命令侧 `field`（见 DelegateCommand Spec）

## 不在范围内

- 跨属性验证规则引擎（由 BindableValidator + DataAnnotations 负责）
