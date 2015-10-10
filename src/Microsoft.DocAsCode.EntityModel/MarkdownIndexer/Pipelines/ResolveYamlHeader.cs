// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Utility;

    public class ResolveYamlHeader : IIndexerPipeline
    {
        public ParseResult Run(MapFileItemViewModel item, IndexerContext context)
        {
            var filePath = context.MarkdownFileTargetPath;
            var content = context.MarkdownContent;
            var apis = context.ExternalApiIndex;
            var apiMapFileOutputFolder = context.ApiMapFileOutputFolder;
            var yamlHeaders = YamlHeaderParser.Select(content);
            if (yamlHeaders == null || yamlHeaders.Count == 0) return new ParseResult(ResultLevel.Info, "No valid yaml header reference found for {0}", filePath);

            if (item.References == null) item.References = new ReferencesViewModel();
            ReferencesViewModel references = item.References;
            List<MarkdownSection> sections = SplitToSections(content, yamlHeaders);
            Dictionary<string, MarkdownSection> validMarkdownSections = new Dictionary<string, MarkdownSection>();
            foreach (var markdownSection in sections)
            {
                if (!string.IsNullOrEmpty(markdownSection.Id))
                {
                    validMarkdownSections[markdownSection.Id] = markdownSection;
                }
            }

            foreach (var yamlHeader in yamlHeaders)
            {
                var referenceId = yamlHeader.Id;
                var apiId = yamlHeader.Id;

                ApiIndexItemModel api;
                if (apis.TryGetValue(apiId, out api))
                {
                    var reference = new MapFileItemViewModel
                                        {
                                            Id = referenceId,
                                            ReferenceKeys = yamlHeader.MatchedSections,
                                            Href = api.Href,
                                        };
                    // *DONOT* Add references to Markdown file
                    // references.AddItem(reference);

                    // 2. Write api reference to API's map file
                    MarkdownSection markdownSection;
                    if (!validMarkdownSections.TryGetValue(apiId, out markdownSection)) continue;
                    
                    var apiPath = api.Href;
                    var apiIndexPath = api.IndexFilePath;

                    var apiYamlPath = PathUtility.GetFullPath(Path.GetDirectoryName(apiIndexPath), apiPath);
                    string apiMapFileName = Path.GetFileName(apiPath) + Constants.MapFileExtension;
                    string apiFolder = Path.GetDirectoryName(apiYamlPath);

                    // Use the same folder as api.yaml if the output folder is not set
                    string apiMapFileFolder = (string.IsNullOrEmpty(apiMapFileOutputFolder) ? apiFolder : apiMapFileOutputFolder);
                    string apiMapFileFullPath = PathUtility.GetFullPath(apiMapFileFolder, apiMapFileName);

                    // Path should be the relative path from .yml to .md
                    var markdownFilePath = context.MarkdownFileTargetPath;
                    var indexFolder = Path.GetDirectoryName(apiIndexPath);
                    var apiYamlFilePath = PathUtility.GetFullPath(indexFolder, api.Href);
                    var relativePath = PathUtility.MakeRelativePath(Path.GetDirectoryName(apiYamlFilePath), markdownFilePath).BackSlashToForwardSlash();
                    MapFileItemViewModel apiMapFileSection = new MapFileItemViewModel
                    {
                        Id = apiId,
                        Remote = item.Remote,
                        Href = relativePath,
                        Startline = markdownSection.Location.StartLocation.Line + 1,
                        Endline = markdownSection.Location.EndLocation.Line + 1, // Endline + 1 - 1, +1 for it starts from 0, -1 for it is actually the start line for next charactor, in code snippet, is always a \n
                        References = SelectReferenceSection(references, markdownSection.Location),
                        CustomProperties = yamlHeader.Properties,
                        MapFileType = MapFileType.Yaml
                    };
                    MapFileViewModel apiMapFile = null;
                    if (File.Exists(apiMapFileFullPath))
                    {
                        try
                        {
                            apiMapFile = JsonUtility.Deserialize<MapFileViewModel>(apiMapFileFullPath);
                        }
                        catch(Exception e)
                        {
                            ParseResult.WriteToConsole(ResultLevel.Warning, "Invalid map file {0} is found, overwriting. Detailed invalid message: {1}.", apiMapFileFullPath, e.Message);
                        }
                    }

                    if (apiMapFile == null)
                        apiMapFile = new MapFileViewModel();

                    // Current behavior: Override existing one
                    apiMapFile[apiId] = apiMapFileSection;

                    // Post-process item
                    // if references'/overrides count is 0, set it to null
                    if (apiMapFileSection.References != null)
                    {
                        // Resolve references' path to relative to current yaml file
                        foreach (var referenceItem in apiMapFileSection.References)
                        {
                            Uri absoluteUri;
                            if (referenceItem.Value.Href != null && !Uri.TryCreate(referenceItem.Value.Href, UriKind.Absolute, out absoluteUri))
                            {
                                var absolutePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(markdownFilePath)), referenceItem.Value.Href);
                                var relativeToCurrentApiYamlPath = PathUtility.MakeRelativePath(Path.GetDirectoryName(apiYamlFilePath), absolutePath).BackSlashToForwardSlash();
                                referenceItem.Value.Href = relativeToCurrentApiYamlPath;
                            }
                        }

                        if (apiMapFileSection.References.Count == 0)
                        {
                            apiMapFileSection.References = null;
                        }
                    }
                    if (apiMapFileSection.CustomProperties != null && apiMapFileSection.CustomProperties.Count == 0) apiMapFileSection.CustomProperties = null;

                    JsonUtility.Serialize(apiMapFileFullPath, apiMapFile);
                    ParseResult.WriteToConsole(ResultLevel.Success, "Successfully generated {0}.", apiMapFileFullPath);
                }
            }

            // Select references to the indices where DefinedLine is between startline and endline
            return new ParseResult(ResultLevel.Success);
        }

        private static ReferencesViewModel SelectReferenceSection(ReferencesViewModel allReferences, Location range)
        {
            ReferencesViewModel referenceSection = new ReferencesViewModel();
            foreach (var reference in allReferences)
            {
                Dictionary<string, Section> sections = new Dictionary<string, Section>();
                foreach (var referenceKey in reference.Value.ReferenceKeys)
                {
                    
                    foreach (var location in referenceKey.Value.Locations)
                    {
                        if (location.IsIn(range))
                        {
                            sections[referenceKey.Key] = referenceKey.Value;
                            continue;
                        }
                    }
                }

                if (sections.Count > 0)
                {
                    var referenceCloned = (MapFileItemViewModel)reference.Value.Clone();
                    referenceCloned.ReferenceKeys = sections;
                    referenceSection.AddItem(referenceCloned);
                }
            }

            return referenceSection;
        }

        private static List<MarkdownSection> SplitToSections(string content, IEnumerable<MatchDetail> yamlDetails)
        {
            MarkdownSection section = new MarkdownSection
                                          {
                                              Location = new Location { EndLocation = Coordinate.GetCoordinate(content) }
                                          };
            List<MarkdownSection> sections = new List<MarkdownSection> { section };
            foreach (var splitterDetail in yamlDetails)
            {
                var matchedSections = splitterDetail.MatchedSections;
                var splitterId = splitterDetail.Id;
                foreach (var matchedSection in matchedSections)
                {
                    foreach (var location in matchedSection.Value.Locations)
                    {
                        sections = Split(sections, location, splitterId);
                    }
                }
            }

            return sections;
        }

        private static List<MarkdownSection> Split(List<MarkdownSection> sectionsInput, Location splitter, string splitterId)
        {
            var sections = new List<MarkdownSection>();
            foreach (var markdownSection in sectionsInput)
            {
                sections.AddRange(Split(markdownSection, splitter, splitterId));
            }

            return sections;
        }

        private static List<MarkdownSection> Split(MarkdownSection section, Location splitter, string splitterId)
        {
            var sections = new List<MarkdownSection>();
            var sectionStart = section.Location.StartLocation;
            var sectionEnd = section.Location.EndLocation;
            var splitterStart = splitter.StartLocation;
            var splitterEnd = splitter.EndLocation;
            if (sectionEnd.CompareTo(splitterStart) <= 0 || sectionStart.CompareTo(splitterEnd) >= 0) return new List<MarkdownSection> { section };

            var firstStart = sectionStart;

            var secondEnd = sectionEnd;

            var firstEnd = splitterStart.CompareTo(sectionStart) > 0 ? splitterStart : sectionStart;

            var secondeStart = splitterEnd.CompareTo(sectionEnd) < 0 ? splitterEnd : sectionEnd;

            if (firstStart.CompareTo(firstEnd) < 0)
                sections.Add(
                    new MarkdownSection
                        {
                            Location = new Location { StartLocation = firstStart, EndLocation = firstEnd },
                            Id = section.Id
                        });
            if (secondeStart.CompareTo(secondEnd) < 0)
                sections.Add(
                    new MarkdownSection
                        {
                            Location = new Location { StartLocation = secondeStart, EndLocation = secondEnd },
                            Id = splitterId
                        });

            return sections;
        }

        private class MarkdownSection
        {
            public Location Location { get; set; }

            /// <summary>
            /// Id from the nearest upper uid from YAML header
            /// </summary>
            public string Id { get; set; }
        }
    }
}
