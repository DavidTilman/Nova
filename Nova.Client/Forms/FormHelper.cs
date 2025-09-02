using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nova.Client.Forms;
internal partial class FormHelper
{
    public static int? ExtractID(string formattedAccountString)
    {
        if (string.IsNullOrWhiteSpace(formattedAccountString))
            return null;

        Match match = NumberContainedInSquareBrackets().Match(formattedAccountString);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    [GeneratedRegex(@"\[(\d+)\]")]
    private static partial Regex NumberContainedInSquareBrackets();
}
