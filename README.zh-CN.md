# Prism.SourceGenerators

[English](README.md) | **简体中文** | [日本語](README.ja.md)

为 [Prism](https://github.com/PrismLibrary/Prism) MVVM 库提供的 Roslyn 源生成器。

参与贡献见 [CONTRIBUTING.md](CONTRIBUTING.md)。自动化代理与 Cursor 的标准约束见 [AGENTS.md](AGENTS.md)（[中文摘要](AGENTS.zh-CN.md)）。

## CI 状态

[![.NET](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)
[![Tests](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/MvvmAIO/Prism.SourceGenerators/master/.github/badges/tests.json)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)

- 点击上方工作流可查看最新构建状态。
- `Tests` 徽章会直接显示最新通过/失败/跳过数量。
- 运行还会上传 `test-results`（`.trx`）制品，可用于查看详细测试报告。

## 文档说明（README、Wiki、文档站点）

| 渠道 | 说明 |
|------|------|
| **[文档站点](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/)**（[源码仓库](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs)） | **权威**：多语言、完整生成器参考、**PSG** 诊断表、架构与 CI 等。深读与交叉链接请以本站为准。 |
| **本 README** / [English](README.md) / [日本語](README.ja.md) | 仓库首屏简介与常用代码片段，**不作**完整手册。 |
| **[GitHub Wiki](https://github.com/MvvmAIO/Prism.SourceGenerators/wiki)**（主仓库 [`wiki/`](https://github.com/MvvmAIO/Prism.SourceGenerators/tree/master/wiki) 目录同步，便于 PR） | 中文导读与条目化笔记，**不是**编译器诊断或 API 的合同文本。 |

## 项目结构

```
Prism.SourceGenerators/                        # 共享项目（.shproj/.projitems/.props + 源代码）
Prism.SourceGenerators.Roslyn4001/             # Roslyn 4.0.1
Prism.SourceGenerators.Roslyn4031/             # Roslyn 4.3.1
Prism.SourceGenerators.Roslyn4120/             # Roslyn 4.12.0
Prism.SourceGenerators.Roslyn5000/             # Roslyn 5.0.0
Prism.SourceGenerators.Core/                   # MvvmAIO.Prism.Core（特性），随 MvvmAIO.Prism.SourceGenerators 打包
Prism.Bcl.Commands/                            # MvvmAIO.Prism.Bcl.Commands（Prism 8 AsyncDelegateCommand 包，需手动安装）
```

示例（Avalonia）：独立仓库 [Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples) — Prism 8 / Prism 9 演示应用，通过 NuGet 引用 **`MvvmAIO.Prism.SourceGenerators`**。

## 生成器

### `[ObservableProperty]`

为继承自 `BindableBase` 的类生成可观察属性。根据 C# 语言版本支持两种使用模式。

#### 字段目标（所有 C# 版本）

在私有字段上标注 `[ObservableProperty]`，生成调用 `SetProperty` 的属性；**默认**生成 **`public`**。可通过 **`PropertyAccess`** 的位置参数或命名参数 `PropertyAccess = …` 指定为 `internal`、`protected`、`private`、`protected internal`、`private protected` 等。

```csharp
// C# 12 或更早版本
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Hello";

    [ObservableProperty(PropertyAccess.Internal)]
    // 或: [ObservableProperty(PropertyAccess = PropertyAccess.Internal)]
    private int _count;

    // 生成：setter 中 OnTitleChanging*、BindableBase.SetProperty(ref _title, value, () => { OnTitleChanged*; })，
    // 以及可选的 [NotifyPropertyChangedFor] / 命令刷新等。
}
```

**部分属性**目标以属性声明上的访问修饰符为准；`PropertyAccess` 会被忽略。

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

#### OnChanging / OnChanged 部分方法

每个 `[ObservableProperty]` 都会生成四个 `partial` 方法声明，可选择性实现以响应变化。`OnXxxChanging` 钩子在写入字段**之前**触发，`OnXxxChanged` 钩子在写入**之后**触发：

```csharp
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial int Age { get; set; }

    // 生成的声明（可实现任意子集）：
    // partial void OnAgeChanging(int value);
    // partial void OnAgeChanging(int oldValue, int newValue);
    // partial void OnAgeChanged(int value);
    // partial void OnAgeChanged(int oldValue, int newValue);

    partial void OnAgeChanging(int oldValue, int newValue)
    {
        Debug.WriteLine($"Age 即将从 {oldValue} 变为 {newValue}");
    }

    partial void OnAgeChanged(int oldValue, int newValue)
    {
        Debug.WriteLine($"Age 从 {oldValue} 变为 {newValue}");
    }
}
```

生成的 setter 先用 `EqualityComparer<T>.Default.Equals` 做快速相等判断。值确实变化时，先调用两个 `OnChanging` 重载，再通过 `SetProperty(ref storage, value, onChanged)` 更新存储并走与手写 Prism 属性一致的 `BindableBase` 路径（可覆写 `SetProperty`）。`onChanged` 回调内调用两个 `OnChanged` 重载，随后由 `SetProperty` 触发主属性的 `PropertyChanged`。`[NotifyPropertyChangedFor]` 与 `[NotifyCanExecuteChangedFor]` 的额外通知在该调用之后发出。

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

### `[NotifyCanExecuteChangedFor]`

与 `[ObservableProperty]` 一起使用，当被标注的属性变化时自动调用指定命令的 `RaiseCanExecuteChanged()`。

```csharp
public partial class EditorViewModel : BindableBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = "";

    [DelegateCommand(CanExecute = nameof(CanSave))]
    private void Save() { /* ... */ }

    private bool CanSave() => !string.IsNullOrEmpty(Name);
}
```

生成的 setter 会在 `RaisePropertyChanged` 之后调用 `SaveCommand?.RaiseCanExecuteChanged()`。可以使用 `[NotifyCanExecuteChangedFor(nameof(A), nameof(B))]` 一次指定多个命令，或多次标注。命名既可以是类型上已有的成员，也可以是 `[DelegateCommand]` / `[AsyncDelegateCommand]` 方法生成的命令属性（例如方法 `Save` 生成 `SaveCommand`）。如果名称无法解析，会报告 **PSG2005**（警告），但 setter 仍会生成。

### 转发属性到生成的属性

对于**字段**目标，与字段写在同一特性列表中、**无目标**或 **`[property: …]`** 的属性会转发到生成的属性上（生成器自有属性 `[ObservableProperty]`、`[NotifyPropertyChangedFor]`、`[NotifyCanExecuteChangedFor]`、`[NotifyDataErrorInfo]` 会被过滤）。显式 **`[field: …]`** 目标的列表仅保留在后备字段上。

```csharp
public partial class Vm : BindableBase
{
    [ObservableProperty]
    [System.ComponentModel.DataAnnotations.Required] // 转发（校验 / DataAnnotations）
    [property: System.Text.Json.Serialization.JsonIgnore] // 转发
    private string _password = "";
}
```

会生成

```csharp
[global::System.ComponentModel.DataAnnotations.RequiredAttribute]
[global::System.Text.Json.Serialization.JsonIgnoreAttribute]
public string Password { get { ... } set { ... } }
```

对于**部分属性（partial property）**目标，继承自 **`ValidationAttribute`** 的特性（如 `[Required]`、`[EmailAddress]`、`[Range]`）只保留在**你写的** partial 声明上；生成器**不会**再抄写到实现 partial，这样 **`Validator`** / **`BindableValidator`** 仍只见到一份元数据，并避免 **CS0579** 特性重复。其它特性（如 `[JsonIgnore]`）仍会转发到生成的实现声明上。生成器自有特性（`[ObservableProperty]`、`[NotifyPropertyChangedFor]`、`[NotifyCanExecuteChangedFor]`、`[NotifyDataErrorInfo]`）会被过滤。转发的属性以完全限定类型名输出，因此不依赖生成文件中的 `using` 指令。

> 转发属性的参数表达式会按原样输出。如果生成文件无法看到 `using` 指令，请使用字面量 / `nameof` / `typeof`，或在参数位置使用完全限定的类型引用。

### `[DelegateCommand]`

从方法生成 `DelegateCommand` 或 `AsyncDelegateCommand` 属性。

- **同步方法**（`void`）生成 `DelegateCommand` / `DelegateCommand<T>`
- **异步方法**若返回 **`Task`**、**`Task<TResult>`**、**`ValueTask`** 或 **`ValueTask<TResult>`**，则生成 `AsyncDelegateCommand` / `AsyncDelegateCommand<T>`。`ValueTask` / `ValueTask<TResult>` 在生成代码中通过 `.AsTask()` 接到 Prism 的 `Func<Task>` / `Func<T, Task>` 构造函数；**`Task<TResult>`** 通过 `async` lambda 等待 execute 方法。若 execute 方法带有 **`CancellationToken`** 参数，则不能返回 `ValueTask`、`ValueTask<TResult>` 或 **`Task<TResult>`**（**PSG1001**）。
- 对于 Prism &lt; 9.0，请使用 NuGet **`MvvmAIO.Prism.SourceGenerators`**：它会添加 **`MvvmAIO.Prism.Core`**，用于提供生成器特性定义。若使用 Prism.Core 8.1.97 的异步命令，请手动安装 **`MvvmAIO.Prism.Bcl.Commands`**，以便存在 `AsyncDelegateCommand`。若使用异步命令却缺少上述程序集，将报告 **PSG3002**。
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
在 Prism 9+ 上使用框架内置类型；在 Prism 8.1.97 上，请安装 **`MvvmAIO.Prism.Bcl.Commands`** 以获得相同的链式配置：`EnableParallelExecution`、`CancelAfter`、`Catch`、`CancellationTokenSourceFactory`、`ObservesCanExecute`。

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

### `[NotifyDataErrorInfo]`（验证）

通过 `INotifyDataErrorInfo` 启用属性验证支持。将 `[NotifyDataErrorInfo]` 应用于单个字段/属性（与 `[ObservableProperty]` 一起使用），或应用于类本身以启用所有生成属性的验证。

包含类型必须继承自 `BindableValidator`，它提供 `INotifyDataErrorInfo` 实现、`ValidateProperty()`、`ValidateAllProperties()` 和 `ClearErrors()` 方法。

```csharp
using System.ComponentModel.DataAnnotations;
using Prism.SourceGenerators;

public partial class RegistrationViewModel : BindableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    [MinLength(2)]
    public partial string Username { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    [EmailAddress]
    public partial string Email { get; set; }
}
```

生成的 setter 会在设置值后自动调用 `ValidateProperty(value, nameof(Property))`。验证错误按属性跟踪，错误状态变化时触发 `ErrorsChanged` 事件。

类级别用法会对所有 `[ObservableProperty]` 成员启用验证：

```csharp
[NotifyDataErrorInfo]
public partial class FormViewModel : BindableValidator
{
    [ObservableProperty]
    [Required]
    public partial string FirstName { get; set; }

    [ObservableProperty]
    [Required]
    public partial string LastName { get; set; }
}
```

## 诊断

| ID | 描述 |
|----|------|
| PSG0001 | 包含 `[ObservableProperty]` 成员的类必须声明为 `partial` |
| PSG0002 | 包含 `[DelegateCommand]` / `[AsyncDelegateCommand]` 方法的类必须声明为 `partial` |
| PSG0003 | 标注 `[ObservableProperty]` 的属性必须声明为 `partial` |
| PSG0004 | 标注 `[BindableBase]` 的类必须声明为 `partial` |
| PSG0005 | 标注 `[BindableValidator]` 的类必须声明为 `partial` |
| PSG0006 | `[BindableValidator]` 仅支持 class，不支持 struct / interface |
| PSG1001 | `[DelegateCommand]` 方法签名无效 |
| PSG1002 | `[AsyncDelegateCommand]` 方法签名无效 |
| PSG2001 | 未找到 Catch 处理程序成员 |
| PSG2002 | Catch 处理程序签名不兼容 |
| PSG2003 | 未找到 CanExecute 成员 |
| PSG2004 | 未找到被观察的属性 |
| PSG2005 | `[NotifyCanExecuteChangedFor]` 引用的命令未找到 |
| PSG2006 | `CanExecute` 所指向的成员签名与命令不兼容 |
| PSG3002 | 未找到 `AsyncDelegateCommand`；请安装 **`MvvmAIO.Prism.Bcl.Commands`**（Prism.Core 8.1.97），或升级到 Prism 9+ |
| PSG4001 | ServiceType 与实现类型不兼容 |
| PSG4002 | ViewModelType 无法解析 |
| PSG5001 | `[NotifyDataErrorInfo]` 要求包含类型继承自 `BindableValidator` |

> **快速修复：** PSG0001–PSG0005 都提供 IDE 代码修复，会自动插入缺失的 `partial` 修饰符（在波浪线处按 Ctrl+. / Alt+Enter，或使用"修复文档/项目/解决方案中的所有问题"在整个代码库中批量应用）。

## 安装

```xml
<PackageReference Include="MvvmAIO.Prism.SourceGenerators" Version="0.7.0" />
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
dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version 0.2.0

# 发布 NuGet（MvvmAIO.Prism.SourceGenerators + MvvmAIO.Prism.Bcl.Commands）
dotnet run --project build/_build.csproj -- --target Publish --configuration Release --version 0.2.0 --nuget-api-key <NUGET_API_KEY>
```

## 要求

- .NET 10 SDK
- Visual Studio 2022 17.13+ / Rider / VS Code with C# Dev Kit（支持 `.slnx`）
