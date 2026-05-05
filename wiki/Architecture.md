# 架构与打包

本文说明 **NuGet 包如何进入你的项目**、**为何存在多个 Roslyn 版本的同一生成器 DLL**，以及 **`MvvmAIO.Prism.Core`** 如何被引用。

---

## 一、仓库内的项目划分（概念）

| 路径 / 项目 | 作用 |
|-------------|------|
| **`Prism.SourceGenerators/`**（共享项） | 生成器**源代码**（通过 `.shproj` / `.projitems` 被各 Roslyn 变体引用） |
| **`Prism.SourceGenerators.Roslyn4001`** … **`Roslyn5000`** | 针对 **不同 Roslyn / 编译器 API 版本** 编出的 **`Prism.SourceGenerators.dll`** |
| **`Prism.SourceGenerators.Core`** | 产出 **`MvvmAIO.Prism.Core.dll`**（特性定义），打进主 NuGet 的 **`lib/netstandard2.0`** |
| **`Prism.SourceGenerators.Package`** | **NuGet 包工程**：收集多份 analyzer DLL + Core + MSBuild **targets** |
| **`Prism.Bcl.Commands`** | 独立包 **`MvvmAIO.Prism.Bcl.Commands`**（Prism 8 异步命令兼容） |

Avalonia 示例应用已迁至独立仓库 **[Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples)**。

---

## 二、NuGet 包内目录结构（逻辑）

主包 **`MvvmAIO.Prism.SourceGenerators`** 大致包含：

- **`build/MvvmAIO.Prism.SourceGenerators.targets`** — 选择分析器路径、注入 **`MvvmAIO.Prism.Core`** 引用。  
- **`build/MvvmAIO.Prism.SourceGenerators.props`** — 包导入标记等。  
- **`analyzers/dotnet/roslyn4.0/cs/Prism.SourceGenerators.dll`**（以及 **roslyn4.3**、**roslyn4.12**、**roslyn5.0**）— 四套与编译器版本匹配的二进制。  
- **`lib/netstandard2.0/MvvmAIO.Prism.Core.dll`** — 特性程序集。

**不包含** Prism 8 的 **`AsyncDelegateCommand`** 实现 — 见独立包 **`MvvmAIO.Prism.Bcl.Commands`**（CHANGELOG **0.2.0** 起的设计）。

---

## 三、MSBuild 如何选择 `roslyn4.0` / `4.3` / `4.12` / `5.0`

逻辑在 **`MvvmAIO.Prism.SourceGenerators.targets`** 中（简化理解）：

1. 读取 **`$(CscToolPath)$(CscToolExe)`** 的文件版本（若文件不存在则走回退）。  
2. 解析 **Major / Minor**：  
   - **Major == 4** 且 **Minor ≤ 0** → **`roslyn4.0`**  
   - **Major == 4** 且 **Minor ≤ 3** → **`roslyn4.3`**  
   - **Major == 4** 且更高 Minor → **`roslyn4.12`**  
   - **Major ≥ 5** → **`roslyn5.0`**  
3. 若仍无法决定 → 默认 **`roslyn4.12`**（与设计时 **MSB4086** 等防御性回退有关，见 CHANGELOG）。

最终 **`Analyzer`** 项指向：

`analyzers/dotnet/<选定目录>/cs/Prism.SourceGenerators.dll`

---

## 四、何时关闭自动 Analyzer

若通过 **`ProjectReference`** 引用生成器工程（例如本仓库的示例），通常应设置：

```xml
<PropertyGroup>
  <MvvmAIOPrismSourceGeneratorsImportAnalyzers>false</MvvmAIOPrismSourceGeneratorsImportAnalyzers>
</PropertyGroup>
```

避免 NuGet 包里的 analyzer 与本地工程引用**重复**加载（targets 文件顶部注释说明）。

---

## 五、`MvvmAIO.Prism.Core` 的自动引用

**`_MvvmAIO_AddMvvmPrismCoreReference`** 目标在 **`ResolveAssemblyReferences`** 之前运行：

- 若 **`ReferencePath`** 中**已有** **`MvvmAIO.Prism.Core`**，则**不再**添加。  
- 否则从包内 **`lib/netstandard2.0`**（或从仓库相对路径的开发布局）添加 **`Reference`**。

因此：你仍可手动引用 **`MvvmAIO.Prism.Core`** 以覆盖版本或做拆分引用；手动引用时自动注入会跳过。

---

## 六、延伸阅读

- 变更与破坏性调整：**[CHANGELOG](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/CHANGELOG.md)**  
- 命令行打包与发布：**[构建、示例与 Wiki 维护](Build-and-samples)**  
