using System;
using System.Collections.Generic;
using System.Text;

namespace SmartBudget.WinForms.Quicken;

public static class OfxSgmlToXml
{
    public static string ConvertToXml(string ofxText)
    {
        // Drop header lines until the first <OFX>
        var start = ofxText.IndexOf("<OFX", StringComparison.OrdinalIgnoreCase);
        if (start < 0) throw new FormatException("No <OFX> root found.");
        var s = ofxText[start..];

        var sb = new StringBuilder(s.Length + 128);
        var i = 0;

        while (i < s.Length)
        {
            if (s[i] != '<')
            {
                sb.Append(s[i++]);
                continue;
            }

            // Read tag
            var tagEnd = s.IndexOf('>', i);
            if (tagEnd < 0) break;

            var rawTag = s.Substring(i + 1, tagEnd - i - 1).Trim();
            var isClose = rawTag.StartsWith("/", StringComparison.Ordinal);
            var tagName = isClose ? rawTag[1..].Trim() : rawTag;

            sb.Append('<').Append(rawTag).Append('>');
            i = tagEnd + 1;

            if (isClose) continue;

            // If next non-whitespace char is '<', it's an aggregate tag (no inline value)
            var j = i;
            while (j < s.Length && char.IsWhiteSpace(s[j])) j++;
            if (j < s.Length && s[j] == '<') continue;

            // Inline value: read until next '<', then auto-close
            var nextTag = s.IndexOf('<', i);
            if (nextTag < 0) nextTag = s.Length;

            var value = s.Substring(i, nextTag - i);
            sb.Append(System.Security.SecurityElement.Escape(value.Trim()) ?? string.Empty);
            sb.Append("</").Append(tagName).Append('>');

            i = nextTag;
        }

        return sb.ToString();
    }
}

