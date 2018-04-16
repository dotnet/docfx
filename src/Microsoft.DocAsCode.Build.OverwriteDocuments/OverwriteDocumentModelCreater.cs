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
        string _file;

        public OverwriteDocumentModelCreater(string file)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }

        public OverwriteDocumentModel Create(MarkdownFragmentModel model)
        {
            return new OverwriteDocumentModel
            {
                Uid = model.Uid,
                Metadata = CreateMetadata(model)
            };
        }

        private Dictionary<string, object> CreateMetadata(MarkdownFragmentModel model)
        {
            var metadata = ConvertYamlCodeBlock(model.YamlCodeBlock, model.YamlCodeBlockSource);
            return ConvertContents(metadata, model.Contents);
        }

        internal static Dictionary<object, object> ConvertYamlCodeBlock(string yamlCodeBlock, Block yamlCodeBlockSource)
        {
            if (string.IsNullOrEmpty(yamlCodeBlock) || yamlCodeBlockSource == null)
            {
                return new Dictionary<object, object>();
            }

            using (var reader = new StringReader(yamlCodeBlock))
            {
                try
                {
                    return YamlUtility.Deserialize<Dictionary<object, object>>(reader);
                }
                catch (Exception ex)
                {
                    throw new MarkdownFragmentsException(
                        $"Encountered an invalid YAML code block: {ex.Message}",
                        yamlCodeBlockSource.Line,
                        ex);
                }
            }
        }

        internal Dictionary<string, object> ConvertContents(Dictionary<object, object> currentMetadata, List<MarkdownPropertyModel> contents)
        {
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

                AppendNewObject(OPathSegments, content.PropertyNameSource, CreateDocument(content), currentMetadata);
            }

            return currentMetadata.ToDictionary(k => k.Key.ToString(), k=> k.Value);
        }

        private void AppendNewObject(List<OPathSegment> OPathSegments, Block codeHeaderBlock, MarkdownDocument propertyValue, Dictionary<object, object> contentsMetadata)
        {
            FindOrCreateObject(contentsMetadata, codeHeaderBlock, OPathSegments, 0, propertyValue,
                string.Join("/", OPathSegments.Select(o => o.OriginalSegmentString)));
        }

        private void FindOrCreateObject(Dictionary<object, object> currentObject, Block codeHeaderBlock, List<OPathSegment> OPathSegments, int index, MarkdownDocument propertyValue, string originalOPathString)
        {
            var segment = OPathSegments[index];
            if (index == OPathSegments.Count - 1)
            {
                CreateCoreObject(segment, codeHeaderBlock, currentObject, propertyValue, originalOPathString);
                return;
            }

            Dictionary<object, object> nextObject;
            if (currentObject.TryGetValue(segment.SegmentName, out object childObject))
            {
                if (string.IsNullOrEmpty(segment.Key))
                {
                    nextObject = childObject as Dictionary<object, object>;
                    if (nextObject != null)
                    {
                        FindOrCreateObject(nextObject, codeHeaderBlock, OPathSegments, ++index, propertyValue, originalOPathString);
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
                    var listObject = childObject as List<object>;
                    if (listObject != null)
                    {
                        object value;
                        var goodItems = (from item in listObject
                                         where item is Dictionary<object, object> 
                                            && ((Dictionary<object, object>)item).TryGetValue(segment.Key, out value) 
                                            && ((string)value).Equals(segment.Value)
                                         select (Dictionary<object, object>)item).ToList();
                        if (goodItems.Count > 0)
                        {
                            FindOrCreateObject(goodItems[0], codeHeaderBlock, OPathSegments, ++index, propertyValue, originalOPathString);
                        }
                        else
                        {
                            nextObject = (Dictionary<object, object>)CreateDictionaryArrayObject(segment)[0];
                            listObject.Add(nextObject);
                            FindOrCreateObject(nextObject, codeHeaderBlock, OPathSegments, ++index, propertyValue, originalOPathString);
                        }
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
                if (string.IsNullOrEmpty(segment.Key))
                {
                    nextObject = CreateDictionaryObject(segment);
                    currentObject[segment.SegmentName] = nextObject;
                }
                else
                {
                    var newObject = CreateDictionaryArrayObject(segment);
                    nextObject = (Dictionary<object, object>)newObject[0];
                    currentObject[segment.SegmentName] = newObject;
                }

                FindOrCreateObject(nextObject, codeHeaderBlock, OPathSegments, ++index, propertyValue, originalOPathString);
            }
        }

        private void CreateCoreObject(OPathSegment lastSegment, Block codeHeaderBlock, Dictionary<object, object> currentObject, MarkdownDocument propertyValue, string originalOPathString)
        {
            if (currentObject.TryGetValue(lastSegment.SegmentName, out object value))
            {
                if (value is MarkdownDocument)
                {
                    // Duplicate OPath in markdown section
                    Logger.LogWarning(
                        $"There are two duplicate OPaths `{originalOPathString}`, the previous one will be overwritten.",
                        line: codeHeaderBlock.Line.ToString(),
                        code: WarningCodes.Overwrite.InvalidMarkdownFragments);
                }
                else if (value is Dictionary<object, object> || value is List<object>)
                {
                    throw new MarkdownFragmentsException(
                        $"A({lastSegment.SegmentName}) is expected to be a dictionary like \"A/B\" or an array of dictionaries like \"A[c=d]/C\", however it is used as an array of Blocks in line {codeHeaderBlock.Line} like \"../A\" OPath syntax",
                        codeHeaderBlock.Line);
                }
                else
                {
                    // Duplicate OPath in yaml section and markdown section
                    Logger.LogWarning(
                        $"Two duplicate OPaths `{originalOPathString}` in yaml code block and contents block",
                        line: codeHeaderBlock.Line.ToString(),
                        code: WarningCodes.Overwrite.InvalidMarkdownFragments);
                }
            }

            currentObject[lastSegment.SegmentName] = propertyValue;
        }

        private MarkdownDocument CreateDocument(MarkdownPropertyModel model)
        {
            var result = new MarkdownDocument();
            if (model != null)
            {
                foreach (var block in model.PropertyValue)
                {
                    block.Parent?.Remove(block);
                    result.Add(block);
                }
                result.SetData("filePath", _file);
                result.SetData(Constants.OPathStringDataName, model.PropertyName);
                result.SetData(Constants.OPathLineNumberDataName, model.PropertyNameSource.Line + 1);
            }
            return result;
        }

        private static Dictionary<object, object> CreateDictionaryObject(OPathSegment segment)
        {
            return new Dictionary<object, object>();
        }

        private static List<object> CreateDictionaryArrayObject(OPathSegment segment)
        {
            var newObject = new List<object>
            {
                new Dictionary<object, object>
                {
                    {segment.Key, segment.Value}
                }
            };
            return newObject;
        }
    }
}
