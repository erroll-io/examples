namespace MinimalApi;

public static class StringLogic
{
    public static string TrimStart(this string target, string trimString)
    {
        if (string.IsNullOrEmpty(trimString))
            return target;

        var result = target;

        while (result.StartsWith(trimString))
        {
            result = result.Substring(trimString.Length);
        }

        return result;
    }
}
