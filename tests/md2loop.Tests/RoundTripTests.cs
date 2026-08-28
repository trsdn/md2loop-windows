using md2loop;

namespace md2loop.Tests;

/// <summary>
/// Markdown -> Loop HTML -> Markdown. This is what a user does when they paste
/// into Loop and later copy the content back out, so the structure has to survive.
/// </summary>
public class RoundTripTests
{
    private static string RoundTrip(string markdown)
        => HtmlToMarkdownConverter.Convert(LoopHtmlConverter.Convert(markdown)).Replace("\r\n", "\n");

    [Theory]
    [InlineData("# Heading one")]
    [InlineData("## Heading two")]
    public void Headings_SurviveARoundTrip(string markdown)
    {
        Assert.Equal(markdown, RoundTrip(markdown));
    }

    [Fact]
    public void InlineFormatting_SurvivesARoundTrip()
    {
        Assert.Equal("Some **bold** and *italic* text.", RoundTrip("Some **bold** and *italic* text."));
    }

    [Fact]
    public void BulletLists_SurviveARoundTrip()
    {
        Assert.Equal("- one\n- two", RoundTrip("- one\n- two"));
    }

    [Fact]
    public void OrderedLists_SurviveARoundTrip()
    {
        Assert.Equal("1. one\n2. two", RoundTrip("1. one\n2. two"));
    }

    [Fact]
    public void NestedLists_SurviveARoundTrip()
    {
        Assert.Equal("- parent\n    - child", RoundTrip("- parent\n    - child"));
    }

    [Fact]
    public void TaskLists_SurviveARoundTrip()
    {
        Assert.Equal("- [x] done\n- [ ] todo", RoundTrip("- [x] done\n- [ ] todo"));
    }

    [Fact]
    public void Links_SurviveARoundTrip()
    {
        Assert.Equal("[docs](http://x.dev)", RoundTrip("[docs](http://x.dev)"));
    }

    [Fact]
    public void FencedCode_KeepsItsLanguage()
    {
        Assert.Equal("```csharp\nvar x = 1;\n```", RoundTrip("```csharp\nvar x = 1;\n```"));
    }

    [Fact]
    public void Blockquotes_SurviveARoundTrip()
    {
        Assert.Equal("> quoted", RoundTrip("> quoted"));
    }

    [Fact]
    public void LiteralAsterisks_AreNotTurnedIntoEmphasis()
    {
        // The text is escaped on the way back, so re-rendering it produces the
        // same literal characters rather than emphasis.
        var once = RoundTrip(@"2 \* 3 \* 4");

        Assert.Equal(@"2 \* 3 \* 4", once);
    }

    [Fact]
    public void RepeatedRoundTrips_AreStable()
    {
        const string markdown = "# Title\n\nSome **bold** text.\n\n- one\n- two";

        var once = RoundTrip(markdown);
        var twice = RoundTrip(once);

        Assert.Equal(once, twice);
    }

    // The remaining cases are known open bugs. They are kept here so the expected
    // behaviour is written down and the tests start passing when the bugs are fixed.

    [Fact(Skip = "Known bug #32: no blank line after a list, so the next block is swallowed into it.")]
    public void ParagraphAfterAList_StaysASeparateBlock()
    {
        Assert.Equal("- one\n- two\n\nAfter the list.", RoundTrip("- one\n- two\n\nAfter the list."));
    }

    [Fact(Skip = "Known bug #33: whitespace between block elements leaks a leading space into the next block.")]
    public void WhitespaceBetweenBlocks_DoesNotIndentTheNextBlock()
    {
        Assert.Equal("one\n\ntwo", HtmlToMarkdownConverter.Convert("<p>one</p>\n<p>two</p>").Replace("\r\n", "\n"));
    }
}
