// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Docfx.Common;

public static class PathUtility
{
    private static readonly Regex UriWithProtocol = new(@"^\w{2,}\:", RegexOptions.Compiled);

    private static readonly char[] AdditionalInvalidChars = ":*".ToArray();
    public static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars().Concat(AdditionalInvalidChars).ToArray();
    public static readonly char[] InvalidPathChars = Path.GetInvalidPathChars().Concat(AdditionalInvalidChars).ToArray();
    private static readonly string InvalidFileNameCharsRegexString = $"[{Regex.Escape(new string(InvalidFileNameChars))}]";

    // refers to http://index/?query=urlencode&rightProject=System&file=%5BRepoRoot%5D%5CNDP%5Cfx%5Csrc%5Cnet%5CSystem%5CNet%5Cwebclient.cs&rightSymbol=fptyy6owkva8
    private static readonly string NeedUrlEncodeFileNameCharsRegexString = "[^0-9a-zA-Z-_.!*()]";

    private static readonly string InvalidOrNeedUrlEncodeFileNameCharsRegexString = $"{InvalidFileNameCharsRegexString}|{NeedUrlEncodeFileNameCharsRegexString}";
    private static readonly Regex InvalidOrNeedUrlEncodeFileNameCharsRegex = new(InvalidOrNeedUrlEncodeFileNameCharsRegexString, RegexOptions.Compiled);

    public static bool IsPathCaseInsensitive()
    {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// http://stackoverflow.com/questions/422090/in-c-sharp-check-that-filename-is-possibly-valid-not-that-it-exists
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static bool IsValidFilePath(string path)
    {
        FileInfo fi = null;
        try
        {
            fi = new FileInfo(path);
        }
        catch (ArgumentException) { }
        catch (PathTooLongException) { }
        catch (NotSupportedException) { }
        return fi != null;
    }

    /// <summary>
    /// Creates a relative path from one file or folder to another.
    /// </summary>
    /// <param name="basePath">Contains the directory that defines the start of the relative path.</param>
    /// <param name="absolutePath">Contains the path that defines the endpoint of the relative path.</param>
    /// <returns>The relative path from the start directory to the end path.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="UriFormatException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static string MakeRelativePath(string basePath, string absolutePath)
    {
        if (string.IsNullOrEmpty(basePath))
        {
            return absolutePath;
        }
        if (string.IsNullOrEmpty(absolutePath))
        {
            return null;
        }
        if (FilePathComparer.OSPlatformSensitiveComparer.Equals(basePath, absolutePath))
        {
            return string.Empty;
        }

        // Append / to the directory
        if (basePath[basePath.Length - 1] != '/')
        {
            basePath += "/";
        }

        Uri fromUri = new(Path.GetFullPath(basePath));
        Uri toUri = new(Path.GetFullPath(absolutePath));

        if (fromUri.Scheme != toUri.Scheme)
        {
            // path can't be made relative.
            return absolutePath;
        }

        Uri relativeUri = fromUri.MakeRelativeUri(toUri);
        string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (string.Equals(toUri.Scheme, "FILE", StringComparison.InvariantCultureIgnoreCase))
        {
            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        return relativePath.BackSlashToForwardSlash();
    }

    public static string ToCleanUrlFileName(this string input, string replacement = "-")
    {
        return InvalidOrNeedUrlEncodeFileNameCharsRegex.Replace(input, replacement);
    }

    public static bool IsRelativePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        // IsWellFormedUriString does not try to escape characters such as '\' ' ', '(', ')' and etc. first. Use TryCreate instead
        if (Uri.TryCreate(path, UriKind.Absolute, out Uri absoluteUri))
        {
            return false;
        }

        if (UriWithProtocol.IsMatch(path))
        {
            return false;
        }

        foreach (var ch in InvalidPathChars)
        {
            if (path.Contains(ch))
            {
                return false;
            }
        }

        return !Path.IsPathRooted(path);
    }

    /// <summary>
    /// Also change backslash to forward slash
    /// </summary>
    /// <param name="path"></param>
    /// <param name="kind"></param>
    /// <param name="basePath"></param>
    /// <returns></returns>
    public static string FormatPath(this string path, UriKind kind, string basePath = null)
    {
        if (kind == UriKind.RelativeOrAbsolute)
        {
            return path.BackSlashToForwardSlash();
        }
        if (kind == UriKind.Absolute)
        {
            return Path.GetFullPath(path).BackSlashToForwardSlash();
        }
        if (kind == UriKind.Relative)
        {
            if (string.IsNullOrEmpty(basePath))
            {
                return path.BackSlashToForwardSlash();
            }

            return MakeRelativePath(basePath, path).BackSlashToForwardSlash();
        }

        return null;
    }

    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }
        return Path.GetFullPath(path).Replace('\\', '/');
    }

    public static bool IsDirectory(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException($"{nameof(path)} should not be null or empty string");
        }

        return Directory.Exists(path);
    }
}
