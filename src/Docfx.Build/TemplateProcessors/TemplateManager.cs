// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

using Switches = Docfx.DataContracts.Common.Constants.Switches;

#nullable enable

namespace Docfx.Build.Engine;

public class TemplateManager
{
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
        var dotnetToolTemplatesDir = GetDotnetToolTemplatesDir();

        foreach (var name in names)
        {
            // Search template from exe's templates directory.
            var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "templates", name));
            if (Directory.Exists(directory))
            {
                yield return directory;
            }

            if (dotnetToolTemplatesDir != null)
            {
                // Search template from DotNetTool's templates directories.
                directory = Path.GetFullPath(Path.Combine(dotnetToolTemplatesDir, name));
                if (Directory.Exists(directory))
                {
                    yield return directory;
                }
            }

            // Search template from base directory.
            directory = Path.GetFullPath(Path.Combine(_baseDirectory, name));
            if (Directory.Exists(directory))
            {
                yield return directory;
            }
        }
    }

    public void ProcessTheme(string outputDirectory, bool overwrite)
    {
        if (_themes is { Count: > 0 })
        {
            TryExportResourceFiles(_themes, outputDirectory, overwrite);
            Logger.LogInfo($"Theme(s) {_themes.ToDelimitedString()} applied.");
        }
    }

    private bool TryExportResourceFiles(List<string> resourceNames, string outputDirectory, bool overwrite, string? regexFilter = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);

        if (!resourceNames.Any())
            return false;

        bool isEmpty = true;

        var templateResource = CreateTemplateResource(resourceNames);
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
            if (!string.IsNullOrEmpty(subfolder))
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

    private static string? GetDotnetToolTemplatesDir()
    {
        if (!Switches.IsDotnetToolsMode)
            return null;

        string templatesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../templates"));
        if (!Directory.Exists(templatesDir))
        {
            Logger.LogWarning($".NET Tools templates directory is not found. Path: {templatesDir}");
            return null;
        }
        return templatesDir;
    }
}
