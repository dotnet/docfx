// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace Docfx.Tests.Common;

public class TestBase : IClassFixture<TestBase>, IDisposable
{
    private readonly List<string> _folderCollection = new();
    private object _locker = new();

    protected string GetRandomFolder()
    {
        var folder = GetFolder();

        lock (_locker)
        {
            _folderCollection.Add(folder);
        }

        Directory.CreateDirectory(folder);
        return folder;
    }

    protected string MoveToRandomFolder(string origin)
    {
        var folder = GetFolder();

        lock (_locker)
        {
            _folderCollection.Remove(folder);
            _folderCollection.Add(folder);
        }

        Directory.Move(origin, folder);
        return folder;
    }

    private string GetFolder()
    {
        var folder = Path.GetRandomFileName();
        if (Directory.Exists(folder))
        {
            folder = folder + DateTime.Now.ToString("HHmmssffff");
            if (Directory.Exists(folder))
            {
                throw new InvalidOperationException($"Random folder name collides {folder}");
            }
        }
        return folder;
    }

    /// <summary>
    /// Find docfx solution folder.
    /// </summary>
    protected string GetSolutionFolder([CallerFilePath] string callerFilePath = "")
    {
        if (!File.Exists(callerFilePath))
        {
            // CallerFilePath is resolved at build timing.
            // If build/test is executed on separated machine. It failed to find file.
            throw new Exception($"File is not found. callerFilePath: {callerFilePath}");
        }

        const string SolutionName = "docfx";

        var dir = new FileInfo(callerFilePath).Directory;
        while (dir != null
            && dir.Name != SolutionName
            && !dir.EnumerateFiles($"{SolutionName}.sln").Any())
        {
            dir = dir.Parent;
        }

        if (dir == null)
            throw new Exception("Failed to find solution folder.");

        return dir.FullName;
    }

    #region IO related

    protected static string CreateFile(string fileName, string[] lines, string baseFolder)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var dir = Path.GetDirectoryName(fileName);
        dir = CreateDirectory(dir, baseFolder);
        var file = Path.Combine(baseFolder, fileName);
        File.WriteAllLines(file, lines);
        return file;
    }

    protected static string CreateFile(string fileName, string content, string baseFolder)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(baseFolder);

        var dir = Path.GetDirectoryName(fileName);
        dir = CreateDirectory(dir, baseFolder);
        var file = Path.Combine(baseFolder, fileName);
        File.WriteAllText(file, content);
        return file.Replace('\\', '/');
    }

    protected static string UpdateFile(string fileName, string[] lines, string baseFolder)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(baseFolder);

        File.Delete(Path.Combine(baseFolder, fileName));
        return CreateFile(fileName, lines, baseFolder);
    }

    protected static string UpdateFile(string fileName, string content, string baseFolder)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(baseFolder);

        File.Delete(Path.Combine(baseFolder, fileName));
        return CreateFile(fileName, content, baseFolder);
    }

    protected static string CreateDirectory(string dir, string baseFolder)
    {
        if (string.IsNullOrEmpty(dir))
        {
            return string.Empty;
        }

        ArgumentNullException.ThrowIfNull(baseFolder);

        var subDirectory = Path.Combine(baseFolder, dir);
        Directory.CreateDirectory(subDirectory);
        return subDirectory;
    }

    #endregion

    public virtual void Dispose()
    {
        try
        {
            foreach (var folder in _folderCollection)
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }
        catch
        {
        }
    }
}
