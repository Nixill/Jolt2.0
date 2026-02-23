using System.Reflection;
using Nixill.Streaming.JoltBot.BotData;
using Nixill.Utils.Extensions;
using NodaTime;
using TwitchLib.Api;
using TwitchLib.EventSub.Websockets;

namespace Nixill.Streaming.JoltBot.Twitch;

/// <summary>
///   Clients for Twitch's EventSub and API for this app.
/// </summary>
public partial class JoltTwitchClient
{
  /// <summary>
  ///   True iff chat bot, false if streamer scoped JTC.
  /// </summary>
  public readonly bool IsChatBot;

  /// <summary>
  ///   Create one of the JTCs.
  /// </summary>
  /// <param name="isChatBot">Chat bot or streamer?</param>
  private JoltTwitchClient(bool isChatBot)
  {
    IsChatBot = isChatBot;
    APIClient = new TwitchAPI(Log.Factory)
    {
      Settings =
      {
        ClientId = Data.Twitch.ClientID
      }
    };
    EventSubClient = new EventSubWebsocketClient(Log.Factory);
  }

  /// <summary>
  ///   The JTC for the streamer account.
  /// </summary>
  public static readonly JoltTwitchClient Streamer = new(false);

  /// <summary>
  ///   The JTC for the chat bot account.
  /// </summary>
  public static readonly JoltTwitchClient ChatBot = new(true);

  /// <summary>
  ///   Get the JTC with a boolean "is chat bot" value.
  /// </summary>
  /// <param name="chatbot">Chat bot true, streamer false</param>
  /// <returns>The JTC.</returns>
  public static JoltTwitchClient Get(bool chatbot) => chatbot ? ChatBot : Streamer;

  /// <summary>
  ///   Gets both of the JTCs.
  /// </summary>
  /// <returns>The streamer JTC, then the chat bot JTC.</returns>
  public static IEnumerable<JoltTwitchClient> Both
    => [Streamer, ChatBot];

  /// <summary>
  ///   Get the active account user ID for this JTC.
  /// </summary>
  /// <returns>The account user ID, null if not signed in.</returns>
  public string? GetActiveAccountUID() => Data.Twitch.StreamerOrChatBot(IsChatBot).ActiveAccountUID;

  /// <summary>
  ///   Get the active account for this JTC.
  /// </summary>
  /// <returns>The account, null if not signed in.</returns>
  public JoltTwitchAccount? GetActiveAccount() => Data.Twitch.StreamerOrChatBot(IsChatBot).ActiveAccount;

  /// <summary>
  ///   Change which account is signed in to this JTC.
  /// </summary>
  /// <param name="value">The new account name. Case sensitive.</param>
  /// <returns>(Task, void.)</returns>
  public async Task SetActiveAccountByUID(string? uid)
  {
    // Close existing websockets first
    Data.Twitch.StreamerOrChatBot(IsChatBot).ActiveAccountUID = uid;
    Data.Twitch.Save();
    APIClient.Settings.AccessToken = Data.Twitch.StreamerOrChatBot(IsChatBot).ActiveAccount?.Token;
  }

  /// <summary>
  ///   Gets whether or not this JTC has an active account.
  /// </summary>
  /// <returns></returns>
  public bool HasActiveAccount() => Data.Twitch.StreamerOrChatBot(IsChatBot).ActiveAccountUID != null;

  /// <summary>
  ///   Refreshes the current account, if any is active.
  /// </summary>
  public void RefreshCurrentAccount()
  {
    var accountNull = Data.Twitch.StreamerOrChatBot(IsChatBot).ActiveAccount;

    if (accountNull.HasValue)
    {
      APIClient.Settings.AccessToken = accountNull.Value.Token;
    }
  }

  /// <summary>
  ///   Remove an account from this JTAM, switching signed-in account if necessary.
  /// </summary>
  /// <param name="value">The account name to remove. Case sensitive.</param>
  /// <returns>(Task, void.)</returns>
  public async Task RemoveAccountByUID(string? value)
  {
    var jtal = Data.Twitch.StreamerOrChatBot(IsChatBot);
    jtal.Accounts.RemoveAll(a => a.Name == value);
    if (jtal.ActiveAccountUID == value) await SetActiveAccountByUID(jtal.Accounts.FirstOrNull()?.UID);
  }

  /// <summary>
  ///   The auth scopes needed by a twitch account of this type.
  /// </summary>
  string[] AuthScopes => [..
    typeof(JoltTwitchClient).Assembly
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
  public bool HasAllAuthScopes(string[] scopes) => !AuthScopes.Except(scopes ?? []).Any();

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
      AvatarURL: infoResponse.ProfileImageUrl,
      LastRefresh: SystemClock.Instance.GetCurrentInstant()
    );

    Data.Twitch.StreamerOrChatBot(IsChatBot).Accounts.RemoveAll(a => a.UID == acct.UID);
    Data.Twitch.StreamerOrChatBot(IsChatBot).Accounts.Add(acct);

    await SetActiveAccountByUID(acct.UID);
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
