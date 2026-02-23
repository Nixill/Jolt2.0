using System.Text.Json;

namespace Nixill.Streaming.JoltBot.BotData;

/// <summary>
///   The data of the "settings.json" file. Access via <see cref="Data.Settings"/>
///   or <see cref="Instance"/>.
/// </summary>
public class SettingsData
{
  /// <summary>
  ///   The settings data instance, loaded from file.
  /// </summary>
  public static readonly SettingsData Instance = JsonSerializer.Deserialize<SettingsData>(
    File.ReadAllText("settings.json"), Data.JOptions)!;

  /// <summary>
  ///   Save the settings data to file.
  /// </summary>
  public void Save() => File.WriteAllText("settings.json", JsonSerializer.Serialize(this, Data.JOptions));

  /// <summary>
  ///   Get or set whether or not to use 24-hour time.
  /// </summary>
  public bool Use24HourTime { get; set; } = true;
}