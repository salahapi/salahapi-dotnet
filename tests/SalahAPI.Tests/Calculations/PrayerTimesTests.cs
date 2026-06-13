using SalahAPI.Calculations;
using Xunit;

namespace SalahAPI.Tests.Calculations;

public class PrayerTimesTests
{
    private const double NewYorkLat  =  40.7128;
    private const double NewYorkLng  = -74.0060;
    private const string NewYorkTz   = "America/New_York";

    [Fact]
    public void GetTimes_NewYork_Winter_ReturnsExpectedPrayers()
    {
        var pt = new PrayerTimes(Method.MethodMwl);
        var date = new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Unspecified);

        // Tell the instance about the timezone by injecting it via the settings reflection
        // (The public API uses the date's timezone derived from the builder context; here we
        //  set it via GetTimesForToday to avoid reflection.)
        var times = pt.GetTimesForToday(NewYorkLat, NewYorkLng, NewYorkTz,
            format: PrayerTimes.TimeFormat24H);

        // Smoke test: all expected keys are present
        Assert.Contains(PrayerTimes.Fajr,    times.Keys);
        Assert.Contains(PrayerTimes.Sunrise, times.Keys);
        Assert.Contains(PrayerTimes.Zhuhr,   times.Keys);
        Assert.Contains(PrayerTimes.Asr,     times.Keys);
        Assert.Contains(PrayerTimes.Maghrib, times.Keys);
        Assert.Contains(PrayerTimes.Isha,    times.Keys);

        // All times must be "HH:mm" formatted
        foreach (var (key, value) in times)
            Assert.Matches(@"^\d{2}:\d{2}$", value);
    }

    [Fact]
    public void GetTimes_SpecificDate_FajrBeforeSunrise()
    {
        var pt = new PrayerTimes(Method.MethodIsna);
        // Use a fixed winter date (Jan 15 2023) to make the test deterministic
        var date = new DateTime(2023, 1, 15);
        var times = pt.GetTimes(date, NewYorkLat, NewYorkLng,
            ianaTimezone: NewYorkTz, format: PrayerTimes.TimeFormat24H);

        TimeSpan ParseTime(string t) => TimeSpan.Parse(t);

        Assert.True(ParseTime(times[PrayerTimes.Fajr]) < ParseTime(times[PrayerTimes.Sunrise]));
        Assert.True(ParseTime(times[PrayerTimes.Sunrise]) < ParseTime(times[PrayerTimes.Zhuhr]));
        Assert.True(ParseTime(times[PrayerTimes.Zhuhr]) < ParseTime(times[PrayerTimes.Asr]));
        Assert.True(ParseTime(times[PrayerTimes.Asr]) < ParseTime(times[PrayerTimes.Maghrib]));
        Assert.True(ParseTime(times[PrayerTimes.Maghrib]) < ParseTime(times[PrayerTimes.Isha]));
    }

    [Fact]
    public void HanafiAsr_IsLaterThanStandardAsr()
    {
        var standard = new PrayerTimes(Method.MethodMwl, PrayerTimes.SchoolStandard);
        var hanafi   = new PrayerTimes(Method.MethodMwl, PrayerTimes.SchoolHanafi);

        var stdTimes = standard.GetTimesForToday(NewYorkLat, NewYorkLng, NewYorkTz);
        var hanTimes = hanafi.GetTimesForToday(NewYorkLat, NewYorkLng, NewYorkTz);

        TimeSpan ParseTime(string t) => TimeSpan.Parse(t);

        // Hanafi Asr is always the same or later than Standard Asr
        Assert.True(ParseTime(hanTimes[PrayerTimes.Asr]) >= ParseTime(stdTimes[PrayerTimes.Asr]));
    }
}
