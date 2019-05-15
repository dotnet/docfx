﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ConceptualDocuments
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ConceptualDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildConceptualDocument : BaseDocumentBuildStep, ISupportIncrementalBuildStep
    {
        private const string ConceptualKey = Constants.PropertyName.Conceptual;
        private const string DocumentTypeKey = "documentType";

        public override string Name => nameof(BuildConceptualDocument);

        public override int BuildOrder => 0;

        public override void Build(FileModel model, IHostService host)
        {
            if (model.Type != DocumentType.Article)
            {
                return;
            }
            var content = (Dictionary<string, object>)model.Content;
            var markdown = (string)content[ConceptualKey];
            var result = host.Markup(markdown, model.OriginalFileAndType, false, true);

            var htmlInfo = HtmlDocumentUtility.SeparateHtml(result.Html);
            model.Properties.IsUserDefinedTitle = false;
            content[Constants.PropertyName.Title] = htmlInfo.Title;
            content["rawTitle"] = htmlInfo.RawTitle;
            if (!string.IsNullOrEmpty(htmlInfo.RawTitle))
            {
                model.ManifestProperties.rawTitle = htmlInfo.RawTitle;
            }
            content[ConceptualKey] = htmlInfo.Content;

            if (result.YamlHeader?.Count > 0)
            {
                foreach (var item in result.YamlHeader)
                {
                    HandleKeyValuePair(item.Key, item.Value);
                }
            }
            model.LinkToFiles = result.LinkToFiles.ToImmutableHashSet();
            model.LinkToUids = result.LinkToUids;
            model.FileLinkSources = result.FileLinkSources;
            model.UidLinkSources = result.UidLinkSources;
            model.Properties.XrefSpec = null;
            if (model.Uids.Length > 0)
            {
                var title = content[Constants.PropertyName.Title] as string;
                model.Properties.XrefSpec = new XRefSpec
                {
                    Uid = model.Uids[0].Name,
                    Name = string.IsNullOrEmpty(title) ? model.Uids[0].Name : title,
                    Href = ((RelativePath)model.File).GetPathFromWorkingFolder()
                };
            }

            foreach (var d in result.Dependency)
            {
                host.ReportDependencyTo(model, d, DependencyTypeName.Include);
            }

            void HandleKeyValuePair(string key, object value)
            {
                switch (key)
                {
                    case Constants.PropertyName.Uid:
                        var uid = value as string;
                        if (!string.IsNullOrWhiteSpace(uid))
                        {
                            model.Uids = new[] { new UidDefinition(uid, model.LocalPathFromRoot) }.ToImmutableArray();
                            content[Constants.PropertyName.Uid] = value;
                        }
                        break;
                    case DocumentTypeKey:
                        content[key] = value;
                        model.DocumentType = value as string;
                        break;
                    case Constants.PropertyName.Title:
                        if (value is string str && !string.IsNullOrEmpty(str))
                        {
                            content[key] = str;
                            model.Properties.IsUserDefinedTitle = true;
                        }
                        break;
                    case Constants.PropertyName.OutputFileName:
                        content[key] = value;
                        var outputFileName = value as string;
                        if (!string.IsNullOrWhiteSpace(outputFileName))
                        {
                            string fn = null;
                            try
                            {
                                fn = Path.GetFileName(outputFileName);
                            }
                            catch (ArgumentException) { }
                            if (fn == outputFileName)
                            {
                                model.File = (RelativePath)model.File + (RelativePath)outputFileName;
                            }
                            else
                            {
                                Logger.LogWarning($"Invalid output file name in yaml header: {outputFileName}, skip rename output file.");
                            }
                        }
                        break;
                    default:
                        content[key] = value;
                        break;
                }
            }
        }

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion
    }
}
