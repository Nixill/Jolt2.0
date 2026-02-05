using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nixill.Streaming.JoltBot.BotData;

public class TwitchData
{
  public static readonly TwitchData Instance = JsonSerializer.Deserialize<TwitchData>(
    File.ReadAllText("twitch.json"), Data.JOptions)!;

  public void Save() => File.WriteAllText("twitch.json", JsonSerializer.Serialize(this, Data.JOptions));

  public required string ClientID { get; set; }
  public required string Secret { get; set; }
  public required JoltTwitchAccountList Streamer { get; set; }
  public required JoltTwitchAccountList ChatBot { get; set; }

  public JoltTwitchAccountList StreamerOrChatBot(bool chatbot) => chatbot ? ChatBot : Streamer;
}

public record class JoltTwitchAccountList
{
  [JsonPropertyName("active")]
  public string? ActiveAccountName { get; set; }

  [JsonIgnore]
  public JoltTwitchAccount? ActiveAccount =>
    (ActiveAccountName != null) ? Accounts.FirstOrDefault(a => a.Name == ActiveAccountName)
      : null;

  public List<JoltTwitchAccount> Accounts { get; set; } = [];
}

public readonly record struct JoltTwitchAccount(string Name, string Token, string Refresh, string UID, string[] Scopes,
  string AvatarURL);
