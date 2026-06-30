# Prism.SourceGenerators

[English](README.md) | [简体中文](README.zh-CN.md) | **日本語**

[Prism](https://github.com/PrismLibrary/Prism) MVVM ライブラリ向けの Roslyn ソースジェネレーター。

## CI ステータス

[![.NET](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)
[![Tests](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/MvvmAIO/Prism.SourceGenerators/master/.github/badges/tests.json)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)

- 上記ワークフローリンクから最新のパイプライン状態を確認できます。
- `Tests` バッジに最新の pass/fail/skip 件数が表示されます。
- 実行ごとに `test-results`（`.trx`）アーティファクトもアップロードされます。

## ドキュメント（README、Wiki、ドキュメントサイト）

| 場所 | 役割 |
|------|------|
| **[ドキュメントサイト](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/)**（[ソース](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs)） | **正典**：英語 / 簡体字中国語 / 日本語、ジェネレータ網羅、**PSG** 一覧、アーキテクチャと CI など。詳細はここを優先。 |
| **本 README** / [English](README.md) / [简体中文](README.zh-CN.md) | リポジトリ概要とコピペ例。**完全なマニュアルではない**。 |
| **[GitHub Wiki](https://github.com/MvvmAIO/Prism.SourceGenerators/wiki)**（PR 用に [`wiki/`](https://github.com/MvvmAIO/Prism.SourceGenerators/tree/master/wiki) を同期） | 短いトピック別メモ（中文中心）。**診断文言や API の契約にはしない**。 |

## プロジェクト構成

```
Prism.SourceGenerators/                        # 共有プロジェクト（.shproj/.projitems/.props + ソースコード）
Prism.SourceGenerators.Roslyn4001/             # Roslyn 4.0.1
Prism.SourceGenerators.Roslyn4031/             # Roslyn 4.3.1
Prism.SourceGenerators.Roslyn4120/             # Roslyn 4.12.0
Prism.SourceGenerators.Roslyn5000/             # Roslyn 5.0.0
Prism.SourceGenerators.Core/                   # MvvmAIO.Prism.Core（属性）、MvvmAIO.Prism.SourceGenerators に同梱
Prism.Bcl.Commands/                            # MvvmAIO.Prism.Bcl.Commands（Prism 8 AsyncDelegateCommand パッケージ、手動インストール）
```

サンプル（Avalonia）: 別リポジトリ [Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples) — Prism 8 / 9 のデモ。NuGet の **`MvvmAIO.Prism.SourceGenerators`** を参照。

## ジェネレーター

### `[ObservableProperty]`

`BindableBase` を継承するクラスに監視可能なプロパティを生成します。C# 言語バージョンに応じて 2 つの使用モードをサポートしています。

#### フィールドターゲット（すべての C# バージョン）

プライベートフィールドに `[ObservableProperty]` を付与すると、setter で `SetProperty` を呼び出すプロパティが生成されます。**既定**は **`public`** です。`PropertyAccess` を位置指定または `PropertyAccess = …` の名前付きで渡すと `internal`、`protected`、`private`、`protected internal`、`private protected` を指定できます。

```csharp
// C# 12 以前
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Hello";

    [ObservableProperty(PropertyAccess.Internal)]
    // または: [ObservableProperty(PropertyAccess = PropertyAccess.Internal)]
    private int _count;

    // 生成: OnTitleChanging* の後に BindableBase.SetProperty(ref _title, value, () => { OnTitleChanged*; }) など。
}
```

**パーシャルプロパティ** ターゲットでは、プロパティ宣言の修飾子が使われます。`PropertyAccess` は無視されます。

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

#### OnChanging / OnChanged パーシャルメソッド

すべての `[ObservableProperty]` に対して、変更に応答するためにオプションで実装できる 4 つの `partial` メソッド宣言が生成されます。`OnXxxChanging` フックはバッキングフィールド書き込みの**前**に実行され、`OnXxxChanged` フックは**後**に実行されます：

```csharp
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial int Age { get; set; }

    // 生成される宣言（任意の組み合わせを実装可能）:
    // partial void OnAgeChanging(int value);
    // partial void OnAgeChanging(int oldValue, int newValue);
    // partial void OnAgeChanged(int value);
    // partial void OnAgeChanged(int oldValue, int newValue);

    partial void OnAgeChanging(int oldValue, int newValue)
    {
        Debug.WriteLine($"Age が {oldValue} から {newValue} に変更されようとしています");
    }

    partial void OnAgeChanged(int oldValue, int newValue)
    {
        Debug.WriteLine($"Age が {oldValue} から {newValue} に変更されました");
    }
}
```

生成された setter はまず `EqualityComparer<T>.Default.Equals` で早期リターンします。値が変化する場合、両方の `OnChanging` オーバーロードを呼び出したうえで、`SetProperty(ref storage, value, onChanged)` を通じて `BindableBase` と同じ更新経路（`SetProperty` のオーバーライドが有効）でストアを更新します。`onChanged` 内で両方の `OnChanged` を呼び、その後 `SetProperty` が主プロパティの `PropertyChanged` を発行します。`[NotifyPropertyChangedFor]` および `[NotifyCanExecuteChangedFor]` による追加通知はその後に出力されます。

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

### `[NotifyCanExecuteChangedFor]`

`[ObservableProperty]` と組み合わせて使用し、対象プロパティが変更されたときに、指定したコマンドの `RaiseCanExecuteChanged()` を自動的に呼び出します。

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

生成された setter は `RaisePropertyChanged` の後に `SaveCommand?.RaiseCanExecuteChanged()` を呼び出します。`[NotifyCanExecuteChangedFor(nameof(A), nameof(B))]` で複数のコマンドを指定、または複数の属性インスタンスを使用できます。名前は型上の既存メンバー、または `[DelegateCommand]` / `[AsyncDelegateCommand]` メソッドが生成するコマンドプロパティ（例：メソッド `Save` が `SaveCommand` を生成）を指定できます。解決できない名前は **PSG2005**（警告）として報告されますが、setter は生成されます。

### 生成されるプロパティへの属性転送

**フィールド**ターゲットでは、フィールドと同じ属性リストで書かれた **ターゲットなし** または **`[property: …]`** の属性が生成されたプロパティに転送されます（ジェネレーター自身の属性 `[ObservableProperty]`、`[NotifyPropertyChangedFor]`、`[NotifyCanExecuteChangedFor]`、`[NotifyDataErrorInfo]` は除外）。明示的な **`[field: …]`** ターゲットのリストはバッキングフィールド側にのみ残ります。

```csharp
public partial class Vm : BindableBase
{
    [ObservableProperty]
    [System.ComponentModel.DataAnnotations.Required] // 転送（検証 / DataAnnotations）
    [property: System.Text.Json.Serialization.JsonIgnore] // 転送
    private string _password = "";
}
```

は次のように生成されます：

```csharp
[global::System.ComponentModel.DataAnnotations.RequiredAttribute]
[global::System.Text.Json.Serialization.JsonIgnoreAttribute]
public string Password { get { ... } set { ... } }
```

**partial プロパティ**ターゲットでは、**`ValidationAttribute`** を継承する属性（`[Required]`、`[EmailAddress]`、`[Range]` など）は、ユーザーが書いた partial 宣言側にのみ残し、生成される実装 partial には**転送しません**（**CS0579** の重複を避け、`Validator` / `BindableValidator` には 1 回だけメタデータが見える）。それ以外（例：`[JsonIgnore]`）は従来どおり実装宣言へ転送します。ジェネレーター自身の属性（`[ObservableProperty]`、`[NotifyPropertyChangedFor]`、`[NotifyCanExecuteChangedFor]`、`[NotifyDataErrorInfo]`）は除外されます。転送される属性は完全修飾型名で出力されるため、生成ファイル内の `using` ディレクティブに依存しません。

> 転送される属性の引数式はそのまま出力されます。生成ファイルから `using` ディレクティブが見えない場合は、リテラル / `nameof` / `typeof`、または引数位置で完全修飾型参照を使用してください。

### `[DelegateCommand]`

メソッドから `DelegateCommand` または `AsyncDelegateCommand` プロパティを生成します。

- **同期メソッド**（`void`）は `DelegateCommand` / `DelegateCommand<T>` を生成
- **非同期メソッド**の戻り値が **`Task`**、**`Task<TResult>`**、**`ValueTask`**、または **`ValueTask<TResult>`** のとき、`AsyncDelegateCommand` / `AsyncDelegateCommand<T>` を生成します。`ValueTask` / `ValueTask<TResult>` は生成コードで `.AsTask()` により Prism の `Func<Task>` / `Func<T, Task>` コンストラクタに接続します。**`Task<TResult>`** は `async` lambda で execute を待機します。**`CancellationToken`** を取る execute メソッドでは `ValueTask`、`ValueTask<TResult>`、**`Task<TResult>`** はサポートされず（**PSG1001**）。
- Prism &lt; 9.0 の場合、NuGet **`MvvmAIO.Prism.SourceGenerators`** を使用してください。**`MvvmAIO.Prism.Core`**（属性定義）を追加します。Prism.Core 8.1.97 の非同期コマンドを使う場合は **`MvvmAIO.Prism.Bcl.Commands`** を手動で追加してください。非同期コマンド使用時にこれらのアセンブリがない場合は **PSG3002** が報告されます。
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

非同期メソッド専用の属性で、Prism と同等の高度な機能を提供します。
Prism 9 以上ではフレームワーク実装を使用し、Prism 8.1.97 では **`MvvmAIO.Prism.Bcl.Commands`** を追加すると同じ Fluent 構成が使えます：`EnableParallelExecution`、`CancelAfter`、`Catch`、`CancellationTokenSourceFactory`、`ObservesCanExecute`。

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

### `[BindableBase]`

`Prism.Mvvm.BindableBase` を継承**していない**クラスに適用すると、`INotifyPropertyChanged` の実装を自動生成します。生成されるコードには `PropertyChanged` イベント、`SetProperty<T>`、`RaisePropertyChanged`、`OnPropertyChanged` メソッドが含まれます。

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

クラスがすでに `BindableBase` を継承している場合、または基底クラスが `INotifyPropertyChanged` を実装している場合、コードは生成されません。

### `[NotifyDataErrorInfo]`（バリデーション）

`INotifyDataErrorInfo` によるプロパティバリデーションサポートを有効にします。`[NotifyDataErrorInfo]` を個々のフィールド/プロパティ（`[ObservableProperty]` と併用）またはクラス自体に適用して、すべての生成プロパティのバリデーションを有効にします。

含まれる型は `BindableValidator` を継承する必要があり、`INotifyDataErrorInfo` 実装、`ValidateProperty()`、`ValidateAllProperties()`、`ClearErrors()` メソッドを提供します。

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

生成された setter は値を設定した後、自動的に `ValidateProperty(value, nameof(Property))` を呼び出します。バリデーションエラーはプロパティごとに追跡され、エラー状態が変化すると `ErrorsChanged` イベントが発生します。

クラスレベルでの使用は、すべての `[ObservableProperty]` メンバーにバリデーションを適用します：

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

## 診断

| ID | 説明 |
|----|------|
| PSG0001 | `[ObservableProperty]` メンバーを持つクラスは `partial` として宣言する必要があります |
| PSG0002 | `[DelegateCommand]` / `[AsyncDelegateCommand]` メソッドを持つクラスは `partial` として宣言する必要があります |
| PSG0003 | `[ObservableProperty]` を付与されたプロパティは `partial` として宣言する必要があります |
| PSG0004 | `[BindableBase]` を付与されたクラスは `partial` として宣言する必要があります |
| PSG0005 | `[BindableValidator]` を付与されたクラスは `partial` として宣言する必要があります |
| PSG0006 | `[BindableValidator]` はクラスのみサポート（struct / interface は不可） |
| PSG1001 | `[DelegateCommand]` メソッドのシグネチャが無効です |
| PSG1002 | `[AsyncDelegateCommand]` メソッドのシグネチャが無効です |
| PSG2001 | Catch ハンドラーのメンバーが見つかりません |
| PSG2002 | Catch ハンドラーのシグネチャに互換性がありません |
| PSG2003 | CanExecute メンバーが見つかりません |
| PSG2004 | 監視対象のプロパティが見つかりません |
| PSG2005 | `[NotifyCanExecuteChangedFor]` が参照するコマンドが見つかりません |
| PSG2006 | `CanExecute` が指すメンバーのシグネチャがコマンドと互換性がありません |
| PSG3002 | `AsyncDelegateCommand` が見つかりません。Prism.Core 8.1.97 では **`MvvmAIO.Prism.Bcl.Commands`** を追加するか、Prism 9+ にアップグレード |
| PSG4001 | ServiceType が実装型と互換性がありません |
| PSG4002 | ViewModelType が解決できませんでした |
| PSG5001 | `[NotifyDataErrorInfo]` は `BindableValidator` を継承する型が必要です |
| PSG0007 | `[NavigationAware]` を付与されたクラスは `partial` として宣言する必要があります |
| PSG0008 | `[DialogAware]` を付与されたクラスは `partial` として宣言する必要があります |
| PSG6001 | フィールド付き `[ObservableProperty]` を partial property に変換することを推奨（C# 13+、Info） |
| PSG7001 | アクセス可能な `IRegionManager` が見つかりません（`[NavigateCommand]` / `[NavigateOnChanged]`） |
| PSG7002–PSG7005 | `[NavigateCommand]` / `[NavigateOnChanged]` の属性検証 |
| PSG7006–PSG7008 | `[FromNavigationParameter]` — 無効なターゲット、`[ObservableProperty]` 不足、空の Key |
| PSG7101–PSG7102 | `[ShowDialogCommand]` — `IDialogService` またはダイアログ `Name` 不足 |
| PSG7103–PSG7105 | `[FromDialogParameter]` — 無効なターゲット、`[ObservableProperty]` 不足、空の Key |

> **クイックフィックス：** PSG0001～PSG0005 にはすべて IDE のコードフィックスが用意されており、欠落している `partial` 修飾子を自動的に挿入します（波線上で Ctrl+. / Alt+Enter を押すか、「ドキュメント/プロジェクト/ソリューション内のすべての問題を修正」でコードベース全体に一括適用できます）。

## インストール

```xml
<PackageReference Include="MvvmAIO.Prism.SourceGenerators" Version="0.8.0" />
```

または:

```bash
dotnet add package MvvmAIO.Prism.SourceGenerators
```

## ビルド

```bash
dotnet build Prism.SourceGenerators.slnx
```

## Nuke ビルド

このリポジトリでは、ローカル自動化および CI のビルドオーケストレーションに [Nuke](https://nuke.build/) を使用しています。

- メインのソースソリューション: `Prism.SourceGenerators.slnx`
- ビルド自動化ソリューション: `build.slnx`（`build/_build.csproj` のみを含む）

よく使うコマンド:

```bash
# ローカルで CI フローを実行（clean + restore + compile + test）
dotnet run --project build/_build.csproj -- --target Ci --configuration Release

# NuGet パッケージを作成（必要に応じてバージョン上書き）
dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version 0.2.0

# NuGet へ公開（MvvmAIO.Prism.SourceGenerators + MvvmAIO.Prism.Bcl.Commands）
dotnet run --project build/_build.csproj -- --target Publish --configuration Release --version 0.2.0 --nuget-api-key <NUGET_API_KEY>
```

## 要件

- .NET 10 SDK
- Visual Studio 2022 17.13+ / Rider / VS Code with C# Dev Kit（`.slnx` サポート）
