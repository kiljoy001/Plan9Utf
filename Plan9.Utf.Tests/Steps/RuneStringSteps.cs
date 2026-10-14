using Reqnroll;
using Xunit;

namespace Plan9.Utf.Tests.Steps;

[Binding]
public sealed class RuneStringSteps(Outcome outcome)
{
    private uint[] buffer = [];
    private Exception? refusal;

    [When(@"^ToRunes reads ""(.*)""$")]
    public void WhenToRunes(string bytes)
    {
        byte[] utf = Outcome.Hex(bytes);
        outcome.Runes = new uint[Utf.CountRunes(utf)];
        Assert.Equal(outcome.Runes.Length, Utf.ToRunes(utf, outcome.Runes));
    }

    [Then(@"^the runes are ""(.*)""$")]
    public void ThenRunes(string runes) => Assert.Equal(runes, Outcome.ShowRunes(outcome.Runes));

    [Then(@"^ToUtf writes them as ""(.*)""$")]
    public void ThenWritesThem(string bytes) => Assert.Equal(bytes, Outcome.ShowBytes(Write(outcome.Runes)));

    [When(@"^ToUtf writes the runes ""(.*)""$")]
    public void WhenToUtf(string runes)
    {
        outcome.Runes = Outcome.RuneList(runes);
        outcome.Bytes = Write(outcome.Runes);
    }

    [Then(@"^the count of them is runenlen's$")]
    public void ThenRuneNLen() => Assert.Equal(Utf.RuneNLen(outcome.Runes), outcome.Bytes.Length);

    [Then(@"^ToRunes reads them back as ""(.*)""$")]
    public void ThenReadsBack(string runes)
    {
        uint[] back = new uint[Utf.CountRunes(outcome.Bytes)];
        Assert.Equal(back.Length, Utf.ToRunes(outcome.Bytes, back));
        Assert.Equal(runes, Outcome.ShowRunes(back));
    }

    [When(@"^the runes of ""(.*)"" are counted$")]
    public void WhenCounted(string bytes) => outcome.Count = Utf.CountRunes(Outcome.Hex(bytes));

    [When(@"^ToRunes reads ""(.*)"" into a span of (\d+)$")]
    public void WhenToRunesInto(string bytes, int size) => refusal = Record(() => Utf.ToRunes(Outcome.Hex(bytes), new uint[size]));

    [When(@"^ToUtf writes the runes ""(.*)"" into a span of (\d+)$")]
    public void WhenToUtfInto(string runes, int size) => refusal = Record(() => Utf.ToUtf(Outcome.RuneList(runes), new byte[size]));

    [Then(@"^it is refused as too small$")]
    public void ThenRefused() => Assert.IsType<ArgumentException>(refusal);

    [When(@"^runestrlen counts ""(.*)""$")]
    public void WhenRuneStrLen(string runes) => outcome.Count = Utf.RuneStrLen(Outcome.RuneList(runes));

    [When(@"^runestrcmp compares ""(.*)"" with ""(.*)""$")]
    public void WhenRuneStrCmp(string first, string second) => outcome.Count = Utf.RuneStrCmp(Outcome.RuneList(first), Outcome.RuneList(second));

    [When(@"^runestrncmp (-?\d+) runes compares ""(.*)"" with ""(.*)""$")]
    public void WhenRuneStrNCmp(int n, string first, string second) => outcome.Count = Utf.RuneStrNCmp(Outcome.RuneList(first), Outcome.RuneList(second), n);

    [When(@"^runestrchr looks for ""(.*)"" in ""(.*)""$")]
    public void WhenRuneStrChr(string rune, string runes) => outcome.Found = Utf.RuneStrChr(Outcome.RuneList(runes), Outcome.Rune(rune));

    [When(@"^runestrrchr looks for ""(.*)"" in ""(.*)""$")]
    public void WhenRuneStrRChr(string rune, string runes) => outcome.Found = Utf.RuneStrRChr(Outcome.RuneList(runes), Outcome.Rune(rune));

    [When(@"^runestrstr looks for ""(.*)"" in ""(.*)""$")]
    public void WhenRuneStrStr(string needle, string runes) => outcome.Found = Utf.RuneStrStr(Outcome.RuneList(runes), Outcome.RuneList(needle));

    [Given(@"^a buffer of runes ""(.*)""$")]
    public void GivenBuffer(string runes) => buffer = Outcome.RuneList(runes);

    [When(@"^runestrecpy copies ""(.*)"" into it$")]
    public void WhenRuneStrECpy(string runes) => outcome.Count = Utf.RuneStrECpy(buffer, Outcome.RuneList(runes));

    [Then(@"^the 0 is at (\d+) and the buffer holds ""(.*)""$")]
    public void ThenCopied(int end, string runes)
    {
        Assert.Equal(end, outcome.Count);
        Assert.Equal(runes, Outcome.ShowRunes(buffer));
    }

    [When(@"^runestrcpy copies ""(.*)"" into it$")]
    public void WhenRuneStrCpy(string runes) => refusal = Record(() => Utf.RuneStrCpy(buffer, Outcome.RuneList(runes)));

    [When(@"^runestrncpy copies (-?\d+) of ""(.*)"" into it$")]
    public void WhenRuneStrNCpy(int n, string runes) => refusal = Record(() => Utf.RuneStrNCpy(buffer, Outcome.RuneList(runes), n));

    [When(@"^runestrcat adds ""(.*)"" into it$")]
    public void WhenRuneStrCat(string runes) => refusal = Record(() => Utf.RuneStrCat(buffer, Outcome.RuneList(runes)));

    [When(@"^runestrncat adds (-?\d+) of ""(.*)"" into it$")]
    public void WhenRuneStrNCat(int n, string runes) => refusal = Record(() => Utf.RuneStrNCat(buffer, Outcome.RuneList(runes), n));

    [Then(@"^the buffer holds ""(.*)""$")]
    public void ThenBufferHolds(string runes)
    {
        Assert.Null(refusal);
        Assert.Equal(runes, Outcome.ShowRunes(buffer));
    }

    [Then(@"^it is refused as too small, and the buffer holds ""(.*)""$")]
    public void ThenRefusedAndHolds(string runes)
    {
        Assert.IsType<ArgumentException>(refusal);
        Assert.Equal(runes, Outcome.ShowRunes(buffer));
    }

    [When(@"^runestrdup copies ""(.*)""$")]
    public void WhenRuneStrDup(string runes) => outcome.Runes = Utf.RuneStrDup(Outcome.RuneList(runes));

    [Then(@"^the new array holds ""(.*)""$")]
    public void ThenNewArray(string runes) => Assert.Equal(runes, Outcome.ShowRunes(outcome.Runes));

    private static byte[] Write(uint[] runes)
    {
        byte[] bytes = new byte[Utf.RuneNLen(runes)];
        Assert.Equal(bytes.Length, Utf.ToUtf(runes, bytes));
        return bytes;
    }

    private static Exception? Record(Action act)
    {
        try
        {
            act();
            return null;
        }
        catch (Exception thrown)
        {
            return thrown;
        }
    }
}
