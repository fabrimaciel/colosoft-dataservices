namespace Colosoft.DataServices
{
    internal static class SortedPageResult
    {
        public static readonly System.Text.RegularExpressions.Regex SortRegex = new System.Text.RegularExpressions.Regex("(sort=)([^&]+)");
    }
}
