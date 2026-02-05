using System.Reflection;
using Nixill.Streaming.JoltBot.BotData;
using Nixill.Utils.Extensions;
using TwitchLib.Api;

namespace Nixill.Streaming.JoltBot.Twitch;

public class JoltTwitchAccountClient
{
  /// <summary>
  ///   Get whether this JTAC represents a chat bot or not.
  /// </summary>
  public readonly bool IsChatBot;

  /// <summary>
  ///   The actual Twitch API Client used by this JTAC.
  /// </summary>
  internal TwitchAPI APIClient;

  /// <summary>
  ///   Create one of the JTACs.
  /// </summary>
  /// <param name="isChatBot">Chat bot or streamer?</param>
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

  /// <summary>
  ///   The JTAC for the streamer account.
  /// </summary>
  public static readonly JoltTwitchAccountClient Streamer = new(false);

  /// <summary>
  ///   The JTAC for the chat bot account.
  /// </summary>
  public static readonly JoltTwitchAccountClient ChatBot = new(true);

  /// <summary>
  ///   Get the JTAC with a boolean "is chat bot" value.
  /// </summary>
  /// <param name="chatbot">Chat bot true, streamer false</param>
  /// <returns>The JTAC.</returns>
  public static JoltTwitchAccountClient Get(bool chatbot) => chatbot ? JoltTwitchAccountClient.ChatBot
    : JoltTwitchAccountClient.Streamer;

  /// <summary>
  ///   Get the active account name for this JTAC.
  /// </summary>
  /// <returns>The account name, null if not signed in.</returns>
  public string? GetActiveAccountName() => Data.Twitch.StreamerOrChatBot(IsChatBot).ActiveAccountName;

  /// <summary>
  ///   Change which account is signed in to this JTAC.
  /// </summary>
  /// <param name="value">The new account name. Case sensitive.</param>
  /// <returns>(Task, void.)</returns>
  public async Task SetActiveAccount(string? value)
  {
    Data.Twitch.StreamerOrChatBot(IsChatBot).ActiveAccountName = value;
    Data.Twitch.Save();
    APIClient.Settings.AccessToken = Data.Twitch.StreamerOrChatBot(IsChatBot).ActiveAccount?.Token;
  }

  /// <summary>
  ///   Remove an account from this JTAM, switching signed-in account if necessary.
  /// </summary>
  /// <param name="value">The account name to remove. Case sensitive.</param>
  /// <returns>(Task, void.)</returns>
  public async Task RemoveAccount(string? value)
  {
    var jtal = Data.Twitch.StreamerOrChatBot(IsChatBot);
    jtal.Accounts.RemoveAll(a => a.Name == value);
    if (jtal.ActiveAccountName == value) await SetActiveAccount(jtal.Accounts.FirstOrNull()?.Name);
  }

  /// <summary>
  ///   The auth scopes needed by a twitch account of this type.
  /// </summary>
  string[] AuthScopes => [..
    typeof(JoltTwitchAccountClient).Assembly
      .GetTypes()
      .SelectMany(t => t.GetMethods())
      .SelectMany(m => m.GetCustomAttributes<UsesAuthScopeAttribute>())
      .Where(a => a.IsChatBot == this.IsChatBot)
      .Select(a => a.Scope)
  ];

  /// <summary>
  ///   The auth scopes needed by a twitch account of this type,
  ///   space-separated.
  /// </summary>
  public string AuthScopeString => string.Join(" ", AuthScopes);

  /// <summary>
  ///   Determines whether a list of scopes includes all scopes needed.
  /// </summary>
  /// <param name="scopes">The list of scopes.</param>
  /// <returns>true if passes, false if fails.</returns>
  public bool HasAllAuthScopes(string[] scopes) => !AuthScopes.Except(scopes).Any();

  /// <summary>
  ///   The "state" parameter of a new-account request.
  /// </summary>
  private string ChangeState = "";

  /// <summary>
  ///   Creates a new change-state parameter and returns it. Invalidates
  ///   any previous change-state parameter.
  /// </summary>
  /// <returns>The new change-state parameter.</returns>
  public string GetChangeState() => ChangeState = Guid.NewGuid().ToString();

  /// <summary>
  ///   Validates a change-state parameter.
  /// </summary>
  /// <param name="state">The parameter to check.</param>
  /// <returns>true if matches, false if doesn't match.</returns>
  public bool IsCorrectChangeState(string state) => ChangeState == state;

  /// <summary>
  ///   Uses an auth code to get a token.
  /// </summary>
  /// <param name="code">The auth code.</param>
  /// <param name="uri">
  ///   The redirect uri (the page performing this request).
  /// </param>
  /// <returns>(Task, void.)</returns>
  public async Task UseAuthCode(string code, string uri)
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
      Scopes: authResponse.Scopes ?? [],
      AvatarURL: infoResponse.ProfileImageUrl
    );

    Data.Twitch.StreamerOrChatBot(IsChatBot).Accounts.RemoveAll(a => a.Name == acct.Name);
    Data.Twitch.StreamerOrChatBot(IsChatBot).Accounts.Add(acct);

    await SetActiveAccount(acct.Name);
  }
}

/// <summary>
///   Marks a method as using a given scope in the Twitch API.
///   Automatically collected at runtime to denote which scopes are needed
///   for each token.
/// </summary>
/// <param name="scope">The scope used.</param>
/// <param name="chatBot">
///   Whether this scope is used on the streamer account (false) or the
///   chat bot account (true).
/// </param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class UsesAuthScopeAttribute(string scope, bool chatBot = false) : Attribute
{
  /// <summary>
  ///   The scope used.
  /// </summary>
  public readonly string Scope = scope;

  /// <summary>
  ///   Whether this scope is used on the streamer account (false) or the
  ///   chat bot account (true).
  /// </summary>
  public readonly bool IsChatBot = chatBot;
}
