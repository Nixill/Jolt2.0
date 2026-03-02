using Nixill.Streaming.JoltBot.BotData;

namespace Nixill.Streaming.JoltBot.Twitch;

public static class TwitchSetup
{
  public static void Run()
  {
    Task.Run(() => JoltTwitchAccountManager.SetActiveAccounts(Data.Twitch.ActivePairUID));
  }
}