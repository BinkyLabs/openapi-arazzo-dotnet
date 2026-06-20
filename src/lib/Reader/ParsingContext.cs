
// Licensed under the MIT license.

using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Reader;

/// <summary>
/// The Parsing Context holds temporary state needed whilst parsing an OpenAPI Document
/// </summary>
public class ParsingContext
{
    private readonly Stack<string> _currentLocation = new();
    private readonly Dictionary<string, object> _tempStorage = new();
    private readonly Dictionary<object, Dictionary<string, object>> _scopedTempStorage = new();
    private readonly Dictionary<string, Stack<string>> _loopStacks = new();

    /// <summary>
    /// Extension parsers
    /// </summary>
    public Dictionary<string, Func<JsonNode, ArazzoSpecVersion, IArazzoExtension>>? ExtensionParsers { get; set; } =
        new();

    internal JsonNode? JsonNode { get; set; }
    /// <summary>
    /// The base url for the document
    /// </summary>
    public Uri? BaseUrl { get; set; }

    /// <summary>
    /// Default content type for a response object
    /// </summary>
    public List<string>? DefaultContentType { get; set; }

    /// <summary>
    /// Diagnostic object that returns metadata about the parsing process.
    /// </summary>
    public ArazzoDiagnostic Diagnostic { get; }

    /// <summary>
    /// Create Parsing Context
    /// </summary>
    /// <param name="diagnostic">Provide instance for diagnostic object for collecting and accessing information about the parsing.</param>
    public ParsingContext(ArazzoDiagnostic diagnostic)
    {
        Diagnostic = diagnostic;
    }
    private static readonly string[] ArazzoV1Versions = ["1.0.0", "1.0.1"];

    /// <summary>
    /// Initiates the parsing process.  Not thread safe and should only be called once on a parsing context
    /// </summary>
    /// <param name="jsonNode">Set of Json nodes to parse.</param>
    /// <param name="location">Location of where the document that is getting loaded is saved</param>
    /// <returns>An ArazzoDocument populated based on the passed yamlDocument </returns>
    public ArazzoDocument Parse(JsonNode jsonNode, Uri location)
    {
        JsonNode = jsonNode;

        var inputVersion = GetVersion(jsonNode);

        ArazzoDocument doc;

        switch (inputVersion)
        {
            case string version when IsArazzoV1Version(version):
                VersionService = new ArazzoV1VersionService(Diagnostic);
                doc = VersionService.LoadDocument(jsonNode, location, this);
                this.Diagnostic.SpecificationVersion = ArazzoSpecVersion.Arazzo1_0;
                ValidateRequiredFields(doc, version);
                break;

            default:
                throw new OpenApiUnsupportedSpecVersionException(inputVersion);
        }

        return doc;
    }

    /// <summary>
    /// Initiates the parsing process of a fragment.  Not thread safe and should only be called once on a parsing context
    /// </summary>
    /// <param name="jsonNode"></param>
    /// <param name="version">OpenAPI version of the fragment</param>
    /// <returns>An ArazzoDocument populated based on the passed yamlDocument </returns>
    public T? ParseFragment<T>(JsonNode jsonNode, ArazzoSpecVersion version) where T : IOpenApiElement
    {
        var element = default(T);

        switch (version)
        {
            case ArazzoSpecVersion.Arazzo1_0:
                VersionService = new ArazzoV1VersionService(Diagnostic);
                element = this.VersionService.LoadElement<T>(jsonNode, this);
                break;
            default:
                throw new OpenApiUnsupportedSpecVersionException(version.ToString());

        }

        return element;
    }

    /// <summary>
    /// Gets the version of the Open API document.
    /// </summary>
    private static string GetVersion(JsonNode jsonNode)
    {
        var versionNode = new JsonPointer($"/{ArazzoConstants.ArazzoDocumentArazzo}").Find(jsonNode);

        if (versionNode is null)
        {
            throw new OpenApiException("Version node not found.");
        }

        var version = versionNode.GetScalarValue()?.Replace("\"", string.Empty);
        if (string.IsNullOrEmpty(version))
        {
            throw new OpenApiException("Version value is null or empty.");
        }

        return version;
    }

    private static bool IsArazzoV1Version(string version)
    {
        return ArazzoV1Versions.Any(supportedVersion => supportedVersion.Equals(version, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Service providing all Version specific conversion functions
    /// </summary>
    internal IArazzoVersionService? VersionService { get; set; }

    /// <summary>
    /// End the current object.
    /// </summary>
    public void EndObject()
    {
        _currentLocation.Pop();
    }

    /// <summary>
    /// Get the current location as string representing JSON pointer.
    /// </summary>
    public string GetLocation()
    {
        return "#/" + string.Join("/", _currentLocation.Reverse().Select(s => s.Replace("~", "~0").Replace("/", "~1")).ToArray());
    }

    /// <summary>
    /// Gets the value from the temporary storage matching the given key.
    /// </summary>
    public T? GetFromTempStorage<T>(string key, object? scope = null)
    {
        Dictionary<string, object>? storage;

        if (scope == null)
        {
            storage = _tempStorage;
        }
        else if (!_scopedTempStorage.TryGetValue(scope, out storage))
        {
            return default;
        }

        return storage.TryGetValue(key, out var value) ? (T)value : default;
    }

    /// <summary>
    /// Sets the temporary storage for this key and value.
    /// </summary>
    public void SetTempStorage(string key, object? value, object? scope = null)
    {
        Dictionary<string, object>? storage;

        if (scope == null)
        {
            storage = _tempStorage;
        }
        else if (!_scopedTempStorage.TryGetValue(scope, out storage))
        {
            storage = _scopedTempStorage[scope] = new();
        }

        if (value == null)
        {
            storage.Remove(key);
        }
        else
        {
            storage[key] = value;
        }
    }

    /// <summary>
    /// Starts an object with the given object name.
    /// </summary>
    public void StartObject(string objectName)
    {
        _currentLocation.Push(objectName);
    }

    /// <summary>
    /// Maintain history of traversals to avoid stack overflows from cycles
    /// </summary>
    /// <param name="loopId">Any unique identifier for a stack.</param>
    /// <param name="key">Identifier used for current context.</param>
    /// <returns>If method returns false a loop was detected and the key is not added.</returns>
    public bool PushLoop(string loopId, string key)
    {
        if (!_loopStacks.TryGetValue(loopId, out var stack))
        {
            stack = new();
            _loopStacks.Add(loopId, stack);
        }

        if (!stack.Contains(key))
        {
            stack.Push(key);
            return true;
        }
        else
        {
            return false;  // Loop detected
        }
    }

    /// <summary>
    /// Reset loop tracking stack
    /// </summary>
    /// <param name="loopid">Identifier of loop to clear</param>
    internal void ClearLoop(string loopid)
    {
        _loopStacks[loopid].Clear();
    }

    /// <summary>
    /// Exit from the context in cycle detection
    /// </summary>
    /// <param name="loopid">Identifier of loop</param>
    public void PopLoop(string loopid)
    {
        if (_loopStacks[loopid].Count > 0)
        {
            _loopStacks[loopid].Pop();
        }
    }

    private void ValidateRequiredFields(ArazzoDocument doc, string version)
    {
        if (IsArazzoV1Version(version) && JsonNode is not null)
        {
            if (doc.Info == null)
            {
                Diagnostic.Errors.Add(new OpenApiError("", $"Info is a REQUIRED field at {GetLocation()}"));
            }
            else
            {
                ValidateInfoRequiredFields(doc.Info);
            }

            if (doc.SourceDescriptions is not { Count: > 0 })
            {
                Diagnostic.Errors.Add(new OpenApiError("", $"SourceDescriptions is a REQUIRED field and MUST contain at least one entry at {GetLocation()}"));
            }

            if (doc.Workflows is not { Count: > 0 })
            {
                Diagnostic.Errors.Add(new OpenApiError("", $"Workflows is a REQUIRED field and MUST contain at least one entry at {GetLocation()}"));
            }
            else
            {
                ValidateWorkflowRequiredFields(doc.Workflows);
            }

            ValidateWorkflowParameters(doc);
        }
    }

    private void ValidateInfoRequiredFields(ArazzoInfo info)
    {
        if (string.IsNullOrEmpty(info.Title))
        {
            Diagnostic.Errors.Add(new OpenApiError("", $"Info.Title is a REQUIRED field at {GetLocation()}"));
        }

        if (string.IsNullOrEmpty(info.Version))
        {
            Diagnostic.Errors.Add(new OpenApiError("", $"Info.Version is a REQUIRED field at {GetLocation()}"));
        }
    }

    private void ValidateWorkflowRequiredFields(IEnumerable<ArazzoWorkflow> workflows)
    {
        foreach (var workflow in workflows)
        {
            if (workflow.Steps is not { Count: > 0 })
            {
                Diagnostic.Errors.Add(new OpenApiError("", $"Workflow '{workflow.WorkflowId}' steps is a REQUIRED field and MUST contain at least one entry."));
            }
        }
    }

    private void ValidateWorkflowParameters(ArazzoDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

        doc.RegisterComponents();

        if (doc.Workflows is null)
        {
            return;
        }

        foreach (var workflow in doc.Workflows)
        {
            if (workflow.Parameters is null || workflow.Parameters.Count < 2)
            {
                continue;
            }

            var parameterNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parameter in workflow.Parameters)
            {
                var parameterName = parameter.Name;
                if (string.IsNullOrEmpty(parameterName))
                {
                    continue;
                }

                if (!parameterNames.Add(parameterName))
                {
                    Diagnostic.Errors.Add(new OpenApiError(string.Empty, $"Workflow '{workflow.WorkflowId}' contains duplicate parameter name '{parameterName}'."));
                }
            }
        }
    }
}