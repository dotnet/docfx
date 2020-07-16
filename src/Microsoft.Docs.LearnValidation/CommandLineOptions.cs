using Microsoft.OpenPublishing.PluginHelper;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;

namespace TripleCrownValidation
{
    public class CommandLineOptions
    {
        public string LogFilePath = "log.json";
        public string OriginalManifestPath = null;
        public string RepoRootPath = null;
        public string XRefEndpoint = null;
        public string XRefTags = null;
        public string Locale = null;
        public string Branch = null;
        public string TripleCrownEndpoint = null;
        public string DependencyFilePath = null;
        public string DocsetFolder = null;
        public string DocsetName = null;
        public string DrySyncEndpoint = null;
        public string FallbackFolders = null;
        public string RepoUrl = null;
        public string SkipPublishFilePath = null;
        public bool IsServerBuild = false;
        public bool ContinueWithError = false;

        List<string> Extras = null;
        OptionSet _options = null;

        public CommandLineOptions()
        {
            _options = new OptionSet {
                { "log=", "the log file path.", l => LogFilePath = l },
                { "originalManifest=", "build/docfx .manifest file path",  os => OriginalManifestPath = os },
                { "repoRootPath=", "Absolute local path to repo root.", r => RepoRootPath = r },
                { "xrefEndpoint=", "", r => XRefEndpoint = r },
                { "xrefTags=", "", r => XRefTags = r },
                { "locale=", "", l => Locale = l },
                { "branch=", "", b => Branch = b },
                { "tripleCrownEndpoint=", "", t => TripleCrownEndpoint = t },
                { "drySyncEndpoint=", "", d => DrySyncEndpoint = d },
                { "dependencyFilePath=", "", dfp => DependencyFilePath = dfp },
                { "docsetFolder=", "", df => DocsetFolder = df },
                { "docsetName=", "", d => DocsetName = d },
                { "fallbackFolders=", "", ff => FallbackFolders = ff },
                { "repoUrl=", "", r => RepoUrl = r },
                { "skipPublishFilePath=", "", spf => SkipPublishFilePath = spf },
                { "isServerBuild", "", isb => IsServerBuild = true },
                { "continueWithError", "", cwe => ContinueWithError = true }

            };
        }

        public bool Parse(string[] args)
        {
            Extras = _options.Parse(args);
            if (string.IsNullOrEmpty(DependencyFilePath) || !File.Exists(DependencyFilePath))
            {
                //OPSLogger.LogSystemError(LogCode.TripleCrown_DependencyFile_NotExist, LogMessageUtility.FormatMessage(LogCode.TripleCrown_DependencyFile_NotExist, DependencyFilePath));
                PrintUsage();
                return false;
            }
            if (string.IsNullOrEmpty(DocsetFolder))
            {
                //OPSLogger.LogSystemError(LogCode.TripleCrown_DocsetFolder_IsNull, LogMessageUtility.FormatMessage(LogCode.TripleCrown_DocsetFolder_IsNull));
                PrintUsage();
                return false;
            }
            if (string.Compare(Locale, "en-us", true) != 0 && string.IsNullOrEmpty(RepoRootPath))
            {
                //OPSLogger.LogSystemError(LogCode.TripleCrown_RepoRootPath_IsNull, LogMessageUtility.FormatMessage(LogCode.TripleCrown_RepoRootPath_IsNull));
                PrintUsage();
                return false;
            }
            if (ContinueWithError && string.IsNullOrEmpty(OriginalManifestPath))
            {
                //OPSLogger.LogSystemError(LogCode.TripleCrown_ManifestFile_NotExist, LogMessageUtility.FormatMessage(LogCode.TripleCrown_ManifestFile_NotExist, OriginalManifestPath));
                PrintUsage();
                return false;
            }

            return true;
        }

        private void PrintUsage()
        {
            //OPSLogger.LogUserError(LogCode.OPSPlugins_Command_Invalid, LogMessageUtility.FormatMessage(LogCode.OPSPlugins_Command_Invalid));
            Console.WriteLine("Usage: TripleCrownValidation.exe <Options>");
            _options.WriteOptionDescriptions(Console.Out);
        }
    }
}
