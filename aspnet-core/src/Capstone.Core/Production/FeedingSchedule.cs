using Abp.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Capstone.Production;

public static class FeedingSchedule
{
        public const int MaxFeedingsPerDay = 8;

    public const int MaxTimesLength = 6 * MaxFeedingsPerDay;

    public const string DefaultTimes = "06:00,10:00,14:00,18:00";

        public static List<string> Times(string stored)
    {
        try
        {
            return Parse(stored);
        }
        catch (UserFriendlyException)
        {
            return Parse(DefaultTimes);
        }
    }

        public static List<string> Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            value = DefaultTimes;
        }

        var times = new List<TimeOnly>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TimeOnly.TryParseExact(part, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            {
                throw new UserFriendlyException($"'{part}' is not a time. Enter each feeding time as hours and minutes, for example 06:00.");
            }

            times.Add(t);
        }

        if (times.Count == 0 || times.Count > MaxFeedingsPerDay)
        {
            throw new UserFriendlyException($"Enter between 1 and {MaxFeedingsPerDay} feeding times.");
        }

        for (var i = 1; i < times.Count; i++)
        {
            if (times[i] <= times[i - 1])
            {
                throw new UserFriendlyException("List the feeding times from earliest to latest, each time once.");
            }
        }

        return times.Select(t => t.ToString("HH:mm", CultureInfo.InvariantCulture)).ToList();
    }

        public static string Format(IEnumerable<string> times) => string.Join(",", times);

        public static string Label(int feedingNo) => $"Feeding {feedingNo}";

        public static TimeOnly MovementTime(int feedingNo, string fedAtTime, IReadOnlyList<string> times)
    {
        if (!string.IsNullOrEmpty(fedAtTime) && TimeOnly.TryParseExact(fedAtTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var typed))
        {
            return typed;
        }

        TimeOnly At(int index) => TimeOnly.ParseExact(times[index], "HH:mm", CultureInfo.InvariantCulture);
        if (feedingNo >= 1 && feedingNo <= times.Count)
        {
            return At(feedingNo - 1);
        }

        var last = At(times.Count - 1);
        var later = last.AddHours(feedingNo - times.Count);
        return later < last ? new TimeOnly(23, 59) : later;
    }
}
