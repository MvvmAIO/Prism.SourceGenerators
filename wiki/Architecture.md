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

## 六、本仓库开发与 Dependabot（Roslyn / Polyfill / 测试对齐）

本节说明**源码树里**（与已发布的 NuGet 内 `roslyn4.x` 目录选择是两层概念）如何固定 **Microsoft.CodeAnalysis.\*** 与相关包，以及为何在 **Dependabot** 中忽略部分包，避免再次出现「测试工程用 Roslyn 5.x、生成器变体却是 4.12」的漂移。

### 6.1 生成器变体工程（`Prism.SourceGenerators.Roslyn*`）

共享逻辑在 **`Prism.SourceGenerators/Prism.SourceGenerators.props`**：

| MSBuild 属性 | 作用 |
|--------------|------|
| **`PrismSourceGeneratorRoslynVersion`** | 由工程名 **`Prism.SourceGenerators.Roslyn<MAJOR><MINOR2><PATCH>`** 解析（与 **CommunityToolkit.Mvvm** 多 Roslyn 变体命名一致），用于 **`Microsoft.CodeAnalysis.CSharp`** 与 **`Microsoft.CodeAnalysis.CSharp.Workspaces`** 的 **PackageReference** 版本。 |
| **`PrismSourceGeneratorCodeAnalysisAnalyzersVersion`** | **`Microsoft.CodeAnalysis.Analyzers`** 的版本：Roslyn **&lt; 5.0** 的变体使用 **3.11.0**；Roslyn **≥ 5.0**（如 **`Roslyn5000`**）使用 **5.3.0**。Analyzers 与编译器 API 的发布节奏不同，因此**不与** `PrismSourceGeneratorRoslynVersion` **强行捆成同一版本号**。 |

每个 **`Prism.SourceGenerators.RoslynXXXX`** 工程仍产出同名 **`Prism.SourceGenerators.dll`**，再按第二节所述被打进 NuGet 的不同 **`analyzers/dotnet/roslyn…/cs/`** 目录。

### 6.2 根目录 `Directory.Build.props`

仓库根 **`Directory.Build.props`** 集中维护：

- **`PolyfillVersion`** — 全仓库 **Polyfill** 包版本（生成器与 **Core** 等一致）。  
- **`PrismSourceGeneratorsTestsRoslynVersion`** — **单元测试 / 集成测试** 工程中 **`Microsoft.CodeAnalysis.*`** 的宿主版本，**必须与** 测试项目 **`ProjectReference`** 所指向的 **`Prism.SourceGenerators.Roslyn*`** 变体一致（当前默认 **4.12.0**，对应 **`Roslyn4120`**）。

**`Prism.SourceGenerators.Tests.Roslyn5000`** 对 **Roslyn5000** 做冒烟测试（工程内覆盖 `PrismSourceGeneratorsTestsRoslynVersion` 为 **5.0.0**），含 **`[ObservableProperty]`** 与 **`[DelegateCommand]`**（含 **`Task<TResult>`** execute）路径；完整快照与回归仍在 **`Prism.SourceGenerators.Tests`**（**4.12.0** / **Roslyn4120**）。

### 6.3 集成测试与 `Microsoft.Bcl.AsyncInterfaces`

**`Prism.SourceGenerators.Integration.Tests`** 与 **`Prism.SourceGenerators.Roslyn4120`** 上的 **`Microsoft.Bcl.AsyncInterfaces`** 需与 **`Prism.Core`**、**`Microsoft.CodeAnalysis` 4.12** 等依赖图一致（避免 **MSB3277** 等绑定冲突），版本由维护者**手动协调**，不交给机器人单独升级。

### 6.4 Dependabot 忽略列表

**`.github/dependabot.yml`** 中 NuGet 更新已 **ignore** 以下包（防止与上述策略冲突）：

- **`Prism.Core`**  
- **`Microsoft.CodeAnalysis.CSharp`**、**`Microsoft.CodeAnalysis.CSharp.Workspaces`**、**`Microsoft.CodeAnalysis.Analyzers`**  
- **`Microsoft.Bcl.AsyncInterfaces`**

其余依赖（如 **xUnit**、**Verify**、**GitHub Actions** 等）仍可按周由 Dependabot 提议升级；若需把更多包改为手动管理，在同一 **`ignore:`** 下追加 **`dependency-name`** 即可。

---

## 七、延伸阅读

- 变更与破坏性调整：**[CHANGELOG](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/CHANGELOG.md)**  
- 命令行打包与发布：**[构建、示例与 Wiki 维护](Build-and-samples)**  
