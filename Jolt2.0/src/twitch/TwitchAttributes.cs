using System.Text.Json.Nodes;
using Nixill.Streaming.JoltBot.BotData;
using Nixill.Utils.Extensions;

namespace Nixill.Streaming.JoltBot.Twitch;

/// <summary>
///   Marks a method as using a given scope in the Twitch API.
///   Automatically collected at runtime to denote which scopes are needed
///   for each token.
/// </summary>
/// <param name="scope">The scope used.</param>
/// <param name="chatBot">
///   Whether this scope is used on the streamer account (false) or the
///   chat bot account (true).
/// </param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class UsesAuthScopeAttribute(string scope, bool chatBot = false) : Attribute
{
  /// <summary>
  ///   The scope used.
  /// </summary>
  public readonly string Scope = scope;

  /// <summary>
  ///   Whether this scope is used on the streamer account (false) or the
  ///   chat bot account (true).
  /// </summary>
  public readonly bool IsChatBot = chatBot;
}

/// <summary>
///   Specifies that a method should create a Twitch EventSub subscription.
/// </summary>
/// <remarks>
///   Don't forget to also add a <see cref="UsesAuthScopeAttribute"/> to
///   the method as applicable!
/// </remarks>
/// <param name="isChatBot">
///   Whether this event should be scoped to the chat bot (<see langword="true"/>)
///   or the streamer (<see langword="false"/>).
/// </param>
/// <param name="name">
///   The name of the event.
/// </param>
/// <param name="version">
///   The version of the event.
/// </param>
/// <param name="conditions">
///   One or more conditions placed on the event subscription.
/// </param>
/// <seealso href="https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/"/>
[AttributeUsage(AttributeTargets.Method)]
public class TwitchEventAttribute(bool isChatBot, string name, string version, TwitchEventCondition conditions) : Attribute
{
  public readonly bool IsChatBot = isChatBot;
  public readonly string Name = name;
  public readonly string Version = version;
  public readonly TwitchEventCondition Conditions = conditions;
}

/// <summary>
///   Event conditions for Twitch EventSub.
/// </summary>
[Flags]
public enum TwitchEventCondition : uint
{
  None = 0,

  /// <summary>
  ///   broadcaster_user_id = (streamer's id)
  /// </summary>
  Broadcaster = 1 << 0,

  /// <summary>
  ///   moderator_user_id = (streamer's id)
  /// </summary>
  Moderator = 1 << 1,

  /// <summary>
  ///   user_id = (streamer's id)
  /// </summary>
  User = 1 << 2,

  /// <summary>
  ///   from_broadcaster_user_id = (streamer's id)
  /// </summary>
  FromBroadcaster = 1 << 3,

  /// <summary>
  ///   to_broadcaster_user_id = (streamer's id)
  /// </summary>
  ToBroadcaster = 1 << 4,

  /// <summary>
  ///   user_id = (chat bot's id)
  /// </summary>
  UserChatBot = 1 << 5,
}

/// <summary>
///   Static methods dealing with <see cref="TwitchEventCondition"/>s.
/// </summary>
public static class TwitchEventConditions
{
  /// <summary>
  ///   Gets a <see cref="JsonObject"/> with the conditions filled in.
  /// </summary>
  /// <param name="condition">The composite condition.</param>
  /// <returns>The JsonObject.</returns>
  public static JsonObject GetConditionObject(this TwitchEventCondition condition)
    => new(condition.GetConditionPairs());

  /// <summary>
  ///   Gets a set of <see cref="KeyValuePair{TKey, TValue}"/>s pertaining
  ///   to the composite condition.
  /// </summary>
  /// <param name="condition">The composite condition.</param>
  /// <returns>The KeyValuePairs.</returns>
  public static IEnumerable<KeyValuePair<string, JsonNode?>> GetConditionPairs(this TwitchEventCondition condition)
    => condition.GetFlags().Select(c => new KeyValuePair<string, JsonNode?>(
      key: c switch
      {
        TwitchEventCondition.Broadcaster => "broadcaster_user_id",
        TwitchEventCondition.Moderator => "moderator_user_id",
        TwitchEventCondition.User or TwitchEventCondition.UserChatBot => "user_id",
        TwitchEventCondition.FromBroadcaster => "from_broadcaster_user_id",
        TwitchEventCondition.ToBroadcaster => "to_broadcaster_user_id",
        _ => default!
      },
      value: c switch
      {
        TwitchEventCondition.Broadcaster or TwitchEventCondition.Moderator or TwitchEventCondition.User
          or TwitchEventCondition.FromBroadcaster or TwitchEventCondition.ToBroadcaster
            => JoltTwitchAccountManager.ActiveStreamerUID,
        TwitchEventCondition.UserChatBot => JoltTwitchAccountManager.ActiveChatBotUID,
        _ => default!
      }
    ));
}