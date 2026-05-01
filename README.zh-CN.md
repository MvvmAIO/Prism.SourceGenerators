# Prism.SourceGenerators

[English](README.md) | **简体中文** | [日本語](README.ja.md)

为 [Prism](https://github.com/PrismLibrary/Prism) MVVM 库提供的 Roslyn 源生成器。

## CI 状态

[![.NET](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)
[![Tests](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/MvvmAIO/Prism.SourceGenerators/master/.github/badges/tests.json)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)

- 点击上方工作流可查看最新构建状态。
- `Tests` 徽章会直接显示最新通过/失败/跳过数量。
- 运行还会上传 `test-results`（`.trx`）制品，可用于查看详细测试报告。

## 项目结构

```
Prism.SourceGenerators/                        # 共享项目（.shproj/.projitems/.props + 源代码）
Prism.SourceGenerators.Roslyn4001/             # Roslyn 4.0.1
Prism.SourceGenerators.Roslyn4031/             # Roslyn 4.3.1
Prism.SourceGenerators.Roslyn4120/             # Roslyn 4.12.0
Prism.SourceGenerators.Roslyn5000/             # Roslyn 5.0.0
Prism.Core/                                    # MvvmAIO.Prism.Core（特性），随 MvvmAIO.Prism.SourceGenerators 打包
Prism.SourceGenerators.Core.Prism8/           # MvvmAIO.Prism.Core.Prism8（Prism 8 AsyncDelegateCommand），在 Prism.Core 8.1.97 时选用
Prism.SourceGenerators.Samples.Prism9/         # Avalonia 12 示例（Prism 9.0，原生 AsyncDelegateCommand）
Prism.SourceGenerators.Samples.Prism8/         # Avalonia 12 示例（Prism 8.1.97；与 NuGet 包相同的 MSBuild 程序集选择）
```

## 生成器

### `[ObservableProperty]`

为继承自 `BindableBase` 的类生成可观察属性。根据 C# 语言版本支持两种使用模式。

#### 字段目标（所有 C# 版本）

在私有字段上标注 `[ObservableProperty]`，生成调用 `SetProperty` 的公共属性。

```csharp
// C# 12 或更早版本
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Hello";

    // 生成：public string Title { get => _title; set => SetProperty(ref _title, value); }
}
```

#### 部分属性目标（C# 13+ `field` 关键字）

在 `partial` 属性上标注 `[ObservableProperty]`，使用 `field` 关键字（半自动属性）生成实现声明。

```csharp
// C# 13+ / .NET 9+（需要 LangVersion 13.0+ 或 preview）
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = "Hello";

    // 生成：public partial string Title { get => field; set => SetProperty(ref field, value); }
}
```

部分属性方式无需单独的后备字段，提供更简洁的 API 接口。两种模式可以在同一项目中共存。

#### OnChanged 部分方法

每个 `[ObservableProperty]` 都会生成两个 `partial` 方法声明，可选择性实现以响应变化：

```csharp
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial int Age { get; set; }

    // 生成的声明（可实现其中一个或两个）：
    // partial void OnAgeChanged(int value);
    // partial void OnAgeChanged(int oldValue, int newValue);

    partial void OnAgeChanged(int oldValue, int newValue)
    {
        Debug.WriteLine($"Age 从 {oldValue} 变为 {newValue}");
    }
}
```

生成的 setter 使用 `EqualityComparer<T>.Default.Equals` 进行变化检测，在触发 `PropertyChanged` 之前调用两个 `OnChanged` 重载。

### `[NotifyPropertyChangedFor]`

与 `[ObservableProperty]` 一起使用，在被标注的属性变化时自动为其他依赖属性触发 `PropertyChanged`。

```csharp
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string _firstName = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string _lastName = "";

    public string FullName => $"{FirstName} {LastName}";
}
```

支持通过 `[NotifyPropertyChangedFor(nameof(A), nameof(B))]` 指定多个属性名，也支持多次标注。

### `[DelegateCommand]`

从方法生成 `DelegateCommand` 或 `AsyncDelegateCommand` 属性。

- **同步方法**（`void`）生成 `DelegateCommand` / `DelegateCommand<T>`
- **异步方法**（`Task`）生成 `AsyncDelegateCommand` / `AsyncDelegateCommand<T>`
- 对于 Prism &lt; 9.0，请使用 NuGet **`MvvmAIO.Prism.SourceGenerators`**：它会添加 **`MvvmAIO.Prism.Core`**，并在引用 **`Prism.Core` 8.1.97** 时添加 **`MvvmAIO.Prism.Core.Prism8`**，以便存在 `AsyncDelegateCommand`。若使用异步命令却缺少上述程序集，将报告 **PSG3002**。
- **C# 14+**：Command 属性使用 `field` 关键字（无需单独后备字段）
- **C# 13 及更早版本**：Command 属性使用传统后备字段

```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    // 生成：DelegateCommand IncrementCommand
    [DelegateCommand]
    private void Increment() { /* ... */ }

    // 生成：AsyncDelegateCommand LoadDataCommand
    [DelegateCommand]
    private async Task LoadDataAsync() { /* ... */ }

    // 支持 CanExecute
    [DelegateCommand(CanExecute = nameof(CanSubmit))]
    private void Submit() { /* ... */ }
    private bool CanSubmit() => true;
}
```

#### 生成代码对比

**C# 14+（LangVersion >= 14）**— 使用 `field` 关键字：
```csharp
// 无需后备字段
public DelegateCommand IncrementCommand => field ??= new DelegateCommand(Increment);
```

**C# 13 及更早版本** — 传统后备字段：
```csharp
private DelegateCommand? _incrementCommand;
public DelegateCommand IncrementCommand => _incrementCommand ??= new DelegateCommand(Increment);
```

### `[AsyncDelegateCommand]`

专用于异步方法的特性，提供与 Prism 一致的进阶能力。
在 Prism 9+ 上使用框架内置类型；在 Prism 8.1.97 上，**`MvvmAIO.Prism.SourceGenerators`** 包会应用 **`MvvmAIO.Prism.Core.Prism8`**，以获得相同的链式配置：`EnableParallelExecution`、`CancelAfter`、`Catch`、`CancellationTokenSourceFactory`、`ObservesCanExecute`。

```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    // 启用并行执行
    [AsyncDelegateCommand(EnableParallelExecution = true)]
    private async Task FetchDataAsync() { /* ... */ }

    // 错误处理 + CanExecute
    [AsyncDelegateCommand(CanExecute = nameof(CanSave), Catch = nameof(HandleError))]
    private async Task SaveAsync() { /* ... */ }

    private bool CanSave() => true;
    private void HandleError(Exception ex) { /* ... */ }
}
```

### `[ObservesProperty]`

当指定属性变化时自动重新计算 `CanExecute`。
同时支持 `[DelegateCommand]` 和 `[AsyncDelegateCommand]`。

```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private bool _isValid;

    [DelegateCommand(CanExecute = nameof(CanSubmit))]
    [ObservesProperty(nameof(IsValid))]
    private void Submit() { /* ... */ }

    // 多个属性
    [AsyncDelegateCommand(CanExecute = nameof(CanSave))]
    [ObservesProperty(nameof(Counter), nameof(IsActive))]
    private async Task SaveAsync() { /* ... */ }
}
```

### `[BindableBase]`

应用于**未**继承 `Prism.Mvvm.BindableBase` 的类，自动生成 `INotifyPropertyChanged` 实现。生成的代码包含 `PropertyChanged` 事件、`SetProperty<T>`、`RaisePropertyChanged` 和 `OnPropertyChanged` 方法。

```csharp
using Prism.SourceGenerators;

[BindableBase]
public partial class SimpleViewModel
{
    private string _message = "Hello!";

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }
}
```

如果类已经继承了 `BindableBase` 或其基类已实现 `INotifyPropertyChanged`，则不会生成任何代码。

## 诊断

| ID | 描述 |
|----|------|
| PSG0001 | 包含 `[ObservableProperty]` 成员的类必须声明为 `partial` |
| PSG0002 | 包含 `[DelegateCommand]` / `[AsyncDelegateCommand]` 方法的类必须声明为 `partial` |
| PSG0003 | 标注 `[ObservableProperty]` 的属性必须声明为 `partial` |
| PSG0004 | 标注 `[BindableBase]` 的类必须声明为 `partial` |
| PSG1001 | `[DelegateCommand]` 方法签名无效 |
| PSG1002 | `[AsyncDelegateCommand]` 方法签名无效 |
| PSG2001 | 未找到 Catch 处理程序成员 |
| PSG2002 | Catch 处理程序签名不兼容 |
| PSG2003 | 未找到 CanExecute 成员 |
| PSG2004 | 未找到被观察的属性 |
| PSG3002 | 未找到 `AsyncDelegateCommand`；在 Prism.Core 8.1.97 上请使用 **`MvvmAIO.Prism.SourceGenerators`**（NuGet）（或升级到 Prism 9+） |

## 安装

```xml
<PackageReference Include="MvvmAIO.Prism.SourceGenerators" Version="0.1.7" />
```

或：

```bash
dotnet add package MvvmAIO.Prism.SourceGenerators
```

## 构建

```bash
dotnet build Prism.SourceGenerators.slnx
```

## Nuke 构建

本仓库使用 [Nuke](https://nuke.build/) 作为本地自动化与 CI 的构建编排层。

- 主源码解决方案：`Prism.SourceGenerators.slnx`
- 构建自动化解决方案：`build.slnx`（仅包含 `build/_build.csproj`）

常用命令：

```bash
# 本地执行 CI 流程（clean + restore + compile + test）
dotnet run --project build/_build.csproj -- --target Ci --configuration Release

# 打包 NuGet（可选覆盖版本号）
dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version 0.1.7

# 发布 NuGet（MvvmAIO.Prism.SourceGenerators，内含 MvvmAIO.Prism.Core 程序集）
dotnet run --project build/_build.csproj -- --target Publish --configuration Release --version 0.1.7 --nuget-api-key <NUGET_API_KEY>
```

## 要求

- .NET 10 SDK
- Visual Studio 2022 17.13+ / Rider / VS Code with C# Dev Kit（支持 `.slnx`）
