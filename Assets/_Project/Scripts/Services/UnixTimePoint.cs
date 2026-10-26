using System;
using System.Globalization;

namespace DungeonBuilder.M0
{
    // Representation precision is 100 ns; this makes no claim about clock accuracy.
    public sealed class UnixTimePoint : IEquatable<UnixTimePoint>, IComparable<UnixTimePoint>
    {
        public const long UnitsPerSecond = TimeSpan.TicksPerSecond;
        public const long MinimumSeconds = -62135596800L;
        public const long MaximumSeconds = 253402300799L;
        public long Seconds { get; }
        public int Fraction100ns { get; }
        public long Unix100ns => checked(Seconds * UnitsPerSecond + Fraction100ns);

        public UnixTimePoint(long seconds, int fraction100ns)
        {
            if (seconds < MinimumSeconds || seconds > MaximumSeconds || fraction100ns < 0 || fraction100ns >= UnitsPerSecond)
                throw new ArgumentOutOfRangeException(nameof(seconds), "time.point.invalid");
            Seconds = seconds; Fraction100ns = fraction100ns;
        }

        public static UnixTimePoint FromDateTimeOffset(DateTimeOffset value) =>
            FromUnix100ns(checked(value.UtcDateTime.Ticks - DateTimeOffset.UnixEpoch.Ticks));
        public DateTimeOffset ToDateTimeOffset() => new DateTimeOffset(
            checked(Unix100ns + DateTimeOffset.UnixEpoch.Ticks), TimeSpan.Zero);
        public static UnixTimePoint FromUnix100ns(long value)
        {
            long seconds = value / UnitsPerSecond;
            long fraction = value % UnitsPerSecond;
            // C# division truncates towards zero; canonical fractions are nonnegative.
            if (fraction < 0) { seconds = checked(seconds - 1); fraction = checked(fraction + UnitsPerSecond); }
            return new UnixTimePoint(seconds, checked((int)fraction));
        }
        public UnixTimePoint Add100ns(long duration) => FromUnix100ns(checked(Unix100ns + duration));
        public long Elapsed100nsSince(UnixTimePoint earlier)
        {
            if (earlier == null || CompareTo(earlier) < 0) throw new ArgumentException("time.point.backward");
            return checked(Unix100ns - earlier.Unix100ns);
        }
        public int CompareTo(UnixTimePoint other) => other == null ? 1 : Unix100ns.CompareTo(other.Unix100ns);
        public bool Equals(UnixTimePoint other) => other != null && Seconds == other.Seconds && Fraction100ns == other.Fraction100ns;
        public override bool Equals(object obj) => Equals(obj as UnixTimePoint);
        public override int GetHashCode() => HashCode.Combine(Seconds, Fraction100ns);
        public string ToCanonicalJson() => "{\"Seconds\":" + Seconds.ToString(CultureInfo.InvariantCulture) +
            ",\"Fraction100ns\":" + Fraction100ns.ToString(CultureInfo.InvariantCulture) + "}";
        public static bool TryParseCanonicalJson(string text, out UnixTimePoint result)
        {
            result = null;
            const string prefix = "{\"Seconds\":";
            const string separator = ",\"Fraction100ns\":";
            if (text == null || !text.StartsWith(prefix, StringComparison.Ordinal) || !text.EndsWith("}", StringComparison.Ordinal)) return false;
            int split = text.IndexOf(separator, prefix.Length, StringComparison.Ordinal);
            if (split < 0 || !long.TryParse(text.Substring(prefix.Length, split - prefix.Length), NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out long seconds) || !int.TryParse(text.Substring(split + separator.Length,
                    text.Length - split - separator.Length - 1), NumberStyles.None, CultureInfo.InvariantCulture, out int fraction)) return false;
            try {
                var point = new UnixTimePoint(seconds, fraction);
                if (point.ToCanonicalJson() != text) return false;
                result = point; return true;
            } catch (ArgumentOutOfRangeException) { return false; }
        }
    }

    public interface IPreciseTimeSource : ITimeSource
    {
        UnixTimePoint UtcNow();
    }
}
