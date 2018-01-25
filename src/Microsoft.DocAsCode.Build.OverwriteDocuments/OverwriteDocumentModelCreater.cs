// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Markdig.Syntax;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;

    public class OverwriteDocumentModelCreater
    {
        public static OverwriteDocumentModel Create(MarkdownFragmentModel model)
        {
            var yamlCodeBlockMetadata = ConvertYamlCodeBlock(model.YamlCodeBlock, model.YamlCodeBlockSource);
            var contentsMetadata = ConvertContents(model.Contents);

            return new OverwriteDocumentModel
            {
                Uid = model.Uid,
                Metadata = MergeYamlCodeMetadataWithContentsMetadata(yamlCodeBlockMetadata, contentsMetadata)
            };
        }

        internal static Dictionary<string, object> ConvertYamlCodeBlock(string yamlCodeBlock, Block yamlCodeBlockSource)
        {
            if (string.IsNullOrEmpty(yamlCodeBlock) || yamlCodeBlockSource == null)
            {
                return new Dictionary<string, object>();
            }

            using (var reader = new StringReader(yamlCodeBlock))
            {
                try
                {
                    return YamlUtility.Deserialize<Dictionary<string, object>>(reader);
                }
                catch (Exception ex)
                {
                    throw new MarkdownFragmentsException(
                        $"Encountered an invalid YAML code block in line {yamlCodeBlockSource.Line}: {ex.Message}",
                        yamlCodeBlockSource.Line,
                        ex);
                }
            }
        }

        internal static Dictionary<string, object> ConvertContents(List<MarkdownPropertyModel> contents)
        {
            var contentsMetadata = new Dictionary<string, object>();
            List<OPathSegment> OPathSegments;
            foreach (var content in contents)
            {
                try
                {
                    OPathSegments = OverwriteUtility.ParseOPath(content.PropertyName);
                }
                catch (ArgumentException ex)
                {
                    throw new MarkdownFragmentsException(ex.Message, content.PropertyNameSource.Line, ex);
                }

                AppendNewObject(OPathSegments, content.PropertyNameSource, content.PropertyValue, contentsMetadata);
            }

            return contentsMetadata;
        }

        internal static Dictionary<string, object> MergeYamlCodeMetadataWithContentsMetadata(Dictionary<string, object> yamlCodeMetadata, Dictionary<string, object> contentsMetadata)
        {
            var metadata = yamlCodeMetadata.Concat(contentsMetadata).GroupBy(p => p.Key);
            var metadatasWithSameOPath = from meta in metadata
                where meta.Count() > 1
                select meta;
            foreach (var meta in metadatasWithSameOPath)
            {
                Logger.LogWarning(
                    $"There are two duplicate OPaths `{meta.Key}` in yaml code block and contents block, the item in yaml code block will be overwritten by contents block item",
                    code: WarningCodes.Overwrite.DuplicateOPaths);
            }

            return metadata.ToDictionary(g => g.Key, g => g.Last().Value);
        }

        private static void AppendNewObject(List<OPathSegment> OPathSegments, Block codeHeaderBlock, List<Block> propertyValue, Dictionary<string, object> contentsMetadata)
        {
            var currentObject = contentsMetadata;
            for (int index = 0; index < OPathSegments.Count - 1; index++)
            {
                currentObject = FindOrCreateSegment(currentObject, codeHeaderBlock, OPathSegments[index]);
            }

            var lastSegment = OPathSegments.Last();
            if (currentObject.TryGetValue(lastSegment.SegmentName, out object value))
            {
                if (value is List<Block>)
                {
                    // Duplicate
                    Logger.LogWarning(
                        $"There are two duplicate OPaths {OPathSegments.Select(o => o.OriginalSegmentString).Aggregate((a, b) => a + "/" + b)}, the previous one will be overwritten",
                        line: codeHeaderBlock.Line.ToString(),
                        code: WarningCodes.Overwrite.InvalidOPaths);
                }
                else
                {
                    throw new MarkdownFragmentsException(
                        $"A({lastSegment.SegmentName}) is expected to be an dictionary like \"A/B\" or an array of dictionaries like \"A[c=d]/C\", however it is used as an array of Blocks in line {codeHeaderBlock.Line} like \"../A\" OPath syntax",
                        codeHeaderBlock.Line);
                }
            }

            currentObject[lastSegment.SegmentName] = propertyValue;
        }

        private static Dictionary<string, object> FindOrCreateSegment(Dictionary<string, object> currentObject, Block codeHeaderBlock, OPathSegment segment)
        {
            Dictionary<string, object> nextObject;
            if (currentObject.TryGetValue(segment.SegmentName, out object childObject))
            {
                if (string.IsNullOrEmpty(segment.Key))
                {
                    nextObject = childObject as Dictionary<string, object>;
                    if (nextObject != null)
                    {
                        return nextObject;
                    }
                    else
                    {
                        throw new MarkdownFragmentsException(
                            $"A({segment.SegmentName}) is not expected to be an object like \"A/B\", however it is used as an object in line {codeHeaderBlock.Line} with `{segment.SegmentName}/...`",
                            codeHeaderBlock.Line);
                    }
                }
                else
                {
                    var listObject = childObject as List<Dictionary<string, object>>;
                    if (listObject != null)
                    {
                        object value;
                        var goodItems = (from item in listObject
                            where item.TryGetValue(segment.Key, out value) && (value as string).Equals(segment.Value)
                            select item).ToList();
                        if (goodItems.Count > 0)
                        {
                            return goodItems[0];
                        }

                        listObject.Add(((List<Dictionary<string, object>>)CreateObject(segment, out nextObject))[0]);
                        return nextObject;
                    }
                    else
                    {
                        throw new MarkdownFragmentsException(
                            $"A({segment.SegmentName}) is not expected to be an array like \"A[c=d]/B\", however it is used as an array in line {codeHeaderBlock.Line} with `{segment.SegmentName}[{segment.Key}=\"{segment.Value}\"]/...`",
                            codeHeaderBlock.Line);
                    }
                }
            }
            else
            {
                currentObject[segment.SegmentName] = CreateObject(segment, out nextObject);
                return nextObject;
            }
        }

        private static object CreateObject(OPathSegment segment, out Dictionary<string, object> nextObject)
        {
            if (string.IsNullOrEmpty(segment.Key))
            {
                nextObject = new Dictionary<string, object>();
                return nextObject;
            }
            else
            {
                var newObject = new List<Dictionary<string,object>>();
                nextObject = new Dictionary<string, object>
                {
                    {segment.Key, segment.Value}
                };
                newObject.Add(nextObject);
                return newObject;
            }
        }
    }
}
