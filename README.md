# SalahAPI .NET

A .NET 8 library for working with the [SalahAPI](https://salahapi.com) specification and calculating Islamic prayer times.

## Installation

```
dotnet add package SalahAPI
```

## Usage

### Calculate prayer times

```csharp
using SalahAPI.Calculations;

var pt = new PrayerTimes(Method.MethodIsna);
var times = pt.GetTimesForToday(
    latitude:  40.7128,
    longitude: -74.0060,
    timezone:  "America/New_York"
);

Console.WriteLine($"Fajr:    {times[PrayerTimes.Fajr]}");
Console.WriteLine($"Sunrise: {times[PrayerTimes.Sunrise]}");
Console.WriteLine($"Dhuhr:   {times[PrayerTimes.Zhuhr]}");
Console.WriteLine($"Asr:     {times[PrayerTimes.Asr]}");
Console.WriteLine($"Maghrib: {times[PrayerTimes.Maghrib]}");
Console.WriteLine($"Isha:    {times[PrayerTimes.Isha]}");
```

### Build a date-range CSV

```csharp
using SalahAPI;
using SalahAPI.Calculations;

var location = new Location
{
    Latitude   = 40.7128,
    Longitude  = -74.0060,
    Timezone   = "America/New_York",
    DateFormat = "yyyy-MM-dd",
    TimeFormat = "HH:mm"
};

var method = new CalculationMethod { Name = Method.MethodIsna };

var builder = new Builder(location, method);
var csv = builder.BuildCsv(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));
Console.WriteLine(csv);
```

### Hijri date & Ramadan detection

```csharp
using SalahAPI.Calculations;

var hijri = HijriDateConverter.ConvertToHijri(DateTime.Today);
Console.WriteLine($"{hijri.Day}/{hijri.Month}/{hijri.Year} AH");

bool isRamadan = HijriDateConverter.IsRamadan(DateTime.Today);
```

## Supported calculation methods

`MWL`, `ISNA`, `Egypt`, `Makkah`, `Karachi`, `Tehran`, `Jafari`, `Gulf`, `Kuwait`, `Qatar`, `Singapore`, `France`, `Turkey`, `Russia`, `Moonsighting`, `Dubai`, `JAKIM`, `Tunisia`, `Algeria`, `Kemenag`, `Morocco`, `Portugal`, `Jordan`

## License

[MIT](LICENSE)
