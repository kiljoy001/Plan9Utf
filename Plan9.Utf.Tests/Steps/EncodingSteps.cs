using System.Text;
using Reqnroll;
using Xunit;

namespace Plan9.Utf.Tests.Steps;

[Binding]
public sealed class EncodingSteps(Outcome outcome)
{
    private readonly Plan9Utf8Encoding plan9 = Plan9Utf8Encoding.Instance;
    private int most;
    private string mostBytes = string.Empty;
    private string mostChars = string.Empty;
    private string[] refusals = [];

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

    [When(@"^a Plan 9 decoder reads ""(.*)"" through arrays and is flushed$")]
    public void WhenDecoderReadsArrays(string pieces)
    {
        Decoder decoder = plan9.GetDecoder();
        var text = new StringBuilder();
        string[] split = pieces.Split('/');
        for (int i = 0; i < split.Length; i++)
        {
            byte[] piece = Outcome.Hex(split[i]);
            bool flush = i == split.Length - 1;
            char[] chars = new char[flush ? decoder.GetCharCount(piece, 0, piece.Length, flush) : decoder.GetCharCount(piece, 0, piece.Length)];
            int written = flush ? decoder.GetChars(piece, 0, piece.Length, chars, 0, flush) : decoder.GetChars(piece, 0, piece.Length, chars, 0);
            Assert.Equal(chars.Length, written);
            text.Append(chars);
        }

        outcome.Text = text.ToString();
    }

    [When(@"^a Plan 9 encoder writes ""(.*)"" through arrays and is flushed$")]
    public void WhenEncoderWritesArrays(string pieces)
    {
        Encoder encoder = plan9.GetEncoder();
        var bytes = new List<byte>();
        string[] split = pieces.Split('/');
        for (int i = 0; i < split.Length; i++)
        {
            char[] piece = Outcome.Units(split[i]).ToCharArray();
            bool flush = i == split.Length - 1;
            byte[] written = new byte[encoder.GetByteCount(piece, 0, piece.Length, flush)];
            Assert.Equal(written.Length, encoder.GetBytes(piece, 0, piece.Length, written, 0, flush));
            bytes.AddRange(written);
        }

        outcome.Bytes = [.. bytes];
    }

    [When(@"^the Plan 9 encoding is asked the most for (-?\d+)$")]
    public void WhenMost(int count)
    {
        mostBytes = Most(() => plan9.GetMaxByteCount(count));
        mostChars = Most(() => plan9.GetMaxCharCount(count));
    }

    [Then(@"^it is (\S+) bytes and (\S+) characters$")]
    public void ThenMost(string bytes, string chars)
    {
        Assert.Equal(bytes, mostBytes);
        Assert.Equal(chars, mostChars);
    }

    [Then(@"^the Plan 9 encoding is named ""(.*)"", with the web name ""(.*)"" and the code page (\d+)$")]
    public void ThenNamed(string name, string webName, int codePage)
    {
        Assert.Equal(name, plan9.EncodingName);
        Assert.Equal(webName, plan9.WebName);
        Assert.Equal(codePage, plan9.CodePage);
    }

    [Then(@"^it is equal to another Plan 9 encoding, with the same hash, and not to \.NET's UTF-8$")]
    public void ThenEqual()
    {
        var other = new Plan9Utf8Encoding();
        Assert.True(plan9.Equals(other));
        Assert.Equal(plan9.GetHashCode(), other.GetHashCode());
        Assert.False(plan9.Equals(Encoding.UTF8));
        Assert.False(plan9.Equals(null));
    }

    [When(@"^(.+) is called on the Plan 9 encoding and on \.NET's UTF-8$")]
    public void WhenCalled(string call)
    {
        refusals = [Refusal(plan9, call), Refusal(Encoding.UTF8, call)];
    }

    [When(@"^(.+) is called on the Plan 9 encoding$")]
    public void WhenCalledOnPlan9(string call) => refusals = [Refusal(plan9, call, message: true)];

    [Then(@"^it is refused with ""(.*)""$")]
    public void ThenRefusedWith(string message) => Assert.Equal(message, Assert.Single(refusals));

    [Then(@"^both throw (\w+)$")]
    public void ThenBothThrow(string exception) => Assert.All(refusals, thrown => Assert.Equal(exception, thrown));

    private static string Most(Func<int> count)
    {
        try
        {
            return count().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (ArgumentOutOfRangeException)
        {
            return "refused";
        }
    }

    private static string Refusal(Encoding encoding, string call, bool message = false)
    {
        char[] chars = ['a'];
        byte[] bytes = [0x61];
        Action act = call switch
        {
            "GetByteCount with no characters" => () => encoding.GetByteCount((char[])null!, 0, 0),
            "GetBytes with no characters" => () => encoding.GetBytes((char[])null!, 0, 0, bytes, 0),
            "GetBytes with nowhere to write" => () => encoding.GetBytes(chars, 0, 1, null!, 0),
            "GetCharCount with no bytes" => () => encoding.GetCharCount(null!, 0, 0),
            "GetChars with no bytes" => () => encoding.GetChars(null!, 0, 0, chars, 0),
            "GetChars with nowhere to write" => () => encoding.GetChars(bytes, 0, 1, null!, 0),
            "GetBytes of a euro into two bytes" => () => encoding.GetBytes(['\u20ac'], 0, 1, new byte[2], 0),
            "GetChars of a letter into no characters" => () => encoding.GetChars(bytes, 0, 1, [], 0),
            "GetChars of an emoji into one character" => () => encoding.GetChars([0xf0, 0x9f, 0x98, 0x80], 0, 4, new char[1], 0),
            "a decoder's GetCharCount with no bytes" => () => encoding.GetDecoder().GetCharCount(null!, 0, 0, flush: true),
            "a decoder's GetChars with no bytes" => () => encoding.GetDecoder().GetChars(null!, 0, 0, chars, 0, flush: true),
            "a decoder's GetChars with nowhere to write" => () => encoding.GetDecoder().GetChars(bytes, 0, 1, null!, 0, flush: true),
            "an encoder's GetByteCount with no characters" => () => encoding.GetEncoder().GetByteCount(null!, 0, 0, flush: true),
            "an encoder's GetBytes with no characters" => () => encoding.GetEncoder().GetBytes(null!, 0, 0, bytes, 0, flush: true),
            "an encoder's GetBytes with nowhere to write" => () => encoding.GetEncoder().GetBytes(chars, 0, 1, null!, 0, flush: true),
            _ => throw new ArgumentException($"no call named {call}", nameof(call)),
        };

        try
        {
            act();
            return "nothing";
        }
        catch (ArgumentException thrown)
        {
            return message ? thrown.Message : thrown.GetType().Name;
        }
    }
}
