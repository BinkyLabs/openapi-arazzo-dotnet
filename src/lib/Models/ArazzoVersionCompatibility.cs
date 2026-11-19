namespace BinkyLabs.OpenApi.Arazzo;

using Microsoft.OpenApi;

internal static class ArazzoVersionCompatibility
{
    internal static void ThrowIfUnsupportedInV1(ArazzoSpecVersion specVersion, string fieldName, string value)
    {
        if (specVersion is ArazzoSpecVersion.Arazzo1_0)
        {
            throw new ArazzoSerializationException($"The value '{value}' for '{fieldName}' is not supported in Arazzo 1.0.");
        }
    }

    internal static void ThrowIfUnsupportedInV1<T>(ArazzoSpecVersion specVersion, string fieldName, T value) where T : struct, Enum
    {
        ThrowIfUnsupportedInV1(specVersion, fieldName, value.GetDisplayName());
    }
}