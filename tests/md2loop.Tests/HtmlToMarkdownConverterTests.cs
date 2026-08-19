using md2loop;

namespace md2loop.Tests;

public class HtmlToMarkdownConverterTests
{
    private static string Convert(string html)
        => HtmlToMarkdownConverter.Convert(html).Replace("\r\n", "\n");

    [Fact]
    public void Convert_ReturnsEmpty_ForBlankInput()
    {
        Assert.Equal(string.Empty, HtmlToMarkdownConverter.Convert(""));
        Assert.Equal(string.Empty, HtmlToMarkdownConverter.Convert("   "));
    }

    [Theory]
    [InlineData("<h1>Title</h1>", "# Title")]
    [InlineData("<h3>Deeper</h3>", "### Deeper")]
    [InlineData("<p>A paragraph.</p>", "A paragraph.")]
    [InlineData("<strong>bold</strong>", "**bold**")]
    [InlineData("<em>italic</em>", "*italic*")]
    [InlineData("<del>gone</del>", "~~gone~~")]
    [InlineData("<a href=\"http://example.com\">link</a>", "[link](http://example.com)")]
    [InlineData("<img src=\"pic.png\" alt=\"a pic\">", "![a pic](pic.png)")]
    public void Convert_HandlesBasicElements(string html, string expected)
    {
        Assert.Equal(expected, Convert(html));
    }

    [Fact]
    public void Convert_HandlesBlockquotes()
    {
        Assert.Equal("> quoted", Convert("<blockquote><p>quoted</p></blockquote>"));
    }

    [Fact]
    public void Convert_BuildsPipeTables()
    {
        var markdown = Convert("<table><thead><tr><th>a</th><th>b</th></tr></thead>" +
                               "<tbody><tr><td>1</td><td>2</td></tr></tbody></table>");

        Assert.Contains("| a", markdown);
        Assert.Contains("| 1", markdown);
        Assert.Contains("---", markdown);
    }

    [Fact]
    public void Convert_EscapesPipesInsideTableCells()
    {
        var markdown = Convert("<table><tr><td>a|b</td></tr></table>");

        Assert.Contains(@"a\|b", markdown);
    }

    // Regression tests for #17. InnerText returns every descendant, so a nested
    // list used to be emitted once as part of its parent's text and again as
    // indented items.
    [Fact]
    public void Convert_DoesNotDuplicateNestedListContent()
    {
        var markdown = Convert("<ul><li>Parent<ul><li>Child</li></ul></li></ul>");

        Assert.Equal("- Parent\n    - Child", markdown);
    }

    [Fact]
    public void Convert_HandlesNestedListsWrappedInAnotherElement()
    {
        var markdown = Convert("<ul><li>Parent<div><ul><li>Child</li></ul></div></li></ul>");

        Assert.Equal("- Parent\n    - Child", markdown);
    }

    [Fact]
    public void Convert_NumbersOrderedListsIndependentlyPerLevel()
    {
        var markdown = Convert("<ol><li>one<ol><li>inner</li><li>inner two</li></ol></li><li>two</li></ol>");

        Assert.Equal("1. one\n    1. inner\n    2. inner two\n2. two", markdown);
    }

    // Regression tests for #15. List items and table cells used to be flattened
    // with InnerText, which threw away every inline element.
    [Fact]
    public void Convert_KeepsInlineFormattingInsideListItems()
    {
        var markdown = Convert("<ul><li><strong>bold</strong> and <em>italic</em></li></ul>");

        Assert.Equal("- **bold** and *italic*", markdown);
    }

    [Fact]
    public void Convert_KeepsLinksInsideListItems()
    {
        var markdown = Convert("<ul><li>see <a href=\"http://x.dev\">docs</a></li></ul>");

        Assert.Equal("- see [docs](http://x.dev)", markdown);
    }

    [Fact]
    public void Convert_KeepsInlineFormattingInsideTableCells()
    {
        var markdown = Convert("<table><tr><td><strong>bold</strong></td><td><code>x</code></td></tr></table>");

        Assert.Contains("**bold**", markdown);
        Assert.Contains("`x`", markdown);
    }

    // Regression tests for #19. Only a single direct <code> child was read, so
    // any other markup inside <pre> lost its content entirely.
    [Fact]
    public void Convert_KeepsPreContent_WhenThereIsNoCodeElement()
    {
        var markdown = Convert("<pre>plain preformatted</pre>");

        Assert.Contains("plain preformatted", markdown);
        Assert.StartsWith("```", markdown);
    }

    [Fact]
    public void Convert_KeepsPreContent_WhenCodeIsWrapped()
    {
        var markdown = Convert("<pre><div><code>wrapped code</code></div></pre>");

        Assert.Contains("wrapped code", markdown);
    }

    [Fact]
    public void Convert_KeepsPreContent_WhenSplitAcrossSeveralCodeElements()
    {
        var markdown = Convert("<pre><code>first\n</code><code>second</code></pre>");

        Assert.Contains("first", markdown);
        Assert.Contains("second", markdown);
    }

    [Fact]
    public void Convert_ReadsTheLanguageHintFromTheCodeClass()
    {
        var markdown = Convert("<pre><code class=\"language-csharp\">var x = 1;</code></pre>");

        Assert.Equal("```csharp\nvar x = 1;\n```", markdown);
    }

    [Fact]
    public void Convert_DoesNotEscapeInsidePreformattedText()
    {
        var markdown = Convert("<pre><code>a * b [c]</code></pre>");

        Assert.Contains("a * b [c]", markdown);
    }

    // Regression tests for #16. Text was emitted verbatim, so characters that
    // mean something in Markdown silently changed the formatting on re-read.
    [Theory]
    [InlineData("<p>2 * 3 * 4</p>", @"2 \* 3 \* 4")]
    [InlineData("<p>see [note] here</p>", @"see \[note\] here")]
    [InlineData("<p>a ` backtick</p>", @"a \` backtick")]
    [InlineData(@"<p>back\slash</p>", @"back\\slash")]
    [InlineData("<p>not ~~struck~~</p>", @"not \~\~struck\~\~")]
    public void Convert_EscapesInlineMarkdownSyntax(string html, string expected)
    {
        Assert.Equal(expected, Convert(html));
    }

    [Fact]
    public void Convert_LeavesUnderscoresInsideWordsAlone()
    {
        // Escaping these would make ordinary identifiers unreadable.
        Assert.Equal("file_name_here", Convert("<p>file_name_here</p>"));
    }

    [Fact]
    public void Convert_EscapesUnderscoresAtWordBoundaries()
    {
        Assert.Equal(@"an \_emphasis\_ attempt", Convert("<p>an _emphasis_ attempt</p>"));
    }

    [Theory]
    [InlineData("<p># not a heading</p>", @"\# not a heading")]
    [InlineData("<p>&gt; not a quote</p>", @"\> not a quote")]
    [InlineData("<p>- not a bullet</p>", @"\- not a bullet")]
    public void Convert_EscapesLeadingBlockMarkers(string html, string expected)
    {
        Assert.Equal(expected, Convert(html));
    }

    [Fact]
    public void Convert_EscapesOrderedMarkersOnTheDelimiter()
    {
        // A backslash before a digit is not an escape sequence, so "\1." would
        // render literally. The delimiter has to carry the escape instead.
        Assert.Equal(@"1\. not a list", Convert("<p>1. not a list</p>"));
    }

    [Fact]
    public void Convert_UsesALongerFence_WhenCodeContainsBackticks()
    {
        var markdown = Convert("<pre><code>a ``` b</code></pre>");

        Assert.StartsWith("````", markdown);
        Assert.Contains("a ``` b", markdown);
    }

    [Fact]
    public void Convert_UsesALongerDelimiter_ForInlineCodeContainingBackticks()
    {
        var markdown = Convert("<p><code>a ` b</code></p>");

        Assert.Equal("``a ` b``", markdown);
    }

    [Fact]
    public void Convert_PadsInlineCode_WhenItStartsOrEndsWithABacktick()
    {
        var markdown = Convert("<p><code>`x`</code></p>");

        Assert.Equal("`` `x` ``", markdown);
    }

    [Fact]
    public void Convert_ReadsUnicodeCheckboxesBackIntoTaskListSyntax()
    {
        var markdown = Convert("<ul><li>☑ done</li><li>☐ todo</li></ul>");

        Assert.Equal("- [x] done\n- [ ] todo", markdown);
    }

    [Fact]
    public void Convert_HandlesLineBreaksAndRules()
    {
        Assert.Equal("a\nb", Convert("a<br>b"));
        Assert.Equal("---", Convert("<hr>"));
    }

    [Fact]
    public void Convert_DecodesHtmlEntities()
    {
        Assert.Equal("a & b", Convert("<p>a &amp; b</p>"));
    }

    [Fact]
    public void Convert_CollapsesRunsOfBlankLines()
    {
        var markdown = Convert("<p>one</p><p>two</p>");

        Assert.Equal("one\n\ntwo", markdown);
        Assert.DoesNotContain("\n\n\n", markdown);
    }
}
