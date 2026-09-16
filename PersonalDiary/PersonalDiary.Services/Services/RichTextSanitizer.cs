using System.Net;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace PersonalDiary.Services;

public static partial class RichTextSanitizer
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "b", "blockquote", "br", "div", "em", "font", "h1", "h2", "h3", "h4", "h5", "h6", "img",
        "i", "li", "ol", "p", "s", "span", "strong", "u", "ul"
    };

    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        html = UnsafeBlockRegex().Replace(html, string.Empty);
        return TokenRegex().Replace(html, match =>
            match.Value.StartsWith("<", StringComparison.Ordinal)
                ? SanitizeTag(match.Value)
                : HtmlEncoder.Default.Encode(WebUtility.HtmlDecode(match.Value)));
    }

    public static string ToPlainText(string? html)
    {
        var sanitized = Sanitize(html);
        return WebUtility.HtmlDecode(TagRegex().Replace(sanitized, " "));
    }

    private static string SanitizeTag(string tag)
    {
        var match = TagRegex().Match(tag);
        if (!match.Success || !AllowedTags.Contains(match.Groups["name"].Value))
        {
            return string.Empty;
        }

        var name = match.Groups["name"].Value.ToLowerInvariant();
        if (tag.StartsWith("</", StringComparison.Ordinal))
        {
            return $"</{name}>";
        }

        var attributes = new List<string>();
        foreach (Match attribute in AttributeRegex().Matches(match.Groups["attributes"].Value))
        {
            var attributeName = attribute.Groups["name"].Value.ToLowerInvariant();
            var attributeValue = WebUtility.HtmlDecode(attribute.Groups["value"].Value);
            var safeValue = attributeName switch
            {
                "href" when name == "a" && IsSafeUrl(attributeValue) => attributeValue,
                "src" when name == "img" && IsSafeImageDataUrl(attributeValue) => attributeValue,
                "alt" when name == "img" => attributeValue,
                "style" => SanitizeStyle(attributeValue),
                "face" when name == "font" => attributeValue,
                "size" when name == "font" && Regex.IsMatch(attributeValue, "^[1-7]$") => attributeValue,
                "color" when name == "font" && Regex.IsMatch(attributeValue, "^(#[0-9a-f]{3,8}|[a-z]+)$", RegexOptions.IgnoreCase) => attributeValue,
                _ => null
            };

            if (!string.IsNullOrEmpty(safeValue))
            {
                attributes.Add($"{attributeName}=\"{HtmlEncoder.Default.Encode(safeValue)}\"");
            }
        }

        return attributes.Count == 0
            ? $"<{name}{(name == "br" ? " /" : string.Empty)}>"
            : $"<{name} {string.Join(" ", attributes)}{(name == "br" ? " /" : string.Empty)}>";
    }

    private static string SanitizeStyle(string style)
    {
        var safeStyles = new List<string>();
        foreach (Match property in StylePropertyRegex().Matches(style))
        {
            safeStyles.Add($"{property.Groups["name"].Value.ToLowerInvariant()}:{property.Groups["value"].Value.Trim()}");
        }

        return string.Join(";", safeStyles);
    }

    private static bool IsSafeUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSafeImageDataUrl(string value)
    {
        return value.Length <= 3_000_000
            && ImageDataUrlRegex().IsMatch(value);
    }

    [GeneratedRegex("<(script|style|iframe|object|embed)\\b[^>]*>.*?</\\1\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex UnsafeBlockRegex();

    [GeneratedRegex("<!--.*?-->|<[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TokenRegex();

    [GeneratedRegex("^\\s*</?\\s*(?<name>[a-z][a-z0-9]*)\\b(?<attributes>[^>]*)>\\s*$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TagRegex();

    [GeneratedRegex("(?<name>[a-z][a-z0-9:-]*)\\s*=\\s*[\\\"'](?<value>.*?)[\\\"']", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex AttributeRegex();

    [GeneratedRegex("(?<name>font-family|font-size|color|background-color|text-align)\\s*:\\s*(?<value>[#a-z0-9(),.%\\s-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex StylePropertyRegex();

    [GeneratedRegex("^data:image/(png|jpeg|gif|webp);base64,[a-z0-9+/]+={0,2}$", RegexOptions.IgnoreCase)]
    private static partial Regex ImageDataUrlRegex();
}
