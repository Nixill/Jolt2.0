using Nixill.Streaming.JoltBot.BotData;
using NodaTime;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Exceptions;

namespace Nixill.Streaming.JoltBot.Twitch;

public partial class JoltTwitchClient
{
  /// <summary>
  ///   The actual Twitch API Client used by this JTC.
  /// </summary>
  internal TwitchAPI APIClient;

  /// <summary>
  ///   Makes an API call with this client's access token.
  /// </summary>
  /// <remarks>
  ///   This method tries up to five times in case of server errors. It
  ///   will also automatically attempt to refresh the access token and
  ///   try again in case of expired token.
  /// </remarks>
  /// <param name="call">The call to make.</param>
  /// <returns>(Task, void.)</returns>
  internal Task Call(TwitchAPICallAsync call) => Call((api, _) => call(api));

  /// <summary>
  ///   Makes an API call with this client's access token and broadcaster ID.
  /// </summary>
  /// <remarks>
  ///   This method tries up to five times in case of server errors. It
  ///   will also automatically attempt to refresh the access token and
  ///   try again in case of expired token.
  /// </remarks>
  /// <param name="call">The call to make.</param>
  /// <returns>(Task, void.)</returns>
  internal Task Call(TwitchAPICallWithBroadcasterIDAsync call) => Call(async (api, id) =>
  {
    await call(api, id);
    return true;
  });

  /// <summary>
  ///   Makes an API call with this client's access token.
  /// </summary>
  /// <remarks>
  ///   This method tries up to five times in case of server errors. It
  ///   will also automatically attempt to refresh the access token and
  ///   try again in case of expired token.
  /// </remarks>
  /// <typeparam name="T">The type of the result of the call.</typeparam>
  /// <param name="call">The call to make.</param>
  /// <returns>(Task) The result of the call.</returns>
  internal Task<T> Call<T>(TwitchAPICallWithReturnValueAsync<T> call) => Call((api, _) => call(api));

  /// <summary>
  ///   Makes an API call with this client's access token and broadcaster ID.
  /// </summary>
  /// <remarks>
  ///   This method tries up to five times in case of server errors. It
  ///   will also automatically attempt to refresh the access token and
  ///   try again in case of expired token.
  /// </remarks>
  /// <typeparam name="T">The type of the result of the call.</typeparam>
  /// <param name="call">The call to make.</param>
  /// <param name="isRetried">
  ///   Whether or not this call is retried after a bad-token exception.
  ///   Only used internally, and should always be set to <see langword="false"/>
  ///   for an external call.
  /// </param>
  /// <returns>(Task) The result of the call.</returns>
  internal async Task<T> Call<T>(TwitchAPICallWithBroadcasterIDAndReturnValueAsync<T> call, bool isRetried = false)
  {
    if (!HasActiveAccount()) throw new NoActiveAccountException();

    try
    {
      int i = 0;
      while (true)
        try
        {
          return await call(APIClient, GetActiveAccountUID()!);
        }
        catch (InternalServerErrorException) when (i < 5)
        {
          await Task.Delay(5);
          i++;
        }
    }
    catch (TokenExpiredException) when (!isRetried)
    {
      await RefreshCurrentToken();
      return await Call(call, true);
    }
    catch (BadScopeException) when (!isRetried)
    {
      await RefreshCurrentToken();
      return await Call(call, true);
    }
  }

  /// <summary>
  ///   Refresh the current account's access token.
  /// </summary>
  /// <returns>(Task, void.)</returns>
  internal async Task RefreshCurrentToken()
  {
    JoltTwitchAccount oldAcct = GetActiveAccount()!.Value;

    var refreshResponse = await APIClient.Auth.RefreshAuthTokenAsync(oldAcct.Refresh, Data.Twitch.Secret);

    APIClient.Settings.AccessToken = refreshResponse.AccessToken;

    JoltTwitchAccount acct = oldAcct with
    {
      Token = refreshResponse.AccessToken,
      Refresh = refreshResponse.RefreshToken,
      // not sure this can change? better safe tho
      Scopes = refreshResponse.Scopes ?? [],
      LastRefresh = SystemClock.Instance.GetCurrentInstant()
    };

    Data.Twitch.StreamerOrChatBot(IsChatBot).Accounts.RemoveAll(a => a.UID == acct.UID);
    Data.Twitch.StreamerOrChatBot(IsChatBot).Accounts.Add(acct);

    await SetActiveAccountByUID(acct.UID);
  }
}

/// <summary>
///   A method that receives a Twitch API object and uses it to make a
///   call that does not return anything (besides its task).
/// </summary>
/// <param name="api">The Twitch API object to use for the call.</param>
/// <returns>(Task, void.)</returns>
/// <seealso cref="JoltTwitchClient.Call(TwitchAPICallAsync)"/>
internal delegate Task TwitchAPICallAsync(TwitchAPI api);

/// <summary>
///   A method that receives a Twitch API object and a broadcaster ID
///   string and uses them to make a call that does not return anything
///   (besides its task).
/// </summary>
/// <param name="api">The Twitch API object to use for the call.</param>
/// <param name="broadcasterID">
///   The broadcaster ID to use for the call.
/// </param>
/// <returns>(Task, void.)</returns>
/// <seealso cref="JoltTwitchClient.Call(TwitchAPICallWithBroadcasterIDAsync)"/>
internal delegate Task TwitchAPICallWithBroadcasterIDAsync(TwitchAPI api, string broadcasterID);

/// <summary>
///   A method that receives a Twitch API object and uses it to make a
///   call that returns a (task which returns a) value.
/// </summary>
/// <typeparam name="T">
///   The type of response returned by the call('s task).
/// </typeparam>
/// <param name="api">The Twitch API object to use for the call.</param>
/// <returns>(Task) The result of the API call.</returns>
internal delegate Task<T> TwitchAPICallWithReturnValueAsync<T>(TwitchAPI api);

/// <summary>
///   A method that receives a Twitch API object and uses it to make a
///   call that returns a (task which returns a) value.
/// </summary>
/// <typeparam name="T">
///   The type of response returned by the call('s task).
/// </typeparam>
/// <param name="api">The Twitch API object to use for the call.</param>
/// <returns>(Task) The result of the API call.</returns>
internal delegate Task<T> TwitchAPICallWithBroadcasterIDAndReturnValueAsync<T>(TwitchAPI api, string broadcasterID);