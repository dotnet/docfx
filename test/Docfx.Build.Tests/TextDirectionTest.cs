// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Common;
using Docfx.Tests.Common;
using Xunit;

using static Docfx.Build.Engine.TextDirection;

namespace Docfx.Build.Engine.Tests;

public class TextDirectionTest
{
    [Theory]
    [InlineData("rtl", "en", "rtl")]
    [InlineData("RTL", null, "rtl")]
    // The explicit direction wins over the language
    [InlineData("ltr", "fa", "ltr")]
    [InlineData("LTR", "ar-SA", "ltr")]
    public void Resolve_DirIsSet_UsesDir(string dir, string lang, string expected)
    {
        Assert.Equal(expected, Resolve(dir, lang));
    }

    [Theory]
    // Arabic, also with a region and when the script is part of the tag
    [InlineData("ar")]
    [InlineData("ar-SA")]
    [InlineData("ar-Arab")]
    [InlineData("ar-Latn", false)]
    // Hebrew, Persian, Urdu, Central Kurdish, Dari, Yiddish
    [InlineData("he")]
    [InlineData("fa")]
    [InlineData("fa-IR")]
    [InlineData("fa_ir")]
    [InlineData("ur")]
    [InlineData("ckb")]
    [InlineData("prs")]
    [InlineData("yi")]
    [InlineData("IW")]
    // Punjabi defaults to the Gurmukhi script, which is left-to-right
    [InlineData("pa", false)]
    [InlineData("pa-Arab")]
    [InlineData("pa-Guru", false)]
    [InlineData("ku-Latn", false)]
    [InlineData("ku-Arab")]
    public void Resolve_LangIsRightToLeft_ReturnsRtl(string lang, bool rightToLeft = true)
    {
        Assert.Equal(rightToLeft ? "rtl" : null, Resolve(null, lang));
    }

    [Theory]
    // Left-to-right pages keep the browser default: no direction
    [InlineData(null)]
    [InlineData("")]
    [InlineData("en")]
    [InlineData("en-US")]
    [InlineData("ja")]
    [InlineData("zh-CN")]
    [InlineData("de-CH")]
    // Not a language tag
    [InlineData("not a language!")]
    public void Resolve_LangIsLeftToRight_ReturnsNull(string lang)
    {
        Assert.Null(Resolve(null, lang));
    }

    [Fact]
    public void Resolve_DirIsInvalid_WarnsAndUsesLang()
    {
        var listener = TestLoggerListener.CreateLoggerListenerWithCodeFilter(WarningCodes.Build.InvalidTextDirection);
        Logger.RegisterListener(listener);
        try
        {
            Assert.Equal("rtl", Resolve("auto", "fa"));
            Assert.Null(Resolve("auto", "en"));

            Assert.All(listener.Items, item =>
            {
                Assert.Equal(LogLevel.Warning, item.LogLevel);
                Assert.Equal("Invalid value \"auto\" for _dir, expected \"rtl\" or \"ltr\".", item.Message);
            });
            Assert.Equal(2, listener.Items.Count);
        }
        finally
        {
            Logger.UnregisterListener(listener);
        }
    }

    [Fact]
    public void AppendTo_SetsIsRtl_AndDirForRightToLeftPages()
    {
        var model = new Dictionary<string, object> { ["_lang"] = "fa" };
        AppendTo(model);
        Assert.Equal("rtl", model[DirKey]);
        Assert.Equal(true, model[IsRtlKey]);

        // Left-to-right pages are not changed: they get no direction, like pages without a language
        foreach (var metadata in new Dictionary<string, object>[]
                 {
                     new() { ["_lang"] = "en" },
                     new() { ["_lang"] = "not a language!" },
                     new() { ["_lang"] = "en", [IsRtlKey] = true },
                     [],
                 })
        {
            AppendTo(metadata);
            Assert.False(metadata.ContainsKey(DirKey));
            Assert.False(metadata.ContainsKey(IsRtlKey));
        }
    }

    [Fact]
    public void AppendTo_KeepsAnExplicitDir()
    {
        var model = new Dictionary<string, object> { ["_lang"] = "fa", [DirKey] = "LTR" };
        AppendTo(model);
        Assert.Equal("ltr", model[DirKey]);
        Assert.False(model.ContainsKey(IsRtlKey));

        // An invalid direction is not written to the page
        model = new Dictionary<string, object> { [DirKey] = "auto" };
        AppendTo(model);
        Assert.False(model.ContainsKey(DirKey));
        Assert.False(model.ContainsKey(IsRtlKey));

        // Values that are no strings are ignored
        model = new Dictionary<string, object> { [DirKey] = true, ["_lang"] = 1 };
        AppendTo(model);
        Assert.False(model.ContainsKey(DirKey));
        Assert.False(model.ContainsKey(IsRtlKey));
    }

    [Fact]
    public void AppendTo_ResolvesTheGlobalMetadataOfADocset()
    {
        var metadata = ImmutableDictionary.Create<string, object>().Add("_lang", "ar").Add("_appTitle", "App");
        var resolved = AppendTo(metadata);

        Assert.Equal("rtl", resolved[DirKey]);
        Assert.Equal(true, resolved[IsRtlKey]);
        Assert.Equal("App", resolved["_appTitle"]);

        // The given metadata is not changed
        Assert.False(metadata.ContainsKey(DirKey));

        // An invalid direction is dropped from the global metadata, so that it is reported once
        // and not again for every page
        resolved = AppendTo(ImmutableDictionary.Create<string, object>().Add(DirKey, "auto").Add("_lang", "fa"));
        Assert.Equal("rtl", resolved[DirKey]);

        // Left-to-right docsets are not changed
        resolved = AppendTo(ImmutableDictionary.Create<string, object>().Add("_lang", "en").Add(IsRtlKey, true));
        Assert.False(resolved.ContainsKey(DirKey));
        Assert.False(resolved.ContainsKey(IsRtlKey));
    }
}
