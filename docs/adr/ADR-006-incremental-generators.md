# ADR-006: IIncrementalGenerator 增量管线

| 字段 | 值 |
|------|-----|
| **状态** | Accepted |
| **日期** | 2026-04-29 |
| **关联上下文** | 无 — 直接决策 |

## 背景

Roslyn 推荐使用增量源生成器以支持大型解决方案的缓存与并行编译。

## 决策

所有生成器实现 **`IIncrementalGenerator`**，通过 `RegisterImplementationSourceOutput` 等增量 API 注册；优先 `ForAttributeWithMetadataName` 绑定特性。共享扩展见 `IncrementalGeneratorInitializationContextExtensions`。

## 后果

- **正面**：编译性能；与 Roslyn 4.x+ 最佳实践一致。
- **负面**：调试管线比单次 `ISourceGenerator` 复杂；须注意 `Equatable` 模型避免无效缓存。

## 参考

- `Prism.SourceGenerators/Extensions/`
