// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.PdfConverter
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Web;

    internal class HtmlToPdfConverter
    {
        #region Fields

        private const string WkHtmlToPdfExeName = "wkhtmltopdf";
        private const int TimeoutInMilliseconds = 60 * 1000;

        private readonly HtmlToPdfOptions _htmlToPdfOptions;
        private readonly IList<HtmlModel> _htmlModels;
        private readonly IList<string> _htmlFilePaths;

        #endregion

        #region Constructor

        public HtmlToPdfConverter(IList<HtmlModel> htmlModels, HtmlToPdfOptions htmlToPdfOptions)
        {
            _htmlToPdfOptions = htmlToPdfOptions ?? throw new ArgumentNullException(nameof(htmlToPdfOptions));
            _htmlModels = htmlModels ?? throw new ArgumentNullException(nameof(htmlModels));
            _htmlFilePaths = new List<string>();
            ExtractHtmlPathFromHtmlModels(_htmlModels);
        }

        #endregion

        #region Public Methods

        public Stream Save()
        {
            var outputStream = new MemoryStream();
            SaveCore(outputStream);

            return outputStream;
        }

        public void Save(string outputFileName)
        {
            if (string.IsNullOrEmpty(outputFileName))
            {
                throw new ArgumentNullException(nameof(outputFileName));
            }

            using (var fileStream = EnvironmentContext.FileAbstractLayer.Create(outputFileName))
            {
                SaveCore(fileStream);
            }
        }

        #endregion

        #region Private Methods

        private static string NormalizePath(string path)
        {
            return HttpUtility.UrlDecode(path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        private static string WrapQuoteToPath(string path)
        {
            return $"\"{NormalizePath(path)}\"";
        }

        private void ConvertToStreamCore(string arguments, Stream stream)
        {
            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardInput = _htmlToPdfOptions.IsReadArgsFromStdin,
                    RedirectStandardOutput = _htmlToPdfOptions.IsOutputToStdout,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = WkHtmlToPdfExeName,
                    Arguments = _htmlToPdfOptions + (_htmlToPdfOptions.IsReadArgsFromStdin ? string.Empty : arguments)
                }
            })
            {
                Logger.LogVerbose($"Executing {process.StartInfo.FileName} {process.StartInfo.Arguments}");
                process.Start();
                if (_htmlToPdfOptions.IsReadArgsFromStdin)
                {
                    using (var standardInput = process.StandardInput)
                    {
                        standardInput.AutoFlush = true;
                        standardInput.Write(arguments);
                        Logger.LogVerbose($"Input arguments: {arguments}");
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

        private void ExtractHtmlPathFromHtmlModels(IList<HtmlModel> htmlModels)
        {
            if (htmlModels != null && htmlModels.Count > 0)
            {
                foreach (var htmlModel in htmlModels)
                {
                    if (!string.IsNullOrEmpty(htmlModel.HtmlFilePath))
                    {
                        string filePath = GetFilePath(htmlModel.HtmlFilePath);
                        if (File.Exists(filePath))
                        {
                            _htmlFilePaths.Add(filePath);
                        }
                        else
                        {
                            Logger.LogWarning($"Unable to find {filePath}, exclude from pdf.");
                        }
                    }
                    ExtractHtmlPathFromHtmlModels(htmlModel.Children);
                }
            }
        }

        private string GetFilePath(string htmlFilePath)
        {
            if (File.Exists(htmlFilePath))
            {
                return htmlFilePath;
            }
            return string.IsNullOrEmpty(_htmlToPdfOptions.BasePath) ? htmlFilePath : Path.Combine(_htmlToPdfOptions.BasePath, htmlFilePath);
        }

        private void SaveCore(Stream stream)
        {
            if (_htmlFilePaths.Count > 0)
            {
                using (var pdfStream = new MemoryStream())
                {
                    ConvertToStream($"{string.Join(" ", _htmlFilePaths.Select(WrapQuoteToPath))} -", pdfStream);
                    pdfStream.Position = 0;

                    pdfStream.CopyTo(stream);
                }
            }
        }

        #endregion
    }
}
