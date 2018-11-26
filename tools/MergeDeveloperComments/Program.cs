// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MergeDeveloperComments
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Xml;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Metadata.ManagedReference;

    internal sealed class Program : IDisposable
    {

        #region Fields

        private const int MaxQueueCount = 64;
        private const int ConsumerCount = 4;

        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, List<UidAndComment>> _aggregator = new Dictionary<string, List<UidAndComment>>();
        private readonly Queue<string> _queue = new Queue<string>(MaxQueueCount);
        private readonly string[] _workingSet = new string[ConsumerCount];
        private readonly ManualResetEventSlim _producerCompleted = new ManualResetEventSlim(false);
        private readonly CountdownEvent _consumerCompleted = new CountdownEvent(ConsumerCount);
        private readonly List<Exception> _exceptions = new List<Exception>();
        private readonly string _yamlDirectory;
        private readonly string _developerCommentDirectory;

        #endregion

        #region Entry Point

        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                PrintUsage();
                return 2;
            }
            try
            {
                using (var p = new Program(args[0], args[1]))
                {
                    p.CheckParameters();
                    p.PatchDeveloperComments();
                    p.RebuildReference();
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        #endregion

        #region Constructor

        public Program(string yamlDirectory, string developCommentDirectory)
        {
            _yamlDirectory = yamlDirectory;
            _developerCommentDirectory = developCommentDirectory;
        }

        #endregion

        #region Methods

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("{0} <yamlDirectory> <developCommentDirectory>", AppDomain.CurrentDomain.FriendlyName);
        }

        private void PatchDeveloperComments()
        {
            new Thread(ProduceDeveloperComments).Start();
            for (int i = 0; i < ConsumerCount; i++)
            {
                new Thread(ConsumeDeveloperComments).Start();
            }
            _producerCompleted.Wait();
            while (!_consumerCompleted.Wait(1))
            {
                lock (_syncRoot)
                {
                    Monitor.PulseAll(_syncRoot);
                }
            }
            if (_exceptions.Count > 0)
            {
                throw new AggregateException(_exceptions);
            }
        }

        private void RebuildReference()
        {
            var references = GetReferences();
            foreach (var model in GetViewModels())
            {
                bool dirty = false;
                // Add references for exceptions
                var types = from i in model.Item2.Items
                            where i.Exceptions != null
                            from e in i.Exceptions
                            select e.Type;
                HashSet<string> set = new HashSet<string>(model.Item2.References.Select(r => r.Uid));
                foreach (var type in types)
                {
                    if (set.Add(type))
                    {
                        if (!references.TryGetValue(type, out ReferenceViewModel reference))
                        {
                            reference = references[type] = new ReferenceViewModel() { Uid = type };
                        }

                        model.Item2.References.Add(reference);
                        dirty = true;
                    }
                }

                if (dirty)
                {
                    Console.WriteLine($"Rebuilding references: {model.Item1}");
                    YamlUtility.Serialize(model.Item1, model.Item2, YamlMime.ManagedReference);
                }
            }
        }

        private IEnumerable<Tuple<string, PageViewModel>> GetViewModels()
        {
            foreach (var yaml in Directory.GetFiles(_yamlDirectory, "*.yml", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(yaml) == "toc.yml") continue;
                yield return Tuple.Create(yaml, YamlUtility.Deserialize<PageViewModel>(yaml));
            }
        }

        private Dictionary<string, ReferenceViewModel> GetReferences()
        {
            Dictionary<string, ReferenceViewModel> references = new Dictionary<string, ReferenceViewModel>();
            Dictionary<string, string> summary = new Dictionary<string, string>();
            foreach (var model in GetViewModels())
            {
                foreach (var item in model.Item2.Items)
                {
                    summary[item.Uid] = item.Summary;
                }
                foreach (var reference in model.Item2.References)
                {
                    references[reference.Uid] = reference;
                }
            }

            return references;
        }

        private void CheckParameters()
        {
            bool hasError = false;
            if (!Directory.Exists(_yamlDirectory))
            {
                Console.WriteLine($"Directory not found: {_yamlDirectory}");
                hasError = true;
            }
            else if (!File.Exists(Path.Combine(_yamlDirectory, ".manifest")))
            {
                Console.WriteLine($"Manifest file not found in directory: {_yamlDirectory}");
                hasError = true;
            }
            if (!Directory.Exists(_developerCommentDirectory))
            {
                Console.WriteLine($"Directory not found: {_developerCommentDirectory}");
                hasError = true;
            }
            if (hasError)
            {
                throw new Exception();
            }
        }

        private void ProduceDeveloperComments()
        {
            try
            {
                ProduceDeveloperCommentsCore();
            }
            catch (Exception ex)
            {
                lock (_syncRoot)
                {
                    _exceptions.Add(ex);
                }
            }
            finally
            {
                _producerCompleted.Set();
            }
        }

        private void ProduceDeveloperCommentsCore()
        {
            var map = GetYamlFileMap();
            foreach (var uidAndReader in from f in EnumerateDevelopCommentFiles()
                                         from uidAndReader in EnumerateDeveloperComments(f)
                                         select uidAndReader)
            {
                if (!map.TryGetValue(uidAndReader.Uid, out string yamlFile))
                {
                    continue;
                }
                var uidAndElement = uidAndReader.ToUidAndElement();
                lock (_syncRoot)
                {
                    if (_aggregator.TryGetValue(yamlFile, out List<UidAndComment> list))
                    {
                        list.Add(uidAndElement);
                    }
                    else
                    {
                        while (_queue.Count == MaxQueueCount)
                        {
                            Monitor.Wait(_syncRoot);
                        }
                        if (_queue.Count == 0)
                        {
                            Monitor.PulseAll(_syncRoot);
                        }
                        _queue.Enqueue(yamlFile);
                        _aggregator.Add(yamlFile, new List<UidAndComment> { uidAndElement });
                    }
                }
            }
        }

        private Dictionary<string, string> GetYamlFileMap() =>
            JsonUtility.Deserialize<Dictionary<string, string>>(Path.Combine(_yamlDirectory, ".manifest"));

        private IEnumerable<string> EnumerateDevelopCommentFiles() =>
            Directory.EnumerateFiles(_developerCommentDirectory, "*.xml", SearchOption.AllDirectories);

        private IEnumerable<UidAndReader> EnumerateDeveloperComments(string file)
        {
            Console.WriteLine($"Loading developer comments form file: {file}");
            return from reader in
                       new Func<XmlReader>(() => XmlReader.Create(file))
                       .EmptyIfThrow()
                       .ProtectResource()
                   where reader.ReadToFollowing("members")
                   from apiReader in reader.Elements("member")
                   let commentId = apiReader.GetAttribute("name")
                   where commentId != null && commentId.Length > 2 && commentId[1] == ':'
                   select new UidAndReader { Uid = commentId.Substring(2), Reader = apiReader };
        }

        private void ConsumeDeveloperComments()
        {
            try
            {
                ConsumeDeveloperCommentsCore();
            }
            catch (Exception ex)
            {
                lock (_syncRoot)
                {
                    _exceptions.Add(ex);
                }
            }
            finally
            {
                _consumerCompleted.Signal();
            }
        }

        private void ConsumeDeveloperCommentsCore()
        {
            string yamlFile = null;
            while (true)
            {
                List<UidAndComment> list;
                lock (_syncRoot)
                {
                    if (yamlFile != null)
                    {
                        for (int i = 0; i < _workingSet.Length; i++)
                        {
                            if (_workingSet[i] == yamlFile)
                            {
                                _workingSet[i] = null;
                            }
                        }
                    }
                    yamlFile = GetYamlFileFromQueue();
                    if (yamlFile == null)
                    {
                        return;
                    }
                    list = _aggregator[yamlFile];
                    _aggregator.Remove(yamlFile);
                }
                try
                {
                    PatchYamlFile(yamlFile, list);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred in file {yamlFile}:\r\n{ex}");
                }
            }
        }

        private string GetYamlFileFromQueue()
        {
            while (true)
            {
                if (_queue.Count == 0)
                {
                    if (_producerCompleted.Wait(0))
                    {
                        return null;
                    }
                    Monitor.Wait(_syncRoot);
                    continue;
                }
                if (_queue.Count < MaxQueueCount / 2)
                {
                    Monitor.Pulse(_syncRoot);
                }
                var yamlFile = _queue.Dequeue();
                if (Array.IndexOf(_workingSet, yamlFile) != -1)
                {
                    _queue.Enqueue(yamlFile);
                    if (_queue.Count < ConsumerCount)
                    {
                        Monitor.Wait(_syncRoot);
                    }
                    continue;
                }
                for (int i = 0; i < _workingSet.Length; i++)
                {
                    if (_workingSet[i] == null)
                    {
                        _workingSet[i] = yamlFile;
                        return yamlFile;
                    }
                }
                Environment.FailFast("Bug in consumer!");
            }
        }

        private void PatchYamlFile(string yamlFile, List<UidAndComment> list)
        {
            var path = Path.Combine(_yamlDirectory, yamlFile);
            if (!File.Exists(path))
            {
                Console.WriteLine($"Cannot find file {yamlFile}.");
                return;
            }
            var pageVM = YamlUtility.Deserialize<PageViewModel>(path);
            foreach (var uidAndComment in list)
            {
                var item = pageVM.Items.Find(x => x.Uid == uidAndComment.Uid);
                if (item == null)
                {
                    Console.WriteLine($"Cannot find {uidAndComment.Uid} in file {yamlFile}.");
                    continue;
                }
                PatchViewModel(item, uidAndComment.Comment);
            }
            Console.WriteLine($"Patching yaml: {yamlFile}");
            YamlUtility.Serialize(path, pageVM, YamlMime.ManagedReference);
        }

        private void PatchViewModel(ItemViewModel item, string comment)
        {
            var commentModel = TripleSlashCommentModel.CreateModel(comment, SyntaxLanguage.CSharp, TripleSlashCommentParserContext.Instance);
            var summary = commentModel.Summary;
            if (!string.IsNullOrEmpty(summary))
            {
                item.Summary = summary;
            }
            var remarks = commentModel.Remarks;
            if (!string.IsNullOrEmpty(remarks))
            {
                item.Remarks = remarks;
            }
            var exceptions = commentModel.Exceptions;
            if (exceptions != null && exceptions.Count > 0)
            {
                item.Exceptions = exceptions;
            }
            var sees = commentModel.Sees;
            if (sees != null && sees.Count > 0)
            {
                item.Sees = sees;
            }
            var seeAlsos = commentModel.SeeAlsos;
            if (seeAlsos != null && seeAlsos.Count > 0)
            {
                item.SeeAlsos = seeAlsos;
            }
            var examples = commentModel.Examples;
            if (examples != null && examples.Count > 0)
            {
                item.Examples = examples;
            }
            if (item.Syntax != null)
            {
                if (item.Syntax.Parameters != null)
                {
                    foreach (var p in item.Syntax.Parameters)
                    {
                        var description = commentModel.GetParameter(p.Name);
                        if (!string.IsNullOrEmpty(description))
                        {
                            p.Description = description;
                        }
                    }
                }
                if (item.Syntax.TypeParameters != null)
                {
                    foreach (var p in item.Syntax.TypeParameters)
                    {
                        var description = commentModel.GetTypeParameter(p.Name);
                        if (!string.IsNullOrEmpty(description))
                        {
                            p.Description = description;
                        }
                    }
                }
                if (item.Syntax.Return != null)
                {
                    var returns = commentModel.Returns;
                    if (!string.IsNullOrEmpty(returns))
                    {
                        item.Syntax.Return.Description = returns;
                    }
                }
            }
            // todo more.
        }

        #region IDisposable

        public void Dispose()
        {
            _consumerCompleted.Dispose();
            _producerCompleted.Dispose();
        }

        #endregion

        #endregion

        #region Nested Class

        internal sealed class UidAndReader
        {
            public string Uid { get; set; }
            public XmlReader Reader { get; set; }
            public UidAndComment ToUidAndElement() => new UidAndComment { Uid = Uid, Comment = Reader.ReadOuterXml() };
        }

        internal sealed class UidAndComment
        {
            public string Uid { get; set; }
            public string Comment { get; set; }
        }

        internal sealed class TripleSlashCommentParserContext : ITripleSlashCommentParserContext
        {
            public static readonly TripleSlashCommentParserContext Instance = new TripleSlashCommentParserContext
            {
                AddReferenceDelegate = (s, e) => { },
                ResolveCRef = null
            };

            public Action<string, string> AddReferenceDelegate { get; set; }
            public Func<string, CRefTarget> ResolveCRef { get; set; }
            public bool PreserveRawInlineComments { get; set; }
            public SourceDetail Source { get; set; }
            public string CodeSourceBasePath { get; set; }
        }

        #endregion
    }
}
