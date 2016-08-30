﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdownReader
    {
        public static IEnumerable<OverwriteDocumentModel> ReadMarkdownAsOverwrite(IHostService host, FileAndType ft)
        {
            // Order the list from top to bottom
            var markdown = File.ReadAllText(ft.FullPath);
            var parts = MarkupMultiple(host, markdown, ft);
            return parts.Select(part => TransformModel(ft.FullPath, part));
        }

        public static Dictionary<string, object> ReadMarkdownAsConceptual(string baseDir, string file)
        {
            var filePath = Path.Combine(baseDir, file);
            var repoInfo = GitUtility.GetGitDetail(filePath);
            return new Dictionary<string, object>
            {
                [Constants.PropertyName.Conceptual] = File.ReadAllText(filePath),
                [Constants.PropertyName.Type] = "Conceptual",
                [Constants.PropertyName.Source] = new SourceDetail() { Remote = repoInfo },
                [Constants.PropertyName.Path] = file,
            };
        }

        private static OverwriteDocumentModel TransformModel(string filePath, YamlHtmlPart part)
        {
            if (part == null)
            {
                return null;
            }

            var properties = part.YamlHeader;
            string checkPropertyMessage;
            var checkPropertyStatus = CheckRequiredProperties(properties, RequiredProperties, out checkPropertyMessage);
            if (!checkPropertyStatus)
            {
                throw new InvalidDataException(checkPropertyMessage);
            }

            var overriden = RemoveRequiredProperties(properties, RequiredProperties);
            var repoInfo = GitUtility.GetGitDetail(filePath);

            return new OverwriteDocumentModel
            {
                Uid = properties[Constants.PropertyName.Uid].ToString(),
                Metadata = overriden,
                Conceptual = part.Conceptual,
                Documentation = new SourceDetail
                {
                    Remote = repoInfo,
                    StartLine = part.StartLine,
                    EndLine = part.EndLine,
                    Path = part.SourceFile
                }
            };
        }

        private static IEnumerable<YamlHtmlPart> MarkupMultiple(IHostService host, string markdown, FileAndType ft)
        {
            try
            {
                var html = host.Markup(markdown, ft, true);
                var parts = YamlHtmlPart.SplitYamlHtml(html);
                foreach (var part in parts)
                {
                    var mr = host.Parse(part.ToMarkupResult(), ft);
                    part.Conceptual = mr.Html;
                    part.LinkToFiles = mr.LinkToFiles;
                    part.LinkToUids = mr.LinkToUids;
                    part.YamlHeader = mr.YamlHeader;
                    part.FileLinkSources = mr.FileLinkSources;
                    part.UidLinkSources = mr.UidLinkSources;
                }
                return parts;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Fail("Markup failed!");
                Logger.LogWarning($"Markup failed:{Environment.NewLine}  Markdown: {markdown}{Environment.NewLine}  Details:{ex.ToString()}");
                return Enumerable.Empty<YamlHtmlPart>();
            }
        }

        private static readonly List<string> RequiredProperties = new List<string> { Constants.PropertyName.Uid };

        private static Dictionary<string, object> RemoveRequiredProperties(ImmutableDictionary<string, object> properties, IEnumerable<string> requiredProperties)
        {
            if (properties == null) return null;

            var overridenProperties = new Dictionary<string, object>(properties);
            foreach (var requiredProperty in requiredProperties)
            {
                if (requiredProperty != null) overridenProperties.Remove(requiredProperty);
            }

            return overridenProperties;
        }

        private static bool CheckRequiredProperties(ImmutableDictionary<string, object> properties, IEnumerable<string> requiredKeys, out string message)
        {
            var notExistsKeys = requiredKeys.Where(k => !properties.Keys.Contains(k, StringComparer.OrdinalIgnoreCase));
            if (notExistsKeys.Any())
            {
                message =
                    $"Required properties {{{{{string.Join(",", notExistsKeys)}}}}} are not set. Note that keys are case insensitive.";
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}
