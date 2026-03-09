using Nixill.Streaming.JoltBot.BotData;
using Nixill.Streaming.JoltBot.Twitch;
using Nixill.Streaming.JoltBot.Twitch.EventSub;
using NodaTime;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;

namespace Nixill.Streaming.JoltBot.Scheduled;

/// <summary>
///   A container for the task of validating and refreshing tokens on an
///   hourly basis.
/// </summary>
public static class TokenRefresher
{
  /// <summary>
  ///   The task of refreshing tokens on an hourly basis.
  /// </summary>
  /// <returns>(Task, infinite loop.)</returns>
  public static async Task RefreshHourlyAsync()
  {
    var api = new TwitchAPI()
    {
      Settings =
      {
        ClientId = Data.Twitch.ClientID
      }
    };

    while (true)
    {
      List<JoltTwitchAccountPair> pairs = [.. Data.Twitch.Accounts.Values];

      foreach (var pair in pairs)
      {
        try
        {
          // Refresh streamer token
          var streamerRefresh = await api.Auth.RefreshAuthTokenAsync(pair.Streamer.RefreshToken, Data.Twitch.Secret)
            ?? throw new InvalidTokenException("streamer");

          // Also refresh chat bot token
          var chatBotRefresh = await api.Auth.RefreshAuthTokenAsync(pair.ChatBot.RefreshToken, Data.Twitch.Secret)
            ?? throw new InvalidTokenException("chat bot");

          // And make sure both user infos are up to date
          var streamerInfo = (await api.Helix.Users.GetUsersAsync(accessToken: streamerRefresh.AccessToken))
            .Users.First()!;

          var chatBotInfo = (await api.Helix.Users.GetUsersAsync(accessToken: chatBotRefresh.AccessToken))
            .Users.First()!;

          var newStreamerAccount = new JoltTwitchAccountInfo(
            UID: streamerInfo.Id,
            DisplayName: streamerInfo.DisplayName,
            LoginName: streamerInfo.Login,
            UserToken: streamerRefresh.AccessToken,
            RefreshToken: streamerRefresh.RefreshToken,
            Scopes: streamerRefresh.Scopes?.ToArray(),
            AvatarURL: streamerInfo.ProfileImageUrl
          );

          var newChatBotAccount = new JoltTwitchAccountInfo(
            UID: chatBotInfo.Id,
            DisplayName: chatBotInfo.DisplayName,
            LoginName: chatBotInfo.Login,
            UserToken: chatBotRefresh.AccessToken,
            RefreshToken: chatBotRefresh.RefreshToken,
            Scopes: chatBotRefresh.Scopes?.ToArray(),
            AvatarURL: chatBotInfo.ProfileImageUrl
          );

          var newAccountPair = new JoltTwitchAccountPair(
            Streamer: newStreamerAccount,
            ChatBot: newChatBotAccount,
            LastRefresh: SystemClock.Instance.GetCurrentInstant()
          );

          Data.Twitch.Accounts[newStreamerAccount.UID] = newAccountPair;
        }
        catch (Exception e) when (e is InvalidTokenException)
        {
          await JoltTwitchAccountManager.RemoveAccount(pair.Streamer.UID);
        }
      }

      // put the new token in places it's expected to be
      if (JoltTwitchAccountManager.ActiveAccounts is JoltTwitchAccountPair activePair)
      {
        JoltEventService.Instance.Connector.UpdateStreamerToken(activePair.Streamer.UserToken);
      }

      await Task.Delay(TimeSpan.FromHours(1));
    }
  }
}

[Serializable]
internal class InvalidTokenException : Exception
{
  public InvalidTokenException()
  {
  }

  public InvalidTokenException(string? message) : base(message)
  {
  }

  public InvalidTokenException(string? message, Exception? innerException) : base(message, innerException)
  {
  }
}