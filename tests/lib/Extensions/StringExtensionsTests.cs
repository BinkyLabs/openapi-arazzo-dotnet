// Licensed under the MIT license.

using System.Reflection;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

using ParsingContext = BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Extensions;

public class StringExtensionsTests
{
    private static bool InvokeTry<T>(string? name, out T? result) where T : Enum
    {
        var method = typeof(StringExtensions)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name == "TryGetEnumFromDisplayName" && m.GetParameters().Length == 2);
        var generic = method.MakeGenericMethod(typeof(T));
        var args = new object?[] { name, null };
        var ok = (bool)generic.Invoke(null, args)!;
        result = (T?)args[1];
        return ok;
    }

    [Fact]
    public void TryGetEnumFromDisplayName_Found()
    {
        // SuccessActionType is a known enum decorated with DisplayAttribute.
        Assert.True(InvokeTry<ArazzoSuccessType>("end", out var result));
        Assert.Equal(ArazzoSuccessType.End, result);
    }

    [Fact]
    public void TryGetEnumFromDisplayName_NotFound_ReturnsFalse()
    {
        Assert.False(InvokeTry<ArazzoSuccessType>("not-existing", out _));
    }

    [Fact]
    public void TryGetEnumFromDisplayName_NullDisplayName_ReturnsFalse()
    {
        Assert.False(InvokeTry<ArazzoSuccessType>(null, out _));
    }

    [Fact]
    public void TryGetEnumFromDisplayName_CacheHit_OnSecondCall()
    {
        Assert.True(InvokeTry<ArazzoFailureType>("end", out _));
        Assert.True(InvokeTry<ArazzoFailureType>("retry", out _));
    }

    [Fact]
    public void TryGetEnumFromDisplayName_WithContext_RecordsDiagnostic()
    {
        var ctx = new ParsingContext(new ArazzoDiagnostic());
        var ok = "unknown-value".TryGetEnumFromDisplayName<ArazzoSuccessType>(ctx, out _);

        Assert.False(ok);
        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("not recognized"));
    }

    [Fact]
    public void TryGetEnumFromDisplayName_WithContext_FoundDoesNotRecord()
    {
        var ctx = new ParsingContext(new ArazzoDiagnostic());
        var ok = "end".TryGetEnumFromDisplayName<ArazzoSuccessType>(ctx, out _);

        Assert.True(ok);
        Assert.Empty(ctx.Diagnostic.Errors);
    }

    [Fact]
    public void ToFirstCharacterLowerCase_Empty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, string.Empty.ToFirstCharacterLowerCase());
    }

    [Fact]
    public void ToFirstCharacterLowerCase_NonEmpty_LowersFirst()
    {
        Assert.Equal("foo", "Foo".ToFirstCharacterLowerCase());
    }
}
