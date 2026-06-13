namespace SalahAPI.Calculations;

/// <summary>
/// Calculation method definitions and configurations.
/// </summary>
public class Method
{
    // Available calculation method constants
    public const string MethodJafari = "JAFARI";
    public const string MethodKarachi = "KARACHI";
    public const string MethodIsna = "ISNA";
    public const string MethodMwl = "MWL";
    public const string MethodMakkah = "MAKKAH";
    public const string MethodEgypt = "EGYPT";
    public const string MethodTehran = "TEHRAN";
    public const string MethodGulf = "GULF";
    public const string MethodKuwait = "KUWAIT";
    public const string MethodQatar = "QATAR";
    public const string MethodSingapore = "SINGAPORE";
    public const string MethodFrance = "FRANCE";
    public const string MethodTurkey = "TURKEY";
    public const string MethodRussia = "RUSSIA";
    public const string MethodMoonsighting = "MOONSIGHTING";
    public const string MethodDubai = "DUBAI";
    public const string MethodJakim = "JAKIM";
    public const string MethodTunisia = "TUNISIA";
    public const string MethodAlgeria = "ALGERIA";
    public const string MethodKemenag = "KEMENAG";
    public const string MethodMorocco = "MOROCCO";
    public const string MethodPortugal = "PORTUGAL";
    public const string MethodJordan = "JORDAN";
    public const string MethodCustom = "CUSTOM";

    public string Name { get; set; }
    public Dictionary<string, object> Params { get; set; }

    public Method(string name = "Custom")
    {
        Name = name;
        Params = new Dictionary<string, object>
        {
            [PrayerTimes.Fajr] = 15.0,
            [PrayerTimes.Isha] = 15.0
        };
    }

    public void SetFajrAngle(double angle) => Params[PrayerTimes.Fajr] = angle;

    public void SetMaghribAngleOrMins(object angleOrMinsAfterSunset)
        => Params[PrayerTimes.Maghrib] = angleOrMinsAfterSunset;

    public void SetIshaAngleOrMins(object angleOrMinsAfterMaghrib)
        => Params[PrayerTimes.Isha] = angleOrMinsAfterMaghrib;

    public static IReadOnlyList<string> GetMethodCodes() =>
    [
        MethodMwl, MethodIsna, MethodEgypt, MethodMakkah, MethodKarachi,
        MethodTehran, MethodJafari, MethodGulf, MethodKuwait, MethodQatar,
        MethodSingapore, MethodFrance, MethodTurkey, MethodRussia,
        MethodMoonsighting, MethodDubai, MethodJakim, MethodTunisia,
        MethodAlgeria, MethodKemenag, MethodMorocco, MethodPortugal,
        MethodJordan, MethodCustom
    ];

    /// <summary>Returns configuration details for all built-in calculation methods.</summary>
    public static IReadOnlyDictionary<string, MethodInfo> GetMethods()
    {
        return new Dictionary<string, MethodInfo>
        {
            [MethodMwl] = new MethodInfo(3, "Muslim World League",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 18.0, [PrayerTimes.Isha] = 17.0 },
                51.5194682, -0.1360365),

            [MethodIsna] = new MethodInfo(2, "Islamic Society of North America (ISNA)",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 15.0, [PrayerTimes.Isha] = 15.0 },
                39.70421229999999, -86.39943869999999),

            [MethodEgypt] = new MethodInfo(5, "Egyptian General Authority of Survey",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 19.5, [PrayerTimes.Isha] = 17.5 },
                30.0444196, 31.2357116),

            [MethodMakkah] = new MethodInfo(4, "Umm Al-Qura University, Makkah",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 18.5, [PrayerTimes.Isha] = "90 min" },
                21.3890824, 39.8579118),

            [MethodKarachi] = new MethodInfo(1, "University of Islamic Sciences, Karachi",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 18.0, [PrayerTimes.Isha] = 18.0 },
                24.8614622, 67.0099388),

            [MethodTehran] = new MethodInfo(7, "Institute of Geophysics, University of Tehran",
                new Dictionary<string, object>
                {
                    [PrayerTimes.Fajr] = 17.7, [PrayerTimes.Isha] = 14.0,
                    [PrayerTimes.Maghrib] = 4.5, [PrayerTimes.Midnight] = MethodJafari
                },
                35.6891975, 51.3889736),

            [MethodJafari] = new MethodInfo(0, "Shia Ithna-Ashari, Leva Institute, Qum",
                new Dictionary<string, object>
                {
                    [PrayerTimes.Fajr] = 16.0, [PrayerTimes.Isha] = 14.0,
                    [PrayerTimes.Maghrib] = 4.0, [PrayerTimes.Midnight] = MethodJafari
                },
                34.6415764, 50.8746035),

            [MethodGulf] = new MethodInfo(8, "Gulf Region",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 19.5, [PrayerTimes.Isha] = "90 min" },
                24.1323638, 53.3199527),

            [MethodKuwait] = new MethodInfo(9, "Kuwait",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 18.0, [PrayerTimes.Isha] = 17.5 },
                29.375859, 47.9774052),

            [MethodQatar] = new MethodInfo(10, "Qatar",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 18.0, [PrayerTimes.Isha] = "90 min" },
                25.2854473, 51.5310398),

            [MethodSingapore] = new MethodInfo(11, "Majlis Ugama Islam Singapura, Singapore",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 20.0, [PrayerTimes.Isha] = 18.0 },
                1.352083, 103.819836),

            [MethodFrance] = new MethodInfo(12, "Union des organisations islamiques de France",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 12.0, [PrayerTimes.Isha] = 12.0 },
                48.8566101, 2.3514992),

            [MethodTurkey] = new MethodInfo(13, "Diyanet İşleri Başkanlığı, Turkey",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 18.0, [PrayerTimes.Isha] = 17.0 },
                39.9199, 32.8543),

            [MethodRussia] = new MethodInfo(14, "Spiritual Administration of Muslims of Russia",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 16.0, [PrayerTimes.Isha] = 15.0 },
                55.7522, 37.6156),

            [MethodDubai] = new MethodInfo(16, "Dubai",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 18.2, [PrayerTimes.Isha] = 18.2 },
                25.2048, 55.2708),

            [MethodJakim] = new MethodInfo(17, "Jabatan Kemajuan Islam Malaysia",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 20.0, [PrayerTimes.Isha] = 18.0 },
                3.1412, 101.6865),

            [MethodTunisia] = new MethodInfo(18, "Tunisia",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 18.0, [PrayerTimes.Isha] = 18.0 },
                36.8065, 10.1815),

            [MethodAlgeria] = new MethodInfo(19, "Algeria",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 18.0, [PrayerTimes.Isha] = 17.0 },
                36.7372, 3.0865),

            [MethodKemenag] = new MethodInfo(20, "Kementerian Agama Republik Indonesia",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 20.0, [PrayerTimes.Isha] = 18.0 },
                -6.2088, 106.8456),

            [MethodMorocco] = new MethodInfo(21, "Morocco",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 18.0, [PrayerTimes.Isha] = 17.0 },
                33.9716, -6.8498),

            [MethodPortugal] = new MethodInfo(22, "Portugal",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 18.0, [PrayerTimes.Isha] = 17.0 },
                38.7223, -9.1393),

            [MethodJordan] = new MethodInfo(23, "Jordan",
                new Dictionary<string, object> { [PrayerTimes.Fajr] = 18.0, [PrayerTimes.Isha] = 17.0 },
                31.9522, 35.2332),
        };
    }
}

/// <summary>Configuration record for a single calculation method.</summary>
public record MethodInfo(
    int Id,
    string Name,
    Dictionary<string, object> Params,
    double Latitude,
    double Longitude);
