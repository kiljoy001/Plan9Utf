using Reqnroll;
using Xunit;

namespace Plan9.Utf.Tests.Steps;

[Binding]
public sealed class UtfSteps(Outcome outcome)
{
    private uint rune;
    private bool answer;
    private byte[] buffer = [];

    [When(@"^chartorune reads ""(.*)""$")]
    public void WhenCharToRune(string bytes) => outcome.Count = Utf.CharToRune(out rune, Outcome.Hex(bytes));

    [Then(@"^the rune is (\S+) and it took (\d+) bytes$")]
    public void ThenRune(string expected, int length)
    {
        Assert.Equal(Outcome.Rune(expected), rune);
        Assert.Equal(length, outcome.Count);
    }

    [When(@"^runetochar writes (\S+)$")]
    public void WhenRuneToChar(string value)
    {
        byte[] written = new byte[Utf.UtfMax];
        int length = Utf.RuneToChar(written, Outcome.Rune(value));
        outcome.Bytes = written[..length];
    }

    [Then(@"^the bytes are ""(.*)""$")]
    public void ThenBytes(string bytes) => Assert.Equal(bytes, Outcome.ShowBytes(outcome.Bytes));

    [Then(@"^chartorune reads it back as (\S+)$")]
    public void ThenReadsBack(string value)
    {
        Assert.Equal(outcome.Bytes.Length, Utf.CharToRune(out uint back, outcome.Bytes));
        Assert.Equal(Outcome.Rune(value), back);
    }

    [When(@"^runelen counts (-?\d+)$")]
    public void WhenRuneLen(int value) => outcome.Count = Utf.RuneLen(value);

    [When(@"^runenlen counts the runes ""(.*)""$")]
    public void WhenRuneNLen(string runes)
        => outcome.Count = Utf.RuneNLen([.. runes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Outcome.Rune)]);

    [Then(@"^the count is (-?\d+)$")]
    public void ThenCount(int count) => Assert.Equal(count, outcome.Count);

    [When(@"^fullrune looks at ""(.*)""$")]
    public void WhenFullRune(string bytes) => answer = Utf.FullRune(Outcome.Hex(bytes));

    [Then(@"^it is (true|false)$")]
    public void ThenAnswer(bool expected) => Assert.Equal(expected, answer);

    [When(@"^utflen counts ""(.*)""$")]
    public void WhenUtfLen(string bytes) => outcome.Count = Utf.UtfLen(Outcome.Hex(bytes));

    [When(@"^utfnlen counts ""(.*)""$")]
    public void WhenUtfNLen(string bytes) => outcome.Count = Utf.UtfNLen(Outcome.Hex(bytes));

    [When(@"^utfrune looks for (\S+) in ""(.*)""$")]
    public void WhenUtfRune(string value, string bytes) => outcome.Found = Utf.UtfRune(Outcome.Hex(bytes), (int)Outcome.Rune(value));

    [When(@"^utfrrune looks for (\S+) in ""(.*)""$")]
    public void WhenUtfRRune(string value, string bytes) => outcome.Found = Utf.UtfRRune(Outcome.Hex(bytes), (int)Outcome.Rune(value));

    [When(@"^utfutf looks for ""(.*)"" in ""(.*)""$")]
    public void WhenUtfUtf(string needle, string bytes) => outcome.Found = Utf.UtfUtf(Outcome.Hex(bytes), Outcome.Hex(needle));

    [Then(@"^it is found at (\d+|nowhere)$")]
    public void ThenFound(string place) => Assert.Equal(place == "nowhere" ? -1 : int.Parse(place, System.Globalization.CultureInfo.InvariantCulture), outcome.Found);

    [Given(@"^a buffer of (\d+) bytes holding ""(.*)""$")]
    public void GivenBuffer(int size, string bytes)
    {
        buffer = Outcome.Hex(bytes);
        Assert.Equal(size, buffer.Length);
    }

    [When(@"^utfecpy copies ""(.*)"" into it$")]
    public void WhenUtfECpy(string bytes) => outcome.Count = Utf.UtfECpy(buffer, Outcome.Hex(bytes));

    [Then(@"^the NUL is at (\d+) and the buffer holds ""(.*)""$")]
    public void ThenCopied(int end, string bytes)
    {
        Assert.Equal(end, outcome.Count);
        Assert.Equal(bytes, Outcome.ShowBytes(buffer));
    }

    [When(@"^utfncmp compares ""(.*)"" with ""(.*)"" for (-?\d+) runes$")]
    public void WhenUtfNCmp(string first, string second, int n) => outcome.Count = Utf.UtfNCmp(Outcome.Hex(first), Outcome.Hex(second), n);

    [Then(@"^the comparison is (-?\d+)$")]
    public void ThenComparison(int result) => Assert.Equal(result, outcome.Count);
}
