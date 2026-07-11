# Design Doc: Register

## 概述

`ContainerRegistryRegistrationGenerator` 从 `[Register]` 特性生成 `IContainerRegistry` 注册代码。

## 实现概览

- `ContainerRegistryRegistrationGenerator.cs`
- 重复注册与类型兼容性诊断 PSG4001–4002

## API 与契约

`[Register(ServiceType = ..., Name = ..., Singleton = ...)]` 应用于 partial class，生成 `IContainerRegistry` 扩展或注册方法片段（视实现版本）。

### 诊断

| ID | 说明 |
|----|------|
| PSG4001 | ServiceType 与实现不兼容 |
| PSG4002 | ViewModelType 无法解析 |

### 不变量

- 检测重复注册，并保留 Prism 8 兼容路径。

### 不在范围内

- 视图导航注册（Prism `RegisterForNavigation` 在 View 层）。

## 参考

- `RegistrationGeneratorTests.cs`
