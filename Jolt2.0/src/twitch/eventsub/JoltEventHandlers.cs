using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace Nixill.Streaming.JoltBot.Twitch.EventSub;

public partial class JoltEventService : IHostedService
{
  private void RegisterEventHandlers(EventSubWebsocketClient client)
  {
    Client.ChannelChatMessage += OnChatMessageReceived;
  }

  [TwitchEvent(true, "channel.chat.message", "1",
    TwitchEventCondition.Broadcaster | TwitchEventCondition.UserChatBot)]
  [UsesAuthScope("user:read:chat", true)]
  [UsesAuthScope("user:bot", true)]
  private async Task OnChatMessageReceived(object sender, ChannelChatMessageArgs args)
  {
    var evt = args.Notification.Payload.Event;

  }
}