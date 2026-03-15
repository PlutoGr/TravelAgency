namespace TravelAgency.Media.Application.Services;

/// <summary>
/// Sanitizes file names for safe use in storage keys.
/// </summary>
public static class FileNameSanitizer
{
    private const int MaxFileNameLength = 255;
    private static readonly char[] InvalidChars = ['/', '\\', ':', '*', '?', '"', '<', '>', '|', '\0'];

    /// <summary>
    /// Sanitizes a file name for use in storage keys.
    /// Removes path separators, invalid characters, and limits length.
    /// </summary>
    public static string Sanitize(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file";

        var name = Path.GetFileName(fileName).Trim();
        foreach (var c in InvalidChars)
            name = name.Replace(c.ToString(), string.Empty);

        if (string.IsNullOrWhiteSpace(name))
            return "file";

        return name.Length > MaxFileNameLength ? name[..MaxFileNameLength] : name;
    }
}
