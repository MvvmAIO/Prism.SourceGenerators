# 可观察属性 `[ObservableProperty]`

适用于继承 **`Prism.Mvvm.BindableBase`** 的类型。生成器会发出调用 **`SetProperty`** 的属性实现，行为路径与手写 Prism 属性一致（包括可覆写的 **`SetProperty`**）。

---

## 一、两种语法形态

### 1. 字段目标（所有 C# 版本）

在 **私有字段** 上使用 **`[ObservableProperty]`**：

- 生成的属性默认 **`public`**。
- 通过 **`PropertyAccess`**（位置参数或 `PropertyAccess = …`）可改为 `internal`、`protected`、`private`、`protected internal`、`private protected` 等。

```csharp
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Hello";

    [ObservableProperty(PropertyAccess.Internal)]
    private int _count;
}
```

### 2. 部分属性（C# 13+，`field` 关键字）

在 **`partial` 属性** 上使用特性；存储由 **`field`** 关键字表示的半自动属性承担，无需单独 **`_title`** 后备字段。

```csharp
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = "Hello";
}
```

**注意：** 部分属性目标的**可访问性**以属性声明为准；**`PropertyAccess`** 参数会被忽略。

同一程序集中两种写法可以混用。

---

## 二、变更钩子：`OnXxxChanging` / `OnXxxChanged`

每个可观察属性会生成若干 **`partial void`** 声明，你可按需实现任意子集：

- **`OnXxxChanging`**：在写入存储**之前**调用（含 **`RaisePropertyChanging`** 相关逻辑，见下节）。
- **`OnXxxChanged`**：在 **`SetProperty`** 完成后的回调里调用，之后再触发主属性的 **`PropertyChanged`**，以及 **`[NotifyPropertyChangedFor]`** / **`[NotifyCanExecuteChangedFor]`** 等连锁。

生成器使用 **`EqualityComparer<T>.Default.Equals`** 做相等短路；仅当新值与旧值不同时进入上述流程。

---

## 三、`INotifyPropertyChanging` 与 `FeatureSwitches`（Unreleased / 近期行为）

与 **CommunityToolkit.Mvvm** 的 **`ObservableObject`** / **`[ObservableProperty]`** 对齐方向一致（详见仓库 **CHANGELOG — Unreleased**）：

- **`FeatureSwitches.EnableINotifyPropertyChangingSupport`** 默认为 **`true`**。
- **`[BindableBase]`** 在类型链上尚未实现 **`INotifyPropertyChanging`** 时，会生成相应成员。
- **`[ObservableProperty]`** 的 setter 中会发出受控的 **`RaisePropertyChanging(nameof(...))`**，并始终生成 **`OnXxxChanging`** 分部方法供你实现。
- 若类型仅有 **`[ObservableProperty]`** 且未从基类或 **`[BindableBase]`** 获得 **`INotifyPropertyChanging`**，可能额外生成伴侣文件 **`*.ObservablePropertyChanging.g.cs`**，内含 **`PropertyChanging`**、**`RaisePropertyChanging`** 等。

若你在运行时不需要 **`PropertyChanging`** 通知，可在启动早期将 **`FeatureSwitches.EnableINotifyPropertyChangingSupport = false`**，以换取与 Toolkit 类似的性能权衡。

---

## 四、依赖通知：`[NotifyPropertyChangedFor]`

当本属性变化时，额外对其它只读/计算属性触发 **`PropertyChanged`**：

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]
private string _firstName = "";

public string FullName => $"{FirstName} {LastName}";
```

多个名字：`[NotifyPropertyChangedFor(nameof(A), nameof(B))]` 或多个特性实例。

---

## 五、命令刷新：`[NotifyCanExecuteChangedFor]`

在 **`RaisePropertyChanged`** 之后，对列出的命令属性调用 **`RaiseCanExecuteChanged()`**。

- 名称可以是已有命令属性，或由 **`[DelegateCommand]`** / **`[AsyncDelegateCommand]`** 从方法 **`Save`** 生成的 **`SaveCommand`**。
- 解析失败 → **PSG2005**（警告），setter 仍会生成，修复命名即可。

---

## 六、把特性转发到生成的属性上

| 目标类型 | 写法 |
|----------|------|
| **字段** | 在字段上使用 **`[property: YourAttribute(...)]`** |
| **部分属性** | 直接写在 **`partial` 属性** 上（生成器自有特性会被剥离） |

转发时使用**完全限定**的特性类型名；参数表达式**原样**写入生成文件。若生成文件看不到你的 **`using`**，请在参数中使用字面量、**`nameof`**、**`typeof`** 或完全限定类型名。

---

## 七、`[BindableBase]`：不继承 Prism 的 `BindableBase` 时

用于**未**继承 **`Prism.Mvvm.BindableBase`**、且基类链上未实现 **`INotifyPropertyChanged`** 的类型，生成 **`SetProperty` / `RaisePropertyChanged`** 等标准实现。若已继承 **`BindableBase`** 或已有 **`INotifyPropertyChanged`**，则不会生成。

详见仓库 README 中的完整示例。

---

## 八、相关诊断

| ID | 含义（简述） |
|----|----------------|
| PSG0001 | 含 `[ObservableProperty]` 的类需 `partial` |
| PSG0003 | partial property 目标需 `partial` |

更多见 [诊断与排错](Diagnostics)。
