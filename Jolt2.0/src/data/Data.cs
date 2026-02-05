using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nixill.Streaming.JoltBot.BotData;

public static class Data
{
  internal static JsonSerializerOptions JOptions = new JsonSerializerOptions
  {
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    AllowTrailingCommas = true,
    WriteIndented = true
  };

  public static TwitchData Twitch = TwitchData.Instance;
}