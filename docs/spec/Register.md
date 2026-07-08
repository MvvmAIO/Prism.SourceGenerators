# Spec: Register

> **版本**：v0.8.1
> **关联 Design Doc**：[design/Register.md](../design/Register.md)

## API 面

`[Register(ServiceType = ..., Name = ..., Singleton = ...)]` 于 partial class：生成 `IContainerRegistry` 扩展或注册方法片段（视实现版本）。

## 诊断 ID

| ID | 说明 |
|----|------|
| PSG4001 | ServiceType 与实现不兼容 |
| PSG4002 | ViewModelType 无法解析 |

## 不变量

- 重复注册检测；Prism 8 兼容路径。

## 不在范围内

- 视图导航注册（Prism `RegisterForNavigation` 在 View 层）
