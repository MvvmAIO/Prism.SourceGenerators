# 文档体系标准

本文档定义本仓库维护者文档的载体与维护规则。面向库使用者的多语言手册在 [Prism.SourceGenerators.Docs](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs) 仓库；该仓库不受本文档变更影响。

## 文档载体

| 载体 | 位置 | 用途 |
|------|------|------|
| ADR | `docs/adr/` | 不应随讨论漂移的架构决策 |
| Design Doc | `docs/design/` | 每个域的 API、诊断、契约、实现与设计权衡 |
| Roadmap | `docs/ROADMAP.md` | 跨 Issue 的宏观优先级与全景 |
| Issue | GitHub Issues | 需求、缺陷与任务追踪 |
| PR | GitHub Pull Requests | 变更审查与实施讨论 |
| Release | GitHub Releases | 已发布版本的历史 |

## ADR

- 文件名为 `docs/adr/ADR-<NNN>-<kebab-case-title>.md`，编号不复用。
- Accepted ADR 正文不可修改；决策被替代时，新建 ADR 并在原文标记 `Superseded by ADR-XXX`。
- ADR 可关联 GitHub Issue、PR、Release 或相关 Design Doc。

## Design Doc

- 每个功能域或模块使用一份 `docs/design/<Name>.md`，同时记录 API 面、诊断、不变量、兼容基线、非目标、实现细节和设计权衡。
- API、诊断或实现变更随代码 PR 同步更新相应 Design Doc。
- 新增或变更用户可见诊断时，同时更新 `AnalyzerReleases.*.md`、README 诊断表与用户文档站。

## 维护检查

- [ ] API、诊断或实现变更已更新对应 Design Doc。
- [ ] 用户可见 API、诊断或示例已同步到 Prism.SourceGenerators.Docs（可使用独立 PR）。
- [ ] 破坏性架构决策已记录为 ADR。
- [ ] 发版时已更新版本表、CHANGELOG 与 GitHub Release 信息。
