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
                sb.Append(CollapseWhitespaceRegex().Replace(text, " "));
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
                ConvertChildren(node, sb, listDepth, 0, false);
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
                    sb.Append('`');
                    sb.Append(HtmlEntity.DeEntitize(node.InnerText));
                    sb.Append('`');
                }
                break;

            case "pre":
                var codeNode = node.SelectSingleNode("code");
                var lang = "";
                if (codeNode != null)
                {
                    var cls = codeNode.GetAttributeValue("class", "");
                    if (cls.Contains("language-"))
                    {
                        var match = Regex.Match(cls, @"language-(\S+)");
                        if (match.Success) lang = match.Groups[1].Value;
                    }
                }
                sb.Append($"\n```{lang}\n");
                ConvertChildren(codeNode ?? node, sb, listDepth, 0, true);
                sb.Append("\n```\n\n");
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
        var text = HtmlEntity.DeEntitize(li.InnerText).Trim();

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
        foreach (var child in li.ChildNodes.Where(c => c.Name is "ul" or "ol"))
        {
            int nestedIdx = 1;
            foreach (var nestedLi in child.ChildNodes.Where(c => c.Name == "li"))
            {
                ConvertListItem(nestedLi, sb, listDepth + 1, ref nestedIdx, child.Name == "ol");
            }
        }
    }

    private static void ConvertTable(HtmlNode table, StringBuilder sb)
    {
        var rows = new List<string[]>();

        var thead = table.SelectSingleNode("thead");
        if (thead != null)
        {
            foreach (var tr in thead.SelectNodes("tr") ?? Enumerable.Empty<HtmlNode>())
            {
                var cells = tr.SelectNodes("th|td")?.Select(c => HtmlEntity.DeEntitize(c.InnerText).Trim()).ToArray();
                if (cells != null) rows.Add(cells);
            }
        }

        var tbody = table.SelectSingleNode("tbody") ?? table;
        foreach (var tr in tbody.SelectNodes("tr") ?? Enumerable.Empty<HtmlNode>())
        {
            var cells = tr.SelectNodes("th|td")?.Select(c => HtmlEntity.DeEntitize(c.InnerText).Trim()).ToArray();
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

    [GeneratedRegex(@"[\s]+")]
    private static partial Regex CollapseWhitespaceRegex();
}
