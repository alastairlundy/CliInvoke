using System.Text.Json.Serialization;

namespace DotnetToolUpdaterExample.Models;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DotnetToolListResponse))]
internal partial class DotnetToolContext : JsonSerializerContext
{
}