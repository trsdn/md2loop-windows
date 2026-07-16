using Windows.ApplicationModel.DataTransfer;

namespace md2loop;

/// <summary>
/// Windows clipboard operations for reading and writing HTML/RTF/text content.
/// </summary>
public static class ClipboardManager
{
    public static async Task<(string? Text, string? Html, string? Rtf)> ReadAsync()
    {
        var content = Clipboard.GetContent();
        string? text = null;
        string? html = null;
        string? rtf = null;

        if (content.Contains(StandardDataFormats.Text))
        {
            text = await content.GetTextAsync();
        }

        if (content.Contains(StandardDataFormats.Html))
        {
            var clipboardHtml = await content.GetHtmlFormatAsync();
            html = HtmlFormatHelper.GetStaticFragment(clipboardHtml);
            if (string.IsNullOrWhiteSpace(html))
                html = clipboardHtml;
        }

        if (content.Contains(StandardDataFormats.Rtf))
        {
            rtf = await content.GetRtfAsync();
        }

        return (text, html, rtf);
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

}
