using Nixill.Streaming.JoltBot.BotData;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;

namespace Nixill.Streaming.JoltBot.Twitch.EventSub;

/// <summary>
///   The hosted service that controls the Twitch EventSub websocket client.
/// </summary>
public partial class JoltEventService : IHostedService
{
  /// <summary>
  ///   Logger.
  /// </summary>
  readonly ILogger<JoltEventService> Logger = Log.Factory.CreateLogger<JoltEventService>();

  /// <summary>
  ///   Actual client.
  /// </summary>
  readonly EventSubWebsocketClient Client;

  /// <summary>
  ///   The events which should be subscribed when the client connects.
  /// </summary>
  readonly List<JoltEventSubArgs> EventsToSubscribe = [];

  /// <summary>
  ///   Whether or not the websocket is currently connected.
  /// </summary>
  public bool Connected { get; private set; }

  public JoltEventService(ILogger<JoltEventService> _, EventSubWebsocketClient client)
  {
    Client = client ?? throw new ArgumentNullException(nameof(client));

    Client.WebsocketConnected += WebsocketConnected;
    Client.WebsocketDisconnected += WebsocketDisconnected;
    Client.WebsocketReconnected += WebsocketReconnected;

    RegisterEventHandlers(Client);
  }

  private async Task WebsocketConnected(object sender, WebsocketConnectedArgs args)
  {
    Connected = true;

    var nActiveStreamerAccount = Data.Twitch.Streamer.ActiveAccount;
    var nActiveChatBotAccount = Data.Twitch.ChatBot.ActiveAccount;

    // If neither account is connected, disconnect the websocket so that
    // twitch doesn't have to make us.
    if (nActiveChatBotAccount is null && nActiveStreamerAccount is null)
    {
      await Client.DisconnectAsync();
      return;
    }

    // Get all the eventsub flags


    if (nActiveStreamerAccount is JoltTwitchAccount streamer)
    {

    }

    if (nActiveChatBotAccount is JoltTwitchAccount chatBot)
    { }
  }

  private async Task WebsocketDisconnected(object sender, EventArgs args)
  {
    throw new NotImplementedException();
  }

  private async Task WebsocketReconnected(object sender, EventArgs args)
  {
    throw new NotImplementedException();
  }

  /// <inheritdoc/>
  public async Task StartAsync(CancellationToken cancellationToken)
  {
    // Nothing happens here.
    // EventSub connects elsewhere,
    // leaves this method blank.
  }

  /// <inheritdoc/>
  public async Task StopAsync(CancellationToken cancellationToken)
  {
    await Client.DisconnectAsync();
  }
}

internal readonly record struct JoltEventSubArgs(string Name, string Version, params KeyValuePair<string, string>[] Conditions);