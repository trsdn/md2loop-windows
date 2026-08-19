using md2loop;

namespace md2loop.Tests;

public class ClipboardContentDetectorTests
{
    [Fact]
    public void Detect_ReturnsUnknown_WhenClipboardIsEmpty()
    {
        Assert.Equal(ClipboardMode.Unknown, ClipboardContentDetector.Detect(null, null, null));
        Assert.Equal(ClipboardMode.Unknown, ClipboardContentDetector.Detect("   ", "", ""));
    }

    [Fact]
    public void Detect_TreatsPlainTextAsMarkdown()
    {
        Assert.Equal(ClipboardMode.Markdown, ClipboardContentDetector.Detect("just a sentence", null));
    }

    [Fact]
    public void Detect_TreatsHtmlOnlyAsRichText()
    {
        Assert.Equal(ClipboardMode.RichText, ClipboardContentDetector.Detect(null, "<p>hello</p>"));
    }

    [Fact]
    public void Detect_TreatsRtfAsRichText()
    {
        Assert.Equal(ClipboardMode.RichText, ClipboardContentDetector.Detect(null, null, @"{\rtf1\ansi hello}"));
    }

    [Fact]
    public void Detect_PrefersRichText_WhenHtmlCarriesRealStructure()
    {
        // A genuine rich text copy: the plain text flavour is just the stripped text.
        const string text = "Quarterly results\nRevenue grew.";
        const string html = "<h1>Quarterly results</h1><p>Revenue grew.</p>";

        Assert.Equal(ClipboardMode.RichText, ClipboardContentDetector.Detect(text, html));
    }

    [Fact]
    public void Detect_PrefersMarkdown_WhenTextHasStrongerMarkdownSignals()
    {
        // Editors publish syntax-coloured HTML next to the original Markdown.
        const string text = "# Title\n\n- one\n- two\n\n`code`";
        const string html = "<div><span>#</span> Title</div><div>- one</div>";

        Assert.Equal(ClipboardMode.Markdown, ClipboardContentDetector.Detect(text, html));
    }

    // Regression test for #14. Markdown that talks about HTML used to be vetoed
    // outright and misreported as rich text.
    [Fact]
    public void Detect_ReturnsMarkdown_WhenMarkdownMentionsHtmlInsideCode()
    {
        const string text = """
            # Embedding video

            Use the `<video>` element:

            ```html
            <video src="clip.mp4"></video>
            ```

            - Works everywhere
            - No plugin needed
            """;
        const string html = "<div><span>#</span> Embedding video</div>";

        Assert.Equal(ClipboardMode.Markdown, ClipboardContentDetector.Detect(text, html));
    }

    [Fact]
    public void Detect_StillPrefersRichText_WhenMarkupIsOutsideCode()
    {
        // Raw markup outside code counts against the text rather than vetoing it,
        // so genuine HTML still wins.
        const string text = "<p>Some <b>content</b></p>";
        const string html = "<p>Some <b>content</b></p>";

        Assert.Equal(ClipboardMode.RichText, ClipboardContentDetector.Detect(text, html));
    }

    [Theory]
    [InlineData("# Heading\n\nSome text\n\n- a\n- b", true)]
    [InlineData("- [x] done\n- [ ] todo", true)]
    [InlineData("Here is `code` and a [link](http://x) and **bold**", true)]
    [InlineData("Just an ordinary sentence without syntax.", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsMarkdown_ScoresTextCorrectly(string? text, bool expected)
    {
        Assert.Equal(expected, ClipboardContentDetector.IsMarkdown(text));
    }

    [Theory]
    [InlineData(@"{\rtf1\ansi hello}", true)]
    [InlineData("   {\\rtf1 padded}", true)]
    [InlineData("not rtf", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ContainsRTF_RecognisesTheRtfHeader(string? rtf, bool expected)
    {
        Assert.Equal(expected, ClipboardContentDetector.ContainsRTF(rtf));
    }

    [Theory]
    [InlineData("<p>hi</p>", true)]
    [InlineData("<table><tr><td>x</td></tr></table>", true)]
    [InlineData("plain text", false)]
    [InlineData("2 < 3 and 4 > 1", false)]
    public void ContainsHTML_DetectsKnownTags(string html, bool expected)
    {
        Assert.Equal(expected, ClipboardContentDetector.ContainsHTML(html));
    }
}
