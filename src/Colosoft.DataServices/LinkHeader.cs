using System.Linq;
using System.Text.RegularExpressions;

namespace Colosoft.DataServices
{
    public class LinkHeader
    {
        public string? FirstLink { get; set; }

        public string? PrevLink { get; set; }

        public string? NextLink { get; set; }

        public string? LastLink { get; set; }

        public static LinkHeader? LinksFromHeader(string? text)
        {
            LinkHeader? linkHeader = null;

            if (!string.IsNullOrWhiteSpace(text))
            {
                var matches = Regex.Matches(text, "\\<(?<link>([^\\>]*))\\>; rel=\"(?<rel>(.*?))\"[,\\ ]*", RegexOptions.IgnoreCase);

                if (matches != null && matches.Any())
                {
                    linkHeader = new LinkHeader();

#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions
                    foreach (Match match in matches)
                    {
                        var relMatch = match.Groups["rel"];
                        var linkMatch = match.Groups["link"];

                        if (relMatch.Success && linkMatch.Success)
                        {
                            string rel = relMatch.Value.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
                            string link = linkMatch.Value;

                            switch (rel)
                            {
                                case "FIRST":
                                    linkHeader.FirstLink = link;
                                    break;
                                case "PREV":
                                    linkHeader.PrevLink = link;
                                    break;
                                case "NEXT":
                                    linkHeader.NextLink = link;
                                    break;
                                case "LAST":
                                    linkHeader.LastLink = link;
                                    break;
                            }
                        }
                    }
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
                }
            }

            return linkHeader;
        }
    }
}
