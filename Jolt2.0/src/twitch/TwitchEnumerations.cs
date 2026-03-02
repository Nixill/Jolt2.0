namespace Nixill.Streaming.JoltBot.Twitch;

/// <summary>
///   Represents which token type (streamer, chat bot, or app token) is
///   needed for a call or scope.
/// </summary>
public enum JoltTwitchTokenType
{
  /// <summary>
  ///   Use the app token, no user authentication.
  /// </summary>
  AppToken = 0,

  /// <summary>
  ///   Use the streamer user token.
  /// </summary>
  Streamer = 1,

  /// <summary>
  ///   Use the chat bot user token.
  /// </summary>
  ChatBot = 2
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
