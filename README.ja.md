# Prism.SourceGenerators

[English](README.md) | [简体中文](README.zh-CN.md) | **日本語**

[Prism](https://github.com/PrismLibrary/Prism) MVVM ライブラリ向けの Roslyn ソースジェネレーター。

## プロジェクト構成

```
Prism.SourceGenerators/                        # 共有プロジェクト（.shproj/.projitems/.props + ソースコード）
Prism.SourceGenerators.Roslyn4001/             # Roslyn 4.0.1
Prism.SourceGenerators.Roslyn4031/             # Roslyn 4.3.1
Prism.SourceGenerators.Roslyn4120/             # Roslyn 4.12.0
Prism.SourceGenerators.Roslyn5000/             # Roslyn 5.0.0
Prism.SourceGenerators.Samples.Prism9/         # WPF サンプル（Prism 9.0、ネイティブ AsyncDelegateCommand）
Prism.SourceGenerators.Samples.Prism8/         # WPF サンプル（Prism 8.x、ポリフィル AsyncDelegateCommand）
```

## ジェネレーター

### `[ObservableProperty]`

`BindableBase` を継承するクラスに監視可能なプロパティを生成します。C# 言語バージョンに応じて 2 つの使用モードをサポートしています。

#### フィールドターゲット（すべての C# バージョン）

プライベートフィールドに `[ObservableProperty]` を付与すると、setter で `SetProperty` を呼び出すパブリックプロパティが生成されます。

```csharp
// C# 12 以前
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Hello";

    // 生成: public string Title { get => _title; set => SetProperty(ref _title, value); }
}
```

#### パーシャルプロパティターゲット（C# 13+ `field` キーワード）

`partial` プロパティに `[ObservableProperty]` を付与すると、`field` キーワード（セミオートプロパティ）を使用した実装宣言が生成されます。

```csharp
// C# 13+ / .NET 9+（LangVersion 13.0+ または preview が必要）
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = "Hello";

    // 生成: public partial string Title { get => field; set => SetProperty(ref field, value); }
}
```

パーシャルプロパティ方式は個別のバッキングフィールドが不要で、よりクリーンな API を提供します。両モードは同一プロジェクト内で共存できます。

#### OnChanged パーシャルメソッド

すべての `[ObservableProperty]` に対して、変更に応答するためにオプションで実装できる 2 つの `partial` メソッド宣言が生成されます：

```csharp
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial int Age { get; set; }

    // 生成される宣言（片方または両方を実装可能）:
    // partial void OnAgeChanged(int value);
    // partial void OnAgeChanged(int oldValue, int newValue);

    partial void OnAgeChanged(int oldValue, int newValue)
    {
        Debug.WriteLine($"Age が {oldValue} から {newValue} に変更されました");
    }
}
```

生成されたsetterは `EqualityComparer<T>.Default.Equals` を使用して変更検出を行い、`PropertyChanged` を発行する前に両方の `OnChanged` オーバーロードを呼び出します。

### `[NotifyPropertyChangedFor]`

`[ObservableProperty]` と組み合わせて使用し、対象プロパティが変更されたときに、他の依存プロパティの `PropertyChanged` を自動的に発行します。

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

`[NotifyPropertyChangedFor(nameof(A), nameof(B))]` で複数のプロパティ名を指定、または複数の属性インスタンスを使用できます。

### `[DelegateCommand]`

メソッドから `DelegateCommand` または `AsyncDelegateCommand` プロパティを生成します。

- **同期メソッド**（`void`）は `DelegateCommand` / `DelegateCommand<T>` を生成
- **非同期メソッド**（`Task`）は `AsyncDelegateCommand` / `AsyncDelegateCommand<T>` を生成
- Prism < 9.0（`AsyncDelegateCommand` が未搭載）の場合、ポリフィルが自動生成されます
- **C# 14+**：Command プロパティは `field` キーワードを使用（個別のバッキングフィールド不要）
- **C# 13 以前**：Command プロパティは従来のバッキングフィールドを使用

```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    // 生成: DelegateCommand IncrementCommand
    [DelegateCommand]
    private void Increment() { /* ... */ }

    // 生成: AsyncDelegateCommand LoadDataCommand
    [DelegateCommand]
    private async Task LoadDataAsync() { /* ... */ }

    // CanExecute サポート
    [DelegateCommand(CanExecute = nameof(CanSubmit))]
    private void Submit() { /* ... */ }
    private bool CanSubmit() => true;
}
```

#### 生成コードの比較

**C# 14+（LangVersion >= 14）**— `field` キーワードを使用：
```csharp
// バッキングフィールド不要
public DelegateCommand IncrementCommand => field ??= new DelegateCommand(Increment);
```

**C# 13 以前** — 従来のバッキングフィールド：
```csharp
private DelegateCommand? _incrementCommand;
public DelegateCommand IncrementCommand => _incrementCommand ??= new DelegateCommand(Increment);
```

### `[AsyncDelegateCommand]`

非同期メソッド専用の属性で、Prism 9.0+ の高度な機能をサポートします。
フルーエント構成をサポート：`EnableParallelExecution`、`CancelAfter`、`Catch`、`CancellationTokenSourceFactory`。

```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    // 並列実行を有効化
    [AsyncDelegateCommand(EnableParallelExecution = true)]
    private async Task FetchDataAsync() { /* ... */ }

    // エラーハンドリング + CanExecute
    [AsyncDelegateCommand(CanExecute = nameof(CanSave), Catch = nameof(HandleError))]
    private async Task SaveAsync() { /* ... */ }

    private bool CanSave() => true;
    private void HandleError(Exception ex) { /* ... */ }
}
```

### `[ObservesProperty]`

指定されたプロパティが変更されたときに `CanExecute` を自動的に再評価します。
`[DelegateCommand]` と `[AsyncDelegateCommand]` の両方で使用できます。

```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private bool _isValid;

    [DelegateCommand(CanExecute = nameof(CanSubmit))]
    [ObservesProperty(nameof(IsValid))]
    private void Submit() { /* ... */ }

    // 複数プロパティ
    [AsyncDelegateCommand(CanExecute = nameof(CanSave))]
    [ObservesProperty(nameof(Counter), nameof(IsActive))]
    private async Task SaveAsync() { /* ... */ }
}
```

## 診断

| ID | 説明 |
|----|------|
| PSG0001 | `[ObservableProperty]` メンバーを持つクラスは `partial` として宣言する必要があります |
| PSG0002 | `[DelegateCommand]` / `[AsyncDelegateCommand]` メソッドを持つクラスは `partial` として宣言する必要があります |
| PSG0003 | `[ObservableProperty]` を付与されたプロパティは `partial` として宣言する必要があります |

## ビルド

```bash
dotnet build Prism.SourceGenerators.slnx
```

## 要件

- .NET SDK 9.0.200+ または .NET 10 SDK
- Visual Studio 2022 17.13+ / Rider / VS Code with C# Dev Kit（`.slnx` サポート）
