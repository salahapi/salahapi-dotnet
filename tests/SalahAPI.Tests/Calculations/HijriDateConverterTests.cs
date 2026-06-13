using SalahAPI.Calculations;
using Xunit;

namespace SalahAPI.Tests.Calculations;

public class HijriDateConverterTests
{
    [Theory]
    [InlineData(2024, 3, 11, 9)]  // Ramadan 1445 AH starts ~11 March 2024
    [InlineData(2024, 3, 31, 9)]  // Still Ramadan 1445 AH
    [InlineData(2024, 4, 9, 9)]   // Last day of Ramadan 1445
    [InlineData(2024, 4, 10, 10)] // Shawwal starts
    public void IsRamadan_CorrectMonth(int year, int month, int day, int expectedMonth)
    {
        var date = new DateTime(year, month, day);
        var hijri = HijriDateConverter.ConvertToHijri(date);
        Assert.Equal(expectedMonth, hijri.Month);
    }

    [Fact]
    public void IsRamadan_DuringRamadan_ReturnsTrue()
    {
        // Ramadan 1445 AH: ~11 March – 9 April 2024
        var date = new DateTime(2024, 3, 20);
        Assert.True(HijriDateConverter.IsRamadan(date));
    }

    [Fact]
    public void IsRamadan_OutsideRamadan_ReturnsFalse()
    {
        var date = new DateTime(2024, 1, 1); // Well outside Ramadan
        Assert.False(HijriDateConverter.IsRamadan(date));
    }

    [Fact]
    public void ConvertToHijri_KnownDate_ReturnsCorrectYear()
    {
        // 1 January 2000 = 24 Ramadan 1420 AH
        var date = new DateTime(2000, 1, 1);
        var hijri = HijriDateConverter.ConvertToHijri(date);
        Assert.Equal(1420, hijri.Year);
        Assert.Equal(9, hijri.Month);  // Ramadan
    }
}
