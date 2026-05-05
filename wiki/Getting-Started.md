# 快速开始

## 1. 安装 NuGet 包

在应用或类库项目中引用（版本号请以 [NuGet](https://www.nuget.org/packages/MvvmAIO.Prism.SourceGenerators) 为准）：

```xml
<PackageReference Include="MvvmAIO.Prism.SourceGenerators" Version="0.2.0" />
```

或命令行：

```bash
dotnet add package MvvmAIO.Prism.SourceGenerators
```

**主包内含：**

- 编译期 **Analyzer**（`Prism.SourceGenerators.dll`，按本机 Roslyn 版本自动选带，见 [架构与打包](Architecture)）。
- **`MvvmAIO.Prism.Core`**：生成器用到的特性（如 `[ObservableProperty]`、`[DelegateCommand]` 等）的程序集引用；构建目标会在解析引用阶段自动加入（若你已手动引用同名程序集则跳过）。

**主包不含：** Prism 8 上的 **`AsyncDelegateCommand`** 实现体 — 见下文 Prism 8 小节。

---

## 2. Prism 版本与包组合（决策表）

| 你使用的 Prism | 异步命令（`AsyncDelegateCommand`） | 需要额外安装的包 |
|----------------|-----------------------------------|------------------|
| **Prism 9+**（自带异步命令类型） | 使用框架自带类型 | 仅 **`MvvmAIO.Prism.SourceGenerators`** |
| **Prism.Core 8.1.97** | 要用异步命令 | **`MvvmAIO.Prism.SourceGenerators`** + **`MvvmAIO.Prism.Bcl.Commands`** |

若已在 Prism 8 上写了异步命令相关生成代码，却**未**安装 **`MvvmAIO.Prism.Bcl.Commands`**，分析器会报 **PSG3002**。详见 [命令](Commands) 与 [诊断与排错](Diagnostics)。

---

## 3. 必备约定：类型必须是 `partial`

生成器会向**另一个分部类文件**里写入成员，因此：

- 带有 **`[ObservableProperty]`** 的类 → **`partial class`**
- 带有 **`[DelegateCommand]`** / **`[AsyncDelegateCommand]`** 的类 → **`partial class`**
- **`[ObservableProperty]`** 标注在 **partial property** 上 → 该属性声明 **`partial`**
- **`[BindableBase]`** 标注的类 → **`partial class`**

违反时会得到 **PSG0001–PSG0004**。在波浪线处 **Ctrl+.**（或 **Alt+Enter**）可使用代码修复批量添加 **`partial`**（支持“修复整个项目/解决方案”）。

---

## 4. 最小可用示例

```csharp
using Prism.Mvvm;
using Prism.SourceGenerators;

namespace MyApp.ViewModels;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "欢迎使用";

    [DelegateCommand]
    private void Increment()
    {
        // 业务逻辑
    }
}
```

生成后你可使用属性 **`Title`** 以及命令 **`IncrementCommand`**（命名规则：方法名 + **`Command`**）。

**`using Prism.SourceGenerators;`** 用于引入特性命名空间；基类仍来自 **`Prism.Mvvm`**。

---

## 5. 开发环境（克隆本仓库时）

- **.NET 10 SDK**
- **Visual Studio 2022 17.13+**、**Rider**，或 **VS Code + C# Dev Kit**（需能打开 **`.slnx`**）

消费 NuGet 包的应用项目**不必**与上述 SDK 完全一致，只需满足你自己的目标框架与 C# 语言版本（例如 partial property 需 C# 13+）。

---

## 6. 下一步读什么

- 属性、钩子、依赖通知 → [可观察属性](ObservableProperty)  
- 同步/异步命令、CanExecute、Prism 8 → [命令](Commands)  
- 报错编号 → [诊断与排错](Diagnostics)  
- 多 VS / Roslyn 下分析器如何选中 → [架构与打包](Architecture)  
