using System.Text;

namespace Plan9.Utf;

/// <summary>
/// UTF-8 as Plan 9 reads and writes it: bytes are read as chartorune reads them, each byte of a bad
/// sequence being U+FFFD and a surrogate being read like any other rune, and text is written as
/// runetochar writes its runes, a lone surrogate as the rune it is. Its fallbacks are never used.
/// </summary>
public sealed class Plan9Utf8Encoding : Encoding
{
    public Plan9Utf8Encoding()
        : base(65001)
    {
    }

    public static Plan9Utf8Encoding Instance { get; } = new();

    public override string EncodingName => "Plan 9 UTF-8";

    public override bool Equals(object? value) => value is Plan9Utf8Encoding;

    public override int GetHashCode() => typeof(Plan9Utf8Encoding).GetHashCode();

    public override Decoder GetDecoder() => new Plan9Decoder();

    public override Encoder GetEncoder() => new Plan9Encoder();

    public override int GetByteCount(char[] chars, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(chars);
        return GetByteCount(new ReadOnlySpan<char>(chars, index, count));
    }

    public override int GetByteCount(ReadOnlySpan<char> chars)
    {
        char high = '\0';
        return Encode(chars, [], write: false, flush: true, ref high);
    }

    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        ArgumentNullException.ThrowIfNull(chars);
        ArgumentNullException.ThrowIfNull(bytes);
        return GetBytes(new ReadOnlySpan<char>(chars, charIndex, charCount), bytes.AsSpan(byteIndex));
    }

    public override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
    {
        char high = '\0';
        return Encode(chars, bytes, write: true, flush: true, ref high);
    }

    public override int GetCharCount(byte[] bytes, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return GetCharCount(new ReadOnlySpan<byte>(bytes, index, count));
    }

    public override int GetCharCount(ReadOnlySpan<byte> bytes)
    {
        int held = 0;
        return Decode(bytes, [], write: false, flush: true, stackalloc byte[Utf.UtfMax - 1], ref held);
    }

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(chars);
        return GetChars(new ReadOnlySpan<byte>(bytes, byteIndex, byteCount), chars.AsSpan(charIndex));
    }

    public override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
    {
        int held = 0;
        return Decode(bytes, chars, write: true, flush: true, stackalloc byte[Utf.UtfMax - 1], ref held);
    }

    // Each character is at most three bytes, as is a high surrogate an encoder holds.
    public override int GetMaxByteCount(int charCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(charCount);
        long most = (charCount + 1L) * 3;
        return most <= int.MaxValue ? (int)most : throw new ArgumentOutOfRangeException(nameof(charCount));
    }

    // Each byte is at most a character, as is each of the bytes a decoder holds.
    public override int GetMaxCharCount(int byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
        long most = byteCount + (Utf.UtfMax - 1L);
        return most <= int.MaxValue ? (int)most : throw new ArgumentOutOfRangeException(nameof(byteCount));
    }

    // Reads what is held and then the bytes, keeping in held, unless flushing, a sequence the
    // bytes end before chartorune can decide it.
    private static int Decode(ReadOnlySpan<byte> bytes, Span<char> chars, bool write, bool flush, Span<byte> held, ref int heldCount)
    {
        int written = 0;
        int i = 0;
        Span<byte> joined = stackalloc byte[Utf.UtfMax];
        while (heldCount > 0)
        {
            int taken = Math.Min(bytes.Length, Utf.UtfMax - heldCount);
            held[..heldCount].CopyTo(joined);
            bytes[..taken].CopyTo(joined[heldCount..]);
            ReadOnlySpan<byte> start = joined[..(heldCount + taken)];
            if (!flush && !Utf.FullRune(start))
            {
                bytes.CopyTo(held[heldCount..]);
                heldCount += bytes.Length;
                return written;
            }

            int n = Utf.CharToRune(out uint rune, start);
            written = Put(rune, chars, written, write);
            if (n >= heldCount)
            {
                i = n - heldCount;
                heldCount = 0;
            }
            else
            {
                held[n..heldCount].CopyTo(held);
                heldCount -= n;
            }
        }

        while (i < bytes.Length)
        {
            ReadOnlySpan<byte> rest = bytes[i..];
            if (!flush && !Utf.FullRune(rest))
            {
                rest.CopyTo(held);
                heldCount = rest.Length;
                break;
            }

            i += Utf.CharToRune(out uint rune, rest);
            written = Put(rune, chars, written, write);
        }

        return written;
    }

    private static int Put(uint rune, Span<char> chars, int at, bool write)
    {
        int length = rune > char.MaxValue ? 2 : 1;
        if (!write)
        {
            return at + length;
        }

        if (at + length > chars.Length)
        {
            throw new ArgumentException("The output buffer is too small for the characters.", nameof(chars));
        }

        if (length == 1)
        {
            chars[at] = (char)rune;
        }
        else
        {
            chars[at] = (char)(0xD800 + ((rune - 0x10000) >> 10));
            chars[at + 1] = (char)(0xDC00 + ((rune - 0x10000) & 0x3FF));
        }

        return at + length;
    }

    // Writes the characters, a high surrogate and the low one after it as the rune they pair to,
    // keeping in high, unless flushing, a high surrogate the characters end with.
    private static int Encode(ReadOnlySpan<char> chars, Span<byte> bytes, bool write, bool flush, ref char high)
    {
        int written = 0;
        foreach (char c in chars)
        {
            if (high != '\0')
            {
                if (char.IsLowSurrogate(c))
                {
                    written = Put((uint)char.ConvertToUtf32(high, c), bytes, written, write);
                    high = '\0';
                    continue;
                }

                written = Put(high, bytes, written, write);
                high = '\0';
            }

            if (char.IsHighSurrogate(c))
            {
                high = c;
            }
            else
            {
                written = Put(c, bytes, written, write);
            }
        }

        if (flush && high != '\0')
        {
            written = Put(high, bytes, written, write);
            high = '\0';
        }

        return written;
    }

    private static int Put(uint rune, Span<byte> bytes, int at, bool write)
    {
        int length = Utf.RuneLen((int)rune);
        if (!write)
        {
            return at + length;
        }

        if (at + length > bytes.Length)
        {
            throw new ArgumentException("The output buffer is too small for the bytes.", nameof(bytes));
        }

        return at + Utf.RuneToChar(bytes[at..], rune);
    }

    private sealed class Plan9Decoder : Decoder
    {
        private readonly byte[] held = new byte[Utf.UtfMax - 1];
        private int heldCount;

        public override void Reset() => heldCount = 0;

        public override int GetCharCount(byte[] bytes, int index, int count) => GetCharCount(bytes, index, count, flush: false);

        public override int GetCharCount(byte[] bytes, int index, int count, bool flush)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            return GetCharCount(new ReadOnlySpan<byte>(bytes, index, count), flush);
        }

        public override int GetCharCount(ReadOnlySpan<byte> bytes, bool flush)
        {
            Span<byte> copy = stackalloc byte[held.Length];
            held.CopyTo(copy);
            int count = heldCount;
            return Decode(bytes, [], write: false, flush, copy, ref count);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            => GetChars(bytes, byteIndex, byteCount, chars, charIndex, flush: false);

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentNullException.ThrowIfNull(chars);
            return GetChars(new ReadOnlySpan<byte>(bytes, byteIndex, byteCount), chars.AsSpan(charIndex), flush);
        }

        public override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool flush)
            => Decode(bytes, chars, write: true, flush, held, ref heldCount);
    }

    private sealed class Plan9Encoder : Encoder
    {
        private char high;

        public override void Reset() => high = '\0';

        public override int GetByteCount(char[] chars, int index, int count, bool flush)
        {
            ArgumentNullException.ThrowIfNull(chars);
            return GetByteCount(new ReadOnlySpan<char>(chars, index, count), flush);
        }

        public override int GetByteCount(ReadOnlySpan<char> chars, bool flush)
        {
            char copy = high;
            return Encode(chars, [], write: false, flush, ref copy);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
        {
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentNullException.ThrowIfNull(bytes);
            return GetBytes(new ReadOnlySpan<char>(chars, charIndex, charCount), bytes.AsSpan(byteIndex), flush);
        }

        public override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool flush)
            => Encode(chars, bytes, write: true, flush, ref high);
    }
}
