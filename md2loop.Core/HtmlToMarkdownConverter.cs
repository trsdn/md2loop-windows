using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace md2loop;

/// <summary>
/// Converts HTML (from Loop rich text) back to clean Markdown.
/// </summary>
public static partial class HtmlToMarkdownConverter
{
    public static string Convert(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var body = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;
        var sb = new StringBuilder();
        ConvertChildren(body, sb, listDepth: 0, orderedIndex: 0, inPre: false);

        var result = sb.ToString().Trim();
        // Collapse excessive newlines
        result = CollapseNewlinesRegex().Replace(result, "\n\n");
        return result;
    }

    private static void ConvertChildren(HtmlNode parent, StringBuilder sb, int listDepth, int orderedIndex, bool inPre)
    {
        foreach (var child in parent.ChildNodes)
        {
            ConvertNode(child, sb, listDepth, ref orderedIndex, inPre);
        }
    }

    private static void ConvertNode(HtmlNode node, StringBuilder sb, int listDepth, ref int orderedIndex, bool inPre)
    {
        if (node.NodeType == HtmlNodeType.Text)
        {
            var text = HtmlEntity.DeEntitize(node.InnerText);
            if (inPre)
            {
                sb.Append(text);
            }
            else
            {
                sb.Append(EscapeInline(CollapseWhitespaceRegex().Replace(text, " ")));
            }
            return;
        }

        if (node.NodeType != HtmlNodeType.Element)
            return;

        var tag = node.Name.ToLowerInvariant();

        switch (tag)
        {
            case "h1" or "h2" or "h3" or "h4" or "h5" or "h6":
                var level = int.Parse(tag[1..]);
                var prefix = new string('#', level);
                sb.Append($"{prefix} ");
                ConvertChildren(node, sb, listDepth, 0, false);
                sb.Append("\n\n");
                break;

            case "p":
                var paragraph = new StringBuilder();
                ConvertChildren(node, paragraph, listDepth, 0, false);
                sb.Append(EscapeLeadingBlockMarker(paragraph.ToString()));
                sb.Append("\n\n");
                break;

            case "strong" or "b":
                sb.Append("**");
                ConvertChildren(node, sb, listDepth, 0, inPre);
                sb.Append("**");
                break;

            case "em" or "i":
                sb.Append('*');
                ConvertChildren(node, sb, listDepth, 0, inPre);
                sb.Append('*');
                break;

            case "s" or "del" or "strike":
                sb.Append("~~");
                ConvertChildren(node, sb, listDepth, 0, inPre);
                sb.Append("~~");
                break;

            case "code":
                if (inPre)
                {
                    ConvertChildren(node, sb, listDepth, 0, true);
                }
                else
                {
                    // The delimiter has to be longer than any backtick run inside.
                    var inlineCode = HtmlEntity.DeEntitize(node.InnerText);
                    var delimiter = new string('`', LongestBacktickRun(inlineCode) + 1);
                    var padding = inlineCode.StartsWith('`') || inlineCode.EndsWith('`') ? " " : "";
                    sb.Append(delimiter);
                    sb.Append(padding);
                    sb.Append(inlineCode);
                    sb.Append(padding);
                    sb.Append(delimiter);
                }
                break;

            case "pre":
                // The language hint comes from a descendant <code>, but the whole
                // subtree is converted so nothing is dropped when the markup is
                // wrapped, split across several <code> elements, or absent entirely.
                var codeNode = node.SelectSingleNode(".//code");
                var lang = "";
                if (codeNode != null)
                {
                    var cls = codeNode.GetAttributeValue("class", "");
                    var match = LanguageClassRegex().Match(cls);
                    if (match.Success) lang = match.Groups[1].Value;
                }

                var codeBuilder = new StringBuilder();
                ConvertChildren(node, codeBuilder, listDepth, 0, true);
                var code = codeBuilder.ToString().Trim('\r', '\n');

                // A fence has to be longer than the longest backtick run it contains.
                var fence = new string('`', Math.Max(3, LongestBacktickRun(code) + 1));
                sb.Append($"\n{fence}{lang}\n");
                sb.Append(code);
                sb.Append($"\n{fence}\n\n");
                break;

            case "a":
                var href = node.GetAttributeValue("href", "");
                sb.Append('[');
                ConvertChildren(node, sb, listDepth, 0, inPre);
                sb.Append($"]({href})");
                break;

            case "img":
                var src = node.GetAttributeValue("src", "");
                var alt = node.GetAttributeValue("alt", "");
                sb.Append($"![{alt}]({src})");
                break;

            case "ul":
                foreach (var li in node.ChildNodes.Where(c => c.Name == "li"))
                {
                    int dummy = 0;
                    ConvertListItem(li, sb, listDepth, ref dummy, ordered: false);
                }
                break;

            case "ol":
                int olIdx = 1;
                foreach (var li in node.ChildNodes.Where(c => c.Name == "li"))
                {
                    ConvertListItem(li, sb, listDepth, ref olIdx, ordered: true);
                }
                break;

            case "table":
                ConvertTable(node, sb);
                break;

            case "blockquote":
                var bqSb = new StringBuilder();
                ConvertChildren(node, bqSb, listDepth, 0, false);
                var bqLines = bqSb.ToString().Trim().Split('\n');
                foreach (var line in bqLines)
                    sb.Append($"> {line}\n");
                sb.Append('\n');
                break;

            case "hr":
                sb.Append("\n---\n\n");
                break;

            case "br":
                sb.Append('\n');
                break;

            case "div":
                ConvertChildren(node, sb, listDepth, 0, inPre);
                sb.Append('\n');
                break;

            default:
                ConvertChildren(node, sb, listDepth, 0, inPre);
                break;
        }
    }

    private static void ConvertListItem(HtmlNode li, StringBuilder sb, int listDepth, ref int orderedIndex, bool ordered)
    {
        var indent = new string(' ', listDepth * 4);
        var text = GetItemText(li);

        // Check for task list (Unicode checkboxes from Loop)
        if (text.StartsWith("☑"))
        {
            var content = text[1..].Trim();
            sb.Append($"{indent}- [x] {content}\n");
        }
        else if (text.StartsWith("☐"))
        {
            var content = text[1..].Trim();
            sb.Append($"{indent}- [ ] {content}\n");
        }
        else if (ordered)
        {
            sb.Append($"{indent}{orderedIndex}. {text}\n");
            orderedIndex++;
        }
        else
        {
            sb.Append($"{indent}- {text}\n");
        }

        // Handle nested lists
        foreach (var child in GetNestedLists(li))
        {
            int nestedIdx = 1;
            foreach (var nestedLi in child.ChildNodes.Where(c => c.Name == "li"))
            {
                ConvertListItem(nestedLi, sb, listDepth + 1, ref nestedIdx, child.Name == "ol");
            }
        }
    }

    /// <summary>
    /// Markdown for the list item's own content. Nested lists are excluded
    /// because they are emitted separately as indented items.
    /// </summary>
    private static string GetItemText(HtmlNode li)
    {
        // Convert a copy with the nested lists detached so inline elements still
        // run through the normal conversion pipeline and keep their formatting.
        var clone = li.Clone();
        foreach (var nested in GetNestedLists(clone).ToList())
            nested.Remove();

        var sb = new StringBuilder();
        ConvertChildren(clone, sb, listDepth: 0, orderedIndex: 0, inPre: false);
        return EscapeLeadingBlockMarker(NormalizeSingleLine(sb.ToString()));
    }

    /// <summary>
    /// Markdown for a single table cell, forced onto one line so the pipe table
    /// stays well formed.
    /// </summary>
    private static string GetCellText(HtmlNode cell)
    {
        var sb = new StringBuilder();
        ConvertChildren(cell, sb, listDepth: 0, orderedIndex: 0, inPre: false);
        return NormalizeSingleLine(sb.ToString()).Replace("|", "\\|");
    }

    private static string NormalizeSingleLine(string value)
        => CollapseWhitespaceRegex().Replace(value, " ").Trim();

    /// <summary>
    /// Escapes characters in a text node that would otherwise be re-read as
    /// inline Markdown syntax. Underscores are only escaped at word boundaries,
    /// so identifiers such as file_name_here stay readable.
    /// </summary>
    private static string EscapeInline(string text)
    {
        if (text.Length == 0)
            return text;

        var sb = new StringBuilder(text.Length);

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            switch (c)
            {
                case '\\' or '`' or '*' or '[' or ']':
                    sb.Append('\\').Append(c);
                    break;

                case '_' when IsWordBoundary(text, i):
                    sb.Append("\\_");
                    break;

                case '~' when i + 1 < text.Length && text[i + 1] == '~':
                    sb.Append("\\~\\~");
                    i++;
                    break;

                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private static bool IsWordBoundary(string text, int index)
    {
        var before = index > 0 && char.IsLetterOrDigit(text[index - 1]);
        var after = index + 1 < text.Length && char.IsLetterOrDigit(text[index + 1]);
        return !before || !after;
    }

    /// <summary>
    /// Escapes a leading character that would turn a paragraph into a heading,
    /// list item, blockquote or thematic break.
    /// </summary>
    private static string EscapeLeadingBlockMarker(string text)
    {
        var match = LeadingBlockMarkerRegex().Match(text);
        if (!match.Success)
            return text;

        // An ordered marker is neutralised on its delimiter ("1\." ), because a
        // backslash before a digit is not an escape sequence and would render.
        var punct = match.Groups["punct"];
        var escapeAt = punct.Success ? punct.Index : match.Groups["marker"].Index;

        return string.Concat(text.AsSpan(0, escapeAt), "\\", text.AsSpan(escapeAt));
    }

    private static int LongestBacktickRun(string value)
    {
        var longest = 0;
        var current = 0;

        foreach (var c in value)
        {
            if (c == '`')
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 0;
            }
        }

        return longest;
    }

    /// <summary>
    /// Nested lists, which may be wrapped in an intermediate element such as a
    /// div or paragraph. Descends only through non-list elements so each list is
    /// yielded once, at its outermost position.
    /// </summary>
    private static IEnumerable<HtmlNode> GetNestedLists(HtmlNode li)
    {
        foreach (var child in li.ChildNodes)
        {
            if (child.NodeType != HtmlNodeType.Element)
                continue;

            if (IsList(child))
            {
                yield return child;
            }
            else
            {
                foreach (var nested in GetNestedLists(child))
                    yield return nested;
            }
        }
    }

    private static bool IsList(HtmlNode node)
        => node.Name is "ul" or "ol";

    private static void ConvertTable(HtmlNode table, StringBuilder sb)
    {
        var rows = new List<string[]>();

        var thead = table.SelectSingleNode("thead");
        if (thead != null)
        {
            foreach (var tr in thead.SelectNodes("tr") ?? Enumerable.Empty<HtmlNode>())
            {
                var cells = tr.SelectNodes("th|td")?.Select(GetCellText).ToArray();
                if (cells != null) rows.Add(cells);
            }
        }

        var tbody = table.SelectSingleNode("tbody") ?? table;
        foreach (var tr in tbody.SelectNodes("tr") ?? Enumerable.Empty<HtmlNode>())
        {
            var cells = tr.SelectNodes("th|td")?.Select(GetCellText).ToArray();
            if (cells is { Length: > 0 }) rows.Add(cells);
        }

        if (rows.Count == 0) return;

        var colCount = rows.Max(r => r.Length);
        var colWidths = new int[colCount];
        foreach (var row in rows)
        {
            for (int i = 0; i < row.Length && i < colCount; i++)
                colWidths[i] = Math.Max(colWidths[i], Math.Max(row[i].Length, 3));
        }

        string FormatRow(string[] cells)
        {
            var padded = new string[colCount];
            for (int i = 0; i < colCount; i++)
            {
                var cell = i < cells.Length ? cells[i] : "";
                padded[i] = cell.PadRight(colWidths[i]);
            }
            return "| " + string.Join(" | ", padded) + " |";
        }

        sb.AppendLine(FormatRow(rows[0]));
        sb.AppendLine("| " + string.Join(" | ", colWidths.Select(w => new string('-', w))) + " |");
        foreach (var row in rows.Skip(1))
            sb.AppendLine(FormatRow(row));
        sb.Append('\n');
    }

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex CollapseNewlinesRegex();

    [GeneratedRegex(@"language-(\S+)")]
    private static partial Regex LanguageClassRegex();

    [GeneratedRegex(@"^\s{0,3}(?:\d+(?<punct>[.)])(?=\s|$)|(?<marker>#{1,6}(?=\s|$)|>|[-+](?=\s|$)|-{3,}\s*$|={3,}\s*$))")]
    private static partial Regex LeadingBlockMarkerRegex();

    [GeneratedRegex(@"[\s]+")]
    private static partial Regex CollapseWhitespaceRegex();
}
