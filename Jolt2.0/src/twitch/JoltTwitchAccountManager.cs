using System.Reflection;
using Nixill.Streaming.JoltBot.BotData;
using Nixill.Streaming.JoltBot.Twitch.API;
using Nixill.Streaming.JoltBot.Twitch.EventSub;
using Nixill.Utils.Extensions;
using NodaTime;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;

namespace Nixill.Streaming.JoltBot.Twitch;

/// <summary>
///   Static class manipulating Twitch auth information.
/// </summary>
public static class JoltTwitchAccountManager
{
  /// <summary>
  ///   The user ID of the active streamer account, if any.
  /// </summary>
  public static string? ActiveStreamerUID => Data.Twitch.ActivePairUID;

  /// <summary>
  ///   The user ID of the active chat bot account, if any.
  /// </summary>
  public static string? ActiveChatBotUID => ActiveAccounts?.ChatBot.UID;

  /// <summary>
  ///   The account information for the active pair of accounts, if any.
  /// </summary>
  public static JoltTwitchAccountPair? ActiveAccounts => Data.Twitch.Accounts.CoalescingGetStruct(Data.Twitch.ActivePairUID);

  /// <summary>
  ///   Change which account is signed in.
  /// </summary>
  /// <param name="uid">The new account UID.</param>
  /// <returns>(Task, void.)</returns>
  public static async Task SetActiveAccounts(string? uid)
  {
    if (Data.Twitch.ActivePairUID != null) await CloseOpenAccounts();

    Data.Twitch.ActivePairUID = uid;
    Data.Twitch.Save();

    JoltTwitchAPI.ChatBotTokenClient.Settings.AccessToken = ActiveChatBotUID;
    JoltTwitchAPI.StreamerTokenClient.Settings.AccessToken = ActiveStreamerUID;

    if (uid != null) await OpenNewAccounts();
  }

  /// <summary>
  ///   Closes existing twitch API and EventSub connections.
  /// </summary>
  /// <returns>(Task, void.)</returns>
  private static async Task CloseOpenAccounts()
  {
    await JoltEventService.Instance.DisconnectAsync();
  }

  /// <summary>
  ///   Opens new twitch API and EventSub connections.
  /// </summary>
  /// <returns>(Task, void.)</returns>
  private static async Task OpenNewAccounts()
  {
    await JoltEventService.Instance.ConnectAsync();
  }

  /// <summary>
  ///   Whether or not an account is active according to the data.
  /// </summary>
  /// <returns>See above.</returns>
  public static bool HasActiveAccount => Data.Twitch.ActivePairUID is not null;

  /// <summary>
  ///   Removes an account from the data, switching out of it if necessary.
  /// </summary>
  /// <remarks>
  ///   If that is the active account, signs out without signing in.
  /// </remarks>
  /// <param name="uid">
  ///   The user ID of the streamer whose account should be removed.
  /// </param>
  /// <returns>(Task, void.)</returns>
  public static async Task RemoveAccount(string uid)
  {
    Data.Twitch.Accounts.Remove(uid);
    if (Data.Twitch.ActivePairUID == uid) await SetActiveAccounts(null);
  }

  /// <summary>
  ///   The auth scopes needed by a streamer twitch account.
  /// </summary>
  static string[] StreamerAuthScopes => [..
    typeof(JoltTwitchAccountManager).Assembly
      .GetTypes()
      .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
      .SelectMany(m => m.GetCustomAttributes<UsesAuthScopeAttribute>())
      .Where(a => a.TokenType == JoltTwitchTokenType.Streamer)
      .Select(a => a.Scope)
  ];

  /// <summary>
  ///   The auth scopes needed by a streamer twitch account,
  ///   space-separated.
  /// </summary>
  public static string StreamerAuthScopeString => string.Join(" ", StreamerAuthScopes);

  /// <summary>
  ///   Determines whether a list of scopes includes all scopes needed for
  ///   a streamer account.
  /// </summary>
  /// <param name="scopes">The list of scopes.</param>
  /// <returns>
  ///   <see langword="true"/> if pass, <see langword="false"/> if fail.
  /// </returns>
  public static bool HasAllStreamerAuthScopes(string[]? scopes) => !StreamerAuthScopes.Except(scopes ?? []).Any();

  /// <summary>
  ///   The auth scopes needed by a streamer twitch account.
  /// </summary>
  static string[] ChatBotAuthScopes => [..
    typeof(JoltTwitchAccountManager).Assembly
      .GetTypes()
      .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
      .SelectMany(m => m.GetCustomAttributes<UsesAuthScopeAttribute>())
      .Where(a => a.TokenType == JoltTwitchTokenType.ChatBot)
      .Select(a => a.Scope)
  ];

  /// <summary>
  ///   The auth scopes needed by a streamer twitch account,
  ///   space-separated.
  /// </summary>
  public static string ChatBotAuthScopeString => string.Join(" ", ChatBotAuthScopes);

  /// <summary>
  ///   Determines whether a list of scopes includes all scopes needed for
  ///   a chat bot account.
  /// </summary>
  /// <param name="scopes">The list of scopes.</param>
  /// <returns>
  ///   <see langword="true"/> if pass, <see langword="false"/> if fail.
  /// </returns>
  public static bool HasAllChatBotAuthScopes(string[]? scopes) => !ChatBotAuthScopes.Except(scopes ?? []).Any();

  /// <summary>
  ///   Determines whether both accounts in a pair have all the necessary
  ///   auth scopes.
  /// </summary>
  /// <param name="pair">The pair of accounts.</param>
  /// <returns>
  ///   <see langword="true"/> if pass, <see langword="false"/> if fail.
  /// </returns>
  public static bool HasAllAuthScopes(JoltTwitchAccountPair pair) =>
    HasAllStreamerAuthScopes(pair.Streamer.Scopes) && HasAllChatBotAuthScopes(pair.ChatBot.Scopes);

  /// <summary>
  ///   The "state" parameter of a new-account request.
  /// </summary>
  private static string ChangeState = "";

  /// <summary>
  ///   Creates a new change-state parameter and returns it. Invalidates
  ///   any previous change-state parameter and cancels a pending streamer
  ///   account.
  /// </summary>
  /// <returns>The new change-state parameter.</returns>
  public static string GetChangeState()
  {
    PendingStreamerAccount = null;
    return ChangeState = Guid.NewGuid().ToString();
  }

  /// <summary>
  ///   Validates a change-state parameter.
  /// </summary>
  /// <param name="state">The parameter to check.</param>
  /// <returns>true if matches, false if doesn't match.</returns>
  public static bool IsCorrectChangeState(string state) => ChangeState == state;

  /// <summary>
  ///   A pending streamer account, waiting to be paired with a chat bot
  ///   account.
  /// </summary>
  private static JoltTwitchAccountInfo? PendingStreamerAccount = null;

  /// <summary>
  ///   Uses an auth code to add an account.
  /// </summary>
  /// <param name="token">The account's authentication code.</param>
  /// <param name="redirect">The redirect uri.</param>
  /// <param name="tokenType">Which token type is being added.</param>
  /// <returns>(Task, void.)</returns>
  public static async Task AddAccount(string code, string redirect, JoltTwitchTokenType tokenType)
  {
    var authResponse = await JoltTwitchAPI.AppTokenClient.Auth.GetAccessTokenFromCodeAsync(code, Data.Twitch.Secret, redirect);

    var token = authResponse.AccessToken;

    var tokenResponse = await JoltTwitchAPI.AppTokenClient.Auth.ValidateAccessTokenAsync(token);

    var infoResponse = (await JoltTwitchAPI.AppTokenClient.Helix.Users.GetUsersAsync(accessToken: token))
      .Users.First();

    JoltTwitchAccountInfo acct = new(
      UID: tokenResponse.UserId,
      DisplayName: infoResponse.DisplayName,
      LoginName: infoResponse.Login,
      UserToken: token,
      RefreshToken: authResponse.RefreshToken,
      Scopes: tokenResponse.Scopes?.ToArray(),
      AvatarURL: infoResponse.ProfileImageUrl
    );

    if (tokenType == JoltTwitchTokenType.ChatBot)
    {
      var newPair = new JoltTwitchAccountPair(
        Streamer: PendingStreamerAccount!.Value,
        ChatBot: acct,
        LastRefresh: SystemClock.Instance.GetCurrentInstant()
      );

      Data.Twitch.Accounts[PendingStreamerAccount!.Value.UID] = newPair;
      Data.Twitch.Save();

      PendingStreamerAccount = null;
    }
    else
    {
      PendingStreamerAccount = acct;
    }
  }
}