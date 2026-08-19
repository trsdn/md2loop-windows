using Windows.ApplicationModel.DataTransfer;

namespace md2loop;

/// <summary>
/// A point-in-time view of the clipboard.
/// </summary>
public readonly record struct ClipboardSnapshot(string? Text, string? Html, string? Rtf, bool IsExcluded)
{
    /// <summary>
    /// Content the source app asked monitoring tools not to process.
    /// </summary>
    public static ClipboardSnapshot Excluded { get; } = new(null, null, null, true);
}

/// <summary>
/// Windows clipboard operations for reading and writing HTML/RTF/text content.
/// </summary>
public static class ClipboardManager
{
    // Set by password managers and similar apps to opt out of clipboard monitoring.
    private const string ExcludeFromMonitoringFormat = "ExcludeClipboardContentFromMonitorProcessing";
    private const string CanIncludeInHistoryProperty = "CanIncludeInClipboardHistory";

    public static async Task<ClipboardSnapshot> ReadAsync()
    {
        var content = Clipboard.GetContent();

        if (IsExcludedFromMonitoring(content))
            return ClipboardSnapshot.Excluded;

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

        return new ClipboardSnapshot(text, html, rtf, IsExcluded: false);
    }

    /// <summary>
    /// Whether the source app marked the content as private. Such content is not
    /// read at all, so passwords and similar secrets never enter this process.
    /// </summary>
    private static bool IsExcludedFromMonitoring(DataPackageView content)
    {
        if (content.Contains(ExcludeFromMonitoringFormat))
            return true;

        return content.Properties.TryGetValue(CanIncludeInHistoryProperty, out var value)
            && value is bool canInclude
            && !canInclude;
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
