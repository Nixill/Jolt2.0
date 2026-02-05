using System.Reflection;
using Nixill.Streaming.JoltBot.BotData;
using TwitchLib.Api;

namespace Nixill.Streaming.JoltBot.Twitch;

public class JoltTwitchAccountClient
{
  public readonly bool IsChatBot;
  internal TwitchAPI APIClient;
  private JoltTwitchAccountClient(bool isChatBot)
  {
    IsChatBot = isChatBot;
    APIClient = new TwitchAPI(Log.Factory)
    {
      Settings =
      {
        ClientId = Data.Twitch.ClientID
      }
    };
  }

  public static readonly JoltTwitchAccountClient Streamer = new(false);
  public static readonly JoltTwitchAccountClient ChatBot = new(true);
  public static JoltTwitchAccountClient Get(bool chatbot) => chatbot ? JoltTwitchAccountClient.ChatBot
    : JoltTwitchAccountClient.Streamer;

  public string? GetActiveAccountName() => Data.Twitch.StreamerOrChatBot(IsChatBot).ActiveAccountName;
  public async Task SetActiveAccount(string? value)
  {
    Data.Twitch.StreamerOrChatBot(IsChatBot).ActiveAccountName = value;
    Data.Twitch.Save();
    APIClient.Settings.AccessToken = Data.Twitch.StreamerOrChatBot(IsChatBot).ActiveAccount!.Value.Token;
  }
  public async Task RemoveAccount(string? value)
  {

  }

  string[] AuthScopes => [..
    typeof(JoltTwitchAccountClient).Assembly
      .GetTypes()
      .SelectMany(t => t.GetMethods())
      .SelectMany(m => m.GetCustomAttributes<UsesAuthScopeAttribute>())
      .Where(a => a.IsChatBot == this.IsChatBot)
      .Select(a => a.Scope)
  ];

  public string AuthScopeString => string.Join(" ", AuthScopes);

  public bool HasAllAuthScopes(string[] scopes) => !AuthScopes.Except(scopes).Any();

  private string ChangeState = "";
  public string GetChangeState() => ChangeState = Guid.NewGuid().ToString();
  public bool IsCorrectChangeState(string state) => ChangeState == state;
  public async Task UseAuthCode(string code, string scopes, string uri)
  {
    var authResponse = await APIClient.Auth.GetAccessTokenFromCodeAsync(code, Data.Twitch.Secret, uri);

    APIClient.Settings.AccessToken = authResponse.AccessToken;

    var tokenResponse = await APIClient.Auth.ValidateAccessTokenAsync(authResponse.AccessToken);

    var infoResponse = (await APIClient.Helix.Users.GetUsersAsync(logins: [tokenResponse.Login]))
      .Users.First(u => u.Id == tokenResponse.UserId);

    JoltTwitchAccount acct = new JoltTwitchAccount(
      Name: infoResponse.Login,
      Token: authResponse.AccessToken,
      Refresh: authResponse.RefreshToken,
      UID: tokenResponse.UserId,
      Scopes: scopes.Split(" "),
      AvatarURL: infoResponse.ProfileImageUrl
    );

    Data.Twitch.StreamerOrChatBot(IsChatBot).Accounts.RemoveAll(a => a.Name == acct.Name);
    Data.Twitch.StreamerOrChatBot(IsChatBot).Accounts.Add(acct);

    await SetActiveAccount(acct.Name);
  }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class UsesAuthScopeAttribute(string scope, bool chatBot = false) : Attribute
{
  public readonly string Scope = scope;
  public readonly bool IsChatBot = chatBot;
}
