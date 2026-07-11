# ADR-002: 共享项目 (.shproj) 承载生成器源码

| 字段 | 值 |
|------|-----|
| **状态** | Accepted |
| **日期** | 2026-04-29 |
| **关联上下文** | 无 — 直接决策 |

## 背景

四套 Roslyn 变体共享几乎全部生成器逻辑，需避免四份拷贝漂移。

## 决策

生成器实现放在 **`Prism.SourceGenerators/`** 共享项（`.shproj` + `.projitems` + `.props`），各 `Prism.SourceGenerators.Roslyn*` 工程仅引用共享项并固定 `Microsoft.CodeAnalysis` 版本。

## 后果

- **正面**：单点修改、与 CommunityToolkit.Mvvm 多 Roslyn 模式一致。
- **负面**：共享项 MSBuild 模型对新人略陌生；修改须考虑所有变体 API 差异。

## 参考

- `Prism.SourceGenerators/Prism.SourceGenerators.props`
