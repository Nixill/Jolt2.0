using Nixill.Streaming.JoltBot.BotData;
using Nixill.Streaming.JoltBot.Twitch;
using NodaTime;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;

namespace Nixill.Streaming.JoltBot.Scheduled;

/// <summary>
///   A container for the task of validating tokens on an hourly basis.
/// </summary>
public static class TokenChecker
{
  /// <summary>
  ///   The task of validating tokens on an hourly basis.
  /// </summary>
  /// <returns>(Task, infinite loop.)</returns>
  public static async Task CheckHourlyAsync()
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
          // Validate streamer token
          var streamerRefresh = await api.Auth.ValidateAccessTokenAsync(pair.Streamer.UserToken)
            ?? throw new InvalidTokenException("streamer");

          // Also validate chat bot token
          var chatBotValidate = await api.Auth.ValidateAccessTokenAsync(pair.ChatBot.UserToken)
            ?? throw new InvalidTokenException("chat bot");

          // And make sure both user info are up to date
          var streamerInfo = (await api.Helix.Users
              .GetUsersAsync(ids: [pair.Streamer.UID], accessToken: pair.Streamer.UserToken))
            .Users.First()!;

          var chatBotInfo = (await api.Helix.Users
              .GetUsersAsync(ids: [pair.ChatBot.UID], accessToken: pair.ChatBot.UserToken))
            .Users.First()!;

          var newStreamerAccount = new JoltTwitchAccountInfo(
            UID: streamerInfo.Id,
            DisplayName: streamerInfo.DisplayName,
            LoginName: streamerInfo.Login,
            UserToken: pair.Streamer.UserToken,
            Scopes: streamerRefresh.Scopes?.ToArray(),
            AvatarURL: streamerInfo.ProfileImageUrl
          );

          var newChatBotAccount = new JoltTwitchAccountInfo(
            UID: chatBotInfo.Id,
            DisplayName: chatBotInfo.DisplayName,
            LoginName: chatBotInfo.Login,
            UserToken: pair.ChatBot.UserToken,
            Scopes: chatBotValidate.Scopes?.ToArray(),
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