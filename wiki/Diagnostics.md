# 诊断与排错（PSGxxxx）

以下为 **MvvmAIO Prism Source Generators** 分析器常见诊断的**速查表**。具体严重级别与消息文案以 IDE 提示为准。

---

## 一、语法与结构类（多为 Error）

| ID | 含义 | 建议处理 |
|----|------|----------|
| **PSG0001** | 含 **`[ObservableProperty]`** 的类型必须是 **`partial class`** | **Ctrl+.** → 添加 `partial`；或“修复整个文档/项目” |
| **PSG0002** | 含 **`[DelegateCommand]`** / **`[AsyncDelegateCommand]`** 的类型必须是 **`partial class`** | 同上 |
| **PSG0003** | **`[ObservableProperty]`** 用在属性上时，该属性必须是 **`partial`** | 同上 |
| **PSG0004** | **`[BindableBase]`** 标注的类型必须是 **`partial class`** | 同上 |

**PSG0001–PSG0004** 均提供 IDE **代码修复**，可批量应用。

---

## 二、命令方法签名类

| ID | 含义 | 建议处理 |
|----|------|----------|
| **PSG1001** | 不符合 **`[DelegateCommand]`** 的 execute 约定 | 检查返回类型（**`Task<TResult>`** 不支持）、**`CancellationToken`** 与 **`ValueTask`** 的组合等 |
| **PSG1002** | 不符合 **`[AsyncDelegateCommand]`** 的约定 | 对照 README 中异步方法示例 |

---

## 三、命名解析与兼容性（部分为 Warning）

| ID | 含义 | 建议处理 |
|----|------|----------|
| **PSG2001** | **`Catch`** 指向的处理程序未找到 | 检查 **`nameof`** 与成员是否存在、是否 **`partial`/`private` 可见** |
| **PSG2002** | **`Catch`** 处理程序签名不兼容 | 应为可接受 **`Exception`**（及可选参数）的形态 |
| **PSG2003** | **`CanExecute`** 成员未找到 | 检查 **`nameof`**、拼写、访问性 |
| **PSG2004** | **`[ObservesProperty]`** 中的属性未找到 | 确认属性名与生成属性名一致（字段目标会去掉下划线前缀生成 PascalCase） |
| **PSG2005** | **`[NotifyCanExecuteChangedFor]`** 中的命令名无法解析 | 修正为已有 **`XxxCommand`** 或由 **`[DelegateCommand]`** 生成的方法名对应命令 |
| **PSG2006** | **`CanExecute`** 成员存在但**不能**作为当前 execute 的 `CanExecute` 委托 | 核对 **`bool M()`** / **`bool M(T)`** / **`Func<bool>`** 等形态 |

---

## 四、程序集 / 平台类

| ID | 含义 | 建议处理 |
|----|------|----------|
| **PSG3002** | 编译期找不到 **`AsyncDelegateCommand`** | **Prism 9+**：一般只需主生成器包；**Prism.Core 8.1.97**：安装 **`MvvmAIO.Prism.Bcl.Commands`**；或升级到 Prism 9 |

---

## 五、排错流程（建议顺序）

1. 先看是否为 **`partial`** 缺失（**PSG0001–0004**）→ 一键修复。  
2. 异步命令 + Prism 8 → 查是否缺 **`MvvmAIO.Prism.Bcl.Commands`**（**PSG3002**）。  
3. **`nameof` 引用**错误 → **PSG2003–2005、2001–2004**。  
4. **`CanExecute` / `Catch` 签名** → **PSG2002、PSG2006、PSG1001–1002**。  

---

## 六、权威对照表

英文对照与 README 内嵌的完整段落见：  
[README — Diagnostics](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/README.md#diagnostics)
