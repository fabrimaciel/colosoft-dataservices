using System;
using System.Collections.Generic;
using System.Text;

namespace Colosoft.DataServices
{
    internal static class SortedResultConstants
    {
        internal static readonly System.Text.RegularExpressions.Regex SortRegex = new System.Text.RegularExpressions.Regex("(sort=)([^&]+)");
    }
}
