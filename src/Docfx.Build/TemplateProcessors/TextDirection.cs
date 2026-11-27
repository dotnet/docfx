// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Common;

namespace Docfx.Build.Engine;

/// <summary>
/// Resolves the text direction of pages, to render right-to-left languages such as Arabic, Hebrew and Persian.
/// </summary>
internal static class TextDirection
{
    /// <summary>The text direction of the pages, <c>rtl</c> or <c>ltr</c>.</summary>
    public const string DirKey = "_dir";

    /// <summary>Set to <c>true</c> for right-to-left pages, for templates that can't compare strings.</summary>
    public const string IsRtlKey = "_isRtl";

    private const string LangKey = "_lang";
    private const string Rtl = "rtl";
    private const string Ltr = "ltr";

    // Languages whose default script is right-to-left, from the culture data of ICU.
    // docfx runs in globalization-invariant mode, where this data is not available.
    private static readonly HashSet<string> s_rightToLeftLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        // Arabic
        "ar", "ara", "ars",
        // Aramaic, Avestan
        "arc", "ae",
        // Baluchi
        "bal", "bcc", "bqi",
        // Brahui
        "brh",
        // Kurdish
        "ckb",
        // Divehi
        "dv", "div",
        // Persian, Dari
        "fa", "fas", "per", "prs",
        // Gilaki
        "glk",
        // Hebrew, Yiddish
        "he", "heb", "iw", "yi", "yid",
        // Kashmiri, Khowar
        "kas", "ks", "khw",
        // Luri
        "lrc",
        // Mazanderani
        "mzn",
        // N'Ko
        "nqo",
        // Pashto
        "ps", "pus",
        // Western Punjabi
        "pnb",
        // Rohingya
        "rhg",
        // Sindhi, Saraiki
        "sd", "snd", "skr",
        // Syriac
        "syr",
        // Uyghur
        "ug", "uig",
        // Urdu
        "ur", "urd",
    };

    // Scripts that are written right-to-left
    private static readonly HashSet<string> s_rightToLeftScripts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Adlm", "Arab", "Hebr", "Mand", "Nkoo", "Rohg", "Samr", "Syrc", "Thaa",
    };

    /// <summary>
    /// Adds the direction to the global metadata of a docset, so that it is determined once
    /// for all its pages. Pages can override it with their own <c>_lang</c> or <c>_dir</c>.
    /// </summary>
    public static ImmutableDictionary<string, object> AppendTo(ImmutableDictionary<string, object> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var (direction, isRtl) = Resolve(key => metadata.TryGetValue(key, out var value) ? value : null);
        metadata = direction is null ? metadata.Remove(DirKey) : metadata.SetItem(DirKey, direction);
        return isRtl ? metadata.SetItem(IsRtlKey, true) : metadata.Remove(IsRtlKey);
    }

    /// <summary>
    /// Adds <c>_dir</c> to the model of a page when it is right-to-left or <c>_dir</c> is set
    /// explicitly, and <c>_isRtl</c> when it is right-to-left. Other pages are not changed:
    /// they keep the browser default and get no <c>dir</c> attribute.
    /// </summary>
    public static void AppendTo(IDictionary<string, object> model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var (direction, isRtl) = Resolve(key => model.TryGetValue(key, out var value) ? value : null);
        if (direction is null)
        {
            model.Remove(DirKey);
        }
        else
        {
            model[DirKey] = direction;
        }
        if (isRtl)
        {
            model[IsRtlKey] = true;
        }
        else
        {
            model.Remove(IsRtlKey);
        }
    }

    /// <summary>
    /// Returns the direction given by <paramref name="dir"/>, or <c>rtl</c> when <paramref name="lang"/>
    /// is a right-to-left language such as Arabic, Hebrew or Persian. Returns null for other
    /// languages, so that the pages keep the browser default.
    /// </summary>
    public static string Resolve(string dir, string lang)
    {
        if (!string.IsNullOrEmpty(dir))
        {
            if (dir.Equals(Rtl, StringComparison.OrdinalIgnoreCase))
            {
                return Rtl;
            }
            if (dir.Equals(Ltr, StringComparison.OrdinalIgnoreCase))
            {
                return Ltr;
            }

            Logger.LogWarning(
                $"Invalid value \"{dir}\" for {DirKey}, expected \"{Rtl}\" or \"{Ltr}\".",
                code: WarningCodes.Build.InvalidTextDirection);
        }

        return IsRightToLeft(lang) ? Rtl : null;
    }

    /// <summary>
    /// Whether a language tag is right-to-left, e.g. <c>fa</c>, <c>ar-SA</c> or <c>pa-Arab</c>.
    /// The script decides when the tag has one, otherwise the language does.
    /// </summary>
    public static bool IsRightToLeft(string lang)
    {
        if (string.IsNullOrEmpty(lang))
        {
            return false;
        }

        var subtags = lang.Split('-', '_');

        // The script subtag is the first subtag after the language that has four letters
        var script = subtags.Skip(1).FirstOrDefault(s => s.Length == 4 && s.All(char.IsAsciiLetter));
        return script is null
            ? s_rightToLeftLanguages.Contains(subtags[0])
            : s_rightToLeftScripts.Contains(script);
    }

    private static (string Direction, bool IsRtl) Resolve(Func<string, object> get)
    {
        var direction = Resolve(get(DirKey) as string, get(LangKey) as string);
        return (direction, direction == Rtl);
    }
}
