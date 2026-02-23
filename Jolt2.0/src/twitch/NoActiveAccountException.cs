namespace Nixill.Streaming.JoltBot.Twitch;

/// <summary>
///   Thrown when attempting to make an API call but no account is active.
/// </summary>
[Serializable]
internal class NoActiveAccountException : Exception
{
  public NoActiveAccountException()
  {
  }

  public NoActiveAccountException(string? message) : base(message)
  {
  }

  public NoActiveAccountException(string? message, Exception? innerException) : base(message, innerException)
  {
  }
}