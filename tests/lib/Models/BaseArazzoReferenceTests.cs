// Licensed under the MIT license.

using System.Reflection;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Models;

public class BaseArazzoReferenceTests
{
    private static void InvokeSetJsonPointerPath(BaseArazzoReference reference, string pointer, string nodeLocation)
    {
        var method = typeof(BaseArazzoReference).GetMethod("SetJsonPointerPath", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(reference, new object?[] { pointer, nodeLocation });
    }

    [Fact]
    public void ReferenceV1_WithComponentId_ReturnsFormattedReference()
    {
        var reference = new BaseArazzoReference { Type = ReferenceType.Parameter, Id = "myParam" };

        Assert.Equal("$components.parameters.myParam", reference.ReferenceV1);
    }

    [Fact]
    public void ReferenceV1_WithHttpUrlId_ReturnsId()
    {
        var reference = new BaseArazzoReference { Type = ReferenceType.Parameter, Id = "http://example.com/ref" };

        Assert.Equal("http://example.com/ref", reference.ReferenceV1);
    }

    [Fact]
    public void ReferenceV1_WithHttpsUrlId_ReturnsId()
    {
        var reference = new BaseArazzoReference { Type = ReferenceType.SuccessAction, Id = "https://example.com/ref" };

        Assert.Equal("https://example.com/ref", reference.ReferenceV1);
    }

    [Fact]
    public void ReferenceV1_WithComponentsInId_ReturnsId()
    {
        var reference = new BaseArazzoReference { Type = ReferenceType.Input, Id = "$components.parameters.foo" };

        Assert.Equal("$components.parameters.foo", reference.ReferenceV1);
    }

    [Fact]
    public void ReferenceV1_WhenSetExplicitly_ReturnsCachedValue()
    {
        var reference = new BaseArazzoReference { Type = ReferenceType.Parameter, Id = "myParam" };
        InvokeSetJsonPointerPath(reference, "http://external/spec.json#/components/parameters/foo", "$");

        Assert.Equal("http://external/spec.json#/components/parameters/foo", reference.ReferenceV1);
    }

    [Fact]
    public void CopyConstructor_CopiesProperties()
    {
        var hostDocument = new ArazzoDocument();
        var original = new BaseArazzoReference
        {
            Type = ReferenceType.Parameter,
            Id = "abc",
            HostDocument = hostDocument
        };

        var copy = new BaseArazzoReference(original);

        Assert.Equal(original.Type, copy.Type);
        Assert.Equal(original.Id, copy.Id);
        Assert.Same(hostDocument, copy.HostDocument);
    }

    [Fact]
    public void CopyConstructor_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BaseArazzoReference(null!));
    }

    [Fact]
    public void SerializeAsV1_WritesReference()
    {
        var reference = new BaseArazzoReference { Type = ReferenceType.Parameter, Id = "myParam" };
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        reference.SerializeAsV1(writer);

        var json = textWriter.ToString();
        Assert.Contains("\"reference\"", json);
        Assert.Contains("$components.parameters.myParam", json);
    }

    [Fact]
    public void SerializeAsV1_ThrowsOnNullWriter()
    {
        var reference = new BaseArazzoReference { Type = ReferenceType.Parameter, Id = "myParam" };

        Assert.Throws<ArgumentNullException>(() => reference.SerializeAsV1(null!));
    }

    [Fact]
    public void SetJsonPointerPath_WithHttpPointer_SetsReference()
    {
        var reference = new BaseArazzoReference { Type = ReferenceType.Parameter, Id = "myParam" };

        InvokeSetJsonPointerPath(reference, "http://external/spec.json#/components/parameters/foo", "$");

        Assert.Equal("http://external/spec.json#/components/parameters/foo", reference.ReferenceV1);
    }

    [Fact]
    public void SetJsonPointerPath_WithRelativePointer_ResolvesToFullPath()
    {
        var reference = new BaseArazzoReference { Type = ReferenceType.Parameter, Id = "myParam" };

        InvokeSetJsonPointerPath(reference, "$.foo.bar", "$.workflows.steps.foo.bar");

        Assert.Equal("$workflows.steps.foo.bar", reference.ReferenceV1);
    }

    [Fact]
    public void SetJsonPointerPath_WithRelativePointerNoMatch_FallsBackToConcatenation()
    {
        var reference = new BaseArazzoReference { Type = ReferenceType.Parameter, Id = "myParam" };

        InvokeSetJsonPointerPath(reference, "$.unmatched.path", "$.workflows.steps");

        Assert.Equal("$unmatched.path", reference.ReferenceV1);
    }

    [Fact]
    public void SetJsonPointerPath_WithSchemaComponents_DoesNotSetRelative()
    {
        var reference = new BaseArazzoReference { Type = ReferenceType.Parameter, Id = "myParam" };

        InvokeSetJsonPointerPath(reference, "$.components/schemas/Foo", "$.workflows");

        Assert.Equal("$components.parameters.myParam", reference.ReferenceV1);
    }

    [Fact]
    public void SetJsonPointerPath_WithSamePointerAsReference_DoesNotReplace()
    {
        var reference = new BaseArazzoReference { Type = ReferenceType.Parameter, Id = "myParam" };
        InvokeSetJsonPointerPath(reference, "http://external/spec.json#/foo", "$");

        InvokeSetJsonPointerPath(reference, "http://external/spec.json#/foo", "$");

        Assert.Equal("http://external/spec.json#/foo", reference.ReferenceV1);
    }
}