namespace Plan9.Utf;

// libc/port's rune.c and utf*.c. A run of bytes stands for a C string: it ends at its first NUL,
// and reading past its end reads the NUL. A Rune is 32 bits without a sign, and a long is an int,
// as both are in 9front; a place that is nil in C is -1 here.
public static class Utf
{
    public const int UtfMax = 4;
    public const int RuneSync = 0x80;
    public const int RuneSelf = 0x80;
    public const uint RuneError = 0xFFFD;
    public const uint RuneMax = 0x10FFFF;
    public const uint RuneMask = 0x1FFFFF;

    private const int Bit1 = 7;
    private const int Bitx = 6;
    private const int Bit2 = 5;
    private const int Bit3 = 4;
    private const int Bit4 = 3;
    private const int Bit5 = 2;

    private const int Tx = ((1 << (Bitx + 1)) - 1) ^ 0xFF;
    private const int T2 = ((1 << (Bit2 + 1)) - 1) ^ 0xFF;
    private const int T3 = ((1 << (Bit3 + 1)) - 1) ^ 0xFF;
    private const int T4 = ((1 << (Bit4 + 1)) - 1) ^ 0xFF;
    private const int T5 = ((1 << (Bit5 + 1)) - 1) ^ 0xFF;

    private const int Rune1 = (1 << (Bit1 + (0 * Bitx))) - 1;
    private const int Rune2 = (1 << (Bit2 + (1 * Bitx))) - 1;
    private const int Rune3 = (1 << (Bit3 + (2 * Bitx))) - 1;
    private const int Rune4 = (1 << (Bit4 + (3 * Bitx))) - 1;

    private const int Maskx = (1 << Bitx) - 1;
    private const int Testx = Maskx ^ 0xFF;

    public static int CharToRune(out uint rune, ReadOnlySpan<byte> str)
    {
        int c = At(str, 0);
        if (c < Tx)
        {
            rune = (uint)c;
            return 1;
        }

        int c1 = At(str, 1) ^ Tx;
        if ((c1 & Testx) != 0)
        {
            return Bad(out rune);
        }

        if (c < T3)
        {
            int l = ((c << Bitx) | c1) & Rune2;
            if (c < T2 || l <= Rune1)
            {
                return Bad(out rune);
            }

            rune = (uint)l;
            return 2;
        }

        int c2 = At(str, 2) ^ Tx;
        if ((c2 & Testx) != 0)
        {
            return Bad(out rune);
        }

        if (c < T4)
        {
            int l = ((((c << Bitx) | c1) << Bitx) | c2) & Rune3;
            if (l <= Rune2)
            {
                return Bad(out rune);
            }

            rune = (uint)l;
            return 3;
        }

        int c3 = At(str, 3) ^ Tx;
        if ((c3 & Testx) != 0)
        {
            return Bad(out rune);
        }

        if (c < T5)
        {
            int l = ((((((c << Bitx) | c1) << Bitx) | c2) << Bitx) | c3) & Rune4;
            if (l <= Rune3 || l > RuneMax)
            {
                return Bad(out rune);
            }

            rune = (uint)l;
            return 4;
        }

        return Bad(out rune);
    }

    // runetochar takes the rune as a long, so one with its top bit set is negative and only its
    // low byte is written.
    public static int RuneToChar(Span<byte> str, uint rune)
    {
        int c = (int)rune;
        if (c > RuneMax)
        {
            c = (int)RuneError;
        }

        if (c <= Rune1)
        {
            str[0] = (byte)c;
            return 1;
        }

        if (c <= Rune2)
        {
            str[0] = (byte)(T2 | (c >> (1 * Bitx)));
            str[1] = (byte)(Tx | (c & Maskx));
            return 2;
        }

        if (c <= Rune3)
        {
            str[0] = (byte)(T3 | (c >> (2 * Bitx)));
            str[1] = (byte)(Tx | ((c >> (1 * Bitx)) & Maskx));
            str[2] = (byte)(Tx | (c & Maskx));
            return 3;
        }

        str[0] = (byte)(T4 | (c >> (3 * Bitx)));
        str[1] = (byte)(Tx | ((c >> (2 * Bitx)) & Maskx));
        str[2] = (byte)(Tx | ((c >> (1 * Bitx)) & Maskx));
        str[3] = (byte)(Tx | (c & Maskx));
        return 4;
    }

    public static int RuneLen(int c) => RuneToChar(stackalloc byte[UtfMax], (uint)c);

    // runenlen holds each rune in an int.
    public static int RuneNLen(ReadOnlySpan<uint> runes)
    {
        int nb = 0;
        foreach (uint rune in runes)
        {
            int c = (int)rune;
            if (c <= Rune1)
            {
                nb++;
            }
            else if (c <= Rune2)
            {
                nb += 2;
            }
            else if (c <= Rune3 || c > RuneMax)
            {
                nb += 3;
            }
            else
            {
                nb += 4;
            }
        }

        return nb;
    }

    public static bool FullRune(ReadOnlySpan<byte> str)
    {
        int n = str.Length;
        if (n <= 0)
        {
            return false;
        }

        int c = str[0];
        if (c < Tx)
        {
            return true;
        }

        if (c < T3)
        {
            return n >= 2;
        }

        if (c < T4)
        {
            return n >= 3;
        }

        return n >= 4;
    }

    public static int UtfLen(ReadOnlySpan<byte> s)
    {
        int n = 0;
        int i = 0;
        while (true)
        {
            int c = At(s, i);
            if (c < RuneSelf)
            {
                if (c == 0)
                {
                    return n;
                }

                i++;
            }
            else
            {
                i += CharToRune(out _, s[i..]);
            }

            n++;
        }
    }

    // utfnlen, over all the bytes given.
    public static int UtfNLen(ReadOnlySpan<byte> s)
    {
        int n;
        int i = 0;
        for (n = 0; i < s.Length; n++)
        {
            int c = s[i];
            if (c < RuneSelf)
            {
                if (c == 0)
                {
                    break;
                }

                i++;
                continue;
            }

            if (!FullRune(s[i..]))
            {
                break;
            }

            i += CharToRune(out _, s[i..]);
        }

        return n;
    }

    public static int UtfRune(ReadOnlySpan<byte> s, int c)
    {
        if (c < RuneSync)
        {
            return StrChr(s, c);
        }

        Span<byte> buf = stackalloc byte[UtfMax];
        return StrStr(s, buf[..RuneToChar(buf, (uint)c)]);
    }

    public static int UtfRRune(ReadOnlySpan<byte> s, int c)
    {
        if (c < RuneSync)
        {
            return StrRChr(s, c);
        }

        Span<byte> buf = stackalloc byte[UtfMax];
        int n = RuneToChar(buf, (uint)c);
        int p = -1;
        for (int at = 0, found; (found = StrStr(s[at..], buf[..n])) >= 0; at = p + n)
        {
            p = at + found;
        }

        return p;
    }

    public static int UtfUtf(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2) => StrStr(s1, s2);

    // utfecpy into all of to, giving where it put the NUL; an empty to is left as it is.
    public static int UtfECpy(Span<byte> to, ReadOnlySpan<byte> from)
    {
        if (to.IsEmpty)
        {
            return 0;
        }

        // memccpy, up to and taking the NUL.
        for (int i = 0; i < to.Length; i++)
        {
            to[i] = (byte)At(from, i);
            if (to[i] == 0)
            {
                return i;
            }
        }

        int end = to.Length;
        while (end > 0 && (to[--end] & 0xC0) == 0x80)
        {
        }

        to[end] = 0;
        return end;
    }

    public static int UtfNCmp(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2, int n)
    {
        int i1 = 0;
        int i2 = 0;
        for (; n > 0; n--)
        {
            i1 += CharToRune(out uint r1, s1[i1..]);
            i2 += CharToRune(out uint r2, s2[i2..]);
            if (r1 != r2)
            {
                return r1 > r2 ? 1 : -1;
            }

            if (r1 == 0)
            {
                break;
            }
        }

        return 0;
    }

    private static int At(ReadOnlySpan<byte> s, int i) => i < s.Length ? s[i] : 0;

    private static int Bad(out uint rune)
    {
        rune = RuneError;
        return 1;
    }

    // amd64's strchr, which looks for c's low byte, so a c whose low byte is 0 finds the NUL.
    private static int StrChr(ReadOnlySpan<byte> s, int c)
    {
        byte b = (byte)c;
        for (int i = 0; ; i++)
        {
            int r = At(s, i);
            if (r == b)
            {
                return i;
            }

            if (r == 0)
            {
                return -1;
            }
        }
    }

    // strrchr. Where its c is not 0 but strchr finds the NUL, it would look on past the NUL; here
    // it is the NUL.
    private static int StrRChr(ReadOnlySpan<byte> s, int c)
    {
        if ((byte)c == 0)
        {
            return StrChr(s, 0);
        }

        int r = -1;
        for (int at = 0, found; (found = StrChr(s[at..], c)) >= 0; at = r + 1)
        {
            r = at + found;
        }

        return r;
    }

    private static int StrStr(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2)
    {
        int c0 = At(s2, 0);
        if (c0 == 0)
        {
            return 0;
        }

        for (int p = StrChr(s1, c0); p >= 0; p = Next(s1, p, c0))
        {
            int pa = p;
            for (int pb = 1; ; pb++)
            {
                int c = At(s2, pb);
                if (c == 0)
                {
                    return p;
                }

                if (c != At(s1, ++pa))
                {
                    break;
                }
            }
        }

        return -1;
    }

    // strchr(p+1, c0), as a place in all of s.
    private static int Next(ReadOnlySpan<byte> s, int p, int c0)
    {
        int found = StrChr(s[(p + 1)..], c0);
        return found < 0 ? -1 : p + 1 + found;
    }
}
