using System.Text;
using System.Xml;
using RtfPipe;

namespace md2loop;

/// <summary>
/// Converts Windows clipboard RTF to Markdown through HTML.
/// </summary>
public static class RtfToMarkdownConverter
{
    static RtfToMarkdownConverter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static bool TryConvert(string? rtf, out string markdown)
    {
        markdown = string.Empty;
        if (!ClipboardContentDetector.ContainsRTF(rtf))
            return false;

        try
        {
            var html = Rtf.ToHtml(rtf!);
            markdown = HtmlToMarkdownConverter.Convert(html);
            return !string.IsNullOrWhiteSpace(markdown);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (XmlException)
        {
            return false;
        }
    }
}
