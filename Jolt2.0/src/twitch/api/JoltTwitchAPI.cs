using Nixill.Streaming.JoltBot.BotData;
using NodaTime;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;

namespace Nixill.Streaming.JoltBot.Twitch.API;

/// <summary>
///   Class for calling Twitch API methods.
/// </summary>
public static class JoltTwitchAPI
{
  /// <summary>
  ///   The actual Twitch API Client used by this JTC for calls (both
  ///   streamer and chat bot).
  /// </summary>
  internal static readonly TwitchAPI Client = new()
  {
    Settings =
    {
      ClientId = Data.Twitch.ClientID
    }
  };

  /// <summary>
  ///   The streamer access token.
  /// </summary>
  private static string? StreamerToken
    => JoltTwitchAccountManager.ActiveAccounts?.Streamer.UserToken;

  /// <summary>
  ///   The chat bot access token.
  /// </summary>
  private static string? ChatBotToken
    => JoltTwitchAccountManager.ActiveAccounts?.ChatBot.UserToken;

  /// <summary>
  ///   Makes an API call with this client's access token.
  /// </summary>
  /// <remarks>
  ///   This method tries up to five times in case of server errors. It
  ///   will also automatically attempt to refresh the access token and
  ///   try again in case of expired token.
  /// </remarks>
  /// <param name="call">The call to make.</param>
  /// <param name="callType">
  ///   Whether to make the call using the app access token, the
  ///   streamer's user access token (default), or the chat bot's user
  ///   access token.
  /// </param>
  /// <returns>(Task, void.)</returns>
  internal static Task Call(TwitchAPICallAsync call, JoltTwitchAPICallType callType = JoltTwitchAPICallType.Streamer)
    => Call((api, token, _) => call(api, token), callType);

  /// <summary>
  ///   Makes an API call with this client's access token and broadcaster ID.
  /// </summary>
  /// <remarks>
  ///   This method tries up to five times in case of server errors. It
  ///   will also automatically attempt to refresh the access token and
  ///   try again in case of expired token.
  /// </remarks>
  /// <param name="call">The call to make.</param>
  /// <param name="callType">
  ///   Whether to make the call using the app access token, the
  ///   streamer's user access token (default), or the chat bot's user
  ///   access token.
  /// </param>
  /// <returns>(Task, void.)</returns>
  internal static Task Call(TwitchAPICallWithBroadcasterIDAsync call,
    JoltTwitchAPICallType callType = JoltTwitchAPICallType.Streamer)
  => Call(async (api, token, id) =>
    {
      await call(api, token, id);
      return true;
    },
  callType);

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
  /// <param name="callType">
  ///   Whether to make the call using the app access token, the
  ///   streamer's user access token (default), or the chat bot's user
  ///   access token.
  /// </param>
  /// <returns>(Task) The result of the call.</returns>
  internal static Task<T> Call<T>(TwitchAPICallWithReturnValueAsync<T> call,
    JoltTwitchAPICallType callType = JoltTwitchAPICallType.Streamer)
    => Call((api, token, _) => call(api, token), callType);

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
  /// <param name="callType">
  ///   Whether to make the call using the app access token, the
  ///   streamer's user access token (default), or the chat bot's user
  ///   access token.
  /// </param>
  /// <param name="isRetried">
  ///   Whether or not this call is retried after a bad-token exception.
  ///   Only used internally, and should always be set to <see langword="false"/>
  ///   for an external call.
  /// </param>
  /// <returns>(Task) The result of the call.</returns>
  internal static async Task<T> Call<T>(TwitchAPICallWithBroadcasterIDAndReturnValueAsync<T> call,
    JoltTwitchAPICallType callType = JoltTwitchAPICallType.Streamer, bool isRetried = false)
  {
    if (!JoltTwitchAccountManager.HasActiveAccount) throw new NoActiveAccountException();

    string? token = callType switch
    {
      JoltTwitchAPICallType.Streamer => StreamerToken ?? throw new NoActiveAccountException(),
      JoltTwitchAPICallType.ChatBot => ChatBotToken ?? throw new NoActiveAccountException(),
      _ => null
    };

    int i = 0;
    while (true)
      try
      {
        return await call(Client, token, JoltTwitchAccountManager.ActiveStreamerUID!);
      }
      catch (InternalServerErrorException) when (i < 5)
      {
        await Task.Delay(5);
        i++;
      }
  }
}

internal enum JoltTwitchAPICallType
{
  AppToken = 0,
  Streamer = 1,
  ChatBot = 2
}

/// <summary>
///   A method that receives a Twitch API object and uses it to make a
///   call that does not return anything (besides its task).
/// </summary>
/// <param name="api">The Twitch API object to use for the call.</param>
/// <param name="token">
///   The access token in use for the call.
/// </param>
/// <returns>(Task, void.)</returns>
/// <seealso cref="JoltTwitchClient.Call(TwitchAPICallAsync)"/>
internal delegate Task TwitchAPICallAsync(TwitchAPI api, string? token);

/// <summary>
///   A method that receives a Twitch API object and a broadcaster ID
///   string and uses them to make a call that does not return anything
///   (besides its task).
/// </summary>
/// <param name="api">The Twitch API object to use for the call.</param>
/// <param name="token">
///   The access token in use for the call.
/// </param>
/// <param name="broadcasterID">
///   The broadcaster ID to use for the call.
/// </param>
/// <returns>(Task, void.)</returns>
/// <seealso cref="JoltTwitchClient.Call(TwitchAPICallWithBroadcasterIDAsync)"/>
internal delegate Task TwitchAPICallWithBroadcasterIDAsync(TwitchAPI api, string? token, string broadcasterID);

/// <summary>
///   A method that receives a Twitch API object and uses it to make a
///   call that returns a (task which returns a) value.
/// </summary>
/// <typeparam name="T">
///   The type of response returned by the call('s task).
/// </typeparam>
/// <param name="api">The Twitch API object to use for the call.</param>
/// <param name="token">
///   The access token in use for the call.
/// </param>
/// <returns>(Task) The result of the API call.</returns>
internal delegate Task<T> TwitchAPICallWithReturnValueAsync<T>(TwitchAPI api, string? token);

/// <summary>
///   A method that receives a Twitch API object and uses it to make a
///   call that returns a (task which returns a) value.
/// </summary>
/// <typeparam name="T">
///   The type of response returned by the call('s task).
/// </typeparam>
/// <param name="api">The Twitch API object to use for the call.</param>
/// <param name="token">
///   The access token in use for the call.
/// </param>
/// <returns>(Task) The result of the API call.</returns>
internal delegate Task<T> TwitchAPICallWithBroadcasterIDAndReturnValueAsync<T>(TwitchAPI api, string? token, string broadcasterID);