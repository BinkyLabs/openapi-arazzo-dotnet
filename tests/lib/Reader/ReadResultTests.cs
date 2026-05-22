// Licensed under the MIT license.

using BinkyLabs.OpenApi.Arazzo.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ReadResultTests
{
    [Fact]
    public void Deconstruct_TwoOut_ReturnsDocumentAndDiagnostic()
    {
        var doc = new ArazzoDocument();
        var diag = new ArazzoDiagnostic();
        var result = new ReadResult { Document = doc, Diagnostic = diag };

        var (d, di) = result;

        Assert.Same(doc, d);
        Assert.Same(diag, di);
    }

    [Fact]
    public void Deconstruct_OneOut_ReturnsDocument()
    {
        var doc = new ArazzoDocument();
        var result = new ReadResult { Document = doc };

        result.Deconstruct(out var d);

        Assert.Same(doc, d);
    }
}