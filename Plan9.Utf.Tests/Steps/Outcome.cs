using System.Globalization;

namespace Plan9.Utf.Tests.Steps;

// What a step got, for the steps that look at it.
public sealed class Outcome
{
    public byte[] Bytes { get; set; } = [];

    public string Text { get; set; } = string.Empty;

    public int Count { get; set; }

    // "e2 82 ac" as its bytes.
    public static byte[] Hex(string bytes)
        => [.. bytes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(b => byte.Parse(b, NumberStyles.HexNumber, CultureInfo.InvariantCulture))];

    // "0041 D83D" as the UTF-16 it lists.
    public static string Units(string text)
        => new([.. text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(u => (char)ushort.Parse(u, NumberStyles.HexNumber, CultureInfo.InvariantCulture))]);

    // "0x20AC" as a rune, and "-1" as a long.
    public static uint Rune(string value)
        => value.StartsWith("0x", StringComparison.Ordinal)
            ? uint.Parse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : unchecked((uint)int.Parse(value, CultureInfo.InvariantCulture));

    public static string ShowUnits(string text) => string.Join(' ', text.Select(c => ((int)c).ToString("X4", CultureInfo.InvariantCulture)));

    public static string ShowBytes(ReadOnlySpan<byte> bytes) => Convert.ToHexString(bytes).ToLowerInvariant().Chunk(2).Select(b => new string(b)).Aggregate(string.Empty, (shown, b) => shown.Length == 0 ? b : $"{shown} {b}");
}
