using SalahAPI;
using Xunit;

namespace SalahAPI.Tests;

public class SalahAPIDocumentTests
{
    [Fact]
    public void CreateBasicDocument()
    {
        var doc = new SalahAPIDocument("1.0");
        Assert.Equal("1.0", doc.Salahapi);
        Assert.Null(doc.Info);
        Assert.Null(doc.Location);
        Assert.Null(doc.CalculationMethod);
        Assert.Null(doc.DailyPrayerTimes);
    }

    [Fact]
    public void CreateDocumentWithInfo()
    {
        var contact = new Contact("Support", "support@example.com");
        var info = new Info(
            "New York Islamic Center Prayer Times",
            "Prayer times for New York City using ISNA calculation method",
            "1.0.0",
            contact);

        var doc = new SalahAPIDocument("1.0", info);

        Assert.NotNull(doc.Info);
        Assert.Equal("New York Islamic Center Prayer Times", doc.Info.Title);
        Assert.Equal("support@example.com", doc.Info.Contact!.Email);
    }

    [Fact]
    public void CreateDocumentWithLocation()
    {
        var location = new Location(40.7128, -74.0060, "America/New_York",
            "YYYY-MM-DD", "HH:mm:ss", "New York", "United States");

        var doc = new SalahAPIDocument("1.0", location: location);

        Assert.NotNull(doc.Location);
        Assert.Equal(40.7128, doc.Location.Latitude);
        Assert.Equal(-74.0060, doc.Location.Longitude);
        Assert.Equal("America/New_York", doc.Location.Timezone);
        Assert.Equal("New York", doc.Location.City);
    }

    [Fact]
    public void CreateDocumentWithCalculationMethod()
    {
        var method = new CalculationMethod("ISNA", 15.0, 15.0, "Standard", "MiddleOfTheNight");
        var doc = new SalahAPIDocument("1.0", calculationMethod: method);

        Assert.NotNull(doc.CalculationMethod);
        Assert.Equal("ISNA", doc.CalculationMethod.Name);
        Assert.Equal(15.0, doc.CalculationMethod.FajrAngle);
        Assert.Equal(15.0, doc.CalculationMethod.IshaAngle);
    }

    [Fact]
    public void CreateDocumentWithIqamaRules()
    {
        var fajrRule = new PrayerCalculationRule(change: "daily", roundMinutes: 15,
            earliest: "04:00", latest: "06:45", beforeEndMinutes: 30);

        var dhuhrRule = new PrayerCalculationRule(@static: "12:30");

        var iqamaRules = new IqamaCalculationRules("friday", fajrRule, dhuhrRule);
        var method = new CalculationMethod("ISNA", 15.0, 15.0, "Standard", "MiddleOfTheNight", iqamaRules);
        var doc = new SalahAPIDocument("1.0", calculationMethod: method);

        Assert.NotNull(doc.CalculationMethod!.IqamaCalculationRules);
        Assert.Equal("friday", doc.CalculationMethod.IqamaCalculationRules.ChangeOn);
        Assert.Equal("daily", doc.CalculationMethod.IqamaCalculationRules.Fajr!.Change);
        Assert.Equal("12:30", doc.CalculationMethod.IqamaCalculationRules.Dhuhr!.Static);
    }

    [Fact]
    public void SerializeToJsonAndDeserialize()
    {
        var doc = new SalahAPIDocument("1.1",
            new Info("Test Title", "Test Description"),
            new Location(40.7128, -74.0060, "America/New_York"));

        string json = doc.ToJson();
        var deserialized = SalahAPIDocument.FromJson(json);

        Assert.Equal(doc.Salahapi, deserialized.Salahapi);
        Assert.Equal(doc.Info!.Title, deserialized.Info!.Title);
        Assert.Equal(doc.Location!.Latitude, deserialized.Location!.Latitude);
        Assert.Equal(doc.Location.Timezone, deserialized.Location.Timezone);
    }

    [Fact]
    public void CreateDocumentWithJumuahRules()
    {
        var location = new JumuahLocation("New York Islamic Center", "123 Main St, New York, NY 10001");
        var time = new PrayerCalculationRule(@static: "12:00");
        var jumuahRule = new JumuahRule("Jumuah 1", time, location);

        var method = new CalculationMethod("ISNA", jumuahRules: new List<JumuahRule> { jumuahRule });
        var doc = new SalahAPIDocument("1.0", calculationMethod: method);

        Assert.NotNull(doc.CalculationMethod!.JumuahRules);
        Assert.Single(doc.CalculationMethod.JumuahRules);
        Assert.Equal("Jumuah 1", doc.CalculationMethod.JumuahRules[0].Name);
        Assert.Equal("12:00", doc.CalculationMethod.JumuahRules[0].Time!.Static);
        Assert.Equal("New York Islamic Center", doc.CalculationMethod.JumuahRules[0].Location!.Name);
    }

    [Fact]
    public void CreateDocumentWithDailyPrayerTimes()
    {
        var csvParams = new CsvUrlParameters()
            .AddDateParameter("fromDate", "query", "fromDate", "YYYY-MM-DD")
            .AddDateParameter("toDate", "query", "toDate", "YYYY-MM-DD")
            .AddStaticParameter("apiVersion", "query", "2.0");

        var dpt = new DailyPrayerTimes("https://example.com/prayer_times",
            "YYYY-MM-DD", "HH:mm:ss", csvParams);

        var doc = new SalahAPIDocument("1.0", dailyPrayerTimes: dpt);

        Assert.NotNull(doc.DailyPrayerTimes);
        Assert.Equal("https://example.com/prayer_times", doc.DailyPrayerTimes.CsvUrl);
        Assert.Equal("YYYY-MM-DD", doc.DailyPrayerTimes.DateFormat);
    }
}
