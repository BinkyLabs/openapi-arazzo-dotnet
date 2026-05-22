// Licensed under the MIT license.

using System.Text;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ArazzoYamlReaderTests
{
    private const string ValidYaml = """
        Arazzo: 1.0.0
        info:
          title: T
          version: '1'
        sourceDescriptions: []
        """;

    [Fact]
    public async Task ReadAsync_MemoryStream_ParsesYaml()
    {
        var reader = new ArazzoYamlReader();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ValidYaml));
        var result = await reader.ReadAsync(stream, new Uri("https://example.com/"), new ArazzoReaderSettings());

        Assert.NotNull(result.Document);
        Assert.Equal("T", result.Document!.Info!.Title);
    }

    [Fact]
    public async Task ReadAsync_NonMemoryStream_BuffersThenParsesYaml()
    {
        var reader = new ArazzoYamlReader();
        using var inner = new MemoryStream(Encoding.UTF8.GetBytes(ValidYaml));
        using var nonMem = new BufferingPassThroughStream(inner);
        var result = await reader.ReadAsync(nonMem, new Uri("https://example.com/"), new ArazzoReaderSettings());

        Assert.NotNull(result.Document);
        Assert.Equal("T", result.Document!.Info!.Title);
    }

    [Fact]
    public async Task ReadAsync_NullStream_Throws()
    {
        var reader = new ArazzoYamlReader();
        await Assert.ThrowsAsync<ArgumentNullException>(() => reader.ReadAsync(null!, new Uri("https://example.com/"), new ArazzoReaderSettings()));
    }

    [Fact]
    public async Task GetJsonNodeFromStreamAsync_ReturnsJsonNode()
    {
        var reader = new ArazzoYamlReader();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ValidYaml));
        var node = await reader.GetJsonNodeFromStreamAsync(stream);
        Assert.NotNull(node);
    }

    [Fact]
    public async Task GetJsonNodeFromStreamAsync_InvalidYaml_Throws()
    {
        var reader = new ArazzoYamlReader();
        // Empty document — no yaml documents found
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.GetJsonNodeFromStreamAsync(stream));
    }

    /// <summary>Wraps an inner stream and pretends to be a non-MemoryStream so the YAML reader takes the buffer branch.</summary>
    private sealed class BufferingPassThroughStream : Stream
    {
        private readonly Stream _inner;
        public BufferingPassThroughStream(Stream inner) => _inner = inner;
        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}