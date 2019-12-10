// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class OpsConfigAdapter
    {
        private static readonly bool s_prod = !string.Equals(
            "PPE", Environment.GetEnvironmentVariable("DOCS_ENVIRONMENT"), StringComparison.OrdinalIgnoreCase);

        private static readonly string s_opsEndpoint = s_prod
            ? "https://op-build-prod.azurewebsites.net"
            : "https://op-build-sandbox2.azurewebsites.net";

        public static JObject Load(FileResolver fileResolver, SourceInfo<string> name, string repository, string branch)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(repository))
            {
                return null;
            }

            var url = $"{s_opsEndpoint}/v2/Queries/Docsets?git_repo_url={repository}&docset_query_status=Created";
            var docsets = JsonConvert.DeserializeAnonymousType(
                ResolveFile(fileResolver, name, url),
                new[] { new { name = "", base_path = "", site_name = "", product_name = "" } });

            var docset = docsets.FirstOrDefault(d => string.Equals(d.name, name, StringComparison.OrdinalIgnoreCase));
            if (docset is null)
            {
                throw Errors.DocsetNotProvisioned(name).ToException();
            }

            return new JObject
            {
                ["product"] = docset.product_name,
                ["siteName"] = docset.site_name,
                ["hostName"] = GetHostName(docset.site_name),
                ["basePath"] = docset.base_path,
                ["xrefHostName"] = GetXrefHostName(docset.site_name, branch),
                ["localization"] = new JObject
                {
                    ["defaultLocale"] = GetDefaultLocale(docset.site_name),
                },
            };
        }

        private static string ResolveFile(FileResolver fileResolver, SourceInfo<string> name, string url)
        {
            try
            {
                return fileResolver.ReadString(new SourceInfo<string>(url));
            }
            catch (DocfxException ex) when (ex.InnerException is HttpRequestException hre && hre.Message.Contains("404"))
            {
                throw Errors.DocsetNotProvisioned(name).ToException();
            }
        }

        private static string GetDefaultLocale(string siteName)
        {
            return siteName == "DocsAzureCN" ? "zh-cn" : "en-us";
        }

        private static string GetHostName(string siteName)
        {
            switch (siteName)
            {
                case "DocsAzureCN":
                    return s_prod ? "docs.azure.cn" : "ppe.docs.azure.cn";
                case "dev.microsoft.com":
                    return s_prod ? "developer.microsoft.com" : "devmsft-sandbox.azurewebsites.net";
                case "rd.microsoft.com":
                    return "rd.microsoft.com";
                default:
                    return s_prod ? "docs.microsoft.com" : "ppe.docs.microsoft.com";
            }
        }

        private static string GetXrefHostName(string siteName, string branch)
        {
            return !IsLive(branch) && s_prod ? $"review.{GetHostName(siteName)}" : GetHostName(siteName);
        }

        private static bool IsLive(string branch)
        {
            return branch == "live" || branch == "live-sxs";
        }
    }
}
