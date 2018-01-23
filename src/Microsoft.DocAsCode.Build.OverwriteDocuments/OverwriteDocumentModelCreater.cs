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
            if (currentObject.ContainsKey(lastSegment.SegmentName))
            {
                if (currentObject[lastSegment.SegmentName] is List<Block>)
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
                        $"A({lastSegment.SegmentName}) is expected to be an dictionary with \"A/B\" or an array of dictionaries with \"A[c=d]/C\", however it is used as an array of Blocks in line {codeHeaderBlock.Line} with \"../A\" OPath syntax",
                        codeHeaderBlock.Line);
                }
            }

            currentObject[lastSegment.SegmentName] = propertyValue;
        }

        private static Dictionary<string, object> FindOrCreateSegment(Dictionary<string, object> currentObject, Block codeHeaderBlock, OPathSegment segment)
        {
            Dictionary<string, object> nextObject;
            if (currentObject.ContainsKey(segment.SegmentName))
            {
                if (currentObject[segment.SegmentName] is List<Block>)
                {
                    throw new MarkdownFragmentsException(
                        $"A({segment.SegmentName}) is expected to be an array of Blocks with \"../A\", however it is used as an dictionary or an array of dictionaries in line {codeHeaderBlock.Line} with \"A/B\" or \"A[c=d]/B\" OPath syntax",
                        codeHeaderBlock.Line);
                }

                nextObject = currentObject[segment.SegmentName] as Dictionary<string, object>;
                if (nextObject != null)
                {
                    if (!string.IsNullOrEmpty(segment.Key))
                    {
                        throw new MarkdownFragmentsException(
                            $"A({segment.SegmentName}) is expected to be an object with \"A/B\", however it is used as an array in line {codeHeaderBlock.Line} with \"A[c=d]/C\" OPath syntax",
                            codeHeaderBlock.Line);
                    }
                    return nextObject;
                }

                var listObject = currentObject[segment.SegmentName] as List<Dictionary<string, object>>;
                if (listObject != null)
                {
                    if (string.IsNullOrEmpty(segment.Key))
                    {
                        throw new MarkdownFragmentsException(
                            $"A({segment.SegmentName}) is expected to be an array with \"A[c=d]/B\", however it is used as an object in line {codeHeaderBlock.Line} with \"A/C\" OPath syntax",
                            codeHeaderBlock.Line);
                    }
                    var goodItems = (from item in listObject
                        where item.ContainsKey(segment.Key) && item[segment.Key].ToString().Equals(segment.Value)
                        select item).ToList();
                    if (goodItems.Count > 0)
                    {
                        return goodItems[0];
                    }

                    ((List<Dictionary<string, object>>) currentObject[segment.SegmentName]).Add(((List<Dictionary<string, object>>) CreateObject(segment, out nextObject))[0]);
                    return nextObject;
                }

                throw new MarkdownFragmentsException(
                    "Current OBject is not invalid",
                    codeHeaderBlock.Line);
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
