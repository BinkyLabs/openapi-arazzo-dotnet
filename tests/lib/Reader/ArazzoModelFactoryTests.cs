// Licensed under the MIT license.

using System.Threading;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public sealed partial class ArazzoModelFactoryTests
{
    private readonly string documentJson =
        """
        {
          "arazzo": "1.0.0",
          "info": {
            "title": "Sample Arazzo",
            "version": "1.0.0"
          },
          "sourceDescriptions": [
            {
              "name": "source1",
              "url": "https://example.com/api",
              "type": "openapi"
            }
          ],
          "workflows": [
            {
              "workflowId": "workflow1",
              "steps": [
                {
                  "stepId": "step1"
                }
              ]
            }
          ]
        }
        """;

    private readonly string documentYaml =
        """
        arazzo: 1.0.0
        info:
          title: Sample Arazzo
          version: 1.0.0
        sourceDescriptions:
          - name: source1
            url: https://example.com/api
            type: openapi
        workflows:
          - workflowId: workflow1
            steps:
              - stepId: step1
        """;

    [Fact]
    public async Task CanLoadANonSeekableStreamInJsonAndDetectFormat()
    {
        // Given
        using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(documentJson));
        using var nonSeekableStream = new NonSeekableStream(memoryStream);

        // When
        var result = await ArazzoModelFactory.LoadFromStreamAsync(nonSeekableStream, cancellationToken: TestContext.Current.CancellationToken);

        // Then
        Assert.NotNull(result);
        Assert.NotNull(result.Document);
        Assert.NotNull(result.Document.Info);
        Assert.Equal("Sample Arazzo", result.Document.Info.Title);
    }

    [Fact]
    public async Task CanLoadANonSeekableStreamInYamlAndDetectFormat()
    {
        // Given
        using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(documentYaml));
        using var nonSeekableStream = new NonSeekableStream(memoryStream);

        // When
        var result = await ArazzoModelFactory.LoadFromStreamAsync(nonSeekableStream, cancellationToken: TestContext.Current.CancellationToken);

        // Then
        Assert.NotNull(result);
        Assert.NotNull(result.Document);
        Assert.NotNull(result.Document.Info);
        Assert.Equal("Sample Arazzo", result.Document.Info.Title);
    }

    [Fact]
    public async Task CanLoadAnAsyncOnlyStreamInJsonAndDetectFormat()
    {
        // Given
        await using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(documentJson));
        await using var asyncOnlyStream = new AsyncOnlyStream(memoryStream);

        // When
        var result = await ArazzoModelFactory.LoadFromStreamAsync(asyncOnlyStream, cancellationToken: TestContext.Current.CancellationToken);

        // Then
        Assert.NotNull(result);
        Assert.NotNull(result.Document);
        Assert.NotNull(result.Document.Info);

        Assert.Equal("Sample Arazzo", result.Document.Info.Title);
    }

    [Fact]
    public async Task CanLoadAnAsyncOnlyStreamInYamlAndDetectFormat()
    {
        // Given
        await using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(documentYaml));
        await using var asyncOnlyStream = new AsyncOnlyStream(memoryStream);

        // When
        var result = await ArazzoModelFactory.LoadFromStreamAsync(asyncOnlyStream, cancellationToken: TestContext.Current.CancellationToken);

        // Then
        Assert.NotNull(result);
        Assert.NotNull(result.Document);
        Assert.NotNull(result.Document.Info);
        Assert.Equal("Sample Arazzo", result.Document.Info.Title);
    }

    [Fact]
    public async Task CanLoadANonSeekableStreamInJsonAndDetectFormatWhenPrecededBySpaces()
    {
        // Given
        using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("   " + documentJson));
        using var nonSeekableStream = new NonSeekableStream(memoryStream);

        // When
        var result = await ArazzoModelFactory.LoadFromStreamAsync(nonSeekableStream, cancellationToken: TestContext.Current.CancellationToken);

        // Then
        Assert.NotNull(result);
        Assert.NotNull(result.Document);
        Assert.NotNull(result.Document.Info);
        Assert.Equal("Sample Arazzo", result.Document.Info.Title);
    }

    [Fact]
    public async Task CanLoadAStringJsonAndDetectFormatWhenPrecededBySpaces()
    {
        // When
        var result = await ArazzoModelFactory.ParseAsync("   " + documentJson, cancellationToken: TestContext.Current.CancellationToken);

        // Then
        Assert.NotNull(result);
        Assert.NotNull(result.Document);
        Assert.NotNull(result.Document.Info);
        Assert.Equal("Sample Arazzo", result.Document.Info.Title);
    }

    [Fact]
    public async Task CanLoadANonSeekableStreamInJsonAndDetectFormatWhenPrecededByMultipleWhitespaces()
    {
        // Given
        using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(" \t\n\r " + documentJson));
        using var nonSeekableStream = new NonSeekableStream(memoryStream);

        // When
        var result = await ArazzoModelFactory.LoadFromStreamAsync(nonSeekableStream, cancellationToken: TestContext.Current.CancellationToken);

        // Then
        Assert.NotNull(result);
        Assert.NotNull(result.Document);
        Assert.NotNull(result.Document.Info);
        Assert.Equal("Sample Arazzo", result.Document.Info.Title);
    }

    [Fact]
    public async Task CanLoadMemoryStreamDirectly()
    {
        // Given
        using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(documentJson));

        // When
        var result = await ArazzoModelFactory.LoadFromStreamAsync(memoryStream, cancellationToken: TestContext.Current.CancellationToken);

        // Then
        Assert.NotNull(result);
        Assert.NotNull(result.Document);
        Assert.NotNull(result.Document.Info);
        Assert.Equal("Sample Arazzo", result.Document.Info.Title);
    }

    [Fact]
    public async Task LoadFormUrlAsync_WithLocalFile_LoadsDocument()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(filePath, documentJson, TestContext.Current.CancellationToken);

        try
        {
            var result = await ArazzoModelFactory.LoadFormUrlAsync(filePath, token: TestContext.Current.CancellationToken);

            Assert.NotNull(result.Document);
            Assert.Equal("Sample Arazzo", result.Document!.Info!.Title);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task LoadFormUrlAsync_WithMissingFile_ThrowsInvalidOperationException()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ArazzoModelFactory.LoadFormUrlAsync(filePath, token: TestContext.Current.CancellationToken));

        Assert.Contains("Could not open the file", exception.Message);
    }

    [Fact]
    public async Task ParseAsync_WithExternalRefLoadingEnabled_InitializesWorkspace()
    {
        var settings = new ArazzoReaderSettings();
        settings.OpenApiSettings.LoadExternalRefs = true;

        var result = await ArazzoModelFactory.ParseAsync(documentJson, settings: settings, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Document!.Info);
    }

    public sealed class AsyncOnlyStream : Stream
    {
        private readonly Stream _innerStream;

        public AsyncOnlyStream(Stream stream) : base()
        {
            _innerStream = stream;
        }

        public override bool CanSeek => _innerStream.CanSeek;

        public override long Position
        {
            get => _innerStream.Position;
            set => throw new NotSupportedException("Blocking operations are not supported");
        }

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return _innerStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override void Flush()
        {
            throw new NotSupportedException("Blocking operations are not supported.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Blocking operations are not supported.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Blocking operations are not supported.");
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Blocking operations are not supported.");
        }

        protected override void Dispose(bool disposing)
        {
            throw new NotSupportedException("Blocking operations are not supported.");
        }

        public override async ValueTask DisposeAsync()
        {
            await _innerStream.DisposeAsync();
            await base.DisposeAsync();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override bool CanTimeout => _innerStream.CanTimeout;

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return _innerStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void CopyTo(Stream destination, int bufferSize)
        {
            throw new NotSupportedException("Blocking operations are not supported.");
        }

        public override void Close()
        {
            _innerStream.Close();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _innerStream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _innerStream.EndWrite(asyncResult);
        }

        public override int ReadByte()
        {
            throw new NotSupportedException("Blocking operations are not supported.");
        }

        public override void WriteByte(byte value)
        {
            throw new NotSupportedException("Blocking operations are not supported.");
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _innerStream.FlushAsync(cancellationToken);
        }

        public override int Read(Span<byte> buffer)
        {
            throw new NotSupportedException("Blocking operations are not supported.");
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _innerStream.ReadAsync(buffer, cancellationToken);
        }

        public override int ReadTimeout
        {
            get => _innerStream.ReadTimeout;
            set => _innerStream.ReadTimeout = value;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            throw new NotSupportedException("Blocking operations are not supported.");
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _innerStream.WriteAsync(buffer, cancellationToken);
        }

        public override int WriteTimeout
        {
            get => _innerStream.WriteTimeout;
            set => _innerStream.WriteTimeout = value;
        }
    }

    public sealed class NonSeekableStream : Stream
    {
        private readonly Stream _innerStream;

        public NonSeekableStream(Stream stream) : base()
        {
            _innerStream = stream;
        }

        public override bool CanSeek => false;

        public override long Position
        {
            get => _innerStream.Position;
            set => throw new NotSupportedException("Seeking is not supported.");
        }

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return _innerStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seeking is not supported.");
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            _innerStream.Dispose();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _innerStream.DisposeAsync();
            await base.DisposeAsync();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override bool CanTimeout => _innerStream.CanTimeout;

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return _innerStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void CopyTo(Stream destination, int bufferSize)
        {
            _innerStream.CopyTo(destination, bufferSize);
        }

        public override void Close()
        {
            _innerStream.Close();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _innerStream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _innerStream.EndWrite(asyncResult);
        }

        public override int ReadByte()
        {
            return _innerStream.ReadByte();
        }

        public override void WriteByte(byte value)
        {
            _innerStream.WriteByte(value);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _innerStream.FlushAsync(cancellationToken);
        }

        public override int Read(Span<byte> buffer)
        {
            return _innerStream.Read(buffer);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _innerStream.ReadAsync(buffer, cancellationToken);
        }

        public override int ReadTimeout
        {
            get => _innerStream.ReadTimeout;
            set => _innerStream.ReadTimeout = value;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _innerStream.Write(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _innerStream.WriteAsync(buffer, cancellationToken);
        }

        public override int WriteTimeout
        {
            get => _innerStream.WriteTimeout;
            set => _innerStream.WriteTimeout = value;
        }
    }
}