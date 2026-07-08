# Spec 索引

生成器**稳定契约**（特性、产出、诊断、不变量）。变更须 RFC + ADR。模板：[_template.md](_template.md)

| 生成器 | Spec | 主要特性 |
|--------|------|----------|
| ObservableProperty | [ObservableProperty.md](ObservableProperty.md) | `[ObservableProperty]`、`[NotifyPropertyChangedFor]`、`[NotifyCanExecuteChangedFor]` |
| DelegateCommand | [DelegateCommand.md](DelegateCommand.md) | `[DelegateCommand]`、`[AsyncDelegateCommand]`、`[ObservesProperty]` |
| BindableBase | [BindableBase.md](BindableBase.md) | `[BindableBase]` |
| BindableValidator | [BindableValidator.md](BindableValidator.md) | `[BindableValidator]`、`[NotifyDataErrorInfo]` |
| Register | [Register.md](Register.md) | `[Register]` |
| NavigationAware | [NavigationAware.md](NavigationAware.md) | `[NavigationAware]`、`[FromNavigationParameter]` |
| DialogAware | [DialogAware.md](DialogAware.md) | `[DialogAware]`、`[FromDialogParameter]` |
| RegionNavigation | [RegionNavigation.md](RegionNavigation.md) | `[NavigateCommand]`、`[NavigateOnChanged]` |
| DialogServiceCommand | [DialogServiceCommand.md](DialogServiceCommand.md) | `[ShowDialogCommand]` |

实现细节见 [design/](../design/README.md)。用户向说明见 [Prism.SourceGenerators.Docs](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/generators/)。
