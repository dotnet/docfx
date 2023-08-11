// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

#nullable enable

namespace Docfx.Build.Engine;

public class TemplateManager
{
    private const string DOCFX_CUSTOM_TEMPLATES_DIR = DataContracts.Common.Constants.EnvironmentVariables.DOCFX_CUSTOM_TEMPLATES_DIR;
    private readonly List<string> _templates;
    private readonly List<string>? _themes;
    private readonly string _baseDirectory;

    public TemplateManager(List<string> templates, List<string>? themes, string? baseDirectory)
    {
        _templates = templates;
        _themes = themes;
        _baseDirectory = baseDirectory ?? Directory.GetCurrentDirectory();
    }

    public bool TryExportTemplateFiles(string outputDirectory, string? regexFilter = null)
    {
        return TryExportResourceFiles(_templates, outputDirectory, true, regexFilter);
    }

    public TemplateProcessor GetTemplateProcessor(DocumentBuildContext context, int maxParallelism)
    {
        return new TemplateProcessor(CreateTemplateResource(_templates), context, maxParallelism);
    }

    public CompositeResourceReader CreateTemplateResource() => CreateTemplateResource(_templates);

    private CompositeResourceReader CreateTemplateResource(IEnumerable<string> resources)
    {
        return new(GetTemplateDirectories(resources).Select(path => new LocalFileResourceReader(path)));
    }

    public IEnumerable<string> GetTemplateDirectories()
    {
        return GetTemplateDirectories(_templates);
    }


    private IEnumerable<string> GetTemplateDirectories(IEnumerable<string> names)
    {
        var customTemplatesDir = Environment.GetEnvironmentVariable(DOCFX_CUSTOM_TEMPLATES_DIR);
        if (customTemplatesDir != null && !Directory.Exists(customTemplatesDir))
        {
            Logger.LogWarning($"Custom templates directory is not found. {DOCFX_CUSTOM_TEMPLATES_DIR}: {customTemplatesDir}");
            customTemplatesDir = null;
        }

        foreach (var name in names)
        {
            //  Search template from exe's templates directory.
            var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "templates", name));
            if (Directory.Exists(directory))
            {
                yield return directory;
            }

            // Search template from directory specified by environment variable.
            if (customTemplatesDir != null)
            {
                var templateDir = Path.GetFullPath(Path.Combine(customTemplatesDir, name));
                if (Directory.Exists(templateDir))
                {
                    yield return templateDir;
                }
            }

            // Search templates from base directory.
            directory = Path.GetFullPath(Path.Combine(_baseDirectory, name));
            if (Directory.Exists(directory))
            {
                yield return directory;
            }
        }
    }

    public void ProcessTheme(string outputDirectory, bool overwrite)
    {
        using (new LoggerPhaseScope("Apply Theme", LogLevel.Verbose))
        {
            if (_themes != null && _themes.Count > 0)
            {
                TryExportResourceFiles(_themes, outputDirectory, overwrite);
                Logger.LogInfo($"Theme(s) {_themes.ToDelimitedString()} applied.");
            }
        }
    }

    private bool TryExportResourceFiles(IEnumerable<string> resourceNames, string outputDirectory, bool overwrite, string? regexFilter = null)
    {
#if NET7_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);
#else
        if (string.IsNullOrEmpty(outputDirectory))
            throw new ArgumentNullException(nameof(outputDirectory));
#endif

        if (!resourceNames.Any())
            return false;

        bool isEmpty = true;

        using (new LoggerPhaseScope("ExportResourceFiles", LogLevel.Verbose))
        {
            using var templateResource = CreateTemplateResource(resourceNames);
            if (templateResource.IsEmpty)
            {
                Logger.Log(LogLevel.Warning, $"No resource found for [{StringExtension.ToDelimitedString(resourceNames)}].");
            }
            else
            {
                foreach (var pair in templateResource.GetResourceStreams(regexFilter))
                {
                    var outputPath = Path.Combine(outputDirectory, pair.Key);
                    CopyResource(pair.Value, outputPath, overwrite);
                    Logger.Log(LogLevel.Verbose, $"File {pair.Key} copied to {outputPath}.");
                    isEmpty = false;
                }
            }

            return !isEmpty;
        }
    }

    private static void CopyResource(Stream stream, string filePath, bool overwrite)
    {
        Copy(stream.CopyTo, filePath, overwrite);
    }

    private static void Copy(Action<Stream> streamHandler, string filePath, bool overwrite)
    {
        FileMode fileMode = overwrite ? FileMode.Create : FileMode.CreateNew;
        try
        {
            var subfolder = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(subfolder) && !Directory.Exists(subfolder))
            {
                Directory.CreateDirectory(subfolder);
            }

            using var fs = new FileStream(filePath, fileMode, FileAccess.ReadWrite, FileShare.ReadWrite);
            streamHandler(fs);
        }
        catch (IOException e)
        {
            // If the file already exists, skip
            Logger.Log(LogLevel.Info, $"File {filePath}: {e.Message}, skipped");
        }
    }
}
