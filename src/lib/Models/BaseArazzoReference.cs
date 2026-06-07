// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// A simple object to allow referencing other components in the specification, internally and externally.
/// </summary>
public class BaseArazzoReference : IArazzoSerializable
{
    /// <summary>
    /// External resource in the reference.
    /// </summary>
    internal string? ExternalResource { get; init; }

    /// <summary>
    /// The element type referenced.
    /// </summary>
    public ReferenceType Type { get; init; }

    /// <summary>
    /// The identifier of the reusable component of one particular ReferenceType.
    /// For example, if the reference is '$components/schemas/componentName', the Id is 'componentName'.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets a flag indicating whether a file is a valid OpenAPI document or a fragment
    /// </summary>
    public bool IsFragment { get; init; }

    /// <summary>
    /// Gets a flag indicating whether this reference is an external reference.
    /// </summary>
    internal bool IsExternal => ExternalResource != null;

    /// <summary>
    /// Gets a flag indicating whether this reference is local.
    /// </summary>
    internal bool IsLocal => ExternalResource == null;

    private ArazzoDocument? hostDocument;
    /// <summary>
    /// The ArazzoDocument that is hosting the OpenApiReference instance. This is used to enable dereferencing the reference.
    /// </summary>
    public ArazzoDocument? HostDocument { get => hostDocument; init => hostDocument = value; }

    private string? _referenceV1;
    /// <summary>
    /// Gets the full reference string for v1.0.
    /// </summary>
    public string? ReferenceV1
    {
        get
        {
            if (!string.IsNullOrEmpty(_referenceV1))
            {
                return _referenceV1;
            }

            if (IsExternal)
            {
                return GetExternalReferenceV1();
            }

            if (!string.IsNullOrEmpty(Id) && Id is not null &&
                (Id.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 Id.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                 Id.Contains("$components", StringComparison.OrdinalIgnoreCase) ||
                 Id.Contains("#/components", StringComparison.OrdinalIgnoreCase) ||
                 Id.StartsWith("#/", StringComparison.OrdinalIgnoreCase)))
            {
                return Id;
            }

            return $"$components.{Type.GetDisplayName()}.{Id}";
        }
        private set
        {
            if (value is not null)
            {
                _referenceV1 = value;
            }
        }
    }

    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public BaseArazzoReference() { }

    /// <summary>
    /// Initializes a copy instance of the <see cref="BaseArazzoReference"/> object
    /// </summary>
    public BaseArazzoReference(BaseArazzoReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ExternalResource = reference.ExternalResource;
        Type = reference.Type;
        Id = reference.Id;
        HostDocument = reference.HostDocument;
    }

    /// <inheritdoc/>
    public virtual void SerializeAsV1(IOpenApiWriter writer)
    {
        SerializeInternal(writer);
    }

    /// <summary>
    /// Serialize <see cref="BaseArazzoReference"/>
    /// </summary>
    private void SerializeInternal(IOpenApiWriter writer, Action<IOpenApiWriter>? callback = null)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();
        if (callback is not null)
        {
            callback(writer);
        }

        // $ref
        writer.WriteProperty(OpenApiConstants.DollarRef, ReferenceV1);

        writer.WriteEndObject();
    }

    internal void SetJsonPointerPath(string pointer, string nodeLocation)
    {
        // Relative reference to internal JSON schema node/resource (e.g. "$/properties/b")
        if ((pointer.StartsWith("$", StringComparison.OrdinalIgnoreCase) || pointer.StartsWith("#/", StringComparison.OrdinalIgnoreCase)) &&
            !IsComponentReference(pointer))
        {
            ReferenceV1 = ResolveRelativePointer(nodeLocation, pointer);
        }

        // Absolute reference or anchor (e.g. "$components/schemas/..." or full URL)
        else if ((pointer.Contains('#') || pointer.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            && !string.Equals(ReferenceV1, pointer, StringComparison.OrdinalIgnoreCase))
        {
            ReferenceV1 = pointer;
        }
    }

    private static string ResolveRelativePointer(string nodeLocation, string relativeRef)
    {
        // Convert nodeLocation to path segments
        var nodeLocationSegments = nodeLocation.StartsWith("#/", StringComparison.OrdinalIgnoreCase)
            ? nodeLocation.TrimStart('#').Split(['/'], StringSplitOptions.RemoveEmptyEntries).ToList()
            : nodeLocation.TrimStart('$').Split(['.'], StringSplitOptions.RemoveEmptyEntries).ToList();

        // Convert relativeRef to dynamic segments
        var relativeSegments = relativeRef.StartsWith("#/", StringComparison.OrdinalIgnoreCase)
            ? relativeRef.TrimStart('#').Split(['/'], StringSplitOptions.RemoveEmptyEntries)
            : relativeRef.TrimStart('$').Split(['.'], StringSplitOptions.RemoveEmptyEntries);

        // Locate the first occurrence of relativeRef segments in the full path
        for (int i = 0; i <= nodeLocationSegments.Count - relativeSegments.Length; i++)
        {
            if (relativeSegments.SequenceEqual(nodeLocationSegments.Skip(i).Take(relativeSegments.Length), StringComparer.Ordinal) &&
                nodeLocationSegments.Take(i + relativeSegments.Length).ToArray() is { Length: > 0 } matchingSegments)
            {
                // Trim to include just the matching segment chain
                return relativeRef.StartsWith("#/", StringComparison.OrdinalIgnoreCase)
                    ? $"#/{string.Join("/", matchingSegments)}"
                    : $"${string.Join(".", matchingSegments)}";
            }
        }

        if (relativeRef.StartsWith("#/", StringComparison.OrdinalIgnoreCase))
        {
            if (nodeLocation.StartsWith("#/components/inputs/", StringComparison.OrdinalIgnoreCase))
            {
                return $"#/{string.Join("/", nodeLocationSegments.Take(3).Concat(relativeSegments))}";
            }

            return $"#/{string.Join("/", nodeLocationSegments.SkipLast(relativeSegments.Length).Concat(relativeSegments))}";
        }

        return $"${string.Join(".", nodeLocationSegments.SkipLast(relativeSegments.Length).Concat(relativeSegments))}";
    }

    internal void EnsureHostDocumentIsSet(ArazzoDocument currentDocument)
    {
        ArgumentNullException.ThrowIfNull(currentDocument);
        hostDocument ??= currentDocument;
    }

    internal static string? GetPropertyValueFromNode(JsonObject jsonObject, string key) =>
        jsonObject.TryGetPropertyValue(key, out var jsonNode) &&
        jsonNode is JsonValue valueCast &&
        valueCast.TryGetValue<string>(out var strValue)
            ? strValue
            : null;

    private static bool IsComponentReference(string pointer)
    {
        return pointer.StartsWith("$components.", StringComparison.OrdinalIgnoreCase) ||
               pointer.StartsWith("#/components/", StringComparison.OrdinalIgnoreCase) ||
               pointer.StartsWith("$.components/", StringComparison.OrdinalIgnoreCase) ||
               pointer.Contains("#/components/", StringComparison.OrdinalIgnoreCase);
    }

    private string? GetExternalReferenceV1()
    {
        if (Id is null)
        {
            return ExternalResource;
        }

        if (IsFragment)
        {
            return ExternalResource + "#" + Id;
        }

        if (Id.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            Id.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return Id;
        }

        if (Id.StartsWith("#/", StringComparison.OrdinalIgnoreCase))
        {
            return ExternalResource + Id;
        }

        return ExternalResource + "#/components/" + Type.GetDisplayName() + "/" + Id;
    }
}