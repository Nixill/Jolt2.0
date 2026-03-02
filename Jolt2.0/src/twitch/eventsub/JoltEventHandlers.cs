using Nixill.Streaming.JoltBot.Twitch.API;
using TwitchLib.Api.Helix.Models.Channels.SendChatMessage;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace Nixill.Streaming.JoltBot.Twitch.EventSub;

public partial class JoltEventService : IHostedService
{
  private void RegisterEventHandlers(EventSubWebsocketClient client)
  {
    Client.ChannelChatMessage += OnChatMessageReceived;
  }

  [TwitchEvent("channel.chat.message", "1",
    TwitchEventCondition.Broadcaster | TwitchEventCondition.UserChatBot)]
  [UsesAuthScope("user:read:chat", JoltTwitchTokenType.ChatBot)]
  [UsesAuthScope("user:bot", JoltTwitchTokenType.ChatBot)]
  // unsure which of these to use
  [UsesAuthScope("channel:bot", JoltTwitchTokenType.ChatBot)]
  [UsesAuthScope("channel:bot", JoltTwitchTokenType.Streamer)]
  private async Task OnChatMessageReceived(object sender, ChannelChatMessageArgs args)
  {
    var evt = args.Notification.Payload.Event;
    if (evt.Message.Text == "!ping")
    {
      await JoltTwitchAPI.Call(
        "Reply to ping (test)",
        (api, sid, cbid) => api.Helix.Chat.SendChatMessage(request: new SendChatMessageRequest
        {
          BroadcasterId = sid,
          SenderId = cbid,
          Message = "Pong!",
          ForSourceOnly = true,
          ReplyParentMessageId = evt.MessageId
        }), JoltTwitchTokenType.AppToken
      );
    }
  }
}