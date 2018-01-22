// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Markdig.Syntax;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Build.Common;

    public class OverwriteDocumentModelCreater
    {
        public static OverwriteDocumentModel Create(MarkdownFragmentModel model)
        {
            var yamlCocdeBlockMetadata = ConvertYamlCodeBlock(model.YamlCodeBlock, model.YamlCodeBlockSource);
            var contentsMetadata = ConvertContents(model.Contents);
            return new OverwriteDocumentModel
            {
                Uid = model.Uid,
                Metadata = yamlCocdeBlockMetadata.Concat(contentsMetadata).GroupBy(p => p.Key)
                    .ToDictionary(g => g.Key, g => g.Last().Value)
            };
        }

        public static Dictionary<string, object> ConvertYamlCodeBlock(string yamlCodeBlock, Block yamlCodeBlockSource)
        {
            if (string.IsNullOrEmpty(yamlCodeBlock))
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
                        $"Failed when convert yaml code block to `Dictionary<string, object>` with  error: {ex.Message}",
                        yamlCodeBlockSource.Line);
                }
            }
        }

        public static Dictionary<string, object> ConvertContents(List<MarkdownPropertyModel> contents)
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
                    throw new MarkdownFragmentsException(ex.Message, content.PropertyNameSource.Line);
                }

                AppendNewObject(OPathSegments, content.PropertyNameSource, content.PropertyValue, contentsMetadata);
            }

            return contentsMetadata;
        }

        private static void AppendNewObject(List<OPathSegment> OPathSegments, Block codeHeaderBlock, List<Block> propertyValue, Dictionary<string, object> contentsMetadata)
        {
            var objectValue = contentsMetadata;
            var leftSegments = new List<OPathSegment>(OPathSegments);
            foreach (var segment in OPathSegments)
            {
                if (objectValue.ContainsKey(segment.SegmentName))
                {
                    if (!string.IsNullOrEmpty(segment.Key))
                    {
                        var listObject = objectValue[segment.SegmentName] as List<Dictionary<string, object>>;
                        if (listObject != null && listObject.Count > 0)
                        {
                            var goodItems = (from item in listObject
                                where item.ContainsKey(segment.Key) &&
                                      item[segment.Key].ToString().Equals(segment.Value.ToString(),
                                          StringComparison.OrdinalIgnoreCase)
                                select item).ToList();
                            if (goodItems.Count > 0)
                            {
                                objectValue = goodItems.First();
                                leftSegments.Remove(segment);
                            }
                            else
                            {
                                ((List<Dictionary<string, object>>) objectValue[segment.SegmentName]).Add(
                                    ((List<Dictionary<string, object>>) ((Dictionary<string, object>) CreateObject(leftSegments, propertyValue))[segment.SegmentName])
                                    .First());
                                return;
                            }
                        }
                        else
                        {
                            // Throw exception if there are two OPaths like:
                            // A/B
                            // A[c=d]/C
                            var sameSegments = new List<OPathSegment>(OPathSegments);
                            sameSegments.RemoveRange(OPathSegments.IndexOf(segment), OPathSegments.Count - OPathSegments.IndexOf(segment));
                            throw new MarkdownFragmentsException(
                                $"OPath {OPathSegments.Select(o => o.OriginalSegmentString).Aggregate((a, b) => a + "/" + b)} " +
                                $"can not be appended to existing object since there is already an OPath like " +
                                $"{sameSegments.Select(o => o.OriginalSegmentString).Aggregate((a, b) => a + "/" + b) + "/" + segment.SegmentName + "/..."}",
                                codeHeaderBlock.Line);
                        }
                    }
                    else
                    {
                        leftSegments.Remove(segment);
                        if (objectValue[segment.SegmentName] is List<Block>)
                        {
                            // Duplication
                            Logger.LogWarning(
                                $"There is two duplicate OPaths {OPathSegments.Select(o => o.OriginalSegmentString).Aggregate((a, b) => a + "/" + b)}, the previous one will be overwritten",
                                line: codeHeaderBlock.Line.ToString(),
                                code: WarningCodes.Overwrite.InvalidOPaths);
                            objectValue[segment.SegmentName] = propertyValue;
                        }
                        else if (objectValue[segment.SegmentName] is List<Dictionary<string, object>>)
                        {

                            // Throw exception if there are two OPaths like:
                            // A[c=d]/C
                            // A/B
                            var sameSegment = new List<OPathSegment>(OPathSegments);
                            sameSegment.RemoveRange(OPathSegments.IndexOf(segment), OPathSegments.Count - OPathSegments.IndexOf(segment));
                            throw new MarkdownFragmentsException(
                                $"OPath {OPathSegments.Select(o => o.OriginalSegmentString).Aggregate((a, b) => a + "/" + b)} " +
                                $"can not be appended to existing object since there is already an OPath like " +
                                $"{sameSegment.Select(o => o.OriginalSegmentString).Aggregate((a, b) => a + "/" + b) + "/" + segment.SegmentName + "[xx=xx]/..."}",
                                codeHeaderBlock.Line);
                        }
                        else
                        {
                            objectValue = (Dictionary<string, object>) objectValue[segment.SegmentName];
                        }
                    }
                }
                else
                {
                    leftSegments.Remove(segment);
                    objectValue[segment.SegmentName] = CreateObject(leftSegments, propertyValue);
                    return;
                }
            }
        }

        private static object CreateObject(List<OPathSegment> OPathSegments, List<Block> propertyValue)
        {
            if (OPathSegments.Count == 0)
            {
                return propertyValue;
            }

            var coreObject = new Dictionary<string, object>
            {
                {OPathSegments.Last().SegmentName, propertyValue}
            };
            for (int i = OPathSegments.Count - 2; i >= 0; i--)
            {
                var segment = OPathSegments[i];
                if (!string.IsNullOrEmpty(segment.Key))
                {
                    coreObject = AddToFirstOfDictionary(coreObject, new KeyValuePair<string, object>(segment.Key, segment.Value));
                    coreObject = new Dictionary<string, object>
                    {
                        {segment.SegmentName, new List<Dictionary<string, object>> {coreObject}}
                    };
                }
                else
                {
                    coreObject = new Dictionary<string, object>
                    {
                        {segment.SegmentName, coreObject}
                    };
                }
            }

            return coreObject;
        }

        private static Dictionary<string, object> AddToFirstOfDictionary(Dictionary<string, object> dictionary, KeyValuePair<string, object> newItem)
        {
            List<KeyValuePair<string, object>> list = dictionary.ToList();
            list.Insert(0, newItem);
            return list.ToDictionary(item => item.Key, item => item.Value);
        }
    }
}
