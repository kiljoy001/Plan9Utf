@runes
Feature: Runes go to and from UTF-8 as 9front's libc does
  chartorune reads the rune a run of bytes starts with, as libc/port/rune.c does. A byte that does
  not start a well-formed sequence, a sequence with a byte that does not continue it, one that is
  longer than its rune needs, and one above Runemax are each a single byte of Runeerror. A
  surrogate is a rune like any other, since chartorune does not check for them. The end of the
  bytes reads as the NUL that ends a C string, so a sequence cut short there is a bad one and an
  empty run is the rune 0. runetochar writes a rune as UTF-8 and anything above Runemax as
  Runeerror. Runes are 32 bits without a sign, as 9front's Rune is, and a long is 32 bits, as it
  is in 9front on every machine, so runetochar, which takes the rune as a long, finds one with its
  top bit set negative and writes its low byte alone, as 9front does. runelen counts the bytes
  runetochar writes for a long. runenlen counts those of a run of runes in an int, so it counts a
  rune with its top bit set as one byte and any other above Runemax as three. fullrune says
  whether bytes hold enough to decide a whole rune.

  @UTF_001
  Scenario Outline: chartorune reads the rune the bytes start with and how many bytes it took
    When chartorune reads "<bytes>"
    Then the rune is <rune> and it took <length> bytes

    Examples:
      | bytes          | rune     | length |
      | 41             | 0x41     | 1      |
      | 00             | 0x0      | 1      |
      |                | 0x0      | 1      |
      | 7f 80          | 0x7F     | 1      |
      | c2 80          | 0x80     | 2      |
      | df bf 41       | 0x7FF    | 2      |
      | e0 a0 80       | 0x800    | 3      |
      | e2 82 ac       | 0x20AC   | 3      |
      | ed a0 80       | 0xD800   | 3      |
      | ed bf bf       | 0xDFFF   | 3      |
      | ef bf bf       | 0xFFFF   | 3      |
      | f0 90 80 80    | 0x10000  | 4      |
      | f4 8f bf bf    | 0x10FFFF | 4      |
      | 80             | 0xFFFD   | 1      |
      | bf 80          | 0xFFFD   | 1      |
      | c0 80          | 0xFFFD   | 1      |
      | c1 bf          | 0xFFFD   | 1      |
      | c2 41          | 0xFFFD   | 1      |
      | c2 c0          | 0xFFFD   | 1      |
      | c2             | 0xFFFD   | 1      |
      | e0 80 80       | 0xFFFD   | 1      |
      | e0 9f bf       | 0xFFFD   | 1      |
      | e2 82          | 0xFFFD   | 1      |
      | e2 82 41       | 0xFFFD   | 1      |
      | e2 28 a1       | 0xFFFD   | 1      |
      | f0 8f bf bf    | 0xFFFD   | 1      |
      | f4 90 80 80    | 0xFFFD   | 1      |
      | f7 bf bf bf    | 0xFFFD   | 1      |
      | f0 90 80       | 0xFFFD   | 1      |
      | f0 90 80 41    | 0xFFFD   | 1      |
      | f8 88 80 80 80 | 0xFFFD   | 1      |
      | fe             | 0xFFFD   | 1      |
      | ff             | 0xFFFD   | 1      |
      | ef bf bd       | 0xFFFD   | 3      |

  @UTF_002
  Scenario Outline: runetochar writes a rune as UTF-8, anything else up to 0x7FFFFFFF as Runeerror, and the low byte of the rest
    When runetochar writes <rune>
    Then the bytes are "<bytes>"

    Examples:
      | rune       | bytes       |
      | 0x0        | 00          |
      | 0x41       | 41          |
      | 0x7F       | 7f          |
      | 0x80       | c2 80       |
      | 0x7FF      | df bf       |
      | 0x800      | e0 a0 80    |
      | 0x20AC     | e2 82 ac    |
      | 0xD800     | ed a0 80    |
      | 0xFFFF     | ef bf bf    |
      | 0x10000    | f0 90 80 80 |
      | 0x10FFFF   | f4 8f bf bf |
      | 0x110000   | ef bf bd    |
      | 0x7FFFFFFF | ef bf bd    |
      | 0x80000000 | 00          |
      | 0x800020AC | ac          |
      | 0xFFFFFFFF | ff          |

  @UTF_002
  Scenario Outline: Every rune runetochar writes is read back by chartorune
    When runetochar writes <rune>
    Then chartorune reads it back as <rune>

    Examples:
      | rune     |
      | 0x0      |
      | 0x7F     |
      | 0x80     |
      | 0x7FF    |
      | 0x800    |
      | 0xDBFF   |
      | 0xFFFF   |
      | 0x10000  |
      | 0x10FFFF |

  @UTF_003
  Scenario Outline: runelen counts the bytes runetochar writes for a long
    When runelen counts <value>
    Then the count is <count>

    Examples:
      | value     | count |
      | 0         | 1     |
      | 127       | 1     |
      | 128       | 2     |
      | 2047      | 2     |
      | 2048      | 3     |
      | 65535     | 3     |
      | 65536     | 4     |
      | 1114111   | 4     |
      | 1114112   | 3     |
      | 2147483647  | 3     |
      | -2147483648 | 1     |
      | -1          | 1     |

  @UTF_003
  Scenario Outline: runenlen counts the bytes of a run of runes, three for one above Runemax and one with its top bit set
    When runenlen counts the runes "<runes>"
    Then the count is <count>

    Examples:
      | runes                                     | count |
      |                                           | 0     |
      | 0x41                                      | 1     |
      | 0x0 0x0                                   | 2     |
      | 0x7F 0x80 0x7FF 0x800                     | 8     |
      | 0xFFFF 0x10000 0x10FFFF 0x110000          | 14    |
      | 0x110000 0x7FFFFFFF                       | 6     |
      | 0x80000000 0xFFFFFFFF                     | 2     |

  @UTF_004
  Scenario Outline: fullrune says whether the bytes hold enough for chartorune to decide a rune
    When fullrune looks at "<bytes>"
    Then it is <answer>

    Examples:
      | bytes          | answer |
      |                | false  |
      | 41             | true   |
      | 00             | true   |
      | 80             | false  |
      | 80 80          | true   |
      | c2             | false  |
      | c2 80          | true   |
      | df 41          | true   |
      | e0             | false  |
      | e2 82          | false  |
      | e2 82 ac       | true   |
      | ef 41 41       | true   |
      | f0 90 80       | false  |
      | f0 90 80 80    | true   |
      | ff 80 80       | false  |
      | ff 80 80 80    | true   |
