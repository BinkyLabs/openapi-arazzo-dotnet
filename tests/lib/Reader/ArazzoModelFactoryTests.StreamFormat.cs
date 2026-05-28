// Licensed under the MIT license.

using System.Text;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public sealed partial class ArazzoModelFactoryTests
{
    [Fact]
    public async Task LoadFromStreamAsync_NonSeekableStream_BuffersAndDetectsFormat()
    {
        var ct = TestContext.Current.CancellationToken;
        using var inner = new MemoryStream(Encoding.UTF8.GetBytes(documentJson));
        using var nonSeekable = new NonSeekableStream(inner);

        var result = await ArazzoModelFactory.LoadFromStreamAsync(nonSeekable, cancellationToken: ct);

        Assert.NotNull(result.Document);
    }

    [Fact]
    public async Task LoadFromStreamAsync_MemoryStreamWithLeadingWhitespace_DetectsJson()
    {
        var ct = TestContext.Current.CancellationToken;
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("   " + documentJson));

        var result = await ArazzoModelFactory.LoadFromStreamAsync(ms, cancellationToken: ct);

        Assert.NotNull(result.Document);
    }
}