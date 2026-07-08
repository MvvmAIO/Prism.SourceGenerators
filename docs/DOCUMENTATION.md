# 文档体系标准

> **权威源**。本文档定义本仓库 **文档驱动开发（Documentation-Driven Development）** 体系：所有内部文档的类型、结构、生命周期、归档规则，以及以文档为先导的开发流程。人类开发者和编码助手均须遵守。`AGENTS.md`「文档体系」章节为本文档的精简摘要。
>
> - **核心原则**：**先文档后代码** — 任何非琐碎变更，先确定它需要哪些文档、文档达到要求状态后才动代码（决策表见 [§11](#11-文档驱动开发流程)）。
> - **语言**：内部维护者文档以 **中文** 为主；面向库使用者的文档在 [Prism.SourceGenerators.Docs](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs) 仓库。
> - **冲突优先级**：`AGENTS.md` > `docs/DOCUMENTATION.md` > 其他内部文档 > `wiki/`（导读笔记）。

---

## 1. 文档类型总览

| 类型 | 目录 | 用途 | 稳定性 | 变更门槛 |
|------|------|------|--------|----------|
| **RFC** | `docs/rfc/` | 设计提案与讨论记录 | 提案阶段，频繁迭代 | 自由修改（Review 前） |
| **ADR** | `docs/adr/` | 架构决策记录（不可变卡片） | 已决策，仅追加 | 仅 Supersede，不修改原文 |
| **Spec** | `docs/spec/` | 稳定契约（特性、诊断 ID、不变量） | 版本化稳定 | 需 RFC + ADR 方可变更 |
| **Design Doc** | `docs/design/` | 实现细节、设计权衡、已知局限 | 随实现演进 | PR 随代码同步更新 |
| **Roadmap** | `docs/ROADMAP.md` | 功能与技术 backlog | 滚动维护 | 维护者评审 |
| **Plan** | `docs/plans/`（大型）/ GitHub Issue（小型） | 任务计划 | 短生命周期 | 计划内自由更新 |
| **Review** | `docs/review/` | 评审记录 | Final 后不可变 | 仅勾选行动项与修复链接 |

### 1.1 不作为独立文档类型

| 内容 | 载体 |
|------|------|
| 编码规范、兼容基线、打包、测试 | `AGENTS.md` |
| 开发环境、构建、仓库布局 | `docs/DEVELOPMENT.md` |
| 发布流程 | `docs/PUBLISHING.md` |
| 变更日志 | `CHANGELOG.md` |
| 贡献流程 | `CONTRIBUTING.md` |
| 使用者手册 | **Prism.SourceGenerators.Docs**（独立仓库） |
| 简短导读 | `README*`、`wiki/` |

---

## 2. RFC — Request for Comments

### 2.1 何时需要 RFC

| 场景 | 需要 RFC？ |
|------|-----------|
| 新增生成器 + 诊断 | ✅ 必须 |
| 新增或变更公共特性 API（破坏性） | ✅ 必须 |
| 新增诊断 ID（`PSG####`） | ✅ 必须 |
| 跨模块架构变更 | ✅ 必须 |
| 单生成器内 bug fix | ❌ Issue + PR |
| 单生成器内非破坏性 API 新增 | ❌ Issue + PR（Design Doc 须更新） |
| 文档/测试/重构 | ❌ Issue + PR |
| 工程整改（CI、Dependabot 策略） | ⚠️ 视影响面 |

### 2.2 文件命名

```
docs/rfc/<PascalCaseName>.md
```

### 2.3 Frontmatter（blockquote）

```markdown
> **状态**：Draft | Review | Accepted | Rejected | Implemented | Superseded
> **类型**：Feature | Generator | Architecture | Process
> **创建**：YYYY-MM-DD
> **更新**：YYYY-MM-DD
> **作者**：维护者 / 贡献者
> **关联 Roadmap**：FXX（如有）
> **关联 Issue**：#XXX（如有）
> **衍生 ADR**：ADR-XXX（Accepted 后填写）
```

### 2.4 生命周期

```
Draft → Review → Accepted → Implemented → archive/
                ↘ Rejected → archive/
```

已实现 / 已否决 / 已取代 → 移入 `docs/rfc/archive/`，更新 [rfc/README.md](rfc/README.md) 状态板。

模板：[rfc/_template.md](rfc/_template.md)

---

## 3. ADR — Architecture Decision Record

- RFC **Accepted** → 产出 ADR（双向链接）。
- 文件命名：`docs/adr/ADR-<NNN>-<kebab-case-title>.md`
- **Accepted 后正文不修改**；推翻时新建 ADR，旧 ADR 标 `Superseded by ADR-XXX`。
- 编号从 `001` 起，**不复用**。

模板：[adr/_template.md](adr/_template.md) · 索引：[adr/README.md](adr/README.md)

---

## 4. Spec — 规范文档

定义生成器的 **稳定契约**：特性签名、生成产出、诊断 ID、不变量、Prism 8/9 兼容基线。描述 **what**，不描述 **how**。

- 文件：`docs/spec/<GeneratorName>.md`
- **新增 API / 诊断 ID**：RFC → ADR → Spec 更新。
- **措辞修正**：直接 PR。

模板：[spec/_template.md](spec/_template.md) · 索引：[spec/README.md](spec/README.md)

---

## 5. Design Doc — 设计文档

记录 **实现细节**、增量管线、Prism 命名空间选择、设计权衡。描述 **how** 和 **why**。

- 文件：`docs/design/<GeneratorName>.md`（与 Spec 同名）
- 随代码 PR 同步更新；若导致 Spec 契约变更，须走 RFC。

模板：[design/_template.md](design/_template.md) · 索引：[design/README.md](design/README.md)

---

## 6. Roadmap

维护 [ROADMAP.md](ROADMAP.md)：`候选 → 排期 → 进行中 → 已完成（归档）`。

---

## 7. Plan — 任务计划

| 规模 | 载体 |
|------|------|
| 小型（单 PR） | GitHub Issue |
| 大型（跨多 PR / 由 RFC 衍生） | `docs/plans/<PascalCaseName>.md` + 主 Issue |

Done / Cancelled → `docs/plans/archive/`。模板：[plans/_template.md](plans/_template.md)

---

## 8. Review — 评审记录

用于 RFC 设计评审、发版前审查、大型 Plan 回顾。Final 后正文不可变。

- 命名：`docs/review/<YYYY-MM-DD>-<kebab-case-topic>.md`
- 模板：[review/_template.md](review/_template.md)

---

## 9. 归档机制（统一规则）

| 类型 | 归档目录 | 归档触发 |
|------|----------|----------|
| RFC | `docs/rfc/archive/` | Implemented / Rejected / Superseded |
| Plan | `docs/plans/archive/` | Done / Cancelled |
| Review | `docs/review/archive/` | Final 且行动项全部关闭 |
| ADR | 不移动 | 仅 Supersede 状态字段 |
| Spec / Design | 不归档 | 随实现演进 |
| Roadmap 条目 | `ROADMAP.md` 归档章节 | 功能落地 |

归档 = **移动文件 + 更新状态 + 更新 README 索引**，同一 PR 完成。

---

## 10. 目录结构

```
docs/
├── DOCUMENTATION.md
├── README.md
├── DEVELOPMENT.md
├── ROADMAP.md
├── PUBLISHING.md
├── rfc/          README.md, _template.md, archive/
├── adr/          README.md, _template.md, ADR-NNN-*.md
├── spec/         README.md, _template.md, <Generator>.md
├── design/       README.md, _template.md, <Generator>.md
├── plans/        README.md, _template.md, archive/
└── review/       README.md, _template.md, archive/
```

---

## 11. 文档驱动开发流程

**先文档后代码**：前置文档未达要求状态，不进入实现。

### 11.1 变更类型 → 文档前置条件

| 变更类型 | RFC | ADR | Plan | Review | 实现 PR 须同步 |
|----------|-----|-----|------|--------|----------------|
| 新增生成器 + 诊断 | ✅ Accepted | ✅ | 视规模 | ✅ 设计评审 | Spec + Design + CHANGELOG + Docs 站 |
| 破坏性公共 API | ✅ Accepted | ✅ | 视规模 | ✅ | Spec + CHANGELOG `Breaking` + Docs 站 |
| 新增诊断 ID | ✅ Accepted | ✅ | 视规模 | 建议 | Spec + `AnalyzerReleases.Unshipped.md` + README 诊断表 + Docs 站 |
| 非破坏性 API（单生成器） | ❌ | ❌ | ❌ | ❌ | Design Doc + CHANGELOG |
| Bug fix | ❌ | ❌ | ❌ | ❌ | CHANGELOG（用户可见时） |
| 重构（无行为变更） | ❌ | 视架构影响 | ❌ | ❌ | Design Doc（结构变化时） |
| 发版 | ❌ | ❌ | ❌ | ✅ Release 审查 | CHANGELOG 版本化 + GitHub Release |

### 11.2 新功能完整流程

```
1. Roadmap 排期
2. RFC Draft → Review → Accepted → ADR
3. Plan（跨多 PR 时）+ Issue
4. 按里程碑实现 → 每 PR 更新 Spec + Design Doc
5. RFC Implemented → archive/；Plan Done → archive/
6. CHANGELOG + Docs 站 + Samples（如需要）
```

### 11.3 与对外文档站的关系

| 文档面 | 维护时机 |
|--------|----------|
| `docs/spec/` | 契约变更时（维护者） |
| **Prism.SourceGenerators.Docs** | 用户可见 API / 诊断 / 示例变更时 |
| `README` / `wiki/` | 首屏摘要；深度内容链接到 Docs 站与本 `docs/` |

**Prism.SourceGenerators.Docs** 中的 `docs/rfc/` 可保留**面向用户的** RFC 摘要；本仓库 `docs/rfc/` 为**实现侧**完整 RFC。

---

## 12. 诊断 ID 区段（PSG）

| 区段 | 用途 |
|------|------|
| PSG0001–0008 | partial 类 / 特性目标约束 |
| PSG1001–1002 | 命令方法签名 |
| PSG2001–2006 | 命令引用解析 |
| PSG3002 | AsyncDelegateCommand 缺失 |
| PSG4001–4002 | 容器注册 |
| PSG5001 | BindableValidator |
| PSG6001 | partial property 建议（Info） |
| PSG7001–7008 | 区域导航 |
| PSG7101–7105 | 对话框服务 |

新增 ID 须在 Spec、 `AnalyzerReleases.Unshipped.md`、README 与 Docs 站同步登记；**已发布 ID 语义不可复用**。

---

## 13. 相关仓库

| 仓库 | 文档职责 |
|------|----------|
| **Prism.SourceGenerators**（本仓库） | `docs/` 内部体系 + `AGENTS.md` |
| **Prism.SourceGenerators.Docs** | 使用者 VitePress 站点 |
| **Prism.SourceGenerators.Samples** | 可运行示例；大型演示须与 Spec 验收对齐 |

本体系源自 [Skymly/DesignPatterns](https://github.com/Skymly/DesignPatterns) 文档驱动开发实践，并按 Roslyn 源生成器与 Prism MVVM 场景做了适配。
