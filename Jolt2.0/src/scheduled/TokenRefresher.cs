using Nixill.Streaming.JoltBot.BotData;
using Nixill.Streaming.JoltBot.Twitch;
using NodaTime;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;

namespace Nixill.Streaming.JoltBot.Scheduled;

public static class TokenRefresher
{
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
      foreach (bool isChatBot in (bool[])[false, true])
      {
        var jtal = Data.Twitch.StreamerOrChatBot(isChatBot);

        List<JoltTwitchAccount> accounts = [.. jtal.Accounts];
        jtal.Accounts.Clear();

        foreach (var acct in accounts)
        {
          try
          {
            var result = await api.Auth.RefreshAuthTokenAsync(acct.Refresh, Data.Twitch.Secret);

            // make sure user info is up to date too
            var user = (await api.Helix.Users.GetUsersAsync(ids: [acct.UID], accessToken: result.AccessToken)).Users.First()!;

            var newAccount = new JoltTwitchAccount(
              Name: user.Login,
              Token: result.AccessToken,
              Refresh: result.RefreshToken,
              UID: acct.UID,
              Scopes: result.Scopes ?? [],
              AvatarURL: user.ProfileImageUrl,
              LastRefresh: SystemClock.Instance.GetCurrentInstant()
            );

            jtal.Accounts.Add(newAccount);
          }
          catch (BadTokenException)
          {
            await JoltTwitchClient.Get(isChatBot).RemoveAccountByUID(acct.UID);
          }
        }

        JoltTwitchClient.Get(isChatBot).RefreshCurrentAccount();
      }

      await Task.Delay(TimeSpan.FromHours(1));
    }
  }
}