@runestrings
Feature: Runes are held in spans, as 9front holds them in Rune arrays, and worked on as runestr does
  UTF-16 cannot hold everything a run of runes can: the runes of a high surrogate and then a low one
  are a pair in a .NET string, and a pair is written as the rune it stands for. A span of runes holds
  them as 9front's Rune arrays do. ToRunes reads every byte of a span of UTF-8 as chartorune reads
  it, a NUL as the rune 0 and a sequence cut short at the end as bad bytes, and ToUtf writes every
  rune as runetochar writes it, so its bytes are counted as runenlen counts them. Runes come back
  from UTF-8 as they went, surrogates and all, except those runetochar does not write as themselves.
  Either refuses a span too small for what it would write. The runestr functions of libc/port work
  on a span of runes that stands for a C string: it ends at its first 0, and reading past its end
  reads the 0. Runes compare as unsigned 32-bit numbers. runestrchr and runestrrchr find a rune, the
  rune 0 at the end; runestrstr finds a string of runes; a place that is nil in C is -1. runestrecpy
  copies into a buffer as much as fits before its 0 and gives where the 0 is. runestrcpy,
  runestrncpy, runestrcat and runestrncat write as the C does, and refuse a buffer the C would write
  past the end of. runestrdup copies a string, 0 and all, into a new array.

  @UTF_018
  Scenario Outline: ToRunes reads every byte as chartorune does, and ToUtf writes the runes back
    When ToRunes reads "<bytes>"
    Then the runes are "<runes>"
    And ToUtf writes them as "<back>"

    Examples:
      | bytes             | runes               | back              |
      |                   |                     |                   |
      | 61 00 62          | 0x61 0x0 0x62       | 61 00 62          |
      | e2 82 ac          | 0x20AC              | e2 82 ac          |
      | f0 9f 98 80       | 0x1F600             | f0 9f 98 80       |
      | ed a0 80 ed b0 80 | 0xD800 0xDC00       | ed a0 80 ed b0 80 |
      | ed b0 80 ed a0 80 | 0xDC00 0xD800       | ed b0 80 ed a0 80 |
      | e2 82             | 0xFFFD 0xFFFD       | ef bf bd ef bf bd |
      | 80 41             | 0xFFFD 0x41         | ef bf bd 41       |
      | f4 90 80 80       | 0xFFFD 0xFFFD 0xFFFD 0xFFFD | ef bf bd ef bf bd ef bf bd ef bf bd |

  @UTF_018
  Scenario Outline: Runes go through UTF-8 and back as they were, but for those runetochar does not write as themselves
    When ToUtf writes the runes "<runes>"
    Then the bytes are "<bytes>"
    And the count of them is runenlen's
    And ToRunes reads them back as "<back>"

    Examples:
      | runes                          | bytes                      | back                           |
      | 0x41 0xD800 0xDC00 0x0         | 41 ed a0 80 ed b0 80 00    | 0x41 0xD800 0xDC00 0x0         |
      | 0xDBFF 0xDFFF 0x10FFFF         | ed af bf ed bf bf f4 8f bf bf | 0xDBFF 0xDFFF 0x10FFFF      |
      | 0x110000 0x7FFFFFFF            | ef bf bd ef bf bd          | 0xFFFD 0xFFFD                  |
      | 0xFFFFFFFF 0x800020AC          | ff ac                      | 0xFFFD 0xFFFD                  |

  @UTF_018
  Scenario Outline: The count of runes is that of every byte, NUL and all
    When the runes of "<bytes>" are counted
    Then the count is <count>

    Examples:
      | bytes             | count |
      |                   | 0     |
      | 61 00 62          | 3     |
      | ed a0 80 ed b0 80 | 2     |
      | e2 82             | 2     |
      | f0 9f 98 80 41    | 2     |

  @UTF_018
  Scenario Outline: A span too small for what would be written is refused
    When <call> into a span of <size>
    Then it is refused with "<message>"

    Examples:
      | call                                | size | message                                                    |
      | ToRunes reads "61 62"               | 1    | The span is too small for the runes. (Parameter 'runes')   |
      | ToRunes reads "e2 82"               | 1    | The span is too small for the runes. (Parameter 'runes')   |
      | ToUtf writes the runes "0x41 0x42"  | 1    | The span is too small for the bytes. (Parameter 'utf')     |
      | ToUtf writes the runes "0x20AC"     | 2    | The span is too small for the bytes. (Parameter 'utf')     |

  @UTF_019
  Scenario Outline: runestrlen counts the runes before the 0
    When runestrlen counts "<runes>"
    Then the count is <count>

    Examples:
      | runes           | count |
      |                 | 0     |
      | 0x0             | 0     |
      | 0x61 0x62       | 2     |
      | 0x61 0x0 0x62   | 1     |
      | 0xD800 0xDC00   | 2     |

  @UTF_019
  Scenario Outline: runestrcmp and runestrncmp compare runes as unsigned numbers, up to the 0
    When <call> compares "<first>" with "<second>"
    Then the comparison is <result>

    Examples:
      | call               | first          | second         | result |
      | runestrcmp         | 0x61 0x62      | 0x61 0x62      | 0      |
      | runestrcmp         | 0x61 0x62      | 0x61 0x63      | -1     |
      | runestrcmp         | 0x61 0x63      | 0x61 0x62      | 1      |
      | runestrcmp         | 0x61           | 0x61 0x62      | -1     |
      | runestrcmp         | 0x61 0x62      | 0x61           | 1      |
      | runestrcmp         |                |                | 0      |
      | runestrcmp         | 0x61 0x0 0x62  | 0x61 0x0 0x63  | 0      |
      | runestrcmp         | 0xFFFFFFFF     | 0x1            | 1      |
      | runestrncmp 2 runes  | 0x61 0x62 0x63 | 0x61 0x62 0x64 | 0      |
      | runestrncmp 3 runes  | 0x61 0x62 0x63 | 0x61 0x62 0x64 | -1     |
      | runestrncmp 3 runes  | 0x61 0x62 0x64 | 0x61 0x62 0x63 | 1      |
      | runestrncmp 0 runes  | 0x61           | 0x62           | 0      |
      | runestrncmp -1 runes | 0x61           | 0x62           | 0      |
      | runestrncmp 5 runes  | 0x61 0x0 0x62  | 0x61 0x0 0x63  | 0      |
      | runestrncmp 5 runes  | 0x61           | 0x61 0x62      | -1     |
      | runestrncmp 1 runes  | 0xFFFFFFFF     | 0x1            | 1      |

  @UTF_020
  Scenario Outline: runestrchr and runestrrchr find the first and last of a rune, and runestrstr a string of them
    When <call> looks for "<needle>" in "<runes>"
    Then it is found at <place>

    Examples:
      | call        | needle           | runes                        | place   |
      | runestrchr  | 0x62             | 0x61 0x62 0x62               | 1       |
      | runestrrchr | 0x62             | 0x61 0x62 0x62               | 2       |
      | runestrchr  | 0x63             | 0x61 0x62 0x62               | nowhere |
      | runestrrchr | 0x63             | 0x61 0x62 0x62               | nowhere |
      | runestrchr  | 0x0              | 0x61 0x62                    | 2       |
      | runestrrchr | 0x0              | 0x61 0x0 0x62                | 1       |
      | runestrchr  | 0x62             | 0x61 0x0 0x62                | nowhere |
      | runestrrchr | 0x62             | 0x61 0x0 0x62                | nowhere |
      | runestrchr  | 0xD800           | 0xDC00 0xD800                | 1       |
      | runestrrchr | 0xFFFFFFFF       | 0xFFFFFFFF 0x61 0xFFFFFFFF   | 2       |
      | runestrstr  |                  | 0x61 0x62                    | 0       |
      | runestrstr  | 0x62 0x63        | 0x61 0x62 0x63               | 1       |
      | runestrstr  | 0x62 0x63        | 0x62 0x61 0x62 0x63          | 2       |
      | runestrstr  | 0x61 0x62        | 0x61 0x61 0x62               | 1       |
      | runestrstr  | 0x63 0x64        | 0x61 0x62 0x63               | nowhere |
      | runestrstr  | 0x61 0x62 0x63   | 0x61 0x62                    | nowhere |
      | runestrstr  | 0x62 0x0 0x61    | 0x61 0x62 0x63               | 1       |
      | runestrstr  | 0x63             | 0x61 0x0 0x63                | nowhere |
      | runestrstr  | 0xFFFFFFFF 0x61  | 0x62 0xFFFFFFFF 0x61         | 1       |

  @UTF_021
  Scenario Outline: runestrecpy copies what fits before the 0 into a buffer and gives where the 0 is
    Given a buffer of runes "<before>"
    When runestrecpy copies "<runes>" into it
    Then the 0 is at <end> and the buffer holds "<after>"

    Examples:
      | before          | runes           | end | after               |
      |                 | 0x61 0x62       | 0   |                     |
      | 0x2E 0x2E 0x2E  | 0x61 0x62       | 2   | 0x61 0x62 0x0       |
      | 0x2E 0x2E 0x2E 0x2E | 0x61 0x62   | 2   | 0x61 0x62 0x0 0x2E  |
      | 0x2E 0x2E       | 0x61 0x62       | 1   | 0x61 0x0            |
      | 0x2E            | 0x61            | 0   | 0x0                 |
      | 0x2E 0x2E       |                 | 0   | 0x0 0x2E            |
      | 0x2E 0x2E 0x2E  | 0x61 0x0 0x62   | 1   | 0x61 0x0 0x2E       |

  @UTF_021
  Scenario Outline: runestrcpy, runestrncpy, runestrcat and runestrncat write as the C does
    Given a buffer of runes "<before>"
    When <call> "<runes>" into it
    Then the buffer holds "<after>"

    Examples:
      | before                  | call                    | runes          | after                   |
      | 0x2E 0x2E 0x2E 0x2E     | runestrcpy copies       | 0x61 0x62      | 0x61 0x62 0x0 0x2E      |
      | 0x2E 0x2E 0x2E          | runestrcpy copies       | 0x61 0x0 0x62  | 0x61 0x0 0x2E           |
      | 0x2E 0x2E 0x2E 0x2E     | runestrncpy copies 4 of | 0x61 0x62      | 0x61 0x62 0x0 0x0       |
      | 0x2E 0x2E 0x2E 0x2E     | runestrncpy copies 1 of | 0x61 0x62      | 0x61 0x2E 0x2E 0x2E     |
      | 0x2E 0x2E 0x2E 0x2E     | runestrncpy copies 3 of | 0x61 0x0 0x63  | 0x61 0x0 0x0 0x2E       |
      | 0x2E 0x2E               | runestrncpy copies 0 of | 0x61           | 0x2E 0x2E               |
      | 0x2E 0x2E               | runestrncpy copies -1 of | 0x61          | 0x2E 0x2E               |
      | 0x61 0x0 0x2E 0x2E      | runestrcat adds         | 0x62           | 0x61 0x62 0x0 0x2E      |
      | 0x61 0x0 0x2E 0x2E      | runestrcat adds         | 0x62 0x63      | 0x61 0x62 0x63 0x0      |
      | 0x61 0x0 0x2E 0x2E      | runestrncat adds 2 of   | 0x62 0x63 0x64 | 0x61 0x62 0x63 0x0      |
      | 0x61 0x0 0x2E 0x2E      | runestrncat adds 5 of   | 0x62           | 0x61 0x62 0x0 0x2E      |
      | 0x61 0x0 0x2E 0x2E      | runestrncat adds 0 of   | 0x62 0x63      | 0x61 0x0 0x2E 0x2E      |
      | 0x61 0x0 0x2E 0x2E      | runestrncat adds -1 of  | 0x62 0x63      | 0x61 0x0 0x2E 0x2E      |

  @UTF_021
  Scenario Outline: A buffer the C would write past the end of is refused, and left as it was
    Given a buffer of runes "<before>"
    When <call> "<runes>" into it
    Then it is refused with "The buffer is too small for the runes. (Parameter 's1')", and the buffer holds "<before>"

    Examples:
      | before                  | call                    | runes          |
      | 0x2E 0x2E               | runestrcpy copies       | 0x61 0x62      |
      |                         | runestrcpy copies       |                |
      | 0x2E 0x2E               | runestrncpy copies 3 of | 0x61           |
      | 0x61 0x0 0x2E           | runestrcat adds         | 0x62 0x63      |
      | 0x61 0x62               | runestrcat adds         |                |
      | 0x61 0x0 0x2E           | runestrncat adds 2 of   | 0x62 0x63 0x64 |
      | 0x61 0x62               | runestrncat adds 0 of   | 0x63           |

  @UTF_021
  Scenario Outline: runestrdup copies a string, 0 and all, into a new array
    When runestrdup copies "<runes>"
    Then the new array holds "<copy>"

    Examples:
      | runes           | copy            |
      |                 | 0x0             |
      | 0x61 0x62       | 0x61 0x62 0x0   |
      | 0x61 0x0 0x62   | 0x61 0x0        |
      | 0xD800 0xDC00   | 0xD800 0xDC00 0x0 |
