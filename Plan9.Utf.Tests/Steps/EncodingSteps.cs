using System.Text;
using Reqnroll;
using Xunit;

namespace Plan9.Utf.Tests.Steps;

[Binding]
public sealed class EncodingSteps(Outcome outcome)
{
    private readonly Plan9Utf8Encoding plan9 = Plan9Utf8Encoding.Instance;
    private int most;

    [When(@"^the Plan 9 encoding reads ""(.*)""$")]
    public void WhenReads(string bytes)
    {
        outcome.Bytes = Outcome.Hex(bytes);
        outcome.Text = plan9.GetString(outcome.Bytes);
    }

    [Then(@"^the text is ""(.*)""$")]
    public void ThenText(string text) => Assert.Equal(text, Outcome.ShowUnits(outcome.Text));

    [Then(@"^\.NET's UTF-8 reads it as ""(.*)""$")]
    public void ThenDotNetReads(string text) => Assert.Equal(text, Outcome.ShowUnits(Encoding.UTF8.GetString(outcome.Bytes)));

    [When(@"^the Plan 9 encoding writes ""(.*)""$")]
    public void WhenWrites(string text)
    {
        outcome.Text = Outcome.Units(text);
        outcome.Bytes = plan9.GetBytes(outcome.Text);
    }

    [Then(@"^\.NET's UTF-8 writes it as ""(.*)""$")]
    public void ThenDotNetWrites(string bytes) => Assert.Equal(bytes, Outcome.ShowBytes(Encoding.UTF8.GetBytes(outcome.Text)));

    [Then(@"^the Plan 9 encoding writes it as ""(.*)""$")]
    public void ThenWritesAs(string bytes) => Assert.Equal(bytes, Outcome.ShowBytes(plan9.GetBytes(outcome.Text)));

    [When(@"^the Plan 9 encoding counts the text ""(.*)"" and the bytes ""(.*)""$")]
    public void WhenCounts(string text, string bytes)
    {
        outcome.Count = plan9.GetByteCount(Outcome.Units(text));
        most = plan9.GetCharCount(Outcome.Hex(bytes));
    }

    [Then(@"^it counts (\d+) bytes and (\d+) characters$")]
    public void ThenCounted(int bytes, int chars)
    {
        Assert.Equal(bytes, outcome.Count);
        Assert.Equal(chars, most);
    }

    [Then(@"^\.NET counts (\d+) characters, and Plan 9 (\d+) bytes and (\d+) runes$")]
    public void ThenPlan9Counts(int length, int bytes, int runes)
    {
        Assert.Equal(length, outcome.Text.Length);
        Assert.Equal(bytes, outcome.Bytes.Length);
        Assert.Equal(runes, Utf.UtfLen(outcome.Bytes));
    }

    [When(@"^a Plan 9 decoder reads ""(.*)"" and is flushed$")]
    public void WhenDecoderReads(string pieces)
    {
        Decoder decoder = plan9.GetDecoder();
        var text = new StringBuilder();
        string[] split = pieces.Split('/');
        for (int i = 0; i < split.Length; i++)
        {
            byte[] piece = Outcome.Hex(split[i]);
            bool flush = i == split.Length - 1;
            char[] chars = new char[decoder.GetCharCount(piece, flush)];
            Assert.Equal(chars.Length, decoder.GetChars(piece, chars, flush));
            text.Append(chars);
        }

        outcome.Text = text.ToString();
    }

    [When(@"^a Plan 9 encoder writes ""(.*)"" and is flushed$")]
    public void WhenEncoderWrites(string pieces)
    {
        Encoder encoder = plan9.GetEncoder();
        var bytes = new List<byte>();
        string[] split = pieces.Split('/');
        for (int i = 0; i < split.Length; i++)
        {
            string piece = Outcome.Units(split[i]);
            bool flush = i == split.Length - 1;
            byte[] written = new byte[encoder.GetByteCount(piece, flush)];
            Assert.Equal(written.Length, encoder.GetBytes(piece, written, flush));
            bytes.AddRange(written);
        }

        outcome.Bytes = [.. bytes];
    }

    [When(@"^the Plan 9 encoding is asked the most for (\d+)$")]
    public void WhenMost(int count)
    {
        outcome.Count = plan9.GetMaxByteCount(count);
        most = plan9.GetMaxCharCount(count);
    }

    [Then(@"^it is (\d+) bytes and (\d+) characters$")]
    public void ThenMost(int bytes, int chars)
    {
        Assert.Equal(bytes, outcome.Count);
        Assert.Equal(chars, most);
    }
}
