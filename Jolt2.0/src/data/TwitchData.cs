using System.Text.Json;
using System.Text.Json.Serialization;

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
  ///   The "streamer" account list.
  /// </summary>
  public required JoltTwitchAccountList Streamer { get; set; }

  /// <summary>
  ///   The "chatBot" account list.
  /// </summary>
  public required JoltTwitchAccountList ChatBot { get; set; }

  /// <summary>
  ///   Gets either the streamer or chatbot account list based on a
  ///   boolean value.
  /// </summary>
  /// <param name="chatbot">true for chatbot, false for streamer.</param>
  /// <returns>The appropriate account list.</returns>
  public JoltTwitchAccountList StreamerOrChatBot(bool chatbot) => chatbot ? ChatBot : Streamer;
}

/// <summary>
///   A listing of accounts in the twitch JSON.
/// </summary>
public record class JoltTwitchAccountList
{
  /// <summary>
  ///   Username of which account is active and signed in, null for none.
  /// </summary>
  [JsonPropertyName("active")]
  public string? ActiveAccountName { get; set; }

  /// <summary>
  ///   Which account is active and signed in, null for none.
  /// </summary>
  [JsonIgnore]
  public JoltTwitchAccount? ActiveAccount =>
    (ActiveAccountName != null) ? Accounts.FirstOrDefault(a => a.Name == ActiveAccountName)
      : null;

  /// <summary>
  ///   The list of all accounts connected.
  /// </summary>
  public List<JoltTwitchAccount> Accounts { get; set; } = [];
}

/// <summary>
///   A single twitch account.
/// </summary>
/// <param name="Name">The account's sign-in name.</param>
/// <param name="Token">The account's access token.</param>
/// <param name="Refresh">The account's refresh token.</param>
/// <param name="UID">The account's user ID.</param>
/// <param name="Scopes">
///   The scopes with which the account is authorized.
/// </param>
/// <param name="AvatarURL">The account's avatar url.</param>
public readonly record struct JoltTwitchAccount(string Name, string Token, string Refresh, string UID, string[] Scopes,
  string AvatarURL);
