namespace Tickflo.Core.Utils;

public static class DurationFormatter
{
    public static string FormatExpiresIn(int maxAgeInSeconds)
    {
        if (maxAgeInSeconds % 86400 == 0)
        {
            var days = maxAgeInSeconds / 86400;
            return days == 1 ? "1 day" : $"{days} days";
        }

        if (maxAgeInSeconds % 3600 == 0)
        {
            var hours = maxAgeInSeconds / 3600;
            return hours == 1 ? "1 hour" : $"{hours} hours";
        }

        if (maxAgeInSeconds % 60 == 0)
        {
            var minutes = maxAgeInSeconds / 60;
            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }

        return $"{maxAgeInSeconds} seconds";
    }
}
