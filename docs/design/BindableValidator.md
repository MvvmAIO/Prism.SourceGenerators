# Design Doc: BindableValidator

> **关联 Spec**：[spec/BindableValidator.md](../spec/BindableValidator.md)

## 概述

`BindableValidatorGenerator` 生成验证基类；`ObservablePropertyGenerator` 在 `[NotifyDataErrorInfo]` 路径注入 `ValidateProperty`。

## 实现概览

- `BindableValidatorGenerator.cs`
- 0.4.0 自 `ObservableValidator` 更名为 `BindableValidator`

## 参考

- `ValidationTests.cs`
