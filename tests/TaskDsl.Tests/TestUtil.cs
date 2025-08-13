namespace TaskDsl.Tests;

public static class TestUtil
{
    public static readonly TimeZoneInfo ChicagoTz =
        TimeZoneInfo.FindSystemTimeZoneById(
#if WINDOWS
            "Central Standard Time"
#else
            "America/Chicago"
#endif
        );

    // Fixed “now” so tests are deterministic: Tue Aug 12, 2025 12:00 UTC
    public static readonly DateTimeOffset FixedNowUtc =
        new DateTimeOffset(2025, 08, 12, 12, 00, 00, TimeSpan.Zero);
}