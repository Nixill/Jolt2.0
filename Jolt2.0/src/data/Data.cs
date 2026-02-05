using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nixill.Streaming.JoltBot.BotData;

public static class Data
{
  /// <summary>
  ///   The options for the (de)serializer to use for JSON.
  /// </summary>
  internal static JsonSerializerOptions JOptions = new JsonSerializerOptions
  {
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    AllowTrailingCommas = true,
    WriteIndented = true
  };

  /// <summary>
  ///   The data in the "twitch.json" file.
  /// </summary>
  public static TwitchData Twitch = TwitchData.Instance;
}