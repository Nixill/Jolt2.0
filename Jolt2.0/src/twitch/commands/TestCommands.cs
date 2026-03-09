using Nixill.Twitch.Interactions.Attributes;
using Nixill.Twitch.Interactions.Objects;
using Nixill.Twitch.Interactions.Objects.Common;

namespace Nixill.Streaming.JoltBot.Twitch.Commands;

public static class TestCommands
{
  [Command("ping")]
  public static async Task<InteractionResponse> PingCommand(CommandContext ctx) => "Pong!";
}