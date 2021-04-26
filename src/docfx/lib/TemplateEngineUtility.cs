// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class TemplateEngineUtility
    {
        private static readonly HashSet<string> s_outputAbsoluteUrlYamlMime = new(StringComparer.OrdinalIgnoreCase)
        {
            "Architecture",
            "TSType",
            "TSEnum",
        };

        private static readonly HashSet<string> s_yamlMimesMigratedFromMarkdown = new(StringComparer.OrdinalIgnoreCase)
        {
            "Architecture",
            "Hub",
            "Landing",
            "LandingData",
        };

        public static bool OutputAbsoluteUrl(string? mime) => mime != null && s_outputAbsoluteUrlYamlMime.Contains(mime);

        public static bool IsConceptual(string? mime) => "Conceptual".Equals(mime, StringComparison.OrdinalIgnoreCase);

        public static bool IsLandingData(string? mime) => "LandingData".Equals(mime, StringComparison.OrdinalIgnoreCase);

        public static bool IsMigratedFromMarkdown(string? mime)
        {
            return mime != null && s_yamlMimesMigratedFromMarkdown.Contains(mime);
        }

        public static JObject LoadGlobalTokens(ErrorBuilder errors, Package package, string locale)
        {
            var defaultTokens = package.TryLoadYamlOrJson<JObject>(errors, "ContentTemplate/token");
            var localeTokens = package.TryLoadYamlOrJson<JObject>(errors, $"ContentTemplate/token.{locale}");
            if (defaultTokens == null)
            {
                return localeTokens ?? new JObject();
            }
            JsonUtility.Merge(defaultTokens, localeTokens);
            return defaultTokens;
        }

        public static (Package package, string? templateBasePath, JObject global)
            PrepareForTemplate(ErrorBuilder errors, Config config, PackageResolver fileResolver, BuildOptions buildOptions)
        {
            var template = config.Template;
            var templateFetchOptions = PackageFetchOptions.DepthOne;
            if (template.Type == PackageType.None)
            {
                template = new("_themes");
                templateFetchOptions |= PackageFetchOptions.IgnoreDirectoryNonExistedError;
            }
            var package = fileResolver.ResolveAsPackage(template, templateFetchOptions);
            var global = LoadGlobalTokens(errors, package, buildOptions.Locale);

            return (package, config.TemplateBasePath, global);
        }
    }
}
