# Prism.SourceGenerators

[English](README.md) | [简体中文](README.zh-CN.md) | **繁體中文** | [日本語](README.ja.md)

為 [Prism](https://github.com/PrismLibrary/Prism) MVVM 程式庫提供的 Roslyn 原始碼產生器。

參與貢獻請參閱 [CONTRIBUTING.md](CONTRIBUTING.md)，儲存庫工程與協作規範請參閱 [AGENTS.md](AGENTS.md)。

[![.NET](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)
[![Tests](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/MvvmAIO/Prism.SourceGenerators/master/.github/badges/tests.json)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/MvvmAIO.Prism.SourceGenerators.svg?label=NuGet&logo=nuget)](https://www.nuget.org/packages/MvvmAIO.Prism.SourceGenerators)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)

## CI 狀態

- 點擊上方工作流程可檢視最新建置狀態。
- `Tests` 徽章會直接顯示最新通過 / 失敗 / 略過數量。
- 執行結果亦會上傳 `test-results`（`.trx`）成品供檢視詳細測試報告。

## 文件說明（README、Wiki、文件網站）

| 管道 | 說明 |
|------|------|
| **[文件網站](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/)**（[原始碼儲存庫](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs)） | **權威手冊**：多語言、完整產生器參考、**PSG** 診斷表、架構與 CI 等。深入閱讀與交叉連結請以本站為準。 |
| **本 README** / [English](README.md) / [简体中文](README.zh-CN.md) / [日本語](README.ja.md) | 儲存庫首頁簡介與常用程式碼片段，**不作為**完整手冊。 |
| **[GitHub Wiki](https://github.com/MvvmAIO/Prism.SourceGenerators/wiki)**（主儲存庫 [`wiki/`](https://github.com/MvvmAIO/Prism.SourceGenerators/tree/master/wiki) 目錄同步，便於 PR） | 簡短主題筆記（中文為主），**非**編譯器診斷文字或 API 契約文本。 |

## 專案結構

```
Prism.SourceGenerators/                        # 共用專案（.shproj/.projitems/.props + 原始碼）
Prism.SourceGenerators.Roslyn4001/             # Roslyn 4.0.1
Prism.SourceGenerators.Roslyn4031/             # Roslyn 4.3.1
Prism.SourceGenerators.Roslyn4120/             # Roslyn 4.12.0
Prism.SourceGenerators.Roslyn5000/             # Roslyn 5.0.0
Prism.SourceGenerators.Core/                   # MvvmAIO.Prism.Core（特性），隨 MvvmAIO.Prism.SourceGenerators 封裝
Prism.Bcl.Commands/                            # MvvmAIO.Prism.Bcl.Commands（Prism 8 AsyncDelegateCommand 套件，需手動安裝）
```

範例（Avalonia、WPF、MAUI、Uno）：獨立儲存庫 [Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples) — Prism 8 / Prism 9 示範應用程式與 ViewModel 程式庫，透過 NuGet 參考 **`MvvmAIO.Prism.SourceGenerators`**。

## 產生器

### `[ObservableProperty]`

為繼承自 `BindableBase` 的類別產生可觀察屬性。依 C# 語言版本支援兩種使用模式。

#### 欄位目標（所有 C# 版本）

在私有欄位上標註 `[ObservableProperty]`，產生呼叫 `SetProperty` 的屬性；**預設**產生 **`public`**。可透過 **`PropertyAccess`** 的位置參數或具名參數 `PropertyAccess = …` 指定為 `internal`、`protected`、`private`、`protected internal`、`private protected` 等。

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

    // 產生：setter 中呼叫 OnTitleChanging*、BindableBase.SetProperty(ref _title, value, () => { OnTitleChanged*; })，
    // 以及選用的 [NotifyPropertyChangedFor] / 命令重新整理等。
}
```

**部分屬性（partial property）**目標以屬性宣告上的存取修飾詞為準；`PropertyAccess` 會被忽略。

#### 部分屬性目標（C# 13+ `field` 關鍵字）

在 `partial` 屬性上標註 `[ObservableProperty]`，使用 `field` 關鍵字（半自動屬性）產生實作宣告。

```csharp
// C# 13+ / .NET 9+（需要 LangVersion 13.0+ 或 preview）
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = "Hello";

    // 產生：public partial string Title { get => field; set => SetProperty(ref field, value); }
}
```

部分屬性寫法無需額外的支援欄位，提供更精簡的 API 介面。兩種模式可在同一專案中共存。

#### OnChanging / OnChanged 部分方法

每個 `[ObservableProperty]` 都會產生四個 `partial` 方法宣告，可選擇性實作以回應變更。`OnXxxChanging` 掛鉤於寫入欄位**之前**觸發，`OnXxxChanged` 掛鉤於寫入**之後**觸發：

```csharp
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial int Age { get; set; }

    // 產生的宣告（可實作任意子集）：
    // partial void OnAgeChanging(int value);
    // partial void OnAgeChanging(int oldValue, int newValue);
    // partial void OnAgeChanged(int value);
    // partial void OnAgeChanged(int oldValue, int newValue);

    partial void OnAgeChanging(int oldValue, int newValue)
    {
        Debug.WriteLine($"Age 即將從 {oldValue} 變更為 {newValue}");
    }

    partial void OnAgeChanged(int oldValue, int newValue)
    {
        Debug.WriteLine($"Age 已從 {oldValue} 變更為 {newValue}");
    }
}
```

產生的 setter 會先使用 `EqualityComparer<T>.Default.Equals` 做快速相等判斷。當值確實變更時，先呼叫兩個 `OnChanging` 多載，再透過 `SetProperty(ref storage, value, onChanged)` 更新儲存值，並沿用與手寫 Prism 屬性一致的 `BindableBase` 路徑（可覆寫 `SetProperty`）。`onChanged` 回呼內會呼叫兩個 `OnChanged` 多載，隨後由 `SetProperty` 觸發主屬性的 `PropertyChanged`。`[NotifyPropertyChangedFor]` 與 `[NotifyCanExecuteChangedFor]` 的額外通知會在該呼叫之後發出。

### `[NotifyPropertyChangedFor]`

搭配 `[ObservableProperty]` 使用，在被標註的屬性變更時自動為其他相依屬性觸發 `PropertyChanged`。

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

支援透過 `[NotifyPropertyChangedFor(nameof(A), nameof(B))]` 指定多個屬性名稱，也支援多次標註。

### `[NotifyCanExecuteChangedFor]`

搭配 `[ObservableProperty]` 使用，當被標註的屬性變更時自動呼叫指定命令的 `RaiseCanExecuteChanged()`。

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

產生的 setter 會在 `RaisePropertyChanged` 之後呼叫 `SaveCommand?.RaiseCanExecuteChanged()`。可使用 `[NotifyCanExecuteChangedFor(nameof(A), nameof(B))]` 一次指定多個命令，或多次標註。名稱可以是型別上既有的成員，或是 `[DelegateCommand]` / `[AsyncDelegateCommand]` 方法產生的命令屬性（例如 `Save` 或 `SaveAsync` 會產生 `SaveCommand`；亦支援特性上的明確 `CommandName`）。若名稱無法解析，會回報 **PSG2005**（警告），但 setter 仍會產生。

### 轉發特性到產生的屬性

針對**欄位**目標，與欄位寫在同一特性清單中、**無目標**或 **`[property: …]`** 的特性會轉發至產生的屬性上（產生器自有特性 `[ObservableProperty]`、`[NotifyPropertyChangedFor]`、`[NotifyCanExecuteChangedFor]`、`[NotifyDataErrorInfo]` 會被過濾）。明確標記 **`[field: …]`** 的清單僅保留於支援欄位上。

```csharp
public partial class Vm : BindableBase
{
    [ObservableProperty]
    [System.ComponentModel.DataAnnotations.Required] // 轉發（驗證 / DataAnnotations）
    [property: System.Text.Json.Serialization.JsonIgnore] // 轉發
    private string _password = "";
}
```

將產生：

```csharp
[global::System.ComponentModel.DataAnnotations.RequiredAttribute]
[global::System.Text.Json.Serialization.JsonIgnoreAttribute]
public string Password { get { ... } set { ... } }
```

針對**部分屬性（partial property）**目標，繼承自 **`ValidationAttribute`** 的特性（如 `[Required]`、`[EmailAddress]`、`[Range]`）只會保留在**你撰寫的** partial 宣告上；產生器**不會**重複複製至實作 partial，讓 **`Validator`** / **`BindableValidator`** 仍只讀取到一份中繼資料，並避免 **CS0579** 特性重複錯誤。其他特性（如 `[JsonIgnore]`）仍會轉發至產生的實作宣告上。產生器自有特性會被過濾，轉發的特性均以完整限定型別名稱輸出，因此不依賴產生檔案中的 `using` 指示詞。

> 轉發特性的引數運算式會按原樣輸出。若產生檔案無法看見 `using` 指示詞，請使用常值 / `nameof` / `typeof`，或在引數位置使用完整限定型別參考。

### `[DelegateCommand]`

從方法產生 `DelegateCommand` 或 `AsyncDelegateCommand` 屬性。

- **同步方法**（`void`）產生 `DelegateCommand` / `DelegateCommand<T>`
- **非同步方法**若回傳 **`Task`**、**`Task<TResult>`**、**`ValueTask`** 或 **`ValueTask<TResult>`**，則產生 `AsyncDelegateCommand` / `AsyncDelegateCommand<T>`。`ValueTask` / `ValueTask<TResult>` 在產生程式碼中透過 `.AsTask()` 銜接至 Prism 的 `Func<Task>` / `Func<T, Task>` 建構函式；**`Task<TResult>`** 透過 `async` lambda 等待執行方法。若執行方法帶有 **`CancellationToken`** 參數，則不可回傳 `ValueTask`、`ValueTask<TResult>` 或 **`Task<TResult>`**（**PSG1001**）。
- 針對 Prism &lt; 9.0，請使用 NuGet 套件 **`MvvmAIO.Prism.SourceGenerators`**，它會帶入 **`MvvmAIO.Prism.Core`** 以提供產生器特性定義。若使用 Prism.Core 8.1.97 的非同步命令，請手動安裝 **`MvvmAIO.Prism.Bcl.Commands`** 以提供 `AsyncDelegateCommand`。若使用非同步命令卻缺少上述組件，將回報 **PSG3002**。
- **C# 14+**：命令屬性使用 `field` 關鍵字（無需額外支援欄位）
- **C# 13 及更早版本**：命令屬性使用傳統支援欄位

```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    // 產生：DelegateCommand IncrementCommand
    [DelegateCommand]
    private void Increment() { /* ... */ }

    // 產生：AsyncDelegateCommand LoadDataCommand
    [DelegateCommand]
    private async Task LoadDataAsync() { /* ... */ }

    // 支援 CanExecute
    [DelegateCommand(CanExecute = nameof(CanSubmit))]
    private void Submit() { /* ... */ }
    private bool CanSubmit() => true;
}
```

#### 產生程式碼對照

**C# 14+（LangVersion >= 14）** — 使用 `field` 關鍵字：
```csharp
// 無需支援欄位
public DelegateCommand IncrementCommand => field ??= new DelegateCommand(Increment);
```

**C# 13 及更早版本** — 傳統支援欄位：
```csharp
private DelegateCommand? _incrementCommand;
public DelegateCommand IncrementCommand => _incrementCommand ??= new DelegateCommand(Increment);
```

### `[AsyncDelegateCommand]`

專為非同步方法設計的特性，提供與 Prism 一致的進階功能。
在 Prism 9+ 上使用框架內建型別；在 Prism 8.1.97 上，請安裝 **`MvvmAIO.Prism.Bcl.Commands`** 以取得相同的鏈式設定：`EnableParallelExecution`、`CancelAfter`、`Catch`、`CancellationTokenSourceFactory`、`ObservesCanExecute`。

```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    // 啟用平行執行
    [AsyncDelegateCommand(EnableParallelExecution = true)]
    private async Task FetchDataAsync() { /* ... */ }

    // 錯誤處理 + CanExecute
    [AsyncDelegateCommand(CanExecute = nameof(CanSave), Catch = nameof(HandleError))]
    private async Task SaveAsync() { /* ... */ }

    private bool CanSave() => true;
    private void HandleError(Exception ex) { /* ... */ }
}
```

### `[ObservesProperty]`

當指定屬性變更時自動重新評估 `CanExecute`。
同時支援 `[DelegateCommand]` 與 `[AsyncDelegateCommand]`。

```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private bool _isValid;

    [DelegateCommand(CanExecute = nameof(CanSubmit))]
    [ObservesProperty(nameof(IsValid))]
    private void Submit() { /* ... */ }

    // 多個屬性
    [AsyncDelegateCommand(CanExecute = nameof(CanSave))]
    [ObservesProperty(nameof(Counter), nameof(IsActive))]
    private async Task SaveAsync() { /* ... */ }
}
```

### `[BindableBase]`

套用於**未**繼承 `Prism.Mvvm.BindableBase` 的類別，自動產生 `INotifyPropertyChanged` 實作。產生程式碼包含 `PropertyChanged` 事件、`SetProperty<T>`、`RaisePropertyChanged` 與 `OnPropertyChanged` 方法。

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

若類別已繼承 `BindableBase` 或其基底類別已實作 `INotifyPropertyChanged`，則不會產生任何程式碼。

### `[NotifyDataErrorInfo]`（驗證）

透過 `INotifyDataErrorInfo` 啟用屬性驗證支援。將 `[NotifyDataErrorInfo]` 套用至單一欄位 / 屬性（搭配 `[ObservableProperty]`），或套用至類別本身以啟用所有產生屬性的驗證。

包含型別必須繼承自 `BindableValidator`，該類別提供 `INotifyDataErrorInfo` 實作、`ValidateProperty()`、`ValidateAllProperties()` 與 `ClearErrors()` 方法。

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

產生的 setter 會在設定值後自動呼叫 `ValidateProperty(value, nameof(Property))`。驗證錯誤依屬性追蹤，錯誤狀態變更時觸發 `ErrorsChanged` 事件。

類別層級套用會對所有 `[ObservableProperty]` 成員啟用驗證：

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

## 診斷

| ID | 描述 |
|----|------|
| PSG0001 | 包含 `[ObservableProperty]` 成員的類別必須宣告為 `partial` |
| PSG0002 | 包含 `[DelegateCommand]` / `[AsyncDelegateCommand]` 方法的類別必須宣告為 `partial` |
| PSG0003 | 標註 `[ObservableProperty]` 的屬性必須宣告為 `partial` |
| PSG0004 | 標註 `[BindableBase]` 的類別必須宣告為 `partial` |
| PSG0005 | 標註 `[BindableValidator]` 的類別必須宣告為 `partial` |
| PSG0006 | `[BindableValidator]` 僅支援 class，不支援 struct / interface |
| PSG1001 | `[DelegateCommand]` 方法簽章無效 |
| PSG1002 | `[AsyncDelegateCommand]` 方法簽章無效 |
| PSG2001 | 找不到 Catch 處理常式成員 |
| PSG2002 | Catch 處理常式簽章不相容 |
| PSG2003 | 找不到 CanExecute 成員 |
| PSG2004 | 找不到被觀察的屬性 |
| PSG2005 | `[NotifyCanExecuteChangedFor]` 參考的命令不存在 |
| PSG2006 | `CanExecute` 所指向的成員簽章與命令不相容 |
| PSG3002 | 找不到 `AsyncDelegateCommand`；請安裝 **`MvvmAIO.Prism.Bcl.Commands`**（Prism.Core 8.1.97），或升級至 Prism 9+ |
| PSG4001 | ServiceType 與實作型別不相容 |
| PSG4002 | ViewModelType 無法解析 |
| PSG5001 | `[NotifyDataErrorInfo]` 要求包含型別繼承自 `BindableValidator` |
| PSG0007 | 標註 `[NavigationAware]` 的類別必須宣告為 `partial` |
| PSG0008 | 標註 `[DialogAware]` 的類別必須宣告為 `partial` |
| PSG7001 | 找不到可存取的 `IRegionManager`（`[NavigateCommand]` / `[NavigateOnChanged]`） |
| PSG7002–PSG7005 | `[NavigateCommand]` / `[NavigateOnChanged]` 特性驗證 |
| PSG7006–PSG7008 | `[FromNavigationParameter]` — 目標無效、缺少 `[ObservableProperty]`、空白 Key |
| PSG7101–PSG7102 | `[ShowDialogCommand]` — 缺少 `IDialogService` 或對話框 `Name` |
| PSG7103–PSG7105 | `[FromDialogParameter]` — 目標無效、缺少 `[ObservableProperty]`、空白 Key |

> **快速修正：** PSG0001–PSG0005 皆提供 IDE 程式碼修正，可自動插入缺少的 `partial` 修飾詞（於波浪線處按 Ctrl+. / Alt+Enter，或使用「修正文件 / 專案 / 解決方案中的所有問題」批次套用）。

## 安裝

```xml
<PackageReference Include="MvvmAIO.Prism.SourceGenerators" Version="0.8.1" />
```

或：

```bash
dotnet add package MvvmAIO.Prism.SourceGenerators
```

## 建置

```bash
dotnet build Prism.SourceGenerators.slnx
```

## Nuke 建置

本儲存庫使用 [Nuke](https://nuke.build/) 作為本機自動化與 CI 的建置編排層。

- 主要原始碼解決方案：`Prism.SourceGenerators.slnx`
- 完整解決方案（包含所有產品專案與 Nuke；文件網站位於 **Prism.SourceGenerators.Docs**）：`Prism.SourceGenerators.Full.slnx`
- 建置自動化解決方案：`build.slnx`（僅包含 `build/_build.csproj`）

常用命令：

```bash
# 本機執行 CI 流程（clean + restore + compile + test）
dotnet run --project build/_build.csproj -- --target Ci --configuration Release

# 封裝 NuGet 套件（可選覆寫版本號）
dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version 0.2.0

# 發布 NuGet 套件（MvvmAIO.Prism.SourceGenerators + MvvmAIO.Prism.Bcl.Commands）
dotnet run --project build/_build.csproj -- --target Publish --configuration Release --version 0.2.0 --nuget-api-key <NUGET_API_KEY>
```

## 需求

- .NET 10 SDK
- Visual Studio 2022 17.13+ / Rider / VS Code with C# Dev Kit（支援 `.slnx`）
