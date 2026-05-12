# 构建、示例与 Wiki 维护

---

## 一、克隆本仓库后构建源码

```bash
dotnet build Prism.SourceGenerators.slnx
```

- 需要 **.NET 10 SDK**。  
- 建议使用支持 **`.slnx`** 的 IDE 版本（见 README **Requirements**）。

---

## 二、Nuke 常用命令

编排入口在 **`build/_build.csproj`**，解决方案 **`build.slnx`**。

```bash
# 接近 CI 的完整流程：clean + restore + compile + test
dotnet run --project build/_build.csproj -- --target Ci --configuration Release

# 打 NuGet 包（版本号可按需修改）
dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version 0.2.0

# 发布到 NuGet（需要 API Key）
dotnet run --project build/_build.csproj -- --target Publish --configuration Release --version 0.2.0 --nuget-api-key <NUGET_API_KEY>
```

---

## 三、示例工程

| 项目 | 说明 |
|------|------|
| **[Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples)**（独立仓库） | Avalonia 示例：Prism 8（含 **`MvvmAIO.Prism.Bcl.Commands`**）与 Prism 9，通过 NuGet 引用 **`MvvmAIO.Prism.SourceGenerators`** |

阅读 `.csproj` 可对照消费方应如何引用 **`MvvmAIO.Prism.SourceGenerators`** 与（在 Prism 8 时）**`MvvmAIO.Prism.Bcl.Commands`**。

---

## 四、把主仓库里的 `wiki/` 同步到 GitHub Wiki

**完整、多语言的权威说明**以 **[文档站点](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/)** 为准；本 Wiki 与 README 仅作简要介绍。主仓库 **`wiki/`** 目录与 **`.wiki.git`** 远程分离，便于 PR 与 Code Review；下面说明如何推送到 GitHub Wiki。

**一次性克隆 Wiki 仓库：**

```bash
git clone https://github.com/MvvmAIO/Prism.SourceGenerators.wiki.git
cd Prism.SourceGenerators.wiki
```

**每次更新（在已克隆的 Wiki 仓库目录执行）：**

```bash
git pull
# 将主仓库 wiki/ 下所有 .md 覆盖复制到本目录
git add -A
git status
git commit -m "docs: sync wiki from MvvmAIO/Prism.SourceGenerators wiki/"
git push origin master
```

> 若远程默认分支名为 **`main`**，请将最后一句中的 **`master`** 改为 **`main`**。

**在 Windows PowerShell 下从本机路径复制（示例）：**

```powershell
$repo = "C:\Code\Prism.SourceGenerators"
$wiki = "C:\path\to\Prism.SourceGenerators.wiki"
Copy-Item "$repo\wiki\*" $wiki -Force
```

推送后在浏览器打开：  
**https://github.com/MvvmAIO/Prism.SourceGenerators/wiki**

---

## 五、CI 与测试结果

工作流见 **`.github/workflows/`**。测试徽章与 **`.trx`** 制品说明见 [首页](Home) 的 CI 一节。
