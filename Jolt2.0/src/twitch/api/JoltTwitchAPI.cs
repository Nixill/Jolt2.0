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
  ///   The actual Twitch API Client used by this JTC for app-token calls.
  /// </summary>
  internal static readonly TwitchAPI AppTokenClient = new()
  {
    Settings =
    {
      ClientId = Data.Twitch.ClientID,
      AccessToken = Data.Twitch.AppToken
    }
  };

  /// <summary>
  ///   The actual Twitch API Client used by this JTC for user-token calls
  ///   using the streamer's user token.
  /// </summary>
  internal static readonly TwitchAPI StreamerTokenClient = new()
  {
    Settings =
    {
      ClientId = Data.Twitch.ClientID
    }
  };

  /// <summary>
  ///   The actual Twitch API Client used by this JTC for user-token calls
  ///   using the chat bot's user token.
  /// </summary>
  internal static readonly TwitchAPI ChatBotTokenClient = new()
  {
    Settings =
    {
      ClientId = Data.Twitch.ClientID
    }
  };

  /// <summary>
  ///   Update access tokens on clients.
  /// </summary>
  internal static void UpdateTokens()
  {
    AppTokenClient.Settings.AccessToken = Data.Twitch.AppToken;
    StreamerTokenClient.Settings.AccessToken = JoltTwitchAccountManager.ActiveAccounts?.Streamer.UserToken;
    ChatBotTokenClient.Settings.AccessToken = JoltTwitchAccountManager.ActiveAccounts?.ChatBot.UserToken;
  }

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
  internal static Task Call(TwitchAPICallAsync call, JoltTwitchTokenType callType = JoltTwitchTokenType.Streamer)
    => Call((api, _, _) => call(api), callType);

  /// <summary>
  ///   Makes an API call with this client's access token.
  /// </summary>
  /// <remarks>
  ///   This method tries up to five times in case of server errors. It
  ///   will also automatically attempt to refresh the access token and
  ///   try again in case of expired token.
  ///   <para/>
  ///   This overload treats the ID parameter as a broadcaster ID. To use
  ///   a chat bot ID, use the three-arg delegate overload and use the
  ///   third parameter.
  /// </remarks>
  /// <param name="call">The call to make.</param>
  /// <param name="callType">
  ///   Whether to make the call using the app access token, the
  ///   streamer's user access token (default), or the chat bot's user
  ///   access token.
  /// </param>
  /// <returns>(Task, void.)</returns>
  internal static Task Call(TwitchAPICallWithUserIDAsync call, JoltTwitchTokenType callType = JoltTwitchTokenType.Streamer)
    => Call((api, id, _) => call(api, id), callType);

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
  internal static Task Call(TwitchAPICallWithTwoUserIDsAsync call,
    JoltTwitchTokenType callType = JoltTwitchTokenType.Streamer)
  => Call(async (api, sid, cbid) =>
    {
      await call(api, sid, cbid);
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
    JoltTwitchTokenType callType = JoltTwitchTokenType.Streamer)
    => Call((api, _, _) => call(api), callType);

  /// <summary>
  ///   Makes an API call with this client's access token.
  /// </summary>
  /// <remarks>
  ///   This method tries up to five times in case of server errors. It
  ///   will also automatically attempt to refresh the access token and
  ///   try again in case of expired token.
  ///   <para/>
  ///   This overload treats the ID parameter as a broadcaster ID. To use
  ///   a chat bot ID, use the three-arg delegate overload and use the
  ///   third parameter.
  /// </remarks>
  /// <typeparam name="T">The type of the result of the call.</typeparam>
  /// <param name="call">The call to make.</param>
  /// <param name="callType">
  ///   Whether to make the call using the app access token, the
  ///   streamer's user access token (default), or the chat bot's user
  ///   access token.
  /// </param>
  /// <returns>(Task) The result of the call.</returns>
  internal static Task<T> Call<T>(TwitchAPICallWithUserIDAndReturnValueAsync<T> call,
    JoltTwitchTokenType callType = JoltTwitchTokenType.Streamer)
    => Call((api, id, _) => call(api, id), callType);

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
  /// <returns>(Task) The result of the call.</returns>
  internal static async Task<T> Call<T>(TwitchAPICallWithTwoUserIDsAndReturnValueAsync<T> call,
    JoltTwitchTokenType callType = JoltTwitchTokenType.Streamer)
  {
    if (!JoltTwitchAccountManager.HasActiveAccount) throw new NoActiveAccountException();

    TwitchAPI client = callType switch
    {
      JoltTwitchTokenType.AppToken => AppTokenClient,
      JoltTwitchTokenType.ChatBot => ChatBotTokenClient,
      _ => StreamerTokenClient
    };

    int i = 0;
    while (true)
      try
      {
        return await call(client, JoltTwitchAccountManager.ActiveStreamerUID!,
          JoltTwitchAccountManager.ActiveChatBotUID!);
      }
      catch (InternalServerErrorException) when (i < 5)
      {
        await Task.Delay(5);
        i++;
      }
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
///   A method that receives a Twitch API object and a broadcaster ID and
///   uses them to make a call that does not return anything (besides its
///   task).
/// </summary>
/// <param name="api">The Twitch API object to use for the call.</param>
/// <param name="broadcasterID">
///   The broadcaster ID to use for the call.
/// </param>
/// <returns>(Task, void.)</returns>
/// <seealso cref="JoltTwitchClient.Call(TwitchAPICallWithUserIDAsync)"/>
internal delegate Task TwitchAPICallWithUserIDAsync(TwitchAPI api, string broadcasterID);

/// <summary>
///   A method that receives a Twitch API object, a broadcaster ID, and a
///   chat bot ID, and uses them to make a call that does not return
///   anything (besides its task).
/// </summary>
/// <param name="api">The Twitch API object to use for the call.</param>
/// <param name="broadcasterID">
///   The broadcaster ID to use for the call.
/// </param>
/// <param name="chatBotID">
///   The chat bot ID to use for the call.
/// </param>
/// <returns>(Task, void.)</returns>
/// <seealso cref="JoltTwitchClient.Call(TwitchAPICallWithUserIDAsync)"/>
internal delegate Task TwitchAPICallWithTwoUserIDsAsync(TwitchAPI api, string broadcasterID, string chatBotID);

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
///   A method that receives a Twitch API object and a broadcaster ID and
///   uses them to make a call that returns a (task which returns a) value.
/// </summary>
/// <typeparam name="T">
///   The type of response returned by the call('s task).
/// </typeparam>
/// <param name="api">The Twitch API object to use for the call.</param>
/// <param name="broadcasterID">The user ID to use for the call.</param>
/// <returns>(Task) The result of the API call.</returns>
internal delegate Task<T> TwitchAPICallWithUserIDAndReturnValueAsync<T>(TwitchAPI api, string broadcasterID);

/// <summary>
///   A method that receives a Twitch API object, a broadcaster ID, and a
///   chat bot ID, and uses them to make a call that returns a (task which
///   returns a) value.
/// </summary>
/// <typeparam name="T">
///   The type of response returned by the call('s task).
/// </typeparam>
/// <param name="api">The Twitch API object to use for the call.</param>
/// <param name="broadcasterID">
///   The broadcaster ID to use for the call.
/// </param>
/// <param name="chatBotID">
///   The chat bot ID to use for the call.
/// </param>
/// <returns>(Task) The result of the API call.</returns>
internal delegate Task<T> TwitchAPICallWithTwoUserIDsAndReturnValueAsync<T>(TwitchAPI api,
  string broadcasterID, string chatBotID);