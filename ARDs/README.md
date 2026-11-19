# Architectural decision records

This section contains the architectural decision records for the project. It's an effort to leave a trace of why things are a certain way, so that when we revisit them, we have additional context.

## General design

As a general rule of thumb, this library is modeled after [Microsoft.OpenApi](https://www.nuget.org/packages/Microsoft.OpenApi) 3.X. This is where the general design for serialization, deserialization, models and more comes from. It also relies on it's "format" serialization layers (the very outer part from the format agnostic serialization to the serialized document, likewise for the deserialization).

One exception to that, because of the lower usage of this library, it also includes the YamlReader (which is in separate library in Microsoft.OpenApi). This may be revisited if strict trim dependencies become a popular demand in the future, at which point we'd create a companion library to mimic the design.

## Records

The table below lists the ADRs.

| Name | Status | Description |
| ---- | ------ | ----------- |
| [Union Types](./001-union-types.md) | Active | Should the library use C# 15 union types or its own |

> Note: Status can be `Draft`, `Active`, `Retired`.
