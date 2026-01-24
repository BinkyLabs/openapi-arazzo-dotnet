// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Text.Json.Nodes;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// A simple object to allow referencing other components in the specification, internally and externally.
/// </summary>
public class BaseArazzoReference : IArazzoSerializable
{
    /// <summary>
    /// The element type referenced.
    /// </summary>
    public ReferenceType Type { get; init; }

    /// <summary>
    /// The identifier of the reusable component of one particular ReferenceType.
    /// If ExternalResource is present, this is the path to the component after the '#/'.
    /// For example, if the reference is 'example.json#/path/to/component', the Id is 'path/to/component'.
    /// If ExternalResource is not present, this is the name of the component without the reference type name.
    /// For example, if the reference is '#/components/schemas/componentName', the Id is 'componentName'.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets a flag indicating whether a file is a valid OpenAPI document or a fragment
    /// </summary>
    public bool IsFragment { get; init; }

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

            if (!string.IsNullOrEmpty(Id) && Id is not null &&
                (Id.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 Id.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                 Id.Contains("#/components", StringComparison.OrdinalIgnoreCase)))
            {
                return Id;
            }

            return $"#/components/{Type.GetDisplayName()}/{Id}";
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
        // Relative reference to internal JSON schema node/resource (e.g. "#/properties/b")
        if (pointer.StartsWith("#/", StringComparison.OrdinalIgnoreCase) && !pointer.Contains("/components/schemas", StringComparison.OrdinalIgnoreCase))
        {
            ReferenceV1 = ResolveRelativePointer(nodeLocation, pointer);
        }

        // Absolute reference or anchor (e.g. "#/components/schemas/..." or full URL)
        else if ((pointer.Contains('#') || pointer.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            && !string.Equals(ReferenceV1, pointer, StringComparison.OrdinalIgnoreCase))
        {
            ReferenceV1 = pointer;
        }
    }

    private static string ResolveRelativePointer(string nodeLocation, string relativeRef)
    {
        // Convert nodeLocation to path segments
        var nodeLocationSegments = nodeLocation.TrimStart('#').Split(['/'], StringSplitOptions.RemoveEmptyEntries).ToList();

        // Convert relativeRef to dynamic segments
        var relativeSegments = relativeRef.TrimStart('#').Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        // Locate the first occurrence of relativeRef segments in the full path
        for (int i = 0; i <= nodeLocationSegments.Count - relativeSegments.Length; i++)
        {
            if (relativeSegments.SequenceEqual(nodeLocationSegments.Skip(i).Take(relativeSegments.Length), StringComparer.Ordinal) &&
                nodeLocationSegments.Take(i + relativeSegments.Length).ToArray() is {Length: > 0} matchingSegments)
            {
                // Trim to include just the matching segment chain
                return $"#/{string.Join("/", matchingSegments)}";
            }
        }

        // Fallback on building a full path
        if (nodeLocation.StartsWith("#/components/schemas/", StringComparison.OrdinalIgnoreCase))
        { // If the nodeLocation is a schema, we only want to keep the first three segments which are components/schemas/{schemaName}
            return $"#/{string.Join("/", nodeLocationSegments.Take(3).Concat(relativeSegments))}";
        }
        return $"#/{string.Join("/", nodeLocationSegments.SkipLast(relativeSegments.Length).Concat(relativeSegments))}";
    }
}