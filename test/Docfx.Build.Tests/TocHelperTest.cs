// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;
using Xunit;

namespace Docfx.Build.TableOfContents;

[Collection("docfx STA")]
public class TocHelperTest: TestBase
{

    private readonly string _inputFolder;
    private readonly string _outputFolder;
    private readonly string _templateFolder;
    private TestLoggerListener Listener { get; set; }

    public TocHelperTest()
    {
        _inputFolder = GetRandomFolder();
        _outputFolder = GetRandomFolder();
        _templateFolder = GetRandomFolder();
        EnvironmentContext.SetBaseDirectory(Directory.GetCurrentDirectory());
        EnvironmentContext.SetOutputDirectory(_outputFolder);
    }

    public override void Dispose()
    {
        EnvironmentContext.Clean();
        base.Dispose();
    }

    [Fact]
    public void PopulateToc_ShouldPopulateTocItems()
    {

        // Arrange
        var tocFileRoot = CreateFile(Constants.TocYamlFileName,
            """
            auto: true
            """,
            _inputFolder);
        var conceptualFile = CreateFile("index.md",
            string.Empty,
            _inputFolder);
        var tocFileFolderA = CreateFile("foldera/toc.yml",
            """
            auto: true
            """,
            _inputFolder);
        var conceptualFile2 = CreateFile("foldera/index.md",
            string.Empty,
            _inputFolder);
        var tocFileFolderB = CreateFile("foldera/folderb/toc.yml",
            """
            auto: true
            """,
            _inputFolder);
        var conceptualFile3 = CreateFile("foldera/folderb/index.md",
            string.Empty,
            _inputFolder);

        var files = Directory.GetFiles(_inputFolder, "*.*",  SearchOption.AllDirectories).Select(f => f.Replace($"{_inputFolder}\\", "~/").Replace("\\", "/")) ;
        var rootTocModel = TocHelper.LoadSingleToc(tocFileRoot);
        var tocFolderAModel = TocHelper.LoadSingleToc(tocFileFolderA);
        var tocFolderBModel = TocHelper.LoadSingleToc(tocFileFolderB);
        Dictionary<string, TocItemViewModel> tocCache = new Dictionary<string, TocItemViewModel>();
        tocCache.Add(tocFileRoot.Replace($"{_inputFolder}/", "~/").Replace("/toc.yml", string.Empty), rootTocModel);
        tocCache.Add(tocFileFolderA.Replace($"{_inputFolder}/", "~/").Replace("/toc.yml", string.Empty), tocFolderAModel);
        tocCache.Add(tocFileFolderB.Replace($"{_inputFolder}/", "~/").Replace("/toc.yml", string.Empty), tocFolderBModel);

        // Act
        TocHelper.RecursivelyPopulateTocs(Constants.TocYamlFileName, files, tocCache);

        // Assert
        var expectedRootTocModel = new TocItemViewModel()
        {
            Auto = true,

            Items = new()
            {
                new TocItemViewModel()
                {
                    Name = "Index",
                    Href = "~/index.md"
                },
                new TocItemViewModel()
                {
                    Name = "Foldera",
                    Href = "./foldera/"
                }
            }
        };

        var expectedTocFolderAModel = new TocItemViewModel()
        {
            Auto = true,

            Items = new()
            {
                new TocItemViewModel()
                {
                    Name = "Index",
                    Href = "~/foldera/index.md"
                },
                new TocItemViewModel()
                {
                    Name = "Folderb",
                    Href = "./folderb/"
                }
            }
        };

        var expectedTocFolderBModel = new TocItemViewModel()
        {
            Auto = true,

            Items = new()
            {
                new TocItemViewModel()
                {
                    Name = "Index",
                    Href = "~/foldera/folderb/index.md"
                }
            }
        };

        AssertTocEqual(expectedRootTocModel, rootTocModel);
        AssertTocEqual(expectedTocFolderAModel, tocFolderAModel);
        AssertTocEqual(expectedTocFolderBModel, tocFolderBModel);
    }

    [Fact]
    public void PopulateToc_ShouldNotPopulateToc_WhenAutoIsFalse()
    {

        // Arrange
        var tocFileRoot = CreateFile(Constants.TocYamlFileName,
            """
            auto: false
            """,
            _inputFolder);

        var files = Directory.GetFiles(_inputFolder, "*.*", SearchOption.AllDirectories).Select(f => f.Replace($"{_inputFolder}\\", "~/").Replace("\\", "/"));
        var rootTocModel = TocHelper.LoadSingleToc(tocFileRoot);
        Dictionary<string, TocItemViewModel> tocCache = new Dictionary<string, TocItemViewModel>();
        tocCache.Add(tocFileRoot.Replace($"{_inputFolder}/", "~/").Replace("/toc.yml", string.Empty), rootTocModel);

        // Act
        TocHelper.RecursivelyPopulateTocs(Constants.TocYamlFileName, files, tocCache);

        // Assert
        var expectedRootTocModel = new TocItemViewModel()
        {
            Auto = false
        };

        AssertTocEqual(expectedRootTocModel, rootTocModel);
    }


    [Fact]
    public void PopulateToc_ShouldNotPopulate_AutoIsFalseForSubFolder()
    {

        // Arrange
        // Arrange
        var tocFileRoot = CreateFile(Constants.TocYamlFileName,
            """
            auto: true
            """,
            _inputFolder);
        var conceptualFile = CreateFile("index.md",
            string.Empty,
            _inputFolder);
        var tocFileFolderA = CreateFile("folder-a/toc.yml",
            """
            auto: true
            """,
            _inputFolder);
        var conceptualFile2 = CreateFile("folder-a/index.md",
            string.Empty,
            _inputFolder);
        var tocFileFolderB = CreateFile("folder-a/folderb/toc.yml",
            """
            auto: false
            """,
            _inputFolder);

        var files = Directory.GetFiles(_inputFolder, "*.*", SearchOption.AllDirectories).Select(f => f.Replace($"{_inputFolder}\\", "~/").Replace("\\", "/"));
        var rootTocModel = TocHelper.LoadSingleToc(tocFileRoot);
        var tocFolderAModel = TocHelper.LoadSingleToc(tocFileFolderA);
        var tocFolderBModel = TocHelper.LoadSingleToc(tocFileFolderB);
        Dictionary<string, TocItemViewModel> tocCache = new Dictionary<string, TocItemViewModel>();
        tocCache.Add(tocFileRoot.Replace($"{_inputFolder}/", "~/").Replace("/toc.yml", string.Empty), rootTocModel);
        tocCache.Add(tocFileFolderA.Replace($"{_inputFolder}/", "~/").Replace("/toc.yml", string.Empty), tocFolderAModel);
        tocCache.Add(tocFileFolderB.Replace($"{_inputFolder}/", "~/").Replace("/toc.yml", string.Empty), tocFolderBModel);

        // Act
        TocHelper.RecursivelyPopulateTocs(Constants.TocYamlFileName, files, tocCache);

        // Assert
        var expectedRootTocModel = new TocItemViewModel()
        {
            Auto = true,

            Items = new()
            {
                new TocItemViewModel()
                {
                    Name = "Index",
                    Href = "~/index.md"
                },
                new TocItemViewModel()
                {
                    Name = "Folder A",
                    Href = "./folder-a/"
                }
            }
        };

        var expectedTocFolderAModel = new TocItemViewModel()
        {
            Auto = true,

            Items = new()
            {
                new TocItemViewModel()
                {
                    Name = "Index",
                    Href = "~/folder-a/index.md"
                }
            }
        };

        var expectedTocFolderBModel = new TocItemViewModel()
        {
            Auto = false,
        };

        AssertTocEqual(expectedRootTocModel, rootTocModel);
        AssertTocEqual(expectedTocFolderAModel, tocFolderAModel);
        AssertTocEqual(expectedTocFolderBModel, tocFolderBModel);
    }


    [Fact]
    public void PopulateToc_ShouldNotOverrideExisting_FileHrefs()
    {

        // Arrange
        var tocFileRoot = CreateFile(Constants.TocYamlFileName,
            """
            auto: true
            items:
            - name: overrideindex
              href: index.md
            """,
            _inputFolder);
        var conceptualFile = CreateFile("index.md",
            string.Empty,
            _inputFolder);
        var tocFileFolderA = CreateFile("foldera/toc.yml",
            """
            auto: true
            """,
            _inputFolder);
        var conceptualFile2 = CreateFile("foldera/index.md",
            string.Empty,
            _inputFolder);
        var tocFileFolderB = CreateFile("foldera/folderb/toc.yml",
            """
            auto: true
            """,
            _inputFolder);
        var conceptualFile3 = CreateFile("foldera/folderb/index.md",
            string.Empty,
            _inputFolder);

        var files = Directory.GetFiles(_inputFolder, "*.*", SearchOption.AllDirectories).Select(f => f.Replace($"{_inputFolder}\\", "~/").Replace("\\", "/"));
        var rootTocModel = TocHelper.LoadSingleToc(tocFileRoot);
        var tocFolderAModel = TocHelper.LoadSingleToc(tocFileFolderA);
        var tocFolderBModel = TocHelper.LoadSingleToc(tocFileFolderB);
        Dictionary<string, TocItemViewModel> tocCache = new Dictionary<string, TocItemViewModel>();
        tocCache.Add(tocFileRoot.Replace($"{_inputFolder}/", "~/").Replace("/toc.yml", string.Empty), rootTocModel);
        tocCache.Add(tocFileFolderA.Replace($"{_inputFolder}/", "~/").Replace("/toc.yml", string.Empty), tocFolderAModel);
        tocCache.Add(tocFileFolderB.Replace($"{_inputFolder}/", "~/").Replace("/toc.yml", string.Empty), tocFolderBModel);

        // Act
        TocHelper.RecursivelyPopulateTocs(Constants.TocYamlFileName, files, tocCache);

        // Assert
        var expectedRootTocModel = new TocItemViewModel()
        {
            Auto = true,

            Items = new()
            {
                new TocItemViewModel()
                {
                    Name = "overrideindex",
                    Href = "index.md"
                },
                new TocItemViewModel()
                {
                    Name = "Foldera",
                    Href = "./foldera/"
                }
            }
        };

        var expectedTocFolderAModel = new TocItemViewModel()
        {
            Auto = true,

            Items = new()
            {
                new TocItemViewModel()
                {
                    Name = "Index",
                    Href = "~/foldera/index.md"
                },
                new TocItemViewModel()
                {
                    Name = "Folderb",
                    Href = "./folderb/"
                }
            }
        };

        var expectedTocFolderBModel = new TocItemViewModel()
        {
            Auto = true,

            Items = new()
            {
                new TocItemViewModel()
                {
                    Name = "Index",
                    Href = "~/foldera/folderb/index.md"
                }
            }
        };

        AssertTocEqual(expectedRootTocModel, rootTocModel);
        AssertTocEqual(expectedTocFolderAModel, tocFolderAModel);
        AssertTocEqual(expectedTocFolderBModel, tocFolderBModel);
    }

    [Fact]
    public void PopulateToc_ShouldNotOverrideExisting_FolderHrefs()
    {

        // Arrange
        var tocFileRoot = CreateFile(Constants.TocYamlFileName,
            """
            auto: true
            items:
            - name: overrideindex
              href: index.md
            - name: overridefolderareference
              href: foldera/
            """,
            _inputFolder);
        var conceptualFile = CreateFile("index.md",
            string.Empty,
            _inputFolder);
        var tocFileFolderA = CreateFile("foldera/toc.yml",
            """
            auto: true
            """,
            _inputFolder);
        var conceptualFile2 = CreateFile("foldera/index.md",
            string.Empty,
            _inputFolder);
        var tocFileFolderB = CreateFile("foldera/folderb/toc.yml",
            """
            auto: true
            """,
            _inputFolder);
        var conceptualFile3 = CreateFile("foldera/folderb/index.md",
            string.Empty,
            _inputFolder);

        var files = Directory.GetFiles(_inputFolder, "*.*", SearchOption.AllDirectories).Select(f => f.Replace($"{_inputFolder}\\", "~/").Replace("\\", "/"));
        var rootTocModel = TocHelper.LoadSingleToc(tocFileRoot);
        var tocFolderAModel = TocHelper.LoadSingleToc(tocFileFolderA);
        var tocFolderBModel = TocHelper.LoadSingleToc(tocFileFolderB);
        Dictionary<string, TocItemViewModel> tocCache = new Dictionary<string, TocItemViewModel>();
        tocCache.Add(tocFileRoot.Replace($"{_inputFolder}/", "~/").Replace("/toc.yml", string.Empty), rootTocModel);
        tocCache.Add(tocFileFolderA.Replace($"{_inputFolder}/", "~/").Replace("/toc.yml", string.Empty), tocFolderAModel);
        tocCache.Add(tocFileFolderB.Replace($"{_inputFolder}/", "~/").Replace("/toc.yml", string.Empty), tocFolderBModel);

        // Act
        TocHelper.RecursivelyPopulateTocs(Constants.TocYamlFileName, files, tocCache);

        // Assert
        var expectedRootTocModel = new TocItemViewModel()
        {
            Auto = true,

            Items = new()
            {
                new TocItemViewModel()
                {
                    Name = "overrideindex",
                    Href = "index.md"
                },
                new TocItemViewModel()
                {
                    Name = "overridefolderareference",
                    Href = "foldera/"
                }
            }
        };

        var expectedTocFolderAModel = new TocItemViewModel()
        {
            Auto = true,

            Items = new()
            {
                new TocItemViewModel()
                {
                    Name = "Index",
                    Href = "~/foldera/index.md"
                },
                new TocItemViewModel()
                {
                    Name = "Folderb",
                    Href = "./folderb/"
                }
            }
        };

        var expectedTocFolderBModel = new TocItemViewModel()
        {
            Auto = true,

            Items = new()
            {
                new TocItemViewModel()
                {
                    Name = "Index",
                    Href = "~/foldera/folderb/index.md"
                }
            }
        };

        AssertTocEqual(expectedRootTocModel, rootTocModel);
        AssertTocEqual(expectedTocFolderAModel, tocFolderAModel);
        AssertTocEqual(expectedTocFolderBModel, tocFolderBModel);
    }

    internal static void AssertTocEqual(TocItemViewModel expected, TocItemViewModel actual, bool noMetadata = true)
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
}

