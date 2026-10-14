@strings
Feature: UTF-8 strings are counted, searched, copied and compared as 9front's libc does
  These are libc/port's utf functions over a run of bytes that stands for a C string: it ends at
  its first NUL, or at the end of the bytes, which reads as the NUL. utflen counts the runes up to
  the NUL as chartorune reads them, so each bad byte is a rune. utfnlen counts within the bytes it
  is given, leaving out a last rune they do not hold all of. utfrune and utfrrune find the first and
  last place a rune's UTF-8 is, by its bytes alone; a rune below Runesync is found as strchr and
  strrchr find a byte, which for the rune 0 is the NUL. A long below 0 is below Runesync too, and
  is found as amd64's strchr finds it, by its low byte, so one whose low byte is 0 is the NUL; for
  utfrrune too, where strrchr would go on looking past the NUL.
  utfutf
  finds one string in another, as strstr does. utfecpy copies a string into a buffer, NUL and all
  if it fits; if not, it ends the copy at the start of the last rune it has room for, which it
  leaves out to make room for the NUL, and gives where the NUL is. utfncmp compares the first runes
  of two strings as chartorune reads them, so a bad byte is equal to Runeerror's UTF-8.

  @UTF_005
  Scenario Outline: utflen counts the runes up to the NUL
    When utflen counts "<bytes>"
    Then the count is <count>

    Examples:
      | bytes                   | count |
      |                         | 0     |
      | 00 41                   | 0     |
      | 61 62 63                | 3     |
      | e2 82 ac 61             | 2     |
      | 61 00 62                | 1     |
      | e2 82                   | 2     |
      | e2 82 00 41             | 2     |
      | 80 80 80                | 3     |
      | ed a0 80 ed b0 80       | 2     |
      | f0 9f 98 80             | 1     |
      | f8 88 80 80 80          | 5     |

  @UTF_006
  Scenario Outline: utfnlen counts the runes the bytes hold all of, up to a NUL
    When utfnlen counts "<bytes>"
    Then the count is <count>

    Examples:
      | bytes             | count |
      |                   | 0     |
      | 61 62 63          | 3     |
      | e2 82 ac 61       | 2     |
      | e2 82 ac          | 1     |
      | 61 e2 82          | 1     |
      | e2 82             | 0     |
      | 61 00 62          | 1     |
      | 80 80             | 1     |
      | 80 80 80          | 2     |
      | 80 80 80 80       | 3     |
      | c0 80             | 1     |
      | c0 80 41          | 3     |
      | f0 9f 98 80       | 1     |
      | f0 9f 98          | 0     |

  @UTF_007
  Scenario Outline: utfrune and utfrrune find the first and last of a rune
    When <call> looks for <rune> in "<bytes>"
    Then it is found at <place>

    Examples:
      | call     | rune    | bytes                   | place   |
      | utfrune  | 0x61    | 62 61 63 61             | 1       |
      | utfrrune | 0x61    | 62 61 63 61             | 3       |
      | utfrune  | 0x64    | 62 61 63 61             | nowhere |
      | utfrrune | 0x64    | 62 61 63 61             | nowhere |
      | utfrune  | 0x0     | 62 61 63                | 3       |
      | utfrrune | 0x0     | 62 61 00 63             | 2       |
      | utfrune  | 0x63    | 62 61 00 63             | nowhere |
      | utfrrune | 0x63    | 62 61 00 63             | nowhere |
      | utfrune  | -1      | 61 ff 62                | 1       |
      | utfrrune | -62     | 41 c2 41                | 1       |
      | utfrune  | -191    | 62 41 41                | 1       |
      | utfrrune | -191    | 62 41 41                | 2       |
      | utfrune  | -256    | 62 61 63                | 3       |
      | utfrrune | -256    | 62 61 00 63             | 2       |
      | utfrune  | 0xFF    | 61 ff 62                | nowhere |
      | utfrune  | 0x20AC  | 61 e2 82 ac 62 e2 82 ac | 1       |
      | utfrrune | 0x20AC  | 61 e2 82 ac 62 e2 82 ac | 5       |
      | utfrrune | 0x20AC  | e2 82 ac                | 0       |
      | utfrune  | 0x20AC  | 61 e2 82 ad             | nowhere |
      | utfrrune | 0x20AC  | 61 e2 82 ad             | nowhere |
      | utfrune  | 0x20AC  | 61 00 e2 82 ac          | nowhere |
      | utfrrune | 0x20AC  | e2 82 ac 00 e2 82 ac    | 0       |
      | utfrune  | 0x80    | e2 82 c2 80             | 2       |
      | utfrune  | 0xD800  | 61 ed a0 80             | 1       |
      | utfrune  | 0x110000 | 61 ef bf bd            | 1       |
      | utfrrune | 0xAA    | c2 aa c2 aa c2 aa       | 4       |
      | utfrrune | 0x10000 | f0 90 80 80 f0 90 80 80 | 4       |

  @UTF_008
  Scenario Outline: utfutf finds a string within another, as strstr does
    When utfutf looks for "<needle>" in "<bytes>"
    Then it is found at <place>

    Examples:
      | needle   | bytes             | place   |
      |          | 61 62             | 0       |
      | 62       | 61 62 63          | 1       |
      | 62 63    | 61 62 63 62 63    | 1       |
      | 63 64    | 61 62 63          | nowhere |
      | 62 00 61 | 61 62 63          | 1       |
      | 63       | 61 00 63          | nowhere |
      | 82 ac    | e2 82 ac          | 1       |
      | 61 62 63 | 61 62             | nowhere |

  @UTF_009
  Scenario Outline: utfecpy copies a string into a buffer, leaving out the last rune it has room for when the NUL does not fit
    Given a buffer of <size> bytes holding "<before>"
    When utfecpy copies "<bytes>" into it
    Then the NUL is at <end> and the buffer holds "<after>"

    Examples:
      | size | before      | bytes          | end | after             |
      | 0    |             | 61 62          | 0   |                   |
      | 4    | 2e 2e 2e 2e | 61 62          | 2   | 61 62 00 2e       |
      | 3    | 2e 2e 2e    | 61 62          | 2   | 61 62 00          |
      | 2    | 2e 2e       | 61 62          | 1   | 61 00             |
      | 1    | 2e          | 61 62          | 0   | 00                |
      | 4    | 2e 2e 2e 2e | 61 62 63 64 65 | 3   | 61 62 63 00       |
      | 5    | 2e 2e 2e 2e 2e | 61 62 e2 82 ac | 2 | 61 62 00 82 ac    |
      | 6    | 2e 2e 2e 2e 2e 2e | 61 62 e2 82 ac | 5 | 61 62 e2 82 ac 00 |
      | 4    | 2e 2e 2e 2e | 61 62 e2 82 ac | 2   | 61 62 00 82       |
      | 3    | 2e 2e 2e    | 61 62 e2 82 ac | 2   | 61 62 00          |
      | 3    | 2e 2e 2e    | 80 80 80 80    | 0   | 00 80 80          |
      | 4    | 2e 2e 2e 2e | 61 00 62 63 64 | 1   | 61 00 2e 2e       |
      | 4    | 2e 2e 2e 2e | 61 c2 c2 c2 c2 | 3   | 61 c2 c2 00       |

  @UTF_010
  Scenario Outline: utfncmp compares the first runes of two strings
    When utfncmp compares "<first>" with "<second>" for <n> runes
    Then the comparison is <result>

    Examples:
      | first       | second      | n  | result |
      | 61 62 63    | 61 62 64    | 3  | -1     |
      | 61 62 64    | 61 62 63    | 3  | 1      |
      | 61 62 63    | 61 62 64    | 2  | 0      |
      | 61 62 63    | 61 62 64    | 0  | 0      |
      | 61 62 63    | 61 62 64    | -1 | 0      |
      |             |             | 5  | 0      |
      | 61 62       | 61 62 63    | 5  | -1     |
      | 61 62 63    | 61 62       | 5  | 1      |
      | 61 00 63    | 61 00 64    | 5  | 0      |
      | e2 82 ac    | e2 82 ad    | 1  | -1     |
      | f0 90 80 80 | ef bf bf    | 1  | 1      |
      | 80          | ef bf bd    | 1  | 0      |
      | 80 61       | ef bf bd 61 | 2  | 0      |
      | c0 80       | ef bf bd 80 | 2  | 0      |
