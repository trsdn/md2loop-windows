using Markdig;
using Markdig.Extensions.TaskLists;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using System.Text.RegularExpressions;

namespace md2loop;

/// <summary>
/// Converts Markdown to Loop-optimized HTML (minimal, no CSS classes, Unicode checkboxes).
/// </summary>
public static partial class LoopHtmlConverter
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static string Convert(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var document = Markdown.Parse(markdown, Pipeline);

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer)
        {
            ImplicitParagraph = false,
            EnableHtmlForBlock = true,
            EnableHtmlForInline = true
        };
        Pipeline.Setup(renderer);

        // Loop does not render <input type="checkbox">, so task list items are
        // written as Unicode checkboxes instead.
        renderer.ObjectRenderers.Replace<HtmlTaskListRenderer>(new UnicodeTaskListRenderer());

        renderer.Render(document);
        writer.Flush();

        var html = writer.ToString();

        // Remove the CSS classes Markdig adds, except the language-* hint on
        // fenced code blocks: that is the only record of the code language, and
        // dropping it means a round-trip back to Markdown loses it.
        html = NonLanguageClassRegex().Replace(html, "");

        return html.Trim();
    }

    [GeneratedRegex(@"\s*class=""(?!language-)[^""]*""")]
    private static partial Regex NonLanguageClassRegex();

    private sealed class UnicodeTaskListRenderer : HtmlObjectRenderer<TaskList>
    {
        protected override void Write(HtmlRenderer renderer, TaskList obj)
        {
            renderer.Write(obj.Checked ? "☑" : "☐");
        }
    }
}
