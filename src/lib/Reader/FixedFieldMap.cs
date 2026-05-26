// Licensed under the MIT license.

using System.Text.Json.Nodes;

namespace BinkyLabs.OpenApi.Arazzo.Reader
{
    internal class FixedFieldMap<T> : Dictionary<string, Action<T, JsonNode, ParsingContext>>
    {
    }
}