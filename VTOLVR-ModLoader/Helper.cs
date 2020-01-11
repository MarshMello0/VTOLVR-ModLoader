using System.Text.RegularExpressions;

public static class Helper
{
    public static string ClearSpaces(string input)
    {
        return Regex.Replace(input, @"\s+", "");
    }
}