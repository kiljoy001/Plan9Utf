@encoding
Feature: .NET text goes to and from UTF-8 as Plan 9 reads and writes it
  Plan9Utf8Encoding is a System.Text.Encoding that reads bytes as chartorune does and writes text
  as runetochar does, so whatever .NET does with an Encoding, it does as Plan 9 would. Its bytes
  are counted text, not a C string, so a NUL is a character like any other; a sequence cut short
  by the end of them is bad, as chartorune finds it at a NUL. A rune above U+FFFF is a surrogate
  pair in .NET's UTF-16, and a surrogate rune, which Plan 9 reads like any other, a surrogate on
  its own. Writing, a high surrogate followed by a low one is the rune they pair to, and any other
  surrogate is written as the rune it is. Bytes encoding a high surrogate and then a low one are
  the one thing that does not come back as it was: in .NET they are a pair, which is written as
  the rune it stands for. A decoder holds what a sequence has of itself until the rest comes or
  it is flushed, and an encoder holds a high surrogate until the next character. Plan 9 counts a
  string by its bytes or by its runes, where .NET counts it in UTF-16; utflen counts the runes of
  what the encoding writes.

  @UTF_011
  Scenario Outline: Bytes are read as chartorune reads them, where .NET's UTF-8 reads some otherwise
    When the Plan 9 encoding reads "<bytes>"
    Then the text is "<text>"
    And .NET's UTF-8 reads it as "<dotnet>"

    Examples:
      | bytes             | text                | dotnet         |
      |                   |                     |                |
      | 41                | 0041                | 0041           |
      | 41 00 42          | 0041 0000 0042      | 0041 0000 0042 |
      | e2 82 ac          | 20AC                | 20AC           |
      | ef bf bd          | FFFD                | FFFD           |
      | ef bf bf          | FFFF                | FFFF           |
      | f0 9f 98 80       | D83D DE00           | D83D DE00      |
      | 80 41             | FFFD 0041           | FFFD 0041      |
      | c0 80             | FFFD FFFD           | FFFD FFFD      |
      | e2 82             | FFFD FFFD           | FFFD           |
      | e2 82 41          | FFFD FFFD 0041      | FFFD 0041      |
      | f0 9f 98          | FFFD FFFD FFFD      | FFFD           |
      | ed a0 80          | D800                | FFFD FFFD FFFD |
      | ed b0 80          | DC00                | FFFD FFFD FFFD |
      | ed a0 80 ed b0 80 | D800 DC00           | FFFD FFFD FFFD FFFD FFFD FFFD |
      | f4 90 80 80       | FFFD FFFD FFFD FFFD | FFFD FFFD FFFD FFFD |

  @UTF_012
  Scenario Outline: Text is written as runetochar writes its runes, a lone surrogate as itself
    When the Plan 9 encoding writes "<text>"
    Then the bytes are "<bytes>"
    And .NET's UTF-8 writes it as "<dotnet>"

    Examples:
      | text           | bytes             | dotnet            |
      |                |                   |                   |
      | 0041           | 41                | 41                |
      | 0000           | 00                | 00                |
      | 20AC           | e2 82 ac          | e2 82 ac          |
      | D83D DE00      | f0 9f 98 80       | f0 9f 98 80       |
      | DBFF DFFF      | f4 8f bf bf       | f4 8f bf bf       |
      | D800           | ed a0 80          | ef bf bd          |
      | DC00 D800      | ed b0 80 ed a0 80 | ef bf bd ef bf bd |
      | D800 0041      | ed a0 80 41       | ef bf bd 41       |
      | 0041 DFFF      | 41 ed bf bf       | 41 ef bf bd       |

  @UTF_012
  Scenario: A high surrogate's bytes and a low one's come back as a pair, which is written as the rune it stands for
    When the Plan 9 encoding reads "ed a0 80 ed b0 80"
    Then the text is "D800 DC00"
    And the Plan 9 encoding writes it as "f0 90 80 80"

  @UTF_013
  Scenario Outline: Its counts are those of what it reads and writes
    When the Plan 9 encoding counts the text "<text>" and the bytes "<bytes>"
    Then it counts <bytecount> bytes and <charcount> characters

    Examples:
      | text      | bytes          | bytecount | charcount |
      |           |                | 0         | 0         |
      | 0041 20AC | 41 e2 82 ac    | 4         | 2         |
      | D83D DE00 | f0 9f 98 80    | 4         | 2         |
      | D800 DC00 | ed a0 80       | 4         | 1         |
      | DC00 D800 | e2 82          | 6         | 2         |
      | D800      | f0 9f 98       | 3         | 3         |

  @UTF_013
  Scenario Outline: Plan 9 counts a string in bytes and runes, where .NET counts it in UTF-16
    When the Plan 9 encoding writes "<text>"
    Then .NET counts <length> characters, and Plan 9 <bytes> bytes and <runes> runes

    Examples:
      | text           | length | bytes | runes |
      | 0041           | 1      | 1     | 1     |
      | 20AC           | 1      | 3     | 1     |
      | D83D DE00      | 2      | 4     | 1     |
      | D83D DE00 0041 | 3      | 5     | 2     |
      | D800           | 1      | 3     | 1     |
      | DC00 D800      | 2      | 6     | 2     |

  @UTF_014
  Scenario Outline: A decoder reads a sequence split between calls as one, and what is cut short when flushed as bad
    When a Plan 9 decoder reads "<pieces>" and is flushed
    Then the text is "<text>"

    Examples:
      | pieces                    | text                |
      | e2 / 82 ac                | 20AC                |
      | e2 82 / ac                | 20AC                |
      | f0 / 9f / 98 / 80         | D83D DE00           |
      | 41 e2 / 82 ac 42          | 0041 20AC 0042      |
      | e2 82 / 41                | FFFD FFFD 0041      |
      | e2 82 /                   | FFFD FFFD           |
      | f0 9f 98 /                | FFFD FFFD FFFD      |
      | 80 / 80                   | FFFD FFFD           |
      | ed a0 / 80 ed / b0 80     | D800 DC00           |

  @UTF_014
  Scenario Outline: An encoder writes a pair split between calls as one rune, and a high surrogate it is left with as itself
    When a Plan 9 encoder writes "<pieces>" and is flushed
    Then the bytes are "<bytes>"

    Examples:
      | pieces                | bytes             |
      | D83D / DE00           | f0 9f 98 80       |
      | 0041 D83D / DE00 0042 | 41 f0 9f 98 80 42 |
      | D83D / 0041           | ed a0 bd 41       |
      | D83D /                | ed a0 bd          |
      | D83D / D83D / DE00    | ed a0 bd f0 9f 98 80 |
      | DE00 / D83D           | ed b8 80 ed a0 bd |

  @UTF_014
  Scenario Outline: A decoder's and an encoder's array calls flush only when asked
    When a Plan 9 <coder> <writes> "<pieces>" through arrays and is flushed
    Then the <result> "<expected>"

    Examples:
      | coder   | writes | pieces            | result    | expected    |
      | decoder | reads  | e2 / 82 ac        | text is   | 20AC        |
      | decoder | reads  | f0 / 9f 98 / 80   | text is   | D83D DE00   |
      | decoder | reads  | e2 82 /           | text is   | FFFD FFFD   |
      | encoder | writes | D83D / DE00       | bytes are | f0 9f 98 80 |
      | encoder | writes | D83D /            | bytes are | ed a0 bd    |

  @UTF_015
  Scenario Outline: The most a count of characters or bytes can come to holds what an encoder or decoder may have left, and is refused past int.MaxValue
    When the Plan 9 encoding is asked the most for <count>
    Then it is <bytes> bytes and <chars> characters

    Examples:
      | count      | bytes      | chars      |
      | 0          | 3          | 3          |
      | 1          | 6          | 4          |
      | 10         | 33         | 13         |
      | 715827881  | 2147483646 | 715827884  |
      | 715827882  | refused    | 715827885  |
      | 2147483644 | refused    | 2147483647 |
      | 2147483645 | refused    | refused    |
      | -1         | refused    | refused    |

  @UTF_016
  Scenario: The encoding is Plan 9 UTF-8 by name, UTF-8 on the web, and equal only to itself
    Then the Plan 9 encoding is named "Plan 9 UTF-8", with the web name "utf-8" and the code page 65001
    And it is equal to another Plan 9 encoding, with the same hash, and not to .NET's UTF-8

  @UTF_017
  Scenario Outline: Arguments are refused as .NET's UTF-8 refuses them
    When <call> is called on the Plan 9 encoding and on .NET's UTF-8
    Then both throw <exception>

    Examples:
      | call                                       | exception                   |
      | GetByteCount with no characters            | ArgumentNullException       |
      | GetBytes with no characters                | ArgumentNullException       |
      | GetBytes with nowhere to write             | ArgumentNullException       |
      | GetCharCount with no bytes                 | ArgumentNullException       |
      | GetChars with no bytes                     | ArgumentNullException       |
      | GetChars with nowhere to write             | ArgumentNullException       |
      | GetBytes of a euro into two bytes          | ArgumentException           |
      | GetChars of an emoji into one character    | ArgumentException           |
      | a decoder's GetCharCount with no bytes     | ArgumentNullException       |
      | a decoder's GetChars with no bytes         | ArgumentNullException       |
      | a decoder's GetChars with nowhere to write | ArgumentNullException       |
      | an encoder's GetByteCount with no characters | ArgumentNullException     |
      | an encoder's GetBytes with no characters   | ArgumentNullException       |
      | an encoder's GetBytes with nowhere to write | ArgumentNullException      |
