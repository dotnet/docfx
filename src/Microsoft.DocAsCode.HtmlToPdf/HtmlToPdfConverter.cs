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

    using iTextSharp.text.pdf;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

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
                    var numberOfPages = Convert($"{WrapQuoteToPath(htmlFilePath)} -", reader => reader.NumberOfPages);
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

        private void CreateOutlines(Dictionary<string, object> rootOutline, IList<HtmlModel> htmlModels, IDictionary<string, int> pdfFileNumberOfPages)
        {
            if (htmlModels?.Count > 0)
            {
                foreach (var htmlModel in htmlModels)
                {
                    var outline = new Dictionary<string, object>
                    {
                        { "Title", htmlModel.Title },
                        { OutLineKidsName, new List<Dictionary<string, object>>() }
                    };

                    if (!string.IsNullOrEmpty(htmlModel.ExternalLink))
                    {
                        outline.Add("Action", "URI");
                        outline.Add("URI", htmlModel.ExternalLink);
                    }
                    else
                    {
                        outline.Add("Action", "GoTo");

                        // please go to http://api.itextpdf.com/itext/com/itextpdf/text/pdf/PdfDestination.html to find the detail.
                        outline.Add("Page", $"{_currentNumberOfPages} FitH");

                        if (!string.IsNullOrEmpty(htmlModel.HtmlFilePath))
                        {
                            string filePath = GetFilePath(htmlModel.HtmlFilePath);
                            if (pdfFileNumberOfPages.ContainsKey(filePath))
                            {
                                _currentNumberOfPages += pdfFileNumberOfPages[filePath];
                            }
                        }
                    }

                    ((List<Dictionary<string, object>>)rootOutline[OutLineKidsName]).Add(outline);
                    CreateOutlines(outline, htmlModel.Children, pdfFileNumberOfPages);
                }
            }
        }

        private List<Dictionary<string, object>> ConvertOutlines()
        {
            var pdfFileNumberOfPages = GetHtmlToPdfNumberOfPages(new List<string>(_htmlFilePaths));
            _currentNumberOfPages = 1;

            var rootOutline = new Dictionary<string, object>
            {
                { OutLineKidsName, new List<Dictionary<string, object>>() }
            };

            CreateOutlines(rootOutline, _htmlModels, pdfFileNumberOfPages);
            return (List<Dictionary<string, object>>)rootOutline[OutLineKidsName];
        }

        private IList<Dictionary<string, object>> GetOutlines()
        {
            switch (_htmlToPdfOptions.OutlineOption)
            {
                case OutlineOption.CustomOutline:
                    return CustomOutlines;
                case OutlineOption.DefaultOutline:
                    return ConvertOutlines();
                default:
                    return ConvertOutlines();
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
                        if (File.Exists(NormalizePath(filePath)))
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
                var outlines = GetOutlines();
                using (var pdfStream = new MemoryStream())
                {
                    ConvertToStream($"{string.Join(" ", _htmlFilePaths.Select(WrapQuoteToPath))} -", pdfStream);
                    pdfStream.Position = 0;

                    using (var pdfReader = new PdfReader(pdfStream))
                    {
                        using (var pdfStamper = new PdfStamper(pdfReader, stream))
                        {
                            pdfStamper.Outlines = outlines;
                        }
                    }
                }
            }
        }

        private T Convert<T>(string arguments, Func<PdfReader, T> readerFunc)
        {
            using (var pdfStream = new MemoryStream())
            {
                ConvertToStream(arguments, pdfStream);
                pdfStream.Position = 0;

                using (var pdfReader = new PdfReader(pdfStream))
                {
                    return readerFunc(pdfReader);
                }
            }
        }

        #endregion
    }
}
