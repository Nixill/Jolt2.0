using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;

namespace Nixill.Streaming.JoltBot.BotData;

/// <summary>
///   The data of the "twitch.json" file. Access via <see cref="Data.Twitch"/>
///   or <see cref="Instance"/>.
/// </summary>
public class TwitchData
{
  /// <summary>
  ///   The twitch data instance, loaded from file.
  /// </summary>
  public static readonly TwitchData Instance = JsonSerializer.Deserialize<TwitchData>(
    File.ReadAllText("twitch.json"), Data.JOptions)!;

  /// <summary>
  ///   Save the twitch data to file.
  /// </summary>
  public void Save() => File.WriteAllText("twitch.json", JsonSerializer.Serialize(this, Data.JOptions));

  /// <summary>
  ///   This application's client ID.
  /// </summary>
  public required string ClientID { get; set; }

  /// <summary>
  ///   This application's client secret.
  /// </summary>
  public required string Secret { get; set; }

  /// <summary>
  ///   Which pair of accounts is currently active.
  /// </summary>
  public string? ActivePairUID { get; set; } = null;

  /// <summary>
  ///   Dictionary of accounts. Key is the streamer's uid.
  /// </summary>
  public required Dictionary<string, JoltTwitchAccountPair> Accounts { get; set; }
}

/// <summary>
///   A pair of twitch accounts (one streamer, one chat bot).
/// </summary>
/// <param name="LastRefresh">
///   The time the token was last refreshed.
/// </param>
public readonly record struct JoltTwitchAccountPair(JoltTwitchAccountInfo Streamer,
  JoltTwitchAccountInfo ChatBot, Instant LastRefresh);

/// <summary>
///   A single twitch account.
/// </summary>
/// <param name="UID">The account's user ID.</param>
/// <param name="DisplayName">The account's display name.</param>
/// <param name="LoginName">The account's login username.</param>
/// <param name="UserToken">The account's user access token.</param>
/// <param name="RefreshToken">The account's refresh token.</param>
/// <param name="Scopes">
///   The scopes with which the account is authorized.
/// </param>
/// <param name="AvatarURL">The account's avatar URL.</param>
public readonly record struct JoltTwitchAccountInfo(string UID, string DisplayName, string LoginName,
  string UserToken, string RefreshToken, string[]? Scopes, string AvatarURL);
