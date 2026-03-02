using System.Reflection;
using Nixill.Streaming.JoltBot.BotData;
using Nixill.Streaming.JoltBot.Twitch.API;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.EventSub.Conduits.CreateConduits;
using TwitchLib.Api.Helix.Models.EventSub.Conduits.Shards.UpdateConduitShards;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;

namespace Nixill.Streaming.JoltBot.Twitch.EventSub;

/// <summary>
///   The hosted service that controls the Twitch EventSub websocket client.
/// </summary>
public partial class JoltEventService : IHostedService
{
  /// <summary>
  ///   The latest created instance of the service.
  /// </summary>
  internal static JoltEventService Instance = null!;

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
    Instance = this;

    Client = client ?? throw new ArgumentNullException(nameof(client));

    Client.WebsocketConnected += WebsocketConnected;
    Client.WebsocketDisconnected += WebsocketDisconnected;
    Client.WebsocketReconnected += WebsocketReconnected;

    RegisterEventHandlers(Client);
  }

  private async Task WebsocketConnected(object sender, WebsocketConnectedArgs args)
  {
    Connected = true;

    // If neither account is connected, disconnect the websocket so that
    // twitch doesn't have to make us.
    if (!JoltTwitchAccountManager.HasActiveAccount)
    {
      await Client.DisconnectAsync();
      return;
    }

    // If a conduit is already established, delete it.
    // (I am NOT dealing with multiplexing the conduits across users.)
    var existingConduits = await JoltTwitchAPI.Call("JoltEventService: Get existing conduits",
      api => api.Helix.EventSub.GetConduits(), JoltTwitchTokenType.AppToken);

    foreach (var conduit in existingConduits.Data)
    {
      _ = JoltTwitchAPI.Call(
        "JoltEventService: Delete conduit",
        api => api.Helix.EventSub.DeleteConduit(conduit.Id),
        JoltTwitchTokenType.AppToken
      );
    }

    // Let's establish our new conduit now
    // We really only need one
    var conduits = await JoltTwitchAPI.Call("JoltEventService: Create conduit",
      api => api.Helix.EventSub.CreateConduits(
        request: new CreateConduitsRequest { ShardCount = 1 }
      ), JoltTwitchTokenType.AppToken);

    // Assign the transport to this existing websocket session
    var transports = await JoltTwitchAPI.Call("JoltEventService: Update conduit shard",
      api => api.Helix.EventSub.UpdateConduitShards(
        request: new UpdateConduitShardsRequest
        {
          ConduitId = conduits.Data[0].Id,
          Shards = [new ShardUpdate { Id = "0", Transport = new TransportUpdate { Method = "websocket", SessionId = Client.SessionId } }]
        }
      ), JoltTwitchTokenType.AppToken);

    // Get all the EventSub subscription types
    IEnumerable<TwitchEventAttribute> subscriptions = typeof(JoltEventService).Assembly
      .GetTypes()
      .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
      .SelectMany(t => t.GetCustomAttributes<TwitchEventAttribute>());

    // Subscribe to those events
    foreach (var sub in subscriptions)
    {
      _ = JoltTwitchAPI.Call("JoltEventService: Subscribe to event",
        api => api.Helix.EventSub.CreateEventSubSubscriptionAsync(sub.Name, sub.Version,
          sub.Conditions.GetCondition(), EventSubTransportMethod.Conduit, conduitId: conduits.Data[0].Id),
        JoltTwitchTokenType.AppToken
      );
    }
  }

  private async Task WebsocketDisconnected(object sender, EventArgs args)
  {
    Connected = false;
  }

  private async Task WebsocketReconnected(object sender, EventArgs args)
  {
    Connected = true;
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

  /// <summary>
  ///   Connect to the websockets.
  /// </summary>
  /// <returns>(Task, void.)</returns>
  internal async Task ConnectAsync()
  {
    if (!Connected) await Client.ConnectAsync();
  }

  /// <summary>
  ///   Disconnect from the websockets.
  /// </summary>
  /// <returns>(Task, void.)</returns>
  internal async Task DisconnectAsync()
  {
    if (Connected) await Client.DisconnectAsync();
  }
}

internal readonly record struct JoltEventSubArgs(string Name, string Version, params KeyValuePair<string, string>[] Conditions);