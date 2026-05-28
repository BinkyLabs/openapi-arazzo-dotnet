// Licensed under the MIT license.

using System.Reflection;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Extensions;

public class StringExtensionsNonDisplayEnumTests
{
    private enum NoDisplayEnum
    {
        First,
        Second
    }

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
    public void TryGetEnumFromDisplayName_EnumWithoutDisplayAttribute_ReturnsFalse()
    {
        Assert.False(InvokeTry<NoDisplayEnum>("First", out _));
        Assert.False(InvokeTry<NoDisplayEnum>("Second", out _));
    }
}