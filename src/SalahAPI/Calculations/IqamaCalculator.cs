namespace SalahAPI.Calculations;

/// <summary>
/// Calculator for Iqama (congregation prayer start time) times.
/// </summary>
public static class IqamaCalculator
{
    /// <summary>
    /// Calculate Iqama times for a specific prayer across a set of days.
    /// </summary>
    /// <param name="daysData">Indexed day data (key = sequential day index).</param>
    /// <param name="prayerName">Lower-case prayer name key (e.g. "fajr").</param>
    /// <param name="rule">Calculation rule; if null, returns an empty dictionary.</param>
    /// <param name="endPrayerName">Optional prayer that marks end of this prayer's window (e.g. "sunrise" for Fajr).</param>
    public static Dictionary<int, DateTime> CalculateIqama(
        Dictionary<int, DayData> daysData,
        string prayerName,
        PrayerCalculationRule? rule = null,
        string? endPrayerName = null)
    {
        if (rule is null) return new Dictionary<int, DateTime>();

        // Static time — resolve overrides per-day
        if (rule.Static is not null)
        {
            var results = new Dictionary<int, DateTime>();
            foreach (var (dayIndex, dayData) in daysData)
            {
                // Resolve effective rule for this specific day (handles DST overrides per-day).
                // DST is resolved at the prayer's Athan time (when available) since that is when
                // the service takes place; a midnight date can still be in the previous DST
                // regime on the day the clocks change.
                var dstReference = dayData.Athan.TryGetValue(prayerName, out var athan) ? athan : (DateTime?)null;
                var effectiveRule = GetEffectiveRule(rule, dayData.Date, dstReference);
                var staticTime = TimeHelpers.ParseTimeString(dayData.Date, effectiveRule!.Static!);
                results[dayIndex] = staticTime;
            }
            return results;
        }

        // Non-static rule with overrides
        if (HasOverrides(rule))
            return CalculateIqamaWithOverrides(daysData, prayerName, rule, endPrayerName);

        // Pure base-rule calculation
        return CalculateIqamaWithRule(daysData, prayerName, rule, endPrayerName);
    }

    // ---------------------------------------------------------------------------
    // Override-aware path
    // ---------------------------------------------------------------------------

    private static Dictionary<int, DateTime> CalculateIqamaWithOverrides(
        Dictionary<int, DayData> daysData,
        string prayerName,
        PrayerCalculationRule rule,
        string? endPrayerName)
    {
        var (daysWithOverrides, daysWithoutOverrides) = PartitionDaysByOverride(daysData, rule, prayerName);

        var results = new Dictionary<int, DateTime>();

        if (daysWithoutOverrides.Count > 0)
        {
            foreach (var (k, v) in CalculateIqamaWithRule(daysWithoutOverrides, prayerName, rule, endPrayerName))
                results[k] = v;
        }

        foreach (var (dayIndex, info) in daysWithOverrides)
        {
            var single = new Dictionary<int, DayData> { [dayIndex] = info.Data };
            foreach (var (k, v) in CalculateIqamaWithRule(single, prayerName, info.Rule, endPrayerName))
                results[k] = v;
        }

        return results;
    }

    private static (Dictionary<int, OverrideDayInfo> WithOverrides, Dictionary<int, DayData> Without)
        PartitionDaysByOverride(Dictionary<int, DayData> daysData, PrayerCalculationRule rule, string prayerName)
    {
        var withOverrides = new Dictionary<int, OverrideDayInfo>();
        var without = new Dictionary<int, DayData>();

        foreach (var (dayIndex, dayData) in daysData)
        {
            var dstReference = dayData.Athan.TryGetValue(prayerName, out var athan) ? athan : (DateTime?)null;
            var effectiveRule = GetEffectiveRule(rule, dayData.Date, dstReference);
            if (!ReferenceEquals(effectiveRule, rule))
                withOverrides[dayIndex] = new OverrideDayInfo(dayData, effectiveRule!);
            else
                without[dayIndex] = dayData;
        }

        return (withOverrides, without);
    }

    // ---------------------------------------------------------------------------
    // Core calculation
    // ---------------------------------------------------------------------------

    private static Dictionary<int, DateTime> CalculateIqamaWithRule(
        Dictionary<int, DayData> daysData,
        string prayerName,
        PrayerCalculationRule rule,
        string? endPrayerName)
    {
        var results = new Dictionary<int, DateTime>();
        bool isWeekly = string.Equals(rule.Change, "weekly", StringComparison.OrdinalIgnoreCase);
        var p = GetRuleParameters(rule);

        if (isWeekly)
        {
            // Compute per-day candidate iqamas and compare them as
            // time-of-day in standard time (DST-normalized wall minutes). The winning time
            // is applied to every day of the week in that day's own DST regime, so a week
            // spanning a DST change renders as a different wall clock per side while still
            // guaranteeing the constraint holds for ALL days.
            int? bestCandidateMinutes = null;
            bool isBeforeEnd = p.BeforeEndMinutes > 0 && endPrayerName is not null;

            foreach (var (_, dayData) in daysData)
            {
                if (!dayData.Athan.TryGetValue(prayerName, out var dayAthan)) continue;

                if (isBeforeEnd && dayData.Athan.TryGetValue(endPrayerName!, out var dayEndTime))
                {
                    var candidate = TimeHelpers.RoundDown(dayEndTime, p.RoundMinutes);
                    candidate = candidate.AddMinutes(-p.BeforeEndMinutes);
                    int candidateMinutes = TimeHelpers.TimeToMinutes(candidate);
                    // Earliest candidate guarantees all days
                    if (bestCandidateMinutes is null || candidateMinutes < bestCandidateMinutes)
                        bestCandidateMinutes = candidateMinutes;
                }
                else
                {
                    var candidate = TimeHelpers.RoundUp(dayAthan, p.RoundMinutes);
                    candidate = candidate.AddMinutes(p.AfterAthanMinutes);
                    int candidateMinutes = TimeHelpers.TimeToMinutes(candidate);
                    // Latest candidate guarantees all days
                    if (bestCandidateMinutes is null || candidateMinutes > bestCandidateMinutes)
                        bestCandidateMinutes = candidateMinutes;
                }
            }

            if (bestCandidateMinutes is not null)
            {
                var bestMinutes = bestCandidateMinutes.Value;
                if (bestMinutes < 0) bestMinutes += 1440;
                int bestHour = bestMinutes / 60;
                int bestMin  = bestMinutes % 60;
                string bestTimeStr = $"{bestHour:D2}:{bestMin:D2}";

                foreach (var (dayIndex, dayData) in daysData)
                {
                    // Re-anchor the standard time on this day, then shift back to the
                    // day's own DST regime (adds one hour when the time is in DST).
                    var dayIqama = TimeHelpers.DenormalizeTimeForDst(
                        TimeHelpers.ParseTimeString(dayData.Date, bestTimeStr));

                    var minTime = TimeHelpers.ParseTimeString(dayData.Date, p.EarliestTime);
                    var maxTime = TimeHelpers.ParseTimeString(dayData.Date, p.LatestTime);

                    if (dayIqama < minTime) dayIqama = minTime;
                    if (dayIqama > maxTime) dayIqama = maxTime;

                    results[dayIndex] = dayIqama;
                }
            }

            return results;
        }

        // Daily calculation path
        var normalizedDays = TimeHelpers.NormalizeTimesForDst(daysData);

        foreach (var (dayIndex, dayData) in normalizedDays)
        {
            if (!dayData.Athan.TryGetValue(prayerName, out var dayAthan)) continue;

            DateTime dayIqama;

            if (p.BeforeEndMinutes > 0 && endPrayerName is not null &&
                dayData.Athan.TryGetValue(endPrayerName, out var dayEndTime))
            {
                dayIqama = TimeHelpers.RoundDown(dayEndTime, p.RoundMinutes);
                dayIqama = dayIqama.AddMinutes(-p.BeforeEndMinutes);
            }
            else
            {
                dayIqama = TimeHelpers.RoundUp(dayAthan, p.RoundMinutes);
                dayIqama = dayIqama.AddMinutes(p.AfterAthanMinutes);
            }

            // Denormalize before applying constraints
            dayIqama = TimeHelpers.DenormalizeTimeForDst(dayIqama);

            // Use the *original* (non-normalized) date for parsing constraint times
            var originalDate = daysData[dayIndex].Date;
            var minTime = TimeHelpers.ParseTimeString(originalDate, p.EarliestTime);
            var maxTime = TimeHelpers.ParseTimeString(originalDate, p.LatestTime);

            if (dayIqama < minTime) dayIqama = minTime;
            if (dayIqama > maxTime) dayIqama = maxTime;

            results[dayIndex] = dayIqama;
        }

        return results;
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static RuleParameters GetRuleParameters(PrayerCalculationRule rule) => new(
        RoundMinutes:      rule.RoundMinutes ?? 1,
        AfterAthanMinutes: rule.AfterAthanMinutes ?? 0,
        BeforeEndMinutes:  rule.BeforeEndMinutes ?? 0,
        EarliestTime:      rule.Earliest ?? "00:00",
        LatestTime:        rule.Latest ?? "23:59");

    /// <summary>
    /// Resolves overrides based on conditions (e.g., daylight savings time, ramadan)
    /// and returns the appropriate rule to use.
    ///
    /// The daylightSavingsTime condition is evaluated against <paramref name="dstReferenceTime"/> (the
    /// prayer's Athan time when available) rather than the date at midnight, because
    /// on the day the clocks change the midnight date still belongs to the previous
    /// DST regime while the actual prayer service does not.
    /// </summary>
    /// <param name="baseRule">The base rule with potential overrides.</param>
    /// <param name="date">The date to check for override conditions.</param>
    /// <param name="dstReferenceTime">Reference instant for the daylightSavingsTime condition (e.g., the Athan time).</param>
    /// <returns>The effective rule (override or base).</returns>
    private static PrayerCalculationRule? GetEffectiveRule(
        PrayerCalculationRule? baseRule,
        DateTime date,
        DateTime? dstReferenceTime = null)
    {
        if (baseRule?.Overrides is null || baseRule.Overrides.Count == 0) return baseRule;

        bool isDst      = IsDaylightSavingTime(dstReferenceTime ?? date);
        bool isRamadan  = HijriDateConverter.IsRamadan(date);

        foreach (var @override in baseRule.Overrides)
        {
            if (string.Equals(@override.Condition, "daylightSavingsTime", StringComparison.OrdinalIgnoreCase) && isDst) return @override.Time;
            if (string.Equals(@override.Condition, "ramadan", StringComparison.OrdinalIgnoreCase) && isRamadan) return @override.Time;
        }

        return baseRule;
    }

    private static bool HasOverrides(PrayerCalculationRule? rule)
        => rule?.Overrides is { Count: > 0 };

    private static bool IsDaylightSavingTime(DateTime date)
        => date.Kind != DateTimeKind.Utc && TimeZoneInfo.Local.IsDaylightSavingTime(date);

    // ---------------------------------------------------------------------------
    // Private data carriers
    // ---------------------------------------------------------------------------

    private sealed record RuleParameters(
        int RoundMinutes,
        int AfterAthanMinutes,
        int BeforeEndMinutes,
        string EarliestTime,
        string LatestTime);

    private sealed record OverrideDayInfo(DayData Data, PrayerCalculationRule Rule);
}
