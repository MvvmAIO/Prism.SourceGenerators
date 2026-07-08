# Spec: NavigationAware

> **版本**：v0.8.1
> **关联 Design Doc**：[design/NavigationAware.md](../design/NavigationAware.md)
> **关联 ADR**：[ADR-004](../adr/ADR-004-prism8-prism9-api-resolution.md)

## API 面

| 特性 | 说明 |
|------|------|
| `[NavigationAware]` | 类级；生成 `INavigationAware` 成员 + `*Core` partial 钩子 |
| `[FromNavigationParameter(key)]` | field / partial property + `[ObservableProperty]`；`OnNavigatedTo` 前绑定 |

### 生成器产出

- `OnNavigatedTo` / `OnNavigatedFrom` / `IsNavigationTarget`
- `OnNavigatedToCore` / `OnNavigatedFromCore` / `IsNavigationTargetCore` partial
- 参数绑定：`TryGetValue<T>` → 属性 setter

## 诊断 ID

| ID | 级别 |
|----|------|
| PSG0007 | Error — 类非 partial |
| PSG7006–7008 | `[FromNavigationParameter]` 目标 / ObservableProperty / 空 key |

## 不变量

1. 接口命名空间由引用程序集探测（Prism 8 `Prism.Regions` / Prism 9 `Prism.Navigation.Regions`）。
2. 参数绑定在 `OnNavigatedToCore` **之前**执行。

## 不在范围内

- 视图注册（`RegisterForNavigation` 在 View code-behind）
