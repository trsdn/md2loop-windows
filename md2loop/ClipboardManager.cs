using Windows.ApplicationModel.DataTransfer;

namespace md2loop;

/// <summary>
/// Windows clipboard operations for reading and writing HTML/RTF/text content.
/// </summary>
public static class ClipboardManager
{
    public static async Task<(string? Text, string? Html)> ReadAsync()
    {
        var content = Clipboard.GetContent();
        string? text = null;
        string? html = null;

        if (content.Contains(StandardDataFormats.Html))
        {
            html = await content.GetHtmlFormatAsync();
            // Windows HTML clipboard format includes headers — extract just the HTML fragment
            html = ExtractHtmlFragment(html);
        }

        if (content.Contains(StandardDataFormats.Text))
        {
            text = await content.GetTextAsync();
        }

        return (text, html);
    }

    /// <summary>
    /// Writes converted content to clipboard with HTML + text for maximum Loop compatibility.
    /// </summary>
    public static void WriteForLoop(string html, string markdown)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetHtmlFormat(HtmlFormatHelper.CreateHtmlFormat(html));
        dataPackage.SetText(markdown);
        Clipboard.SetContent(dataPackage);
        Clipboard.Flush();
    }

    /// <summary>
    /// Writes plain markdown text to clipboard.
    /// </summary>
    public static void WriteMarkdown(string markdown)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(markdown);
        Clipboard.SetContent(dataPackage);
        Clipboard.Flush();
    }

    /// <summary>
    /// Extract HTML fragment from Windows clipboard HTML format (which includes headers).
    /// </summary>
    private static string ExtractHtmlFragment(string? clipboardHtml)
    {
        if (string.IsNullOrEmpty(clipboardHtml))
            return string.Empty;

        // Windows CF_HTML format has StartFragment/EndFragment markers
        const string startMarker = "<!--StartFragment-->";
        const string endMarker = "<!--EndFragment-->";

        var startIdx = clipboardHtml.IndexOf(startMarker, StringComparison.Ordinal);
        var endIdx = clipboardHtml.IndexOf(endMarker, StringComparison.Ordinal);

        if (startIdx >= 0 && endIdx > startIdx)
        {
            return clipboardHtml[(startIdx + startMarker.Length)..endIdx].Trim();
        }

        return clipboardHtml;
    }
}
