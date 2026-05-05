# 常见问题（FAQ）

---

## 1. 为什么我的类一堆红色波浪线，提示要加 `partial`？

生成器会把属性、命令等生成到 **另一个 `.g.cs` 分部类文件** 里。承载 **`[ObservableProperty]`**、**`[DelegateCommand]`** 等的类型必须是 **`partial`**，否则无法合并编译单元。

**处理：** 在错误上 **Ctrl+.** → “添加 partial 修饰符”，或使用“修复整个项目/解决方案”。

---

## 2. Prism 8 下用了异步命令，为什么报 PSG3002？

从 **0.2.0** 起，**`AsyncDelegateCommand`** 不再内嵌在主分析器包中。 **Prism.Core 8.1.97** 本身不带与 Prism 9 完全一致的异步命令表面时，需要单独安装 **`MvvmAIO.Prism.Bcl.Commands`**。

**处理：** 对 Prism 8 项目添加 **`MvvmAIO.Prism.Bcl.Commands`** 包引用；或升级到 **Prism 9+** 并使用框架类型。

---

## 3. `ValueTask` 可以用在 `[DelegateCommand]` 上吗？

可以。生成器会包一层 **`.AsTask()`** 以适配 Prism 现有的 **`Func<Task>`** / **`Func<T, Task>`** 构造函数。

**注意：** 若 execute 方法带 **`CancellationToken`**，则不能与 **`ValueTask` / `ValueTask<TResult>`** 按当前生成形状组合 → **PSG1001**。

---

## 4. `Task<TResult>` 能做异步 execute 的返回类型吗？

**不能**（在 **`[DelegateCommand]`** 这条路径上）。请改为无泛型 **`Task`**，或在允许的组合下使用 **`ValueTask<TResult>`**，或改用 **`[AsyncDelegateCommand]`** 并符合其签名要求。

---

## 5. 为什么设计时或 CI 里选到了意外的 Roslyn 分析器目录？

targets 根据 **`csc.exe` 的文件版本** 映射到 **`roslyn4.0` / `4.3` / `4.12` / `5.0`**。当 **`CscToolPath`** 在设计时不可用或版本解析失败时，会回退到默认 **`roslyn4.12`**（见 CHANGELOG 中 **MSB4086** 相关修复说明）。

**处理：** 确保使用受支持的 SDK/IDE；若通过特殊宿主编译，检查 **`CscToolPath`** 是否正常。

---

## 6. `[NotifyCanExecuteChangedFor]` 写了 `SaveCommand` 仍警告 PSG2005？

检查：

- 方法是否命名为 **`Save`** 且已标 **`[DelegateCommand]`**，从而生成 **`SaveCommand`**；或直接使用已有命令属性名。  
- **`nameof`** 是否拼写错误、目标是否为 **`static`** 且不可见等。

---

## 7. 特性参数里的类型在生成文件里报错找不到？

属性转发时，参数表达式 **原样** 进入生成文件。请使用 **完全限定类型名**，或仅用 **`nameof` / `typeof` / 字面量**。

---

## 8. Wiki 和 README 重复吗？

**README** 面向仓库访客与英文技术细节；**本 Wiki** 侧重中文导读、架构、排错与维护流程。二者会交叉引用，避免在 Wiki 里维护第二份易过期的超长 API 列表 — 细节仍以 README / CHANGELOG 为准。

---

## 9. 还有问题？

到主仓库提 [**Issue**](https://github.com/MvvmAIO/Prism.SourceGenerators/issues)，并附上 **最小复现**、**目标框架**、**Prism 包版本**、**MvvmAIO 包版本**、以及关键 **PSGxxxx** 编号与完整诊断文本。
