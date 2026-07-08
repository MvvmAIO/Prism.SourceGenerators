# Design Doc: Register

> **关联 Spec**：[spec/Register.md](../spec/Register.md)

## 概述

`ContainerRegistryRegistrationGenerator` 从 `[Register]` 特性生成 `IContainerRegistry` 注册代码。

## 实现概览

- `ContainerRegistryRegistrationGenerator.cs`
- 重复注册与类型兼容性诊断 PSG4001–4002

## 参考

- `RegistrationGeneratorTests.cs`
