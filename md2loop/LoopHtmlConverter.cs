using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace md2loop;

/// <summary>
/// Converts Markdown to Loop-optimized HTML (minimal, no CSS classes, Unicode checkboxes).
/// </summary>
public static class LoopHtmlConverter
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static string Convert(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var document = Markdown.Parse(markdown, Pipeline);

        // Transform task list items to use Unicode checkboxes
        TransformTaskLists(document);

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer)
        {
            ImplicitParagraph = false,
            EnableHtmlForBlock = true,
            EnableHtmlForInline = true
        };
        Pipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();

        var html = writer.ToString();

        // Post-process: replace checkbox inputs with Unicode chars for Loop compatibility
        html = html.Replace("<input checked=\"\" disabled=\"\" type=\"checkbox\" /> ", "☑ ");
        html = html.Replace("<input disabled=\"\" type=\"checkbox\" /> ", "☐ ");

        // Remove CSS classes that Markdig adds
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\s*class=""[^""]*""", "");

        return html.Trim();
    }

    private static void TransformTaskLists(MarkdownDocument document)
    {
        foreach (var block in document.Descendants())
        {
            if (block is not ListItemBlock listItem) continue;
            if (listItem.Count == 0) continue;

            var firstBlock = listItem[0];
            if (firstBlock is not ParagraphBlock paragraph) continue;
            if (paragraph.Inline?.FirstChild is not LiteralInline literal) continue;

            var content = literal.Content.ToString();
            if (content.StartsWith("[x] ") || content.StartsWith("[X] "))
            {
                literal.Content = new Markdig.Helpers.StringSlice("☑ " + content[4..]);
            }
            else if (content.StartsWith("[ ] "))
            {
                literal.Content = new Markdig.Helpers.StringSlice("☐ " + content[4..]);
            }
        }
    }
}
