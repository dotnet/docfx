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
        private static readonly string s_token = Environment.GetEnvironmentVariable("DOCS_OPS_TOKEN");
        private static readonly bool s_prod = string.Equals(
            "PROD", Environment.GetEnvironmentVariable("DOCS_ENVIRONMENT"), StringComparison.OrdinalIgnoreCase);

        private static readonly string s_opsEndpoint = s_prod
            ? "https://op-build-prod.azurewebsites.net"
            : "https://op-build-sandbox2.azurewebsites.net";

        public static JObject Load(SourceInfo<string> name, string repository, string branch, bool noFetch = false)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(s_token))
            {
                return null;
            }

            var url = $"{s_opsEndpoint}/v2/Queries/Docsets?git_repo_url={repository}&docset_query_status=Created";
            var docsets = JsonConvert.DeserializeAnonymousType(
                ResolveFile(name, url, noFetch),
                new[] { new { name = "", base_path = "", site_name = "", product_name = "" } });

            var docset = docsets.FirstOrDefault(d => d.name == name);
            if (docset is null)
            {
                throw Errors.DocsetNotProvisioned(name).ToException();
            }

            return new JObject
            {
                ["product"] = docset.product_name,
                ["siteName"] = docset.site_name,
                ["baseUrl"] = $"https://{GetHostName(docset.site_name)}{docset.base_path}",
                ["xrefBaseUrl"] = $"https://{GetXrefHostName(docset.site_name, branch)}",
                ["localization"] = new JObject
                {
                    ["defaultLocale"] = GetDefaultLocale(docset.site_name),
                },
            };
        }

        private static string ResolveFile(SourceInfo<string> name, string url, bool noFetch)
        {
            try
            {
                return new FileResolver(".", ProvideCredential, noFetch).ReadString(new SourceInfo<string>(url));
            }
            catch (DocfxException ex) when (ex.InnerException is HttpRequestException hre && hre.Message.Contains("404"))
            {
                throw Errors.DocsetNotProvisioned(name).ToException();
            }
        }

        private static void ProvideCredential(HttpRequestMessage request)
        {
            request.Headers.Add("X-OP-BuildUserToken", s_token);
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
