using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Nixill.Streaming.JoltBot.BotData;

/// <summary>
///   Static class for accessing data files.
/// </summary>
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
  }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

  /// <summary>
  ///   The data in the "settings.json" file.
  /// </summary>
  public static readonly SettingsData Settings = SettingsData.Instance;

  /// <summary>
  ///   The data in the "twitch.json" file.
  /// </summary>
  public static readonly TwitchData Twitch = TwitchData.Instance;
}