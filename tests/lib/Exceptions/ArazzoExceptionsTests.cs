// Licensed under the MIT license.

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoExceptionsTests
{
    [Fact]
    public void ArazzoException_Default_HasDefaultMessage()
    {
        var ex = new ArazzoException();
        Assert.Equal("Error parsing the Arazzo document.", ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void ArazzoException_Message()
    {
        var ex = new ArazzoException("boom");
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public void ArazzoException_MessageAndInner()
    {
        var inner = new InvalidOperationException();
        var ex = new ArazzoException("boom", inner);
        Assert.Same(inner, ex.InnerException);
        Assert.Null(ex.Pointer);
        ex.Pointer = "#/foo";
        Assert.Equal("#/foo", ex.Pointer);
    }

    [Fact]
    public void ArazzoReaderException_Default()
    {
        var ex = new ArazzoReaderException();
        Assert.NotNull(ex);
    }

    [Fact]
    public void ArazzoReaderException_Message()
    {
        var ex = new ArazzoReaderException("oops");
        Assert.Equal("oops", ex.Message);
    }

    [Fact]
    public void ArazzoReaderException_MessageAndInner()
    {
        var inner = new InvalidOperationException();
        var ex = new ArazzoReaderException("oops", inner);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void ArazzoSerializationException_Default()
    {
        var ex = new ArazzoSerializationException();
        Assert.NotNull(ex);
    }

    [Fact]
    public void ArazzoSerializationException_Message()
    {
        var ex = new ArazzoSerializationException("oops");
        Assert.Equal("oops", ex.Message);
    }

    [Fact]
    public void ArazzoSerializationException_MessageAndInner()
    {
        var inner = new InvalidOperationException();
        var ex = new ArazzoSerializationException("oops", inner);
        Assert.Same(inner, ex.InnerException);
    }
}