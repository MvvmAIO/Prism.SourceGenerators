# Design Doc: BindableBase

> **关联 Spec**：[spec/BindableBase.md](../spec/BindableBase.md)

## 概述

`BindableBaseGenerator` 为无 `BindableBase` 基类的 partial 类型生成 INPC 基础设施。

## 实现概览

- `BindableBaseGenerator.cs`
- 与 `PropertyChangingGenerator` 协调 `INotifyPropertyChanging`

## 参考

- `BindableBaseGenerator` 单元测试
