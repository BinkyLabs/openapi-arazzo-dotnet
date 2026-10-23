# Union Types

## Context

The Arazzo specification defines a lot of union types for its properties. For example [a Payload Replacement Object value property](https://spec.openapis.org/arazzo/v1.1.0.html#fixed-fields-13) can be one of any JSON value, a Runtime Expression, a Selector Object. This needs to be modeled in such a way it provides a good developer experience.

## Solutions

### A. JsonNode everywhere

A simple solution would be to make all those properties JsonNode. But that diminishes the value of using a library to parse the document and make access to inner properties complex, lacking validation etc.

### B. Native union types support

C# 15 [adds support for union types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union), this would be the preferred way to model these properties since it receives native compiler/runtime/editor support and would feel *natural* to developers familiar with the language, or the concept in another language.

The major drawback is this language feature relies on runtime infrastructure that's [only available starting net11.0 and above](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/unions#prerequisites) (release planned for November 2026). This requirement applies not only to the developer building this library, but the ones consuming it, and the deployed applications.

### C. Dedicated union types

Adding "intermediate models" with a property for each of the union type members to use exclusively, and which handles the serialization/deserialization complexity.

Additionally, we can add implicit conversions to provide a better experience to the consumer.

#### Example - Assigning the properties

```csharp
var myPayloadReplacement = new ArazzoPayloadReplacement
{
    // Value is ArazzoPayloadReplacementValueUnion
    Value = "string"
};
// Works because string -> JsonNode -> ArazzoPayloadReplacementValueUnion thanks to implicit conversion operators

var myPayloadReplacement2 = new ArazzoPayloadReplacement
{
    // Value is ArazzoPayloadReplacementValueUnion
    Value = new ArazzoRuntimeExpression("expression")
};
// Also works because of the implicit convertors
```

#### Example - Testing the properties

```csharp
var myPayloadReplacement = new ArazzoPayloadReplacement
{
    // Value is ArazzoPayloadReplacementValueUnion
    Value = "string"
};

var isJsonNode1 = myPayloadReplacement is JsonNode;
// false because the implicit conversion is never used in combination with the is keyword by language design

var isJsonNode2 = myPayloadReplacement is { Value: JsonNode };
// true because we're inspecting the sub-property

// usage

if (myPayloadReplacement is { Value: JsonNode jsonNodeValue })
{
    // do something with jsonNodeValue
}
```

Arguably this brings a little more ceremony than the native union types where the **isJsonNode1** example would return true.

#### D. Mix B and C

We *could* mix B and C using conditional compilation (offer B for net11+ TFMs, and C for net10- TFMs). While this sounds like a good idea at first, it brings major drawbacks:

- It adds a lot of complexity to the code, and subsequently increases maintenance efforts.
- Someone with existing code upgrading their application from net10- to net11+ would all of a sudden get source breaking changes all over.
- net11 is not generally available at the time of writing (extra setup required for Continuous integration).

### Decision

We'll go with option C, and use xUnion as a naming convention. This can be revisited once net10 falls out of support (Nov 2028).
