using Nixill.Streaming.JoltBot.BotData;
using NodaTime;
using NodaTime.Text;

namespace Nixill.Streaming.JoltBot.NodaTime;

/// <summary>
///   Time formatting shortcuts
/// </summary>
public static class TimeFormats
{
  static ZonedDateTimePattern Time24 = ZonedDateTimePattern.CreateWithCurrentCulture("HH:mm", DateTimeZoneProviders.Tzdb);
  static ZonedDateTimePattern Time12 = ZonedDateTimePattern.CreateWithCurrentCulture("hh:mm tt", DateTimeZoneProviders.Tzdb);
  static DateTimeZone TimeZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();

  /// <summary>
  ///   Converts an instant to a time string in either 12 or 24 hour
  ///   format (depending on settings) and the current time zone.
  /// </summary>
  /// <param name="inst">The instant.</param>
  /// <returns>The string.</returns>
  public static string GetTime(Instant inst)
  {
    ZonedDateTime zdt = inst.InZone(TimeZone);

    if (Data.Settings.Use24HourTime) return Time24.Format(zdt);
    else return Time12.Format(zdt);
  }
}