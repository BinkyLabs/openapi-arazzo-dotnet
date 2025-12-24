using BinkyLabs.OpenApi.Arazzo.Reader;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents the result of reading and parsing an OpenAPI Arazzo document.
/// </summary>
public class ReadResult
{
    /// <summary>
    /// The parsed ArazzoDocument. Null will be returned if the document could not be parsed.
    /// </summary>
    public ArazzoDocument? Document { get; set; }

    /// <summary>
    /// ArazzoDiagnostic contains the errors reported while parsing.
    /// </summary>
    public ArazzoDiagnostic? Diagnostic { get; set; }

    /// <summary>
    /// Deconstructs the result for easier assignment on the client application.
    /// </summary>
    /// <param name="document">The parsed Arazzo document.</param>
    /// <param name="diagnostic">The diagnostic information containing parsing errors.</param>
    public void Deconstruct(out ArazzoDocument? document, out ArazzoDiagnostic? diagnostic)
    {
        document = Document;
        diagnostic = Diagnostic;
    }
    /// <summary>
    /// Deconstructs the result for easier assignment on the client application.
    /// </summary>
    /// <param name="document">The parsed Arazzo document.</param>
    public void Deconstruct(out ArazzoDocument? document)
    {
        Deconstruct(out document, out _);
    }
}