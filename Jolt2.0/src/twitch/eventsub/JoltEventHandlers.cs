using Nixill.Streaming.JoltBot.BotData;
using Nixill.Streaming.JoltBot.Twitch.API;
using Nixill.Twitch.Interactions.Objects;
using Nixill.Twitch.Interactions.Objects.Commands;
using TwitchLib.Api.Helix.Models.Channels.SendChatMessage;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Websockets;

namespace Nixill.Streaming.JoltBot.Twitch.EventSub;

public partial class JoltEventService : IHostedService
{
  /// <summary>
  ///   Chat connector.
  /// </summary>
  internal ChannelConnector Connector = null!;

  private void RegisterEventHandlers(EventSubWebsocketClient client)
  {
    // Nothing here right now.
  }

  [TwitchEvent("channel.chat.message", "1",
    TwitchEventCondition.Broadcaster | TwitchEventCondition.UserChatBot)]
  [UsesAuthScope("user:read:chat", JoltTwitchTokenType.ChatBot)]
  [UsesAuthScope("user:write:chat", JoltTwitchTokenType.ChatBot)]
  [UsesAuthScope("user:bot", JoltTwitchTokenType.ChatBot)]
  [UsesAuthScope("channel:bot", JoltTwitchTokenType.Streamer)]
  private async Task RegisterInteractions()
  {
    var accts = JoltTwitchAccountManager.ActiveAccounts!.Value;

    // this'll set it multiple times but idc, that's better than no times
    ChannelConnector.SetClientID(Data.Twitch.ClientID);

    Connector = new(
      eventSub: Client,
      appToken: Data.Twitch.AppToken,
      streamerToken: accts.Streamer.UserToken,
      streamerUID: accts.Streamer.UID,
      chatBotUID: accts.ChatBot.UID
    );

    CommandDispatchModule commands = Connector.EnableCommands("!");

    commands.RegisterCodeCommands(typeof(JoltEventService).Assembly);
  }
}