// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.FileAbstractLayer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    public class LinkFileSystem
    {

        #region Consts/Fields
        private const int MaxRetry = 3;
        private readonly List<PathMapping> _outputList = new List<PathMapping>();
        private Dictionary<RelativePath, string> _allInputs;
        #endregion

        #region Properties

        public ImmutableArray<PathMapping> Mappings { get; }

        public string OutputFolder { get; }

        public bool IsReadOnly => string.IsNullOrEmpty(OutputFolder);

        #endregion

        #region Constructors

        public LinkFileSystem(params PathMapping[] mappings)
            : this(mappings, null) { }

        public LinkFileSystem(IEnumerable<PathMapping> mappings, string outputFolder = null)
        {
            if (mappings == null)
            {
                throw new ArgumentNullException(nameof(mappings));
            }
            Mappings = mappings.ToImmutableArray();
            OutputFolder = outputFolder;
        }

        #endregion

        #region Public Methods

        public ImmutableHashSet<RelativePath> GetAllInputFiles()
        {
            EnsureAllInputs();
            return _allInputs.Keys.ToImmutableHashSet();
        }

        public ImmutableHashSet<RelativePath> GetAllOutputFiles()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException();
            }
            return (from m in _outputList
                    select m.LogicPath).ToImmutableHashSet();
        }

        public LinkFileSystem CreateNextLayer(string outputFolder = null) =>
            new LinkFileSystem(_outputList, outputFolder);

        public bool Exists(string file)
        {
            return Exists((RelativePath)file);
        }

        public bool Exists(RelativePath file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            return FindPhysicPathNoThrow(file) != null;
        }

        public FileStream OpenRead(string file)
        {
            return OpenRead((RelativePath)file);
        }

        public FileStream OpenRead(RelativePath file)
        {
            string pp = FindPhysicPath(file.GetPathFromWorkingFolder());
            return File.OpenRead(pp);
        }

        public string ReadAllText(string file)
        {
            using (var sr = new StreamReader(OpenRead(file)))
            {
                return sr.ReadToEnd();
            }
        }

        public string ReadAllText(RelativePath file)
        {
            using (var sr = new StreamReader(OpenRead(file)))
            {
                return sr.ReadToEnd();
            }
        }

        public FileStream Create(string file) =>
            Create((RelativePath)file);

        public FileStream Create(RelativePath file)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException();
            }
            var pair = CreateRandomFileStream();
            _outputList.Add(new PathMapping(file.GetPathFromWorkingFolder(), Path.Combine(OutputFolder, pair.Item1)));
            return pair.Item2;
        }

        public void Copy(string sourceFileName, string destFileName) =>
            Copy((RelativePath)sourceFileName, (RelativePath)destFileName);

        public void Copy(RelativePath sourceFileName, RelativePath destFileName)
        {
            string pp = FindPhysicPath(sourceFileName.GetPathFromWorkingFolder());
            _outputList.Add(new PathMapping(destFileName.GetPathFromWorkingFolder(), pp));
        }

        #region Help Methods

        public string GetRandomEntry()
        {
            string name;
            do
            {
                name = Path.GetRandomFileName();
            } while (Directory.Exists(Path.Combine(OutputFolder, name)) || File.Exists(Path.Combine(OutputFolder, name)));
            return name;
        }

        public string CreateRandomFileName() =>
            RetryIO(() =>
            {
                string fileName = GetRandomEntry();
                using (File.Create(Path.Combine(OutputFolder, fileName)))
                {
                    // create new zero length file.
                }
                return fileName;
            });

        public Tuple<string, FileStream> CreateRandomFileStream()
        {
            return RetryIO(() =>
            {
                var file = GetRandomEntry();
                return Tuple.Create(file, File.Create(Path.Combine(OutputFolder, file)));
            });
        }

        public static T RetryIO<T>(Func<T> func)
        {
            var count = 0;
            while (true)
            {
                try
                {
                    return func();
                }
                catch (IOException)
                {
                    if (count++ >= MaxRetry)
                    {
                        throw;
                    }
                }
            }
        }

        public static void RetryIO(Action action)
        {
            var count = 0;
            while (true)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException)
                {
                    if (count++ >= MaxRetry)
                    {
                        throw;
                    }
                }
            }
        }

        #endregion

        #endregion

        #region Private Methods

        private string FindPhysicPath(RelativePath file)
        {
            var physicPath = FindPhysicPathNoThrow(file);
            if (physicPath == null)
            {
                throw new FileNotFoundException("File not found.", file);
            }
            return physicPath;
        }

        private string FindPhysicPathNoThrow(RelativePath file)
        {
            var path = file.GetPathFromWorkingFolder();
            foreach (var m in Mappings)
            {
                if (m.IsFolder)
                {
                    var localPath = path - m.LogicPath;
                    if (m.AllowMoveOut || localPath.ParentDirectoryCount == 0)
                    {
                        var physicPath = Path.Combine(m.PhysicPath, localPath.ToString());
                        if (File.Exists(physicPath))
                        {
                            return physicPath;
                        }
                    }
                }
                else if (m.LogicPath == file)
                {
                    return m.PhysicPath;
                }
            }
            return null;
        }

        private void EnsureAllInputs()
        {
            if (_allInputs == null)
            {
                var allInputs = new Dictionary<RelativePath, string>();
                foreach (var m in Mappings)
                {
                    if (m.IsFolder)
                    {
                        var fp = Path.GetFullPath(m.PhysicPath);
                        foreach (var f in Directory.EnumerateFiles(fp, "*.*", SearchOption.AllDirectories))
                        {
                            var lf = f.Substring(fp.Length + 1);
                            var rp = m.LogicPath + (RelativePath)lf;
                            if (!allInputs.ContainsKey(rp))
                            {
                                allInputs.Add(rp, f);
                            }
                        }
                    }
                    else
                    {
                        allInputs.Add(m.LogicPath, m.PhysicPath);
                    }
                }
                _allInputs = allInputs;
            }
        }

        #endregion

    }
}
