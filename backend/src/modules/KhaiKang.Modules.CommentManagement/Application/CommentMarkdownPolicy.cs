using System.Text.RegularExpressions;

namespace KhaiKang.Modules.CommentManagement.Application;

public static class CommentMarkdownPolicy
{
    private static readonly Regex FencedCode = new(
        "```[\\s\\S]*?```|~~~[\\s\\S]*?~~~",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex InlineCode = new(
        "`[^`\\r\\n]*`",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex RawHtml = new(
        "<\\s*/?\\s*[a-z][^>]*>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex MarkdownDestination = new(
        "(?<image>!)?\\[[^\\]]*\\]\\((?<destination>[^)\\s]+)(?:\\s+[^)]*)?\\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex WindowsPath = new(
        "(?:^|\\s)(?:[a-z]:[\\\\/]|\\\\\\\\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex AuthorizedAttachment = new(
        "^/api/v1/(?:projects/[0-9a-f-]+/issues/[0-9a-f-]+/attachments/[0-9a-f-]+/content|test-workspaces/[0-9a-f-]+/cases/[0-9a-f-]+/attachments/[0-9a-f-]+/content|test-workspaces/[0-9a-f-]+/runs/[0-9a-f-]+/items/[0-9a-f-]+/attachments/[0-9a-f-]+/content)(?:\\?version=\\d+)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static string? Validate(string content)
    {
        var prose = InlineCode.Replace(FencedCode.Replace(content, string.Empty), string.Empty);
        if (RawHtml.IsMatch(prose))
        {
            return "Raw HTML and embedded content are not allowed.";
        }

        if (WindowsPath.IsMatch(prose) ||
            prose.Contains("file:", StringComparison.OrdinalIgnoreCase) ||
            prose.Contains("data:", StringComparison.OrdinalIgnoreCase))
        {
            return "Data URLs and local file paths are not allowed.";
        }

        foreach (Match match in MarkdownDestination.Matches(prose))
        {
            var destination = match.Groups["destination"].Value.Trim('<', '>');
            var isAuthorizedAttachment = AuthorizedAttachment.IsMatch(destination);
            var isImage = match.Groups["image"].Success;
            if (isImage && !isAuthorizedAttachment)
            {
                return "Images must reference an authorized KhaiKang attachment.";
            }

            if (!isAuthorizedAttachment &&
                !destination.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !destination.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return "Links must use HTTP(S) or reference an authorized KhaiKang attachment.";
            }
        }

        return null;
    }
}
