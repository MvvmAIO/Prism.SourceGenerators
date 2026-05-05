# ObservableProperty

Applies to types that inherit **`Prism.Mvvm.BindableBase`**. The class must be **`partial`**.

## Field target (all C# versions)

Annotate a **private field**; the generator emits a property whose setter uses **`SetProperty`**. The generated property is **`public`** by default. Use **`PropertyAccess`** (positional or named) for `internal`, `protected`, `private`, etc.

```csharp
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Hello";

    [ObservableProperty(PropertyAccess.Internal)]
    private int _count;
}
```

For **partial property** targets, accessibility comes from the property declaration; **`PropertyAccess`** is ignored.

## Partial property (C# 13+ with `field`)

```csharp
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = "Hello";
}
```

Both styles can coexist in one project.

## OnChanging / OnChanged

The generator declares **`partial`** methods you can implement. `OnXxxChanging` runs **before** the storage is updated; `OnXxxChanged` runs **after** `SetProperty` completes (including `PropertyChanged` and dependent notifications).

Implement any subset of:

- `partial void OnAgeChanging(int value);`
- `partial void OnAgeChanging(int oldValue, int newValue);`
- `partial void OnAgeChanged(int value);`
- `partial void OnAgeChanged(int oldValue, int newValue);`

## NotifyPropertyChangedFor

Raise **`PropertyChanged`** for other members when this property changes:

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]
private string _firstName = "";
```

Multiple names: `[NotifyPropertyChangedFor(nameof(A), nameof(B))]` or multiple attributes.

## NotifyCanExecuteChangedFor

After `RaisePropertyChanged`, the setter calls **`Command?.RaiseCanExecuteChanged()`** for each named command (must resolve to a command property or a generated command from `[DelegateCommand]` / `[AsyncDelegateCommand]`). Unresolved names → **PSG2005** (warning); setter is still emitted.

## Forwarding attributes to the generated property

- **Field target:** use **`[property: SomeAttribute]`** on the field; those attributes are emitted on the generated property.
- **Partial property:** attributes on the partial declaration (except generator-owned attributes) are forwarded to the implementing declaration, with **fully-qualified** attribute type names.

Use literals, **`nameof`**, **`typeof`**, or fully-qualified types in attribute arguments so the generated file does not depend on your `using` directives.

## INotifyPropertyChanging

Recent releases align **`OnXxxChanging`** / **`RaisePropertyChanging`** behavior with CommunityToolkit-style patterns. See **[CHANGELOG](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/CHANGELOG.md)** for **`FeatureSwitches.EnableINotifyPropertyChangingSupport`** and companion generated files.

## BindableBase attribute

**`[BindableBase]`** on a class that does **not** inherit Prism’s **`BindableBase`** generates **`INotifyPropertyChanged`** helpers (`SetProperty`, `RaisePropertyChanged`, etc.). If the type already implements **`INotifyPropertyChanged`** through a base type, nothing is generated.

See the main [README](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/README.md) for full examples.
