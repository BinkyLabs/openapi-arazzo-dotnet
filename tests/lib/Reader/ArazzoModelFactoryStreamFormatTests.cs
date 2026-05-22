// Licensed under the MIT license.

using System.Text;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ArazzoModelFactoryStreamFormatTests
{
    private const string DocumentJson = """
        {
          "Arazzo": "1.0.0",
          "info": { "title": "T", "version": "1" },
          "sourceDescriptions": []
        }
        """;

    [Fact]
    public async Task LoadFromStreamAsync_NonSeekableStream_BuffersAndDetectsFormat()
    {
        var ct = TestContext.Current.CancellationToken;
        using var inner = new MemoryStream(Encoding.UTF8.GetBytes(DocumentJson));
        using var nonSeekable = new NonSeekableStream(inner);

        var result = await ArazzoModelFactory.LoadFromStreamAsync(nonSeekable, cancellationToken: ct);

        Assert.NotNull(result.Document);
    }

    [Fact]
    public async Task LoadFromStreamAsync_MemoryStreamWithLeadingWhitespace_DetectsJson()
    {
        var ct = TestContext.Current.CancellationToken;
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("   " + DocumentJson));

        var result = await ArazzoModelFactory.LoadFromStreamAsync(ms, cancellationToken: ct);

        Assert.NotNull(result.Document);
    }

    private sealed class NonSeekableStream : Stream
    {
        private readonly Stream _inner;
        public NonSeekableStream(Stream inner) => _inner = inner;
        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}