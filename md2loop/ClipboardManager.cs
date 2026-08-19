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

    /// <summary>
    /// Reads the clipboard, returning <c>null</c> when it could not be read.
    /// The clipboard is a shared, single-owner OS resource, so any access can
    /// fail transiently; that is an expected outcome here rather than an error.
    /// </summary>
    public static Task<ClipboardSnapshot?> TryReadAsync() => TryAsync(ReadCoreAsync);

    private static async Task<ClipboardSnapshot?> ReadCoreAsync()
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
    /// Returns false if the clipboard could not be written.
    /// </summary>
    public static Task<bool> TryWriteForLoopAsync(string html, string markdown) =>
        TryWriteAsync(() =>
        {
            var dataPackage = new DataPackage();
            dataPackage.SetHtmlFormat(HtmlFormatHelper.CreateHtmlFormat(html));
            dataPackage.SetText(markdown);
            return dataPackage;
        });

    /// <summary>
    /// Writes plain markdown text to clipboard.
    /// Returns false if the clipboard could not be written.
    /// </summary>
    public static Task<bool> TryWriteMarkdownAsync(string markdown) =>
        TryWriteAsync(() =>
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(markdown);
            return dataPackage;
        });

    private static async Task<bool> TryWriteAsync(Func<DataPackage> build)
    {
        var written = await TryRunAsync(() => Clipboard.SetContent(build()));

        if (!written)
            return false;

        // Flush only makes the content outlive this process. If it fails the
        // user still has the content, so a failure here is not reported.
        await TryRunAsync(Clipboard.Flush);

        return true;
    }

    private static async Task<bool> TryRunAsync(Action operation)
    {
        var result = await TryAsync<bool>(() =>
        {
            operation();
            return Task.FromResult<bool?>(true);
        });

        return result == true;
    }

    /// <summary>
    /// Runs a clipboard operation, retrying briefly because another process may
    /// hold the clipboard open, and returning <c>null</c> if it keeps failing.
    /// Only clipboard interop runs in here, so this never hides an application bug.
    /// </summary>
    private static async Task<T?> TryAsync<T>(Func<Task<T?>> operation) where T : struct
    {
        const int attempts = 3;

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception) when (attempt < attempts - 1)
            {
                await Task.Delay(40 * (attempt + 1));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
