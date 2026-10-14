# Plan9.Utf

9front's UTF-8 for .NET: `libc/port`'s rune and utf functions, and a `System.Text.Encoding` that
reads and writes text the way Plan 9 does.

.NET counts a string in UTF-16 code units and replaces bad UTF-8 by the Unicode rules. Plan 9
counts in bytes or runes, reads each bad byte as one `Runeerror`, and takes a surrogate as a
rune like any other:

| Bytes | Plan 9 | .NET's UTF-8 |
| --- | --- | --- |
| `e2 82` (cut short) | U+FFFD U+FFFD | U+FFFD |
| `f0 9f 98` (cut short) | U+FFFD U+FFFD U+FFFD | U+FFFD |
| `ed a0 80` (U+D800) | U+D800 | U+FFFD U+FFFD U+FFFD |

| Text | .NET `Length` | Plan 9 bytes | Plan 9 runes |
| --- | --- | --- | --- |
| `😀` | 2 | 4 | 1 |
| `€` | 1 | 3 | 1 |

## Use

```sh
dotnet add package Plan9.Utf
```

```csharp
using Plan9.Utf;

string text = Plan9Utf8Encoding.Instance.GetString(bytes);
byte[] written = Plan9Utf8Encoding.Instance.GetBytes(text);
int runes = Utf.UtfLen(written);
```

`Utf` holds `CharToRune`, `RuneToChar`, `RuneLen`, `RuneNLen`, `FullRune`, `UtfLen`, `UtfNLen`,
`UtfRune`, `UtfRRune`, `UtfUtf`, `UtfECpy` and `UtfNCmp`. Each works on a span of bytes that stands
for a C string: it ends at its first NUL, and reading past its end reads the NUL. Runes are `uint`,
as 9front's `Rune` is, and a C `long` is an `int`, as it is in 9front on every machine. A pointer
that would be nil is -1.

`Plan9Utf8Encoding` decodes as `chartorune` and encodes as `runetochar`, with a decoder that holds
a sequence split between calls and an encoder that holds a high surrogate. Its bytes are counted
text, so a NUL is a character. A high surrogate's bytes followed by a low one's come back as a
UTF-16 pair, which is written as the rune it stands for.

## Runes in spans

UTF-16 cannot hold every run of runes, so runes can be held in spans of `uint`, as 9front holds
them in `Rune` arrays. `ToRunes` reads every byte as `chartorune` does, `ToUtf` writes every rune
as `runetochar` does, and `CountRunes` and `RuneNLen` give the sizes. Runes go through UTF-8 and
back as they were, surrogates and all, except those `runetochar` does not write as themselves:
one above `Runemax` is written as `Runeerror`, and one with its top bit set as its low byte.

```csharp
uint[] runes = new uint[Utf.CountRunes(bytes)];
Utf.ToRunes(bytes, runes);
byte[] back = new byte[Utf.RuneNLen(runes)];
Utf.ToUtf(runes, back);
```

`RuneStrLen`, `RuneStrCmp`, `RuneStrNCmp`, `RuneStrChr`, `RuneStrRChr`, `RuneStrStr`,
`RuneStrECpy`, `RuneStrCpy`, `RuneStrNCpy`, `RuneStrCat`, `RuneStrNCat` and `RuneStrDup` are
libc's `runestr` functions over such spans, which end at their first 0 as C strings do. Where the
C would write past the end of a buffer, the call is refused and nothing is written.

## Where it differs from 9front

- `utfrune` and `utfrrune` find a rune below `Runesync` by its low byte, as amd64's `strchr` does;
  the portable C `strchr` compares a signed `char`, so the two differ for a long below -128.
- Where `strrchr` would read on past the NUL, for a long below 0 whose low byte is 0, `UtfRRune`
  gives the NUL.

## Build and test

```sh
dotnet test
bash scripts/run-quality.sh
```

`run-quality.sh` builds with warnings as errors, runs the tests and packs the package into
`.artifacts/package`.

The features in `Plan9.Utf.Tests/Features` give the behaviour, row by row, against the C.
