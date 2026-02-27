namespace Nixill.Streaming.JoltBot.Scheduled;

public static class ScheduledActions
{
  public static void RunAll()
  {
    Task.Run(TokenRefresher.RefreshHourlyAsync);
  }
}