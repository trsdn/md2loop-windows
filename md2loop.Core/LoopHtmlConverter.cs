using Markdig;
using Markdig.Extensions.TaskLists;
using Markdig.Renderers;
using Markdig.Renderers.Html;

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

        // Remove CSS classes that Markdig adds
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\s*class=""[^""]*""", "");

        return html.Trim();
    }

    private sealed class UnicodeTaskListRenderer : HtmlObjectRenderer<TaskList>
    {
        protected override void Write(HtmlRenderer renderer, TaskList obj)
        {
            renderer.Write(obj.Checked ? "☑" : "☐");
        }
    }
}
