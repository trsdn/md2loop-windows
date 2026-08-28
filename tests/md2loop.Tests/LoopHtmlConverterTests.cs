using md2loop;

namespace md2loop.Tests;

public class LoopHtmlConverterTests
{
    [Fact]
    public void Convert_ReturnsEmpty_ForBlankInput()
    {
        Assert.Equal(string.Empty, LoopHtmlConverter.Convert(""));
        Assert.Equal(string.Empty, LoopHtmlConverter.Convert("   \n  "));
    }

    [Fact]
    public void Convert_RendersHeadingsAndInlineFormatting()
    {
        var html = LoopHtmlConverter.Convert("# Title\n\nSome **bold** and *italic* text.");

        // AutoIdentifiers adds an id attribute to headings, so match on the content.
        Assert.Contains(">Title</h1>", html);
        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<em>italic</em>", html);
    }

    [Fact]
    public void Convert_RendersLists()
    {
        var html = LoopHtmlConverter.Convert("- one\n- two");

        Assert.Contains("<ul>", html);
        Assert.Contains("<li>one</li>", html);
    }

    [Fact]
    public void Convert_RendersTables_FromAdvancedExtensions()
    {
        var html = LoopHtmlConverter.Convert("| a | b |\n| --- | --- |\n| 1 | 2 |");

        Assert.Contains("<table>", html);
        Assert.Contains("<th>a</th>", html);
    }

    // Regression tests for #34. The class-stripping regex used to be blanket and
    // removed language-*, which is the only record of a code block's language.
    [Fact]
    public void Convert_KeepsTheCodeLanguageClass()
    {
        var html = LoopHtmlConverter.Convert("```csharp\nvar x = 1;\n```");

        Assert.Contains("class=\"language-csharp\"", html);
    }

    [Fact]
    public void Convert_StripsNonLanguageCssClasses()
    {
        // A custom container renders <div class="warning">, which Loop cannot use.
        var html = LoopHtmlConverter.Convert(":::warning\nheads up\n:::");

        Assert.DoesNotContain("class=\"warning\"", html);
        Assert.Contains("heads up", html);
    }

    // Regression tests for #21. Markdig renders task lists as <input type="checkbox">,
    // which Loop does not display, and the previous string replacement never matched
    // the markup Markdig actually emits.
    [Fact]
    public void Convert_RendersTaskListsAsUnicodeCheckboxes()
    {
        var html = LoopHtmlConverter.Convert("- [x] done\n- [ ] todo");

        Assert.Contains("☑", html);
        Assert.Contains("☐", html);
    }

    [Fact]
    public void Convert_NeverEmitsCheckboxInputs()
    {
        var html = LoopHtmlConverter.Convert("- [x] done\n- [ ] todo");

        Assert.DoesNotContain("<input", html);
        Assert.DoesNotContain("checkbox", html);
    }

    [Fact]
    public void Convert_KeepsTaskListItemTextAlongsideTheCheckbox()
    {
        var html = LoopHtmlConverter.Convert("- [x] ship it");

        Assert.Contains("☑", html);
        Assert.Contains("ship it", html);
    }
}
