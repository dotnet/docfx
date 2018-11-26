// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Common.Git;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdownReader
    {
        private static readonly ImmutableList<string> RequiredProperties = ImmutableList.Create(Constants.PropertyName.Uid);

        public static IEnumerable<OverwriteDocumentModel> ReadMarkdownAsOverwrite(IHostService host, FileAndType ft)
        {
            // Order the list from top to bottom
            var markdown = EnvironmentContext.FileAbstractLayer.ReadAllText(ft.File);
            var parts = MarkupMultiple(host, markdown, ft);
            return from part in parts
                   select TransformModel(ft.FullPath, part);
        }

        public static Dictionary<string, object> ReadMarkdownAsConceptual(string file)
        {
            var filePath = EnvironmentContext.FileAbstractLayer.GetPhysicalPath(file);
            var repoInfo = GitUtility.TryGetFileDetail(filePath);
            return new Dictionary<string, object>
            {
                [Constants.PropertyName.Conceptual] = EnvironmentContext.FileAbstractLayer.ReadAllText(file),
                [Constants.PropertyName.Type] = "Conceptual",
                [Constants.PropertyName.Source] = new SourceDetail { Remote = repoInfo },
                [Constants.PropertyName.Path] = file,
                [Constants.PropertyName.Documentation] = new SourceDetail { Remote = repoInfo }
            };
        }

        private static OverwriteDocumentModel TransformModel(string filePath, YamlHtmlPart part)
        {
            if (part == null)
            {
                return null;
            }

            var properties = part.YamlHeader;
            var checkPropertyStatus = CheckRequiredProperties(properties, RequiredProperties, out string checkPropertyMessage);
            if (!checkPropertyStatus)
            {
                throw new InvalidDataException(checkPropertyMessage);
            }

            var overriden = RemoveRequiredProperties(properties, RequiredProperties);
            var repoInfo = GitUtility.TryGetFileDetail(filePath);

            return new OverwriteDocumentModel
            {
                Uid = properties[Constants.PropertyName.Uid].ToString(),
                LinkToFiles = new HashSet<string>(part.LinkToFiles),
                LinkToUids = new HashSet<string>(part.LinkToUids),
                FileLinkSources = part.FileLinkSources.ToDictionary(p => p.Key, p => p.Value.ToList()),
                UidLinkSources = part.UidLinkSources.ToDictionary(p => p.Key, p => p.Value.ToList()),
                Metadata = overriden,
                Conceptual = part.Conceptual,
                Documentation = new SourceDetail
                {
                    Remote = repoInfo,
                    StartLine = part.StartLine,
                    EndLine = part.EndLine,
                    Path = part.SourceFile
                },
                Dependency = part.Origin.Dependency
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
                Debug.Fail("Markup failed!");
                var message = $"Markup failed: {ex.Message}.";
                Logger.LogError(message);
                throw new DocumentException(message, ex);
            }
        }

        private static Dictionary<string, object> RemoveRequiredProperties(ImmutableDictionary<string, object> properties, IEnumerable<string> requiredProperties)
        {
            if (properties == null)
            {
                return null;
            }

            var overridenProperties = new Dictionary<string, object>(properties);
            foreach (var requiredProperty in requiredProperties)
            {
                if (requiredProperty != null)
                {
                    overridenProperties.Remove(requiredProperty);
                }
            }

            return overridenProperties;
        }

        private static bool CheckRequiredProperties(ImmutableDictionary<string, object> properties, IEnumerable<string> requiredKeys, out string message)
        {
            var notExistsKeys = (from key in requiredKeys
                                 where !properties.Keys.Contains(key)
                                 select key).ToList();
            if (notExistsKeys.Count > 0)
            {
                message =
                    $"Required properties {{{{{string.Join(",", notExistsKeys)}}}}} are not set. Note that keys are case sensitive.";
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}
