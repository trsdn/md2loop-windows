using System.Text.RegularExpressions;

namespace md2loop;

public enum ClipboardMode
{
    Markdown,
    RichText,
    Unknown
}

public static partial class ClipboardContentDetector
{
    public static ClipboardMode Detect(string? text, string? html)
    {
        if (ContainsHTML(html))
            return ClipboardMode.RichText;

        if (IsMarkdown(text))
            return ClipboardMode.Markdown;

        return ClipboardMode.Unknown;
    }

    public static bool ContainsHTML(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return false;

        return HtmlTagRegex().IsMatch(html);
    }

    public static bool IsMarkdown(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        if (ContainsHTML(trimmed))
            return false;

        var lines = trimmed.Split('\n');

        // Headings
        if (lines.Any(l => HeadingRegex().IsMatch(l))) return true;
        // Unordered list
        if (lines.Any(l => UnorderedListRegex().IsMatch(l))) return true;
        // Ordered list
        if (lines.Any(l => OrderedListRegex().IsMatch(l))) return true;
        // Task list
        if (lines.Any(l => TaskListRegex().IsMatch(l))) return true;
        // Blockquote
        if (lines.Any(l => BlockquoteRegex().IsMatch(l))) return true;
        // Code fence
        if (lines.Any(l => CodeFenceRegex().IsMatch(l))) return true;
        // Table
        if (HasMarkdownTable(lines)) return true;
        // Links/images
        if (LinkRegex().IsMatch(trimmed)) return true;
        // Inline code
        if (InlineCodeRegex().IsMatch(trimmed)) return true;
        // Bold
        if (BoldRegex().IsMatch(trimmed)) return true;
        // Italic
        if (ItalicRegex().IsMatch(trimmed)) return true;

        return false;
    }

    private static bool HasMarkdownTable(string[] lines)
    {
        bool hasPipeRow = lines.Any(l =>
        {
            var t = l.Trim();
            return t.StartsWith('|') && t.EndsWith('|');
        });
        bool hasSeparator = lines.Any(l => TableSeparatorRegex().IsMatch(l));
        return hasPipeRow && hasSeparator;
    }

    [GeneratedRegex(@"<(html|body|p|div|span|h[1-6]|ul|ol|li|table|tr|td|th|blockquote|pre|code|strong|b|em|i|a|br|hr)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s+\S")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^\s{0,3}[-*+]\s+\S")]
    private static partial Regex UnorderedListRegex();

    [GeneratedRegex(@"^\s{0,3}\d+[.)]\s+\S")]
    private static partial Regex OrderedListRegex();

    [GeneratedRegex(@"^\s{0,3}[-*+]\s+\[[ xX]\]\s+\S")]
    private static partial Regex TaskListRegex();

    [GeneratedRegex(@"^\s{0,3}>\s+\S")]
    private static partial Regex BlockquoteRegex();

    [GeneratedRegex(@"^\s*(```|~~~)")]
    private static partial Regex CodeFenceRegex();

    [GeneratedRegex(@"^\s{0,3}\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)+\|?\s*$")]
    private static partial Regex TableSeparatorRegex();

    [GeneratedRegex(@"!?\[[^\]\n]+\]\([^)]+\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"`[^`\n]+`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"(\*\*|__)[^\s].*?[^\s]\1")]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"(^|[\s(])\*[^\s*][^*\n]*[^\s*]\*($|[\s).,!?:;])")]
    private static partial Regex ItalicRegex();
}
