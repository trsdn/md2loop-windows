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
    public static ClipboardMode Detect(string? text, string? html, string? rtf = null)
    {
        var hasText = !string.IsNullOrWhiteSpace(text);
        var hasHtml = !string.IsNullOrWhiteSpace(html);
        var hasRtf = ContainsRTF(rtf);

        if (!hasText && !hasHtml && !hasRtf)
            return ClipboardMode.Unknown;

        if (hasHtml)
        {
            var markdownScore = GetMarkdownScore(text);
            var richTextScore = GetRichTextScore(html!);

            // Editors often publish syntax-colored HTML alongside the original Markdown.
            // Prefer the text only when its Markdown signals are stronger than the HTML semantics.
            if (markdownScore >= 3 && markdownScore > richTextScore)
                return ClipboardMode.Markdown;

            return ClipboardMode.RichText;
        }

        if (hasRtf)
            return ClipboardMode.RichText;

        // Plain text is valid Markdown input even when it has no special syntax.
        return hasText ? ClipboardMode.Markdown : ClipboardMode.Unknown;
    }

    public static bool ContainsHTML(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return false;

        return HtmlTagRegex().IsMatch(html);
    }

    public static bool ContainsRTF(string? rtf)
    {
        if (string.IsNullOrWhiteSpace(rtf))
            return false;

        return rtf.TrimStart().StartsWith(@"{\rtf", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsMarkdown(string? text)
        => GetMarkdownScore(text) >= 3;

    private static int GetMarkdownScore(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var trimmed = text.Trim();
        if (ContainsHTML(trimmed))
            return 0;

        var lines = trimmed.ReplaceLineEndings("\n").Split('\n');
        var score = 0;
        var hasSetextHeading = HasSetextHeading(lines);
        var hasTaskList = lines.Any(l => TaskListRegex().IsMatch(l));

        if (lines.Any(l => HeadingRegex().IsMatch(l)) || hasSetextHeading) score += 6;
        if (hasTaskList) score += 6;
        if (lines.Any(l => CodeFenceRegex().IsMatch(l))) score += 6;
        if (HasMarkdownTable(lines)) score += 6;
        if (!hasTaskList && lines.Any(l => UnorderedListRegex().IsMatch(l))) score += 3;
        if (lines.Any(l => OrderedListRegex().IsMatch(l))) score += 3;
        if (lines.Any(l => BlockquoteRegex().IsMatch(l))) score += 3;
        if (!hasSetextHeading && lines.Any(l => HorizontalRuleRegex().IsMatch(l))) score += 3;
        if (LinkRegex().IsMatch(trimmed)) score += 4;
        if (InlineCodeRegex().IsMatch(trimmed)) score += 3;
        if (BoldRegex().IsMatch(trimmed)) score += 4;
        if (ItalicRegex().IsMatch(trimmed)) score += 3;
        if (StrikethroughRegex().IsMatch(trimmed)) score += 3;

        return score;
    }

    private static int GetRichTextScore(string html)
    {
        var score = 0;

        if (RichHeadingRegex().IsMatch(html)) score += 6;
        if (RichListRegex().IsMatch(html)) score += 7;
        if (RichTableRegex().IsMatch(html)) score += 6;
        if (RichBlockquoteRegex().IsMatch(html)) score += 4;
        if (RichPreformattedRegex().IsMatch(html)) score += 5;
        if (RichStrongRegex().IsMatch(html)) score += 4;
        if (RichEmphasisRegex().IsMatch(html)) score += 3;
        if (RichLinkOrImageRegex().IsMatch(html)) score += 4;
        if (RichCodeRegex().IsMatch(html)) score += 3;
        if (RichStrikethroughRegex().IsMatch(html)) score += 3;
        if (RichFormattingStyleRegex().IsMatch(html)) score += 3;
        if (HtmlWrapperRegex().IsMatch(html)) score += 1;

        return score;
    }

    private static bool HasSetextHeading(string[] lines)
    {
        for (var i = 1; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i - 1]) && SetextHeadingRegex().IsMatch(lines[i]))
                return true;
        }

        return false;
    }

    private static bool HasMarkdownTable(string[] lines)
    {
        for (var i = 1; i < lines.Length; i++)
        {
            if (TableSeparatorRegex().IsMatch(lines[i]) && lines[i - 1].Contains('|'))
                return true;
        }

        return false;
    }

    [GeneratedRegex(@"<(html|body|article|section|main|p|div|span|h[1-6]|ul|ol|li|dl|dt|dd|table|thead|tbody|tfoot|tr|td|th|blockquote|pre|code|strong|b|em|i|s|del|strike|a|img|br|hr)\b[^>]*>", RegexOptions.IgnoreCase)]
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

    [GeneratedRegex(@"^\s{0,3}(=+|-+)\s*$")]
    private static partial Regex SetextHeadingRegex();

    [GeneratedRegex(@"^\s{0,3}((\*\s*){3,}|(-\s*){3,}|(_\s*){3,})$")]
    private static partial Regex HorizontalRuleRegex();

    [GeneratedRegex(@"^\s{0,3}\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)+\|?\s*$")]
    private static partial Regex TableSeparatorRegex();

    [GeneratedRegex(@"!?\[[^\]\n]*\]\([^)]+\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"`[^`\n]+`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"(\*\*|__)(?=\S)(.+?)(?<=\S)\1")]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"(?:^|[\s(])\*(?=\S)([^*\n]*?\S)\*(?=$|[\s).,!?:;])|(?:^|[\s(])_(?=\S)([^_\n]*?\S)_(?=$|[\s).,!?:;])")]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"~~(?=\S)(.+?)(?<=\S)~~")]
    private static partial Regex StrikethroughRegex();

    [GeneratedRegex(@"<h[1-6]\b", RegexOptions.IgnoreCase)]
    private static partial Regex RichHeadingRegex();

    [GeneratedRegex(@"<(ul|ol|li)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RichListRegex();

    [GeneratedRegex(@"<(table|thead|tbody|tfoot|tr|td|th)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RichTableRegex();

    [GeneratedRegex(@"<blockquote\b", RegexOptions.IgnoreCase)]
    private static partial Regex RichBlockquoteRegex();

    [GeneratedRegex(@"<pre\b", RegexOptions.IgnoreCase)]
    private static partial Regex RichPreformattedRegex();

    [GeneratedRegex(@"<(strong|b)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RichStrongRegex();

    [GeneratedRegex(@"<(em|i)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RichEmphasisRegex();

    [GeneratedRegex(@"<(a|img)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RichLinkOrImageRegex();

    [GeneratedRegex(@"<code\b", RegexOptions.IgnoreCase)]
    private static partial Regex RichCodeRegex();

    [GeneratedRegex(@"<(s|del|strike)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RichStrikethroughRegex();

    [GeneratedRegex(@"style\s*=\s*[""'][^""']*(font-weight\s*:\s*(bold|[6-9]00)|font-style\s*:\s*italic|text-decoration[^;""']*(underline|line-through))", RegexOptions.IgnoreCase)]
    private static partial Regex RichFormattingStyleRegex();

    [GeneratedRegex(@"<(p|div|span|br)\b", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlWrapperRegex();
}
