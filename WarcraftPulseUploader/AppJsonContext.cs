using System.Text.Json.Serialization;
using WarcraftPulseUploader.Parser;

namespace WarcraftPulseUploader;

[JsonSerializable(typeof(CombatLogData))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]
internal sealed partial class AppJsonContext : JsonSerializerContext { }
