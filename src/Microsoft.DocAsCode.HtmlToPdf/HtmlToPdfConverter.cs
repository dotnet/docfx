// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Web;

using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Actions;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Outline.Destinations;
using UglyToad.PdfPig.Writer;

namespace Microsoft.DocAsCode.HtmlToPdf;

public class HtmlToPdfConverter
{
    #region Fields

    private const string OutLineKidsName = "Kids";
    private const int TimeoutInMilliseconds = 60 * 1000;

    private readonly HtmlToPdfOptions _htmlToPdfOptions;
    private readonly IList<HtmlModel> _htmlModels;
    private readonly IList<string> _htmlFilePaths;

    private int _currentNumberOfPages;

    #endregion

    #region Constructor

    public HtmlToPdfConverter(IList<HtmlModel> htmlModels, HtmlToPdfOptions htmlToPdfOptions)
    {
        Guard.ArgumentNotNull(htmlModels, nameof(htmlModels));
        Guard.Argument(() => htmlModels.All(p => p != null), nameof(htmlModels), $"{nameof(htmlModels)} cannot contain null htmlModel.");
        Guard.Argument(() => htmlModels.All(p => !string.IsNullOrEmpty(p.Title)), nameof(htmlModels), $"Title of {nameof(htmlModels)} must be provided.");
        Guard.ArgumentNotNull(htmlToPdfOptions, nameof(htmlToPdfOptions));

        _htmlToPdfOptions = htmlToPdfOptions;
        _htmlModels = htmlModels;
        _htmlFilePaths = new List<string>();
        ExtractHtmlPathFromHtmlModels(_htmlModels);
    }

    #endregion

    #region Public Methods

    public IDictionary<string, PartialPdfModel> GetPartialPdfModels(IList<string> htmlFilePaths)
    {
        Guard.ArgumentNotNull(htmlFilePaths, nameof(htmlFilePaths));
        Guard.Argument(() => htmlFilePaths.All(p => !string.IsNullOrEmpty(p)), nameof(htmlFilePaths), $"{nameof(htmlFilePaths)} cannot contain null or empty html file path.");

        var pdfFileNumberOfPages = new ConcurrentDictionary<string, PartialPdfModel>();

        Parallel.ForEach(
            htmlFilePaths,
            new ParallelOptions { MaxDegreeOfParallelism = _htmlToPdfOptions.MaxDegreeOfParallelism },
            htmlFilePath =>
            {
                var numberOfPages = Convert($"{WrapQuoteToPath(htmlFilePath)} -");

                PartialPdfModel pdfModel = new()
                {
                    FilePath = htmlFilePath,
                    NumberOfPages = numberOfPages
                };

                pdfFileNumberOfPages.TryAdd(htmlFilePath, pdfModel);
            });

        return pdfFileNumberOfPages;
    }

    public void Save(string outputFileName)
    {
        Guard.ArgumentNotNullOrEmpty(outputFileName, nameof(outputFileName));
        Guard.ArgumentNotNullOrEmpty(Path.GetFileName(outputFileName), $"There is no file name {nameof(outputFileName)}.");

        string directoryName = Path.GetDirectoryName(outputFileName);
        if (!string.IsNullOrEmpty(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        using var fileStream = new FileStream(outputFileName, FileMode.Create);
        SaveCore(fileStream);
    }

    #endregion

    #region Private Methods

    private static string NormalizePath(string path)
    {
        Guard.ArgumentNotNullOrEmpty(path, nameof(path));

        return HttpUtility.UrlDecode(path.Replace('\\', '/'));
    }

    private static string WrapQuoteToPath(string path)
    {
        Guard.ArgumentNotNullOrEmpty(path, nameof(path));

        return $"\"{NormalizePath(path)}\"";
    }

    private void ConvertToStreamCore(string arguments, Stream stream)
    {
        // In advanced scenarios where the user is passing additional arguments directly to the command line,
        // disable the quiet mode so problems can be diagnosed.
        if (!string.IsNullOrEmpty(this._htmlToPdfOptions.AdditionalArguments))
        {
            this._htmlToPdfOptions.IsQuiet = false;
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardInput = _htmlToPdfOptions.IsReadArgsFromStdin,
                RedirectStandardOutput = _htmlToPdfOptions.IsOutputToStdout,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = _htmlToPdfOptions.FilePath ?? Constants.PdfCommandName,
                Arguments = _htmlToPdfOptions + (_htmlToPdfOptions.IsReadArgsFromStdin ? string.Empty : (" " + arguments)),
            }
        };
        using (new LoggerPhaseScope(Constants.PdfCommandName))
        {
            Logger.LogVerbose($"Executing {process.StartInfo.FileName} {process.StartInfo.Arguments} ({arguments})");
            process.Start();
            if (_htmlToPdfOptions.IsReadArgsFromStdin)
            {
                using var standardInput = process.StandardInput;
                standardInput.AutoFlush = true;
                standardInput.Write(arguments);
            }
            if (_htmlToPdfOptions.IsOutputToStdout)
            {
                using (var standardOutput = process.StandardOutput)
                {
                    standardOutput.BaseStream.CopyTo(stream);
                }
                if (stream.CanSeek)
                    Logger.LogVerbose($"got {process.StartInfo.FileName} output {stream.Length}Bytes");
            }
            process.WaitForExit(TimeoutInMilliseconds);
        }
    }

    private void ConvertToStream(string arguments, Stream stream)
    {
        try
        {
            RetryHelper.Retry(() => ConvertToStreamCore(arguments, stream), _htmlToPdfOptions.RetryIntervals);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Convert the file : " + arguments + " has exception, the details: " + ex.Message);
        }
    }

    private BookmarkNode[] CreateOutlines(IList<HtmlModel> htmlModels, IDictionary<string, PartialPdfModel> pdfPages)
    {
        if (htmlModels is null || htmlModels.Count is 0)
            return Array.Empty<BookmarkNode>();

        return htmlModels.Select<HtmlModel, BookmarkNode>(htmlModel =>
        {
            if (!string.IsNullOrEmpty(htmlModel.ExternalLink))
            {
                return new UriBookmarkNode(htmlModel.Title, 0, htmlModel.ExternalLink, CreateOutlines(htmlModel.Children, pdfPages));
            }

            int pageNumber = 0;

            if (!string.IsNullOrEmpty(htmlModel.HtmlFilePath))
            {
                string filePath = GetFilePath(htmlModel.HtmlFilePath);

                if (pdfPages.ContainsKey(filePath))
                {
                    PartialPdfModel pdfModel = pdfPages[filePath];

                    if (!pdfModel.PageNumber.HasValue)
                    {
                        pdfModel.PageNumber = _currentNumberOfPages;
                        _currentNumberOfPages += pdfModel.NumberOfPages;
                    }

                    pageNumber = pdfModel.PageNumber.Value;
                }
            }
            else
            {
                // this is a parent node for the next topic
                pageNumber = _currentNumberOfPages;
            }

            return new DocumentBookmarkNode(
                htmlModel.Title, 0,
                new(pageNumber, ExplicitDestinationType.FitHorizontally, ExplicitDestinationCoordinates.Empty),
                CreateOutlines(htmlModel.Children, pdfPages));
        }).ToArray();
    }

    private BookmarkNode[] ConvertOutlines()
    {
        var pdfFileNumberOfPages = GetPartialPdfModels(new List<string>(_htmlFilePaths));
        _currentNumberOfPages = 1;
        return CreateOutlines(_htmlModels, pdfFileNumberOfPages);
    }

    private BookmarkNode[] GetOutlines()
    {
        switch (_htmlToPdfOptions.OutlineOption)
        {
            case OutlineOption.NoOutline:
            case OutlineOption.WkDefaultOutline:
                return null;
            case OutlineOption.DefaultOutline:
                return ConvertOutlines();
            default:
                throw new NotSupportedException(_htmlToPdfOptions.OutlineOption.ToString());
        }
    }

    private void ExtractHtmlPathFromHtmlModels(IList<HtmlModel> htmlModels)
    {
        if (htmlModels?.Count > 0)
        {
            foreach (var htmlModel in htmlModels)
            {
                if (!string.IsNullOrEmpty(htmlModel.HtmlFilePath))
                {
                    string filePath = GetFilePath(htmlModel.HtmlFilePath);
                    if ((!_htmlFilePaths.Contains(filePath))
                        && File.Exists(NormalizePath(filePath)))
                    {
                        _htmlFilePaths.Add(filePath);
                    }
                }
                ExtractHtmlPathFromHtmlModels(htmlModel.Children);
            }
        }
    }

    private string GetFilePath(string htmlFilePath)
    {
        var basePath = string.IsNullOrEmpty(_htmlToPdfOptions.BasePath) ? EnvironmentContext.BaseDirectory : _htmlToPdfOptions.BasePath;
        return Path.Combine(basePath, htmlFilePath);
    }

    private void SaveCore(Stream stream)
    {
        if (_htmlFilePaths.Count > 0)
        {
            using var pdfStream = new MemoryStream();
            ConvertToStream($"{string.Join(" ", _htmlFilePaths.Select(WrapQuoteToPath))} -", pdfStream);
            pdfStream.Position = 0;

            WriteOutlines(pdfStream, stream);
        }
    }

    private int Convert(string arguments)
    {
        using var pdfStream = new MemoryStream();
        ConvertToStream(arguments, pdfStream);
        pdfStream.Position = 0;

        using var document = PdfDocument.Open(pdfStream);
        return document.NumberOfPages;
    }

    private void WriteOutlines(MemoryStream input, Stream output)
    {
        using var document = PdfDocument.Open(input);
        using var builder = new PdfDocumentBuilder(output);

        for (var i = 1; i <= document.NumberOfPages; i++)
        {
            builder.AddPage(document, i, CopyLink);
        }

        builder.Bookmarks = new(GetOutlines());

        PdfAction CopyLink(PdfAction action)
        {
            return action switch
            {
                GoToAction link => new GoToAction(new(link.Destination.PageNumber, link.Destination.Type, link.Destination.Coordinates)),
                _ => action,
            };
        }
    }

    #endregion
}
