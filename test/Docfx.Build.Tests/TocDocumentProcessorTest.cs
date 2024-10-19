﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;
using System.Web;
using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Docfx.Build.TableOfContents.Tests;

[Collection("docfx STA")]
public class TocDocumentProcessorTest : TestBase
{
    private readonly string _outputFolder;
    private readonly string _inputFolder;
    private readonly FileCreator _fileCreator;
    private readonly ApplyTemplateSettings _applyTemplateSettings;

    private const string RawModelFileExtension = ".raw.json";

    public TocDocumentProcessorTest()
    {
        _outputFolder = GetRandomFolder();
        _inputFolder = GetRandomFolder();
        _applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder);
        _applyTemplateSettings.RawModelExportSettings.Export = true;
        _fileCreator = new FileCreator(_inputFolder);
        EnvironmentContext.SetBaseDirectory(_inputFolder);
        EnvironmentContext.SetOutputDirectory(_outputFolder);
    }

    public override void Dispose()
    {
        EnvironmentContext.Clean();
        base.Dispose();
    }

    [Fact]
    public void ProcessMarkdownTocWithComplexHrefShouldSucceed()
    {
        var fileName = "#ctor";
        var href = HttpUtility.UrlEncode(fileName);
        var content = $@"
#[Constructor]({href}.md)
";
        var file = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent, fileNameWithoutExtension: fileName);
        var toc = _fileCreator.CreateFile(content, FileType.MarkdownToc);
        FileCollection files = new(_inputFolder);
        files.Add(DocumentType.Article, new[] { toc, file });
        BuildDocument(files);

        var outputRawModelPath = Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(toc, RawModelFileExtension)));
        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
        var expectedModel = new TocItemViewModel
        {
            Items = new List<TocItemViewModel>
            {
                new()
                {
                    Name = "Constructor",
                    Href = $"{href}.md",
                    TopicHref = $"{href}.md",
                }
            }
        };

        AssertTocEqual(expectedModel, model);
    }

    [Fact]
    public void ProcessMarkdownTocWithAbsoluteHrefShouldSucceed()
    {
        var content = @"
#[Topic1 Language](/href1) #
##Topic1.1 Language C#
###[Topic1.1.1](/href1.1.1) ###
##[Topic1.2]() ##
#[Topic2](http://href.com) #
";
        var toc = _fileCreator.CreateFile(content, FileType.MarkdownToc);
        FileCollection files = new(_inputFolder);
        files.Add(DocumentType.Article, new[] { toc });
        BuildDocument(files);

        var outputRawModelPath = Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(toc, RawModelFileExtension)));
        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
        var expectedModel = new TocItemViewModel
        {
            Items = new List<TocItemViewModel>
            {
                new()
                {
                    Name = "Topic1 Language",
                    Href = "/href1",
                    TopicHref = "/href1",
                    Items = new List<TocItemViewModel>
                    {
                        new()
                        {
                            Name = "Topic1.1 Language C#",
                            Items = new List<TocItemViewModel>
                            {
                                new()
                                {
                                    Name = "Topic1.1.1",
                                    Href = "/href1.1.1",
                                    TopicHref = "/href1.1.1"
                                }
                            }
                        },
                        new()
                        {
                            Name = "Topic1.2",
                            Href = string.Empty,
                            TopicHref = string.Empty
                        }
                    }
                },
                new()
                {
                    Name = "Topic2",
                    Href = "http://href.com",
                    TopicHref = "http://href.com"
                }
            }
        };

        AssertTocEqual(expectedModel, model);
    }

    [Fact]
    public void ProcessMarkdownTocWithRelativeHrefShouldSucceed()
    {
        var file1 = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent);
        var file2 = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent, "a");
        var content = $@"
#[Topic1](/href1)
##[Topic1.1]({file1})
###[Topic1.1.1]({file2})
##[Topic1.2]()
#[Topic2](http://href.com)
#[Topic3](invalid.md)
";
        var toc = _fileCreator.CreateFile(content, FileType.MarkdownToc);
        FileCollection files = new(_inputFolder);
        files.Add(DocumentType.Article, new[] { file1, file2, toc });
        BuildDocument(files);
        var outputRawModelPath = Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(toc, RawModelFileExtension)));
        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
        var expectedModel = new TocItemViewModel
        {
            Items = new List<TocItemViewModel>
            {
                new()
                {
                    Name = "Topic1",
                    Href = "/href1",
                    TopicHref = "/href1",
                    Items = new List<TocItemViewModel>
                    {
                        new()
                        {
                            Name = "Topic1.1",
                            Href = file1,
                            TopicHref = file1,
                            Items = new List<TocItemViewModel>
                            {
                                new()
                                {
                                    Name = "Topic1.1.1",
                                    Href = file2,
                                    TopicHref = file2
                                }
                            }
                        },
                        new()
                        {
                            Name = "Topic1.2",
                            Href = string.Empty,
                            TopicHref = string.Empty
                        }
                    }
                },
                new()
                {
                    Name = "Topic2",
                    Href = "http://href.com",
                    TopicHref = "http://href.com"
                },
                new()
                {
                    Name = "Topic3",
                    Href = "invalid.md",
                    TopicHref = "invalid.md"
                }
            }
        };

        AssertTocEqual(expectedModel, model);
    }

    [Fact]
    public void ProcessYamlTocWithFolderShouldSucceed()
    {
        var file1 = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent);
        var file2 = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent, "sub");
        var subToc = _fileCreator.CreateFile($@"
#[Topic]({Path.GetFileName(file2)})
", FileType.MarkdownToc, "sub");
        var content = $@"
- name: Topic1
  href: {file1}
  items:
    - name: Topic1.1
      href: {file1}
      homepage: {file2}
    - name: Topic1.2
      href: sub/
      homepage: {file1}
- name: Topic2
  href: sub/
";
        var toc = _fileCreator.CreateFile(content, FileType.YamlToc);
        FileCollection files = new(_inputFolder);
        files.Add(DocumentType.Article, new[] { file1, file2, toc, subToc });
        BuildDocument(files);
        var outputRawModelPath = Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(toc, RawModelFileExtension)));
        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
        var expectedModel = new TocItemViewModel
        {
            Items = new List<TocItemViewModel>
            {
                new()
                {
                    Name = "Topic1",
                    Href = file1,
                    TopicHref = file1,
                    Items = new List<TocItemViewModel>
                    {
                        new()
                        {
                            Name = "Topic1.1",
                            Href = file1, // For relative file, href keeps unchanged
                            Homepage = file2, // Homepage always keeps unchanged
                            TopicHref = file2,
                        },
                        new()
                        {
                            Name = "Topic1.2",
                            Href = file1, // For relative folder, href should be overwritten by homepage
                            Homepage = file1,
                            TopicHref = file1,
                            TocHref = "sub/toc.md",
                        }
                    }
                },
                new()
                {
                    Name = "Topic2",
                    Href = file2,
                    TopicHref = file2,
                    TocHref = "sub/toc.md",
                }
            }
        };

        AssertTocEqual(expectedModel, model);
    }

    [Fact]
    public void ProcessYamlTocWithMetadataShouldSucceed()
    {
        var file1 = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent);
        var content = $@"
metadata:
  meta: content
items:
- name: Topic1
  href: {file1}
  items:
    - name: Topic1.1
      href: {file1}
";
        var toc = _fileCreator.CreateFile(content, FileType.YamlToc);
        FileCollection files = new(_inputFolder);
        files.Add(DocumentType.Article, new[] { file1, toc, });
        BuildDocument(files);
        var outputRawModelPath = Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(toc, RawModelFileExtension)));
        Assert.True(File.Exists(outputRawModelPath));

        var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);

        if (JsonUtility.IsSystemTextJsonSupported<TocItemViewModel>())
        {
            var meta = (IDictionary<string, object>)model.Metadata["metadata"];
            Assert.NotNull(meta);
            Assert.Single(meta);
            Assert.Equal("content", meta["meta"]);
        }
        else
        {
            var meta = (JObject)model.Metadata["metadata"];
            Assert.NotNull(meta);
            Assert.Single(meta);
            Assert.Equal("content", meta["meta"]);
        }

        var expectedModel = new TocItemViewModel
        {
            Items = new List<TocItemViewModel>
            {
                new()
                {
                    Name = "Topic1",
                    Href = file1,
                    TopicHref = file1,
                    Items = new List<TocItemViewModel>
                    {
                        new()
                        {
                            Name = "Topic1.1",
                            Href = file1, // For relative file, href keeps unchanged
                            TopicHref = file1,
                        }
                    }
                }
            }
        };
        AssertTocEqual(expectedModel, model);
    }

    [Fact]
    public void ProcessYamlTocWithReferencedTocShouldSucceed()
    {
        var file1 = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent);
        var file2 = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent, "sub1");
        var file3 = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent, "sub1/sub2");
        var sub1sub2tocyaml = _fileCreator.CreateFile($@"
- name: Topic
  href: {Path.GetFileName(file3)}
- name: NotExistTopic
  href: a/b/c.md
", FileType.YamlToc, "sub1/sub2");
        var sub1sub3tocmd = _fileCreator.CreateFile(@"
#[Not-existed-md](sub2/notexist.md)
", FileType.MarkdownToc, "sub1/sub3");
        var sub1tocmd = _fileCreator.CreateFile($@"
#[Topic]({Path.GetFileName(file2)})
#[ReferencedToc](sub2/toc.yml)
#[ReferencedToc2](sub3/toc.md)
#[Not-existed-md](sub2/notexist.md)
", FileType.MarkdownToc, "sub1");
        var content = $@"
- name: Topic1
  href: {file1}
  items:
    - name: Topic1.1
      href: sub1/toc.md
      items:
        - name: Topic1.1.1
        - name: Topic1.1.2
    - name: Topic1.2
      href: sub1/toc.md
      homepage: {file1}
- name: Topic2
  href: sub1/sub2/toc.yml
";

        var toc = _fileCreator.CreateFile(content, FileType.YamlToc);
        FileCollection files = new(_inputFolder);
        files.Add(DocumentType.Article, new[] { file1, file2, file3, toc, sub1tocmd, sub1sub3tocmd });
        BuildDocument(files);
        var outputRawModelPath = Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(toc, RawModelFileExtension)));

        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
        var expectedModel = new TocItemViewModel
        {
            Items = new List<TocItemViewModel>
            {
                new()
                {
                    Name = "Topic1",
                    Href = file1,
                    TopicHref = file1,
                    Items = new List<TocItemViewModel>
                    {
                        new()
                        {
                            Name = "Topic1.1",
                            IncludedFrom = "~/sub1/toc.md",
                            Href = null, // For referenced toc, the content from the referenced toc is expanded as the items of current toc, and href is cleared
                            TopicHref = null,
                            Items = new List<TocItemViewModel>
                            {
                                new()
                                {
                                    Name = "Topic",
                                    Href = file2,
                                    TopicHref = file2,
                                },
                                new()
                                {
                                    Name = "ReferencedToc",
                                    IncludedFrom = "~/sub1/sub2/toc.yml",
                                    Items = new List<TocItemViewModel>
                                    {
                                        new()
                                        {
                                            Name = "Topic",
                                            Href = file3,
                                            TopicHref = file3,
                                        },
                                        new()
                                        {
                                            Name = "NotExistTopic",
                                            Href = "sub1/sub2/a/b/c.md",
                                            TopicHref = "sub1/sub2/a/b/c.md",
                                        }
                                    }
                                },

                                new()
                                {
                                    Name = "ReferencedToc2",
                                    IncludedFrom = "~/sub1/sub3/toc.md",
                                    Items = new List<TocItemViewModel>
                                    {
                                        new()
                                        {
                                            Name = "Not-existed-md",
                                            Href = "sub1/sub3/sub2/notexist.md",
                                            TopicHref = "sub1/sub3/sub2/notexist.md",
                                        },
                                    }
                                },
                                new()
                                {
                                    Name = "Not-existed-md",
                                    Href = "sub1/sub2/notexist.md",
                                    TopicHref = "sub1/sub2/notexist.md",
                                }
                            }
                        },
                        new()
                        {
                            Name = "Topic1.2",
                            Href = file1, // For referenced toc, href should be overwritten by homepage
                            TopicHref = file1,
                            IncludedFrom = "~/sub1/toc.md",
                            Homepage = file1,
                            Items = new List<TocItemViewModel>
                            {
                                new()
                                {
                                    Name = "Topic",
                                    Href = file2,
                                    TopicHref = file2,
                                },
                                new()
                                {
                                    Name = "ReferencedToc",
                                    IncludedFrom = "~/sub1/sub2/toc.yml",
                                    Items = new List<TocItemViewModel>
                                    {
                                        new()
                                        {
                                            Name = "Topic",
                                            Href = file3,
                                            TopicHref = file3,
                                        },
                                        new()
                                        {
                                            Name = "NotExistTopic",
                                            Href = "sub1/sub2/a/b/c.md",
                                            TopicHref = "sub1/sub2/a/b/c.md",
                                        }
                                    }
                                },
                                new()
                                {
                                    Name = "ReferencedToc2",
                                    IncludedFrom = "~/sub1/sub3/toc.md",
                                    Items = new List<TocItemViewModel>
                                    {
                                        new()
                                        {
                                            Name = "Not-existed-md",
                                            Href = "sub1/sub3/sub2/notexist.md",
                                            TopicHref = "sub1/sub3/sub2/notexist.md",
                                        }
                                    }
                                },
                                new()
                                {
                                    Name = "Not-existed-md",
                                    Href = "sub1/sub2/notexist.md",
                                    TopicHref = "sub1/sub2/notexist.md",
                                }
                            }
                        }
                    }
                },
                new()
                {
                    Name = "Topic2",
                    IncludedFrom = "~/sub1/sub2/toc.yml",
                    Href = null,
                    Items = new List<TocItemViewModel>
                    {
                        new()
                        {
                            Name = "Topic",
                            Href = file3,
                            TopicHref = file3,
                        },
                        new()
                        {
                            Name = "NotExistTopic",
                            Href = "sub1/sub2/a/b/c.md",
                            TopicHref = "sub1/sub2/a/b/c.md",
                        }
                    }
                }
            }
        };

        AssertTocEqual(expectedModel, model);

        // Referenced TOC File should exist
        var referencedTocPath = Path.Combine(_outputFolder, Path.ChangeExtension(sub1tocmd, RawModelFileExtension));
        Assert.True(File.Exists(referencedTocPath));
    }

    [Fact]
    public void ProcessTocWithCircularReferenceShouldFail()
    {
        var referencedToc = _fileCreator.CreateFile(@"
- name: Topic
  href: toc.md
", FileType.YamlToc, "sub1");
        var subToc = _fileCreator.CreateFile(@"
#Topic
##[ReferencedToc](toc.yml)
", FileType.MarkdownToc, "sub1");
        var content = $@"
- name: Topic1
  href: {subToc}
";
        var toc = _fileCreator.CreateFile(content, FileType.YamlToc);
        FileCollection files = new(_inputFolder);
        files.Add(DocumentType.Article, new[] { toc, subToc });
        var e = Assert.Throws<DocumentException>(() => BuildDocument(files));
        Assert.Equal($"Circular reference to {StringExtension.ToDisplayPath(Path.GetFullPath(Path.Combine(_inputFolder, subToc)))} is found in {StringExtension.ToDisplayPath(Path.GetFullPath(Path.Combine(_inputFolder, referencedToc)))}", e.Message, true);
    }

    [Fact]
    public void ProcessMarkdownTocWithNonExistentReferencedTocShouldLogError()
    {
        var pathToReferencedToc = "non-existent/toc.yml";
        var toc = _fileCreator.CreateFile($@"
#Topic
##[ReferencedToc]({pathToReferencedToc})
", FileType.MarkdownToc);
        var files = new FileCollection(_inputFolder);
        files.Add(DocumentType.Article, new[] { toc });

        var listener = TestLoggerListener.CreateLoggerListenerWithCodesFilter(new List<string> { WarningCodes.Build.InvalidTocInclude });
        Logger.RegisterListener(listener);

        BuildDocument(files);
        Logger.UnregisterListener(listener);

        Assert.Single(listener.Items);

        Assert.Equal(WarningCodes.Build.InvalidTocInclude, listener.Items[0].Code);
        Assert.Equal($"Referenced TOC file {StringExtension.ToDisplayPath(Path.GetFullPath(Path.Combine(_inputFolder, pathToReferencedToc)))} does not exist.", listener.Items[0].Message, true);
        Assert.Equal(LogLevel.Error, listener.Items[0].LogLevel);
    }

    [Fact]
    public void ProcessYamlTocWithNonExistentReferencedTocShouldLogError()
    {
        var pathToReferencedToc = "non-existent/TOC.md";
        var toc = _fileCreator.CreateFile($@"
- name: Topic
  href: {pathToReferencedToc}
", FileType.YamlToc);
        var files = new FileCollection(_inputFolder);
        files.Add(DocumentType.Article, new[] { toc });

        var listener = TestLoggerListener.CreateLoggerListenerWithCodesFilter(new List<string> { WarningCodes.Build.InvalidTocInclude });
        Logger.RegisterListener(listener);
        BuildDocument(files);
        Logger.UnregisterListener(listener);

        Assert.Single(listener.Items);

        Assert.Equal(WarningCodes.Build.InvalidTocInclude, listener.Items[0].Code);
        Assert.Equal($"Referenced TOC file {StringExtension.ToDisplayPath(Path.GetFullPath(Path.Combine(_inputFolder, pathToReferencedToc)))} does not exist.", listener.Items[0].Message, true);
        Assert.Equal(LogLevel.Error, listener.Items[0].LogLevel);
    }

    [Fact]
    public void ProcessYamlTocWithTocHrefShouldSucceed()
    {
        var file1 = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent);
        var file2 = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent, "sub1/sub2");
        var referencedToc = _fileCreator.CreateFile($@"
- name: Topic
  href: {Path.GetFileName(file2)}
", FileType.YamlToc, "sub1/sub2");
        var content = $@"
- name: Topic1
  tocHref: /Topic1/
  topicHref: /Topic1/index.html
  items:
    - name: Topic1.1
      tocHref: /Topic1.1/
      topicHref: /Topic1.1/index.html
    - name: Topic1.2
      tocHref: /Topic1.2/
      topicHref: /Topic1.2/index.html
- name: Topic2
  tocHref: {referencedToc}
  topicHref: {file2}
";
        var toc = _fileCreator.CreateFile(content, FileType.YamlToc);
        FileCollection files = new(_inputFolder);
        files.Add(DocumentType.Article, new[] { file1, file2, toc, referencedToc });
        BuildDocument(files);
        var outputRawModelPath = Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(toc, RawModelFileExtension)));

        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
        var expectedModel = new TocItemViewModel
        {
            Items = new List<TocItemViewModel>
            {
                new()
                {
                    Name = "Topic1",
                    Href = "/Topic1/",
                    TocHref = "/Topic1/",
                    Homepage = "/Topic1/index.html",
                    TopicHref = "/Topic1/index.html",
                    Items = new List<TocItemViewModel>
                    {
                        new()
                        {
                            Name = "Topic1.1",
                            Href = "/Topic1.1/",
                            TocHref = "/Topic1.1/",
                            Homepage = "/Topic1.1/index.html",
                            TopicHref = "/Topic1.1/index.html",
                        },
                        new()
                        {
                            Name = "Topic1.2",
                            Href = "/Topic1.2/",
                            TocHref = "/Topic1.2/",
                            Homepage = "/Topic1.2/index.html",
                            TopicHref = "/Topic1.2/index.html",
                        }
                    }
                },
                new()
                {
                    Name = "Topic2",
                    TocHref = referencedToc,
                    Href = referencedToc,
                    TopicHref = file2,
                    Homepage = file2,
                }
            }
        };

        AssertTocEqual(expectedModel, model);
    }

    [Fact]
    public void RelativePathToTocShouldChooseTheNearestReferenceToc()
    {
        // |-toc.md
        // |-sub1
        //    |-sub2
        //       |-file
        //       |-sub3
        //           |-toc.md

        // Arrange
        const string fileFolder = "sub1/sub2";
        var file = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent, fileFolder);
        var toc1 = _fileCreator.CreateFile($"#[Topic]({file})", FileType.MarkdownToc);
        const string toc2Folder = "sub1/sub2/sub3";
        var filePathRelativeToToc2 = ((RelativePath)file).MakeRelativeTo((RelativePath)toc2Folder);
        var toc2 = _fileCreator.CreateFile($"#[Same Topic]({filePathRelativeToToc2.FileName}", FileType.MarkdownToc, toc2Folder);
        var files = new FileCollection(_inputFolder);
        files.Add(DocumentType.Article, new[] { file, toc1, toc2 });

        // Act
        BuildDocument(files);

        // Assert
        var outputRawModelPath = Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(file, RawModelFileExtension)));
        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<Dictionary<string, object>>(outputRawModelPath);
        Assert.Equal("../../toc.md", model["_tocRel"]);
    }

    [Fact]
    public void RelativePathToTocShouldExistWithUrlEncodedHref()
    {
        // Arrange
        const string fileFolder = "sub1()";
        const string fileFolderEncoded = "sub1%28%29";
        var file = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent, fileFolder);
        var toc1 = _fileCreator.CreateFile($"#[Topic]({Path.Combine(fileFolderEncoded, Path.GetFileName(file))})", FileType.MarkdownToc);
        var toc2 = _fileCreator.CreateFile("# a nearer toc", FileType.MarkdownToc, fileFolder);
        var files = new FileCollection(_inputFolder);
        files.Add(DocumentType.Article, new[] { file, toc1, toc2 });

        // Act
        BuildDocument(files);

        // Assert
        var outputRawModelPath = Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(file, RawModelFileExtension)));
        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<Dictionary<string, object>>(outputRawModelPath);
        Assert.Equal("../toc.md", model["_tocRel"]);
        Assert.Equal("Hello world!", model["meta"]);
    }

    [Fact]
    public void ProcessYamlTocWithTocHrefAndHomepageShouldFail()
    {
        var content = @"
- name: Topic1
  tocHref: /Topic1/
  href: /Topic1/index.html
  homepage: /Topic1/index.html
";
        var toc = _fileCreator.CreateFile(content, FileType.YamlToc);
        FileCollection files = new(_inputFolder);
        files.Add(DocumentType.Article, new[] { toc });
        var e = Assert.Throws<DocumentException>(() => BuildDocument(files));
        Assert.Equal("TopicHref should be used to specify the homepage for /Topic1/ when tocHref is used.", e.Message);
    }

    [Fact]
    public void LoadBadTocYamlFileShouldGiveLineNumber()
    {
        var content = @"
- name: x
    items:
    - name: x1
      href: x1.md
    - name: x2
      href: x2.md";
        var toc = _fileCreator.CreateFile(content, FileType.YamlToc);
        var ex = Assert.Throws<DocumentException>(() => TocHelper.LoadSingleToc(toc));
        Assert.Equal("toc.yml is not a valid TOC File: (Line: 3, Col: 10, Idx: 22) - (Line: 3, Col: 10, Idx: 22): While scanning a plain scalar value, found invalid mapping.", ex.Message);
    }

    [Fact]
    public void LoadTocYamlWithEmptyNodeShouldSucceed()
    {
        // Arrange
        var content = @"
- name: x
  href: a.md
-";
        var files = new FileCollection(_inputFolder);
        var file = _fileCreator.CreateFile(content, FileType.YamlToc);
        files.Add(DocumentType.Article, new[] { file });

        // Act
        BuildDocument(files);

        // Assert
        var outputRawModelPath = Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(file, RawModelFileExtension)));
        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
        Assert.Single(model.Items); // empty node is removed
    }

    [Fact]
    public void WarningShouldBeFromIncludedToc()
    {
        // Arrange
        var masterContent = @"
- name: TOC2
  href: ../included/toc.yml";
        var includedContent = @"
- name: Article2
  href: not-existing2.md
- name: Article3ByUid
  uid: not-existing-uid";
        var files = new FileCollection(_inputFolder);
        var masterFile = _fileCreator.CreateFile(masterContent, FileType.YamlToc, "master");
        var includedFile = _fileCreator.CreateFile(includedContent, FileType.YamlToc, "included");
        files.Add(DocumentType.Article, new[] { masterFile });

        // Act
        var listener = TestLoggerListener.CreateLoggerListenerWithCodesFilter(
            new List<string> { WarningCodes.Build.InvalidFileLink, WarningCodes.Build.UidNotFound });
        Logger.RegisterListener(listener);
        BuildDocument(files);
        Logger.UnregisterListener(listener);

        // Assert
        Assert.NotNull(listener.Items);
        Assert.Equal(2, listener.Items.Count);
        Assert.Equal(WarningCodes.Build.InvalidFileLink, listener.Items[0].Code);
        Assert.Equal("~/included/toc.yml", listener.Items[0].File);
        Assert.Equal(WarningCodes.Build.UidNotFound, listener.Items[1].Code);
        Assert.Equal("~/included/toc.yml", listener.Items[1].File);
    }

    [Fact]
    public void UrlDecodeHrefInYamlToc()
    {
        // Arrange
        var tocContent = @"
- name: NAME
  href: a%20b.md";
        var files = new FileCollection(_inputFolder);
        var tocFile = _fileCreator.CreateFile(tocContent, FileType.YamlToc);
        var markdownFile = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent, fileNameWithoutExtension: "a b");
        files.Add(DocumentType.Article, new[] { tocFile, markdownFile });

        // Act
        var listener = TestLoggerListener.CreateLoggerListenerWithCodesFilter(
            new List<string> { WarningCodes.Build.InvalidFileLink });
        Logger.RegisterListener(listener);
        BuildDocument(files);
        Logger.UnregisterListener(listener);

        // Assert
        Assert.NotNull(listener.Items);
        Assert.Empty(listener.Items);
    }

    [Fact]
    public void UrlDecodeHrefInMarkdownToc()
    {
        // Arrange
        var tocContent = "# [NAME](a%20b.md)";
        var files = new FileCollection(_inputFolder);
        var tocFile = _fileCreator.CreateFile(tocContent, FileType.MarkdownToc);
        var markdownFile = _fileCreator.CreateFile(string.Empty, FileType.MarkdownContent, fileNameWithoutExtension: "a b");
        files.Add(DocumentType.Article, new[] { tocFile, markdownFile });

        // Act
        var listener = TestLoggerListener.CreateLoggerListenerWithCodesFilter(
            new List<string> { WarningCodes.Build.InvalidFileLink });
        Logger.RegisterListener(listener);
        BuildDocument(files);
        Logger.UnregisterListener(listener);

        // Assert
        Assert.NotNull(listener.Items);
        Assert.Empty(listener.Items);
    }
    #region Helper methods

    private enum FileType
    {
        MarkdownToc,
        YamlToc,
        MarkdownContent
    }

    private sealed class FileCreator
    {
        private const string MarkdownTocName = "toc.md";
        private const string YamlTocName = "toc.yml";
        private readonly string _rootDir;
        public FileCreator(string rootDir)
        {
            _rootDir = rootDir ?? Directory.GetCurrentDirectory();
        }

        public string CreateFile(string content, FileType type, string folder = null, string fileNameWithoutExtension = null)
        {
            string fileName;
            switch (type)
            {
                case FileType.MarkdownToc:
                    fileName = MarkdownTocName;
                    break;
                case FileType.YamlToc:
                    fileName = YamlTocName;
                    break;
                case FileType.MarkdownContent:
                    fileName = (fileNameWithoutExtension ?? Path.GetRandomFileName()) + ".md";
                    break;
                default:
                    throw new NotSupportedException(type.ToString());
            }

            fileName = Path.Combine(folder ?? string.Empty, fileName);

            var filePath = Path.Combine(_rootDir, fileName);
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(filePath, content);
            return fileName.Replace('\\', '/');
        }
    }

    private void BuildDocument(FileCollection files)
    {
        var parameters = new DocumentBuildParameters
        {
            Files = files,
            OutputBaseDir = _outputFolder,
            ApplyTemplateSettings = _applyTemplateSettings,
            Metadata = new Dictionary<string, object>
            {
                ["meta"] = "Hello world!",
            }.ToImmutableDictionary(),
        };

        using var builder = new DocumentBuilder([], []);
        builder.Build(parameters);
    }

    private static void AssertTocEqual(TocItemViewModel expected, TocItemViewModel actual, bool noMetadata = true)
    {
        using var swForExpected = new StringWriter();
        YamlUtility.Serialize(swForExpected, expected);
        using var swForActual = new StringWriter();
        if (noMetadata)
        {
            actual.Metadata.Clear();
        }
        YamlUtility.Serialize(swForActual, actual);
        Assert.Equal(swForExpected.ToString(), swForActual.ToString());
    }

    #endregion
}
