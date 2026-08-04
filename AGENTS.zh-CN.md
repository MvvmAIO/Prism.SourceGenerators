# AGENTS.zh-CN.md

面向中文贡献者与自动化代理的**摘要**。[**AGENTS.md**](AGENTS.md)（英文）为唯一 canonical 约束全文；若冲突以英文为准。

## 要点

1. **构建与 CI**：`dotnet run --project build/_build.csproj -- --target Ci --configuration Release`（或 `dotnet build` / `dotnet test`）。
2. **解决方案**：使用根目录 **`.slnx`**，勿默认新建 `.sln`。
3. **临时文件**：实验与一次性项目放在 **`.Temp/`**（已 gitignore，勿提交）。
4. **GitHub 流程**：先 Issue → 分支 PR → squash merge 到 `master`；PR 正文链接 Issue（`Fixes #NN`）。
5. **生成器变更**：同步测试（含 Verify 快照）、`CHANGELOG.md`、三语 README / wiki / [Docs 仓](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs) 用户可见页。
6. **NuGet**：主包 **MvvmAIO.Prism.SourceGenerators**；Prism 8 异步命令另需 **MvvmAIO.Prism.Bcl.Commands**（独立发布密钥 `NUGET_API_KEY_BCL`）。
7. **Roslyn 变体**：`Roslyn4001` … `Roslyn5000` 多目标；Roslyn 5.0 冒烟见 `Prism.SourceGenerators.Tests.Roslyn5000`。
8. **文档站点**：https://mvvmaio.github.io/Prism.SourceGenerators.Docs/
9. **Agent skills**：通用目录 [`.agents/skills/`](.agents/skills/)（`mattpocock/skills`）；配置见 [`docs/agents/`](docs/agents/)。勿使用 Cursor 专用 skills 目录。

完整目录结构、诊断清单与发布步骤见 [**AGENTS.md**](AGENTS.md)。
