namespace Plan9.Utf;

// Runes held in spans as 9front holds them in Rune arrays, and libc/port's runestr*.c over them.
// A span of runes stands for a C string as a span of bytes does: it ends at its first 0, and
// reading past its end reads the 0. Where the C would write past the end of a buffer, nothing is
// written and the call is refused.
public static partial class Utf
{
    // Every byte, as chartorune reads it: a NUL is the rune 0, and a sequence the end cuts short is bad.
    public static int CountRunes(ReadOnlySpan<byte> utf)
    {
        int n = 0;
        for (int i = 0; i < utf.Length; n++)
        {
            i += CharToRune(out _, utf[i..]);
        }

        return n;
    }

    public static int ToRunes(ReadOnlySpan<byte> utf, Span<uint> runes)
    {
        if (CountRunes(utf) > runes.Length)
        {
            throw new ArgumentException("The span is too small for the runes.", nameof(runes));
        }

        int n = 0;
        for (int i = 0; i < utf.Length; n++)
        {
            i += CharToRune(out runes[n], utf[i..]);
        }

        return n;
    }

    // Every rune, as runetochar writes it, in as many bytes as runenlen counts.
    public static int ToUtf(ReadOnlySpan<uint> runes, Span<byte> utf)
    {
        if (RuneNLen(runes) > utf.Length)
        {
            throw new ArgumentException("The span is too small for the bytes.", nameof(utf));
        }

        int at = 0;
        foreach (uint rune in runes)
        {
            at += RuneToChar(utf[at..], rune);
        }

        return at;
    }

    public static int RuneStrLen(ReadOnlySpan<uint> s) => RuneStrChr(s, 0);

    public static int RuneStrCmp(ReadOnlySpan<uint> s1, ReadOnlySpan<uint> s2)
    {
        for (int i = 0; ; i++)
        {
            uint c1 = At(s1, i);
            uint c2 = At(s2, i);
            if (c1 != c2)
            {
                return Math.Sign(c1.CompareTo(c2));
            }

            if (c1 == 0)
            {
                return 0;
            }
        }
    }

    public static int RuneStrNCmp(ReadOnlySpan<uint> s1, ReadOnlySpan<uint> s2, int n)
    {
        for (int i = 0; n > 0; i++)
        {
            uint c1 = At(s1, i);
            uint c2 = At(s2, i);
            n--;
            if (c1 != c2)
            {
                return Math.Sign(c1.CompareTo(c2));
            }

            if (c1 == 0)
            {
                break;
            }
        }

        return 0;
    }

    public static int RuneStrChr(ReadOnlySpan<uint> s, uint c)
    {
        for (int i = 0; ; i++)
        {
            uint r = At(s, i);
            if (r == c)
            {
                return i;
            }

            if (r == 0)
            {
                return -1;
            }
        }
    }

    public static int RuneStrRChr(ReadOnlySpan<uint> s, uint c)
    {
        if (c == 0)
        {
            return RuneStrChr(s, 0);
        }

        int r = -1;
        for (int at = 0, found; (found = RuneStrChr(s[at..], c)) >= 0; at = r + 1)
        {
            r = at + found;
        }

        return r;
    }

    public static int RuneStrStr(ReadOnlySpan<uint> s1, ReadOnlySpan<uint> s2)
    {
        uint c0 = At(s2, 0);
        if (c0 == 0)
        {
            return 0;
        }

        for (int p = RuneStrChr(s1, c0); p >= 0; p = NextRune(s1, p, c0))
        {
            int pa = p;
            for (int pb = 1; ; pb++)
            {
                uint c = At(s2, pb);
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

    // runestrecpy into all of s1, giving where it put the 0; an empty s1 is left as it is.
    public static int RuneStrECpy(Span<uint> s1, ReadOnlySpan<uint> s2)
    {
        if (s1.IsEmpty)
        {
            return 0;
        }

        for (int i = 0; ; i++)
        {
            s1[i] = At(s2, i);
            if (s1[i] == 0)
            {
                return i;
            }

            if (i + 1 == s1.Length)
            {
                s1[i] = 0;
                return i;
            }
        }
    }

    public static void RuneStrCpy(Span<uint> s1, ReadOnlySpan<uint> s2)
    {
        int length = RuneStrLen(s2);
        Room(s1, length + 1);
        s2[..length].CopyTo(s1);
        s1[length] = 0;
    }

    // runestrncpy writes n runes: s2's, then 0s once it has copied s2's 0.
    public static void RuneStrNCpy(Span<uint> s1, ReadOnlySpan<uint> s2, int n)
    {
        Room(s1, n);
        for (int i = 0; i < n; i++)
        {
            s1[i] = At(s2, i);
            if (s1[i] == 0)
            {
                s1[i..n].Clear();
                return;
            }
        }
    }

    public static void RuneStrCat(Span<uint> s1, ReadOnlySpan<uint> s2)
    {
        RuneStrCpy(s1[RuneStrLen(s1)..], s2);
    }

    // runestrncat copies s2 after s1 until it has copied s2's 0, or n runes and one more, which it
    // makes the 0.
    public static void RuneStrNCat(Span<uint> s1, ReadOnlySpan<uint> s2, int n)
    {
        int end = RuneStrLen(s1);
        Room(s1, end + Math.Min(RuneStrLen(s2), Math.Max(n, 0)) + 1);
        Span<uint> to = s1[end..];
        for (int i = 0; ; i++)
        {
            to[i] = At(s2, i);
            if (to[i] == 0)
            {
                return;
            }

            if (--n < 0)
            {
                to[i] = 0;
                return;
            }
        }
    }

    public static uint[] RuneStrDup(ReadOnlySpan<uint> s)
    {
        uint[] copy = new uint[RuneStrLen(s) + 1];
        RuneStrCpy(copy, s);
        return copy;
    }

    private static uint At(ReadOnlySpan<uint> s, int i) => i < s.Length ? s[i] : 0;

    private static void Room(Span<uint> s1, int runes)
    {
        if (runes > s1.Length)
        {
            throw new ArgumentException("The buffer is too small for the runes.", nameof(s1));
        }
    }

    // runestrchr(p+1, c0), as a place in all of s.
    private static int NextRune(ReadOnlySpan<uint> s, int p, uint c0)
    {
        int found = RuneStrChr(s[(p + 1)..], c0);
        return found < 0 ? -1 : p + 1 + found;
    }
}
