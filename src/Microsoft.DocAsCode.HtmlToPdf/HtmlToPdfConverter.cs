// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using PdfSharp.Pdf;
    using PdfSharp.Pdf.Actions;
    using PdfSharp.Pdf.Advanced;
    using PdfSharp.Pdf.IO;

    public class HtmlToPdfConverter
    {
        #region Fields

        private const string OutLineKidsName = "Kids";
        private const int TimeoutInMilliseconds = 60 * 1000;

        private readonly HtmlToPdfOptions _htmlToPdfOptions;
        private readonly IList<HtmlModel> _htmlModels;
        private readonly IList<string> _htmlFilePaths;

        private int _currentNumberOfPages;

        public IList<Dictionary<string, object>> CustomOutlines { get; set; }

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

        public IDictionary<string, int> GetHtmlToPdfNumberOfPages(IList<string> htmlFilePaths)
        {
            Guard.ArgumentNotNull(htmlFilePaths, nameof(htmlFilePaths));
            Guard.Argument(() => htmlFilePaths.All(p => !string.IsNullOrEmpty(p)), nameof(htmlFilePaths), $"{nameof(htmlFilePaths)} cannot contain null or empty html file path.");

            var pdfFileNumberOfPages = new ConcurrentDictionary<string, int>();

            Parallel.ForEach(
                htmlFilePaths,
                new ParallelOptions { MaxDegreeOfParallelism = _htmlToPdfOptions.MaxDegreeOfParallelism },
                htmlFilePath =>
                {
                    var numberOfPages = Convert($"{WrapQuoteToPath(htmlFilePath)} -", reader => reader.PageCount);
                    pdfFileNumberOfPages.TryAdd(htmlFilePath, numberOfPages);
                });

            return pdfFileNumberOfPages;
        }

        public void Save(string outputFileName)
        {
            Guard.ArgumentNotNullOrEmpty(outputFileName, nameof(outputFileName));
            Guard.ArgumentNotNullOrEmpty(Path.GetFileName(outputFileName), $"There is no file name {nameof(outputFileName)}.");
            GuardCustomOutlinesNotNull();

            string directoryName = Path.GetDirectoryName(outputFileName);
            if (!string.IsNullOrEmpty(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            using (var fileStream = new FileStream(outputFileName, FileMode.Create))
            {
                SaveCore(fileStream);
            }
        }

        #endregion

        #region Private Methods

        private static string NormalizePath(string path)
        {
            Guard.ArgumentNotNullOrEmpty(path, nameof(path));

            return HttpUtility.UrlDecode(path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        private static string WrapQuoteToPath(string path)
        {
            Guard.ArgumentNotNullOrEmpty(path, nameof(path));

            return $"\"{NormalizePath(path)}\"";
        }

        private void GuardCustomOutlinesNotNull()
        {
            if (_htmlToPdfOptions.OutlineOption == OutlineOption.CustomOutline)
            {
                Guard.ArgumentNotNull(CustomOutlines, nameof(CustomOutlines));
                Guard.Argument(() => CustomOutlines.All(p => p != null), nameof(CustomOutlines), $"{nameof(CustomOutlines)} cannot contain null outline.");
            }
        }

        private void ConvertToStreamCore(string arguments, Stream stream)
        {
            // In advanced scenarios where the user is passing additional arguments directly to the command line,
            // disable the quiet mode so problems can be diagnosed.
            if (!string.IsNullOrEmpty(this._htmlToPdfOptions.AdditionalArguments))
            {
                this._htmlToPdfOptions.IsQuiet = false;
            }

            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardInput = _htmlToPdfOptions.IsReadArgsFromStdin,
                    RedirectStandardOutput = _htmlToPdfOptions.IsOutputToStdout,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = Constants.PdfCommandName,
                    Arguments = _htmlToPdfOptions + (_htmlToPdfOptions.IsReadArgsFromStdin ? string.Empty : arguments)
                }
            })
            {
                using(new LoggerPhaseScope(Constants.PdfCommandName))
                {
                    Logger.LogVerbose($"Executing {process.StartInfo.FileName} {process.StartInfo.Arguments} {arguments}");
                    process.Start();
                    if (_htmlToPdfOptions.IsReadArgsFromStdin)
                    {
                        using (var standardInput = process.StandardInput)
                        {
                            standardInput.AutoFlush = true;
                            standardInput.Write(arguments);
                        }
                    }
                    if (_htmlToPdfOptions.IsOutputToStdout)
                    {
                        using (var standardOutput = process.StandardOutput)
                        {
                            standardOutput.BaseStream.CopyTo(stream);
                        }
                    }
                    process.WaitForExit(TimeoutInMilliseconds);
                }
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

        private void CreateOutlines(PdfOutlineCollection outlineCollection, IList<HtmlModel> htmlModels, IDictionary<string, int> pdfFileNumberOfPages)
        {
            if (htmlModels?.Count > 0)
            {
                foreach (var htmlModel in htmlModels)
                {
                    PdfOutline outline = new PdfOutline()
                    {
                        Title = htmlModel.Title,
                        Opened = true
                    };

                    if (!string.IsNullOrEmpty(htmlModel.ExternalLink))
                    {
                        outline.Elements.Add("/Type", new PdfString("/Action"));
                        outline.Elements.Add("/Subtype", new PdfString("/Link"));
                        outline.Elements.Add("/A", new PdfLiteral($"<</S/URI/URI({htmlModel.ExternalLink})>>"));
                    }
                    else
                    {
                        outline.DestinationPage = outlineCollection.Owner.Pages[_currentNumberOfPages - 1];
                        outline.PageDestinationType = PdfPageDestinationType.FitH;

                        if (!string.IsNullOrEmpty(htmlModel.HtmlFilePath))
                        {
                            string filePath = GetFilePath(htmlModel.HtmlFilePath);
                            if (pdfFileNumberOfPages.ContainsKey(filePath))
                            {
                                _currentNumberOfPages += pdfFileNumberOfPages[filePath];
                            }
                        }
                    }

                    outlineCollection.Add(outline);
                    CreateOutlines(outline.Outlines, htmlModel.Children, pdfFileNumberOfPages);
                }
            }
        }

        private void AddOutlines(PdfDocument pdfDocument)
        {
            var pdfFileNumberOfPages = GetHtmlToPdfNumberOfPages(new List<string>(_htmlFilePaths));
            _currentNumberOfPages = 1;

            CreateOutlines(pdfDocument.Outlines, _htmlModels, pdfFileNumberOfPages);
        }

        private void CreateOutlines(PdfDocument pdfDocument)
        {
            switch (_htmlToPdfOptions.OutlineOption)
            {
                case OutlineOption.CustomOutline:
                    throw new NotImplementedException();
                case OutlineOption.DefaultOutline:
                    AddOutlines(pdfDocument);
                    break;
                default:
                    return;
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
                using (var pdfStream = new MemoryStream())
                {
                    ConvertToStream($"{string.Join(" ", _htmlFilePaths.Select(WrapQuoteToPath))} -", pdfStream);
                    pdfStream.Position = 0;

                    using (var pdfDocument = PdfReader.Open(pdfStream))
                    {
                        CreateOutlines(pdfDocument);

                        pdfDocument.Save(stream);
                    }
                }
            }
        }

        private T Convert<T>(string arguments, Func<PdfDocument, T> readerFunc)
        {
            using (var pdfStream = new MemoryStream())
            {
                ConvertToStream(arguments, pdfStream);
                pdfStream.Position = 0;

                using (var pdfDocument = PdfReader.Open(pdfStream))
                {
                    return readerFunc(pdfDocument);
                }
            }
        }

        #endregion
    }
}
